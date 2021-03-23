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
using System.IO;
using System.Linq;
using System.Text;
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
            help.AddValue("<output zip file> <included item 1> <included item 2> <etc>");

        }




        protected override void Execute()
        {
            var bs = GetArgParameterOrConfigInt("bufferSizeMegabytes", "b", 10) ;
            bs = bs * (int)Constant.BYTES_MEGA;

            var r = GetArgParameterOrConfigBool("recursive", "r", false);

            var m = GetArgParameterOrConfig("mask", "m", "*");

            var l = GetArgParameterOrConfigInt("compressionLevel", "l", 9);
            if (l < 0) l = 0;
            if (l > 9) l = 9;
            log.Debug($"compressionLevel: {l}");

            var s = GetArgParameterOrConfigBool("skipTopLevelDirectory", "s", false);

            var values = new Queue<string>(GetArgValues().OrEmpty().TrimOrNull().WhereNotNull());
            var outputFileString = values.DequeueOrDefault();
            log.Debug($"outputFileString: {outputFileString}");
            var outputFile = Path.GetFullPath(outputFileString);
            log.Debug($"outputFile: {outputFile}");
            if (outputFile == null) throw new ArgsException("outputFile", "No <outputFile> specified");

            var inputFileStrings = values.ToList();
            log.Debug($"inputFileStrings: " + string.Join(", ", inputFileStrings));
            var inputFiles = Util.ParseInputFiles(inputFileStrings);
            for (var i = 0; i < inputFiles.Count; i++) log.Debug($"inputFile[{i}]: {inputFiles[i]}");
            if (inputFiles.IsEmpty()) throw new ArgsException("inputFiles", "No <inputFiles> specified or no files exist");

            DeleteExistingFile(outputFile);


            using (var fs = Util.FileOpenWrite(outputFile))
            {
                using (var zos = new ZipOutputStream(fs, bs))
                {
                    zos.SetLevel(l);

                    foreach (var includedItem in inputFiles)
                    {
                        if (!File.Exists(includedItem) && !Directory.Exists(includedItem))
                        {
                            throw new FileNotFoundException($"File or directory to compress not found {includedItem}", includedItem);
                        }
                        if (Util.IsFile(includedItem))
                        {
                            var fi = new FileInfo(includedItem);
                            External.Zip.AddFileToZip(fi, fi.Directory, zos, bs, Path.GetFileName(outputFile));
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
                                    External.Zip.AddDirectoryToZip((DirectoryInfo)item.GetFileSystemInfo(), basePath, zos, Path.GetFileName(outputFile));
                                    continue;
                                }

                                if (!item.IsDirectory)
                                {
                                    var fi = (FileInfo)item.GetFileSystemInfo();
                                    if (!fi.Name.EqualsWildcard(m))
                                    {
                                        continue;
                                    }
                                    External.Zip.AddFileToZip(fi, basePath, zos, bs, Path.GetFileName(outputFile));
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