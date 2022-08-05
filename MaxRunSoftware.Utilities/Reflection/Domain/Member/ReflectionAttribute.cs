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

public interface IReflectionAttribute : IReflectionObject<IReflectionAttribute>
{
    Attribute Attribute { get; }
    IReflectionType Type { get; }
}

public class ReflectionAttribute : ReflectionObject<IReflectionAttribute>, IReflectionAttribute
{
    public Attribute Attribute { get; }

    public IReflectionType Type => type.Value;
    private readonly Lzy<IReflectionType> type;
    private IReflectionType Type_Build() => GetType(Attribute.GetType());

    public ReflectionAttribute(IReflectionDomain domain, Attribute attribute) : base(domain)
    {
        Attribute = attribute.CheckNotNull(nameof(attribute));
        type = CreateLazy(Type_Build);
    }

    #region Overrides

    protected override string Name_Build() => Attribute.GetType().NameFormatted();
    protected override Attribute[] Attributes_Build(bool inherited) => Array.Empty<Attribute>();
    protected override string ToString_Build() => Attribute.ToString();

    protected override int GetHashCode_Build() => Hash(
        Domain,
        Name,
        Attribute
    );

    protected override bool Equals_Internal(IReflectionAttribute other) =>
        IsEqual(Domain, other.Domain) &&
        IsEqual(Name, other.Name) &&
        IsEqual(Attribute, other.Attribute);

    protected override int CompareTo_Internal(IReflectionAttribute other)
    {
        int c;
        if (0 != (c = Compare(Domain, other.Domain))) return c;
        if (0 != (c = Compare(Name, other.Name))) return c;
        if (0 != (c = Compare(GetHashCode(), other.GetHashCode()))) return c;
        return c;
    }

    #endregion Overrides

    private static bool IsEqual(Attribute x, Attribute y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        return x.Equals(y);
    }
}
