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

public enum CommandManagerCommandState
{
    Created,
    ReadCompleted,
    ExecuteCompleted
}

public interface ICommandManager
{
    public ICommandEnvironment Environment { get; set; }

    void AddCommand(ICommand command);
}

public class CommandManager : ICommandManager
{
    private ICommandEnvironment environment;

    public ICommandEnvironment Environment
    {
        get => environment;
        set
        {
            var oldEnvironment = environment;
            environment = value;
            foreach (var command in Commands.Values)
            {
                if (command.Environment == null || command.Environment.Equals(oldEnvironment)) command.Environment = environment;
            }
        }
    }


    public Dictionary<string, ICommand> Commands { get; } = new(StringComparer.OrdinalIgnoreCase);

    public T AddCommand<T>() where T : class, ICommand, new() => (T)AddCommand(typeof(T));

    public ICommand AddCommand(Type type)
    {
        type.CheckNotNull(nameof(type));
        if (!type.IsAssignableTo(typeof(ICommand))) throw new ArgumentException($"{type.FullNameFormatted()} does not implement {nameof(ICommand)}", nameof(type));
        if (type.GetConstructor(Type.EmptyTypes) == null) throw new ArgumentException($"{type.FullNameFormatted()} does not have a no-arg constructor", nameof(type));

        var instance = (ICommand)Activator.CreateInstance(type);
        if (instance == null) throw new NullReferenceException($"Could not create {nameof(ICommand)} from {type.FullNameFormatted()}");
        instance.Environment = environment;

        AddCommand(instance);

        return instance;
    }

    public void AddCommand(ICommand instance)
    {
        instance.CheckNotNull(nameof(instance));
        var name = instance.CommandName.CheckNotNullTrimmed(instance.GetType().NameFormatted() + "." + nameof(instance.CommandName));
        if (Commands.ContainsKey(name)) throw new ArgumentException($"{nameof(ICommand)}={name} already exists");
        Commands.Add(name, instance);
    }

    public void Execute(string commandName, ICommandArgumentReader argumentReader, IValidationFailureCollection failureCollection = null, Action<IValidationFailureCollection> validationCallback = null)
    {
        commandName = commandName.CheckNotNullTrimmed(nameof(commandName));
        if (!Commands.TryGetValue(commandName, out var instance) || instance == null) throw new ArgumentException($"Command '{commandName}' does not exist", nameof(commandName));
        argumentReader.CheckNotNull(nameof(argumentReader));

        instance.Environment ??= Environment;
        if (instance.Environment == null) throw new InvalidOperationException($"Command '{commandName}' has no {nameof(ICommandEnvironment)} and neither does {GetType().NameFormatted()}");

        failureCollection ??= new ValidationFailureCollection();
        instance.Setup(argumentReader, failureCollection);
        if (validationCallback != null) validationCallback(failureCollection);

        instance.Execute();
    }
}
