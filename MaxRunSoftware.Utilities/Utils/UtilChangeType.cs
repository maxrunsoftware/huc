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

using System.Net;
using System.Net.Mail;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    public static TOutput ChangeType<TInput, TOutput>(TInput obj) => (TOutput)ChangeType(obj, typeof(TOutput));

    public static TOutput ChangeType<TOutput>(object obj) => (TOutput)ChangeType(obj, typeof(TOutput));

    public static object? ChangeType(object? obj, Type outputType)
    {
        if (obj == null || obj == DBNull.Value)
        {
            if (!outputType.IsValueType) return null;

            if (outputType.IsNullable()) return null;

            return Convert.ChangeType(obj, outputType); // Should throw exception
        }

        if (outputType.IsNullable(out var underlyingTypeOutput)) return ChangeType(obj, underlyingTypeOutput);

        var inputType = obj.GetType();
        if (inputType.IsNullable(out var underlyingTypeInput)) inputType = underlyingTypeInput;

        if (inputType == typeof(string))
        {
            if (obj is not string str) throw new NullReferenceException(); // should not happen

            if (outputType == typeof(bool)) return str.ToBool();
            if (outputType == typeof(DateTime)) return str.ToDateTime();
            if (outputType == typeof(Guid)) return str.ToGuid();
            if (outputType == typeof(MailAddress)) return str.ToMailAddress();
            if (outputType == typeof(Uri)) return str.ToUri();
            if (outputType == typeof(IPAddress)) return str.ToIPAddress();
            if (outputType.IsEnum)
            {
                if (Enum.TryParse(outputType, str, false, out var outputEnum)) return outputEnum;
                return Enum.Parse(outputType, str, true);
                //return  outputType.GetEnumValue(str);
            }
        }

        if (inputType.IsEnum) return ChangeType(obj.ToString(), outputType);

        if (outputType == typeof(string)) return obj.ToStringGuessFormat();

        return Convert.ChangeType(obj, outputType);
    }
}
