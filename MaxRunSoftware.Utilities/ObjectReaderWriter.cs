// /*
// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MaxRunSoftware.Utilities
{
    public class ObjectReaderWriter
    {
        internal sealed class ClassReaderWriter
        {
            private static readonly IBucketReadOnly<Type, ClassReaderWriter> CACHE = new BucketCacheThreadSafeCopyOnWrite<Type, ClassReaderWriter>(o => new ClassReaderWriter(o));
            private readonly IReadOnlyDictionary<string, PropertyReaderWriter> propertiesCaseSensitive;
            private readonly IReadOnlyDictionary<string, PropertyReaderWriter> propertiesCaseInsensitive;
            private readonly Type type;

            private ClassReaderWriter(Type type)
            {
                this.type = type.CheckNotNull(nameof(type));
                var props = PropertyReaderWriter.Create(type);

                var d1 = new Dictionary<string, PropertyReaderWriter>();
                var d2 = new Dictionary<string, PropertyReaderWriter>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in props)
                {
                    d1.Add(prop.PropertyInfo.Name, prop);
                    d2[prop.PropertyInfo.Name] = prop;
                }
                propertiesCaseSensitive = d1.AsReadOnly();
                propertiesCaseInsensitive = d2.AsReadOnly();
            }

            private static ClassReaderWriter Create(Type type) => CACHE[type.CheckNotNull(nameof(type))];

            private PropertyReaderWriter GetProperty(string propertyName)
            {
                propertyName = propertyName.CheckNotNullTrimmed(nameof(propertyName));
                if (propertiesCaseSensitive.TryGetValue(propertyName, out var value)) return value;
                if (propertiesCaseInsensitive.TryGetValue(propertyName, out value)) return value;
                throw new ArgumentException("Public property not found: " + type.FullNameFormatted() + "." + propertyName);
            }

            private void SetPropertyValueInternal(object instance, string propertyName, object propertyValue, TypeConverter converter = null)
            {
                var p = GetProperty(propertyName);

                if (propertyValue != null)
                {
                    var tSource = propertyValue.GetType();
                    var tTarget = p.PropertyInfo.PropertyType;
                    if (tSource != tTarget)
                    {
                        if (!tTarget.IsAssignableFrom(tSource))
                        {
                            if (converter == null)
                            {
                                // throw new ArgumentException($"No converter available for property
                                // {p} to convert {tSource.FullNameFormatted()} to
                                // {tTarget.FullNameFormatted()} value:
                                // {propertyValue.ToStringGuessFormat()}", nameof(converter));
                                propertyValue = Util.ChangeType(propertyValue, tTarget);
                            }
                            else
                            {
                                propertyValue = converter(propertyValue, tTarget);
                            }
                        }
                    }
                }

                p.SetValue(instance, propertyValue);
            }

            private object GetPropertyValueInternal(object instance, string propertyName, TypeConverter converter = null, Type returnType = null)
            {
                if (converter != null && returnType == null) throw new ArgumentNullException(nameof(returnType), "If argument '" + nameof(converter) + "' is provided then argument '" + nameof(returnType) + "' must also be provided");
                if (returnType != null && converter == null) throw new ArgumentNullException(nameof(converter), "If argument '" + nameof(returnType) + "' is provided then argument '" + nameof(converter) + "' must also be provided");

                var p = GetProperty(propertyName);
                var o = p.GetValue(instance);

                if (o != null)
                {
                    if (converter != null)
                    {
                        var tSource = p.PropertyInfo.PropertyType;
                        var tTarget = returnType;
                        if (tSource != tTarget)
                        {
                            if (converter == null) throw new ArgumentException($"No converter available for property {p} to convert {tSource.FullNameFormatted()} to {tTarget.FullNameFormatted()} value: {o.ToStringGuessFormat()}", nameof(converter));
                            o = converter(o, tTarget);
                        }
                    }
                }

                return o;
            }

            public static void SetPropertyValue(object instance, string propertyName, object propertyValue, TypeConverter converter = null)
            {
                instance.CheckNotNull(nameof(instance));
                Create(instance.GetType()).SetPropertyValueInternal(instance, propertyName, propertyValue, converter);
            }

            public static object GetPropertyValue(object instance, string propertyName, TypeConverter converter = null, Type returnType = null)
            {
                instance.CheckNotNull(nameof(instance));
                return Create(instance.GetType()).GetPropertyValueInternal(instance, propertyName, converter, returnType);
            }

            public static void SetPropertyValue(Type type, string propertyName, object propertyValue, TypeConverter converter = null)
            {
                type.CheckNotNull(nameof(type));
                Create(type).SetPropertyValueInternal(null, propertyName, propertyValue, converter);
            }

            public static object GetPropertyValue(Type type, string propertyName, TypeConverter converter = null, Type returnType = null)
            {
                type.CheckNotNull(nameof(type));
                return Create(type).GetPropertyValueInternal(null, propertyName, converter, returnType);
            }
        }

        internal sealed class PropertyReaderWriter
        {
            private static readonly IBucketReadOnly<Type, IReadOnlyList<PropertyReaderWriter>> CACHE = new BucketCacheThreadSafeCopyOnWrite<Type, IReadOnlyList<PropertyReaderWriter>>(o => CreateInternal(o));
            private readonly Action<object, object> propertySetter;
            private readonly Func<object, object> propertyGetter;
            private object DefaultNullValue { get; }
            public bool CanGet => propertyGetter != null;
            public bool CanSet => propertySetter != null;
            public PropertyInfo PropertyInfo { get; }
            public bool IsStatic { get; }

            private PropertyReaderWriter(PropertyInfo propertyInfo, bool isStatic)
            {
                PropertyInfo = propertyInfo.CheckNotNull(nameof(propertyInfo));
                IsStatic = isStatic;

                // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
                var pt = propertyInfo.PropertyType;

                if (pt.IsPrimitive || pt.IsValueType || pt.IsEnum)
                {
                    DefaultNullValue = Activator.CreateInstance(pt);
                }
                else
                {
                    DefaultNullValue = null;
                }

                propertyGetter = CreatePropertyGetter(propertyInfo);
                propertySetter = CreatePropertySetter(propertyInfo);
            }

            private static Func<object, object> CreatePropertyGetter(PropertyInfo propertyInfo)
            {
                if (!propertyInfo.CanRead) return null;
                // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
                var mi = propertyInfo.GetGetMethod();
                if (mi == null) return null;
                //IsStatic = mi.IsStatic;
                var instance = Expression.Parameter(typeof(object), "instance");
                var callExpr = mi.IsStatic   // Is this a static property
                    ? Expression.Call(null, mi)
                    : Expression.Call(Expression.Convert(instance, propertyInfo.DeclaringType), mi);

                var unaryExpression = Expression.TypeAs(callExpr, typeof(object));
                var action = Expression.Lambda<Func<object, object>>(unaryExpression, instance).Compile();
                return action;
            }

            private static Action<object, object> CreatePropertySetter(PropertyInfo propertyInfo)
            {
                if (!propertyInfo.CanWrite) return null;
                // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
                var mi = propertyInfo.GetSetMethod();
                if (mi == null) return null;

                //IsStatic = mi.IsStatic;
                var instance = Expression.Parameter(typeof(object), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                var value2 = Expression.Convert(value, propertyInfo.PropertyType);
                var callExpr = mi.IsStatic   // Is this a static property
                    ? Expression.Call(null, mi, value2)
                    : Expression.Call(Expression.Convert(instance, propertyInfo.DeclaringType), mi, value2);
                var action = Expression.Lambda<Action<object, object>>(callExpr, instance, value).Compile();

                return action;
            }

            private static IReadOnlyList<PropertyReaderWriter> CreateInternal(Type type)
            {
                var list = new List<PropertyReaderWriter>();

                foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    list.Add(new PropertyReaderWriter(propertyInfo, false));
                }

                foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    list.Add(new PropertyReaderWriter(propertyInfo, true));
                }

                return list.Where(o => o.CanGet || o.CanSet).ToList().AsReadOnly();
            }

            public static IReadOnlyList<PropertyReaderWriter> Create(Type type) => CACHE[type.CheckNotNull(nameof(type))];

            public override string ToString() => PropertyInfo.DeclaringType.FullNameFormatted() + "." + PropertyInfo.Name + "{ " + (CanGet ? "get; " : "") + (CanSet ? "set; " : "") + "}";

            public void SetValue(object instance, object propertyValue)
            {
                if (!CanSet) throw new InvalidOperationException("Cannot SET property " + ToString());
                if (propertyValue == null)
                {
                    propertySetter(instance, DefaultNullValue);
                }
                else
                {
                    propertySetter(instance, propertyValue);
                }
            }

            public object GetValue(object instance)
            {
                if (!CanGet) throw new InvalidOperationException("Cannot GET property " + ToString());
                return propertyGetter(instance);
            }
        }

        public static void SetPropertyValue(object instance, string propertyName, object propertyValue, TypeConverter converter = null) => ClassReaderWriter.SetPropertyValue(instance, propertyName, propertyValue, converter);

        public static object GetPropertyValue(object instance, string propertyName, TypeConverter converter = null, Type returnType = null) => ClassReaderWriter.GetPropertyValue(instance, propertyName, converter, returnType);

        public static void SetPropertyValueStatic(Type type, string propertyName, object propertyValue, TypeConverter converter = null) => ClassReaderWriter.SetPropertyValue(type, propertyName, propertyValue, converter);

        public static object GetPropertyValueStatic(Type type, string propertyName, TypeConverter converter = null, Type returnType = null) => ClassReaderWriter.GetPropertyValue(type, propertyName, converter, returnType);

        public static object CopyPropertyValue(object sourceInstance, object targetInstance, string propertyName)
        {
            var o = GetPropertyValue(sourceInstance, propertyName);
            SetPropertyValue(targetInstance, propertyName, o);
            return o;
        }

    }
}
