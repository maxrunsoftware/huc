/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class Zip : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("File compression");
            help.AddParameter(nameof(bufferSizeMegabytes), "b", "Buffer size in megabytes (10)");
            help.AddParameter(nameof(recursive), "r", "Whether to include subdirectory files or not (false)");
            help.AddParameter(nameof(mask), "m", "Mask filter to apply to file list (*)");
            help.AddParameter(nameof(compressionLevel), "l", "Compression level 0-9, 9 being the highest level of compression (9)");
            help.AddParameter(nameof(skipTopLevelDirectory), "s", "Whether to not include the top level directory or rather include all the files under that directory instead (false)");
            help.AddParameter(nameof(password), "p", "Password on the ZIP file");
            help.AddValue("<output zip file> <included item 1> <included item 2> <etc>");
            help.AddExample("myOuputFile.zip someLocalFile.txt");
            help.AddExample("myOuputFile.zip *.txt *.csv");
        }

        private int bufferSizeMegabytes;
        private bool recursive;
        private string mask;
        private int compressionLevel;
        private bool skipTopLevelDirectory;
        private string password;

        protected override void ExecuteInternal()
        {
            bufferSizeMegabytes = GetArgParameterOrConfigInt(nameof(bufferSizeMegabytes), "b", 10);
            int bufferSize = bufferSizeMegabytes * (int)Constant.BYTES_MEGA;
            log.DebugParameter(nameof(bufferSize), bufferSize);

            recursive = GetArgParameterOrConfigBool(nameof(recursive), "r", false);
            mask = GetArgParameterOrConfig(nameof(mask), "m", "*");

            compressionLevel = GetArgParameterOrConfigInt(nameof(compressionLevel), "l", 9);
            if (compressionLevel < 0) compressionLevel = 0;
            if (compressionLevel > 9) compressionLevel = 9;
            log.DebugParameter(nameof(compressionLevel), compressionLevel);

            skipTopLevelDirectory = GetArgParameterOrConfigBool(nameof(skipTopLevelDirectory), "s", false);

            password = GetArgParameterOrConfig(nameof(password), "p").TrimOrNull();

            var values = GetArgValuesTrimmed1N();
            var outputFile = values.firstValue;
            outputFile.CheckValueNotNull(nameof(outputFile), log);
            outputFile = Path.GetFullPath(outputFile);
            outputFile.CheckValueNotNull(nameof(outputFile), log);

            var inputFiles = new List<string>();
            foreach (var inputFile in values.otherValues)
            {
                if (inputFile.ContainsAny("*", "?")) inputFiles.AddRange(ParseFileName(inputFile, recursive));
                else inputFiles.Add(Path.GetFullPath(inputFile));
            }
            log.Debug(inputFiles, nameof(inputFiles));
            if (inputFiles.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(inputFiles));

            // check to be sure all of the files or directories exist
            foreach (var includedItem in inputFiles)
            {
                if (!File.Exists(includedItem) && !Directory.Exists(includedItem))
                {
                    throw new FileNotFoundException($"File or directory to compress not found {includedItem}", includedItem);
                }
            }

            DeleteExistingFile(outputFile);
            var isWindows = Constant.OS_WINDOWS;

            using (var fs = Util.FileOpenWrite(outputFile))
            {
                using (var zos = new ZipOutputStream(fs, bufferSize))
                {
                    zos.SetLevel(compressionLevel);
                    if (password != null)
                    {
                        // https://stackoverflow.com/a/31722359
                        zos.Password = password;
                        zos.UseZip64 = UseZip64.On;
                    }

                    foreach (var includedItem in inputFiles)
                    {
                        if (Util.IsFile(includedItem))
                        {
                            var fi = new FileInfo(includedItem);
                            External.Zip.AddFileToZip(fi, fi.Directory, zos, Path.GetFileName(outputFile), encrypt: password != null);
                        }
                        else // Directory
                        {
                            var basePath = new DirectoryInfo(includedItem);
                            var items = Util.FileList(includedItem, recursive: recursive);
                            if (!recursive) items = items.Where(o => !o.IsDirectory || string.Equals(o.Path, includedItem, isWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
                            if (skipTopLevelDirectory) items = items.Where(o => !string.Equals(o.Path, includedItem, isWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
                            if (!skipTopLevelDirectory) basePath = basePath.Parent;
                            items = items.OrderBy(o => isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                            foreach (var item in items)
                            {
                                if (item.Exception != null)
                                {
                                    log.Warn($"ERROR: {item.Path} --> {item.Exception.Message}", item.Exception);
                                }
                                else if (item.IsDirectory)
                                {
                                    External.Zip.AddDirectoryToZip((DirectoryInfo)item.GetFileSystemInfo(), basePath, zos, Path.GetFileName(outputFile), encrypt: password != null);
                                }
                                else // IsFile
                                {
                                    var fi = (FileInfo)item.GetFileSystemInfo();
                                    if (fi.Name.EqualsWildcard(mask))
                                    {
                                        External.Zip.AddFileToZip(fi, basePath, zos, Path.GetFileName(outputFile), encrypt: password != null);
                                    }
                                }
                            }

                            //var items = itemsE.ToList();
                        }
                    }

                    zos.IsStreamOwner = true; // Makes the Close also Close the underlying stream
                    //zos.CloseEntry();
                    zos.Flush();
                    fs.Flush();
                    zos.Close();
                }
            }
        }
    }
}
