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

public interface ICommandProperty
{
    string Name { get; }
    bool IsWritable { get; }
    TypeSlim Parent { get; }
    bool IsNullable { get; }
    bool IsComparable { get; }
    void SetValue(object instance, object? value);
    int CompareValues(object? valueX, object? valueY);
    void ValidateValue(object? value);
}

public class CommandProperty : ICommandProperty
{
    public CommandProperty(PropertyInfo info, TypeSlim? parent = null)
    {
        Info = info;
        Parent = parent ?? new TypeSlim((info.ReflectedType ?? info.DeclaringType).CheckNotNull());
    }

    public PropertyInfo Info { get; }
    public TypeSlim Parent { get; }

    public string Name => Info.Name;

    public void SetValue(object instance, object? value)
    {
        if (!IsWritable) throw new Exception("Cannot write property " + this);
        var valueTyped = Convert(value);

        // TODO: https://stackoverflow.com/a/18937268

        var setMethod = Info.GetSetMethod(true);
        setMethod!.Invoke(instance, new[] { valueTyped });
    }

    public int CompareValues(object? valueX, object? valueY)
    {
        if (compareToMethod == null || !IsComparable) throw new ArgumentException($"Type {Info.PropertyType.Name} does not implement interface " + typeof(IComparable<>).MakeGenericType(Info.PropertyType).NameFormatted() + " or interface " + typeof(IComparable).NameFormatted());
        var valueXTyped = Convert(valueX);
        var valueYTyped = Convert(valueY);
        if (valueXTyped == valueYTyped) return 0;
        if (valueXTyped == null) return -1;
        if (valueYTyped == null) return 1;

        return (int)compareToMethod.Invoke(valueXTyped, new[] { valueYTyped })!;
    }
    public void ValidateValue(object? value)
    {
        var _ = Convert(value);
    }

    public object? Convert(string? obj) => Util.ChangeType(obj, Info.PropertyType);
    public object? Convert(object? obj) => Convert(obj?.ToString());
}
