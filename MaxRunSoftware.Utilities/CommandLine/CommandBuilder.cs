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

public interface ICommandBuilder : IValidatable
{
    ICommand Command { get; }
    void AddSummary(CommandSummary commandSummary);
    void AddDetail(CommandDetailInfo commandDetail);
    void AddExample(CommandExample commandExample);
    ICommandBuilderArgument AddArgument();
}

public class CommandBuilder : CommandObject, ICommandBuilder
{
    public CommandBuilder(ICommand command)
    {
        Command = command.CheckNotNull(nameof(command));
    }
    public List<CommandSummary> Summaries { get; } = new();
    public List<CommandDetailInfo> Details { get; } = new();
    public List<CommandExample> Examples { get; } = new();
    public List<CommandBuilderArgument> BuilderArguments { get; } = new();

    public void AddSummary(CommandSummary commandSummary) => Summaries.Add(commandSummary.CheckNotNull(nameof(commandSummary)));

    public void AddDetail(CommandDetailInfo commandDetail) => Details.Add(commandDetail.CheckNotNull(nameof(commandDetail)));

    public void AddExample(CommandExample commandExample) => Examples.Add(commandExample.CheckNotNull(nameof(commandExample)));

    public ICommandBuilderArgument AddArgument()
    {
        var ba = new CommandBuilderArgument(this);
        BuilderArguments.Add(ba);
        return ba;
    }

    public override void Validate(IValidationFailureCollection failures)
    {
        base.Validate(failures);

        if (Summaries.Count < 1) failures.Add(this, $"Command {Command.CommandName} does not contain any {nameof(CommandSummary)} items");

        // Basic checks
        foreach (var o in BuilderArguments) o.Validate(failures);

        // Argument name uniqueness check
        var dArgumentsName = new Dictionary<string, List<Argument>>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in BuilderArguments.Select(o => o.Argument))
        {
            if (o.Name != null) dArgumentsName.AddToList(o.Name, o);
            if (o.ShortName != null && !o.ShortName.EqualsIgnoreCase(o.Name)) dArgumentsName.AddToList(o.ShortName, o);
        }

        foreach (var kvp in dArgumentsName)
        {
            if (kvp.Value.Count > 1) failures.Add(this, $"{this}.{nameof(BuilderArguments)} contains multiple ({kvp.Value.Count}) with {nameof(Argument.Name)}={kvp.Key}");
        }

        // Parameter index uniqueness check
        var dParametersIndex = new Dictionary<ushort, List<Argument>>();
        foreach (var o in BuilderArguments.Select(o => o.Argument).Where(o => o.ArgumentType == CommandArgumentType.Parameter))
        {
            o.Validate(failures);
            dParametersIndex.AddToList(o.ParameterIndex, o);
        }

        foreach (var kvp in dParametersIndex)
        {
            if (kvp.Value.Count > 1) failures.Add(this, $"{this}.{nameof(BuilderArguments)} contains multiple ({kvp.Value.Count}) with {nameof(Argument.Index)}={kvp.Key}");
        }
    }

    private class ParameterSortItem : IComparable<ParameterSortItem>
    {
        public CommandBuilderArgument BuilderArgument { get; }
        public Argument Parameter { get; }
        public int ListIndex { get; }
        public int Index { get; }
        public int IsMultiple { get; }

        public ParameterSortItem(CommandBuilderArgument builder, int listIndex)
        {
            BuilderArgument = builder.CheckNotNull(nameof(builder));
            Parameter = BuilderArgument.Argument.CheckNotNull(nameof(BuilderArgument) + "." + nameof(BuilderArgument.Argument));
            if (Parameter.ArgumentType != CommandArgumentType.Parameter) throw new ArgumentOutOfRangeException(nameof(Parameter), $"Expecting {nameof(Argument)}.{nameof(Argument.ArgumentType)}={CommandArgumentType.Parameter} but instead was {Parameter.ArgumentType}");
            ListIndex = listIndex;
            Index = Parameter.ParameterIndex;
            IsMultiple = Parameter.IsParameterMultiple ? 1 : 0;
        }

        public int CompareTo(ParameterSortItem other)
        {
            if (other == null) return 1;
            if (other == this) return 0;
            var c = IsMultiple - other.IsMultiple;
            if (c != 0) return c;
            c = Index - other.Index;
            if (c != 0) return c;
            c = ListIndex - other.ListIndex;
            if (c != 0) return c;

            return c;
        }
    }

    public override void Clean()
    {
        Summaries.RemoveAll(o => o == null);
        foreach (var o in Summaries) o.Clean();

        Summaries.RemoveAll(o => o.Text == null);


        Details.RemoveAll(o => o == null);
        foreach (var o in Details) o.Clean();

        Details.RemoveAll(o => o.Text == null);


        Examples.RemoveAll(o => o == null);
        foreach (var o in Examples) o.Clean();

        Examples.RemoveAll(o => o.Text == null);


        BuilderArguments.RemoveAll(o => o == null);
        foreach (var o in BuilderArguments) o.Clean();

        var parametersList = new List<ParameterSortItem>();
        var parametersIndex = 0;
        foreach (var o in BuilderArguments.Where(o => o.Argument.ArgumentType == CommandArgumentType.Parameter))
        {
            if (o.Argument.Index == null) o.Argument.Index = (ushort)parametersIndex;
            parametersList.Add(new ParameterSortItem(o, parametersIndex));
            parametersIndex++;
        }

        parametersList = parametersList.OrderBy(o => o).ToList();
        BuilderArguments.RemoveAll(o => o.Argument.ArgumentType == CommandArgumentType.Parameter);
        for (ushort i = 0; i < parametersList.Count; i++)
        {
            var p = parametersList[i];
            p.Parameter.Index = i;
            BuilderArguments.Add(p.BuilderArgument);
        }
    }
}
