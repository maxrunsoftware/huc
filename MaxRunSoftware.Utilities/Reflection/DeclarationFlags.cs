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

[Serializable]
[Flags]
public enum DeclarationFlags
{
    None = 1 << 0,
    Public = 1 << 1,
    Protected = 1 << 2,
    Private = 1 << 3,
    Internal = 1 << 4,
    Static = 1 << 5,
    Instance = 1 << 6,
    Abstract = 1 << 7,
    Virtual = 1 << 8,
    Override = 1 << 9,
    NewShadowSignature = 1 << 10,
    NewShadowName = 1 << 11,
    Explicit = 1 << 12,
    Sealed = 1 << 13,
    Readonly = 1 << 16,
    Inherited = 1 << 17,
    GenericParameter = 1 << 18
}

public static class DeclarationFlagsExtensions
{
    public static bool IsPublic(this DeclarationFlags flags) => (flags & DeclarationFlags.Public) != 0;
    public static bool IsProtected(this DeclarationFlags flags) => (flags & DeclarationFlags.Protected) != 0;
    public static bool IsPrivate(this DeclarationFlags flags) => (flags & DeclarationFlags.Private) != 0;
    public static bool IsInternal(this DeclarationFlags flags) => (flags & DeclarationFlags.Internal) != 0;
    public static bool IsStatic(this DeclarationFlags flags) => (flags & DeclarationFlags.Static) != 0;
    public static bool IsInstance(this DeclarationFlags flags) => (flags & DeclarationFlags.Instance) != 0;
    public static bool IsAbstract(this DeclarationFlags flags) => (flags & DeclarationFlags.Abstract) != 0;
    public static bool IsVirtual(this DeclarationFlags flags) => (flags & DeclarationFlags.Virtual) != 0;
    public static bool IsOverride(this DeclarationFlags flags) => (flags & DeclarationFlags.Override) != 0;
    public static bool IsNewShadowSignature(this DeclarationFlags flags) => (flags & DeclarationFlags.NewShadowSignature) != 0;
    public static bool IsNewShadowName(this DeclarationFlags flags) => (flags & DeclarationFlags.NewShadowName) != 0;
    public static bool IsExplicit(this DeclarationFlags flags) => (flags & DeclarationFlags.Explicit) != 0;
    public static bool IsSealed(this DeclarationFlags flags) => (flags & DeclarationFlags.Sealed) != 0;
    public static bool IsReadonly(this DeclarationFlags flags) => (flags & DeclarationFlags.Readonly) != 0;
    public static bool IsInherited(this DeclarationFlags flags) => (flags & DeclarationFlags.Inherited) != 0;
    public static bool IsGenericParameter(this DeclarationFlags flags) => (flags & DeclarationFlags.GenericParameter) != 0;

    public static bool IsOverridable(this DeclarationFlags flags) => flags.IsVirtual() && flags.IsSealed(); // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.isfinal


    #region GetDeclarationFlags

    public static DeclarationFlags GetDeclarationFlags(this Type type) => type.GetTypeInfo().GetDeclarationFlags();

    public static DeclarationFlags GetDeclarationFlags(this TypeInfo info)
    {
        // https://stackoverflow.com/a/34394502
        var flags = DeclarationFlags.None;

        flags |= info.IsAbstract && info.IsSealed ? DeclarationFlags.Static : DeclarationFlags.Instance; // https://stackoverflow.com/questions/1175888/determine-if-a-type-is-static

        // https://stackoverflow.com/a/16024302
        if (info.IsPublic) flags |= DeclarationFlags.Public;
        if (info.IsNestedPublic) flags |= DeclarationFlags.Public;

        if (!info.IsVisible) flags |= DeclarationFlags.Internal;
        if (info.IsNestedAssembly) flags |= DeclarationFlags.Internal;

        if (info.IsNestedFamily) flags |= DeclarationFlags.Protected;
        if (info.IsNestedPrivate) flags |= DeclarationFlags.Private;
        if (info.IsNestedFamORAssem) flags |= DeclarationFlags.Protected | DeclarationFlags.Internal;
        if (info.IsNestedFamANDAssem) flags |= DeclarationFlags.Private | DeclarationFlags.Protected;

        if (info.IsAbstract) flags |= DeclarationFlags.Abstract;
        if (info.IsSealed) flags |= DeclarationFlags.Sealed;

        if (info.IsGenericParameter) flags |= DeclarationFlags.GenericParameter;

        return flags;
    }


    public static DeclarationFlags GetDeclarationFlags(this ConstructorInfo info)
    {
        var flags = DeclarationFlags.None;

        flags |= info.IsStatic ? DeclarationFlags.Static : DeclarationFlags.Instance;

        // https://stackoverflow.com/a/16024302
        if (info.IsPublic) flags |= DeclarationFlags.Public;
        if (info.IsPrivate) flags |= DeclarationFlags.Private;
        if (info.IsFamily) flags |= DeclarationFlags.Protected;
        if (info.IsAssembly) flags |= DeclarationFlags.Internal;
        if (info.IsFamilyOrAssembly) flags |= DeclarationFlags.Protected | DeclarationFlags.Internal;
        if (info.IsFamilyAndAssembly) flags |= DeclarationFlags.Private | DeclarationFlags.Protected;
        if (info.IsAbstract) flags |= DeclarationFlags.Abstract;
        if (info.IsFinal) flags |= DeclarationFlags.Sealed;
        if (info.IsVirtual) flags |= DeclarationFlags.Virtual;

        var attrs = info.Attributes;
        if ((attrs & MethodAttributes.Virtual) != 0) flags |= DeclarationFlags.Virtual;

        var baseType = info.ReflectedType?.BaseType;

        if (info.DeclaringType != info.ReflectedType) flags |= DeclarationFlags.Override;
        else if ((attrs & MethodAttributes.Virtual) != 0 && (attrs & MethodAttributes.NewSlot) == 0) flags |= DeclarationFlags.Override;
        else if (info.IsHideBySig && baseType != null)
        {
            // https://stackoverflow.com/a/288928
            var flagsSig = info.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flagsSig |= info.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            flagsSig |= BindingFlags.ExactBinding; //https://stackoverflow.com/questions/288357/how-does-reflection-tell-me-when-a-property-is-hiding-an-inherited-member-with-t#comment75338322_288928
            var paramTypes = info.GetParameters().Select(p => p.ParameterType).ToArray();
            var baseMethod = baseType.GetMethod(info.Name, flagsSig, null, paramTypes, null);
            if (baseMethod != null) flags |= DeclarationFlags.NewShadowSignature;

            if (baseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Any(m => StringComparer.Ordinal.Equals(m.Name, info.Name))) flags |= DeclarationFlags.NewShadowName;
        }

        return flags;
    }

    public static DeclarationFlags GetDeclarationFlags(this EventInfo info)
    {
        var flags = DeclarationFlags.None;

        var mis = new List<MethodInfo?>();
        mis.Add(info.GetAddMethod(true));
        mis.Add(info.GetRaiseMethod(true));
        mis.Add(info.GetRemoveMethod(true));
        mis.AddRange(info.GetOtherMethods(true));

        foreach (var mi in mis.WhereNotNull())
        {
            flags |= mi.GetDeclarationFlags();
        }

        return flags;
    }

    public static DeclarationFlags GetDeclarationFlags(this FieldInfo info)
    {
        var flags = DeclarationFlags.None;

        flags |= info.IsStatic ? DeclarationFlags.Static : DeclarationFlags.Instance;

        // https://stackoverflow.com/a/16024302
        if (info.IsPublic) flags |= DeclarationFlags.Public;
        if (info.IsPrivate) flags |= DeclarationFlags.Private;
        if (info.IsFamily) flags |= DeclarationFlags.Protected;
        if (info.IsAssembly) flags |= DeclarationFlags.Internal;
        if (info.IsFamilyOrAssembly) flags |= DeclarationFlags.Protected | DeclarationFlags.Internal;
        if (info.IsFamilyAndAssembly) flags |= DeclarationFlags.Private | DeclarationFlags.Protected;

        if (info.IsInitOnly) flags |= DeclarationFlags.Readonly;

        return flags;
    }

    public static DeclarationFlags GetDeclarationFlags(this MethodInfo info)
    {
        var flags = DeclarationFlags.None;

        flags |= info.IsStatic ? DeclarationFlags.Static : DeclarationFlags.Instance;

        // https://stackoverflow.com/a/16024302
        if (info.IsPublic) flags |= DeclarationFlags.Public;
        if (info.IsPrivate) flags |= DeclarationFlags.Private;
        if (info.IsFamily) flags |= DeclarationFlags.Protected;
        if (info.IsAssembly) flags |= DeclarationFlags.Internal;
        if (info.IsFamilyOrAssembly) flags |= DeclarationFlags.Protected | DeclarationFlags.Internal;
        if (info.IsFamilyAndAssembly) flags |= DeclarationFlags.Private | DeclarationFlags.Protected;
        if (info.IsAbstract) flags |= DeclarationFlags.Abstract;
        if (info.IsFinal) flags |= DeclarationFlags.Sealed;
        if (info.IsVirtual) flags |= DeclarationFlags.Virtual;

        if (IsExplicitInterfaceImplementation(info)) flags |= DeclarationFlags.Explicit; // https://stackoverflow.com/a/17854048

        var attrs = info.Attributes;
        if ((attrs & MethodAttributes.Virtual) != 0) flags |= DeclarationFlags.Virtual;

        var baseType = info.ReflectedType?.BaseType;

        if (info.DeclaringType != info.ReflectedType) flags |= DeclarationFlags.Override;
        else if ((attrs & MethodAttributes.Virtual) != 0 && (attrs & MethodAttributes.NewSlot) == 0) flags |= DeclarationFlags.Override;
        else if (info.IsHideBySig && baseType != null)
        {
            // https://stackoverflow.com/a/288928
            var flagsSig = info.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flagsSig |= info.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            flagsSig |= BindingFlags.ExactBinding; //https://stackoverflow.com/questions/288357/how-does-reflection-tell-me-when-a-property-is-hiding-an-inherited-member-with-t#comment75338322_288928
            var paramTypes = info.GetParameters().Select(p => p.ParameterType).ToArray();
            var baseMethod = baseType.GetMethod(info.Name, flagsSig, null, paramTypes, null);
            if (baseMethod != null) flags |= DeclarationFlags.NewShadowSignature;

            if (baseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Any(m => StringComparer.Ordinal.Equals(m.Name, info.Name))) flags |= DeclarationFlags.NewShadowName;
        }

        return flags;
    }

    /// <summary>
    /// https://stackoverflow.com/a/17854048
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    private static bool IsExplicitInterfaceImplementation(MethodInfo method)
    {
        // https://stackoverflow.com/a/52743438
        // InterfaceMapping interfaceMap = reflectedType.GetInterfaceMap(interfaceMethod.DeclaringType);
        // for (var i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
        // {
        //     if (interfaceMap.InterfaceMethods[i].Equals(interfaceMethod)) return interfaceMap.TargetMethods[i];
        // }

        // Check all interfaces implemented in the type that declares
        // the method we want to check, with this we'll exclude all methods
        // that don't implement an interface method
        var declaringType = method.DeclaringType;
        if (declaringType == null) return false;

        foreach (var implementedInterface in declaringType.GetInterfaces())
        {
            InterfaceMapping mapping;
            try { mapping = declaringType.GetInterfaceMap(implementedInterface); }
            catch
            {
                // System.ArgumentException: Interface maps for generic interfaces on arrays cannot be retrieved.
                //     at System.RuntimeType.GetInterfaceMap(Type ifaceType)
                return false;
            }


            // If interface isn't implemented in the type that owns this method then we can ignore it because it definitely isn't an explicit implementation
            if (mapping.TargetType != declaringType) continue;

            // Is this method the implementation of this interface?
            var methodIndex = Array.IndexOf(mapping.TargetMethods, method);
            if (methodIndex == -1) continue;

            // Is it true for any language? Can we just skip this check?
            if (!method.IsFinal || !method.IsVirtual) return false;

            // It's not required in all languages to implement every method in the interface (if the type is abstract)
            string? methodName = null;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (mapping.InterfaceMethods[methodIndex] != null) methodName = mapping.InterfaceMethods[methodIndex].Name;

            // If names don't match then it's explicit
            if (!method.Name.Equals(methodName, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    public static DeclarationFlags GetDeclarationFlags(this PropertyInfo info)
    {
        var flags = DeclarationFlags.None;

        var methodGet = info.GetGetMethod(true);
        if (methodGet != null)
        {
            flags |= methodGet.GetDeclarationFlags();
        }

        var methodSet = info.GetSetMethod(true);
        if (methodSet != null)
        {
            flags |= methodSet.GetDeclarationFlags();
        }

        if (methodGet != null && methodSet == null) flags |= DeclarationFlags.Readonly;

        return flags;
    }

    #endregion GetDeclarationFlags
}
