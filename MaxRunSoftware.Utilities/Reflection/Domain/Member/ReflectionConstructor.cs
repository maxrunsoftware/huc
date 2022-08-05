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

public interface IReflectionConstructor :
    IReflectionObjectTypeMember<IReflectionConstructor, ConstructorInfo>,
    IReflectionGeneric,
    IReflectionParameterized { }

public sealed class ReflectionConstructor : ReflectionObjectTypeMember<IReflectionConstructor, ConstructorInfo>, IReflectionConstructor
{
    public ReflectionConstructor(IReflectionType parent, ConstructorInfo info) : base(parent, info)
    {
        genericParameters = CreateLazy(GenericParameters_Build);
        parameters = CreateLazy(Parameters_Build);
    }

    private readonly Lzy<IReflectionCollection<IReflectionType>> genericParameters;
    public IReflectionCollection<IReflectionType> GenericParameters => genericParameters.Value;
    private IReflectionCollection<IReflectionType> GenericParameters_Build() => GetGenericParameters(Info);

    private readonly Lzy<IReflectionCollection<IReflectionParameter>> parameters;
    public IReflectionCollection<IReflectionParameter> Parameters => parameters.Value;
    private IReflectionCollection<IReflectionParameter> Parameters_Build() => GetParameters(Info);

    #region Overrides

    protected override string ToString_Build() => Info.Name + GetParametersString();

    protected override int GetHashCode_Build() => Hash(
        base.GetHashCode_Build(),
        GenericParameters,
        Parameters
    );

    protected override bool Equals_Internal(IReflectionConstructor other) =>
        base.Equals_Internal(other) &&
        IsEqual(GenericParameters, other.GenericParameters) &&
        IsEqual(Parameters, other.Parameters);

    protected override int CompareTo_Internal(IReflectionConstructor other)
    {
        int c;
        if (0 != (c = base.CompareTo_Internal(other))) return c;
        if (0 != (c = Compare(GenericParameters, other.GenericParameters))) return c;
        if (0 != (c = Compare(Parameters, other.Parameters))) return c;
        return c;
    }

    #endregion Overrides
}

public static class ReflectionConstructorExtensions { }
