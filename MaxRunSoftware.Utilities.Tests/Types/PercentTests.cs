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

namespace MaxRunSoftware.Utilities.Tests.Types;

public class PercentTests : TestBase
{
    public PercentTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    [TestFact]
    public void Default_Value()
    {
        var p = new Percent();
        Assert.Equal(0, (int)p);
    }

    [TestFact]
    public void Cast_To_Int_Rounds()
    {
        Assert.Equal(2, (int)(Percent)1.75f);
        Assert.Equal(2, (int)(Percent)1.51f);
        Assert.Equal(1, (int)(Percent)1.25f);
        Assert.Equal(1, (int)(Percent)1.49f);
    }

    [TestFact]
    public void Cast_To_Int_Rounds_Every_Other()
    {
        Assert.Equal(2, (int)(Percent)1.5f); // up
        Assert.Equal(2, (int)(Percent)2.5f); // down
        Assert.Equal(4, (int)(Percent)3.5f); // up
        Assert.Equal(4, (int)(Percent)4.5f); // down
    }

    [TestFact]
    public void Min()
    {
        Assert.Equal(0, (int)(Percent)(-5.0f));
        Assert.Equal(0, (int)(Percent)(-1000.0f));
    }

    [TestFact]
    public void Max()
    {
        Assert.Equal(100, (int)(Percent)101.0f);
        Assert.Equal(100, (int)(Percent)1000.0f);
    }
}
