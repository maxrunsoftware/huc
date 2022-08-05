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

namespace MaxRunSoftware.Utilities.Tests;

public class TestBase : XUnitTestBase
{
    public TestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper.CheckNotNull(nameof(testOutputHelper)))
    {
        /*
        var type = testOutputHelper.GetType();
        var testField = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
        var test = (ITest)testField!.GetValue(testOutputHelper);
        var testCase = test!.TestCase;
        var testMethod = testCase.TestMethod;
        var method = testMethod.Method;
        var testClass = testMethod.TestClass;
        var clazz = testClass.Class;
        */
    }


    protected override string ConfigTempDirectory => Config.Temp_Directory;
}
