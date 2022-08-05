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

public interface IReflectionObject : IReflectionDomainObject
{
    string Name { get; }
    IReflectionCollection<IReflectionAttribute> Attributes { get; }
    IReflectionCollection<IReflectionAttribute> AttributesDeclaredOnly { get; }

    string ToStringInCollection();
}

public interface IReflectionObject<T> : IReflectionObject, IEquatable<T>, IComparable<T>, IComparable where T : IReflectionObject<T> { }

public abstract class ReflectionObject<T> : ImmutableObjectBase<T>, IReflectionObject<T> where T : class, IReflectionObject<T>
{
    public IReflectionDomain Domain { get; }

    private readonly Lzy<string> name;
    public string Name => name.Value;
    protected abstract string Name_Build();

    private readonly Lzy<string> toStringInCollection;
    public string ToStringInCollection() => toStringInCollection.Value;
    protected virtual string ToStringInCollection_Build() => ToString();

    #region Attributes

    public IReflectionCollection<IReflectionAttribute> Attributes => attributes.Value;
    private readonly Lzy<IReflectionCollection<IReflectionAttribute>> attributes;
    private IReflectionCollection<IReflectionAttribute> Attributes_Build() => GetAttributes(Attributes_Build(true));

    public IReflectionCollection<IReflectionAttribute> AttributesDeclaredOnly => attributesDeclaredOnly.Value;
    private readonly Lzy<IReflectionCollection<IReflectionAttribute>> attributesDeclaredOnly;
    private IReflectionCollection<IReflectionAttribute> AttributesDeclaredOnly_Build() => GetAttributes(Attributes_Build(false));

    private IReflectionCollection<IReflectionAttribute> GetAttributes(Attribute[] attrs) => attrs.OrEmpty().WhereNotNull().Select(o => new ReflectionAttribute(Domain, o)).Cast<IReflectionAttribute>().ToReflectionCollection(Domain);

    protected abstract Attribute[] Attributes_Build(bool inherited);

    #endregion Attributes

    protected ReflectionObject(IReflectionDomain domain)
    {
        Domain = domain.CheckNotNull(nameof(domain));
        name = CreateLazy(Name_Build);
        attributes = CreateLazy(Attributes_Build);
        attributesDeclaredOnly = CreateLazy(AttributesDeclaredOnly_Build);
        toStringInCollection = CreateLazy(ToStringInCollection_Build);
    }

    #region HelpersInstance

    protected IReflectionType? GetType(Type? type) => type == null ? null : Domain.GetType(type);

    protected IReflectionCollection<IReflectionParameter> GetParameters(MethodBase info) => info.CheckNotNull(nameof(info)).GetParameters().OrEmpty().Select(o => new ReflectionParameter(Domain, o)).Cast<IReflectionParameter>().ToReflectionCollection(Domain);

    protected IReflectionCollection<IReflectionType> GetGenericParameters(MethodBase info) => info.CheckNotNull(nameof(info)).IsGenericMethod ? info.GetGenericArguments().OrEmpty().Select(GetType).ToReflectionCollection(Domain) : ReflectionCollection.Empty<IReflectionType>(Domain);

    protected IReflectionCollection<IReflectionType> GetGenericParameters(TypeInfo info) => info.CheckNotNull(nameof(info)).IsGenericType ? info.GetGenericArguments().OrEmpty().Select(GetType).ToReflectionCollection(Domain) : ReflectionCollection.Empty<IReflectionType>(Domain);

    protected IReflectionCollection<TItem> EmptyCollection<TItem>() where TItem : class, IReflectionObject<TItem> => Domain.EmptyCollection<TItem>();

    #endregion HelpersInstance

    #region HelpersStatic

    protected static bool IsEqual(DeclarationFlags? x, DeclarationFlags? y) => x == y;

    protected static bool IsEqual<TItem>(IReflectionCollection<TItem> x, IReflectionCollection<TItem> y) where TItem : class, IReflectionObject<TItem>
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        return x.Equals(y);
    }

    protected static int Compare<TItem>(IReflectionCollection<TItem> x, IReflectionCollection<TItem> y) where TItem : class, IReflectionObject<TItem>
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(x, null)) return -1;
        if (ReferenceEquals(y, null)) return 1;
        return x.CompareTo(y);
    }

    protected static int Compare(DeclarationFlags? x, DeclarationFlags? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        return x.Value.CompareTo(y.Value);
    }

    #endregion HelpersStatic
}

public static class ReflectionObjectExtensions
{
    public static IReflectionCollection<T> Named<T>(this IReflectionCollection<T> obj, string name, bool ignoreCase = false) where T : class, IReflectionObject<T>
    {
        var sc = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        name = name.CheckNotNullTrimmed(nameof(name));
        if (name.Contains('<')) return obj.Where(o => sc.Equals(o.Name, name)).ToReflectionCollection(obj.Domain);

        var scm = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var nameGeneric = name + "<";
        return obj.Where(o => sc.Equals(o.Name, name) || o.Name.StartsWith(nameGeneric, scm));
    }

    internal static IReflectionCollection<T> ReturnType<T>(this IReflectionCollection<T> obj, Func<T, IReflectionType> funcType, bool isExactType, params Type[] returnTypes) where T : class, IReflectionObject<T>
    {
        obj.CheckNotNull(nameof(obj));
        returnTypes.CheckNotNull(nameof(returnTypes));

        return obj.ReturnType(funcType, isExactType, obj.Domain.GetTypes(returnTypes).ToArray());
    }
    internal static IReflectionCollection<T> ReturnType<T>(this IReflectionCollection<T> obj, Func<T, IReflectionType> funcType, bool isExactType, params IReflectionType[] returnTypes) where T : class, IReflectionObject<T>
    {
        obj.CheckNotNull(nameof(obj));
        funcType.CheckNotNull(nameof(funcType));
        returnTypes.CheckNotNull(nameof(returnTypes));

        if (obj.Count == 0 || returnTypes.Length == 0) return ReflectionCollection.Empty<T>(obj.Domain);

        var l = new List<T>();

        IEnumerable<T> enumerator = obj.Where(item => funcType(item).IsAssignableToAny(isExactType, returnTypes));
        foreach (var item in obj)
        {
            if (funcType(item).IsAssignableToAny(isExactType, returnTypes)) l.Add(item);
        }

        return l.ToReflectionCollection(obj.Domain);
    }
}
