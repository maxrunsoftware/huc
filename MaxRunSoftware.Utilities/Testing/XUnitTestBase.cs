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

public abstract class XUnitTestBase : IDisposable
{
    protected XUnitTestBase(object testOutputHelper)
    {
        type = GetType();

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


        TestNameClass = (className != null ? className.Split('.').TrimOrNull().WhereNotNull().LastOrDefault() : type.NameFormatted()) ?? type.Name;
        TestNameMethod = methodName ?? "[UNKNOWN-METHOD]";
        Logger = logWriter;
        TestNumber = testNumberCount.Next();

        workingDirectory = new Lzy<string>(WorkingDirectory_Build);
        random = new Lzy<Random>(Random_Build);
        timeStart = DateTime.Now;
        timeStopwatch.Start();


        Info("+++ START +++" + "".PadRight(5) + timeStart.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    protected abstract string ConfigTempDirectory { get; }

    private static readonly Counter testNumberCount = new();
    protected readonly Type type;
    protected int TestNumber { get; }

    protected virtual string TestNameClass { get; }
    protected virtual string TestNameMethod { get; }

    // ReSharper disable once InconsistentNaming
    private readonly Action<string> Logger;

    protected virtual string TestName => TestNameClass.CheckPropertyNotNullTrimmed(nameof(TestNameClass), type) + "." + TestNameMethod.CheckPropertyNotNullTrimmed(nameof(TestNameClass), type);
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
    protected void OnDispose(Action action) => disposeAlso.Add(action);

    protected bool IsDebug { get; set; }


    public void Dispose()
    {
        if (!disposable.TryUse()) return;

        foreach (var action in disposeAlso)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Info(e);
            }
        }

        timeStopwatch.Stop();
        var timeEnd = timeStart + timeStopwatch.Elapsed;
        var timeElapsed = timeStopwatch.ElapsedMilliseconds * timeElapsedMultiplier;

        Info("---- END ----" + "".PadRight(5) + timeEnd.ToString("yyyy-MM-dd HH:mm:ss") + "  " + timeElapsed);
    }


    #region FileSystem

    protected string WorkingDirectory => workingDirectory.Value;
    private readonly Lzy<string> workingDirectory;
    private string WorkingDirectory_Build()
    {
        var tempDirectory = ConfigTempDirectory.CheckPropertyNotNull(nameof(ConfigTempDirectory), type);
        tempDirectory = Path.GetFullPath(tempDirectory);
        tempDirectory.CheckDirectoryExists(nameof(ConfigTempDirectory));

        var workingDirectoryName = TestNameClass + "_" + TestNameMethod;
        var workingDirectoryPath = Path.GetFullPath(Path.Join(tempDirectory, workingDirectoryName));

        if (Directory.Exists(workingDirectoryPath))
        {
            Warn("WorkingDirectory already exists so deleting " + workingDirectoryPath);
            Directory.Delete(workingDirectoryPath, true);
        }

        Debug("Creating WorkingDirectory " + workingDirectoryPath);
        var di = Directory.CreateDirectory(workingDirectoryPath);

        var wd = di.FullName;

        OnDispose(() =>
        {
            Debug("Deleting WorkingDirectory " + wd);
            Directory.Delete(wd, true);
        });

        return wd;
    }


    public string NextFileName() => NextFileName(null);
    public string NextFileName(string? extension) => "test" + NextInt() + (extension == null ? "" : "." + extension);


    protected string WriteFile(string path, string text, Encoding? encoding = null, bool generateFileName = false) =>
        WriteFile(
            path,
            (encoding ?? Constant.Encoding_UTF8).GetBytes(text),
            generateFileName,
            "txt"
        );

    protected string WriteFile(string path, byte[] data, bool generateFileName = false, string generatedFileExtension = "dat")
    {
        if (generateFileName)
        {
            string p;
            do { p = GetPath(path + "/" + NextFileName(generatedFileExtension)); } while (File.Exists(p) || Directory.Exists(p));

            path = p;
        }
        else
        {
            path = GetPath(path);
            if (string.Equals(path, WorkingDirectory, Constant.Path_StringComparison)) throw new Exception("No filename specified");
        }

        var filename = Path.GetFileName(path);
        if (filename == null) throw new Exception("No filename specified");

        var directory = Path.GetDirectoryName(path);
        if (directory == null) throw new Exception("Could not determine directory from path " + path);

        if (!Directory.Exists(directory))
        {
            Debug("Creating directory " + directory);
            Directory.CreateDirectory(directory);
        }

        //var path = Path.GetFullPath(Path.Combine(pathDir, fileName.ToString()));
        if (DoesFileExist(path)) { Debug($"Overwriting existing file ({data.Length})  " + path); }
        else { Debug($"Writing data to file ({data.Length})  {path}"); }

        //File.WriteAllBytes(path, data);
        Util.FileWrite(path, data);

        return path;
    }

    protected bool DoesFileExist(string path) => File.Exists(GetPath(path));

    protected bool DoesDirectoryExist(string path) => Directory.Exists(GetPath(path));

    protected string GetPath(string relativePath) =>
        Path.GetFullPath(
            Path.Combine(
                WorkingDirectory
                    .Yield()
                    .ToArray()
                    .AppendTail(
                        relativePath.OrEmpty()
                            .SplitOnDirectorySeparator()
                            .Where(o => o.TrimOrNull() != null)
                            .ToArray()
                    )));

    protected string CreateDirectory(string path)
    {
        var pathFull = GetPath(path);
        if (pathFull == null) throw new IOException("Could not determine full path from " + path);
        if (Directory.Exists(pathFull))
        {
            Debug("Skipping creating directories because they already exist " + pathFull);
            return pathFull;
        }

        Debug("Creating directories " + pathFull);
        return Directory.CreateDirectory(pathFull).FullName;
    }

    #endregion FileSystem

    #region Logging

    private readonly Counter logCurrentLineNumber = new();
    protected virtual int LogMaxLines => int.MaxValue;
    protected bool LogNulls { get; set; } = true;
    protected string LogNullsString { get; set; } = "~null~";

    protected virtual string LogPrefix(LogLevel level)
    {
        var prefix = $"[{TestNumber}] {TestName}: ";
        if (IsDebug) return prefix + ("[" + level + "] ").PadRight(8);

        if (level is LogLevel.Trace or LogLevel.Debug) return prefix;

        prefix += level switch
        {
            LogLevel.Warn => "* WARN *  ",
            LogLevel.Error => "*** ERROR ****  ",
            _ => ""
        };

        return prefix;
    }

    protected void Debug(object o) => Log(o, LogLevel.Debug);
    protected void Info(object o) => Log(o, LogLevel.Info);
    protected void Info() => Log(string.Empty, LogLevel.Info);
    protected void Warn(object o) => Log(o, LogLevel.Warn);
    protected void Error(object o) => Log(o, LogLevel.Error);

    private void Log(object? o, LogLevel level)
    {
        var prefix = LogPrefix(level);

        if (o == null)
        {
            if (LogNulls) Log(prefix + LogNullsString);
            return;
        }

        var s = o.ToStringGuessFormat();
        if (s == null)
        {
            if (LogNulls) Log(prefix + LogNullsString);
            return;
        }

        Log(prefix + s);
    }

    private void Log(string message)
    {
        if (logCurrentLineNumber.Current > LogMaxLines) return;
        var log = Logger.CheckPropertyNotNull(nameof(Logger), GetType());
        log(message);
        logCurrentLineNumber.Next();
    }

    #endregion Logging

    #region Random

    private static readonly char[] randomTextCharsArray = Constant.Chars_Printable.ToArray();

    private int randomSeedDefault = Random.Shared.Next();

    protected int RandomSeed
    {
        get
        {
            lock (locker) { return randomSeedDefault; }
        }
        set
        {
            lock (locker)
            {
                if (random != null) throw new InvalidOperationException($"Could not set {nameof(RandomSeed)} to {value} because {nameof(Random)} has already been initialized with {randomSeedDefault}");

                randomSeedDefault = value;
            }
        }
    }

    protected Random Random => random.Value;
    private readonly Lzy<Random> random;
    private Random Random_Build()
    {
        lock (locker)
        {
            var s = RandomSeed;
            var r = new Random(s);
            Debug($"Created: Random({s})");
            return r;
        }
    }


    protected byte[] RandomBinary(
        // ReSharper disable once ParameterHidesMember
        int? seed = null,
        int lengthMin = 1,
        int lengthMax = 100_000
    )
    {
        lock (locker)
        {
            var randomSeed = seed ?? Random.Next();
            Debug($"{nameof(RandomBinary)}(seed: {randomSeed})");
            var r = new Random(randomSeed);
            var length = r.Next(lengthMin, lengthMax);
            var array = new byte[length];
            r.NextBytes(array);
            Debug($"{nameof(RandomBinary)} generated random byte[{array.Length}] with seed {randomSeed}");
            return array;
        }
    }


    protected string RandomString(
        // ReSharper disable once ParameterHidesMember
        int? seed = null,
        int? length = null,
        int lenghtMin = 1,
        int lengthMax = 1_000,
        string characterPool = Constant.Chars_Alphanumeric_String
    )
    {
        var randomSeed = seed ?? Random.Next();
        var r = new Random(randomSeed);

        var len = length ?? r.Next(lenghtMin, lengthMax);
        var s = r.NextString(len, characterPool);
        Debug($"{nameof(RandomString)}(seed: {randomSeed}): " + s);
        return s;
    }

    protected string RandomText(
        // ReSharper disable once ParameterHidesMember
        int? seed = null,
        int numberOfLinesMin = 0,
        int numberOfLinesMax = 100,
        int lineLengthMin = 1,
        int lineLengthMax = 100,
        byte chanceOfBlankLine = 10,
        byte chanceOfEndWithNewLine = 75,
        byte chanceOfSpace = 20,
        string[]? newLines = null
    )
    {
        lock (locker)
        {
            newLines ??= new[] { Constant.NewLine_Windows, Constant.NewLine_Unix };
            var randomSeed = seed ?? Random.Next();
            Debug($"{nameof(RandomText)}(seed: {randomSeed})");
            var r = new Random(randomSeed);

            var sb = new StringBuilder();
            var numberOfLines = r.Next(numberOfLinesMin, numberOfLinesMax);

            for (var i = 0; i < numberOfLines; i++)
            {
                if (Random.NextBool(chanceOfBlankLine))
                {
                    sb.Append(""); // NOOP
                }
                else
                {
                    var lineLen = r.Next(lineLengthMin, lineLengthMax);
                    var str = r.NextString(lineLen, randomTextCharsArray).ToCharArray();
                    foreach (var c in str) sb.Append(r.NextBool(chanceOfSpace) ? ' ' : c);
                }

                if (i == numberOfLines - 1)
                {
                    // last line, chance to include new line rather than leaving off the newline
                    if (r.NextBool(chanceOfEndWithNewLine)) sb.Append(r.Pick(newLines));
                }
                else
                {
                    // Pick a random newline
                    sb.Append(r.Pick(newLines));
                }
            }

            Debug($"{nameof(RandomText)} generated random string[{sb.Length}] with seed {randomSeed}");
            return sb.ToString();
        }
    }

    #endregion Random
}
