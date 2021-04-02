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
using System.Text;

namespace HavokMultimedia.Utilities.Console
{
    public class Args
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Command { get; init; }
        public IReadOnlyList<string> Values { get; init; }
        public IReadOnlyDictionary<string, string> Parameters { get; init; }
        public bool IsDebug { get; init; }
        public bool IsNoBanner { get; init; }


        public Args(params string[] args)
        {

            var list = new Queue<string>(args);
            var values = new List<string>();
            var d = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (list.Count > 0)
            {
                var arg = list.Dequeue();
                if (arg.TrimOrNull() == null) continue;
                if (arg.TrimOrNull().StartsWith("-"))
                {
                    while (arg.Length > 0)
                    {
                        var c = arg[0];
                        if (char.IsWhiteSpace(c) || c == '-')
                        {
                            if (arg.Length == 1) arg = string.Empty;
                            else arg = arg.Substring(1);
                            continue;
                        }
                        break;
                    }
                    if (arg.TrimOrNull() == null) continue; // nothing left
                    if (arg.IndexOf("=") == 0) continue; // no key supplied
                    if (arg.IndexOf("=") > 0)
                    {
                        string[] parts = arg.Split('=', 2);
                        var key = parts[0].TrimOrNull();
                        var val = parts[1];
                        if (key == null) continue; // no key supplied
                        if (val.TrimOrNull() == null) continue; // no value supplied
                        d[key] = val;
                    }
                    if (arg.IndexOf("=") < 0)
                    {
                        var key = arg;
                        var val = "true";
                        if (key == null) continue; // no key supplied
                        d[key] = val;

                    }
                }
                else
                {
                    if (Command == null) Command = arg;
                    else values.Add(arg);
                }


            }

            Values = values;
            IsDebug = d.Remove("DEBUG");
            IsNoBanner = d.Remove("NOBANNER");
            Parameters = d;

        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Args [ Command=" + Command);
            foreach (var kvp in Parameters)
            {
                sb.AppendLine("  " + kvp.Key + "=" + kvp.Value);
            }
            for (int i = 0; i < Values.Count; i++)
            {
                sb.AppendLine("  [" + (i + 1) + "] " + Values[i]);
            }
            sb.AppendLine("]");

            return sb.ToString();
        }


        public string GetParameter(params string[] keys)
        {
            foreach (var key in keys.OrEmpty().TrimOrNull().WhereNotNull())
            {
                if (Parameters.TryGetValue(key, out var v))
                {
                    if (v != null) return v;
                }
            }
            return null;
        }






    }

    public class ArgsException : Exception
    {
        public ArgsException(string argName, string message) : base(argName + ": " + message)
        {

        }
    }
}
