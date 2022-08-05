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

public class OptionTestsEnum : OptionTestsBase
{
    public enum MyEnum
    {
        One,
        Two,
        Three,
        Four,
        Five
    }

    public OptionTestsEnum(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }


    [Command(NAME)]
    public class Value_Command
    {
        [Option(DESCRIPTION)]
        public MyEnum Value { get; set; }
    }

    [Fact]
    public void Value()
    {
        var (o, r) = Scan<Value_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.True(opt.IsRequired);

        Assert.Equal(default, o.Value);
        Assert.Equal(MyEnum.One, o.Value);
        opt.SetValue(o, nameof(MyEnum.Three));
        Assert.Equal(MyEnum.Three, o.Value);

        Assert.ThrowsAny<Exception>(() => opt.SetValue(o, null));
    }


    [Command(NAME)]
    public class Value_Nullable_Command
    {
        [Option(DESCRIPTION)]
        public MyEnum? Value { get; set; }
    }

    [Fact]
    public void Value_Nullable()
    {
        var (o, r) = Scan<Value_Nullable_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.False(opt.IsRequired);

        Assert.Null(o.Value);
        opt.SetValue(o, nameof(MyEnum.Three));
        Assert.Equal(MyEnum.Three, o.Value);

        opt.SetValue(o, null);
        Assert.Null(o.Value);

        o = new Value_Nullable_Command();
        opt.SetValue(o, null);
        Assert.Null(o.Value);
    }


    [Command(NAME)]
    public class Value_Min_Command
    {
        [Option(DESCRIPTION, Min = MyEnum.Three)]
        public MyEnum Value { get; set; }
    }

    [Fact]
    public void Value_Min()
    {
        var (o, r) = Scan<Value_Min_Command>();
        var cdw = Assert.Single(r.CommandsInvalid)!;
        var opt = cdw.Options.Single();
        Assert.NotNull(opt.Exception);
    }


    [Command(NAME)]
    public class Value_Max_Command
    {
        [Option(DESCRIPTION, Max = MyEnum.Five)]
        public MyEnum Value { get; set; }
    }

    [Fact]
    public void Value_Max()
    {
        var (o, r) = Scan<Value_Min_Command>();
        var cdw = Assert.Single(r.CommandsInvalid)!;
        var opt = cdw.Options.Single();
        Assert.NotNull(opt.Exception);
    }


    [Command(NAME)]
    public class Value_Min_Max_Command
    {
        [Option(DESCRIPTION, Min = MyEnum.Three, Max = MyEnum.Five)]
        public MyEnum Value { get; set; }
    }

    [Fact]
    public void Value_Min_Max()
    {
        var (o, r) = Scan<Value_Min_Command>();
        var cdw = Assert.Single(r.CommandsInvalid)!;
        var opt = cdw.Options.Single();
        Assert.NotNull(opt.Exception);
    }


    [Command(NAME)]
    public class Validate_Default_Command
    {
        [Option(DESCRIPTION, Default = MyEnum.Four)]
        public MyEnum Value { get; set; }
    }

    [Fact]
    public void Validate_Default()
    {
        var (o, r) = Scan<Validate_Default_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();

        Assert.Equal(nameof(MyEnum.Four), opt.Default!);
        Assert.True(opt.IsTrimmed);

        Assert.Equal(default, o.Value);
        Assert.Equal(MyEnum.One, o.Value);

        o = opt.SetValueNew(o, null);
        Assert.Equal(MyEnum.Four, o.Value);


        o = opt.SetValueNew(o, "  ");
        Assert.Equal(MyEnum.Four, o.Value);

        opt.SetValue(o, nameof(MyEnum.Two));
        Assert.Equal(MyEnum.Two, o.Value);
        opt.SetValue(o, null);
        Assert.Equal(MyEnum.Four, o.Value);
    }


    [Command(NAME)]
    public class Validate_Default_Nullable_Command
    {
        [Option(DESCRIPTION, Default = MyEnum.Four)]
        public MyEnum? Value { get; set; }
    }

    [Fact]
    public void Validate_Default_Nullable()
    {
        var (o, r) = Scan<Validate_Default_Nullable_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();

        Assert.Equal(nameof(MyEnum.Four), opt.Default!);

        Assert.Null(o.Value);

        o = opt.SetValueNew(o, null);
        Assert.Equal(MyEnum.Four, o.Value);

        Assert.True(opt.IsTrimmed);
        o = opt.SetValueNew(o, "  ");
        Assert.Equal(MyEnum.Four, o.Value);

        opt.SetValue(o, nameof(MyEnum.Five));
        Assert.Equal(MyEnum.Five, o.Value);
        opt.SetValue(o, null);
        Assert.Equal(MyEnum.Four, o.Value);
    }


    [Command(NAME)]
    public class Validate_Default_Not_Found_Command
    {
        [Option(DESCRIPTION, Default = "foo")]
        public int Value { get; set; }
    }

    [Fact]
    public void Validate_Default_Not_Found()
    {
        var (o, r) = Scan<Validate_Default_Not_Found_Command>();
        Assert.Empty(r.Commands);
        var cdw = Assert.Single(r.CommandsInvalid);
        Assert.NotNull(cdw.Options.Single().Exception!);
    }
}
