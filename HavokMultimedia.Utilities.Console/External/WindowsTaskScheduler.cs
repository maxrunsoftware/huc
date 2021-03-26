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
using System.Linq;
using Microsoft.Win32.TaskScheduler;

namespace HavokMultimedia.Utilities.Console.External
{
    public class WindowsTaskSchedulerPathComparer : ComparatorBase<string[]>
    {
        private readonly StringComparer stringComparer;
        public static readonly WindowsTaskSchedulerPathComparer OrdinalIgnoreCase = new WindowsTaskSchedulerPathComparer(StringComparer.OrdinalIgnoreCase);

        private WindowsTaskSchedulerPathComparer(StringComparer stringComparer) => this.stringComparer = stringComparer.CheckNotNull(nameof(stringComparer));

        public override int Compare(string[] x, string[] y) => Util.Compare(x, y, stringComparer);

        public override int GetHashCode(string[] obj) => Util.GenerateHashCodeFromCollection(obj);
    }

    public static class WindowsTaskSchedulerExtensions
    {
        public static string NameFull(this Task task)
        {
            return "/" + string.Join("/", NameFullParts(task));
        }

        public static string[] NameFullParts(this Task task)
        {

            var list = new List<string>();
            list.AddRange(PathParts(task));
            list.Add(task.Name);
            return list.ToArray();

        }

        public static bool IsMatchNameFull(this Task task, string pathAndName)
        {
            var mypath = NameFullParts(task);
            var pathParts = pathAndName.Split(new char[] { '/', '\\' }).TrimOrNull().WhereNotNull();
            if (mypath.Length != pathParts.Length) return false;
            for (int i = 0; i < mypath.Length; i++)
            {
                if (!mypath[i].EqualsCaseInsensitive(pathParts[i])) return false;
            }
            return true;
        }

        public static bool IsMatchPath(this Task task, string path)
        {
            var mypath = PathParts(task);
            var pathParts = path.Split(new char[] { '/', '\\' }).TrimOrNull().WhereNotNull();
            if (mypath.Length != pathParts.Length) return false;
            for (int i = 0; i < mypath.Length; i++)
            {
                if (!mypath[i].EqualsCaseInsensitive(pathParts[i])) return false;
            }
            return true;

        }

        public static string[] PathParts(this Task task) => (task.Folder?.Path ?? string.Empty).Split(new char[] { '/', '\\' }).TrimOrNull().WhereNotNull();
    }

    public class WindowsTaskScheduler : IDisposable
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly object locker = new object();
        private TaskService taskService;
        public static readonly IReadOnlyList<string> PATH_PARSE_CHARACTERS = (new string[] { "/", "\\" }).ToList().AsReadOnly();
        public static readonly string USER_SYSTEM = "NT AUTHORITY\\SYSTEM";
        public static readonly string USER_LOCALSERVICE = "NT AUTHORITY\\LOCALSERVICE";
        public static readonly string USER_NETWORKSERVICE = "NT AUTHORITY\\NETWORKSERVICE";

        public TaskService TaskService
        {
            get
            {
                lock (locker)
                {
                    if (taskService != null) return taskService;
                    throw new ObjectDisposedException(GetType().FullNameFormatted());
                }
            }
        }

        public WindowsTaskScheduler(string host, string username, string password, bool forceV1 = false)
        {
            string accountDomain = null;
            if (username != null)
            {
                var parts = username.Split('\\').TrimOrNull().WhereNotNull().ToArray();
                if (parts.Length > 1)
                {
                    accountDomain = parts[0];
                    username = parts[1];
                }
            }

            log.Debug($"Creating new {typeof(TaskService).FullNameFormatted()}(host: {host}, username: {username}, accountDomain: {accountDomain}, password: {password}, forceV1: {forceV1})");
            taskService = new TaskService(host, userName: username, accountDomain: accountDomain, password: password, forceV1: forceV1);
        }

        #region Helpers

        private static void CheckTime(int hour, int minute, int second)
        {
            if (hour < 0 || hour > 23) throw new ArgumentOutOfRangeException(nameof(hour), hour, $"Argument [{nameof(hour)}] must be between 0 - 23");
            if (minute < 0 || minute > 59) throw new ArgumentOutOfRangeException(nameof(minute), minute, $"Argument [{nameof(minute)}] must be between 0 - 59");
            if (second < 0 || second > 59) throw new ArgumentOutOfRangeException(nameof(second), second, $"Argument [{nameof(second)}] must be between 0 - 59");
        }

        public static string[] ParsePath(TaskFolder taskFolder) => Util.PathParse(taskFolder.Path, PATH_PARSE_CHARACTERS);

        #endregion Helpers

        #region Triggers

        public static MonthlyTrigger CreateTriggerMonthly(int dayOfMonth, int hour = 0, int minute = 0, int second = 0)
        {
            // TODO: Check daysOfMonth
            var t = new MonthlyTrigger(monthsOfYear: MonthsOfTheYear.AllMonths)
            {
                DaysOfMonth = new int[] { dayOfMonth },
                StartBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute)
            };
            return t;
        }

        public static Trigger CreateTriggerInterval(TimeSpan interval)
        {
            var trigger = new TimeTrigger();
            trigger.Repetition.Interval = interval;
            return trigger;
        }

        public static DailyTrigger CreateTriggerDaily(int hour = 0, int minute = 0, int second = 0)
        {
            CheckTime(hour, minute, second);
            var startBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);
            return new DailyTrigger { StartBoundary = startBoundary };
        }

        public static WeeklyTrigger CreateTriggerWeekly(IEnumerable<DayOfWeek> days, int hour = 0, int minute = 0, int second = 0)
        {
            CheckTime(hour, minute, second);
            var startBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);
            var t = new WeeklyTrigger { StartBoundary = startBoundary };

            var list = new List<DaysOfTheWeek>();
            foreach (var day in days)
            {
                var dotw = Util.GetEnumItem<DaysOfTheWeek>(day.ToString());
                list.Add(dotw);
            }

            t.DaysOfWeek = Util.CombineEnumFlags(list);

            return t;
        }

        public static IEnumerable<Trigger> CreateTriggerCron(string cron) => Trigger.FromCronFormat(cron);

        #endregion Triggers

        #region Add

        public Task TaskAdd(string[] path, string taskName, string filePath, IEnumerable<Trigger> triggers, string arguments = null, string workingDirectory = null, string description = null, string documentation = null, string username = null, string password = null)
        {
            path.CheckNotNull(nameof(path));
            taskName = taskName.CheckNotNullTrimmed(nameof(taskName));

            var dir = GetTaskFolder(path, true);
            if (dir == null) return null;

            var td = TaskService.NewTask();

            td.RegistrationInfo.Description = description.TrimOrNull();
            td.RegistrationInfo.Documentation = documentation.TrimOrNull();

            if (username == null) username = USER_SYSTEM;

            var tlt = TaskLogonType.Password;
            foreach (var sc in Utilities.Constant.LIST_StringComparer)
            {
                if (username.In(sc, USER_SYSTEM, USER_LOCALSERVICE, USER_NETWORKSERVICE))
                {
                    tlt = TaskLogonType.ServiceAccount;
                    username = username.ToUpper();
                    password = null;
                    break;
                }
            }
            td.Principal.LogonType = tlt;
            td.Principal.UserId = username;
            td.Settings.Hidden = false;
            foreach (var trigger in triggers)
            {
                td.Triggers.Add(trigger);
            }

            td.Actions.Add(new ExecAction(filePath, arguments, workingDirectory));

            var task = dir.RegisterTaskDefinition(taskName, td, TaskCreation.CreateOrUpdate, username, password: password, logonType: tlt);

            return task;
        }

        #endregion Add

        #region Delete

        public bool TaskDelete(Task task)
        {
            var taskName = task.Name;
            if (task.State.In(TaskState.Queued, TaskState.Running, TaskState.Unknown))
            {
                try
                {
                    log.Debug($"Stopping task {taskName} in state {task.State}");
                    task.Stop();
                }
                catch (Exception e)
                {
                    log.Warn($"Error stopping task {taskName}. {e.Message}", e);
                }
            }

            if (task.Enabled == true)
            {
                try
                {
                    log.Debug($"Disabling task {taskName}");
                    task.Enabled = false;
                }
                catch (Exception e)
                {
                    log.Warn($"Error disabling task {taskName}. {e.Message}", e);
                }
            }

            var result = false;
            log.Debug($"Deleting task [{taskName}]");
            try
            {
                task.Folder.DeleteTask(taskName);
                result = true;
            }
            catch (Exception e)
            {
                log.Warn($"Error deleting task {taskName}. {e.Message}", e);
            }

            return result;
        }

        public bool TaskDelete(string[] path, string taskName)
        {
            var t = GetTask(path, taskName);
            if (t == null) return false;
            return TaskDelete(t);
        }

        #endregion Delete

        #region Get


        public IEnumerable<Task> GetTasksAll()
        {
            var list = new List<Task>();
            foreach (var a in GetTasks())
            {
                foreach (var b in a.Value)
                {
                    list.Add(b);
                }
            }
            return list;
        }
        public Dictionary<string[], List<Task>> GetTasks()
        {
            var queue = new Queue<TaskFolder>();
            queue.Enqueue(TaskService.RootFolder);
            var d = new Dictionary<string[], List<Task>>(WindowsTaskSchedulerPathComparer.OrdinalIgnoreCase);

            while (queue.Count > 0)
            {
                var currentFolder = queue.Dequeue();
                var currentFolderPath = ParsePath(currentFolder);

                if (!d.TryGetValue(currentFolderPath, out var currentFolderTasks))
                {
                    currentFolderTasks = new List<Task>();
                    d.Add(currentFolderPath, currentFolderTasks);
                }

                foreach (var subfolder in currentFolder.SubFolders) queue.Enqueue(subfolder);
                foreach (var task in currentFolder.Tasks)
                {
                    currentFolderTasks.Add(task);
                }
            }

            return d;
        }

        public IEnumerable<Task> GetTasks(string[] path)
        {
            var dir = GetTaskFolder(path, false);
            if (dir == null) return Enumerable.Empty<Task>();
            return dir.Tasks;
        }

        public Task GetTask(string path)
        {
            foreach (var task in GetTasksAll())
            {
                if (task.IsMatchNameFull(path)) return task;
            }
            return null;
        }

        public Task GetTask(string[] path, string taskName)
        {
            path.CheckNotNull(nameof(path));
            taskName = taskName.CheckNotNullTrimmed(nameof(taskName));

            var dir = GetTaskFolder(path, false);
            if (dir == null) return null;

            Task GetTaskSub(TaskFolder currentFolder, string tn)
            {
                var d = new Dictionary<string, Task>();
                foreach (var task in currentFolder.Tasks) d[task.Name] = task;
                foreach (var sc in Utilities.Constant.LIST_StringComparison) foreach (var kvp in d) if (string.Equals(kvp.Key, tn, sc)) return kvp.Value;
                return null;
            }

            return GetTaskSub(dir, taskName);
        }

        public TaskFolder GetTaskFolder(string[] path, bool createFolders)
        {
            path.CheckNotNull(nameof(path));

            var currentDirectory = TaskService.RootFolder;

            TaskFolder GetTaskFolderSub(TaskFolder currentFolder, string folderName)
            {
                var d = new Dictionary<string, TaskFolder>();
                foreach (var subfolder in currentFolder.SubFolders) d[subfolder.Name] = subfolder;
                foreach (var sc in Utilities.Constant.LIST_StringComparison) foreach (var kvp in d) if (string.Equals(kvp.Key, folderName, sc)) return kvp.Value;
                return null;
            }

            for (var i = 0; i < path.Length; i++)
            {
                var pathItem = path[i];
                var taskFolder = GetTaskFolderSub(currentDirectory, pathItem);
                if (taskFolder == null)
                {
                    if (!createFolders) return null;
                    log.Debug("Creating task folder " + Util.PathToString(ParsePath(currentDirectory).Concat(pathItem)));
                    currentDirectory = currentDirectory.CreateFolder(pathItem);
                }
                else
                {
                    currentDirectory = taskFolder;
                }
            }

            return currentDirectory;
        }

        #endregion Get

        public void Dispose()
        {
            TaskService ts;
            lock (locker)
            {
                ts = taskService;
                taskService = null;
            }
            if (ts != null)
            {
                log.Debug($"Dispose() called, disposing of {typeof(TaskService).FullNameFormatted()}");
                ts.Dispose();
            }
        }
    }

}
