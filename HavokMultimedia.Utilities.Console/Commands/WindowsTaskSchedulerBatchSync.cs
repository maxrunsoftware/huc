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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32.TaskScheduler;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WindowsTaskSchedulerBatchSync : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Syncs Windows Task Scheduler folder with a directory of batch files (.bat|.cmd)");
            help.AddParameter("host", "h", "Server hostname or IP");
            help.AddParameter("username", "u", "Server username");
            help.AddParameter("password", "p", "Server password");
            help.AddParameter("forceV1", "v1", "Server force version 1 task scheduler implementation (false)");
            help.AddParameter("taskUsername", "tu", "User account username to run the tasks as, SYSTEM, LOCALSERVICE, NETWORKSERVICE are valid values as well");
            help.AddParameter("taskPassword", "tp", "User account password to run the tasks as");
            help.AddParameter("taskFolder", "tf", "User account username to run the tasks as, SYSTEM, LOCALSERVICE, NETWORKSERVICE are valid values as well");
            help.AddValue("<folder to scan 1> <folder to scan 2> <etc>");
            help.AddDetail("Batch file formats are...");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync DAILY {hour}:{minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync MONDAY {hour}:{minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync HOURLY {minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync MONTHLY {dayOfMonth}:{hour}:{minute}");
            help.AddDetail("  :: WindowsTaskSchedulerBatchSync CRON <Minute> <Hour> <Day_of_the_Month> <Month_of_the_Year> <Day_of_the_Week>");
        }

        private class BatchFile
        {
            private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            private static readonly Func<byte[], string> HASHING_ALGORITHM = Util.GenerateHashSHA256;
            public string FilePath { get; }
            public string FileName { get; }
            public string TaskName { get; }
            public string Hash { get; }
            public IReadOnlyList<Trigger> Triggers { get; }

            public BatchFile(string file)
            {
                file.CheckNotNullTrimmed(nameof(file));
                FilePath = Path.GetFullPath(file);
                FileName = Path.GetFileName(FilePath);
                TaskName = Path.GetFileNameWithoutExtension(FilePath);

                var filetext = Util.FileRead(FilePath, Utilities.Constant.ENCODING_UTF8_WITHOUT_BOM).TrimOrNull() ?? string.Empty;
                var fileLines = filetext.SplitOnNewline().TrimOrNull().ToList();

                var triggers = new List<Trigger>();

                var hashList = new List<string[]>();
                var sb = new StringBuilder();
                for (var i = 0; i < fileLines.Count; i++)
                {
                    var fileLineParts = (fileLines[i].OrEmpty()).SplitOnWhiteSpace().TrimOrNull().WhereNotNull().ToArray();

                    if (!fileLineParts.EqualsAtAny(0, StringComparer.OrdinalIgnoreCase, "REM", "::")) continue;
                    fileLineParts = fileLineParts.RemoveHead();

                    if (!fileLineParts.EqualsAt(0, StringComparer.OrdinalIgnoreCase, nameof(WindowsTaskSchedulerBatchSync))) continue;
                    fileLineParts = fileLineParts.RemoveHead();

                    var logHeader = FilePath + " [" + (i + 1) + "]: ";
                    try
                    {
                        var hashCopy = fileLineParts.Copy();
                        var triggersOfLine = ParseDirective(fileLineParts, logHeader);
                        if (triggersOfLine.Count > 0)
                        {
                            triggers.AddRange(triggersOfLine);
                            hashList.Add(hashCopy);
                        }
                    }
                    catch (Exception e)
                    {
                        log.Warn(logHeader + e.Message, e);
                    }
                }

                Triggers = triggers.AsReadOnly();
                Hash = HashDirectiveLines(hashList);
            }

            private static string HashDirectiveLines(IEnumerable<string[]> lineTokensEnumerable)
            {
                var sb = new StringBuilder();
                foreach (var line in lineTokensEnumerable)
                {
                    var l = string.Join(" ", line.OrEmpty()).TrimOrNull();
                    if (l != null) sb.Append(l + Utilities.Constant.NEWLINE_WINDOWS);
                }
                return HASHING_ALGORITHM(Utilities.Constant.ENCODING_UTF8_WITHOUT_BOM.GetBytes(sb.ToString()));
            }

            private List<Trigger> ParseDirective(string[] directiveParts, string fileAndLine)
            {
                var triggers = new List<Trigger>();
                var directive = directiveParts.GetAtIndexOrDefault(0).TrimOrNullUpper();
                if (directive == null) return triggers;
                directiveParts = directiveParts.RemoveHead();

                (byte hour, byte minute) parseTimeHHMM(string time)
                {
                    log.Trace($"Parsing hours and minutes from {time}");
                    var timeparts = time.Split(':').TrimOrNull().WhereNotNull().ToArray();
                    if (timeparts.Length < 2) throw new Exception($"{fileAndLine} Error creating {directive} triggers from value {time}. Invalid time format. Expected : character in time {time}");
                    if (timeparts.Length > 2) throw new Exception($"{fileAndLine} Error creating {directive} triggers from value {time}. Invalid time format. Encountered multiple : characters in time {time} but only 1 is allowed seperating hours and minutes");
                    var hours = timeparts[0].ToByte();
                    var mins = timeparts[1].ToByte();
                    log.Trace($"Parsed {hours} hours and {mins} minutes from {time}");
                    return (hours, mins);
                }

                (byte dayOfMonth, byte hour, byte minute) parseTimeDayOfMonthHHMM(string time)
                {
                    log.Trace($"Parsing dayOfMonth, hours, and minutes from {time}");
                    var timeparts = time.Split(':').Concat("00:00".Split(':')).TrimOrNull().WhereNotNull().ToArray();
                    if (timeparts.Length < 3) throw new Exception($"{fileAndLine} Error creating {directive} triggers from value {time}. Invalid time format. Expected a day of month specified. Format dayOfMonth:HH:MM");
                    var dayOfMonth = timeparts[0].ToByte();
                    var hour = timeparts[1].ToByte();
                    var minute = timeparts[2].ToByte();
                    log.Trace($"Parsed {dayOfMonth} day of month, {hour} hour, and {minute} minute from {time}");
                    return (dayOfMonth, hour, minute);
                }

                if (directive == "HOURLY")
                {
                    foreach (var time in directiveParts)
                    {
                        var minute = time.ToByte();
                        var minuteString = ("00" + minute).Right(2);

                        var t = WindowsTaskScheduler.CreateTriggerInterval(TimeSpan.FromMinutes(minute));
                        triggers.Add(t);
                        log.Debug($"{fileAndLine} {directive} created at times " + string.Join($":{minuteString}:00 ", Enumerable.Range(0, 24)) + " --> " + t);
                    }
                }

                if (directive == "CRON")
                {
                    var directiveLine = string.Join(" ", directiveParts).TrimOrNull();
                    if (directiveLine == null)
                    {
                        log.Warn($"{fileAndLine} CRON directive doesn't provide any details");
                    }
                    else
                    {
                        var cronTriggers = WindowsTaskScheduler.CreateTriggerCron(directiveLine);
                        triggers.AddRange(cronTriggers);
                        log.Warn($"{fileAndLine} {directive} created {triggers.Count} triggers");
                    }
                }

                if (directive == "DAILY")
                {
                    foreach (var time in directiveParts)
                    {
                        var (hour, minute) = parseTimeHHMM(time);
                        var t = WindowsTaskScheduler.CreateTriggerDaily(hour: hour, minute: minute);
                        triggers.Add(t);
                        log.Debug($"{fileAndLine} {directive} created at time {time}");
                    }
                }

                if (directive.In(Util.GetEnumItems<DayOfWeek>().Select(o => o.ToString().ToUpper())))
                {
                    foreach (var time in directiveParts)
                    {
                        var (hour, minute) = parseTimeHHMM(time);
                        var dow = Util.GetEnumItem<DayOfWeek>(directive);
                        var t = WindowsTaskScheduler.CreateTriggerWeekly(dow.Yield(), hour: hour, minute: minute);
                        triggers.Add(t);
                        log.Debug($"{fileAndLine} {directive} created at time {time}");
                    }
                }

                if (directive == "MONTHLY")
                {
                    foreach (var time in directiveParts)
                    {
                        var (dayOfMonth, hour, minute) = parseTimeDayOfMonthHHMM(time);
                        var t = WindowsTaskScheduler.CreateTriggerMonthly(dayOfMonth, hour: hour, minute: minute);
                        triggers.Add(t);
                        log.Debug($"{fileAndLine} {directive} created for {time}");
                    }
                }

                if (triggers.Count == 0) log.Warn($"{fileAndLine} {directive} No triggers created");

                return triggers;
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

                var batchFilesInDir = Util.FileListFiles(dir).Where(o => BatchFile.IsBatchFile(o)).Select(o => new BatchFile(o)).ToList();
                log.Debug($"Found {batchFilesInDir.Count} batch files in directory {dir}");

                batchFilesInDir = batchFilesInDir.Where(o => o.Triggers.Count > 0).ToList();
                log.Debug($"Found {batchFilesInDir.Count} batch files containing {nameof(WindowsTaskSchedulerBatchSync)} triggers in directory {dir}");

                foreach (var batchFile in batchFilesInDir) batchFiles.Add(batchFile);
            }

            var potentialTaskNameCollisions = new SortedDictionary<string, List<BatchFile>>(StringComparer.OrdinalIgnoreCase);
            foreach (var batchFile in batchFiles)
            {
                if (!potentialTaskNameCollisions.TryGetValue(batchFile.TaskName, out var list)) potentialTaskNameCollisions.Add(batchFile.TaskName, list = new List<BatchFile>());
                list.Add(batchFile);
            }

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

            Debug.Assert(potentialTaskNameCollisions.Values.All(o => o.Count == 1), "Expecting all Tasks to have only 1 batch file at this point");
            batchFiles = potentialTaskNameCollisions.Values.Select(o => o.First()).OrderBy(o => o.TaskName, StringComparer.OrdinalIgnoreCase).ToList();

            return batchFiles;
        }

        private void TaskCreate(WindowsTaskScheduler scheduler, string[] path, BatchFile batchFile, string taskUsername, string taskPassword)
        {
            try
            {
                scheduler.TaskAdd(path, batchFile.TaskName, batchFile.FilePath, batchFile.Triggers
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

        protected override void Execute()
        {
            #region Initialization


            var h = GetArgParameterOrConfigRequired("host", "h").TrimOrNull();

            var u = GetArgParameterOrConfigRequired("username", "u").TrimOrNull();

            var p = GetArgParameterOrConfigRequired("password", "p").TrimOrNull();

            var v1 = GetArgParameterOrConfigBool("forceV1", "v1", false);

            var tu = GetArgParameterOrConfigRequired("taskUsername", "tu").TrimOrNull();
            log.Debug($"taskUsername: {tu}");
            var tp = GetArgParameterOrConfig("taskPassword", "tp").TrimOrNull();
            log.Debug($"taskPassword: {tp}");
            var taskUsernameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SYSTEM", WindowsTaskScheduler.USER_SYSTEM },
                { "LOCALSERVICE", WindowsTaskScheduler.USER_LOCALSERVICE },
                { "NETWORKSERVICE", WindowsTaskScheduler.USER_NETWORKSERVICE }
            };
            if (tu != null && taskUsernameMapping.TryGetValue(tu, out var taskUsernameMappingValue))
            {
                tu = taskUsernameMappingValue;
                tp = null;
                log.Debug($"taskUsername: {tu}");
                log.Debug($"taskPassword: {tp}");
            }

            var tf = GetArgParameterOrConfigRequired("taskFolder", null).TrimOrNull();
            log.Debug($"taskFolder: {tf}");
            var taskSchedulerFolderPath = Util.PathParse(tf, WindowsTaskScheduler.PATH_PARSE_CHARACTERS);
            log.Debug($"taskFolderPath: {taskSchedulerFolderPath}");

            var foldersToScan = GetArgValues().OrEmpty().TrimOrNull().WhereNotNull().ToList();
            if (foldersToScan.IsEmpty()) throw new ArgsException("foldersToScan", "No folders to scan specified");
            for (var i = 0; i < foldersToScan.Count; i++)
            {
                foldersToScan[i] = Path.GetFullPath(foldersToScan[i]);
            }
            log.Debug("foldersToScan: " + string.Join(", ", foldersToScan));
            foreach (var dir in foldersToScan) if (!Directory.Exists(dir)) throw new DirectoryNotFoundException("Cannot find directory " + dir);

            #endregion Initialization

            var batchFiles = GetBatchFiles(foldersToScan);

            using (var scheduler = new WindowsTaskScheduler(h, u, p, forceV1: v1))
            {
                var currentDirectory = scheduler.GetTaskFolder(taskSchedulerFolderPath, true);
                taskSchedulerFolderPath = WindowsTaskScheduler.ParsePath(currentDirectory);
                log.Debug($"Using task folder {Util.PathToString(taskSchedulerFolderPath)}");

                var existingTasks = new SortedDictionary<string, Task>();
                foreach (var t in currentDirectory.Tasks) existingTasks.Add(t.Name, t);

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
                    log.Info("Deleting task " + Util.PathToString(WindowsTaskScheduler.ParsePath(task.Folder).Concat(task.Name)));
                    scheduler.TaskDelete(task);
                }

                foreach (var batchFile in tasksToCreate)
                {
                    TaskCreate(scheduler, taskSchedulerFolderPath, batchFile, tu, tp);
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
                        log.Info("Recreating task " + Util.PathToString(WindowsTaskScheduler.ParsePath(task.Folder).Concat(task.Name)));
                        scheduler.TaskDelete(task);
                        TaskCreate(scheduler, taskSchedulerFolderPath, batchFile, tu, tp);
                    }
                }
            }
        }
    }

}
