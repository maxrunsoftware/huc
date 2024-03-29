﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console;

public class ConfigFile : IBucketReadOnly<string, string>
{
    private static readonly ILogger log = Program.LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
    private readonly IDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static string Name => Path.GetFileName(FullPathName);
    public static string FullPathName => ProgramFullPathName + ".properties";

    private static string ProgramFullPathName
    {
        get
        {
            var fileName = Constant.CURRENT_EXE;
            if (fileName == null) throw new InvalidOperationException("Could not determine current executable name");

            return fileName;
        }
    }

    public IEnumerable<string> Keys => values.Keys;

    public string this[string key]
    {
        get
        {
            key = key.TrimOrNull();
            if (key == null) return null;

            if (values.TryGetValue(key, out var v))
            {
                if (v == null) return null;

                if (v.StartsWith(ProgramPasswordEscape)) return Decrypt(v);

                return v;
            }

            return null;
        }
    }

    private static string Key => Constant.ID.Replace("-", "");

    public string Encrypt(string unencryptedData)
    {
        var encryptedData = Encryption.EncryptSymmetric(Constant.ENCODING_UTF8.GetBytes(Key), Constant.ENCODING_UTF8.GetBytes(unencryptedData));
        var encryptedDataBase64 = Util.Base64(encryptedData);
        var encryptedDataBase64Padded = ProgramPasswordEscape + encryptedDataBase64;
        return encryptedDataBase64Padded;
    }

    private string Decrypt(string encryptedData)
    {
        if (!encryptedData.StartsWith(ProgramPasswordEscape)) return null;

        var encryptedDataBase64 = encryptedData.Substring(ProgramPasswordEscape.Length);
        var encryptedDataBytes = Util.Base64(encryptedDataBase64);
        var unencryptedData = Encryption.DecryptSymmetric(Constant.ENCODING_UTF8.GetBytes(Key), encryptedDataBytes);
        return Constant.ENCODING_UTF8.GetString(unencryptedData);
    }


    public ConfigFile(string fileName)
    {
        try
        {
            var props = new JavaProperties();
            var data = Util.FileRead(fileName, Constant.ENCODING_UTF8);
            props.LoadFromString(data);
            foreach (var kvp in props.ToDictionary())
            {
                var key = kvp.Key.TrimOrNull();
                if (key == null) continue;

                var val = kvp.Value.TrimOrNull().WhereNotNull().FirstOrDefault();
                if (val == null) continue;

                values[key] = val;
            }
        }
        catch (Exception e) { log.Warn("Could not load config file", e); }
    }

    public ConfigFile() : this(FullPathName) { }

    public static List<string> GetAllKeys()
    {
        var commandObjects = Program.CommandObjects;
        var commandObjectParams = new List<string>();
        commandObjectParams.Add("Log.FileLevel");
        commandObjectParams.Add("Log.FileName");
        commandObjectParams.Add("Program.SuppressBanner");
        commandObjectParams.Add("Program.PasswordEscape");
        foreach (var commandObject in commandObjects)
        {
            if (commandObject is Command commandObject2)
            {
                foreach (var p in commandObject2.Help.Parameters) { commandObjectParams.Add(commandObject.Name + "." + p.p1); }
            }
        }

        return commandObjectParams;
    }

    public string LogFileLevel => this["Log.FileLevel"] ?? "info";
    public string LogFileName => this["Log.FileName"];
    public string ProgramSuppressBanner => this["Program.SuppressBanner"] ?? "false";
    public string ProgramPasswordEscape => this["Program.PasswordEscape"] ?? "~^`";

    public static void CreateDefaultPropertiesFile()
    {
        var programFile = ProgramFullPathName;
        if (programFile == null) throw new Exception("Could not determine current program location");

        var propFile = FullPathName;
        if (File.Exists(propFile)) return;

        var commandObjectParams = GetAllKeys();
        var sb = new StringBuilder();
        foreach (var commandObjectParam in commandObjectParams) sb.AppendLine(commandObjectParam + "=");

        Util.FileWrite(propFile, sb.ToString(), Constant.ENCODING_UTF8);
    }
}
