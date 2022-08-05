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

public abstract class PropertyAttribute : Attribute
{
    public string Description { get; }
    public string? Name { get; set; }
    public CallerInfo CallerInfo { get; }
    public bool? IsTrimmed { get; set; }

    protected PropertyAttribute(string description, string? filePath, int lineNumber, string? memberName)
    {
        Description = description;
        CallerInfo = new CallerInfo(filePath, lineNumber == int.MinValue ? null : lineNumber, memberName);
    }
}

public abstract class PropertyDetail<TAttribute> where TAttribute : PropertyAttribute
{
    public static bool IsTrimmedDefault => true;

    public TypeSlim Type { get; }
    public PropertyInfo Info { get; }
    public TAttribute Attribute { get; }
    public bool IsTrimmed { get; }

    public string Name { get; }
    public string Description { get; }

    protected PropertyDetail(TypeSlim type, PropertyInfo info, TAttribute attribute)
    {
        Type = type;
        Info = info;
        Attribute = attribute;

        var name = Attribute.Name.TrimOrNull() ?? Info.Name.TrimOrNull();
        Name = name.CheckNotNull(nameof(Name));

        var description = Attribute.Description.TrimOrNull();
        Description = description.CheckNotNull(nameof(Description));

        IsTrimmed = Attribute.IsTrimmed ?? IsTrimmedDefault;
    }

    public object? ConvertValue(string value) => Util.ChangeType(value, Info.PropertyType);

    public virtual void SetValue(object instance, string? valueString)
    {
        object? converted = null;
        if (valueString != null) converted = ConvertValue(valueString);
        Info.SetValue(instance, converted);
    }
}

public abstract class PropertyDetailWrapped<TAttribute, TDetail>
    where TAttribute : PropertyAttribute
    where TDetail : PropertyDetail<TAttribute>
{
    public TypeSlim Type { get; }
    public PropertyInfo Info { get; }
    public TAttribute Attribute { get; }
    public TDetail? Detail { get; }
    public Exception? Exception { get; }

    protected PropertyDetailWrapped(TypeSlim type, PropertyInfo info, TAttribute attribute, TDetail? detail, Exception? exception)
    {
        Type = type;
        Info = info;
        Attribute = attribute;
        Detail = detail;
        Exception = exception;
    }
}
