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

public interface IReflectionGeneric
{
    IReflectionCollection<IReflectionType> GenericParameters { get; }
}

public static class ReflectionGenericExtensions
{
    public static IReflectionCollection<T> GenericParameters<T>(this IReflectionCollection<T> obj, int count) where T : class, IReflectionObject<T>, IReflectionGeneric => obj.Where(item => item.GenericParameters.Count == count);
    public static IReflectionCollection<T> GenericParameters<T>(this IReflectionCollection<T> obj, bool isExactType = true, params Type[] types) where T : class, IReflectionObject<T>, IReflectionGeneric => obj.Where(item => item.GenericParameters.IsAssignableTo(obj.Domain, isExactType, types));
    public static IReflectionCollection<T> GenericParameters<T>(this IReflectionCollection<T> obj, bool isExactType = true, params IReflectionType[] types) where T : class, IReflectionObject<T>, IReflectionGeneric => obj.Where(item => item.GenericParameters.IsAssignableTo(isExactType, types));
}

public interface IReflectionParameterized
{
    IReflectionCollection<IReflectionParameter> Parameters { get; }
}

public static class ReflectionParameterizedExtensions
{
    public static IReflectionCollection<T> Parameters<T>(this IReflectionCollection<T> obj, int count) where T : class, IReflectionObject<T>, IReflectionParameterized => obj.Where(item => item.Parameters.Count == count);
    public static IReflectionCollection<T> Parameters<T>(this IReflectionCollection<T> obj, bool isExactType = true, params Type[] types) where T : class, IReflectionObject<T>, IReflectionParameterized => obj.Where(method => method.Parameters.Select(o => o.Type).IsAssignableTo(obj.Domain, isExactType, types));
    public static IReflectionCollection<T> Parameters<T>(this IReflectionCollection<T> obj, bool isExactType = true, params IReflectionType[] types) where T : class, IReflectionObject<T>, IReflectionParameterized => obj.Where(method => method.Parameters.Select(o => o.Type).IsAssignableTo(isExactType, types));
}

public interface IReflectionGetSet
{
    IReflectionType Type { get; }
    bool CanGet { get; }
    bool CanSet { get; }
    object? GetValue(object? instance);
    void SetValue(object? instance, object? value);
}

public static class ReflectionGetSetExtensions
{
    public static object? GetValueStatic(this IReflectionGetSet obj) => obj.GetValue(null);
    public static void SetValueStatic(this IReflectionGetSet obj, object? value) => obj.SetValue(null, value);
}

public interface IReflectionInfo<out TInfo> where TInfo : class
{
    TInfo Info { get; }
}

public interface IReflectionBase<T> where T : class, IReflectionBase<T>, IReflectionObject<T>
{
    T? Base { get; }
    IReflectionCollection<T> Bases { get; }
}

public static class ReflectionBaseExtensions
{
    internal static IReflectionCollection<T> Bases_Build<T>(this IReflectionBase<T> obj, IReflectionDomain domain) where T : class, IReflectionObject<T>, IReflectionBase<T>
    {
        var list = new List<T>();
        var b = obj.Base;
        while (true)
        {
            if (b == null) break;
            if (list.Any(listB => ReferenceEquals(b, listB))) break; // Safety check
            list.Add(b);
            b = b.Base;
        }

        return list.ToReflectionCollection(domain, false);
    }
}

public interface IReflectionDeclaringType
{
    IReflectionType? DeclaringType { get; }
}

public static class ReflectionDeclaringTypeExtensions
{
    //public static IReflectionCollection<T> DeclaringType<T>(this IReflectionCollection<T> obj, bool isExactType = true, params Type[] types) where T : class, IReflectionObject<T>, IReflectionDeclaringType => obj.Where(method => method.DeclaringType != null && method.DeclaringType.IsEqual(obj.Domain, isExactType, types));
    //public static IReflectionCollection<T> DeclaringType<T>(this IReflectionCollection<T> obj, bool isExactType = true, params IReflectionType[] types) where T : class, IReflectionObject<T>, IReflectionDeclaringType => obj.Where(method => method.Parameters.Select(o => o.Type).IsEqual(isExactType, types));
}

public interface IReflectionReflectedType
{
    IReflectionType? ReflectedType { get; }
}

public interface IReflectionDeclarationFlags
{
    DeclarationFlags DeclarationFlags { get; }
}

public static class ReflectionDeclarationFlagsExtensions
{
    #region IReflectionDeclarationFlags

    public static bool IsPublic(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsPublic();
    public static bool IsProtected(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsProtected();
    public static bool IsPrivate(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsPrivate();
    public static bool IsInternal(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsInternal();
    public static bool IsStatic(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsStatic();
    public static bool IsInstance(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsInstance();
    public static bool IsAbstract(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsAbstract();
    public static bool IsVirtual(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsVirtual();
    public static bool IsOverride(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsOverride();
    public static bool IsNewShadowSignature(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsNewShadowSignature();
    public static bool IsNewShadowName(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsNewShadowName();
    public static bool IsExplicit(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsExplicit();
    public static bool IsSealed(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsSealed();
    public static bool IsReadonly(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsReadonly();
    public static bool IsInherited(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsInherited();
    public static bool IsGenericParameter(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsGenericParameter();

    public static bool IsOverridable(this IReflectionDeclarationFlags obj) => obj.DeclarationFlags.IsOverridable(); // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.isfinal

    #endregion IReflectionDeclarationFlags
}
