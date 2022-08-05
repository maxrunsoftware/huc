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

namespace MaxRunSoftware.Utilities.CommandLine;

public interface IArgumentParser
{
    IArgumentParserResult Parse(IEnumerable<string> args);
}

public class ArgumentParser : IArgumentParser
{
    public ISet<char> OptionIdentifiers { get; } = new HashSet<char> { '-' };
    public ISet<char> OptionDelimiters { get; } = new HashSet<char> { '=' };

    public IArgumentParserResult Parse(IEnumerable<string> args)
    {
        var r = new ArgumentParserResult();
        var q = new Queue<string>(args.OrEmpty());

        ushort parameterCount = 0;
        while (q.Count > 0)
        {
            var a = q.Dequeue();
            var o = ParseOption(a);
            if (o != null) { r.OptionsList.Add(o); }
            else
            {
                var p = new ArgumentParameter(parameterCount, a);
                r.ParametersList.Add(p);
                parameterCount++;
            }
        }

        return r;
    }

    private ArgumentOption ParseOption(string arg)
    {
        if (arg == null) return null;
        var name = new StringBuilder(100);
        var value = new StringBuilder(500);

        const int parsingUnknown = 0;
        const int parsingName = 1;
        const int parsingValue = 2;
        var state = parsingUnknown;

        var q = new Queue<char>(arg.ToCharArray());
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (char.IsWhiteSpace(c) && state is parsingUnknown or parsingName) continue;

            if (state == parsingUnknown)
            {
                if (!OptionIdentifiers.Contains(c)) return null; // no starting dash

                state = parsingName;
                while (q.Count > 0) // remove any spaces or extra dashes before name
                {
                    c = q.Peek();
                    if (char.IsWhiteSpace(c) || OptionIdentifiers.Contains(c)) { q.Dequeue(); }
                    else { break; }
                }
            }
            else if (state == parsingName)
            {
                if (OptionDelimiters.Contains(c)) { state = parsingValue; }
                else { name.Append(c); }
            }
            else if (state == parsingValue) value.Append(c);
        }

        var n = name.ToString().TrimOrNull();
        if (n == null) return null;
        return new ArgumentOption(n, value.ToString());
    }
}

public interface IArgumentParserResult
{
    IReadOnlyList<ArgumentOption> Options { get; }
    IReadOnlyList<ArgumentParameter> Parameters { get; }
}

public class ArgumentParserResult : IArgumentParserResult
{
    public IReadOnlyList<ArgumentOption> Options => OptionsList;
    public List<ArgumentOption> OptionsList { get; } = new();
    public IReadOnlyList<ArgumentParameter> Parameters => ParametersList;
    public List<ArgumentParameter> ParametersList { get; } = new();
}

public class ArgumentOption
{
    public string Name { get; }
    public string Value { get; }

    public ArgumentOption(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public override string ToString() => this.ToStringGenerated();
}

public class ArgumentParameter
{
    public ushort Index { get; }
    public string Value { get; }

    public ArgumentParameter(ushort index, string value)
    {
        Index = index;
        Value = value;
    }

    public override string ToString() => this.ToStringGenerated();
}
