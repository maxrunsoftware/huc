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

public interface ICommandArgumentReaderSource : IEquatable<ICommandArgumentReaderSource>
{
    bool TryGetOption(string name, out string value);
    bool TryGetParameter(ushort index, out string value);
}

public class CommandArgumentReaderSource : ICommandArgumentReaderSource
{
    private readonly ImmutableArray<ArgumentOption> options;
    private readonly ImmutableArray<ArgumentParameter> parameters;

    public CommandArgumentReaderSource(IEnumerable<ArgumentOption> options, IEnumerable<ArgumentParameter> parameters)
    {
        this.options = ImmutableArray.Create(options.OrEmpty().WhereNotNull().ToArray());
        this.parameters = ImmutableArray.Create(parameters.OrEmpty().WhereNotNull().ToArray());
    }

    public bool TryGetOption(string name, out string value)
    {
        name = name.CheckNotNullTrimmed(nameof(name));

        foreach (var option in options)
        {
            var n = option.Name.TrimOrNull();
            if (n == null) continue;
            if (name.EqualsIgnoreCase(n))
            {
                value = option.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool TryGetParameter(ushort index, out string value)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Index == index)
            {
                value = parameter.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool Equals(ICommandArgumentReaderSource other) => throw new NotImplementedException();
}
