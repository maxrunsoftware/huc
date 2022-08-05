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

public class CommandArgumentValueCollection : CommandObject
{
    public Argument Argument { get; }
    public List<CommandArgumentValue> Values { get; } = new();
    public bool IsFound => Values.Any(o => o.IsFound);

    private readonly List<ICommandArgumentReaderSource> sourcesRead = new();

    public CommandArgumentValueCollection(Argument argument)
    {
        Argument = argument.CheckNotNull(nameof(argument));
        Command = Argument.Command;
    }

    public override void Validate(IValidationFailureCollection failures)
    {
        base.Validate(failures);

        foreach (var value in Values) value.Validate(failures);
    }

    public override void Clean()
    {
        base.Clean();
        Argument.Clean();
        foreach (var value in Values) value.Clean();
    }

    public void Read(ICommandArgumentReaderSource source)
    {
        source.CheckNotNull(nameof(source));

        foreach (var sourceRead in sourcesRead)
        {
            if (source.Equals(sourceRead)) throw new InvalidOperationException($"{Argument} has already been read from source {source}");
        }

        sourcesRead.Add(source);

        var isMultiple = Argument.IsParameterMultiple;

        if (Argument.ArgumentType == CommandArgumentType.Option)
        {
            var v = new CommandArgumentValue(Argument) { Command = Command.CheckNotNull(nameof(Command)) };
            Values.Add(v);
            v.Read(source);
        }
        else if (Argument.ArgumentType == CommandArgumentType.Parameter)
        {
            var index = Argument.Index;
            while (true)
            {
                var v = new CommandArgumentValue(Argument) { Command = Command.CheckNotNull(nameof(Command)) };
                Values.Add(v);
                v.Read(source, index);

                if (!isMultiple) break;
                if (!v.IsFound) break;
                index++;
            }
        }
        else throw new NotImplementedException($"{nameof(CommandArgumentValue)}.{nameof(Argument)}.{nameof(Argument.ArgumentType)}={Argument.ArgumentType} is not yet supported");
    }
}
