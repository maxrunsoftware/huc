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
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ZipMany : Command
    {
        private readonly int processTotal = 0;
        private bool delete;
        private int processCurrent = 0;
        private int compressionLevel;
        private int bufferSize;

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Multiple file compression");
            help.AddParameter("bufferSizeMegabytes", "b", "Buffer size in megabytes (10)");
            help.AddParameter("compressionLevel", "l", "Compression level 0-9, 9 being the highest level of compression (9)");
            help.AddParameter("delete", "d", "Whether to delete the uncompressed file after compression completes (false)");
            help.AddParameter("threads", "t", "Number of files to process at one time (total # of logical processors - 1)");
            help.AddValue("<included item 1> <included item 2> <etc>");
        }

        private void ProcessFile(string inputFile)
        {
            try
            {
                var outputFile = inputFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? inputFile + ".zip" : Util.FileChangeExtension(inputFile, "zip");
                if (string.Equals(inputFile, outputFile, StringComparison.OrdinalIgnoreCase)) throw new Exception("Input file same as output file " + inputFile);

                DeleteExistingFile(outputFile);

                var currentCount = -1;
                lock (this) currentCount = processCurrent++;
                var runningCount = "[" + Util.FormatRunningCount(currentCount, processTotal) + "]  ";

                log.Debug(runningCount + inputFile + "  -->  " + outputFile + "  [starting]");
                lock (this)
                {
                    log.Info(runningCount + Path.GetFileName(inputFile) + "  -->  " + Path.GetFileName(outputFile));
                }

                using (var fs = Util.FileOpenWrite(outputFile))
                {
                    using (var zos = new ZipOutputStream(fs, bufferSize))
                    {
                        zos.SetLevel(compressionLevel);

                        if (!File.Exists(inputFile) && !Directory.Exists(inputFile))
                        {
                            throw new FileNotFoundException($"File or directory to compress not found {inputFile}", inputFile);
                        }
                        if (Util.IsFile(inputFile))
                        {
                            var fi = new FileInfo(inputFile);
                            External.Zip.AddFileToZip(fi, fi.Directory, zos, bufferSize, Path.GetFileName(outputFile));
                        }
                        else
                        {
                            // Directory
                            log.Warn("Ignoring directory " + inputFile);
                        }

                        zos.IsStreamOwner = true; // Makes the Close also Close the underlying stream
                        zos.Flush();
                        fs.Flush();
                        zos.Close();
                    }
                }

                log.Debug(runningCount + inputFile + "  -->  " + outputFile + "  [complete]");

                if (delete) DeleteExistingFile(inputFile);
            }
            catch (Exception e)
            {
                log.Error("Failed compressing file " + inputFile, e);
            }
        }

        protected override void Execute()
        {
            bufferSize = GetArgParameterOrConfigInt("bufferSizeMegabytes", "b", 10);
            bufferSize = bufferSize * (int)Constant.BYTES_MEGA;

            compressionLevel = GetArgParameterOrConfigInt("compressionLevel", "l", 9);
            if (compressionLevel < 0) compressionLevel = 0;
            if (compressionLevel > 9) compressionLevel = 9;
            log.Debug($"compressionLevel: {compressionLevel}");

            delete = GetArgParameterOrConfigBool("delete", "d", false);

            var t = GetArgParameterOrConfigInt("threads", "t", Environment.ProcessorCount - 1);

            var inputFileStrings = GetArgValuesTrimmed();
            log.Debug($"inputFileStrings: " + string.Join(", ", inputFileStrings));
            var inputFiles = Util.ParseInputFiles(inputFileStrings);
            for (var i = 0; i < inputFiles.Count; i++) log.Debug($"inputFile[{i}]: {inputFiles[i]}");
            if (inputFiles.IsEmpty()) throw new ArgsException("inputFiles", "No <inputFiles> specified");

            using (var tp = new ConsumerThreadPool<string>(ProcessFile))
            {
                foreach (var fileToZip in inputFiles) tp.AddWorkItem(fileToZip);
                tp.FinishedAddingWorkItems();
                tp.NumberOfThreads = t;
                while (!tp.IsComplete)
                {
                    Thread.Sleep(100);
                }
            }

            log.Info("Completed processing " + inputFiles.Count + " files");
        }
    }
}
