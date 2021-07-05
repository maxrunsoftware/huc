/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

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
using System.Collections.Generic;
using System.Dynamic;

namespace MaxRunSoftware.Utilities.Console
{
    public static class Extensions
    {
        public static void Debug<T>(this ILogger log, IEnumerable<T> enumerable, string name)
        {
            if (enumerable == null) return;
            var list = new List<T>(enumerable);
            for (int i = 0; i < list.Count; i++)
            {
                log.Debug(name + "[" + i + "]: " + list[i]);
            }
        }

        public static void DebugParameter(this ILogger log, string parameterName, object parameterValue)
        {
            log.Debug(parameterName + ": " + parameterValue);
        }

        public static string CheckValueNotNull(this string val, string valName, ILogger log)
        {
            log.DebugParameter(valName, val);
            if (val == null) throw ArgsException.ValueNotSpecified(valName);
            return val;
        }


    }
}
