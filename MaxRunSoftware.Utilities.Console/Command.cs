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

namespace MaxRunSoftware.Utilities.Console;

[AttributeUsage(AttributeTargets.Class)]
public sealed class HideCommandAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public sealed class SuppressBannerAttribute : Attribute { }

public abstract class Command : ICommand
{
    private Args args;

    public string[] Args
    {
        get => args.ArgsString;
        set => args = new Args(value);
    }

    public string WorkingDirectory => Path.GetFullPath(Environment.CurrentDirectory);
    private ConfigFile config;
    private ConfigFile Config => config ??= new ConfigFile();

    protected string Encrypt(string data) => Config.Encrypt(data);

    protected readonly ILogger log;

    public bool IsHidden => GetType().GetCustomAttributes(true).Any(o => o is HideCommandAttribute);
    public bool SuppressBanner => GetType().GetCustomAttributes(true).Any(o => o is SuppressBannerAttribute);

    public CommandHelpBuilder Help { get; }
    public string Name => Help.Name;
    public string HelpSummary => Name.PadRight(Program.CommandObjects.Select(o => o.Name).MaxLength() + 2) + Help.Summary;

    public string HelpDetails
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine(Name);
            sb.AppendLine(Help.Summary);
            foreach (var s in Help.Details) sb.AppendLine(s);

            if (!Help.Parameters.IsEmpty())
            {
                sb.AppendLine();
                sb.AppendLine("Parameters:");
            }

            var padWidth = 0;
            foreach (var s in Help.Parameters)
            {
                var ss = "-" + s.p1;
                if (s.p2 != null) ss += ", -" + s.p2;

                var len = ss.Length;
                if (len > padWidth) padWidth = len;
            }

            padWidth += 3;

            foreach (var s in Help.Parameters)
            {
                var ss = "-" + s.p1;
                if (s.p2 != null) ss += ", -" + s.p2;

                ss = ss.PadRight(padWidth);
                ss += s.description;
                sb.AppendLine("  " + ss);
            }

            if (!Help.Values.IsEmpty())
            {
                sb.AppendLine();
                sb.AppendLine("Arguments:");
            }

            foreach (var s in Help.Values) sb.AppendLine("  " + s);

            if (!Help.Examples.IsEmpty())
            {
                sb.AppendLine();
                sb.AppendLine("Examples:");
            }

            foreach (var example in Help.Examples) sb.AppendLine("  huc " + Name + " " + example);

            return sb.ToString();
        }
    }

    protected Command()
    {
        log = Program.GetLogger(GetType());
        Help = new CommandHelpBuilder(GetType().Name);
        // ReSharper disable once VirtualMemberCallInConstructor
        CreateHelp(Help);
    }

    public void Execute()
    {
        if (args == null) throw new Exception("Args not set");

        using (Util.Diagnostic(log.Debug)) { ExecuteInternal(); }
    }

    protected abstract void ExecuteInternal();
    protected abstract void CreateHelp(CommandHelpBuilder help);

    #region File

    protected void DeleteExistingFile(string file)
    {
        if (File.Exists(file))
        {
            log.Info("Deleting existing file " + file);
            File.Delete(file);
        }
    }

    protected void CheckFileExists(string file)
    {
        if (!File.Exists(file)) throw new FileNotFoundException("File " + file + " does not exist", file);
    }

    protected void CheckFileExists(IEnumerable<string> files)
    {
        foreach (var file in files) CheckFileExists(file);
    }

    protected void CheckDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("Directory " + directory + " does not exist");
    }

    protected void CheckDirectoryExists(IEnumerable<string> directories)
    {
        foreach (var directory in directories) CheckDirectoryExists(directory);
    }

    protected string ReadFile(string path, Encoding encoding = null)
    {
        string data;
        path = Path.GetFullPath(path);
        log.Debug($"Reading text file {path}");
        using (Util.Diagnostic(log.Trace))
        {
            CheckFileExists(path);
            data = Util.FileRead(path, encoding ?? Constant.Encoding_UTF8);
        }

        log.Debug($"Read text file {path}   {data.Length} characters");
        return data;
    }

    protected byte[] ReadFileBinary(string path)
    {
        byte[] data;
        path = Path.GetFullPath(path);
        log.Debug($"Reading binary file {path}");
        using (Util.Diagnostic(log.Trace))
        {
            CheckFileExists(path);
            data = Util.FileRead(path);
        }

        log.Debug($"Read binary file {path}   {data.Length} bytes");
        return data;
    }

    protected void WriteFile(string path, string data, Encoding encoding = null)
    {
        path = Path.GetFullPath(path);
        log.Debug($"Writing text file {path}   {data.Length} characters");
        using (Util.Diagnostic(log.Trace))
        {
            DeleteExistingFile(path);
            Util.FileWrite(path, data, encoding ?? Constant.Encoding_UTF8);
        }

        log.Debug($"Wrote text file {path}   {data.Length} characters");
    }

    protected void WriteFileBinary(string path, byte[] data)
    {
        path = Path.GetFullPath(path);
        log.Debug($"Writing binary file {path}   {data.Length} bytes");
        using (Util.Diagnostic(log.Trace))
        {
            DeleteExistingFile(path);
            Util.FileWrite(path, data);
        }

        log.Debug($"Wrote binary file {path}   {data.Length} bytes");
    }

    protected Table ReadTableTab(string path, Encoding encoding = null, bool headerRow = true)
    {
        var data = ReadFile(path, encoding);
        log.Debug($"Read {data.Length} characters from file {path}");

        var lines = data.SplitOnNewline();
        if (lines.Length > 0 && lines[^1] != null && lines[^1].Length == 0) lines = lines.RemoveTail(); // Ignore if last line is just line feed

        log.Debug($"Found {lines.Length} lines in file {path}");

        var t = Table.Create(lines.Select(l => l.Split('\t')), headerRow);
        log.Debug($"Created table with {t.Columns.Count} columns and {t.Count} rows");

        return t;
    }

    protected void WriteTableTab(Table table, TextWriter writer)
    {
        table.CheckNotNull(nameof(table));

        table.ToDelimited(
            writer.Write,
            "\t",
            null,
            includeHeader: true,
            dataDelimiter: "\t",
            dataQuoting: null,
            includeRows: true,
            newLine: Constant.NewLine_Windows,
            headerDelimiterReplacement: "        ",
            dataDelimiterReplacement: "        "
        );
        writer.Flush();
    }


    protected void WriteTableTab(string fileName, Table table, string suffix = null)
    {
        fileName = Path.GetFullPath(fileName);

        log.Debug("Writing TAB delimited Table to file " + fileName);
        using (var stream = Util.FileOpenWrite(fileName))
        using (var writer = new StreamWriter(stream, Constant.Encoding_UTF8))
        {
            WriteTableTab(table, writer);
            stream.Flush(true);
        }

        log.Info("Successfully wrote " + table + " to file " + fileName + (suffix ?? string.Empty));
    }

    protected string WriteTableTab(Table table)
    {
        using (var writer = new StringWriter())
        {
            WriteTableTab(table, writer);
            return writer.ToString();
        }
    }

    #endregion File

    #region Parameters

    private static bool ShouldLog(string parameterName)
    {
        if (parameterName == null) return true;

        parameterName = parameterName.ToLower();
        if (parameterName.Contains("password")) return false;

        return true;
    }

    public string GetArgParameter(string key1, string key2) => args.GetParameter(key1, key2);

    public string GetArgParameterOrConfig(string key1, string key2)
    {
        var v = GetArgParameter(key1, key2);
        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        if (ShouldLog(key1) && ShouldLog(key2)) log.Debug($"{key1}: {v}");

        return v;
    }

    public string GetArgParameterOrConfig(string key1, string key2, string defaultValue)
    {
        var v = GetArgParameter(key1, key2);
        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        if (v.TrimOrNull() == null) v = defaultValue;

        if (ShouldLog(key1) && ShouldLog(key2)) log.Debug($"{key1}: {v}");

        return v;
    }

    public string GetArgParameterOrConfigRequired(string key1, string key2)
    {
        var v = GetArgParameter(key1, key2);

        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        if (v.TrimOrNull() != null)
        {
            if (ShouldLog(key1) && ShouldLog(key2)) log.Debug($"{key1}: {v}");

            return v;
        }

        var msg = $"No value provided for argument '{key1}' or properties file entry for '{Name}.{key1}'";
        throw new ArgsException(key1, msg);
    }

    public Encoding GetArgParameterOrConfigEncoding(string key1, string key2)
    {
        var v = GetArgParameter(key1, key2);
        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        log.Debug($"{key1}String: {v}");
        var encoding = Constant.Encoding_UTF8;
        if (v.TrimOrNull() != null) encoding = Util.ParseEncoding(v);

        log.Debug($"{key1}: {encoding}");
        return encoding;
    }

    public int GetArgParameterOrConfigInt(string key1, string key2, int defaultValue)
    {
        var v = GetArgParameter(key1, key2);
        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        log.Debug($"{key1}String: {v}");
        var o = defaultValue;
        if (v.TrimOrNull() != null) o = v.ToInt();

        log.Debug($"{key1}: {o}");
        return o;
    }

    public ushort GetArgParameterOrConfigUShort(string key1, string key2, ushort defaultValue)
    {
        var v = GetArgParameter(key1, key2);
        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        log.Debug($"{key1}String: {v}");
        var o = defaultValue;
        if (v.TrimOrNull() != null) o = v.ToUShort();

        log.Debug($"{key1}: {o}");
        return o;
    }

    public bool GetArgParameterOrConfigBool(string key1, string key2, bool defaultValue)
    {
        var v = GetArgParameter(key1, key2);
        if (v.TrimOrNull() == null) v = GetArgParameterConfig(key1);

        log.Debug($"{key1}String: {v}");
        var o = defaultValue;
        if (v.TrimOrNull() != null) o = v.ToBool();

        log.Debug($"{key1}: {o}");
        return o;
    }

    public TEnum GetArgParameterOrConfigEnum<TEnum>(string key1, string key2, TEnum defaultValue) where TEnum : struct, Enum
    {
        var v = GetArgParameterOrConfigEnum<TEnum>(key1, key2);
        if (v == null) return defaultValue;

        return v.Value;
    }

    public TEnum? GetArgParameterOrConfigEnum<TEnum>(string key1, string key2) where TEnum : struct, Enum
    {
        var v = GetArgParameter(key1, key2).TrimOrNull() ?? GetArgParameterConfig(key1);
        log.Debug($"{key1}String: {v}");

        if (v == null)
        {
            log.Debug($"{key1}: {null}");
            return null;
        }

        var enumType = typeof(TEnum);
        var enumObject = enumType.GetEnumValue(v);
        if (enumObject == null) throw new ArgsException(key1, "Parameter " + key1 + " is not valid, values are [ " + enumType.GetEnumNames().ToStringDelimited(" | ") + " ]");

        log.Debug($"{key1}: {enumObject}");

        return (TEnum)enumObject;
    }

    public string GetArgParameterConfig(string key) => Config[Name + "." + key];

    public IReadOnlyList<string> GetArgValues() => args.Values;

    public List<string> GetArgValuesTrimmed() => GetArgValues().TrimOrNull().WhereNotNull().ToList();

    public string GetArgValueTrimmed(int index) => GetArgValuesTrimmed().GetAtIndexOrDefault(index);

    public (string firstValue, List<string> otherValues) GetArgValuesTrimmed1N()
    {
        var list = GetArgValuesTrimmed();
        if (list.Count < 1) return (null, list);

        var firstItem = list.PopHead();
        return (firstItem, list);
    }

    public string GetArgValueDirectory(int index, string valueName = "targetDirectory", bool isRequired = true, bool isExist = true, bool useCurrentDirectoryAsDefault = false)
    {
        var val = GetArgValueTrimmed(index);
        log.DebugParameter(valueName, val);
        if (val == null && useCurrentDirectoryAsDefault) val = Environment.CurrentDirectory;

        if (val == null)
        {
            if (isRequired) throw ArgsException.ValueNotSpecified(valueName);

            return null;
        }

        val = Path.GetFullPath(val);
        log.DebugParameter(valueName, val);
        if (isExist)
        {
            if (!Directory.Exists(val)) throw new DirectoryNotFoundException("Arg <" + valueName + "> directory " + val + " does not exist");
        }

        return val;
    }

    public string GetArgValueFile(int index, string valueName = "targetFile", bool isRequired = true)
    {
        var val = GetArgValueTrimmed(index);
        log.DebugParameter(valueName, val);
        if (val == null)
        {
            if (isRequired) throw ArgsException.ValueNotSpecified(valueName);

            return null;
        }

        val = Path.GetFullPath(val);
        log.DebugParameter(valueName, val);

        return val;
    }

    #endregion Parameters

    public string DisplayEnumOptions<TEnum>() where TEnum : struct, Enum => "[ " + typeof(TEnum).GetEnumNames().ToStringDelimited(" | ") + " ]";

    public string DisplayEnumOptions<TEnum>(TEnum defaultOption) where TEnum : struct, Enum => "(" + defaultOption + ")  " + DisplayEnumOptions<TEnum>();

    public static string ParseInputFile(string inputFile) => ParseInputFiles(inputFile.Yield()).FirstOrDefault();

    public static List<string> ParseInputFiles(IEnumerable<string> inputFiles, bool recursive = false) =>
        inputFiles.OrEmpty()
            .TrimOrNull()
            .WhereNotNull()
            .SelectMany(o => ParseFileName(o, recursive))
            .Select(Path.GetFullPath)
            .Distinct(Constant.Path_IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static List<string> ParseFileName(string fileName, bool recursive = false)
    {
        var l = new List<string>();
        fileName = fileName.TrimOrNull();
        if (fileName == null) return l;

        if (fileName.IndexOf('*') >= 0 || fileName.IndexOf('?') >= 0) // wildcard
        {
            var idx1 = fileName.LastIndexOf(Path.DirectorySeparatorChar);
            var idx2 = fileName.LastIndexOf(Path.AltDirectorySeparatorChar);
            var workingDirectory = Environment.CurrentDirectory;
            var filePattern = fileName;
            if (idx1 == -1 && idx2 == -1)
            {
                // No directory prefix
            }
            else if (idx1 == idx2 || idx1 > idx2)
            {
                workingDirectory = fileName.Substring(0, idx1);
                filePattern = fileName.Substring(idx1 + 1);
            }
            else
            {
                workingDirectory = fileName.Substring(0, idx2);
                filePattern = fileName.Substring(idx2 + 1);
            }

            foreach (var f in Util.FileListFiles(workingDirectory, recursive))
            {
                var n = Path.GetFileName(f);
                if (n.EqualsWildcard(filePattern, true)) l.Add(f);
            }
        }
        else
        {
            fileName = Path.GetFullPath(fileName);
            if (Util.IsDirectory(fileName)) { l.AddRange(Util.FileListFiles(fileName, recursive)); }
            else if (Util.IsFile(fileName)) l.Add(fileName);
        }

        return l;
    }

    public static List<string> ParseFileNames(IEnumerable<string> fileNames)
    {
        var l = new List<string>();
        foreach (var fileName in fileNames.OrEmpty()) l.AddRange(ParseFileName(fileName));

        return l.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
