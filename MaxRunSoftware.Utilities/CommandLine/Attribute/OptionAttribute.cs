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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MaxRunSoftware.Utilities.CommandLine;

[AttributeUsage(AttributeTargets.Property)]
public class OptionAttribute : PropertyAttribute, IEquatable<OptionAttribute>
{
    public string? ShortName { get; init; }
    public object? Default { get; init; }
    public object? Min { get; init; }
    public object? Max { get; init; }
    public bool NoShortName { get; init; }
    public bool? IsRequired { get; init; }

    public OptionAttribute(string description, [CallerFilePath] string? callerFilePath = null, [CallerLineNumber] int callerLineNumber = int.MinValue, [CallerMemberName] string? callerMemberName = null) : base(description, new CallerInfo(callerFilePath, callerLineNumber, callerMemberName)) { }

    protected override void Properties_Build(List<object?> list) => list.AddRange(
        ShortName,
        Default,
        Min,
        Max,
        NoShortName,
        IsRequired
    );

    public bool Equals([NotNullWhen(true)] OptionAttribute? other) => base.Equals(other);
}

public class OptionAttributeDetail : PropertyAttributeDetail<OptionAttribute>
{
    private static volatile bool isTrimmedDefault = true;
    public static bool IsTrimmedDefault { get => isTrimmedDefault; set => isTrimmedDefault = value; }
    protected override bool GetIsTrimmedDefault() => IsTrimmedDefault;

    public string? ShortName { get; }
    public string? Default { get; }
    public string? Min { get; }
    public string? Max { get; }
    public bool IsRequired { get; }
    public bool IsHidden { get; }

    public OptionAttributeDetail(TypeSlim parent, PropertyInfo info, OptionAttribute attribute) : base(parent, info, attribute)
    {
        var shortName = Attribute.ShortName.TrimOrNull();
        if (shortName == null && !Attribute.NoShortName)
        {
            shortName = Name.Select((c, i) => i == 0 || char.IsUpper(c) || char.IsNumber(c) ? c.ToString() : string.Empty).ToStringDelimited("");
        }

        ShortName = shortName;

        if (Attribute.Default != null && Attribute.IsRequired.HasValue && Attribute.IsRequired.Value) throw new ArgumentException($"If {Default} has value then {IsRequired} cannot be true", nameof(Default));
        var defaultString = ParseValueToString(Attribute.Default);
        if (defaultString != null) ValidateValue(defaultString);
        Default = defaultString;

        Min = ParseValueToString(Attribute.Min);
        if (Min != null)
        {
            ValidateValue(Min);
            if (!IsComparable) throw new ArgumentException($"Cannot define {nameof(Min)}:{Min} because {this} does not implement {nameof(IComparable)}");
            ValidateValue(Min);
            if (Default != null) ValidateMin(Default, Min);
        }


        Max = ParseValueToString(Attribute.Max);
        if (Max != null)
        {
            ValidateValue(Max);
            if (!IsComparable) throw new ArgumentException($"Cannot define {nameof(Max)}:{Max} because {this} does not implement {nameof(IComparable)}");
            ValidateValue(Max);
            if (Default != null) ValidateMax(Default, Max);
        }


        IsHidden = Attribute.IsHidden;

        IsRequired = Attribute.IsRequired ?? !IsNullable;
    }

    public void SetValue(object instance, string? valueString)
    {
        if (IsTrimmed) valueString = valueString.TrimOrNull();
        if (valueString == null) valueString = Default;
        if (IsTrimmed) valueString = valueString.TrimOrNull();
        if (valueString == null && IsRequired) throw new ArgumentNullException(nameof(valueString), "Property value is required");
        if (Min != null) ValidateMin(valueString, Min);
        if (Max != null) ValidateMax(valueString, Max);
        base.SetValue(instance, valueString);
    }
}

public static class OptionAttributeExtensions
{
    private static readonly StringComparer co = StringComparer.Ordinal;
    private static readonly StringComparer coc = StringComparer.OrdinalIgnoreCase;

    public static OptionAttributeDetail? Named(this IEnumerable<OptionAttributeDetail> details, string name)
    {
        var items = details as ICollection<OptionAttributeDetail> ?? details.ToList();
        return items.FirstOrDefault(o => co.Equals(o.Name, name)) ?? items.FirstOrDefault(o => coc.Equals(o.Name, name));
    }
}
