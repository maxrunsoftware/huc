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

public interface IReflectionAssembly : IReflectionObject<IReflectionAssembly>
{
    AssemblySlim AssemblySlim { get; }
    string NameFull { get; }
    bool IsSystemAssembly { get; }
    IReflectionCollection<IReflectionType> Types { get; }
    IReflectionCollection<IReflectionType> TypesReal { get; }
    IReflectionCollection<IReflectionType> TypesNotReal { get; }
    IReflectionType GetType(TypeSlim typeSlim);
}

public sealed class ReflectionAssembly : ReflectionObject<IReflectionAssembly>, IReflectionAssembly
{
    public ReflectionAssembly(IReflectionDomain domain, AssemblySlim assemblySlim, IEnumerable<TypeSlim> types) : base(domain)
    {
        typesContainerRecord = new TypesContainerRecord(ReflectionCollection.Empty<IReflectionType>(Domain));

        AssemblySlim = assemblySlim;
        IsSystemAssembly = Assembly.IsSystemAssembly();
        foreach (var typeSlim in types) AddType(typeSlim);
    }


    public IReadOnlyDictionary<TypeSlim, IReflectionType> GetTypesCache()
    {
        lock (locker)
        {
            return new Dictionary<TypeSlim, IReflectionType>(typesCache);
        }
    }

    private readonly object locker = new();
    public AssemblySlim AssemblySlim { get; }
    public Assembly Assembly => AssemblySlim.Assembly;
    public string NameFull => AssemblySlim.NameFull;
    public bool IsSystemAssembly { get; }

    private readonly Dictionary<TypeSlim, IReflectionType> typesCache = new();
    private volatile bool typesIsDirty;

    private class TypesContainerRecord
    {
        public IReflectionCollection<IReflectionType> All { get; }

        public IReflectionCollection<IReflectionType> Real => real.Value;
        private readonly Lzy<IReflectionCollection<IReflectionType>> real;

        public IReflectionCollection<IReflectionType> NotReal => notReal.Value;
        private readonly Lzy<IReflectionCollection<IReflectionType>> notReal;

        public TypesContainerRecord(IReflectionCollection<IReflectionType> all)
        {
            All = all;
            real = CreateLazy(() => All.Where(o => o.IsRealType));
            notReal = CreateLazy(() => All.Where(o => !o.IsRealType));
        }
    }

    private volatile TypesContainerRecord typesContainerRecord;

    private TypesContainerRecord TypesContainer
    {
        get
        {
            lock (locker)
            {
                if (typesIsDirty)
                {
                    var valCol = typesCache.Values;
                    if (typesContainerRecord.All.Count != valCol.Count) typesContainerRecord = new TypesContainerRecord(valCol.ToReflectionCollection(Domain));

                    typesIsDirty = false;
                }

                return typesContainerRecord;
            }
        }
    }

    public IReflectionCollection<IReflectionType> Types => TypesContainer.All;
    public IReflectionCollection<IReflectionType> TypesReal => TypesContainer.Real;
    public IReflectionCollection<IReflectionType> TypesNotReal => TypesContainer.NotReal;

    public IReflectionType GetType(TypeSlim typeSlim)
    {
        typeSlim.CheckNotNull(nameof(typeSlim));
        if (!AssemblySlim.Equals(typeSlim.Assembly)) throw new Exception(typeSlim + " is not in assembly " + this);
        lock (locker)
        {
            return typesCache.TryGetValue(typeSlim, out var typeReflection) ? typeReflection : AddType(typeSlim);
        }
    }

    private IReflectionType AddType(TypeSlim typeSlim)
    {
        if (!AssemblySlim.Equals(typeSlim.Assembly)) throw new Exception(typeSlim + " is not in assembly " + this);
        var typeReflection = new ReflectionType(this, typeSlim);
        typesCache[typeSlim] = typeReflection;
        typesIsDirty = true;
        return typeReflection;
    }


    #region Overrides

    protected override string Name_Build() => Assembly.GetName().Name.TrimOrNull() ?? NameFull;
    protected override Attribute[] Attributes_Build(bool inherited) => Attribute.GetCustomAttributes(Assembly);
    protected override int GetHashCode_Build() => AssemblySlim.GetHashCode();
    protected override string ToString_Build() => NameFull;

    protected override bool Equals_Internal(IReflectionAssembly other) => AssemblySlim.Equals(other.AssemblySlim);
    protected override int CompareTo_Internal(IReflectionAssembly other) => AssemblySlim.CompareTo(other.AssemblySlim);

    #endregion Overrides
}
