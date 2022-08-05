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

// ReSharper disable UseNameOfInsteadOfTypeOf

namespace MaxRunSoftware.Utilities.Tests.Extensions;

public class ExtensionsTypeTests : TestBase
{
    public ExtensionsTypeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    [Fact]
    public void Name_Primitive()
    {
        Assert.Equal("Void", typeof(void).Name);
        Assert.Equal("void", typeof(void).NameFormatted());

        Assert.Equal("Boolean", typeof(bool).Name);
        Assert.Equal("bool", typeof(bool).NameFormatted());

        Assert.Equal("Int32", typeof(int).Name);
        Assert.Equal("int", typeof(int).NameFormatted());
    }
}
