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

namespace MaxRunSoftware.Utilities.CommandLine;

public abstract class AttributeBase : Attribute, IEquatable<AttributeBase>
{
    public CallerInfo CallerInfo { get; }
    public string Description { get; }
    public string? Name { get; init; }
    public bool IsHidden { get; init; }

    private readonly Lzy<List<object?>> properties;
    protected abstract void Properties_Build(List<object?> list);

    protected AttributeBase(string description, CallerInfo callerInfo)
    {
        CallerInfo = callerInfo;
        Description = description;

        properties = new Lzy<List<object?>>(() =>
        {
            var l = new List<object?>(20) { Description, Name, IsHidden };
            Properties_Build(l);
            return l;
        });

        getHashCode = new Lzy<int>(() => Util.HashEnumerable(properties.Value));
    }

    public sealed override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as AttributeBase);

    public bool Equals([NotNullWhen(true)] AttributeBase? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetHashCode() != other.GetHashCode()) return false;
        return properties.Value.EqualsEnumerable(other.properties.Value);
    }

    public sealed override int GetHashCode() => getHashCode.Value;
    private readonly Lzy<int> getHashCode;
}

public abstract class AttributeDetailBase { }
