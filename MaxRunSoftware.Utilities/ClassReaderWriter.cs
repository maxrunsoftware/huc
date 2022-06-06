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

using System.Linq.Expressions;

namespace MaxRunSoftware.Utilities;

public sealed class PropertyReaderWriter
{
    private readonly Action<object, object> propertySetter;
    private readonly Func<object, object> propertyGetter;
    private object DefaultNullValue { get; }
    public bool CanGet => propertyGetter != null;
    public bool CanSet => propertySetter != null;
    public PropertyInfo PropertyInfo { get; }
    public string Name { get; }
    public bool IsStatic { get; }
    public bool IsInstance => !IsStatic;

    public PropertyReaderWriter(PropertyInfo propertyInfo, bool? isStatic = null)
    {
        PropertyInfo = propertyInfo.CheckNotNull(nameof(propertyInfo));
        Name = propertyInfo.Name;
        IsStatic = isStatic ?? propertyInfo.IsStatic();

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

    public void SetValueRaw(object instance, object propertyValue)
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

    public void SetValue(object instance, object propertyValue, TypeConverter converter = null)
    {
        if (propertyValue != null)
        {
            var tSource = propertyValue.GetType();
            var tTarget = PropertyInfo.PropertyType;
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

        SetValueRaw(instance, propertyValue);
    }

    public object GetValueRaw(object instance)
    {
        if (!CanGet) throw new InvalidOperationException("Cannot GET property " + ToString());
        return propertyGetter(instance);
    }

    public object GetValue(object instance, TypeConverter converter = null, Type returnType = null)
    {
        if (converter != null && returnType == null) throw new ArgumentNullException(nameof(returnType), "If argument '" + nameof(converter) + "' is provided then argument '" + nameof(returnType) + "' must also be provided");
        if (converter == null && returnType != null) throw new ArgumentNullException(nameof(converter), "If argument '" + nameof(returnType) + "' is provided then argument '" + nameof(converter) + "' must also be provided");

        var o = GetValueRaw(instance);

        if (o != null)
        {
            if (converter != null)
            {
                var tSource = PropertyInfo.PropertyType;
                var tTarget = returnType;
                if (tSource != tTarget)
                {
                    if (converter == null) throw new ArgumentException($"No converter available for property {ToString()} to convert {tSource.FullNameFormatted()} to {tTarget.FullNameFormatted()} value: {o.ToStringGuessFormat()}", nameof(converter));
                    o = converter(o, tTarget);
                }
            }
        }

        return o;
    }

    public object CopyValue(object sourceInstance, object targetInstance)
    {
        var o = GetValueRaw(sourceInstance);
        SetValueRaw(targetInstance, o);
        return o;
    }

    public override string ToString() => PropertyInfo.DeclaringType.FullNameFormatted() + "." + PropertyInfo.Name + "{ " + (CanGet ? "get; " : "") + (CanSet ? "set; " : "") + "}";
}

public sealed class ClassReaderWriter
{
    private static readonly IBucketReadOnly<Type, ClassReaderWriter> cache = new BucketCacheThreadSafeCopyOnWrite<Type, ClassReaderWriter>(t => new ClassReaderWriter(t));

    public Type Type { get; }
    public IReadOnlyDictionary<string, PropertyReaderWriter> Properties { get; }

    public ClassReaderWriter(Type type)
    {
        Type = type.CheckNotNull(nameof(type));

        var list = new List<PropertyReaderWriter>();
        foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            list.Add(new PropertyReaderWriter(propertyInfo, isStatic: false));
        }
        foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            list.Add(new PropertyReaderWriter(propertyInfo, isStatic: true));
        }
        list = list.Where(o => o.CanGet || o.CanSet).ToList();

        var d = new Dictionary<string, PropertyReaderWriter>();
        foreach (var prop in list)
        {
            d.Add(prop.Name, prop);
        }
        Properties = d.AsReadOnly();
    }

    public static ClassReaderWriter Get(Type type) => cache[type.CheckNotNull(nameof(type))];

    public static IEnumerable<PropertyReaderWriter> GetProperties(
        Type type,
        bool canGet = false,
        bool canSet = false,
        bool isStatic = false,
        bool isInstance = false
        )
    {
        var list = new List<PropertyReaderWriter>();
        foreach (var prop in Get(type).Properties.Values)
        {
            if (canGet && !prop.CanGet) continue;
            if (canSet && !prop.CanSet) continue;
            if (!isStatic && !isInstance) continue; // don't include anything? maybe throw exception because you probably forgot something
            if (!isStatic && isInstance && prop.IsStatic) continue; // don't include static
            if (isStatic && !isInstance && prop.IsInstance) continue; // don't include instance
                                                                      //if (isStatic && isInstance) ; // include everything

            list.Add(prop);
        }
        return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
    }
}
