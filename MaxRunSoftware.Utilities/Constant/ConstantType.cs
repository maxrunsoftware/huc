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

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    public static readonly ImmutableHashSet<Type> Types_Numeric = CreateHashSet(
        typeof(sbyte), typeof(byte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal)
    );

    public static readonly ImmutableHashSet<Type> Types_Decimal = CreateHashSet(
        typeof(float),
        typeof(double),
        typeof(decimal)
    );

    public static readonly ImmutableDictionary<Type, string> Type_PrimitiveAlias = CreateDictionary(
        // ReSharper disable BuiltInTypeReferenceStyle
        (typeof(Boolean), "bool"),
        (typeof(SByte), "sbyte"), (typeof(Byte), "byte"),
        (typeof(Char), "char"), (typeof(Int16), "short"), (typeof(UInt16), "ushort"),
        (typeof(Int32), "int"), (typeof(UInt32), "uint"),
        (typeof(Int64), "long"), (typeof(UInt64), "ulong"),
        (typeof(Single), "float"), (typeof(Double), "double"), (typeof(Decimal), "decimal"),
        (typeof(Object), "object"), (typeof(String), "string"),
        (typeof(void), "void")
        // ReSharper restore BuiltInTypeReferenceStyle
    );

    public static readonly ImmutableDictionary<string, Type> PrimitiveAlias_Type = CreateDictionary(Type_PrimitiveAlias.Select(o => (o.Value, o.Key)).ToArray());


    public const BindingFlags BindingFlag_Lookup_Default = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public; // https://referencesource.microsoft.com/#mscorlib/system/type.cs,1868

    public const BindingFlags BindingFlag_Lookup_DeclaredOnly = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly; // https://referencesource.microsoft.com/#mscorlib/system/type.cs,1869
}
