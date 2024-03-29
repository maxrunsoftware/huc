﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MaxRunSoftware.Utilities.Console;

public class Args
{
    public string Command { get; }
    public IReadOnlyList<string> Values { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
    public bool IsDebug { get; }
    public bool IsTrace { get; }
    public bool IsNoBanner { get; }
    public bool IsHelp { get; }
    public bool IsShowHidden { get; }
    public bool IsVersion { get; }
    public bool IsExpandFileArgs { get; }
    public bool IsExpandFileArgsNoTrim { get; }

    public string[] ArgsString => args.Copy();
    private readonly string[] args;

    public Args(params string[] args)
    {
        this.args = args;
        var list = new Queue<string>(args);
        var values = new List<string>();
        var d = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (list.Count > 0)
        {
            var arg = list.Dequeue();
            var argTrimmed = arg.TrimOrNull();
            if (argTrimmed == null) continue;

            if (argTrimmed.StartsWith("-"))
            {
                while (arg.Length > 0)
                {
                    var c = arg[0];
                    if (char.IsWhiteSpace(c) || c == '-')
                    {
                        arg = arg.Length == 1 ? string.Empty : arg[1..];
                        continue;
                    }

                    break;
                }

                if (arg.TrimOrNull() == null) continue; // nothing left

                if (arg.IndexOf("=", StringComparison.Ordinal) == 0) continue; // no key supplied

                if (arg.IndexOf("=", StringComparison.Ordinal) > 0)
                {
                    var parts = arg.Split('=', 2);
                    var key = parts[0].TrimOrNull();
                    var val = parts[1];
                    if (key == null) continue; // no key supplied

                    if (val.TrimOrNull() == null) continue; // no value supplied

                    d[key] = val;
                }

                if (arg.IndexOf("=", StringComparison.Ordinal) < 0) d[arg] = "true";
            }
            else
            {
                if (arg.EqualsCaseInsensitive("HELP")) { IsHelp = true; }
                else if (Command == null) { Command = arg; }
                else { values.Add(arg); }
            }
        }

        IsDebug = d.Remove("DEBUG");
        IsTrace = d.Remove("TRACE");
        // ReSharper disable StringLiteralTypo
        IsNoBanner = d.Remove("NOBANNER");
        IsShowHidden = d.Remove("SHOWHIDDEN");
        if (!IsHelp) IsHelp = d.Remove("HELP");

        IsVersion = d.Remove("VERSION");
        IsExpandFileArgs = d.Remove("EXPANDFILEARGS");
        IsExpandFileArgsNoTrim = d.Remove("EXPANDFILEARGSNOTRIM");
        // ReSharper restore StringLiteralTypo

        if (IsExpandFileArgs)
        {
            values = values.Select(ParseParameterFile).ToList();
            foreach (var key in d.Keys.ToArray()) d[key] = ParseParameterFile(d[key]);
        }

        Values = values;
        Parameters = d;
    }

    private string ParseParameterFile(string str)
    {
        if (str == null) return null;

        if (!str.Contains("F{")) return str;

        if (!str.Contains("}")) return str;

        var stack = new Stack<StringBuilder>();
        stack.Push(new StringBuilder());

        for (var i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (c == 'F' && str.GetAtIndexOrDefault(i + 1) == '{')
            {
                i++;
                stack.Push(new StringBuilder());
            }
            else if (c == '}' && stack.Count > 1)
            {
                var filename = stack.Pop().ToString().TrimOrNull();
                if (filename != null)
                {
                    if (!File.Exists(filename)) throw new FileNotFoundException("File not found " + filename, filename);

                    var fileData = Util.FileRead(filename, Constant.ENCODING_UTF8);
                    if (!IsExpandFileArgsNoTrim) fileData = fileData.TrimOrNull();

                    fileData = fileData ?? "";
                    stack.Peek().Append(fileData);
                }
            }
            else { stack.Peek().Append(c); }
        }

        return stack.ToListReversed().Select(o => o.ToString()).ToStringDelimited("");
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Args [ Command=" + Command);
        foreach (var kvp in Parameters) sb.AppendLine("  " + kvp.Key + "=" + kvp.Value);

        for (var i = 0; i < Values.Count; i++) sb.AppendLine("  [" + (i + 1) + "] " + Values[i]);

        sb.AppendLine("]");

        return sb.ToString();
    }


    public string GetParameter(params string[] keys)
    {
        foreach (var key in keys.OrEmpty().TrimOrNull().WhereNotNull())
        {
            if (Parameters.TryGetValue(key, out var v))
            {
                if (v != null) { return v; }
            }
        }

        return null;
    }
}

public class ArgsException : Exception
{
    public ArgsException(string argName, string message) : base(argName + ": " + message) { }

    public static ArgsException ValueNotSpecified(string argName) => new(argName, "Arg <" + argName + "> not specified");
}
