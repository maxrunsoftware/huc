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

/// <summary>
/// Formatting of methods for debugging
/// https://github.com/kellyelton/System.Reflection.ExtensionMethods/tree/master/System.Reflection.ExtensionMethods
/// </summary>
public static class ExtensionsMethod
{
    public static string GetSignature(this MethodBase method, bool invokable)
    {
        var signatureBuilder = new StringBuilder();

        // Add our method accessors if it's not invokable
        if (!invokable)
        {
            signatureBuilder.Append(GetMethodAccessorSignature(method));
            signatureBuilder.Append(" ");
        }

        // Add method name
        signatureBuilder.Append(method.Name);

        // Add method generics
        if (method.IsGenericMethod) signatureBuilder.Append(GetGenericSignature(method));

        // Add method parameters
        signatureBuilder.Append(GetMethodArgumentsSignature(method, invokable));

        return signatureBuilder.ToString();
    }

    public static string GetMethodAccessorSignature(this MethodBase method)
    {
        string signature = null;

        if (method.IsAssembly)
        {
            signature = "internal ";

            if (method.IsFamily) signature += "protected ";
        }
        else if (method.IsPublic) { signature = "public "; }
        else if (method.IsPrivate) { signature = "private "; }
        else if (method.IsFamily) { signature = "protected "; }

        if (method.IsStatic) signature += "static ";

        if (method is MethodInfo methodInfo) signature += GetSignature(methodInfo.ReturnType);

        return signature;
    }

    public static string GetGenericSignature(this MethodBase method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));

        if (!method.IsGenericMethod) throw new ArgumentException($"{method.Name} is not generic.");

        return BuildGenericSignature(method.GetGenericArguments());
    }

    public static string GetMethodArgumentsSignature(this MethodBase method, bool invokable)
    {
        var isExtensionMethod = method.IsDefined(typeof(ExtensionAttribute), false);
        var methodParameters = method.GetParameters().AsEnumerable();

        // If this signature is designed to be invoked and it's an extension method
        if (isExtensionMethod && invokable)
            // Skip the first argument
            methodParameters = methodParameters.Skip(1);

        var methodParameterSignatures = methodParameters.Select(param =>
        {
            var signature = string.Empty;

            if (param.ParameterType.IsByRef)
                signature = "ref ";
            else if (param.IsOut)
                signature = "out ";
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
    public static string GetSignature(this Type type)
    {
        var isNullableType = type.IsNullable(out var underlyingNullableType);

        var signatureType = isNullableType
            ? underlyingNullableType
            : type;

        var isGenericType = signatureType.IsGeneric();

        var signature = GetQualifiedTypeName(signatureType);

        if (isGenericType)
            // Add the generic arguments
            signature += BuildGenericSignature(signatureType.GetGenericArguments());

        if (isNullableType) signature += "?";

        return signature;
    }

    /// <summary>
    /// Takes an <see cref="IEnumerable{T}" /> and creates a generic type signature (&lt;string,
    /// string&gt; for example)
    /// </summary>
    /// <param name="genericArgumentTypes"></param>
    /// <returns>Generic type signature like &lt;Type, ...&gt;</returns>
    public static string BuildGenericSignature(IEnumerable<Type> genericArgumentTypes)
    {
        var argumentSignatures = genericArgumentTypes.Select(GetSignature);

        return "<" + string.Join(", ", argumentSignatures) + ">";
    }

    /// <summary>
    /// Gets the fully qualified type name of <paramref name="type" />. This will use any
    /// keywords in place of types where possible (string instead of System.String for example)
    /// </summary>
    /// <param name="type"></param>
    /// <returns>The fully qualified name for <paramref name="type" /></returns>
    public static string GetQualifiedTypeName(Type type)
    {
        switch (type.Name.ToUpperInvariant())
        {
            case "SYSTEM.BOOLEAN":
            case "BOOLEAN":
            case "BOOL":
                return "bool";

            case "SYSTEM.BYTE":
            case "BYTE":
                return "byte";

            case "SYSTEM.SBYTE":
            case "SBYTE":
                return "sbyte";

            case "SYSTEM.CHAR":
            case "CHAR":
                return "char";

            case "SYSTEM.DECIMAL":
            case "DECIMAL":
                return "decimal";

            case "SYSTEM.DOUBLE":
            case "DOUBLE":
                return "double";

            case "SYSTEM.SINGLE":
                return "float";

            case "SYSTEM.INT32":
            case "INT32":
            case "SYSTEM.INT":
            case "INT":
                return "int";

            case "SYSTEM.UINT32":
            case "UINT32":
            case "SYSTEM.UINT":
            case "UINT":
                return "uint";

            case "SYSTEM.INT64":
            case "INT64":
            case "SYSTEM.LONG":
            case "LONG":
                return "long";

            case "SYSTEM.UINT64":
            case "UINT64":
            case "SYSTEM.ULONG":
            case "ULONG":
                return "ulong";

            case "SYSTEM.INT16":
            case "INT16":
            case "SYSTEM.SHORT":
            case "SHORT":
                return "short";

            case "SYSTEM.UINT16":
            case "UINT16":
            case "SYSTEM.USHORT":
            case "USHORT":
                return "ushort";

            case "SYSTEM.OBJECT":
            case "OBJECT":
                return "object";

            case "SYSTEM.STRING":
            case "STRING":
                return "string";

            case "SYSTEM.VOID":
            case "VOID":
                return "void";
        }

        var signature = type.FullName.TrimOrNull() ?? type.Name;

        if (type.IsGeneric()) signature = RemoveGenericTypeNameArgumentCount(signature);

        return signature;
    }

    /// <summary>This removes the `{argumentCount} from a the signature of a generic type</summary>
    /// <param name="genericTypeSignature">Signature of a generic type</param>
    /// <returns><paramref name="genericTypeSignature" /> without any argument count</returns>
    public static string RemoveGenericTypeNameArgumentCount(string genericTypeSignature) => genericTypeSignature.Substring(0, genericTypeSignature.IndexOf('`'));
}
