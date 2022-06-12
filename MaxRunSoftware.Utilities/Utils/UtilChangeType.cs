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
    public static TOutput ChangeType<TInput, TOutput>(TInput obj)
    {
        return (TOutput)ChangeType(obj, typeof(TOutput));
    }

    public static TOutput ChangeType<TOutput>(object obj)
    {
        return (TOutput)ChangeType(obj, typeof(TOutput));
    }

    public static object ChangeType(object obj, Type outputType)
    {
        if (obj == null || obj == DBNull.Value)
        {
            if (!outputType.IsValueType)
            {
                return null;
            }

            if (outputType.IsNullable())
            {
                return null;
            }

            return Convert.ChangeType(obj, outputType); // Should throw exception
        }

        if (outputType.IsNullable(out var underlyingTypeOutput))
        {
            return ChangeType(obj, underlyingTypeOutput);
        }

        var inputType = obj.GetType();
        if (inputType.IsNullable(out var underlyingTypeInput))
        {
            inputType = underlyingTypeInput;
        }

        if (inputType == typeof(string))
        {
            var o = obj as string;
            if (outputType == typeof(bool))
            {
                return o.ToBool();
            }

            if (outputType == typeof(DateTime))
            {
                return o.ToDateTime();
            }

            if (outputType == typeof(Guid))
            {
                return o.ToGuid();
            }

            if (outputType == typeof(MailAddress))
            {
                return o.ToMailAddress();
            }

            if (outputType == typeof(Uri))
            {
                return o.ToUri();
            }

            if (outputType == typeof(IPAddress))
            {
                return o.ToIPAddress();
            }

            if (outputType.IsEnum)
            {
                return GetEnumItem(outputType, o);
            }
        }

        if (inputType.IsEnum)
        {
            return ChangeType(obj.ToString(), outputType);
        }

        if (outputType == typeof(string))
        {
            return obj.ToStringGuessFormat();
        }

        return Convert.ChangeType(obj, outputType);
    }
}
