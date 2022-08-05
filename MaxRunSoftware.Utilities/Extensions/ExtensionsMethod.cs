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

using System.Runtime.CompilerServices;

namespace MaxRunSoftware.Utilities;

public enum MethodDeclarationType
{
    None,
    Override,
    ShadowSignature,
    ShadowName,
    Virtual
}

public static class MethodDeclarationTypeExtensions
{
    public static MethodDeclarationType GetDeclarationType(this MethodBase info)
    {
        // https://stackoverflow.com/a/288928
        if (info == null) return MethodDeclarationType.None;
        if (info.DeclaringType != info.ReflectedType)
        {
            //if (info.IsHideBySig) return MethodDeclarationType.Override;
            return MethodDeclarationType.None;
        }

        var attrs = info.Attributes;

        if ((attrs & MethodAttributes.Virtual) != 0 && (attrs & MethodAttributes.NewSlot) == 0) return MethodDeclarationType.Override;

        var baseType = info.DeclaringType?.BaseType;
        if (baseType == null) return MethodDeclarationType.None;

        if (info.IsHideBySig)
        {
            var flagsSig = info.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flagsSig |= info.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            flagsSig |= BindingFlags.ExactBinding; //https://stackoverflow.com/questions/288357/how-does-reflection-tell-me-when-a-property-is-hiding-an-inherited-member-with-t#comment75338322_288928
            var paramTypes = info.GetParameters().Select(p => p.ParameterType).ToArray();
            var baseMethod = baseType.GetMethod(info.Name, flagsSig, null, paramTypes, null);
            if (baseMethod != null) return MethodDeclarationType.ShadowSignature;

            // return MethodDeclarationType.None;
        }


        var flagsName = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        if (baseType.GetMethods(flagsName).Any(m => m.Name == info.Name)) return MethodDeclarationType.ShadowName;

        if ((attrs & MethodAttributes.Virtual) != 0) return MethodDeclarationType.Virtual;

        return MethodDeclarationType.None;
    }
}

/// <summary>
/// Formatting of methods for debugging
/// https://github.com/kellyelton/System.Reflection.ExtensionMethods/tree/master/System.Reflection.ExtensionMethods
/// </summary>
public static class ExtensionsMethod
{
    public static string GetSignature(this MethodBase method, bool invokable)
    {
        var sb = new StringBuilder();

        // Add our method accessors if it's not invokable
        if (!invokable)
        {
            sb.Append(GetMethodAccessorSignature(method));
            sb.Append(' ');
        }

        // Add method name
        sb.Append(method.Name);

        // Add method generics
        if (method.IsGenericMethod) sb.Append(BuildGenericSignature(method.GetGenericArguments()));

        // Add method parameters
        sb.Append(GetMethodArgumentsSignature(method, invokable));

        return sb.ToString();
    }

    private static string GetMethodAccessorSignature(this MethodBase method)
    {
        var signature = string.Empty;

        if (method.IsAssembly)
        {
            signature = "internal ";
            if (method.IsFamily) signature += "protected ";
        }
        else if (method.IsPublic) signature = "public ";
        else if (method.IsPrivate) signature = "private ";
        else if (method.IsFamily) signature = "protected ";

        if (method.IsStatic) signature += "static ";

        if (method is MethodInfo methodInfo) signature += GetSignature(methodInfo.ReturnType);

        return signature;
    }

    private static string GetMethodArgumentsSignature(this MethodBase method, bool invokable)
    {
        var isExtensionMethod = method.IsDefined(typeof(ExtensionAttribute), false);
        var methodParameters = method.GetParameters().AsEnumerable();

        // If this signature is designed to be invoked and it's an extension method
        // Skip the first argument
        if (isExtensionMethod && invokable) methodParameters = methodParameters.Skip(1);

        var methodParameterSignatures = methodParameters.Select(param =>
        {
            var signature = string.Empty;
            if (param.ParameterType.IsByRef) signature = "ref ";
            else if (param.IsOut) signature = "out ";
            else if (isExtensionMethod && param.Position == 0) signature = "this ";
            if (!invokable) signature += GetSignature(param.ParameterType) + " ";
            signature += param.Name;
            return signature;
        });

        var methodParameterString = "(" + string.Join(", ", methodParameterSignatures) + ")";

        return methodParameterString;
    }

    /// <summary>Get a fully qualified signature for <paramref name="type" /></summary>
    /// <param name="type">Type. May be generic or <see cref="Nullable{T}" /></param>
    /// <returns>Fully qualified signature</returns>
    private static string GetSignature(this Type type)
    {
        var isNullableType = type.IsNullable(out type);
        var signature = GetQualifiedTypeName(type);
        if (type.IsGeneric()) signature += BuildGenericSignature(type.GetGenericArguments());
        if (isNullableType) signature += "?";

        return signature;
    }

    /// <summary>
    /// Takes an <see cref="IEnumerable{T}" /> and creates a generic type signature (&lt;string,
    /// string&gt; for example)
    /// </summary>
    /// <param name="genericArgumentTypes"></param>
    /// <returns>Generic type signature like &lt;Type, ...&gt;</returns>
    private static string BuildGenericSignature(IEnumerable<Type> genericArgumentTypes) => "<" + genericArgumentTypes.Select(GetSignature).ToStringDelimited(", ") + ">";

    /// <summary>
    /// Gets the fully qualified type name of <paramref name="type" />. This will use any
    /// keywords in place of types where possible (string instead of System.String for example)
    /// </summary>
    /// <param name="type"></param>
    /// <returns>The fully qualified name for <paramref name="type" /></returns>
    private static string GetQualifiedTypeName(Type type)
    {
        if (Constant.Type_PrimitiveAlias.TryGetValue(type, out var aliasString)) return aliasString;
        var signature = type.FullName.TrimOrNull() ?? type.Name;
        if (type.IsGeneric()) signature = signature.Substring(0, signature.IndexOf('`'));
        return signature;
    }


    public static bool IsPropertyMethod(this MethodInfo info) => IsPropertyMethod(info, true, true);
    public static bool IsPropertyMethodGet(this MethodInfo info) => IsPropertyMethod(info, true, false);
    public static bool IsPropertyMethodSet(this MethodInfo info) => IsPropertyMethod(info, false, true);

    private static bool IsPropertyMethod(MethodInfo info, bool isCheckGet, bool isCheckSet)
    {
        (isCheckGet || isCheckSet).AssertTrue();

        // https://stackoverflow.com/a/73908
        if (!info.IsSpecialName) return false;
        if ((info.Attributes & MethodAttributes.HideBySig) == 0) return false;


        var result = true;
        if (isCheckGet && isCheckSet)
        {
            if (!info.Name.StartsWithAny("get_", "set_")) result = false;
        }
        else if (isCheckGet)
        {
            if (!info.Name.StartsWith("get_")) result = false;
        }
        else if (isCheckSet)
        {
            if (!info.Name.StartsWith("set_")) result = false;
        }

        if (!result) return false;

        // https://stackoverflow.com/a/40128143
        var reflectedType = info.ReflectedType;
        if (reflectedType != null)
        {
            var methodHashCode = info.GetHashCode();
            foreach (var p in reflectedType.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (isCheckGet)
                {
                    var m = p.GetGetMethod(true);
                    if (m != null)
                    {
                        if (m.GetHashCode() == methodHashCode) return true;
                    }
                }

                if (isCheckSet)
                {
                    var m = p.GetSetMethod(true);
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
}
