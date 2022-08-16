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
public class ArgumentAttribute : PropertyAttribute, IEquatable<ArgumentAttribute>
{
    private static volatile bool isTrimmedDefault = true;
    public static bool IsTrimmedDefault { get => isTrimmedDefault; set => isTrimmedDefault = value; }

    public ushort Index { get; }
    public int? MinCount { get; init; }

    public ArgumentAttribute(ushort index, string description, [CallerFilePath] string? callerFilePath = null, [CallerLineNumber] int callerLineNumber = int.MinValue, [CallerMemberName] string? callerMemberName = null) : base(description, new CallerInfo(callerFilePath, callerLineNumber, callerMemberName))
    {
        Index = index;
    }

    public bool Equals([NotNullWhen(true)] ArgumentAttribute? other) => base.Equals(other);

    protected override void Properties_Build(List<object?> list) => list.AddRange(Index, MinCount);
}

public class ArgumentAttributeDetail : PropertyAttributeDetail<ArgumentAttribute>
{
    private static volatile bool isTrimmedDefault = true;
    public static bool IsTrimmedDefault { get => isTrimmedDefault; set => isTrimmedDefault = value; }
    protected override bool GetIsTrimmedDefault() => IsTrimmedDefault;

    public ushort Index { get; }
    public int MinCount { get; }

    public ArgumentAttributeDetail(TypeSlim parent, PropertyInfo info, ArgumentAttribute attribute) : base(parent, info, attribute)
    {
        Index = Attribute.Index;
        MinCount = Attribute.MinCount ?? (IsNullable ? 0 : 1);
    }
}

public static class ArgumentAttributeExtensions { }
