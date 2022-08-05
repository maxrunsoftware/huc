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

public interface ICommand
{
    ICommandEnvironment Environment { get; set; }

    string CommandName { get; }

    void Build(ICommandBuilder b);

    void Setup(ICommandArgumentReader r, IValidationFailureCollection f) { }

    void Execute();
}

public abstract class Command : ICommand
{
    public virtual ICommandEnvironment Environment { get; set; }

    public virtual string CommandName => GetType().NameFormatted();

    public abstract void Build(ICommandBuilder b);

    public abstract void Setup(ICommandArgumentReader r, IValidationFailureCollection f);

    public abstract void Execute();
}
