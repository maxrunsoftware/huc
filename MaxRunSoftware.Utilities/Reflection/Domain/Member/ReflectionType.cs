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

public interface IReflectionType :
    IReflectionObject<IReflectionType>,
    IReflectionDeclarationFlags,
    IReflectionChild<IReflectionAssembly>,
    IReflectionGeneric,
    IReflectionInfo<TypeInfo>,
    IReflectionBase<IReflectionType>,
    IReflectionDeclaringType, IReflectionReflectedType
{
    Type Type { get; }
    TypeSlim TypeSlim { get; }
    string NameFull { get; }
    bool IsRealType { get; }

    IReflectionCollection<IReflectionConstructor> Constructors { get; }
    IReflectionCollection<IReflectionEvent> Events { get; }
    IReflectionCollection<IReflectionField> Fields { get; }
    IReflectionCollection<IReflectionMethod> Methods { get; }
    IReflectionCollection<IReflectionProperty> Properties { get; }
    IReflectionCollection<IReflectionType> Bases { get; }
}

public sealed class ReflectionType : ReflectionObjectChildInfo<IReflectionType, IReflectionAssembly, TypeInfo>, IReflectionType
{
    public ReflectionType(IReflectionAssembly assembly, TypeSlim typeSlim) : base(assembly, typeSlim.CheckNotNull(nameof(typeSlim)).Type.GetTypeInfo())
    {
        TypeSlim = typeSlim;
        defaultValue = CreateLazy(DefaultValue_Build);
        genericParameters = CreateLazy(GenericParameters_Build);
        baseType = CreateLazy(Base_Build);
        bases = CreateLazy(Bases_Build);
        isRealType = CreateLazy(IsRealType_Build);

        constructors = CreateLazy(Constructors_Build);
        events = CreateLazy(Events_Build);
        fields = CreateLazy(Fields_Build);
        methods = CreateLazy(Methods_Build);
        properties = CreateLazy(Properties_Build);
    }


    private static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public Type Type => TypeSlim.Type;
    public TypeSlim TypeSlim { get; }
    public string NameFull => TypeSlim.NameFull;

    private readonly Lzy<bool> isRealType;
    public bool IsRealType => isRealType.Value;
    private bool IsRealType_Build() => Info.IsRealType();

    private readonly Lzy<IReflectionCollection<IReflectionType>> genericParameters;
    public IReflectionCollection<IReflectionType> GenericParameters => genericParameters.Value;
    private IReflectionCollection<IReflectionType> GenericParameters_Build() => GetGenericParameters(Info);

    public IReflectionType? Base => baseType.Value;
    private readonly Lzy<IReflectionType?> baseType;
    private IReflectionType? Base_Build() => GetType(Type.BaseType);

    private readonly Lzy<IReflectionCollection<IReflectionType>> bases;
    public IReflectionCollection<IReflectionType> Bases => bases.Value;
    private IReflectionCollection<IReflectionType> Bases_Build() => this.Bases_Build(Domain);

    private readonly Lzy<object> defaultValue;
    public object DefaultValue => defaultValue.Value;
    private object DefaultValue_Build() => Type.GetDefaultValue();

    #region Members

    public IReflectionCollection<IReflectionConstructor> Constructors => constructors.Value;
    private readonly Lzy<IReflectionCollection<IReflectionConstructor>> constructors;
    private IReflectionCollection<IReflectionConstructor> Constructors_Build() => Type.GetConstructors(flags).Select(o => Domain.CreateConstructor(this, o)).ToReflectionCollection(Domain);

    public IReflectionCollection<IReflectionEvent> Events => events.Value;
    private readonly Lzy<IReflectionCollection<IReflectionEvent>> events;
    private IReflectionCollection<IReflectionEvent> Events_Build() => Type.GetEvents(flags).Select(o => Domain.CreateEvent(this, o)).ToReflectionCollection(Domain);

    public IReflectionCollection<IReflectionField> Fields => fields.Value;
    private readonly Lzy<IReflectionCollection<IReflectionField>> fields;
    private IReflectionCollection<IReflectionField> Fields_Build() => Type.GetFields(flags).Select(o => Domain.CreateField(this, o)).ToReflectionCollection(Domain);

    public IReflectionCollection<IReflectionMethod> Methods => methods.Value;
    private readonly Lzy<IReflectionCollection<IReflectionMethod>> methods;
    private IReflectionCollection<IReflectionMethod> Methods_Build() => Type.GetMethods(flags).Select(o => Domain.CreateMethod(this, o)).ToReflectionCollection(Domain);

    public IReflectionCollection<IReflectionProperty> Properties => properties.Value;
    private readonly Lzy<IReflectionCollection<IReflectionProperty>> properties;
    private IReflectionCollection<IReflectionProperty> Properties_Build() => Type.GetProperties(flags).Select(o => Domain.CreateProperty(this, o)).ToReflectionCollection(Domain);

    #endregion

    #region Overrides

    protected override string Name_Build() => Type.NameFormatted().TrimOrNull() ?? Type.Name.TrimOrNull();
    protected override string ToString_Build() => $"[{Parent.Name}] {NameFull}";

    protected override int GetHashCode_Build() => TypeSlim.GetHashCode();
    protected override bool Equals_Internal(IReflectionType other) => TypeSlim.Equals(other.TypeSlim);
    protected override int CompareTo_Internal(IReflectionType other) => TypeSlim.CompareTo(other.TypeSlim);

    #endregion Overrides
}

public static class ReflectionTypeExtensions
{
    public static readonly ImmutableArray<string> IsTypeRealType_Name_Prefixes = ImmutableArray.Create(
        "<>c__"
    );

    public static readonly ImmutableArray<string> IsTypeRealType_Name_Suffixes = ImmutableArray.Create(
        "<>c",
        "&",
        "[]"
    );

    public static bool IsRealType(this Type type)
    {
        if (type.IsGenericParameter) return false;
        if (type.IsAnonymous()) return false;
        if (type.IsCompilerGenerated()) return false;

        if (typeof(Delegate).IsAssignableFrom(type)) return false;

        var invalidPrefixes = new[] { "<>c__" };
        var invalidSuffixes = new[] { "<>c", "&", "[]" };

        var name = type.Name.TrimOrNull();
        if (name == null) return false;

        if (invalidPrefixes.Any(invalid => name.Equals(invalid) || name.StartsWith(invalid))) return false;
        if (invalidSuffixes.Any(invalid => name.Equals(invalid) || name.EndsWith(invalid))) return false;

        if (type.IsArray)
        {
            var arrayType = type.GetElementType();
            if (arrayType == null) return false;
            if (!IsRealType(arrayType)) return false;
        }

        return true;
    }

    #region Inheritence

    public static bool IsParentOf(this IReflectionType obj, IReflectionType other) => !obj.Equals(other) && other.Type.IsAssignableTo(obj.Type);

    public static bool IsChildOf(this IReflectionType obj, IReflectionType other) => !obj.Equals(other) && obj.Type.IsAssignableTo(other.Type);

    public static IEnumerable<IReflectionType> GetSubclasses(this IReflectionType obj, params AssemblySlim[] assemblies) => GetSubclasses(obj, assemblies.OrEmpty().Select(o => o.Assembly).ToArray());

    public static IEnumerable<IReflectionType> GetSubclasses(this IReflectionType obj, params Assembly[] assemblies) => GetSubclasses(obj, assemblies.OrEmpty().Select(obj.Domain.GetAssembly).ToArray());

    public static IEnumerable<IReflectionType> GetSubclasses(this IReflectionType obj, params IReflectionAssembly[] assemblies) => GetSubclasses(obj, assemblies.OrEmpty().SelectMany(o => o.Types));

    public static IEnumerable<IReflectionType> GetSubclasses(this IReflectionType obj, IEnumerable<IReflectionType> types)
    {
        if (!obj.Type.IsClass && !obj.Type.IsInterface) yield break;

        foreach (var type in types.OrEmpty())
        {
            if (obj.IsParentOf(type)) yield return type;
        }
    }

    public static bool IsAssignableTo(this IReflectionType obj, bool isExactType, IReflectionType other) => obj.Equals(other) || (!isExactType && obj.Type.IsAssignableTo(other.Type));

    public static bool IsAssignableToAny(this IReflectionType obj, bool isExactType, IEnumerable<IReflectionType> other) => other.Any(otherType => IsAssignableTo(obj, isExactType, otherType));

    #endregion Inheritence

    #region Collections

    public static bool IsAssignableTo<T>(this IEnumerable<IReflectionType> obj, bool isExactType, IEnumerable<T> other, Func<T, IReflectionType> func) => IsAssignableTo(obj, isExactType, other.Select(func));

    public static bool IsAssignableTo(this IEnumerable<IReflectionType> obj, IReflectionDomain domain, bool isExactType, params Type[] other) => IsAssignableTo(obj.CheckNotNull(nameof(obj)), isExactType, domain.CheckNotNull(nameof(domain)).GetTypes(other.OrEmpty()));

    public static bool IsAssignableTo(this IEnumerable<IReflectionType> obj, IReflectionDomain domain, bool isExactType, IEnumerable<Type> other) => IsAssignableTo(obj.CheckNotNull(nameof(obj)), isExactType, domain.CheckNotNull(nameof(domain)).GetTypes(other.OrEmpty()));

    public static bool IsAssignableTo(this IEnumerable<IReflectionType> obj, bool isExactType, IEnumerable<IReflectionType> other)
    {
        var objList = obj.OrEmpty().ToList();
        var otherList = other.OrEmpty().ToList();
        if (objList.Count != otherList.Count) return false;

        for (var i = 0; i < objList.Count; i++)
        {
            var x = objList[i];
            var y = otherList[i];
            if (!x.IsAssignableTo(isExactType, y)) return false;
        }

        return true;
    }

    #endregion Collections
}
