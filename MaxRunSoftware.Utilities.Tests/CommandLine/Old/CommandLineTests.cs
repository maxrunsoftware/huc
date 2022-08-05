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

using MaxRunSoftware.Utilities.CommandLine;

namespace MaxRunSoftware.Utilities.Tests.CommandLine;

public class CommandTests : TestBase
{
    public CommandTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    public abstract class MyBaseCommand : Command { }

    public class MyTestCommand : MyBaseCommand
    {
        public override void Build(ICommandBuilder b) => throw new NotImplementedException();
        public override void Setup(ICommandArgumentReader r, IValidationFailureCollection f) => throw new NotImplementedException();
        public override void Execute() => throw new NotImplementedException();
    }

    public class MySubCmd : MyTestCommand { }

    [TestFact]
    public void CommandName()
    {
        var c3 = new MySubCmd();
        Assert.Equal(nameof(MySubCmd), c3.CommandName);
    }
}
