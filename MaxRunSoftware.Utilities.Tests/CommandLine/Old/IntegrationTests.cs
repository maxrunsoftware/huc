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

public class IntegrationTests
{
    public class MyCommand : Command
    {
        /*
        private bool? someFlag1", "sf1", "Some Flag #1");
        private bool?>("someFlag2", "sf2", "Some Flag #2");
        private string>("someString", "ss", "Some String 1");
        private int>("someInt", "si", "Some Int");
        private ulong?>("someLong", "sl", "Some Long");
        private double>("someFloat", "sf", "Some Float");
        private decimal>("someDecimal1", "sd1", "Some Decimal 1");
        private decimal?>("someDecimal2", "sd2", "Some Decimal 2");
        private StringComparison>("scomparison", "sc1", "String Comparison");
        private DateTimeKind?>("dtkind", "dtk", "Date Time Kind");

        private DateTime>(0, "pdateTime", "Parameter Date Time");
        private FileSystemFile>(4, "pfile", "Parameter File");
        private List<FileSystemObject>("pfileOrDirectory", "Parameter Multiple File or Directory");
        private bool>(2, "pbool", "Parameter Bool");
        */
        public override void Build(ICommandBuilder b)
        {
            b.Summary("My Summary");
            b.Detail("My Details");
            b.Example("My Example 1");
            b.Example("My Example 2");

            b.Option<bool?>("SomeFlag1", "sf1", "Some Flag #1");
            b.Option<bool?>("SomeFlag2", "sf2", "Some Flag #2");
            b.Option<string>("SomeString", "ss", "Some String 1");
            b.Option<int>("SomeInt", "si", "Some Int");
            b.Option<ulong?>("SomeLong", "sl", "Some Long");
            b.Option<double>("SomeFloat", "sf", "Some Float");
            b.Option<decimal>("SomeDecimal1", "sd1", "Some Decimal 1");
            b.Option<decimal?>("SomeDecimal2", "sd2", "Some Decimal 2");
            b.Option<StringComparison>("SComparison", "sc1", "String Comparison");
            b.Option<DateTimeKind?>("DTKind", "dtk", "Date Time Kind");

            b.Parameter<DateTime>(0, "PDateTime", "Parameter Date Time");
            b.Parameter<FileSystemFile>(4, "PFile", "Parameter File");
            b.ParameterMultiple<FileSystemObject>("PFileOrDirectory", "Parameter Multiple File or Directory");
            b.Parameter<bool>(2, "PBool", "Parameter Bool");
        }

        public override void Setup(ICommandArgumentReader r, IValidationFailureCollection f) { }


        public override void Execute() => throw new NotImplementedException();
    }

    [TestFact]
    public void Full_Test()
    {
        var cm = new CommandManager();
        Assert.Empty(cm.Commands);
        var cmd = cm.AddCommand<MyCommand>();
        Assert.NotNull(cmd);
    }
}
