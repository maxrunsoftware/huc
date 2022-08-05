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

public abstract class ReflectionObjectChildInfo<T, TParent, TInfo> :
    ReflectionObjectChild<T, TParent>
    where T : class, IReflectionObject<T>
    where TInfo : MemberInfo
    where TParent : class, IReflectionObject<TParent>
{
    protected ReflectionObjectChildInfo(TParent parent, TInfo info) : base(parent)
    {
        Info = info.CheckNotNull(nameof(info));
        declarationFlags = CreateLazy(DeclarationFlags_Build);
        declaringType = CreateLazy(DeclaringType_Build);
        reflectedType = CreateLazy(ReflectedType_Build);
    }


    public TInfo Info { get; }


    protected override string Name_Build() => Info.Name + GetGenericParametersString();
    protected sealed override Attribute[] Attributes_Build(bool inherited) => Attribute.GetCustomAttributes(Info, inherited);


    private readonly Lzy<DeclarationFlags> declarationFlags;
    public DeclarationFlags DeclarationFlags => declarationFlags.Value;
    private DeclarationFlags DeclarationFlags_Build() => Info switch
    {
        ConstructorInfo c => c.GetDeclarationFlags(),
        EventInfo e => e.GetDeclarationFlags(),
        FieldInfo f => f.GetDeclarationFlags(),
        MethodInfo m => m.GetDeclarationFlags(),
        PropertyInfo p => p.GetDeclarationFlags(),
        TypeInfo t => t.GetDeclarationFlags(),
        _ => throw new NotImplementedException()
    };


    private readonly Lzy<IReflectionType?> declaringType;
    public IReflectionType? DeclaringType => declaringType.Value;
    private IReflectionType? DeclaringType_Build() => Info.DeclaringType == null ? null : GetType(Info.DeclaringType);


    private readonly Lzy<IReflectionType?> reflectedType;
    public IReflectionType? ReflectedType => reflectedType.Value;
    private IReflectionType? ReflectedType_Build() => Info.ReflectedType == null ? null : GetType(Info.ReflectedType);


    protected string GetGenericParametersString()
    {
        if (this is not IReflectionGeneric generic) return string.Empty;
        if (generic.GenericParameters.Count == 0) return string.Empty;
        return "<" + generic.GenericParameters.Select(o => o.Name).ToStringDelimited(",") + ">";
    }

    protected string GetParametersString()
    {
        if (this is not IReflectionParameterized parameterized) return string.Empty;
        return "(" + parameterized.Parameters.Select(o => $"{o.Type.Name} {o.Name}").ToStringDelimited(", ") + ")";
    }
}
