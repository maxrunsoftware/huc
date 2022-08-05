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

namespace MaxRunSoftware.Utilities;

public sealed class TypeSlim : IEquatable<TypeSlim>, IComparable<TypeSlim>, IComparable
{
    public string NameFull { get; }
    public Type Type { get; }
    public AssemblySlim Assembly { get; }
    public int TypeHashCode { get; }
    private readonly int getHashCode;

    public TypeSlim(Type type)
    {
        Type = type.CheckNotNull(nameof(type));
        Assembly = new AssemblySlim(type.Assembly);
        NameFull = NameFull_Build(type);
        TypeHashCode = Type.GetHashCode();
        getHashCode = Util.GenerateHashCode(TypeHashCode, Assembly.GetHashCode(), StringComparer.Ordinal.GetHashCode(NameFull));
    }

    #region Override

    public override int GetHashCode() => getHashCode;

    public override bool Equals(object? obj) => Equals(obj as TypeSlim);
    public bool Equals(TypeSlim? other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;

        if (getHashCode != other.getHashCode) return false;
        if (TypeHashCode != other.TypeHashCode) return false;
        if (!Assembly.Equals(other.Assembly)) return false;
        if (!StringComparer.Ordinal.Equals(NameFull, other.NameFull)) return false;
        if (Type != other.Type) return false;

        return true;
    }

    public int CompareTo(object? obj) => CompareTo(obj as TypeSlim);
    public int CompareTo(TypeSlim? other)
    {
        if (ReferenceEquals(other, null)) return 1;
        if (ReferenceEquals(this, other)) return 0;

        int c;
        if (0 != (c = Assembly.CompareTo(other.Assembly))) return c;
        if (0 != (c = StringComparerOrdinalThenOrdinalIgnoreCase.INSTANCE.Compare(NameFull, other.NameFull))) return c;
        if (0 != (c = TypeHashCode.CompareTo(other.TypeHashCode))) return c;
        if (0 != (c = getHashCode.CompareTo(other.getHashCode))) return c;
        return c;
    }

    public override string ToString() => NameFull;

    #endregion Override

    #region Static

    public static string NameFull_Build(Type type)
    {
        var name = type.FullName;
        if (!string.IsNullOrWhiteSpace(name))
        {
            name = type.FullNameFormatted().TrimOrNull();
            if (name != null) return name;
        }

        name = type.NameFormatted().TrimOrNull();
        if (name != null) return name;

        name = type.Name.TrimOrNull();
        if (name != null) return name;

        name = type.ToString().TrimOrNull();
        if (name != null) return name;

        return type.Name;
    }

    #endregion Static
}
