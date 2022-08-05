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

public abstract class CommandObject : ICleanable
{
    public ICommand Command { get; init; }

    public virtual void Clean() { }

    public virtual void Validate(IValidationFailureCollection failures)
    {
        failures.CheckNotNull(nameof(failures));
        Clean();
        if (Command == null) failures.Add(this, $"{this}.{nameof(Command)} is null");
    }

    public override string ToString() => this.ToStringGenerated();
}

public abstract class CommandText : CommandObject
{
    public string Text { get; set; }

    public override void Clean()
    {
        base.Clean();
        Text = Text.TrimOrNull();
    }

    public override void Validate(IValidationFailureCollection failures)
    {
        base.Validate(failures);
        if (Text == null) failures.Add(this, $"{this}.{nameof(Text)} is null");
    }
}

public class CommandSummary : CommandText { }

public class CommandDetailInfo : CommandText { }

public class CommandExample : CommandText { }
