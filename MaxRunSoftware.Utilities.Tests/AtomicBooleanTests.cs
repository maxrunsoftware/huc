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

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace MaxRunSoftware.Utilities.Tests;

public class AtomicBooleanTests
{
    [Fact]
    public void EqualsWork()
    {
        var ab1 = new AtomicBoolean(true);
        var ab2 = new AtomicBoolean(false);

        var b1 = true;
        var b2 = false;

        Assert.True(ab1.Equals(b1));
        Assert.True(ab2.Equals(b2));
        Assert.True(b1.Equals(ab1));
        Assert.True(b2.Equals(ab2));
        Assert.True(ab1.Equals(new AtomicBoolean(true)));

        Assert.False(ab1.Equals(b2));
        Assert.False(ab2.Equals(b1));
        Assert.False(b1.Equals(ab2));
        Assert.False(b2.Equals(ab1));
        Assert.False(ab1.Equals(ab2));
        Assert.False(ab2.Equals(ab1));
    }
}
