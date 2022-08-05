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

using System.Diagnostics;

namespace MaxRunSoftware.Utilities;

public abstract partial class XUnitTestBase : IDisposable
{
    protected abstract string ConfigTempDirectory { get; }

    private static readonly Counter testNumberCount = new();
    protected readonly Type getType;
    protected int TestNumber { get; }

    protected virtual StringComparer TestTraitsComparer => StringComparer.OrdinalIgnoreCase;
    protected virtual string TestNameClass { get; }
    protected virtual string TestNameMethod { get; }

    // ReSharper disable once InconsistentNaming
    private readonly Action<string> Logger;

    protected virtual string TestName => TestNameClass.CheckPropertyNotNullTrimmed(nameof(TestNameClass), getType) + "." + TestNameMethod.CheckPropertyNotNullTrimmed(nameof(TestNameClass), getType);
    private readonly Counter counter = new();
    protected int NextInt() => counter.Next();
    protected readonly object locker = new();
    private readonly SingleUse disposable = new();
    protected bool IsDisposed => disposable.IsUsed;

    private static readonly decimal timeElapsedMultiplier = decimal.Parse("0.001");

    private readonly DateTime timeStart;
    private readonly Stopwatch timeStopwatch = new();

    // ReSharper disable once InconsistentNaming
    protected static readonly object lockerStatic = new();

    private readonly List<Action> disposeAlso = new();

    protected bool IsDebug { get; set; }


    protected XUnitTestBase(object testOutputHelper)
    {
        getType = GetType();


        var testOutputHelperType = testOutputHelper.GetType();
        //var testField = testOutputHelperType.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
        var test = testOutputHelper.GetType().GetFieldValue("test", testOutputHelper);
        var testCase = test.GetType().GetPropertyValue("TestCase", test);
        var testMethod = testCase.GetType().GetPropertyValue("TestMethod", testCase);
        var method = testMethod.GetType().GetPropertyValue("Method", testMethod);
        var methodName = method.GetType().GetPropertyValue("Name", method).ToStringGuessFormat();

        var testClass = testMethod.GetType().GetPropertyValue("TestClass", testMethod);
        var clazz = testClass.GetType().GetPropertyValue("Class", testClass);
        var className = clazz.GetType().GetPropertyValue("Name", clazz).ToStringGuessFormat();

        var methodCaller = MethodCaller.GetCaller<string>(testOutputHelperType, "WriteLine");
        Action<string> logWriter = o => methodCaller.Call(testOutputHelper, o);
        //var testTraitsRaw = (Dictionary<string, List<string>>)testCase.GetType().GetPropertyValue("Traits", testCase);


        TestNameClass = className.Split('.').TrimOrNull().WhereNotNull().LastOrDefault() ?? getType.NameFormatted();
        TestNameMethod = methodName;
        Logger = logWriter;

        testTraits = new Lazy<XUnitTraitCollection>(() => XUnitTraitCollection.Get(Reflection.GetType(getType)));

        TestNumber = testNumberCount.Next();
        timeStart = DateTime.Now;
        timeStopwatch.Start();


        Info("+++ START +++" + "".PadRight(5) + timeStart.ToString("yyyy-MM-dd HH:mm:ss"));

        // ReSharper disable once VirtualMemberCallInConstructor
        SetUp();
    }

    protected virtual void SetUp() { }
    protected virtual void TearDown() { }

    public void Dispose()
    {
        if (!disposable.TryUse()) return;
        TearDown();

        foreach (var action in disposeAlso) action();

        timeStopwatch.Stop();
        var timeEnd = timeStart + timeStopwatch.Elapsed;
        var timeElapsed = timeStopwatch.ElapsedMilliseconds * timeElapsedMultiplier;

        Info("---- END ----" + "".PadRight(5) + timeEnd.ToString("yyyy-MM-dd HH:mm:ss") + "  " + timeElapsed);
    }
}
