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

public interface IReflectionParameter :
    IReflectionObject<IReflectionParameter>,
    IReflectionInfo<ParameterInfo>
{
    IReflectionType Type { get; }
    byte Index { get; }

    bool IsRef { get; }
    bool IsOut { get; }
    bool IsIn { get; }
}

public sealed class ReflectionParameter : ReflectionObject<IReflectionParameter>, IReflectionParameter
{
    public ParameterInfo Info { get; }

    public IReflectionType Type => type.Value;
    private readonly Lzy<IReflectionType> type;
    private IReflectionType Type_Build() => GetType(Info.ParameterType);

    public byte Index { get; }

    public bool IsRef { get; }
    public bool IsOut { get; }
    public bool IsIn { get; }

    public ReflectionParameter(IReflectionDomain domain, ParameterInfo info) : base(domain)
    {
        Info = info.CheckNotNull(nameof(info));
        type = CreateLazy(Type_Build);
        Index = (byte)Info.Position;

        // https://stackoverflow.com/a/38110036
        if (Info.ParameterType.IsByRef)
        {
            IsOut = Info.IsOut;
            IsRef = !Info.IsOut;
            IsIn = Info.IsIn;
        }
    }

    #region Overrides

    protected override string Name_Build() => Info.Name;
    protected override Attribute[] Attributes_Build(bool inherited) => Attribute.GetCustomAttributes(Info, inherited);
    protected override string ToString_Build() => $"[{Index}] {Type.Name} {Name}";


    protected override int GetHashCode_Build() => Hash(
        Domain,
        Index,
        Info.Name,
        Type,
        IsRef,
        IsOut,
        IsIn
    );

    protected override bool Equals_Internal(IReflectionParameter other) =>
        IsEqual(Domain, other.Domain) &&
        IsEqual(Index, other.Index) &&
        IsEqual(Info.Name, other.Info.Name) &&
        IsEqual(Type, other.Type) &&
        IsEqual(IsRef, other.IsRef) &&
        IsEqual(IsOut, other.IsOut) &&
        IsEqual(IsIn, other.IsIn);

    protected override int CompareTo_Internal(IReflectionParameter other)
    {
        int c;
        if (0 != (c = Compare(Domain, other.Domain))) return c;
        if (0 != (c = Compare(Index, other.Index))) return c;
        if (0 != (c = Compare(Info.Name, other.Info.Name))) return c;
        if (0 != (c = Compare(Type, other.Type))) return c;
        if (0 != (c = Compare(IsRef, other.IsRef))) return c;
        if (0 != (c = Compare(IsOut, other.IsOut))) return c;
        if (0 != (c = Compare(IsIn, other.IsIn))) return c;
        return 0;
    }

    #endregion Overrides
}
