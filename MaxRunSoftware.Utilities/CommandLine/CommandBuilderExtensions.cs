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

public static class CommandBuilderExtensions
{
    public static CommandSummary Summary(this ICommandBuilder builder, CommandSummary commandSummary)
    {
        builder.AddSummary(commandSummary);
        return commandSummary;
    }

    public static CommandSummary Summary(this ICommandBuilder builder, string summary) => builder.Summary(new CommandSummary { Text = summary });

    public static CommandDetailInfo Detail(this ICommandBuilder builder, CommandDetailInfo commandDetail)
    {
        builder.AddDetail(commandDetail);
        return commandDetail;
    }

    public static CommandDetailInfo Detail(this ICommandBuilder builder, string detail) => builder.Detail(new CommandDetailInfo { Text = detail });

    public static CommandExample Example(this ICommandBuilder builder, CommandExample commandExample)
    {
        builder.AddExample(commandExample);
        return commandExample;
    }

    public static CommandExample Example(this ICommandBuilder builder, string example) => builder.Example(new CommandExample { Text = example });

    public static ICommandBuilderArgument Option<T>(this ICommandBuilder builder, string name, string shortName, string description)
    {
        var a = AddArgument<T>(builder, name, shortName, description, null);
        a.Argument.ArgumentType = CommandArgumentType.Option;
        return a;
    }

    public static ICommandBuilderArgument Parameter<T>(this ICommandBuilder builder, ushort index, string name, string description)
    {
        var a = AddArgument<T>(builder, name, null, description, index);
        a.Argument.ArgumentType = CommandArgumentType.Parameter;
        return a;
    }

    public static ICommandBuilderArgument ParameterMultiple<T>(this ICommandBuilder builder, string name, string description)
    {
        var a = AddArgument<T>(builder, name, null, description, ushort.MaxValue);
        a.Argument.ArgumentType = CommandArgumentType.Parameter;
        a.Argument.IsParameterMultiple = true;
        return a;
    }

    private static ICommandBuilderArgument AddArgument<T>(ICommandBuilder builder, string name, string shortName, string description, ushort? index)
    {
        var a = builder.AddArgument();
        var aa = a.Argument;
        aa.Name = name;
        aa.ShortName = shortName;
        aa.Description = description;
        aa.Index = index;

        // https://stackoverflow.com/a/72797084
        var type = typeof(T);

        if (type.IsNullable(out type))
        {
            aa.IsRequired = false;
            aa.ParameterMinRequired = 0;
        }
        else if (type.IsValueType)
        {
            aa.IsRequired = true;
            aa.ParameterMinRequired = 1;
        }
        else // class
        {
            aa.IsRequired = false;
            aa.ParameterMinRequired = 0;
        }

        aa.ParserType = Argument.GetParserType(type);

        return a;
    }
}
