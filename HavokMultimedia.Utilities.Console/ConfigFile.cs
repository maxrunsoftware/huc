﻿/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HavokMultimedia.Utilities.Console
{
    public class ConfigFile : IBucketReadOnly<string, string>
    {
        private static readonly ILogger log = Program.LOGFACTORY.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string Name => Path.GetFileName(FullPathName);
        public static string FullPathName => ProgramFullPathName + ".properties";
        private static string ProgramFullPathName
        {
            get
            {
                using (var processModule = Process.GetCurrentProcess().MainModule)
                {
                    return Path.GetFullPath(processModule?.FileName);
                }
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
                    return v;
                }
                return null;
            }
        }

        public ConfigFile(string fileName)
        {
            try
            {
                var props = new JavaProperties();
                var data = Util.FileRead(fileName, Constant.ENCODING_UTF8_WITHOUT_BOM);
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
            catch (Exception e)
            {
                log.Warn("Could not load config file", e);
            }
        }

        public ConfigFile() : this(FullPathName)
        {

        }


        public static List<string> GetAllKeys()
        {
            var cmdobjs = Program.GetCommandObjects();
            var cmdobjsParams = new List<string>();
            foreach (var cmdobj in cmdobjs)
            {
                if (cmdobj is Command cmdobj2)
                {
                    foreach (var p in cmdobj2.Help.parameters)
                    {
                        cmdobjsParams.Add(cmdobj.Name + "." + p.p1);
                    }
                }
            }
            return cmdobjsParams;
        }


        public static void CreateDefaultPropertiesFile()
        {
            var programFile = ProgramFullPathName;
            if (programFile == null) throw new Exception("Could not determine current program location");
            var propFile = FullPathName;
            if (File.Exists(propFile)) return;
            var cmdobjsParams = GetAllKeys();
            var sb = new StringBuilder();
            foreach (var cmdobjsParam in cmdobjsParams) sb.AppendLine(cmdobjsParam + "=");
            Util.FileWrite(propFile, sb.ToString(), Constant.ENCODING_UTF8_WITHOUT_BOM);

        }

    }
}