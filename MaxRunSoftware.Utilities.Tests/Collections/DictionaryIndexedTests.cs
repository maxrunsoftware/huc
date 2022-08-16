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

namespace MaxRunSoftware.Utilities.Tests.Collections;

public class DictionaryIndexedTests : TestBase
{
    public DictionaryIndexedTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }


    [Fact]
    public void Order_Of_Items()
    {
        var count = 10000;
        var items = new List<(int, string)>();
        var usedKeys = new HashSet<int>();

        for (var i = 0; i < count; i++)
        {
            var k = Random.Next(int.MinValue, int.MaxValue);
            if (!usedKeys.Add(k))
            {
                i--;
                continue;
            }

            var v = RandomString();
            items.Add((k, v));
        }

        Assert.Equal(count, items.Count);

        var d = new DictionaryIndexed<int, string>();
        foreach (var kvp in items)
        {
            d.Add(kvp.Item1, kvp.Item2);
        }

        var dKeys = d.Keys.ToList();
        Assert.Equal(count, dKeys.Count);

        var dVals = d.Values.ToList();
        Assert.Equal(count, dVals.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(items[i].Item1, dKeys[i]);
            Assert.Equal(items[i].Item2, dVals[i]);
        }
    }
}
