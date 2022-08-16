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

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : AttributeBase, IEquatable<CommandAttribute>
{
    public CommandAttribute(string description, [CallerFilePath] string? callerFilePath = null, [CallerLineNumber] int callerLineNumber = int.MinValue, [CallerMemberName] string? callerMemberName = null) : base(description, new CallerInfo(callerFilePath, callerLineNumber, callerMemberName)) { }

    public bool Equals([NotNullWhen(true)] CommandAttribute? other) => base.Equals(other);
    protected override void Properties_Build(List<object?> list) => list.AddRange(Description, Name, IsHidden);
}

public class CommandAttributeDetail
{
    public TypeSlim Type { get; }
    public CommandAttribute Attribute { get; }

    public string Name { get; }
    public string Description { get; }
    public bool IsHidden { get; }

    public CommandAttributeDetail(TypeSlim type, CommandAttribute attribute)
    {
        Type = type;
        Attribute = attribute;

        var name = Attribute.Name.TrimOrNull() ?? Type.Type.Name.TrimOrNull();
        Name = name.CheckNotNull(nameof(Name));

        var description = Attribute.Description.TrimOrNull();
        Description = description.CheckNotNull(nameof(Description));

        IsHidden = Attribute.IsHidden;
    }
}

public static class CommandAttributeExtensions { }
