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
using System.Linq;
using Microsoft.Win32.TaskScheduler;

namespace MaxRunSoftware.Utilities.External
{
    public class WindowsTaskScheduler : IDisposable
    {
        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
            if (accountDomain == null) accountDomain = host;

            log.Debug($"Creating new {typeof(TaskService).FullNameFormatted()}(host: {host}, username: {username}, accountDomain: {accountDomain}, password: {password}, forceV1: {forceV1})");
            taskService = new TaskService(host, userName: username, accountDomain: accountDomain, password: password, forceV1: forceV1);
        }

        public Task TaskAdd(WindowsTaskSchedulerPath path, string[] filePaths, IEnumerable<Trigger> triggers, string arguments = null, string workingDirectory = null, string description = null, string documentation = null, string username = null, string password = null)
        {
            path.CheckNotNull(nameof(path));
            var taskName = path.Name.CheckNotNullTrimmed(nameof(path.Name));

            var dir = CreateTaskFolder(path.Parent);

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

            foreach (var filePath in filePaths)
            {
                td.Actions.Add(new ExecAction(filePath, arguments, workingDirectory));
            }

            var task = dir.RegisterTaskDefinition(taskName, td, TaskCreation.CreateOrUpdate, username, password: password, logonType: tlt);

            return task;
        }

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

        public bool TaskDelete(WindowsTaskSchedulerPath path)
        {
            var t = GetTask(path);
            if (t == null) return false;
            return TaskDelete(t);
        }

        public IEnumerable<Task> GetTasks()
        {
            foreach (var f in GetTaskFolders())
            {
                foreach (var t in f.Tasks)
                {
                    log.Trace("Found Task: " + t.GetPath());
                    yield return t;
                }
            }
        }

        public Dictionary<WindowsTaskSchedulerPath, List<Task>> GetTasksByFolder()
        {
            var d = new Dictionary<WindowsTaskSchedulerPath, List<Task>>();
            foreach (var tf in GetTaskFolders())
            {
                d.AddToList(tf.GetPath(), tf.Tasks.ToArray());
            }
            return d;
        }

        public IEnumerable<TaskFolder> GetTaskFolders()
        {
            var queue = new Queue<TaskFolder>();
            var h = new HashSet<WindowsTaskSchedulerPath>();

            queue.Enqueue(TaskService.RootFolder);
            while (queue.Count > 0)
            {
                var currentFolder = queue.Dequeue();
                if (!h.Add(currentFolder.GetPath())) continue;
                foreach (var subfolder in currentFolder.SubFolders) queue.Enqueue(subfolder);
                log.Trace("Found TaskFolder: " + currentFolder.GetPath());
                yield return currentFolder;
            }
        }

        public Task GetTask(WindowsTaskSchedulerPath path)
        {
            log.Debug("Getting Task: " + path);
            foreach (var task in GetTasks())
            {
                if (path.Equals(task.GetPath())) return task;
            }
            return null;
        }
        public Task GetTask(string path) => GetTask(new WindowsTaskSchedulerPath(path));

        public TaskFolder GetTaskFolder(WindowsTaskSchedulerPath path)
        {
            log.Debug("Getting TaskFolder: " + path);
            foreach (var folder in GetTaskFolders())
            {
                if (path.Equals(folder.GetPath())) return folder;
            }
            return null;
        }
        public TaskFolder GetTaskFolder(string path) => GetTaskFolder(new WindowsTaskSchedulerPath(path));

        public TaskFolder CreateTaskFolder(WindowsTaskSchedulerPath path)
        {
            var existingFolder = GetTaskFolder(path);
            if (existingFolder != null) return existingFolder;

            var parent = path.Parent;
            var parentFolder = GetTaskFolder(parent);
            if (parentFolder == null) parentFolder = CreateTaskFolder(parent);
            log.Debug("Creating TaskFolder: " + path);
            return parentFolder.CreateFolder(path.Name);
        }

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
