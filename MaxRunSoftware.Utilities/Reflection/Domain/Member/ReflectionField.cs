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

public interface IReflectionField :
    IReflectionObjectTypeMember<IReflectionField, FieldInfo>,
    IReflectionGetSet { }

public sealed class ReflectionField : ReflectionObjectTypeMember<IReflectionField, FieldInfo>, IReflectionField
{
    public ReflectionField(IReflectionType parent, FieldInfo info) : base(parent, info)
    {
        getValue = CreateLazy(GetValue_Build);
        setValue = CreateLazy(SetValue_Build);
        type = CreateLazy(Type_Build);
    }

    public IReflectionType Type => type.Value;
    private readonly Lzy<IReflectionType> type;
    private IReflectionType Type_Build() => GetType(Info.FieldType);

    public bool CanGet => true;
    public bool CanSet => !Info.IsInitOnly;

    public object GetValue(object instance) => getValue.Value(instance);
    private readonly Lzy<Func<object, object>> getValue;
    private Func<object, object> GetValue_Build() => Info.CreateFieldGetter();

    public void SetValue(object instance, object value) => setValue.Value(instance, value);
    private readonly Lzy<Action<object, object>> setValue;
    private Action<object, object> SetValue_Build() => CanSet ? Info.CreateFieldSetter() : throw new Exception($"Cannot set field {this}");

    #region Overrides

    protected override string ToString_Build() => Type.Name + " " + Parent.NameFull + "." + Info.Name;

    protected override int GetHashCode_Build() => Hash(
        base.GetHashCode_Build(),
        Type,
        CanGet,
        CanSet
    );

    protected override bool Equals_Internal(IReflectionField other) =>
        base.Equals_Internal(other) &&
        IsEqual(Type, other.Type) &&
        IsEqual(CanGet, other.CanGet) &&
        IsEqual(CanSet, other.CanSet);

    protected override int CompareTo_Internal(IReflectionField other)
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

public static class ReflectionFieldExtensions
{
    public static IReflectionCollection<IReflectionField> Type<T>(this IReflectionCollection<IReflectionField> obj, bool isExactType = true) => obj.ReturnType(o => o.Type, isExactType, typeof(T));
    public static IReflectionCollection<IReflectionField> Type(this IReflectionCollection<IReflectionField> obj, Type type, bool isExactType = true) => obj.ReturnType(o => o.Type, isExactType, type);


    //private static ReflectionField GetReflectionField(object obj, string fieldName) => obj.CheckNotNull(nameof(obj)).GetType().GetReflectionField(fieldName);

    //public static ReflectionField GetReflectionField(this Type type, string fieldName) => Reflection.GetType(type.CheckNotNull(nameof(type))).Fields[fieldName.CheckNotNullTrimmed(nameof(fieldName))];

    //public static object GetFieldValue(this object obj, string fieldName) => GetReflectionField(obj, fieldName).GetValue(obj);

    //public static void SetFieldValue(this object obj, string fieldName, object value) => GetReflectionField(obj, fieldName).SetValue(obj, value);
}
