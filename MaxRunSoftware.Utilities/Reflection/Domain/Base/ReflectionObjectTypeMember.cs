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

public interface IReflectionObjectTypeMember<T, out TInfo> :
    IReflectionObject<T>,
    IReflectionChild<IReflectionType>,
    IReflectionDeclarationFlags,
    IReflectionInfo<TInfo>,
    IReflectionDeclaringType, IReflectionReflectedType
    where T : class, IReflectionObjectTypeMember<T, TInfo>, IEquatable<T>, IComparable<T>
    where TInfo : MemberInfo { }

public abstract class ReflectionObjectTypeMember<T, TInfo> :
    ReflectionObjectChildInfo<T, IReflectionType, TInfo>,
    IReflectionObjectTypeMember<T, TInfo>
    where T : class, IReflectionObjectTypeMember<T, TInfo>
    where TInfo : MemberInfo
{
    protected ReflectionObjectTypeMember(IReflectionType parent, TInfo info) : base(parent, info) { }


    protected override int GetHashCode_Build() =>
        Hash(
            Domain,
            Parent,
            Info.Name,
            DeclaringType,
            ReflectedType,
            DeclarationFlags
        );

    protected override bool Equals_Internal(T other)
    {
        if (!IsEqual(Domain, other.Domain)) return false;
        if (!IsEqual(Parent, other.Parent)) return false;
        if (!IsEqual(Info.Name, other.Info.Name)) return false;
        if (!IsEqual(DeclaringType, other.DeclaringType)) return false;
        if (!IsEqual(ReflectedType, other.ReflectedType)) return false;
        if (!IsEqual(DeclarationFlags, other.DeclarationFlags)) return false;
        return true;
    }

    protected override int CompareTo_Internal(T other)
    {
        int c;
        if (0 != (c = Compare(Domain, other.Domain))) return c;
        if (0 != (c = Compare(Parent, other.Parent))) return c;
        if (0 != (c = Compare(Info.Name, other.Info.Name))) return c;
        if (0 != (c = Compare(DeclaringType, other.DeclaringType))) return c;
        if (0 != (c = Compare(ReflectedType, other.ReflectedType))) return c;
        if (0 != (c = Compare(DeclarationFlags, other.DeclarationFlags))) return c;
        return c;
    }
}
