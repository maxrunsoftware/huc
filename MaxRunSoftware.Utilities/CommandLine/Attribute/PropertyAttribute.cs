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

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace MaxRunSoftware.Utilities.CommandLine;

public abstract class PropertyAttribute : AttributeBase
{
    public bool? IsTrimmed { get; init; }

    protected PropertyAttribute(string description, CallerInfo callerInfo) : base(description, callerInfo) { }

    protected override void Properties_Build(List<object?> list) => list.Add(IsTrimmed);
}

public abstract class PropertyAttributeDetail<TAttribute> where TAttribute : PropertyAttribute
{
    protected PropertyAttributeDetail(TypeSlim parent, PropertyInfo info, TAttribute attribute)
    {
        Parent = parent;
        Info = info;
        Attribute = attribute;

        Name = (Attribute.Name.TrimOrNull() ?? Info.Name.TrimOrNull()).CheckNotNull(nameof(Name));

        Description = Attribute.Description.TrimOrNull().CheckNotNull(nameof(Description));

        var vt = GetValueType(Info);
        ValueType = vt.valueType;
        IsCollection = vt.isCollection;

        if (IsCollection) addMethod = GetValueAddMethod(Info.PropertyType);

        compareToMethod = GetCompareToMethod(ValueType);
    }


    public TypeSlim Parent { get; }
    public PropertyInfo Info { get; }
    public TAttribute Attribute { get; }
    public bool IsNullable => Info.IsNullable();
    public bool IsTrimmed => Attribute.IsTrimmed ?? GetIsTrimmedDefault();
    public string Name { get; }
    public string Description { get; }
    public override string ToString() => $"[{Info.PropertyType.NameFormatted()}] {Parent.Type.FullNameFormatted()}.{Info.Name}";
    public bool IsComparable => compareToMethod != null;
    public Type ValueType { get; }
    public bool IsWritable => Info.GetSetMethod(true) != null; // TODO: https://stackoverflow.com/a/18937268
    public bool IsCollection { get; }

    private readonly MethodInfo? compareToMethod;
    private readonly MethodInfo? addMethod;


    private static MethodInfo? GetCompareToMethod(Type propertyType)
    {
        if (propertyType.IsAssignableTo(typeof(IComparable<>).MakeGenericType(propertyType)))
        {
            // ReSharper disable once ReplaceWithSingleCallToFirst
            return propertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(nameof(IComparable.CompareTo)))
                .Where(mi => mi.ReturnType == typeof(int))
                .Where(mi => mi.GetParameters().Length == 1)
                .Where(mi => mi.GetParameters()[0].ParameterType.IsAssignableFrom(propertyType))
                .First();
        }

        if (propertyType.IsAssignableTo(typeof(IComparable)))
        {
            // ReSharper disable once ReplaceWithSingleCallToFirst
            return propertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(nameof(IComparable.CompareTo)))
                .Where(mi => mi.ReturnType == typeof(int))
                .Where(mi => mi.GetParameters().Length == 1)
                .Where(mi => mi.GetParameters()[0].ParameterType == typeof(object))
                .First();
        }

        //
        return null;
    }

    private static (bool isCollection, Type valueType) GetValueType(PropertyInfo info)
    {
        var t = info.PropertyType;
        var tCollectionGeneric = typeof(ICollection<>);

        if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableTo(tCollectionGeneric))
        {
            return (true, t.GetGenericArguments().First());
        }

        foreach (var iface in t.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;
            if (iface.GetGenericTypeDefinition() == tCollectionGeneric)
            {
                return (true, t.GetGenericArguments().First());
            }
        }

        return (false, t);
    }

    private static MethodInfo GetValueAddMethod(Type propertyType)
    {
        var names = new[] { nameof(IList.Add) };

        foreach (var name in names)
        {
            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
            var mi = propertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(name))
                .Where(mi => mi.GetParameters().Length == 1)
                .FirstOrDefault();
        }

        throw new Exception("Could not find any method named: " + names.ToStringDelimited(", "));
    }


    protected abstract bool GetIsTrimmedDefault();

    protected virtual void SetValue(object instance, string? value)
    {
        if (addMethod != null)
        {
            var getMethod = Info.GetGetMethod();
        }
    }

    protected string? ParseValueToString(object? value)
    {
        var s = value?.ToString();
        if (IsTrimmed) s = s.TrimOrNull();
        return s;
    }

    protected object? ParseValueToObject(string? value)
    {
        if (IsTrimmed) value = value.TrimOrNull();
        return Util.ChangeType(value, Info.PropertyType);
    }


    #region Validate

    protected void ValidateValue(string? value)
    {
        var _ = ParseValueToObject(value);
    }

    protected void ValidateMin(string? valueString, string min)
    {
        if (ValidateCompare(valueString, min) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(min), min, $"Value {valueString} cannot be less than {min}");
        }
    }

    protected void ValidateMax(string? valueString, string max)
    {
        if (ValidateCompare(valueString, max) > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, $"Value {valueString} cannot be greater than {max}");
        }
    }

    private int ValidateCompare(string? valueString, string? valueOther)
    {
        if (!IsComparable || compareToMethod == null) throw new ArgumentException($"Cannot compare because {this} does not implement {nameof(IComparable)}");
        if (compareToMethod == null) return 0;

        if (valueString == null) return 0;
        var value = ParseValueToObject(valueString);
        if (value == null) return 0;

        if (valueOther == null) return 0;
        var valMinMaxConverted = ParseValueToObject(valueOther);
        if (valMinMaxConverted == null) return 0;

        var compVal = (int)compareToMethod.Invoke(value, new[] { valMinMaxConverted })!;
        return compVal;
    }

    #endregion Validate
}

public static class PropertyAttributeExtensions { }
