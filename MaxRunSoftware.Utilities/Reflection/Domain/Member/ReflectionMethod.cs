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

public interface IReflectionMethod :
    IReflectionBase<IReflectionMethod>,
    IReflectionGeneric,
    IReflectionObjectTypeMember<IReflectionMethod, MethodInfo>,
    IReflectionParameterized
{
    IReflectionType ReturnType { get; }
    object? Invoke(object? instance, params object[] parameters);
}

[Serializable]
[Flags]
public enum ReflectionMethodEqualsFlags
{
    None = 1 << 0,

    MethodName = 1 << 1,
    MethodReturnType = 1 << 2,
    Method = MethodName | MethodReturnType,

    ParametersCount = 1 << 3,
    ParametersName = 1 << 4,
    ParametersIndex = 1 << 5,
    ParametersType = 1 << 6,
    ParametersModifiers = 1 << 7,
    Parameters = ParametersCount | ParametersName | ParametersIndex | ParametersType | ParametersModifiers,

    GenericParametersCount = 1 << 8,
    GenericParametersType = 1 << 9,
    GenericParameters = GenericParametersCount | GenericParametersType,

    All = Method | Parameters | GenericParameters
}

public static class ReflectionMethodMatchFlagsExtensions
{
    public static bool IsMethodName(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.MethodName) != 0;
    public static bool IsMethodReturnType(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.MethodReturnType) != 0;

    public static bool IsParametersCount(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.ParametersCount) != 0;

    public static bool IsParametersName(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.ParametersName) != 0;
    public static ReflectionMethodEqualsFlags MinusParametersName(this ReflectionMethodEqualsFlags flags) => flags & ~ReflectionMethodEqualsFlags.ParametersName;

    public static bool IsParametersIndex(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.ParametersIndex) != 0;
    public static bool IsParametersType(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.ParametersType) != 0;
    public static bool IsParametersModifiers(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.ParametersModifiers) != 0;

    public static bool IsGenericParametersCount(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.GenericParametersCount) != 0;
    public static bool IsGenericParametersType(this ReflectionMethodEqualsFlags flags) => (flags & ReflectionMethodEqualsFlags.GenericParametersType) != 0;
}

public sealed class ReflectionMethod : ReflectionObjectTypeMember<IReflectionMethod, MethodInfo>, IReflectionMethod
{
    public ReflectionMethod(IReflectionType parent, MethodInfo info) : base(parent, info)
    {
        returnType = CreateLazy(ReturnType_Build);
        baseMethod = CreateLazy(Base_Build);
        bases = CreateLazy(Bases_Build);
        genericParameters = CreateLazy(GenericParameters_Build);
        parameters = CreateLazy(Parameters_Build);
    }

    private readonly Lzy<IReflectionType> returnType;
    public IReflectionType ReturnType => returnType.Value;
    private IReflectionType ReturnType_Build() => GetType(Info.ReturnType);

    private readonly Lzy<IReflectionCollection<IReflectionType>> genericParameters;
    public IReflectionCollection<IReflectionType> GenericParameters => genericParameters.Value;
    private IReflectionCollection<IReflectionType> GenericParameters_Build() => GetGenericParameters(Info);

    private readonly Lzy<IReflectionCollection<IReflectionParameter>> parameters;
    public IReflectionCollection<IReflectionParameter> Parameters => parameters.Value;
    private IReflectionCollection<IReflectionParameter> Parameters_Build() => GetParameters(Info);

    private readonly Lzy<IReflectionMethod?> baseMethod;
    public IReflectionMethod? Base => baseMethod.Value;
    private IReflectionMethod? Base_Build()
    {
        // var infoExpected = Info.GetBaseDefinition();

        var baseType = Parent.Base;
        if (baseType == null) return null;

        if (this.IsNewShadowName() || this.IsNewShadowSignature()) return null;
        if (!this.IsOverride()) return null;

        var flags = ReflectionMethodEqualsFlags.All.MinusParametersName();
        var list = new List<IReflectionMethod>();
        foreach (var baseReflectionMethod in baseType.Methods)
        {
            if (!this.Equals(baseReflectionMethod, flags)) continue;
            list.Add(baseReflectionMethod);
        }

        if (list.Count == 0) return null;
        if (list.Count > 1) throw new Exception($"Found multiple matching methods for {nameof(Base)}: " + list.OrderBy(o => o).Select(o => o.ToString()).ToStringDelimited(" | "));
        return list[0];
    }

    private readonly Lzy<IReflectionCollection<IReflectionMethod>> bases;
    public IReflectionCollection<IReflectionMethod> Bases => bases.Value;
    private IReflectionCollection<IReflectionMethod> Bases_Build() => this.Bases_Build(Domain);


    #region Overrides

    protected override string ToString_Build() => ReturnType.Name + " " + Parent.NameFull + "." + Info.Name + GetParametersString();

    protected override int GetHashCode_Build() => Hash(
        base.GetHashCode_Build(),
        GenericParameters,
        Parameters,
        ReturnType
    );

    protected override bool Equals_Internal(IReflectionMethod other) =>
        base.Equals_Internal(other) &&
        IsEqual(GenericParameters, other.GenericParameters) &&
        IsEqual(Parameters, other.Parameters) &&
        IsEqual(ReturnType, other.ReturnType);

    protected override int CompareTo_Internal(IReflectionMethod other)
    {
        int c;
        if (0 != (c = base.CompareTo_Internal(other))) return c;
        if (0 != (c = Compare(GenericParameters, other.GenericParameters))) return c;
        if (0 != (c = Compare(Parameters, other.Parameters))) return c;
        if (0 != (c = Compare(ReturnType, other.ReturnType))) return c;
        return c;
    }

    #endregion Overrides

    #region Invoke

    public object? Invoke(object? instance, params object[] parameterValues)
    {
        parameterValues.CheckNotNull(nameof(parameterValues));

        if (parameterValues.Length != Parameters.Count) throw new Exception($"{this} expects {Parameters.Count} parameters but {parameterValues.Length} parameters were supplied");

        var result = Info.Invoke(instance, parameterValues);

        return result;
    }

    #endregion Invoke
}

public static class ReflectionMethodExtensions
{
    public static bool IsPropertyMethod(this IReflectionMethod obj) => IsPropertyMethod(obj, true, true);
    public static bool IsPropertyMethodGet(this IReflectionMethod obj) => IsPropertyMethod(obj, true, false);
    public static bool IsPropertyMethodSet(this IReflectionMethod obj) => IsPropertyMethod(obj, false, true);

    private static bool IsPropertyMethod(this IReflectionMethod obj, bool isCheckGet, bool isCheckSet)
    {
        (isCheckGet || isCheckSet).AssertTrue();

        // https://stackoverflow.com/a/73908
        if (!obj.Info.IsSpecialName) return false;
        if ((obj.Info.Attributes & MethodAttributes.HideBySig) == 0) return false;


        var result = true;
        if (isCheckGet && isCheckSet)
        {
            if (!obj.Info.Name.StartsWithAny("get_", "set_")) result = false;
        }
        else if (isCheckGet)
        {
            if (!obj.Info.Name.StartsWith("get_")) result = false;
        }
        else if (isCheckSet)
        {
            if (!obj.Info.Name.StartsWith("set_")) result = false;
        }

        if (!result) return false;

        // https://stackoverflow.com/a/40128143
        var reflectedType = obj.ReflectedType;
        if (reflectedType != null)
        {
            var methodHashCode = obj.Info.GetHashCode();
            foreach (var p in reflectedType.Properties)
            {
                if (isCheckGet)
                {
                    var m = p.Info.GetGetMethod(true);
                    if (m != null)
                    {
                        if (m.GetHashCode() == methodHashCode) return true;
                    }
                }

                if (isCheckSet)
                {
                    var m = p.Info.GetSetMethod(true);
                    if (m != null)
                    {
                        if (m.GetHashCode() == methodHashCode) return true;
                    }
                }
            }

            return false;
        }

        return true;
    }


    public static bool Equals(this IReflectionMethod obj, IReflectionMethod other, ReflectionMethodEqualsFlags flags)
    {
        if (flags.IsMethodName() && !StringComparer.Ordinal.Equals(obj.Info.Name, other.Info.Name)) return false; // Use Info.Name so that it doesn't include generic info
        if (flags.IsMethodReturnType() && !Reflection.InternalIsEqual(obj.ReturnType, other.ReturnType)) return false;

        // TODO: Better way to do this 'if'
        if (flags.IsParametersCount() || flags.IsParametersName() || flags.IsParametersIndex() || flags.IsParametersType() || flags.IsParametersModifiers())
        {
            int count;
            if (flags.IsParametersCount())
            {
                if (obj.Parameters.Count != other.Parameters.Count) return false;
                count = obj.Parameters.Count;
            }
            else count = Math.Min(obj.Parameters.Count, other.Parameters.Count);

            for (var i = 0; i < count; i++)
            {
                var x = obj.Parameters[i];
                var y = other.Parameters[i];

                if (flags.IsParametersName() && !Reflection.InternalIsEqual(x.Name, y.Name)) return false;
                if (flags.IsParametersIndex() && x.Index != y.Index) return false;
                if (flags.IsParametersType() && !Reflection.InternalIsEqual(x.Type, y.Type)) return false;
                if (flags.IsParametersModifiers() && (x.IsIn != y.IsIn || x.IsOut != y.IsOut || x.IsRef != y.IsRef)) return false;
            }
        }

        // TODO: Better way to do this 'if'
        if (flags.IsGenericParametersCount() || flags.IsGenericParametersType())
        {
            int count;
            if (flags.IsParametersCount())
            {
                if (obj.GenericParameters.Count != other.GenericParameters.Count) return false;
                count = obj.GenericParameters.Count;
            }
            else count = Math.Min(obj.GenericParameters.Count, other.GenericParameters.Count);

            for (var i = 0; i < count; i++)
            {
                var x = obj.GenericParameters[i];
                var y = other.GenericParameters[i];

                if (flags.IsGenericParametersType() && !Reflection.InternalIsEqual(x, y)) return false;
            }
        }

        return true;
    }


    #region IReflectionCollection

    public static IReflectionCollection<IReflectionMethod> ReturnType<T>(this IReflectionCollection<IReflectionMethod> obj, bool isExactType = true) => obj.ReturnType(o => o.ReturnType, isExactType, typeof(T));
    public static IReflectionCollection<IReflectionMethod> ReturnType(this IReflectionCollection<IReflectionMethod> obj, Type type, bool isExactType = true) => obj.ReturnType(o => o.ReturnType, isExactType, type);
    public static IReflectionCollection<IReflectionMethod> ReturnTypeVoid(this IReflectionCollection<IReflectionMethod> obj) => obj.ReturnType(o => o.ReturnType, true, typeof(void));

    public static IReflectionCollection<IReflectionMethod> NotMethodsFrom<T>(this IReflectionCollection<IReflectionMethod> obj) => obj.NotMethodsFrom(typeof(T));

    public static IReflectionCollection<IReflectionMethod> NotMethodsFrom(this IReflectionCollection<IReflectionMethod> obj, Type typeWithMethods)
    {
        obj.CheckNotNull(nameof(obj));
        typeWithMethods.CheckNotNull(nameof(typeWithMethods));
        if (obj.Count == 0) return ReflectionCollection.Empty<IReflectionMethod>(obj.Domain);

        var typeWithMethodsReflection = obj.Domain.GetType(typeWithMethods);
        var l = new List<IReflectionMethod>();
        var flags = ReflectionMethodEqualsFlags.All.MinusParametersName();


        foreach (var method in obj)
        {
            if (typeWithMethodsReflection.Methods.Any(otherMethod => method.Equals(otherMethod, flags))) continue;
            l.Add(method);
        }

        return l.ToReflectionCollection(obj.Domain);
    }

    public static IReflectionCollection<IReflectionMethod> NotProperties(this IReflectionCollection<IReflectionMethod> obj) => obj.Where(o => !o.IsPropertyMethod());

    #region Parameters

    public static IReflectionCollection<IReflectionMethod> Parameters<T1>(this IReflectionCollection<IReflectionMethod> obj, bool isExactMatch = true) => obj.Parameters(isExactMatch, typeof(T1));
    public static IReflectionCollection<IReflectionMethod> Parameters<T1, T2>(this IReflectionCollection<IReflectionMethod> obj, bool isExactMatch = true) => obj.Parameters(isExactMatch, typeof(T1), typeof(T2));
    public static IReflectionCollection<IReflectionMethod> Parameters<T1, T2, T3>(this IReflectionCollection<IReflectionMethod> obj, bool isExactMatch = true) => obj.Parameters(isExactMatch, typeof(T1), typeof(T2), typeof(T3));
    public static IReflectionCollection<IReflectionMethod> Parameters<T1, T2, T3, T4>(this IReflectionCollection<IReflectionMethod> obj, bool isExactMatch = true) => obj.Parameters(isExactMatch, typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    #endregion Parameters

    #endregion IReflectionCollection
}
