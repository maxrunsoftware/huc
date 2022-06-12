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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32.TaskScheduler;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class WindowsTaskSchedulerBatchSync : WindowsTaskSchedulerBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Syncs Windows Task Scheduler folder with a directory of batch files (.bat|.cmd)");
            help.AddParameter(nameof(taskUsername), "tu", "User account username to run the tasks as, SYSTEM, LOCALSERVICE, NETWORKSERVICE are valid values as well");
            help.AddParameter(nameof(taskPassword), "tp", "User account password to run the tasks as");
            help.AddParameter(nameof(taskFolder), "tf", "Folder in task scheduler to put the tasks");
            help.AddParameter(nameof(forceRebuild), "fr", "Forces a delete of all tasks in task folder and then recreates them all (false)");
            help.AddParameter(nameof(batchKeyword), "bk", "The keyword to use when scanning batch files to detect whether to process the line (" + nameof(WindowsTaskSchedulerBatchSync) + ")");
            help.AddValue("<folder to scan 1> <folder to scan 2> <etc>");
            help.AddDetail("Batch file formats are...");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync DAILY {hour}:{minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync MONDAY {hour}:{minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync HOURLY {minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync MONTHLY {dayOfMonth}:{hour}:{minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync CRON <Minute> <Hour> <Day_of_the_Month> <Month_of_the_Year> <Day_of_the_Week>");
        }

        private string taskUsername;
        private string taskPassword;
        private string taskFolder;
        private bool forceRebuild;
        private string batchKeyword;

        private class BatchFile
        {
            private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            private static readonly Func<byte[], string> HASHING_ALGORITHM = Util.GenerateHashSHA256;
            public string FilePath { get; }
            public string FileName { get; }
            public string TaskName { get; }
            public string Hash { get; }
            public IReadOnlyList<Trigger> Triggers { get; }

            public BatchFile(string file, string batchKeyword)
            {
                file.CheckNotNullTrimmed(nameof(file));
                FilePath = Path.GetFullPath(file);
                FileName = Path.GetFileName(FilePath);
                TaskName = Path.GetFileNameWithoutExtension(FilePath);

                var fileText = Util.FileRead(FilePath, Utilities.Constant.ENCODING_UTF8).TrimOrNull() ?? string.Empty;
                var fileLines = fileText.SplitOnNewline().TrimOrNull().ToList();

                var triggers = new List<Trigger>();

                var hashList = new List<string>();
                var sb = new StringBuilder();
                for (var i = 0; i < fileLines.Count; i++)
                {
                    var fileLineParts = (fileLines[i].OrEmpty()).SplitOnWhiteSpace().TrimOrNull().WhereNotNull().ToArray();

                    if (!fileLineParts.EqualsAtAny(0, StringComparer.OrdinalIgnoreCase, "REM", "::")) continue;
                    fileLineParts = fileLineParts.RemoveHead();

                    if (!fileLineParts.EqualsAt(0, StringComparer.OrdinalIgnoreCase, batchKeyword)) continue;
                    fileLineParts = fileLineParts.RemoveHead();

                    var logHeader = FilePath + " [" + (i + 1) + "]: ";
                    try
                    {
                        var triggerLine = fileLineParts.ToStringDelimited(" ");
                        var triggersOfLine = External.WindowsTaskSchedulerTrigger.CreateTriggers(triggerLine, logHeader).ToList();
                        if (triggersOfLine.IsNotEmpty())
                        {
                            triggers.AddRange(triggersOfLine);
                            hashList.Add(triggerLine);
                        }
                    }
                    catch (Exception e)
                    {
                        log.Warn(logHeader + e.Message, e);
                    }
                }

                Triggers = triggers.AsReadOnly();

                var hashValue = hashList.ToStringDelimited(Constant.NEWLINE_WINDOWS);
                Hash = HASHING_ALGORITHM(Constant.ENCODING_UTF8.GetBytes(hashValue));
            }

            public static bool IsBatchFile(string filename)
            {
                filename = filename.TrimOrNull();
                if (filename == null) return false;
                var ext = Path.GetExtension(filename).TrimOrNull();
                if (ext == null) return false;
                if (!ext.In(StringComparer.OrdinalIgnoreCase, "cmd", ".cmd", "bat", ".bat")) return false;
                if (Util.FileGetSize(filename) > (Utilities.Constant.BYTES_MEGA * 10L)) return false; // no batch file should be over 10MB
                return true;
            }
        }

        private List<BatchFile> GetBatchFiles(IEnumerable<string> directoriesToScan)
        {
            var batchFiles = new List<BatchFile>();
            foreach (var dir in directoriesToScan)
            {
                log.Debug($"Scanning directory {dir}");

                var batchFilesInDir = Util.FileListFiles(dir).Where(o => BatchFile.IsBatchFile(o)).Select(o => new BatchFile(o, batchKeyword)).ToList();
                log.Debug($"Found {batchFilesInDir.Count} batch files in directory {dir}");

                batchFilesInDir = batchFilesInDir.Where(o => o.Triggers.Count > 0).ToList();
                log.Debug($"Found {batchFilesInDir.Count} batch files containing {nameof(WindowsTaskSchedulerBatchSync)} triggers in directory {dir}");

                foreach (var batchFile in batchFilesInDir) batchFiles.Add(batchFile);
            }

            var potentialTaskNameCollisions = new SortedDictionary<string, List<BatchFile>>(StringComparer.OrdinalIgnoreCase);
            foreach (var batchFile in batchFiles) potentialTaskNameCollisions.AddToList(batchFile.TaskName, batchFile);

            foreach (var taskName in potentialTaskNameCollisions.Keys.ToList())
            {
                if (potentialTaskNameCollisions[taskName].Count == 1) continue;
                var l = potentialTaskNameCollisions[taskName];
                for (var i = 0; i < l.Count; i++)
                {
                    log.Warn($"Task name collision for task [{taskName}] {(i + 1)}/{l.Count} --> {l[i].FilePath}");
                }

                log.Warn($"Task [{taskName}] skipped because of task name collisions and will be removed if it exists in the task scheduler");
                potentialTaskNameCollisions.Remove(taskName);
            }

            if (!potentialTaskNameCollisions.Values.All(o => o.Count == 1)) throw new Exception("Expecting all Tasks to have only 1 batch file at this point");
            batchFiles = potentialTaskNameCollisions.Values.Select(o => o.First()).OrderBy(o => o.TaskName, StringComparer.OrdinalIgnoreCase).ToList();

            return batchFiles;
        }

        private void TaskCreate(WindowsTaskScheduler scheduler, WindowsTaskSchedulerPath path, BatchFile batchFile, string taskUsername, string taskPassword)
        {
            try
            {
                scheduler.TaskAdd(path.Add(batchFile.TaskName), batchFile.FilePath.Yield().ToArray(), batchFile.Triggers
                    , workingDirectory: Path.GetDirectoryName(batchFile.FilePath)
                    , description: batchFile.TaskName
                    , documentation: batchFile.Hash
                    , username: taskUsername
                    , password: taskPassword
                );
                log.Info($"Created Task with {batchFile.Triggers.Count} triggers from file {batchFile.FilePath}");
            }
            catch (Exception e)
            {
                var items = new List<string>
                {
                    "BatchFile:  " + batchFile.FilePath,
                    "TaskName:  " + batchFile.TaskName
                };
                for (var i = 0; i < batchFile.Triggers.Count; i++)
                {
                    items.Add("Trigger[" + Util.FormatRunningCount(i, batchFile.Triggers.Count) + "]:  " + batchFile.Triggers[i].ToString());
                }

                log.Error($"Failed to create Task with {batchFile.Triggers.Count} triggers from file {batchFile.FilePath}", e);
                log.Error(string.Join(System.Environment.NewLine + "  ", items) + System.Environment.NewLine);
            }
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            taskUsername = GetArgParameterOrConfigRequired(nameof(taskUsername), "tu").TrimOrNull();
            taskPassword = GetArgParameterOrConfig(nameof(taskPassword), "tp").TrimOrNull();
            if (RemapUsername(ref taskUsername)) taskPassword = null;
            log.DebugParameter(nameof(taskUsername), taskUsername);
            log.DebugParameter(nameof(taskPassword), taskPassword);

            forceRebuild = GetArgParameterOrConfigBool(nameof(forceRebuild), "fr", false);
            batchKeyword = GetArgParameterOrConfig(nameof(batchKeyword), "bk", nameof(WindowsTaskSchedulerBatchSync));

            taskFolder = GetArgParameterOrConfigRequired(nameof(taskFolder), "tf").TrimOrNull();
            log.DebugParameter(nameof(taskFolder), taskFolder);
            var taskSchedulerFolderPath = new WindowsTaskSchedulerPath(taskFolder);
            log.DebugParameter(nameof(taskSchedulerFolderPath), taskSchedulerFolderPath);

            var foldersToScan = GetArgValuesTrimmed();
            log.Debug(foldersToScan, nameof(foldersToScan));
            if (foldersToScan.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(foldersToScan));
            foldersToScan = foldersToScan.Select(o => Path.GetFullPath(o)).ToList();
            log.Debug(foldersToScan, nameof(foldersToScan));
            CheckDirectoryExists(foldersToScan);

            var batchFiles = GetBatchFiles(foldersToScan);
            using (var scheduler = GetTaskScheduler())
            {
                var currentDirectory = scheduler.CreateTaskFolder(taskSchedulerFolderPath);
                taskSchedulerFolderPath = currentDirectory.GetPath();
                log.Debug($"Using task folder {taskSchedulerFolderPath}");

                var existingTasks = new SortedDictionary<string, Task>();
                foreach (var t in currentDirectory.Tasks) existingTasks.Add(t.Name, t);

                if (forceRebuild)
                {
                    log.Debug("Forcing rebuild of " + currentDirectory.GetPath());
                    foreach (var t in currentDirectory.Tasks) scheduler.TaskDelete(t);
                    existingTasks.Clear();
                }

                var tasksToVerify = new List<Tuple<Task, BatchFile>>();
                var tasksToCreate = new List<BatchFile>();
                foreach (var batchFile in batchFiles)
                {
                    if (existingTasks.TryGetValue(batchFile.TaskName, out var task))
                    {
                        tasksToVerify.Add(Tuple.Create(task, batchFile));
                        existingTasks.Remove(batchFile.TaskName);
                    }
                    else
                    {
                        tasksToCreate.Add(batchFile);
                    }
                }
                var tasksToDelete = existingTasks.Values.ToList();

                log.Debug($"Tasks to create: {tasksToCreate.Count}");
                log.Debug($"Tasks to verify: {tasksToVerify.Count}");
                log.Debug($"Tasks to delete: {tasksToDelete.Count}");

                foreach (var task in tasksToDelete)
                {
                    log.Info("Deleting task " + task.GetPath());
                    scheduler.TaskDelete(task);
                }

                foreach (var batchFile in tasksToCreate)
                {
                    TaskCreate(scheduler, taskSchedulerFolderPath, batchFile, taskUsername, taskPassword);
                }

                foreach (var tuple in tasksToVerify)
                {
                    var task = tuple.Item1;
                    var batchFile = tuple.Item2;
                    var taskHash = task.Definition.RegistrationInfo.Documentation.TrimOrNull();
                    var batchFileHash = batchFile.Hash.TrimOrNull();

                    log.Debug($"Comparing existing task [{taskHash}] {task.Name} to batch file [{batchFileHash}] {batchFile.FilePath}");

                    if (taskHash == null && batchFileHash == null)
                    {
                        throw new NotImplementedException("Both taskHash and batchFileHash are null, not sure what to do here");
                    }
                    else if (taskHash == batchFileHash)
                    {
                        log.Debug($"Task {task.Name} and batchfile {batchFile.FilePath} are the same, ignoring");
                    }
                    else
                    {
                        log.Debug("Hashes are different, removing and recreating task");
                        log.Info("Recreating task " + task.GetPath());
                        scheduler.TaskDelete(task);
                        TaskCreate(scheduler, taskSchedulerFolderPath, batchFile, taskUsername, taskPassword);
                    }
                }
            }
        }
    }

}
