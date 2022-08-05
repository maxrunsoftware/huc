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

public interface ICommandBuilderArgument
{
    ICommandBuilder Builder { get; }
    Argument Argument { get; }

    ICommandBuilderArgument Default(object defaultValue)
    {
        Argument.DefaultValue = defaultValue;
        return this;
    }

    ICommandBuilderArgument Required()
    {
        Argument.IsRequired = true;
        return this;
    }

    ICommandBuilderArgument CombineAllValueSources()
    {
        Argument.AllSourcesIncluded = true;
        return this;
    }

    ICommandBuilderArgument FileOrDirectoryMustExist()
    {
        Argument.FileOrDirectoryMustExist = true;
        return this;
    }

    ICommandBuilderArgument AllowWildcard()
    {
        Argument.AllowWildcard = true;
        return this;
    }

    ICommandBuilderArgument AllowMultipleParameters()
    {
        Argument.IsParameterMultiple = true;
        return this;
    }

    ICommandBuilderArgument MinRequired(ushort minRequired)
    {
        Argument.ParameterMinRequired = minRequired;
        return this;
    }

    ICommandBuilderArgument MinValue(object minValue)
    {
        Argument.MinValue = minValue;
        return this;
    }

    ICommandBuilderArgument MaxValue(object maxValue)
    {
        Argument.MaxValue = maxValue;
        return this;
    }
}

public class CommandBuilderArgument : CommandObject, ICommandBuilderArgument
{
    public ICommandBuilder Builder { get; }
    public Argument Argument { get; }

    public CommandBuilderArgument(ICommandBuilder builder)
    {
        Builder = builder.CheckNotNull(nameof(builder));
        Command = builder.Command;
        Argument = new Argument(Command, this);
    }

    public override void Clean()
    {
        base.Clean();
        Argument.Clean();
    }

    public override void Validate(IValidationFailureCollection failures)
    {
        base.Validate(failures);
        Argument.Validate(failures);
    }
}
