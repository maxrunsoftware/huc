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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MaxRunSoftware.Utilities
{
    public delegate object TypeConverter(object inputObject, Type outputType);

    public static class TypeConverterExtensions
    {
        public static TypeConverter AsTypeConverter<TInput, TOutput>(this Converter<TInput, TOutput> converter) => (inputObject, outputType) => converter((TInput)inputObject);

        public static Converter<TInput, TOutput> AsConverter<TInput, TOutput>(this TypeConverter typeConverter) => inputObject => (TOutput)typeConverter(inputObject, typeof(TOutput));
    }

    /// <summary>
    /// Formatting of methods for debugging
    /// https://github.com/kellyelton/System.Reflection.ExtensionMethods/tree/master/System.Reflection.ExtensionMethods
    /// </summary>
    public static class MethodSignatureTools
    {
        public static string GetSignature(this MethodBase method, bool invokable)
        {
            var signatureBuilder = new StringBuilder();

            // Add our method accessors if it's not invokable
            if (!invokable)
            {
                signatureBuilder.Append(GetMethodAccessorSignature(method));
                signatureBuilder.Append(" ");
            }

            // Add method name
            signatureBuilder.Append(method.Name);

            // Add method generics
            if (method.IsGenericMethod)
            {
                signatureBuilder.Append(GetGenericSignature(method));
            }

            // Add method parameters
            signatureBuilder.Append(GetMethodArgumentsSignature(method, invokable));

            return signatureBuilder.ToString();
        }

        public static string GetMethodAccessorSignature(this MethodBase method)
        {
            string signature = null;

            if (method.IsAssembly)
            {
                signature = "internal ";

                if (method.IsFamily)
                    signature += "protected ";
            }
            else if (method.IsPublic)
            {
                signature = "public ";
            }
            else if (method.IsPrivate)
            {
                signature = "private ";
            }
            else if (method.IsFamily)
            {
                signature = "protected ";
            }

            if (method.IsStatic)
                signature += "static ";

            if (method is MethodInfo methodInfo)
            {
                signature += GetSignature(methodInfo.ReturnType);
            }

            return signature;
        }

        public static string GetGenericSignature(this MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.IsGenericMethod) throw new ArgumentException($"{method.Name} is not generic.");

            return BuildGenericSignature(method.GetGenericArguments());
        }

        public static string GetMethodArgumentsSignature(this MethodBase method, bool invokable)
        {
            var isExtensionMethod = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
            var methodParameters = method.GetParameters().AsEnumerable();

            // If this signature is designed to be invoked and it's an extension method
            if (isExtensionMethod && invokable)
            {
                // Skip the first argument
                methodParameters = methodParameters.Skip(1);
            }

            var methodParameterSignatures = methodParameters.Select(param =>
            {
                var signature = string.Empty;

                if (param.ParameterType.IsByRef)
                    signature = "ref ";
                else if (param.IsOut)
                    signature = "out ";
                else if (isExtensionMethod && param.Position == 0)
                    signature = "this ";

                if (!invokable)
                {
                    signature += GetSignature(param.ParameterType) + " ";
                }

                signature += param.Name;

                return signature;
            });

            var methodParameterString = "(" + string.Join(", ", methodParameterSignatures) + ")";

            return methodParameterString;
        }

        /// <summary>Get a fully qualified signature for <paramref name="type" /></summary>
        /// <param name="type">Type. May be generic or <see cref="Nullable{T}" /></param>
        /// <returns>Fully qualified signature</returns>
        public static string GetSignature(this Type type)
        {
            var isNullableType = type.IsNullable(out var underlyingNullableType);

            var signatureType = isNullableType
                ? underlyingNullableType
                : type;

            var isGenericType = signatureType.IsGeneric();

            var signature = GetQualifiedTypeName(signatureType);

            if (isGenericType)
            {
                // Add the generic arguments
                signature += BuildGenericSignature(signatureType.GetGenericArguments());
            }

            if (isNullableType)
            {
                signature += "?";
            }

            return signature;
        }

        /// <summary>
        /// Takes an <see cref="IEnumerable{T}" /> and creates a generic type signature (&lt;string,
        /// string&gt; for example)
        /// </summary>
        /// <param name="genericArgumentTypes"></param>
        /// <returns>Generic type signature like &lt;Type, ...&gt;</returns>
        public static string BuildGenericSignature(IEnumerable<Type> genericArgumentTypes)
        {
            var argumentSignatures = genericArgumentTypes.Select(GetSignature);

            return "<" + string.Join(", ", argumentSignatures) + ">";
        }

        /// <summary>
        /// Gets the fully qualified type name of <paramref name="type" />. This will use any
        /// keywords in place of types where possible (string instead of System.String for example)
        /// </summary>
        /// <param name="type"></param>
        /// <returns>The fully qualified name for <paramref name="type" /></returns>
        public static string GetQualifiedTypeName(Type type)
        {
            var n = type.Name;
            if (n != null)
            {
                switch (n.ToUpperInvariant())
                {
                    case "SYSTEM.BOOLEAN":
                    case "BOOLEAN":
                    case "BOOL":
                        return "bool";

                    case "SYSTEM.BYTE":
                    case "BYTE":
                        return "byte";

                    case "SYSTEM.SBYTE":
                    case "SBYTE":
                        return "sbyte";

                    case "SYSTEM.CHAR":
                    case "CHAR":
                        return "char";

                    case "SYSTEM.DECIMAL":
                    case "DECIMAL":
                        return "decimal";

                    case "SYSTEM.DOUBLE":
                    case "DOUBLE":
                        return "double";

                    case "SYSTEM.SINGLE":
                        return "float";

                    case "SYSTEM.INT32":
                    case "INT32":
                    case "SYSTEN.INT":
                    case "INT":
                        return "int";

                    case "SYSTEM.UINT32":
                    case "UINT32":
                    case "SYSTEM.UINT":
                    case "UINT":
                        return "uint";

                    case "SYSTEM.INT64":
                    case "INT64":
                    case "SYSTEM.LONG":
                    case "LONG":
                        return "long";

                    case "SYSTEM.UINT64":
                    case "UINT64":
                    case "SYSTEM.ULONG":
                    case "ULONG":
                        return "ulong";

                    case "SYSTEM.INT16":
                    case "INT16":
                    case "SYSTEM.SHORT":
                    case "SHORT":
                        return "short";

                    case "SYSTEM.UINT16":
                    case "UINT16":
                    case "SYSTEM.USHORT":
                    case "USHORT":
                        return "ushort";

                    case "SYSTEM.OBJECT":
                    case "OBJECT":
                        return "object";

                    case "SYSTEM.STRING":
                    case "STRING":
                        return "string";

                    case "SYSTEM.VOID":
                    case "VOID":
                        return "void";
                }
            }

            var signature = type.FullName.TrimOrNull() ?? type.Name;

            if (type.IsGeneric()) signature = RemoveGenericTypeNameArgumentCount(signature);
            return signature;
        }

        /// <summary>This removes the `{argumentcount} from a the signature of a generic type</summary>
        /// <param name="genericTypeSignature">Signature of a generic type</param>
        /// <returns><paramref name="genericTypeSignature" /> without any argument count</returns>
        public static string RemoveGenericTypeNameArgumentCount(string genericTypeSignature) => genericTypeSignature.Substring(0, genericTypeSignature.IndexOf('`'));
    }

    [System.Diagnostics.DebuggerStepThrough]
    public static partial class Extensions
    {
        /// <summary>
        /// Attempts to convert the DotNet type to a DbType
        /// </summary>
        /// <param name="type">The DotNet type</param>
        /// <returns>The DbType</returns>
        public static DbType GetDbType(this Type type) => Constant.MAP_Type_DbType.TryGetValue(type, out var dbType) ? dbType : DbType.String;

        /// <summary>
        /// Converts a DbType to a DotNet type
        /// </summary>
        /// <param name="dbType">The DbType</param>
        /// <returns>The DotNet type</returns>
        public static Type GetDotNetType(this DbType dbType) => Constant.MAP_DbType_Type.TryGetValue(dbType, out var type) ? type : typeof(string);

        /// <summary>
        /// Gets a typed service from a IServiceProvider
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <param name="serviceProvider">The service provider</param>
        /// <returns>The typed service</returns>
        public static T GetService<T>(this IServiceProvider serviceProvider) => (T)serviceProvider.GetService(typeof(T));

        /// <summary>
        /// Gets a typed value from a serialized object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializationInfo"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetValue<T>(this System.Runtime.Serialization.SerializationInfo serializationInfo, string name) => (T)serializationInfo.GetValue(name, typeof(T));

        #region IDisposable

        public static void DisposeSafely(this IDisposable disposable, Action<string, Exception> onErrorLog)
        {
            onErrorLog.CheckNotNull(nameof(onErrorLog));
            if (disposable == null) return;
            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                onErrorLog($"Error calling {disposable.GetType().FullNameFormatted()}.Dispose() : {e.Message}", e);
            }
        }

        private static readonly IBucketReadOnly<Type, Action<object>> closeSafelyCache = new BucketCacheThreadSafeCopyOnWrite<Type, Action<object>>(o => CloseSafelyCreate(o));

        private static Action<object> CloseSafelyCreate(Type type)
        {
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(o => o.GetParameters().Length == 0)
                .Where(o => o.Name.Equals("Close"))
                .FirstOrDefault();

            if (method == null) throw new Exception("Close() method not found on object " + type.FullNameFormatted());

            return Util.CreateAction(method);

        }
        public static void CloseSafely(this IDisposable objectWithCloseMethod, Action<string, Exception> onErrorLog)
        {
            if (objectWithCloseMethod == null) return;
            var type = objectWithCloseMethod.GetType();
            var closer = closeSafelyCache[type];

            try
            {
                closer(objectWithCloseMethod);
            }
            catch (Exception e)
            {
                onErrorLog($"Error calling {objectWithCloseMethod.GetType().FullNameFormatted()}.Close() : {e.Message}", e);
            }

        }

        #endregion IDisposable

        #region Regex

        public static string[] MatchAll(this Regex regex, string input)
        {
            var matchCollection = regex.Matches(input);
            var list = new List<string>(matchCollection.Count);
            foreach (Match match in matchCollection)
            {
                var val = match.Value;
                if (val != null) list.Add(val);
            }

            return list.ToArray();
        }

        public static string MatchFirst(this Regex regex, string input)
        {
            var matchCollection = regex.Matches(input);
            foreach (Match match in matchCollection)
            {
                var val = match.Value;
                if (val != null) return val;
            }

            return null;
        }

        #endregion Regex

        #region RoundAwayFromZero

        public static decimal Round(this decimal value, MidpointRounding rounding, int decimalPlaces) => Math.Round(value, decimalPlaces, rounding);

        public static double Round(this double value, MidpointRounding rounding, int decimalPlaces) => Math.Round(value, decimalPlaces, rounding);

        public static double Round(this float value, MidpointRounding rounding, int decimalPlaces) => Math.Round(value, decimalPlaces, rounding);

        #endregion RoundAwayFromZero

        #region Equals Floating Point

        public static bool Equals(this double left, double right, byte digitsOfPrecision) => Math.Abs(left - right) < Math.Pow(10d, ((-1d) * digitsOfPrecision));

        public static bool Equals(this float left, float right, byte digitsOfPrecision) => Math.Abs(left - right) < Math.Pow(10f, ((-1f) * digitsOfPrecision));

        #endregion Equals Floating Point

        #region Color

        public static string ToCSS(this Color color)
        {
            var a = (color.A * (100d / 255d) / 100d).ToString(MidpointRounding.AwayFromZero, 1);
            return $"rgba({color.R}, {color.G}, {color.B}, {a})";
        }

        public static Color Shift(this Color startColor, Color endColor, double percentShifted)
        {
            if (percentShifted > 1d) percentShifted = 1d;
            if (percentShifted < 0d) percentShifted = 0d;

            byte shiftPercent(byte start, byte end, double percent)
            {
                if (start == end) return start;

                var d = (double)start - end;
                d = d * percent;
                var i = int.Parse(d.ToString(MidpointRounding.AwayFromZero, 0));
                i = start - i;
                if (start < end && i > end) i = end;
                if (start > end && i < end) i = end;
                if (i > byte.MaxValue) i = byte.MaxValue;
                if (i < byte.MinValue) i = byte.MinValue;

                var by = (byte)i;
                return by;
            }

            int rStart = startColor.R;
            int rEnd = endColor.R;
            var r = shiftPercent(startColor.R, endColor.R, percentShifted);
            var g = shiftPercent(startColor.G, endColor.G, percentShifted);
            var b = shiftPercent(startColor.B, endColor.B, percentShifted);
            var a = shiftPercent(startColor.A, endColor.A, percentShifted);

            return Color.FromArgb(a, r, g, b);
        }

        #endregion Color

        #region Conversion

        public static T ToNotNull<T>(T? nullable, T ifNull) where T : struct => (nullable == null || !nullable.HasValue) ? ifNull : nullable.Value;

        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this Tuple<TKey, TValue> tuple) => new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);

        public static ILookup<TKey, TElement> ToLookup<TKey, TEnumerable, TElement>(this IDictionary<TKey, TEnumerable> dictionary) where TEnumerable : IEnumerable<TElement> => dictionary.SelectMany(p => p.Value, Tuple.Create).ToLookup(p => p.Item1.Key, p => p.Item2);

        public static Tuple<TKey, TValue> ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair) => Tuple.Create(keyValuePair.Key, keyValuePair.Value);

        #endregion Conversion

    }
}
