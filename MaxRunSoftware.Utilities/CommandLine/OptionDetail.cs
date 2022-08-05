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

namespace MaxRunSoftware.Utilities.CommandLine;

public class OptionDetail : PropertyDetail<OptionAttribute>
{
    public string? ShortName { get; }
    public string? Default { get; }
    public string? Min { get; }
    public string? Max { get; }
    public bool IsRequired { get; }
    public bool IsHidden { get; }
    private readonly MethodInfo? compareTo;

    public OptionDetail(TypeSlim type, PropertyInfo info, OptionAttribute attribute) : base(type, info, attribute)
    {
        var shortName = Attribute.ShortName.TrimOrNull();
        if (shortName == null && !Attribute.NoShortName)
        {
            shortName = Name.Select((c, i) => i == 0 || char.IsUpper(c) || char.IsNumber(c) ? c.ToString() : string.Empty).ToStringDelimited("");
        }

        ShortName = shortName;

        if (Attribute.Default != null && Attribute.IsRequired.HasValue && Attribute.IsRequired.Value) throw new ArgumentException($"If {Default} has value then {IsRequired} cannot be true", nameof(Default));


        var defaultString = Attribute.Default?.ToString();
        if (IsTrimmed) defaultString = defaultString.TrimOrNull();
        if (defaultString != null)
        {
            var _ = ConvertValue(defaultString);
        }

        Default = defaultString;


        var minString = Attribute.Min?.ToString();
        if (IsTrimmed) minString = minString.TrimOrNull();
        if (minString != null)
        {
            var _ = ConvertValue(minString);
        }

        Min = minString;


        var maxString = Attribute.Max?.ToString();
        if (IsTrimmed) maxString = maxString.TrimOrNull();
        if (maxString != null)
        {
            var _ = ConvertValue(maxString);
        }

        Max = maxString;

        if (Attribute.Min != null || Attribute.Max != null)
        {
            var propertyType = Nullable.GetUnderlyingType(Info.PropertyType) ?? Info.PropertyType;
            if (propertyType.IsEnum) throw new ArgumentException($"Cannot define {nameof(Min)} or {nameof(Max)} for Enum");
            if (propertyType == typeof(string)) throw new ArgumentException($"Cannot define {nameof(Min)} or {nameof(Max)} for String");
            compareTo = GetCompareTo();
        }

        if (compareTo != null)
        {
            ValidateMin(Default);
            ValidateMax(Default);
        }

        IsHidden = Attribute.IsHidden;

        IsRequired = Attribute.IsRequired ?? !Info.IsNullable();
    }

    protected MethodInfo GetCompareTo()
    {
        if (Info.PropertyType.IsAssignableTo(typeof(IComparable<>).MakeGenericType(Info.PropertyType)))
        {
            // ReSharper disable once ReplaceWithSingleCallToFirst
            return Info.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(nameof(IComparable.CompareTo)))
                .Where(mi => mi.ReturnType == typeof(int))
                .Where(mi => mi.GetParameters().Length == 1)
                .Where(mi => mi.GetParameters()[0].ParameterType.IsAssignableFrom(Info.PropertyType))
                .First();
        }

        if (Info.PropertyType.IsAssignableTo(typeof(IComparable)))
        {
            // ReSharper disable once ReplaceWithSingleCallToFirst
            return Info.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(nameof(IComparable.CompareTo)))
                .Where(mi => mi.ReturnType == typeof(int))
                .Where(mi => mi.GetParameters().Length == 1)
                .Where(mi => mi.GetParameters()[0].ParameterType == typeof(object))
                .First();
        }

        throw new ArgumentException($"Type {Info.PropertyType.Name} does not implement interface " + typeof(IComparable<>).MakeGenericType(Info.PropertyType).NameFormatted() + " or interface " + typeof(IComparable).NameFormatted());
    }

    public override void SetValue(object instance, string? valueString)
    {
        if (IsTrimmed) valueString = valueString.TrimOrNull();
        if (valueString == null) valueString = Default;
        if (IsTrimmed) valueString = valueString.TrimOrNull();
        if (valueString == null && IsRequired) throw new ArgumentNullException(nameof(valueString), "Property value is required");
        ValidateMin(valueString);
        ValidateMax(valueString);
        base.SetValue(instance, valueString);
    }

    private void ValidateMin(string? valueString)
    {
        if (ValidateCompare(valueString, Min) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Min), Min, $"Value {valueString} cannot be less than {Min}");
        }
    }
    private void ValidateMax(string? valueString)
    {
        if (ValidateCompare(valueString, Max) > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Max), Max, $"Value {valueString} cannot be greater than {Max}");
        }
    }

    private int ValidateCompare(string? valueString, string? valMinMax)
    {
        if (compareTo == null) return 0;

        if (valueString == null) return 0;
        var value = ConvertValue(valueString);
        if (value == null) return 0;

        if (valMinMax == null) return 0;
        var valMinMaxConverted = ConvertValue(valMinMax);
        if (valMinMaxConverted == null) return 0;

        var compVal = (int)compareTo.Invoke(value, new[] { valMinMaxConverted })!;
        return compVal;
    }
}
