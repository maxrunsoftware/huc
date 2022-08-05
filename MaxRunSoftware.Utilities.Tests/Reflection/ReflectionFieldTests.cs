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

// ReSharper disable ConvertToAutoProperty

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public class ReflectionFieldTests : ReflectionTestBase
{
    public ReflectionFieldTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    public class TestClass
    {
        public string myField;

        private string myFieldPrivate;

        public string MyFieldPrivate
        {
            get => myFieldPrivate;
            set => myFieldPrivate = value;
        }

        public readonly string myFieldReadonly;

        public TestClass() { }

        public TestClass(string myFieldReadonly) { this.myFieldReadonly = myFieldReadonly; }
    }

    [TestFact]
    public void CanRead_Public()
    {
        var o = new TestClass();
        var x = F(o, "myField");
        var v = x.GetValue(o);
        Assert.Null(v);
        o.myField = "Foo";
        v = x.GetValue(o);
        Assert.NotNull(v);
        Assert.Equal("Foo", v as string);
    }

    [TestFact]
    public void CanWrite_Public()
    {
        var o = new TestClass();
        var x = F(o, "myField");
        x.SetValue(o, "Bar");
        Assert.Equal("Bar", o.myField);
    }


    [TestFact]
    public void CanRead_Private()
    {
        var o = new TestClass();
        var x = F(o, "myFieldPrivate");
        Assert.NotNull(x);
        var v = x.GetValue(o);
        Assert.Null(v);
        o.MyFieldPrivate = "Foo";
        v = x.GetValue(o);
        Assert.NotNull(v);
        Assert.Equal("Foo", v as string);
    }

    [TestFact]
    public void CanWrite_Private()
    {
        var o = new TestClass();
        var x = F(o, "myFieldPrivate");
        Assert.NotNull(x);
        x.SetValue(o, "Bar");
        Assert.Equal("Bar", o.MyFieldPrivate);
    }

    [TestFact]
    public void CanRead_Readonly()
    {
        var o = new TestClass();
        var x = F(o, "myFieldReadonly");
        Assert.NotNull(x);
        var v = x.GetValue(o);
        Assert.Null(v);
        o = new TestClass("Foo");
        v = x.GetValue(o);
        Assert.NotNull(v);
        Assert.Equal("Foo", v as string);
    }
}
