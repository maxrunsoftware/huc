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

public class CommandBuilderTests : TestBase
{
    public CommandBuilderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    private class MyCommand : Command
    {
        public override void Build(ICommandBuilder b) => throw new NotImplementedException();
        public override void Setup(ICommandArgumentReader r, IValidationFailureCollection f) => throw new NotImplementedException();
        public override void Execute() => throw new NotImplementedException();
    }

    [TestFact]
    public void Summary()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);
        cb.Summary("  Hello World  ");
        cb.Summary("   ");
        cb.Summary((string)null);

        Assert.Equal(3, cb.Summaries.Count);
        Assert.Equal("  Hello World  ", cb.Summaries[0].Text);
        Assert.Equal("   ", cb.Summaries[1].Text);
        Assert.Null(cb.Summaries[2].Text);

        cb.Clean();
        Assert.Single(cb.Summaries);
        Assert.Equal("Hello World", cb.Summaries[0].Text);
    }

    [TestFact]
    public void Example()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);
        cb.Example("  Hello World  ");
        cb.Example("   ");
        cb.Example((string)null);

        Assert.Equal(3, cb.Examples.Count);
        Assert.Equal("  Hello World  ", cb.Examples[0].Text);
        Assert.Equal("   ", cb.Examples[1].Text);
        Assert.Null(cb.Examples[2].Text);

        cb.Clean();
        Assert.Single(cb.Examples);
        Assert.Equal("Hello World", cb.Examples[0].Text);
    }

    [TestFact]
    public void Detail()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);
        cb.Detail("  Hello World  ");
        cb.Detail("   ");
        cb.Detail((string)null);

        Assert.Equal(3, cb.Details.Count);
        Assert.Equal("  Hello World  ", cb.Details[0].Text);
        Assert.Equal("   ", cb.Details[1].Text);
        Assert.Null(cb.Details[2].Text);

        cb.Clean();
        Assert.Single(cb.Details);
        Assert.Equal("Hello World", cb.Details[0].Text);
    }

    [TestFact]
    public void Option_Bool()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);

        cb.Option<bool>("myBoolName  ", "myBoolShort  ", "myBoolOptionDescription  ");
        Assert.Single(cb.BuilderArguments);
        var a = cb.BuilderArguments[0].Argument;
        Assert.Equal("myBoolName  ", a.Name);
        Assert.Equal("myBoolShort  ", a.ShortName);
        Assert.Equal("myBoolOptionDescription  ", a.Description);
        Assert.Equal(CommandArgumentType.Option, a.ArgumentType);
        Assert.Equal(CommandArgumentParserType.Bool, a.ParserType);

        cb.Clean();
        Assert.Equal("myBoolName", a.Name);
        Assert.Equal("myBoolShort", a.ShortName);
        Assert.Equal("myBoolOptionDescription", a.Description);

        Assert.True(a.IsRequired);
        Assert.Equal(1, a.ParameterMinRequired);
    }

    [TestFact]
    public void Option_Bool_Nullable()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);

        cb.Option<bool?>("myName", "myShortName", "myDescription");
        var a = cb.BuilderArguments[0].Argument;
        Assert.Equal(CommandArgumentParserType.Bool, a.ParserType);
        cb.Clean();
        Assert.False(a.IsRequired);
        Assert.Equal(0, a.ParameterMinRequired);
    }

    [TestFact]
    public void CommandBuilder_Command_IsSet()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);
        Assert.Equal(mc, cb.Command);
    }

    [TestFact]
    public void Parameter_Order_After_Clean()
    {
        var b = Pb();
        b.P<bool>(99, 99, 99);
        b.AssertIndexes(99, 99, 99);
        b.Clean();
        b.AssertIndexes(0, 1, 2);
    }

    [TestFact]
    public void Parameter_Order_After_Clean_With_Indexes()
    {
        var b = Pb();
        b.P<bool>(3, 7, 5);
        b.AssertIndexes(3, 7, 5);
        b.Clean();
        b.AssertIndexes(0, 2, 1);
    }


    [TestFact]
    public void Parameter_Order_After_Clean_Multiple_Always_Last()
    {
        var b = Pb();
        b.P<bool>(0, null, 1, 0, null, 0);
        b.BuilderArguments[3].Argument.IsParameterMultiple = true;

        b.AssertIndexes(0, null, 1, 0, null, 0);
        b.Clean();
        b.AssertIndexes(0, 2, 3, 5, 4, 1);
    }

    [TestFact]
    public void Parameter_Order_After_Clean_With_Indexes_Some_Without()
    {
        var b = Pb();
        b.P<bool>(0, null, 1, null, 0);
        b.AssertIndexes(0, null, 1, null, 0);
        b.Clean();
        b.AssertIndexes(0, 2, 3, 4, 1);
    }

    [TestFact]
    public void ValidationTests()
    {
        var cb = Cb();

        cb.Summary("My summary");
        cb.Example("My example");
        cb.Option<int>("myInt", "mi", "my int value");
        cb.Option<int?>("myIntNullable", "min", "my int nullable value");
        cb.Option<string>("myString", "ms", "my string value");

        var vc = new ValidationFailureCollection();
        cb.Validate(vc);
        Assert.Empty(vc.Failures);
    }


    private static CommandBuilder Cb()
    {
        var mc = new MyCommand();
        var cb = new CommandBuilder(mc);
        return cb;
    }

    private class ParameterBuilder : ICleanable
    {
        public CommandBuilder Builder { get; }
        public List<ICommandBuilderArgument> BuilderArguments { get; } = new();
        public ParameterBuilder() { Builder = Cb(); }

        public void P<T>(params ushort?[] indexes)
        {
            foreach (var index in indexes)
            {
                var itemIndex = BuilderArguments.Count;
                var cba = Builder.Parameter<T>(index ?? ushort.MaxValue, "n" + itemIndex, "d" + itemIndex);
                if (index == null) cba.Argument.Index = null;
                BuilderArguments.Add(cba);
            }
        }

        public void Clean() => Builder.Clean();

        public ICommandBuilderArgument this[int i] => BuilderArguments[i];

        public void AssertIndexes(params ushort?[] expectedIndexes)
        {
            for (var i = 0; i < expectedIndexes.Length; i++)
            {
                var expectedIndex = expectedIndexes[i];
                var ba = this[i];
                if (expectedIndex == null)
                    Assert.Null(ba.Argument.Index);
                else
                    Assert.Equal(expectedIndex, ba.Argument.ParameterIndex);
            }
        }
    }

    private static ParameterBuilder Pb() => new();
}
