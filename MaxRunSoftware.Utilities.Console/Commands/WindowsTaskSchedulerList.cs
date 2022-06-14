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

using System;
using System.Collections.Generic;
using MaxRunSoftware.Utilities.External;
using Microsoft.Win32.TaskScheduler;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class WindowsTaskSchedulerList : WindowsTaskSchedulerBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Lists tasks in the Windows Task Scheduler");
        help.AddParameter(nameof(all), "a", "Lists all tasks including the Microsoft/Windows ones (false)");
        help.AddParameter(nameof(detail), "d", "Lists the task details (false)");
        help.AddParameter(nameof(xml), "x", "Show XML for task (false)");
        help.AddValue("<optional folder path>");
        help.AddExample(HelpExamplePrefix);
        help.AddExample(HelpExamplePrefix + " -d /myTaskFolder/MyTask");
    }

    private bool all;
    private bool detail;
    private bool xml;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        all = GetArgParameterOrConfigBool(nameof(all), "a", false);
        detail = GetArgParameterOrConfigBool(nameof(detail), "d", false);
        xml = GetArgParameterOrConfigBool(nameof(xml), "x", false);

        var folderPath = GetArgValueTrimmed(0);
        log.DebugParameter(nameof(folderPath), folderPath);
        var folderPathPath = new WindowsTaskSchedulerPath(folderPath);

        using (var scheduler = GetTaskScheduler())
        {
            var tasksAll = scheduler.GetTasks();
            var tasks = new List<Task>();
            foreach (var task in tasksAll)
            {
                var taskPath = task.GetPath();
                if (folderPath == null) { tasks.Add(task); }
                else
                {
                    if (folderPathPath.Equals(taskPath))
                        tasks.Add(task);
                    else if (taskPath.Parent != null && taskPath.Parent.Equals(folderPathPath)) tasks.Add(task);
                }
            }

            foreach (var task in tasks)
            {
                var taskPath = task.GetPath();
                var part1 = taskPath.PathFull.GetAtIndexOrDefault(0);
                var part2 = taskPath.PathFull.GetAtIndexOrDefault(1);
                if (part1 != null && part2 != null)
                    if (part1.EqualsCaseInsensitive("Microsoft") && part2.EqualsCaseInsensitive("Windows"))
                        if (!all)
                            continue;

                log.Info(taskPath.ToString());
                if (detail)
                {
                    log.Info("Name: " + task.Name);
                    log.Info("IsActive: " + task.IsActive);
                    log.Info("LastRunTime: " + task.LastRunTime);
                    log.Info("NextRunTime: " + task.NextRunTime);
                    log.Info("NumberOfMissedRuns: " + task.NumberOfMissedRuns);
                    log.Info("ReadOnly: " + task.ReadOnly);
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (task.Definition != null)
                    {
                        var actionIndex = 0;
                        foreach (var action in task.Definition.Actions)
                        {
                            log.Info($"Definition.Actions[{actionIndex}]: {action}");
                            log.Info($"Definition.Actions[{actionIndex}].ActionType: {action.ActionType}");
                            log.Info($"Definition.Actions[{actionIndex}].Id: {action.Id}");
                            actionIndex++;
                        }

                        log.Info("Definition.Data: " + task.Definition.Data);
                        log.Info("Definition.LowestSupportedVersion: " + task.Definition.LowestSupportedVersion);

                        log.Info("Definition.Principal.Account: " + task.Definition.Principal.Account);
                        log.Info("Definition.Principal.DisplayName: " + task.Definition.Principal.DisplayName);
                        log.Info("Definition.Principal.GroupId: " + task.Definition.Principal.GroupId);
                        log.Info("Definition.Principal.Id: " + task.Definition.Principal.Id);
                        log.Info("Definition.Principal.LogonType: " + task.Definition.Principal.LogonType);
                        log.Info("Definition.Principal.ProcessTokenSidType: " + task.Definition.Principal.ProcessTokenSidType);
                        foreach (var p in task.Definition.Principal.RequiredPrivileges) log.Info("Definition.Principal.RequiredPrivileges: " + p);

                        log.Info("Definition.Principal.RunLevel: " + task.Definition.Principal.RunLevel);
                        log.Info("Definition.Principal.UserId: " + task.Definition.Principal.UserId);

                        log.Info("Definition.RegistrationInfo.Author: " + task.Definition.RegistrationInfo.Author);
                        log.Info("Definition.RegistrationInfo.Date: " + task.Definition.RegistrationInfo.Date);
                        log.Info("Definition.RegistrationInfo.Description: " + task.Definition.RegistrationInfo.Description);
                        log.Info("Definition.RegistrationInfo.Documentation: " + task.Definition.RegistrationInfo.Documentation);
                        // ReSharper disable once StringLiteralTypo
                        log.Info("Definition.RegistrationInfo.SecurityDescriptorSddlForm: " + task.Definition.RegistrationInfo.SecurityDescriptorSddlForm);
                        log.Info("Definition.RegistrationInfo.Source: " + task.Definition.RegistrationInfo.Source);
                        log.Info("Definition.RegistrationInfo.URI: " + task.Definition.RegistrationInfo.URI);
                        log.Info("Definition.RegistrationInfo.Version: " + task.Definition.RegistrationInfo.Version);
                        if (xml) log.Info("Definition.RegistrationInfo.XmlText: " + task.Definition.RegistrationInfo.XmlText);

                        log.Info("Definition.Settings.AllowDemandStart: " + task.Definition.Settings.AllowDemandStart);
                        log.Info("Definition.Settings.AllowHardTerminate: " + task.Definition.Settings.AllowHardTerminate);
                        log.Info("Definition.Settings.Compatibility: " + task.Definition.Settings.Compatibility);
                        log.Info("Definition.Settings.DeleteExpiredTaskAfter: " + task.Definition.Settings.DeleteExpiredTaskAfter);
                        log.Info("Definition.Settings.DisallowStartIfOnBatteries: " + task.Definition.Settings.DisallowStartIfOnBatteries);
                        log.Info("Definition.Settings.DisallowStartOnRemoteAppSession: " + task.Definition.Settings.DisallowStartOnRemoteAppSession);
                        log.Info("Definition.Settings.Enabled: " + task.Definition.Settings.Enabled);
                        log.Info("Definition.Settings.ExecutionTimeLimit: " + task.Definition.Settings.ExecutionTimeLimit);
                        log.Info("Definition.Settings.Hidden: " + task.Definition.Settings.Hidden);
                        log.Info("Definition.Settings.IdleSettings.IdleDuration: " + task.Definition.Settings.IdleSettings.IdleDuration);
                        log.Info("Definition.Settings.IdleSettings.RestartOnIdle: " + task.Definition.Settings.IdleSettings.RestartOnIdle);
                        log.Info("Definition.Settings.IdleSettings.StopOnIdleEnd: " + task.Definition.Settings.IdleSettings.StopOnIdleEnd);
                        log.Info("Definition.Settings.IdleSettings.WaitTimeout: " + task.Definition.Settings.IdleSettings.WaitTimeout);
                        log.Info("Definition.Settings.MaintenanceSettings.Deadline: " + task.Definition.Settings.MaintenanceSettings.Deadline);
                        log.Info("Definition.Settings.MaintenanceSettings.Exclusive: " + task.Definition.Settings.MaintenanceSettings.Exclusive);
                        log.Info("Definition.Settings.MaintenanceSettings.Period: " + task.Definition.Settings.MaintenanceSettings.Period);
                        log.Info("Definition.Settings.MultipleInstances: " + task.Definition.Settings.MultipleInstances);
                        log.Info("Definition.Settings.NetworkSettings.Id: " + task.Definition.Settings.NetworkSettings.Id);
                        log.Info("Definition.Settings.NetworkSettings.Name: " + task.Definition.Settings.NetworkSettings.Name);
                        log.Info("Definition.Settings.Priority: " + task.Definition.Settings.Priority);
                        log.Info("Definition.Settings.RestartCount: " + task.Definition.Settings.RestartCount);
                        log.Info("Definition.Settings.RestartInterval: " + task.Definition.Settings.RestartInterval);
                        log.Info("Definition.Settings.RunOnlyIfIdle: " + task.Definition.Settings.RunOnlyIfIdle);
                        log.Info("Definition.Settings.RunOnlyIfLoggedOn: " + task.Definition.Settings.RunOnlyIfLoggedOn);
                        log.Info("Definition.Settings.RunOnlyIfNetworkAvailable: " + task.Definition.Settings.RunOnlyIfNetworkAvailable);
                        log.Info("Definition.Settings.StartWhenAvailable: " + task.Definition.Settings.StartWhenAvailable);
                        log.Info("Definition.Settings.StopIfGoingOnBatteries: " + task.Definition.Settings.StopIfGoingOnBatteries);
                        log.Info("Definition.Settings.UseUnifiedSchedulingEngine: " + task.Definition.Settings.UseUnifiedSchedulingEngine);
                        log.Info("Definition.Settings.Volatile: " + task.Definition.Settings.Volatile);
                        log.Info("Definition.Settings.WakeToRun: " + task.Definition.Settings.WakeToRun);
                        if (xml) log.Info("Definition.Settings.XmlText: " + task.Definition.Settings.XmlText);

                        var ti = 0;
                        foreach (var t in task.Definition.Triggers)
                        {
                            log.Info($"Definition.Trigger[{ti}]: " + t);
                            log.Info($"Definition.Trigger[{ti}].Enabled: " + t.Enabled);
                            log.Info($"Definition.Trigger[{ti}].EndBoundary: " + t.EndBoundary);
                            log.Info($"Definition.Trigger[{ti}].ExecutionTimeLimit: " + t.ExecutionTimeLimit);
                            log.Info($"Definition.Trigger[{ti}].Id: " + t.Id);
                            log.Info($"Definition.Trigger[{ti}].Repetition.Duration: " + t.Repetition.Duration);
                            log.Info($"Definition.Trigger[{ti}].Repetition.Interval: " + t.Repetition.Interval);
                            log.Info($"Definition.Trigger[{ti}].Repetition.StopAtDurationEnd: " + t.Repetition.StopAtDurationEnd);
                            log.Info($"Definition.Trigger[{ti}].StartBoundary: " + t.StartBoundary);
                            log.Info($"Definition.Trigger[{ti}].TriggerType: " + t.TriggerType);
                            ti++;
                        }
                    }

                    log.Info("Enabled: " + task.Enabled);
                    log.Info("LastTaskResult: " + task.LastTaskResult);
                    log.Info("State: " + task.State);
                }

                if (xml) log.Info(Environment.NewLine + task.Xml);

                if (detail || xml) log.Info("");
            }
        }
    }
}
