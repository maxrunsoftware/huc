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

using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsReflection
{
    #region BindingFlags

    public static bool IsIgnoreCase(this BindingFlags flags) => (flags & BindingFlags.IgnoreCase) != 0;
    public static bool IsDeclaredOnly(this BindingFlags flags) => (flags & BindingFlags.DeclaredOnly) != 0;
    public static bool IsInstance(this BindingFlags flags) => (flags & BindingFlags.Instance) != 0;
    public static bool IsStatic(this BindingFlags flags) => (flags & BindingFlags.Static) != 0;
    public static bool IsPublic(this BindingFlags flags) => (flags & BindingFlags.Public) != 0;
    public static bool IsNonPublic(this BindingFlags flags) => (flags & BindingFlags.NonPublic) != 0;
    public static bool IsFlattenHierarch(this BindingFlags flags) => (flags & BindingFlags.FlattenHierarchy) != 0;

    public static bool IsInvokeMethod(this BindingFlags flags) => (flags & BindingFlags.InvokeMethod) != 0;
    public static bool IsCreateInstance(this BindingFlags flags) => (flags & BindingFlags.CreateInstance) != 0;
    public static bool IsGetField(this BindingFlags flags) => (flags & BindingFlags.GetField) != 0;
    public static bool IsSetField(this BindingFlags flags) => (flags & BindingFlags.SetField) != 0;
    public static bool IsGetProperty(this BindingFlags flags) => (flags & BindingFlags.GetProperty) != 0;
    public static bool IsSetProperty(this BindingFlags flags) => (flags & BindingFlags.SetProperty) != 0;

    public static bool IsPutDispProperty(this BindingFlags flags) => (flags & BindingFlags.PutDispProperty) != 0;
    public static bool IsPutRefDispProperty(this BindingFlags flags) => (flags & BindingFlags.PutRefDispProperty) != 0;

    public static bool IsExactBinding(this BindingFlags flags) => (flags & BindingFlags.ExactBinding) != 0;
    public static bool IsSuppressChangeType(this BindingFlags flags) => (flags & BindingFlags.SuppressChangeType) != 0;

    public static bool IsOptionalParamBinding(this BindingFlags flags) => (flags & BindingFlags.OptionalParamBinding) != 0;

    public static bool IsIgnoreReturn(this BindingFlags flags) => (flags & BindingFlags.IgnoreReturn) != 0;
    public static bool IsDoNotWrapExceptions(this BindingFlags flags) => (flags & BindingFlags.DoNotWrapExceptions) != 0;

    #endregion BindingFlags

    public static string GetFileVersion(this Assembly assembly) => FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;

    public static string GetVersion(this Assembly assembly) => assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;

    private static readonly string anonymousMagicTag;
    private static readonly string innerMagicTag;
    public static bool IsAnonymous(this MethodInfo mi) => mi.Name.Contains(anonymousMagicTag);
    public static bool IsInner(this MethodInfo mi) => mi.Name.Contains(innerMagicTag);

    private static string GetNameMagicTag(this MethodInfo mi)
    {
        var match = new Regex(">([a-zA-Z]+)__").Match(mi.Name);
        if (match.Success && match.Value is { } val && !match.NextMatch().Success) return val;

        throw new ArgumentException($"Cant find magic tag of {mi}");
    }

    static ExtensionsReflection()
    {
        // https://stackoverflow.com/a/56496797
        void Inner() { }
        ;
        var inner = Inner;
        innerMagicTag = GetNameMagicTag(inner.Method);

        var anonymous = () => { };
        anonymousMagicTag = GetNameMagicTag(anonymous.Method);
    }


    public static bool IsSystemAssembly(this Assembly assembly)
    {
        assembly.CheckNotNull(nameof(assembly));

        var namePrefixes = new[]
        {
            "CommonLanguageRuntimeLibrary",
            "System.",
            "System,",
            "mscorlib,",
            "netstandard,",
            "Microsoft.CSharp",
            "Microsoft.VisualStudio"
        };

        foreach (var module in assembly.Modules)
        {
            var scopeName = module.ScopeName.TrimOrNull();
            if (scopeName == null) continue;
            if (scopeName.StartsWithAny(StringComparison.OrdinalIgnoreCase, namePrefixes)) return true;
        }

        var name = assembly.FullName.TrimOrNull();
        if (name != null && name.StartsWithAny(StringComparison.OrdinalIgnoreCase, namePrefixes)) return true;

        name = assembly.ToString().TrimOrNull();
        if (name != null && name.StartsWithAny(StringComparison.OrdinalIgnoreCase, namePrefixes)) return true;

        var assemblyName = assembly.GetName();
        name = assemblyName.ToString().TrimOrNull();
        if (name != null && name.StartsWithAny(StringComparison.OrdinalIgnoreCase, namePrefixes)) return true;

        name = assemblyName.FullName.TrimOrNull();
        if (name != null && name.StartsWithAny(StringComparison.OrdinalIgnoreCase, namePrefixes)) return true;

        name = assemblyName.Name.TrimOrNull();
        if (name != null && name.StartsWithAny(StringComparison.OrdinalIgnoreCase, namePrefixes)) return true;

        return false;
    }

    #region PropertyInfo

    public static bool IsNullable(this PropertyInfo info)
    {
        var type = info.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null) return true;

        if (type.IsValueType) return false;

        // https://devblogs.microsoft.com/dotnet/announcing-net-6-preview-7/#libraries-reflection-apis-for-nullability-information
        // https://stackoverflow.com/a/68757807

        var ctx = new NullabilityInfoContext();
        var nullabilityInfo = ctx.Create(info);
        if (nullabilityInfo.WriteState == NullabilityState.Nullable) return true;
        if (nullabilityInfo.WriteState == NullabilityState.NotNull) return false;
        if (nullabilityInfo.ReadState == NullabilityState.Nullable) return true;
        if (nullabilityInfo.ReadState == NullabilityState.NotNull) return false;
        return true;
    }

    public static bool IsGettable(this PropertyInfo info) => info.CanRead && info.GetMethod != null;

    public static bool IsSettable(this PropertyInfo info) => info.CanWrite && info.SetMethod != null;

    /// <summary>
    /// Determines if this property is marked as init-only.
    /// </summary>
    /// <param name="info">The property.</param>
    /// <returns>True if the property is init-only, false otherwise.</returns>
    public static bool IsInitOnly(this PropertyInfo info)
    {
        // https://alistairevans.co.uk/2020/11/01/detecting-init-only-properties-with-reflection-in-c-9/
        if (!info.CanWrite) return false;

        var setMethod = info.SetMethod;
        if (setMethod == null) return false;

        // Get the modifiers applied to the return parameter.
        var setMethodReturnParameterModifiers = setMethod.ReturnParameter.GetRequiredCustomModifiers();

        // Init-only properties are marked with the IsExternalInit type.
        return setMethodReturnParameterModifiers.Contains(typeof(IsExternalInit));
    }


    public static Func<object, object> CreatePropertyGetter(this PropertyInfo info)
    {
        var exceptionMsg = $"Property {GetTypeNamePrefix(info)}{info.Name} is not gettable";
        if (!info.CanRead) throw new ArgumentException(exceptionMsg, nameof(info));

        // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
        var mi = info.GetMethod;
        if (mi == null) throw new ArgumentException(exceptionMsg, nameof(info));

        //IsStatic = mi.IsStatic;
        var instance = Expression.Parameter(typeof(object), "instance");
        var callExpr = mi.IsStatic // Is this a static property
            ? Expression.Call(null, mi)
            : Expression.Call(Expression.Convert(instance, info.DeclaringType ?? throw new NullReferenceException()), mi);

        var unaryExpression = Expression.TypeAs(callExpr, typeof(object));
        var action = Expression.Lambda<Func<object, object>>(unaryExpression, instance).Compile();
        return action;
    }

    public static Action<object, object> CreatePropertySetter(this PropertyInfo info)
    {
        var exceptionMsg = $"Property {GetTypeNamePrefix(info)}{info.Name} is not settable";
        if (!info.CanWrite) throw new ArgumentException(exceptionMsg, nameof(info));

        // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
        var methodInfo = info.SetMethod;
        if (methodInfo == null) throw new ArgumentException(exceptionMsg, nameof(info));

        //IsStatic = mi.IsStatic;
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var valueConverted = Expression.Convert(value, info.PropertyType);
        var callExpr = methodInfo.IsStatic // Is this a static property
            ? Expression.Call(null, methodInfo, valueConverted)
            : Expression.Call(Expression.Convert(instance, info.DeclaringType ?? throw new NullReferenceException()), methodInfo, valueConverted);
        var action = Expression.Lambda<Action<object, object>>(callExpr, instance, value).Compile();

        return action;
    }

    #endregion PropertyInfo

    private static string GetTypeNamePrefix(MemberInfo info)
    {
        var type = info.ReflectedType;
        if (type == null) type = info.DeclaringType;
        if (type == null) return string.Empty;
        return type.FullNameFormatted() + ".";
    }

    #region FieldInfo

    public static bool IsSettable(this FieldInfo info) => !info.IsLiteral && !info.IsInitOnly;

    public static Func<object, object> CreateFieldGetter(this FieldInfo info)
    {
        // https://stackoverflow.com/a/321686
        var instance = Expression.Parameter(typeof(object), "instance");
        var fieldExpr = info.IsStatic ? Expression.Field(null, info) : Expression.Field(Expression.Convert(instance, info.DeclaringType ?? throw new NullReferenceException()), info);
        var unaryExpression = Expression.TypeAs(fieldExpr, typeof(object));
        var action = Expression.Lambda<Func<object, object>>(unaryExpression, instance).Compile();
        return action;
    }


    public static Action<object, object> CreateFieldSetter(this FieldInfo info)
    {
        // Can no longer write to 'readonly' field
        // https://stackoverflow.com/questions/934930/can-i-change-a-private-readonly-field-in-c-sharp-using-reflection#comment116393125_934942
        if (!info.IsSettable()) throw new ArgumentException($"Field {GetTypeNamePrefix(info)}{info.Name} is not settable", nameof(info));

        // https://stackoverflow.com/a/321686
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var valueConverted = Expression.Convert(value, info.FieldType);
        var fieldExpr = info.IsStatic ? Expression.Field(null, info) : Expression.Field(Expression.Convert(instance, info.DeclaringType ?? throw new NullReferenceException()), info);
        var assignExp = Expression.Assign(fieldExpr, valueConverted);

        var action = Expression.Lambda<Action<object, object>>(assignExp, instance, value).Compile();
        return action;
    }

    #endregion FieldInfo

    /// <summary>
    /// https://stackoverflow.com/a/38110036
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public static bool IsOut(this ParameterInfo info) => info.ParameterType.IsByRef && !info.IsOut;

    /// <summary>
    /// https://stackoverflow.com/a/38110036
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public static bool IsIn(this ParameterInfo info) => info.ParameterType.IsByRef && info.IsIn;

    /// <summary>
    /// https://stackoverflow.com/a/38110036
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public static bool IsRef(this ParameterInfo info) => info.ParameterType.IsByRef && !info.IsOut;
}
