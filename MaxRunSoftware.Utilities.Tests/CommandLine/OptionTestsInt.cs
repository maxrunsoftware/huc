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

// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Global
// ReSharper disable InconsistentNaming

namespace MaxRunSoftware.Utilities.Tests.CommandLine;

public class OptionTestsInt : OptionTestsBase
{
    public OptionTestsInt(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    [Command(NAME)]
    public class Value_Command
    {
        [Option(DESCRIPTION)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Value()
    {
        var (o, r) = Scan<Value_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.True(opt.IsRequired);

        Assert.Equal(0, o.Value);
        opt.SetValue(o, "4");
        Assert.Equal(4, o.Value);

        Assert.ThrowsAny<Exception>(() => opt.SetValue(o, null));
    }


    [Command(NAME)]
    public class Value_Nullable_Command
    {
        [Option(DESCRIPTION)]
        public int? Value { get; set; }
    }

    [TestFact]
    public void Value_Nullable()
    {
        var (o, r) = Scan<Value_Nullable_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.False(opt.IsRequired);

        Assert.Null(o.Value);
        opt.SetValue(o, "4");
        Assert.Equal(4, o.Value);

        opt.SetValue(o, null);
        Assert.Null(o.Value);

        o = new Value_Nullable_Command();
        opt.SetValue(o, null);
        Assert.Null(o.Value);
    }


    [Command(NAME)]
    public class Value_Min_Command
    {
        [Option(DESCRIPTION, Min = 3)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Value_Min()
    {
        var (o, r) = Scan<Value_Min_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.Equal("3", opt.Min!);
        Assert.Null(opt.Max!);

        Assert.Equal(0, o.Value);

        o = opt.SetValueNew(o, "4");
        Assert.Equal(4, o.Value);

        o = opt.SetValueNew(o, "3");
        Assert.Equal(3, o.Value);

        Assert.ThrowsAny<Exception>(() => opt.SetValue(o, "2"));
        Assert.Equal(3, o.Value);
    }


    [Command(NAME)]
    public class Value_Max_Command
    {
        [Option(DESCRIPTION, Max = 5)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Value_Max()
    {
        var (o, r) = Scan<Value_Max_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.Null(opt.Min!);
        Assert.Equal("5", opt.Max!);

        Assert.Equal(0, o.Value);

        o = opt.SetValueNew(o, "4");
        Assert.Equal(4, o.Value);

        o = opt.SetValueNew(o, "5");
        Assert.Equal(5, o.Value);

        Assert.ThrowsAny<Exception>(() => opt.SetValue(o, "6"));
        Assert.Equal(5, o.Value);
    }


    [Command(NAME)]
    public class Value_Min_Max_Command
    {
        [Option(DESCRIPTION, Min = 3, Max = 5)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Value_Min_Max()
    {
        var (o, r) = Scan<Value_Min_Max_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();
        Assert.Equal("3", opt.Min!);
        Assert.Equal("5", opt.Max!);

        Assert.Equal(0, o.Value);

        Assert.ThrowsAny<Exception>(() => opt.SetValueNew(o, "2"));
        Assert.Equal(0, o.Value);

        o = opt.SetValueNew(o, "3");
        Assert.Equal(3, o.Value);

        o = opt.SetValueNew(o, "4");
        Assert.Equal(4, o.Value);

        o = opt.SetValueNew(o, "5");
        Assert.Equal(5, o.Value);

        Assert.ThrowsAny<Exception>(() => opt.SetValue(o, "6"));
        Assert.Equal(5, o.Value);
    }


    [Command(NAME)]
    public class Validate_Default_Command
    {
        [Option(DESCRIPTION, Default = 4)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Validate_Default()
    {
        var (o, r) = Scan<Validate_Default_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();

        Assert.Equal("4", opt.Default!);
        Assert.True(opt.IsTrimmed);

        Assert.Equal(0, o.Value);

        o = opt.SetValueNew(o, null);
        Assert.Equal(4, o.Value);


        o = opt.SetValueNew(o, "  ");
        Assert.Equal(4, o.Value);

        opt.SetValue(o, "9");
        Assert.Equal(9, o.Value);
        opt.SetValue(o, null);
        Assert.Equal(4, o.Value);
    }


    [Command(NAME)]
    public class Validate_Default_Nullable_Command
    {
        [Option(DESCRIPTION, Default = 4)]
        public int? Value { get; set; }
    }

    [TestFact]
    public void Validate_Default_Nullable()
    {
        var (o, r) = Scan<Validate_Default_Nullable_Command>();
        var opt = Assert.Single(r.Commands)!.Options.Single();

        Assert.Equal("4", opt.Default!);

        Assert.Null(o.Value);

        o = opt.SetValueNew(o, null);
        Assert.Equal(4, o.Value);


        Assert.True(opt.IsTrimmed);
        o = opt.SetValueNew(o, "  ");
        Assert.Equal(4, o.Value);

        opt.SetValue(o, "9");
        Assert.Equal(9, o.Value);
        opt.SetValue(o, null);
        Assert.Equal(4, o.Value);
    }


    [Command(NAME)]
    public class Validate_Min_Default_Fails_Command
    {
        [Option(DESCRIPTION, Min = 3, Default = 2)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Validate_Min_Default_Fails()
    {
        var (o, r) = Scan<Validate_Min_Default_Fails_Command>();
        Assert.Empty(r.Commands);
        var cdw = Assert.Single(r.CommandsInvalid);
        Assert.NotNull(cdw.Options.Single().Exception!);
    }


    [Command(NAME)]
    public class Validate_Max_Default_Fails_Command
    {
        [Option(DESCRIPTION, Max = 5, Default = 6)]
        public int Value { get; set; }
    }

    [TestFact]
    public void Validate_Max_Default_Fails()
    {
        var (o, r) = Scan<Validate_Max_Default_Fails_Command>();
        Assert.Empty(r.Commands);
        var cdw = Assert.Single(r.CommandsInvalid);
        Assert.NotNull(cdw.Options.Single().Exception!);
    }
}
