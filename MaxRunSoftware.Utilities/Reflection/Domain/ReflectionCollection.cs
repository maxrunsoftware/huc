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

public interface IReflectionCollection<T> :
    IReflectionDomainObject,
    IReadOnlyList<T>, IReadOnlySet<T>,
    IEquatable<IReflectionCollection<T>>, IComparable<IReflectionCollection<T>>
    where T : class, IReflectionObject<T>
{
    T this[string name] { get; }

    IReflectionCollection<T> Where(params Predicate<T>[] predicates);
    IReflectionCollection<T> Where(bool isSorted, params Predicate<T>[] predicates);
}

public static class ReflectionCollection
{
    public static IReflectionCollection<T> Empty<T>(IReflectionDomain domain) where T : class, IReflectionObject<T> => ReflectionCollection<T>.Empty<T>(domain);
}

public class ReflectionCollection<T> : ImmutableObjectBase<IReflectionCollection<T>>, IReflectionCollection<T> where T : class, IReflectionObject<T>
{
    public IReflectionDomain Domain { get; }
    private readonly ImmutableHashSet<T> set;
    private readonly ImmutableList<T> list;
    private ReflectionCollection(IReflectionDomain domain, ImmutableHashSet<T> set, ImmutableList<T> list)
    {
        Domain = domain.CheckNotNull(nameof(domain));
        this.set = set;
        this.list = list;
    }

    public static IReflectionCollection<TItem> Create<TItem>(IReflectionDomain domain, bool isSorted, IEnumerable<TItem> enumerable) where TItem : class, IReflectionObject<TItem>
    {
        var objs = enumerable.OrEmpty().WhereNotNull().ToList();
        if (objs.Count == 0) return Empty<TItem>(domain);

        var s = new HashSet<TItem>();
        var l = new List<TItem>();

        foreach (var item in objs)
        {
            if (s.Add(item)) l.Add(item);
        }

        var ll = l.AsEnumerable();
        if (isSorted) ll = ll.OrderBy(o => o);

        return new ReflectionCollection<TItem>(domain, s.ToImmutableHashSet(), ll.ToImmutableList());
    }

    #region Empty

    public static IReflectionCollection<TItem> Empty<TItem>(IReflectionDomain domain) where TItem : class, IReflectionObject<TItem> => new ReflectionCollection<TItem>(domain, ImmutableHashSet<TItem>.Empty, ImmutableList<TItem>.Empty);

    #endregion Empty

    #region Overrides

    public T this[string name]
    {
        get
        {
            name.CheckNotNullTrimmed(nameof(name));
            var c = this.Named(name);
            if (c.Count == 0) throw new ArgumentOutOfRangeException(nameof(name), name, $"{nameof(name)} '{name}' was not found in {this}");
            if (c.Count > 1) throw new ArgumentOutOfRangeException(nameof(name), name, $"{nameof(name)} '{name}' was found ({c.Count}) times in {this}");
            return c[0];
        }
    }

    public IReflectionCollection<T> Where(params Predicate<T>[] predicates) => Where(true, predicates);
    public IReflectionCollection<T> Where(bool isSorted, params Predicate<T>[] predicates) => Create(Domain, isSorted, list.Where(item => predicates.Any(predicate => predicate(item))));

    #endregion Overrides


    public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)list).GetEnumerator();
    public T this[int index] => list[index];

    public int Count => list.Count;
    public bool Contains(T item) => set.Contains(item);
    public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
    public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);

    protected override bool Equals_Internal(IReflectionCollection<T> other)
    {
        if (Count != other.Count) return false;
        return this.EqualsEnumerable(other);
    }


    protected override int CompareTo_Internal(IReflectionCollection<T> other)
    {
        int c;
        if (0 != (c = this.CompareToEnumerable(other))) return c;
        return 0;
    }

    protected override int GetHashCode_Build() => Count == 0 ? 0 : Util.GenerateHashCodeReadOnlyCollection(this);
    protected override string ToString_Build() => GetType().NameFormatted() + " (" + set.Count + ") [ " + set.Select(o => o.ToStringInCollection()).ToStringDelimited(" | ") + " ]";
}

public static class ReflectionCollectionExtensions
{
    public static IReflectionCollection<T> ToReflectionCollection<T>(this IEnumerable<T> enumerable, IReflectionDomain domain, bool isSorted = true) where T : class, IReflectionObject<T> => ReflectionCollection<T>.Create(domain, isSorted, enumerable);
}
