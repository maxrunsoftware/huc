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

// ReSharper disable StringLiteralTypo

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public class ReflectionPropertyTests : ReflectionTestBase
{
    public ReflectionPropertyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    public class CanReadWrite
    {
        public string MyProperty { get; set; }
        private string MyPropertyPrivate { get; set; }

        public string MyPropertyPrivateValue
        {
            get => MyPropertyPrivate;
            set => MyPropertyPrivate = value;
        }
    }

    [TestFact]
    public void CanRead_Public()
    {
        var o = new CanReadWrite();
        var x = P(o, "MyProperty");
        Assert.NotNull(x);
        var v = x.GetValue(o);
        Assert.Null(v);
        o.MyProperty = "Foo";
        v = x.GetValue(o);
        Assert.NotNull(v);
        Assert.Equal("Foo", v as string);
    }

    [TestFact]
    public void CanWrite_Public()
    {
        var o = new CanReadWrite();
        var x = P(o, "MyProperty");
        Assert.NotNull(x);
        x.SetValue(o, "Bar");
        Assert.Equal("Bar", o.MyProperty);
    }


    [TestFact]
    public void CanRead_Private()
    {
        var o = new CanReadWrite();
        var x = P(o, "MyPropertyPrivate");
        Assert.NotNull(x);
        var v = x.GetValue(o);
        Assert.Null(v);
        o.MyPropertyPrivateValue = "Foo";
        v = x.GetValue(o);
        Assert.NotNull(v);
        Assert.Equal("Foo", v as string);
    }

    [TestFact]
    public void CanWrite_Private()
    {
        var o = new CanReadWrite();
        var x = P(o, "MyPropertyPrivate");
        Assert.NotNull(x);
        x.SetValue(o, "Bar");
        Assert.Equal("Bar", o.MyPropertyPrivateValue);
    }


    public class CollectionSearch
    {
        public string S1 { get; set; }
        private string S2 { get; set; }
        public static string S3 { private get; set; }
        private static string S4 { get; }

        public MethodInfo MI1 { get; set; }

        public int I1 { get; set; }
    }

    [TestFact]
    public void Collection_ReturnType()
    {
        var t = T<CollectionSearch>();
        Assert.Equal(4, t.Properties.Type<string>().Count);

        Assert.Single(t.Properties.Type<MethodInfo>());
        Assert.Empty(t.Properties.Type<MethodBase>());
        Assert.Empty(t.Properties.Type<MemberInfo>());

        Assert.Single(t.Properties.Type<MethodInfo>(false));
        Assert.Single(t.Properties.Type<MethodBase>(false));
        Assert.Single(t.Properties.Type<MemberInfo>(false));

        Assert.Equal(t.Properties.Count, t.Properties.Type<object>(false).Count);
    }
}
