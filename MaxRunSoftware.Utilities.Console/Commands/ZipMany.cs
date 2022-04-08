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
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class ZipMany : Command
    {
        private readonly object locker = new object();
        private readonly int processTotal = 0;
        private int processCurrent = 0;
        private int bufferSize;

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Multiple file compression");
            help.AddParameter(nameof(bufferSizeMegabytes), "b", "Buffer size in megabytes (10)");
            help.AddParameter(nameof(compressionLevel), "l", "Compression level 0-9, 9 being the highest level of compression (9)");
            help.AddParameter(nameof(delete), "d", "Whether to delete the uncompressed file after compression completes (false)");
            help.AddParameter(nameof(threads), "t", "Number of files to process at one time (total # of logical processors - 1)");
            help.AddValue("<included item 1> <included item 2> <etc>");
        }

        private int bufferSizeMegabytes;
        private int compressionLevel;
        private bool delete;
        private int threads;

        private void ProcessFile(string inputFile)
        {
            try
            {
                var outputFile = inputFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? inputFile + ".zip" : Util.FileChangeExtension(inputFile, "zip");

                // shouldn't happen but sanity check anyways
                if (string.Equals(inputFile, outputFile, StringComparison.OrdinalIgnoreCase)) throw new Exception("Input file same as output file " + inputFile);

                DeleteExistingFile(outputFile);

                var currentCount = -1;
                lock (locker)
                {
                    currentCount = processCurrent++;
                }
                var runningCount = "[" + Util.FormatRunningCount(currentCount, processTotal) + "]  ";

                log.Debug(runningCount + inputFile + "  -->  " + outputFile + "  [starting]");
                lock (locker)
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

        protected override void ExecuteInternal()
        {
            bufferSizeMegabytes = GetArgParameterOrConfigInt(nameof(bufferSizeMegabytes), "b", 10);
            bufferSize = bufferSizeMegabytes * (int)Constant.BYTES_MEGA;

            compressionLevel = GetArgParameterOrConfigInt(nameof(compressionLevel), "l", 9);
            if (compressionLevel < 0) compressionLevel = 0;
            if (compressionLevel > 9) compressionLevel = 9;
            log.DebugParameter(nameof(compressionLevel), compressionLevel);

            delete = GetArgParameterOrConfigBool(nameof(delete), "d", false);

            threads = GetArgParameterOrConfigInt(nameof(threads), "t", Environment.ProcessorCount - 1);

            var inputFiles = GetArgValuesTrimmed();
            log.Debug(inputFiles, nameof(inputFiles));
            inputFiles = ParseInputFiles(inputFiles);
            log.Debug(inputFiles, nameof(inputFiles));
            if (inputFiles.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(inputFiles));

            using (var tp = new ConsumerThreadPool<string>(ProcessFile))
            {
                foreach (var fileToZip in inputFiles) tp.AddWorkItem(fileToZip);
                tp.FinishedAddingWorkItems();
                tp.NumberOfThreads = threads;
                while (!tp.IsComplete)
                {
                    Thread.Sleep(100);
                }
            }

            log.Info("Completed processing " + inputFiles.Count + " files");
        }
    }
}
