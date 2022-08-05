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

public class ArgumentParserTests : TestBase
{
    public ArgumentParserTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    private record ExpectedOption(string Name, string Value);

    private record ExpectedParameter(ushort Index, string Value);

    private void AssertTest(string[] args, IReadOnlyList<ExpectedOption> expectedOptions, IReadOnlyList<ExpectedParameter> expectedParameters)
    {
        var p = new ArgumentParser();
        Info(p.ToStringGenerated());

        var r = p.Parse(args);
        Info(r.ToStringGenerated());

        expectedOptions ??= Array.Empty<ExpectedOption>();
        expectedParameters ??= Array.Empty<ExpectedParameter>();

        Assert.Equal(expectedOptions.Count, r.Options.Count);
        for (var i = 0; i < expectedOptions.Count; i++)
        {
            var expected = expectedOptions[i];
            var actual = r.Options[i];

            if (expected == null) { Assert.Null(actual); }
            else
            {
                Assert.NotNull(actual);
                Assert.Equal(expected.Name, actual.Name);
                Assert.Equal(expected.Value, actual.Value);
            }
        }


        Assert.Equal(expectedParameters.Count, r.Parameters.Count);
        for (var i = 0; i < expectedParameters.Count; i++)
        {
            var expected = expectedParameters[i];
            var actual = r.Parameters[i];

            if (expected == null) { Assert.Null(actual); }
            else
            {
                Assert.NotNull(actual);
                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(expected.Value, actual.Value);
            }
        }
    }

    private static string[] Args(params string[] args) => args;

    private static ExpectedOption[] Opts(params (string name, string value)[] expected) => expected.OrEmpty().Select(o => new ExpectedOption(o.name, o.value)).ToArray();

    private static ExpectedParameter[] Paras(params (ushort index, string value)[] expected) => expected.OrEmpty().Select(o => new ExpectedParameter(o.index, o.value)).ToArray();

    private static ExpectedParameter[] Paras(params string[] expected) => expected.OrEmpty().Select((o, i) => new ExpectedParameter((ushort)i, o)).ToArray();

    [TestFact]
    public void Parse_Null() => AssertTest(null, null, null);

    [TestFact]
    public void Parse_Empty() => AssertTest(Args(), null, null);

    [TestFact]
    public void Parse_Option1() => AssertTest(Args("-foo=bar"), Opts(("foo", "bar")), null);

    [TestFact]
    public void Parse_Option3() => AssertTest(Args("-foo=bar", "--faa=bee2", "---fee=baa"), Opts(("foo", "bar"), ("faa", "bee2"), ("fee", "baa")), null);

    [TestFact]
    public void Parse_Option1_With_Space() => AssertTest(Args("-foo=bar train"), Opts(("foo", "bar train")), null);

    [TestFact]
    public void Parse_Option1_With_Dash() => AssertTest(Args("-foo=bar-train"), Opts(("foo", "bar-train")), null);

    [TestFact]
    public void Parse_Option1_With_SpaceDashEquals() => AssertTest(Args("-foo==b-a=r-tr-=ain-=-"), Opts(("foo", "=b-a=r-tr-=ain-=-")), null);

    [TestFact]
    public void Parse_Parameter1() => AssertTest(Args("foo"), null, Paras((0, "foo")));

    [TestFact]
    public void Parse_Parameter2() => AssertTest(Args("foo", "bar"), null, Paras("foo", "bar"));

    [TestFact]
    public void Parse_Parameter4_Null() => AssertTest(Args("foo", "bar", "", "train"), null, Paras("foo", "bar", "", "train"));

    [TestFact]
    public void Parse_Parameter4_Empty() => AssertTest(Args("foo", "bar", "", "train"), null, Paras("foo", "bar", "", "train"));

    [TestFact]
    public void Parse_Parameter5_NullEmpty() => AssertTest(Args(null, "", null, "", null), null, Paras(null, "", null, "", null));
}
