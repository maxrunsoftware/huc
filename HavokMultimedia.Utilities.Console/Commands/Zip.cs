/*
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
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Zip : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("File compression");
            help.AddParameter("bufferSizeMegabytes", "b", "Buffer size in megabytes (10)");
            help.AddParameter("recursive", "r", "Whether to include subdirectory files or not (false)");
            help.AddParameter("mask", "m", "Mask filter to apply to file list (*)");
            help.AddParameter("compressionLevel", "l", "Compression level 0-9, 9 being the highest level of compression (9)");
            help.AddParameter("skipTopLevelDirectory", "s", "Whether to not include the top level directory or rather include all the files under that directory instead (false)");
            help.AddParameter("password", "p", "Password on the ZIP file");
            help.AddValue("<output zip file> <included item 1> <included item 2> <etc>");
            help.AddExample("myOuputFile.zip someLocalFile.txt");
            help.AddExample("myOuputFile.zip *.txt *.csv");
        }

        protected override void ExecuteInternal()
        {
            var bs = GetArgParameterOrConfigInt("bufferSizeMegabytes", "b", 10);
            bs = bs * (int)Constant.BYTES_MEGA;

            var r = GetArgParameterOrConfigBool("recursive", "r", false);

            var m = GetArgParameterOrConfig("mask", "m", "*");

            var l = GetArgParameterOrConfigInt("compressionLevel", "l", 9);
            if (l < 0) l = 0;
            if (l > 9) l = 9;
            log.Debug($"compressionLevel: {l}");

            var s = GetArgParameterOrConfigBool("skipTopLevelDirectory", "s", false);

            var password = GetArgParameterOrConfig("password", "p").TrimOrNull();

            var values = GetArgValuesTrimmed1N();
            var outputFileString = values.firstValue;
            log.Debug($"outputFileString: {outputFileString}");
            var outputFile = Path.GetFullPath(outputFileString);
            log.Debug($"outputFile: {outputFile}");
            if (outputFile == null) throw new ArgsException("outputFile", "No <outputFile> specified");

            var inputFileStrings = values.otherValues;
            log.Debug(inputFileStrings, nameof(inputFileStrings));
            var inputFiles = new List<string>();
            foreach (var inputFileString in inputFileStrings)
            {
                var ifs = Path.GetFullPath(inputFileString);
                if (ifs.ContainsAny("*", "?")) inputFiles.AddRange(ParseFileName(ifs, r));
                else inputFiles.Add(ifs);
            }
            log.Debug(inputFiles, nameof(inputFiles));
            if (inputFiles.IsEmpty()) throw new ArgsException(nameof(inputFiles), $"No <{nameof(inputFiles)}> specified or no files exist");

            DeleteExistingFile(outputFile);


            using (var fs = Util.FileOpenWrite(outputFile))
            {
                using (var zos = new ZipOutputStream(fs, bs))
                {
                    zos.SetLevel(l);
                    if (password != null)
                    {
                        // https://stackoverflow.com/a/31722359
                        zos.Password = password;
                        zos.UseZip64 = UseZip64.On;
                    }

                    foreach (var includedItem in inputFiles)
                    {
                        if (!File.Exists(includedItem) && !Directory.Exists(includedItem))
                        {
                            throw new FileNotFoundException($"File or directory to compress not found {includedItem}", includedItem);
                        }
                        if (Util.IsFile(includedItem))
                        {
                            var fi = new FileInfo(includedItem);
                            External.Zip.AddFileToZip(fi, fi.Directory, zos, bs, Path.GetFileName(outputFile), encrypt: password != null);
                        }
                        else
                        {
                            // Directory
                            var basePath = new DirectoryInfo(includedItem);
                            var items = Util.FileList(includedItem, recursive: r);
                            if (!r) items = items.Where(o => !o.IsDirectory || string.Equals(o.Path, includedItem, StringComparison.OrdinalIgnoreCase));
                            if (s) items = items.Where(o => !string.Equals(o.Path, includedItem, StringComparison.OrdinalIgnoreCase));
                            if (!s) basePath = basePath.Parent;
                            items = items.OrderBy(o => StringComparer.OrdinalIgnoreCase);
                            foreach (var item in items)
                            {
                                if (item.Exception != null)
                                {
                                    log.Warn($"ERROR: {item.Path} --> {item.Exception.Message}", item.Exception);
                                    continue;
                                }

                                if (item.IsDirectory)
                                {
                                    External.Zip.AddDirectoryToZip((DirectoryInfo)item.GetFileSystemInfo(), basePath, zos, Path.GetFileName(outputFile), encrypt: password != null);
                                    continue;
                                }

                                if (!item.IsDirectory)
                                {
                                    var fi = (FileInfo)item.GetFileSystemInfo();
                                    if (!fi.Name.EqualsWildcard(m))
                                    {
                                        continue;
                                    }
                                    External.Zip.AddFileToZip(fi, basePath, zos, bs, Path.GetFileName(outputFile), encrypt: password != null);
                                }
                            }

                            //var items = itemsE.ToList();
                        }
                    }

                    zos.IsStreamOwner = true; // Makes the Close also Close the underlying stream
                    zos.Flush();
                    fs.Flush();
                    zos.Close();
                }
            }
        }
    }
}
