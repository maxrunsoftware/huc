/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

namespace MaxRunSoftware.Utilities;

public class MissingAttributeException : InvalidOperationException
{
    public Type Attribute { get; }
    public Type Class { get; }
    public string MemberName { get; }
    public MemberTypes MemberType { get; }

    #region Constructors

    public MissingAttributeException() { }

    public MissingAttributeException(string message) : base(message) { }

    public MissingAttributeException(string message, Exception innerException) : base(message, innerException) { }

    public MissingAttributeException(string message, Type attribute, Type clazz, string memberName, MemberTypes memberType) : base(message)
    {
        Attribute = attribute;
        Class = clazz;
        MemberName = memberName;
        MemberType = memberType;
    }

    #endregion Constructors

    public static MissingAttributeException ClassMissingAttribute<TAttribute>(Type clazz) where TAttribute : Attribute => ClassMissingAttribute(clazz, typeof(TAttribute));

    public static MissingAttributeException ClassMissingAttribute(Type clazz, Type attribute) => GenerateMissingAttributeException(attribute, clazz, null, MemberTypes.TypeInfo);

    public static MissingAttributeException FieldMissingAttribute<TAttribute>(Type clazz, string fieldName) where TAttribute : Attribute => FieldMissingAttribute(clazz, typeof(TAttribute), fieldName);

    public static MissingAttributeException FieldMissingAttribute(Type clazz, Type attribute, string fieldName) => GenerateMissingAttributeException(attribute, clazz, fieldName, MemberTypes.Field);

    public static MissingAttributeException PropertyMissingAttribute<TAttribute>(Type clazz, string propertyName) where TAttribute : Attribute => PropertyMissingAttribute(clazz, typeof(TAttribute), propertyName);

    public static MissingAttributeException PropertyMissingAttribute(Type clazz, Type attribute, string propertyName) => GenerateMissingAttributeException(attribute, clazz, propertyName, MemberTypes.Property);

    public static MissingAttributeException MethodMissingAttribute<TAttribute>(Type clazz, string methodName) where TAttribute : Attribute => MethodMissingAttribute(clazz, typeof(TAttribute), methodName);

    public static MissingAttributeException MethodMissingAttribute(Type clazz, Type attribute, string methodName) => GenerateMissingAttributeException(attribute, clazz, methodName, MemberTypes.Method);

    public static MissingAttributeException ConstructorMissingAttribute<TAttribute>(Type clazz, string constructorName) where TAttribute : Attribute => ConstructorMissingAttribute(clazz, typeof(TAttribute), constructorName);

    public static MissingAttributeException ConstructorMissingAttribute(Type clazz, Type attribute, string constructorName) => GenerateMissingAttributeException(attribute, clazz, constructorName, MemberTypes.Constructor);

    public static MissingAttributeException GenerateMissingAttributeException(Type attribute, Type clazz, string memberName, MemberTypes memberType)
    {
        var memberTypeString = memberType.ToString();
        if (memberType == MemberTypes.TypeInfo) memberTypeString = "Class";

        var sb = new StringBuilder();
        sb.Append($"{memberTypeString} {clazz.FullNameFormatted()}");
        if (memberName != null) sb.Append($".{memberName}");
        sb.Append($" does not define required attribute {attribute.FullNameFormatted()}");
        var msg = sb.ToString();

        return new MissingAttributeException(msg, attribute, clazz, memberName, memberType);
    }

}
