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
using HavokMultimedia.Utilities.Console.External;
using Microsoft.Win32.TaskScheduler;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WindowsTaskSchedulerCreate : WindowsTaskSchedulerBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Creates a new Windows Task Scheduler task");
            help.AddParameter("taskName", "tn", "The full path and name of the task");
            help.AddParameter("taskUsername", "tu", "User account username to run the task as, SYSTEM, LOCALSERVICE, NETWORKSERVICE are valid values as well");
            help.AddParameter("taskPassword", "tp", "User account password to run the task as");
            help.AddParameter("taskWorkingDirectory", "tw", "The working directory for when the task executes");
            help.AddParameter("taskDescription", "td", "The description for the task");
            help.AddParameter("trigger1", "t1", "Trigger 1");
            help.AddParameter("trigger2", "t2", "Trigger 2");
            help.AddParameter("trigger3", "t3", "Trigger 3");
            help.AddParameter("trigger4", "t4", "Trigger 4");
            help.AddParameter("trigger5", "t5", "Trigger 5");
            help.AddParameter("trigger6", "t6", "Trigger 6");
            help.AddParameter("trigger7", "t7", "Trigger 7");
            help.AddParameter("trigger8", "t8", "Trigger 8");
            help.AddParameter("trigger9", "t9", "Trigger 9");
            help.AddValue("<execute file path 1> <execute file path 2> <etc>");
            help.AddDetail("Trigger formats are...");
            help.AddDetail("  DAILY {hour}:{minute}");
            help.AddDetail("  MONDAY {hour}:{minute}");
            help.AddDetail("  HOURLY {minute}");
            help.AddDetail("  MONTHLY {dayOfMonth}:{hour}:{minute}");
            help.AddDetail("  CRON <Minute> <Hour> <Day_of_the_Month> <Month_of_the_Year> <Day_of_the_Week>");
        }

        protected override void Execute()
        {
            base.Execute();


            var taskUsername = GetArgParameterOrConfigRequired("taskUsername", "tu").TrimOrNull();
            var taskPassword = GetArgParameterOrConfig("taskPassword", "tp").TrimOrNull();
            var taskUsernameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SYSTEM", WindowsTaskScheduler.USER_SYSTEM },
                { "LOCALSERVICE", WindowsTaskScheduler.USER_LOCALSERVICE },
                { "NETWORKSERVICE", WindowsTaskScheduler.USER_NETWORKSERVICE }
            };
            if (taskUsername != null && taskUsernameMapping.TryGetValue(taskUsername, out var taskUsernameMappingValue))
            {
                taskUsername = taskUsernameMappingValue;
                taskPassword = null;
                log.Debug($"taskUsername: {taskUsername}");
                log.Debug($"taskPassword: {taskPassword}");
            }

            var taskNameFull = GetArgParameterOrConfigRequired("taskName", "tn").TrimOrNull();
            log.Debug($"taskNameFull: {taskNameFull}");
            var taskPath = WindowsTaskScheduler.ParsePath(taskNameFull).ToList();
            var taskName = taskPath.PopTail();
            log.Debug($"taskName: {taskName}");
            log.Debug($"taskPath: {taskPath}");

            var taskWorkingDirectory = GetArgParameterOrConfig("taskWorkingDirectory", "tw");
            var taskDescription = GetArgParameterOrConfig("taskDescription", "td");

            var triggerStrings = new List<string>();
            for (int i = 1; i < 10; i++) triggerStrings.Add(GetArgParameterOrConfig("trigger" + i, "t" + i));
            triggerStrings = triggerStrings.TrimOrNull().WhereNotNull().ToList();
            for (int i = 1; i < triggerStrings.Count; i++) log.Debug($"triggerStrings[{i}]: {triggerStrings[i]}");

            var executeFiles = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            for (int i = 0; i < executeFiles.Count; i++)
            {
                executeFiles[i] = Path.GetFullPath(executeFiles[i]);
                log.Debug($"ExecuteFile[{i}]: {executeFiles[i]}");
            }

            var triggers = new List<Trigger>();
            foreach (var triggerString in triggerStrings) triggers.AddRange(CreateTriggers(triggerString));
            for (int i = 0; i < triggers.Count; i++) log.Debug($"Parsed trigger[{i}]: {triggers[i]}");

            using (var scheduler = GetTaskScheduler())
            {
                var existingTask = scheduler.GetTask(taskNameFull);
                if (existingTask != null) throw new ArgsException(nameof(taskName), $"Task already exists {taskNameFull}");

                var t = scheduler.TaskAdd(
                    taskPath.ToArray(),
                    taskName,
                    executeFiles.ToArray(),
                    triggers,
                    workingDirectory: taskWorkingDirectory,
                    username: taskUsername,
                    password: taskPassword,
                    description: taskDescription
                    );

                log.Info("Successfully created task: " + t);


            }




        }
    }
}
