// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaxRunSoftware.Utilities.External;
using Microsoft.Win32.TaskScheduler;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class WindowsTaskSchedulerAdd : WindowsTaskSchedulerBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Creates a new Windows Task Scheduler task");
        help.AddParameter(nameof(taskName), "tn", "The full path and name of the task");
        help.AddParameter(nameof(taskUsername), "tu", "User account username to run the task as, SYSTEM, LOCALSERVICE, NETWORKSERVICE are valid values as well");
        help.AddParameter(nameof(taskPassword), "tp", "User account password to run the task as");
        help.AddParameter(nameof(taskWorkingDirectory), "tw", "The working directory for when the task executes");
        help.AddParameter(nameof(taskDescription), "td", "The description for the task");
        help.AddParameter(nameof(trigger1), "t1", "Trigger 1");
        help.AddParameter(nameof(trigger2), "t2", "Trigger 2");
        help.AddParameter(nameof(trigger3), "t3", "Trigger 3");
        help.AddParameter(nameof(trigger4), "t4", "Trigger 4");
        help.AddParameter(nameof(trigger5), "t5", "Trigger 5");
        help.AddParameter(nameof(trigger6), "t6", "Trigger 6");
        help.AddParameter(nameof(trigger7), "t7", "Trigger 7");
        help.AddParameter(nameof(trigger8), "t8", "Trigger 8");
        help.AddParameter(nameof(trigger9), "t9", "Trigger 9");
        help.AddValue("<execute file path 1> <execute file path 2> <etc>");
        help.AddDetail("Trigger formats are...");
        help.AddDetail("  DAILY {hour}:{minute}");
        help.AddDetail("  MONDAY {hour}:{minute}");
        help.AddDetail("  HOURLY {minute}");
        help.AddDetail("  MONTHLY {dayOfMonth}:{hour}:{minute}");
        help.AddDetail("  CRON <Minute> <Hour> <Day_of_the_Month> <Month_of_the_Year> <Day_of_the_Week>");
        help.AddExample(HelpExamplePrefix + " -taskUsername=`system` -tw=`c:\\temp` -t1=`DAILY 04:15` -tn=`MyTask` `C:\\temp\\RunMe.bat`");
        help.AddExample(HelpExamplePrefix + " -taskUsername=`system` -tw=`c:\\temp` -t1=`HOURLY 35` -tn=`MyTask` `C:\\temp\\RunMe.bat`");
        help.AddExample(HelpExamplePrefix + " -taskUsername=`system` -tw=`c:\\temp` -t1=`MONDAY 19:12` -t2=`WEDNESDAY 19:12` -tn=`MyTask` `C:\\temp\\RunMe.bat`");
    }

    private string taskName;
    private string taskUsername;
    private string taskPassword;
    private string taskWorkingDirectory;
    private string taskDescription;
    private string trigger1;
    private string trigger2;
    private string trigger3;
    private string trigger4;
    private string trigger5;
    private string trigger6;
    private string trigger7;
    private string trigger8;
    private string trigger9;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        taskUsername = GetArgParameterOrConfigRequired(nameof(taskUsername), "tu").TrimOrNull();
        taskPassword = GetArgParameterOrConfig(nameof(taskPassword), "tp").TrimOrNull();
        if (RemapUsername(ref taskUsername)) taskPassword = null;

        log.DebugParameter(nameof(taskUsername), taskUsername);
        log.DebugParameter(nameof(taskPassword), taskPassword);

        taskName = GetArgParameterOrConfigRequired(nameof(taskName), "tn").TrimOrNull();
        log.DebugParameter(nameof(taskName), taskName);

        taskWorkingDirectory = GetArgParameterOrConfig(nameof(taskWorkingDirectory), "tw");
        taskDescription = GetArgParameterOrConfig(nameof(taskDescription), "td");

        trigger1 = GetArgParameterOrConfig(nameof(trigger1), "t1");
        trigger2 = GetArgParameterOrConfig(nameof(trigger2), "t2");
        trigger3 = GetArgParameterOrConfig(nameof(trigger3), "t3");
        trigger4 = GetArgParameterOrConfig(nameof(trigger4), "t4");
        trigger5 = GetArgParameterOrConfig(nameof(trigger5), "t5");
        trigger6 = GetArgParameterOrConfig(nameof(trigger6), "t6");
        trigger7 = GetArgParameterOrConfig(nameof(trigger7), "t7");
        trigger8 = GetArgParameterOrConfig(nameof(trigger8), "t8");
        trigger9 = GetArgParameterOrConfig(nameof(trigger9), "t9");
        var triggerArray = new[] { trigger1, trigger2, trigger3, trigger4, trigger5, trigger6, trigger7, trigger8, trigger9 };

        var triggerStrings = triggerArray.TrimOrNull().WhereNotNull().ToList();
        log.Debug(triggerStrings, nameof(triggerStrings));

        var triggers = new List<Trigger>();
        foreach (var triggerString in triggerStrings) triggers.AddRange(WindowsTaskSchedulerTrigger.CreateTriggers(triggerString));

        for (var i = 0; i < triggers.Count; i++) log.Debug($"Parsed trigger[{i}]: {triggers[i]}");

        var executeFiles = GetArgValuesTrimmed();
        for (var i = 0; i < executeFiles.Count; i++)
        {
            executeFiles[i] = Path.GetFullPath(executeFiles[i]);
            log.Debug($"ExecuteFile[{i}]: {executeFiles[i]}");
        }


        using (var scheduler = GetTaskScheduler())
        {
            var existingTask = scheduler.GetTask(taskName);
            if (existingTask != null) throw new ArgsException(nameof(taskName), $"Task already exists {taskName}");

            var taskPath = new WindowsTaskSchedulerPath(taskName);
            log.Debug("Creating task: " + taskPath);
            var t = scheduler.TaskAdd(
                taskPath,
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
