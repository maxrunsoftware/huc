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

using System.Runtime.CompilerServices;

//[assembly: TestFramework("MyNamespace.Xunit.MyTestFramework", "MyAssembly")]

namespace MaxRunSoftware.Utilities.Tests;

public class TestBase : XUnitTestBase
{
    public static string GetSkipMessage(XUnitTraitCollection traits)
    {
        //if (traits.Count > 0) LogStatic(traits.ToString());
        var d = new List<(string Key, string Value)>
        {
            ("Sql", "Slow"),
            ("Sql", "Oracle"),
            ("Type", "Sql")
        };

        foreach (var kvp in d)
        {
            if (traits.Has(kvp.Key, kvp.Value))
            {
                var msg = "Skipped: " + kvp.Key + "=" + kvp.Value;
                return msg;
            }
        }

        return null;
    }

    public TestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper.CheckNotNull(nameof(testOutputHelper)))
    {
        /*
        foreach (var kvp in TestTraits.OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var v in kvp.Value.OrderBy(o => o, StringComparer.OrdinalIgnoreCase))
            {
                Debug($"Trait: {kvp.Key}={v}");
            }
        }
        */

        //Skip.If(TestTraitsContainAny(Config.SkipTraits));

        /*
        var type = testOutputHelper.GetType();
        var testField = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
        var test = (ITest)testField!.GetValue(testOutputHelper);
        var testCase = test!.TestCase;
        var testMethod = testCase.TestMethod;
        var method = testMethod.Method;
        var testClass = testMethod.TestClass;
        var clazz = testClass.Class;

        var d = test.TestCase.Traits;
        foreach (var kvp in d)
        {
            foreach (var v in kvp.Value)
            {
                Info($"Trait({kvp.Key}, {v})");
            }
        }
        */
        //testName = clazz.Name.Split('.').Last() + "." + method.Name;

        //Log("+++ START +++" + "".PadRight(5) + timeStart.ToString("yyyy-MM-dd HH:mm:ss"));

        // ReSharper disable once VirtualMemberCallInConstructor
        //Setup();


        //throw new SkipException("");
    }


    protected override string ConfigTempDirectory => Config.Temp_Directory;
}

public class TestFactAttribute : FactAttribute
{
    public override string Skip
    {
        get => base.Skip.TrimOrNull() ?? GetSkipValue();
        set => base.Skip = value;
    }

    public CallerInfo CallerInfo { get; }

    public TestFactAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = null)
    {
        CallerInfo = new CallerInfo(filePath, lineNumber, memberName);
    }

    private string GetSkipValue()
    {
        var traits = XUnitTestBase.GetAttributeTraits(this, a => a.CallerInfo);
        if (traits == null) throw new Exception("Could not find TraitMethod for " + (GetType().FullName ?? GetType().Name) + "  " + CallerInfo);
        return TestBase.GetSkipMessage(traits);
    }
}

public class TestTheoryAttribute : TheoryAttribute
{
    public override string Skip
    {
        get => base.Skip.TrimOrNull() ?? GetSkipValue();
        set => base.Skip = value;
    }

    public CallerInfo CallerInfo { get; }

    public TestTheoryAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = null)
    {
        CallerInfo = new CallerInfo(filePath, lineNumber, memberName);
    }

    private string GetSkipValue()
    {
        var traits = XUnitTestBase.GetAttributeTraits(this, a => a.CallerInfo);
        if (traits == null) throw new Exception("Could not find TraitMethod for " + (GetType().FullName ?? GetType().Name) + "  " + CallerInfo);
        return TestBase.GetSkipMessage(traits);
    }
}
/*
public class MyTestFramework : XunitTestFramework
{
    public MyTestFramework(IMessageSink messageSink) : base(messageSink) { }

    protected override ITestFrameworkDiscoverer CreateDiscoverer(IAssemblyInfo assemblyInfo) =>
        new MyTestFrameworkDiscoverer(
            assemblyInfo,
            SourceInformationProvider,
            DiagnosticMessageSink);
}

public class MyTestFrameworkDiscoverer : XunitTestFrameworkDiscoverer
{
    public MyTestFrameworkDiscoverer(
        IAssemblyInfo assemblyInfo,
        ISourceInformationProvider sourceProvider,
        IMessageSink diagnosticMessageSink,
        IXunitTestCollectionFactory collectionFactory = null)
        : base(
            assemblyInfo,
            sourceProvider,
            diagnosticMessageSink,
            collectionFactory) { }

    protected override bool IsValidTestClass(ITypeInfo type) => base.IsValidTestClass(type) && FilterType(type);


    private bool FilterType(ITypeInfo type) => true;
}
*/
