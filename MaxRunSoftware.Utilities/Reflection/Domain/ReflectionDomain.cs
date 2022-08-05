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

public interface IReflectionDomain : IEquatable<IReflectionDomain>, IComparable<IReflectionDomain>
{
    int Id { get; }
    IReflectionAssembly GetAssembly(Assembly assembly);

    IReflectionConstructor CreateConstructor(IReflectionType parent, ConstructorInfo info);
    IReflectionEvent CreateEvent(IReflectionType parent, EventInfo info);
    IReflectionField CreateField(IReflectionType parent, FieldInfo info);
    IReflectionMethod CreateMethod(IReflectionType parent, MethodInfo info);
    IReflectionProperty CreateProperty(IReflectionType parent, PropertyInfo info);

    IReflectionCollection<T> EmptyCollection<T>() where T : class, IReflectionObject<T>;
}

public interface IReflectionDomainObject
{
    IReflectionDomain Domain { get; }
}

public class ReflectionDomain : ImmutableObjectBase<IReflectionDomain>, IReflectionDomain
{
    public bool ScanOtherAssembliesOnScan { get; set; }
    private volatile Dictionary<Type, object> emptyCollections = new();
    public IReflectionCollection<T> EmptyCollection<T>() where T : class, IReflectionObject<T>
    {
        var t = typeof(T);
        if (emptyCollections.TryGetValue(t, out var o)) return (IReflectionCollection<T>)o;
        lock (locker)
        {
            if (emptyCollections.TryGetValue(t, out o)) return (IReflectionCollection<T>)o;

            var c = ReflectionCollection.Empty<T>(this);
            var emptyCollectionsNew = new Dictionary<Type, object>(emptyCollections);
            emptyCollectionsNew.Add(t, c);
            emptyCollections = emptyCollectionsNew;
            return c;
        }
    }


    public IReadOnlyDictionary<AssemblySlim, ReflectionAssembly> GetAssemblies()
    {
        lock (locker)
        {
            return new Dictionary<AssemblySlim, ReflectionAssembly>(d);
        }
    }

    private readonly object locker = new();

    private volatile Dictionary<AssemblySlim, ReflectionAssembly> d = new(); // volatile is required http://disq.us/p/2ge3kge


    public int Id { get; } = Constant.NextInt();

    public IReflectionConstructor CreateConstructor(IReflectionType parent, ConstructorInfo info) => new ReflectionConstructor(parent, info);
    public IReflectionEvent CreateEvent(IReflectionType parent, EventInfo info) => new ReflectionEvent(parent, info);
    public IReflectionField CreateField(IReflectionType parent, FieldInfo info) => new ReflectionField(parent, info);
    public IReflectionMethod CreateMethod(IReflectionType parent, MethodInfo info) => new ReflectionMethod(parent, info);
    public IReflectionProperty CreateProperty(IReflectionType parent, PropertyInfo info) => new ReflectionProperty(parent, info);


    public IReflectionAssembly GetAssembly(Assembly assembly) => GetAssembly(new AssemblySlim(assembly));
    private IReflectionAssembly GetAssembly(AssemblySlim assembly)
    {
        if (d.TryGetValue(assembly, out var reflectionAssembly)) return reflectionAssembly;

        lock (locker)
        {
            if (d.TryGetValue(assembly, out reflectionAssembly)) return reflectionAssembly;

            var scannerResult = assembly.Assembly.IsSystemAssembly() ? new ReflectionScannerResult(assembly) : ReflectionScanner.Scan(assembly);
            reflectionAssembly = new ReflectionAssembly(this, assembly, scannerResult.TypesInThisAssembly);

            var dd = new Dictionary<AssemblySlim, ReflectionAssembly>(d);
            dd.Add(assembly, reflectionAssembly);
            d = dd;

            if (ScanOtherAssembliesOnScan)
            {
                foreach (var typeInOtherAssembly in scannerResult.TypesInOtherAssemblies)
                {
                    GetAssembly(typeInOtherAssembly.Assembly).GetType(typeInOtherAssembly);
                }
            }

            return reflectionAssembly;
        }
    }

    protected override int GetHashCode_Build() => Id;
    protected override string ToString_Build() => GetType().NameFormatted() + "[" + Id + "]";

    protected override bool Equals_Internal(IReflectionDomain other) => Id.Equals(other.Id);
    protected override int CompareTo_Internal(IReflectionDomain other) => Id.CompareTo(other.Id);
}

public static class ReflectionDomainExtensions
{
    public static IReflectionAssembly GetAssembly(this IReflectionDomain domain, AssemblySlim assembly) => domain.GetAssembly(assembly.Assembly);
    public static IReflectionType GetType<T>(this IReflectionDomain domain) => GetType(domain, typeof(T));
    public static IReflectionType GetType(this IReflectionDomain domain, Type type) => GetType(domain, new TypeSlim(type));
    public static IReflectionType GetType(this IReflectionDomain domain, TypeSlim typeSlim) => domain.GetAssembly(typeSlim.Assembly).GetType(typeSlim);
    public static IReflectionCollection<IReflectionType> GetTypes(this IReflectionDomain domain, params Type[] types) => GetTypes(domain, (IEnumerable<Type>)types.OrEmpty());
    public static IReflectionCollection<IReflectionType> GetTypes(this IReflectionDomain domain, IEnumerable<Type> types) => types.OrEmpty().Select(type => GetType(domain, new TypeSlim(type))).ToReflectionCollection(domain);
}
