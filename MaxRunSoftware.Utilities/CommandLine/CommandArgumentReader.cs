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

public interface ICommandArgumentReader : IValidatable
{
    public T GetOption<T>(string name);
}

public class CommandArgumentReader : CommandObject, ICommandArgumentReader
{
    public List<Argument> Arguments { get; } = new();

    public IReadOnlyList<CommandArgumentValueCollection> ArgumentValues => argumentValues;
    private readonly List<CommandArgumentValueCollection> argumentValues = new();

    public CommandArgumentReader(ICommand command) { Command = command.CheckNotNull(nameof(command)); }

    public void Read(ICommandArgumentReaderSource source)
    {
        source.CheckNotNull(nameof(source));
        foreach (var argument in Arguments.OrEmpty().WhereNotNull())
        {
            var vc = GetValueCollection(argument);
            vc.Read(source);
        }
    }

    private CommandArgumentValueCollection GetValueCollection(Argument argument)
    {
        foreach (var argumentValue in argumentValues)
        {
            if (argument.ArgumentType == CommandArgumentType.Option)
            {
                if (argument.Name.EqualsCaseSensitive(argumentValue.Argument.Name)) return argumentValue;
            }
            else if (argument.ArgumentType == CommandArgumentType.Parameter)
            {
                if (argument.Index == argumentValue.Argument.Index) return argumentValue;
            }
            else throw new NotImplementedException();
        }

        var argumentValueCollection = new CommandArgumentValueCollection(argument) { Command = Command.CheckNotNull(nameof(Command)) };
        argumentValues.Add(argumentValueCollection);
        return argumentValueCollection;
    }

    public override void Validate(IValidationFailureCollection validationObject)
    {
        foreach (var value in ArgumentValues) value.Validate(validationObject);

        throw new NotImplementedException(); // TODO: Implement additional validation like file checking and required arguments
    }


    public T GetOption<T>(string name) => throw new NotImplementedException();
}
