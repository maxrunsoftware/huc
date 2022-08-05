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

public interface IReflectionProperty :
    IReflectionObjectTypeMember<IReflectionProperty, PropertyInfo>,
    IReflectionBase<IReflectionProperty>,
    IReflectionGetSet { }

public sealed class ReflectionProperty : ReflectionObjectTypeMember<IReflectionProperty, PropertyInfo>, IReflectionProperty
{
    public ReflectionProperty(IReflectionType parent, PropertyInfo info) : base(parent, info)
    {
        getValue = CreateLazy(GetValue_Build);
        setValue = CreateLazy(SetValue_Build);
        type = CreateLazy(Type_Build);
        baseProperty = CreateLazy(Base_Build);
        bases = CreateLazy(Bases_Build);
    }

    public IReflectionType Type => type.Value;
    private readonly Lzy<IReflectionType> type;
    private IReflectionType Type_Build() => GetType(Info.PropertyType);

    public bool CanGet => Info.CanRead;
    public bool CanSet => Info.CanWrite;

    public object GetValue(object instance) => getValue.Value(instance);
    private readonly Lzy<Func<object, object>> getValue;
    private Func<object, object> GetValue_Build() => CanGet ? Info.CreatePropertyGetter() : throw new Exception($"Cannot get property {this}");

    public void SetValue(object instance, object value) => setValue.Value(instance, value);
    private readonly Lzy<Action<object, object>> setValue;
    private Action<object, object> SetValue_Build() => CanSet ? Info.CreatePropertySetter() : throw new Exception($"Cannot set property {this}");

    public IReflectionProperty? Base => baseProperty.Value;
    private readonly Lzy<IReflectionProperty?> baseProperty;
    private IReflectionProperty? Base_Build()
    {
        if (DeclarationFlags.IsNewShadowName()) return null;
        if (DeclarationFlags.IsNewShadowSignature()) return null;
        if (this.IsAbstract()) return null; // TODO: Double check this isn't possible
        if (!this.IsOverride()) return null;

        var currentType = Parent.Base;
        while (currentType != null)
        {
            var items = currentType.Properties.Named(Name).Where(o => o.Type.Equals(Type)).ToList();
            if (items.Count > 0)
            {
                if (items.Count == 1) return items[0];
                if (items.Count > 1) throw new Exception($"Found multiple matching parent Properties for {this} in parent class {currentType}: " + items.OrderBy(o => o).Select(o => o.ToString()).ToStringDelimited(" | "));
            }

            currentType = currentType.Base;
        }

        return null;
    }

    private readonly Lzy<IReflectionCollection<IReflectionProperty>> bases;
    public IReflectionCollection<IReflectionProperty> Bases => bases.Value;
    private IReflectionCollection<IReflectionProperty> Bases_Build() => this.Bases_Build(Domain);


    #region Overrides

    protected override string ToString_Build() => Type.Name + " " + Parent.NameFull + "." + Info.Name + " { " + (CanGet ? "get; " : "") + (CanSet ? "set; " : "") + "}";

    protected override int GetHashCode_Build() => Hash(
        base.GetHashCode_Build(),
        Type,
        CanGet,
        CanSet
    );

    protected override bool Equals_Internal(IReflectionProperty other) =>
        base.Equals_Internal(other) &&
        IsEqual(Type, other.Type) &&
        IsEqual(CanGet, other.CanGet) &&
        IsEqual(CanSet, other.CanSet);

    protected override int CompareTo_Internal(IReflectionProperty other)
    {
        int c;
        if (0 != (c = base.CompareTo_Internal(other))) return c;
        if (0 != (c = Compare(Type, other.Type))) return c;
        if (0 != (c = Compare(CanGet, other.CanGet))) return c;
        if (0 != (c = Compare(CanSet, other.CanSet))) return c;
        return c;
    }

    #endregion Overrides
}

public static class ReflectionPropertyExtensions
{
    private static IReflectionProperty GetReflectionProperty(object obj, string propertyName) => obj.CheckNotNull(nameof(obj)).GetType().GetReflectionProperty(propertyName);

    public static IReflectionProperty GetReflectionProperty(this Type type, string propertyName)
    {
        type.CheckNotNull(nameof(type));
        propertyName = propertyName.CheckNotNullTrimmed(nameof(propertyName));

        // TODO: Throw a better exception for Property not found or multiple Properties found
        return Reflection.GetType(type).Properties.Named(propertyName).First();
    }

    public static object GetPropertyValue(this object obj, string propertyName) => GetReflectionProperty(obj, propertyName).GetValue(obj);

    public static void SetPropertyValue(this object obj, string propertyName, object value) => GetReflectionProperty(obj, propertyName).SetValue(obj, value);

    public static IReflectionCollection<IReflectionProperty> Type<T>(this IReflectionCollection<IReflectionProperty> obj, bool isExactType = true) => obj.ReturnType(o => o.Type, isExactType, typeof(T));
    public static IReflectionCollection<IReflectionProperty> Type(this IReflectionCollection<IReflectionProperty> obj, Type type, bool isExactType = true) => obj.ReturnType(o => o.Type, isExactType, type);
}
