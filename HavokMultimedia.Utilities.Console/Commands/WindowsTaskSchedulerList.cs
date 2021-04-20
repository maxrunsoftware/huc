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
using HavokMultimedia.Utilities.Console.External;
using Microsoft.Win32.TaskScheduler;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WindowsTaskSchedulerList : WindowsTaskSchedulerBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Lists tasks in the Windows Task Scheduler");
            help.AddParameter("all", "a", "Lists all tasks including the Microsoft/Windows ones (false)");
            help.AddParameter("detail", "d", "Lists the task details (false)");
            help.AddParameter("xml", "x", "Show XML for task (false)");
            help.AddValue("ALL | <folder path>");
            help.AddExample("-h=`localhost` -u=`administrator` -p=`password` ALL");
            help.AddExample("-h=`localhost` -u=`administrator` -p=`password` -d /myTaskFolder/MyTask");
        }

        private bool MatchesPath(Task task, string path)
        {
            var pathParts = WindowsTaskScheduler.ParsePath(path);
            var taskParts = task.NameFullParts();

            if (pathParts.Length > taskParts.Length) return false;
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (!pathParts[i].EqualsCaseInsensitive(taskParts[i])) return false;
            }
            return true;
        }

        protected override void Execute()
        {
            base.Execute();

            var all = GetArgParameterOrConfigBool("all", "a", false);
            var detail = GetArgParameterOrConfigBool("detail", "d", false);
            var xml = GetArgParameterOrConfigBool("xml", "x", false);

            var allOrFolderPath = GetArgValueTrimmed(0) ?? "ALL";

            using (var scheduler = GetTaskScheduler())
            {
                var tasksAll = scheduler.GetTasksAll();
                var tasks = new List<Task>();
                foreach (var task in tasksAll)
                {
                    if (allOrFolderPath.EqualsCaseInsensitive("ALL"))
                    {
                        tasks.Add(task);
                    }
                    else
                    {
                        if (MatchesPath(task, allOrFolderPath)) tasks.Add(task);
                    }
                }
                foreach (var task in tasks)
                {
                    var taskNameParts = task.NameFullParts();
                    var part1 = taskNameParts.GetAtIndexOrDefault(0);
                    var part2 = taskNameParts.GetAtIndexOrDefault(1);
                    if (part1 != null && part2 != null)
                    {
                        if (part1.EqualsCaseInsensitive("Microsoft") && part2.EqualsCaseInsensitive("Windows"))
                        {
                            if (!all) continue;
                        }
                    }

                    log.Info(task.NameFull());
                    if (detail)
                    {
                        log.Info("Name: " + task.Name);
                        log.Info("IsActive: " + task.IsActive);
                        log.Info("LastRunTime: " + task.LastRunTime);
                        log.Info("NextRunTime: " + task.NextRunTime);
                        log.Info("NumberOfMissedRuns: " + task.NumberOfMissedRuns);
                        log.Info("ReadOnly: " + task.ReadOnly);
                        if (task.Definition != null)
                        {
                            var actioni = 0;
                            foreach (var action in task.Definition.Actions)
                            {
                                log.Info($"Definition.Actions[{actioni}]: {action}");
                                log.Info($"Definition.Actions[{actioni}].ActionType: {action.ActionType}");
                                log.Info($"Definition.Actions[{actioni}].Id: {action.Id}");
                                actioni++;
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

                            int ti = 0;
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

                    if (xml)
                    {
                        log.Info(Environment.NewLine + task.Xml);
                    }

                    if (detail || xml) log.Info("");

                }
            }



        }
    }
}
