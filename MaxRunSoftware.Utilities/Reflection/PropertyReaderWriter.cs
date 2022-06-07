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

    public void SetValue(object instance, object propertyValue, TypeConverter converter = null)
    {
        if (!CanSet) throw new InvalidOperationException("Cannot SET property " + ToString());

        if (converter != null && propertyValue != null)
        {
            var tSource = propertyValue.GetType();
            var tTarget = PropertyInfo.PropertyType;
            if (tSource != tTarget && !tTarget.IsAssignableFrom(tSource))
            {
                propertyValue = converter(propertyValue, tTarget);
            }
        }
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

        var o = propertyGetter(instance);

        return o;
    }

    public object CopyValue(object sourceInstance, object targetInstance)
    {
        var o = GetValue(sourceInstance);
        SetValue(targetInstance, o);
        return o;
    }

    public override string ToString() => PropertyInfo.DeclaringType.FullNameFormatted() + "." + PropertyInfo.Name + "{ " + (CanGet ? "get; " : "") + (CanSet ? "set; " : "") + "}";
}
