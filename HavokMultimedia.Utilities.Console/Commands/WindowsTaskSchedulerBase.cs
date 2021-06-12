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
    public abstract class WindowsTaskSchedulerBase : Command
    {
        public enum TriggerDirective
        {
            Hourly,
            Daily,
            Monthly,
            Cron,
            Sunday,
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday
        }
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter(nameof(host), "h", "Server hostname or IP");
            help.AddParameter(nameof(username), "u", "Server username");
            help.AddParameter(nameof(password), "p", "Server password");
            help.AddParameter(nameof(forceV1), "v1", "Server force version 1 task scheduler implementation (false)");
        }

        protected string HelpExamplePrefix => "-h=192.168.1.5 -u=administrator -p=testpass";

        protected WindowsTaskScheduler GetTaskScheduler()
        {
            if (host == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());

            return new WindowsTaskScheduler(host, username, password, forceV1: forceV1);
        }

        private string host;
        private string username;
        private string password;
        private bool forceV1;

        protected override void ExecuteInternal()
        {
            host = GetArgParameterOrConfigRequired(nameof(host), "h").TrimOrNull();
            username = GetArgParameterOrConfigRequired(nameof(username), "u").TrimOrNull();
            password = GetArgParameterOrConfigRequired(nameof(password), "p").TrimOrNull();
            forceV1 = GetArgParameterOrConfigBool(nameof(forceV1), "v1", false);
        }

        protected bool RemapUsername(ref string username)
        {
            if (username == null)
            {
                return false;
            }

            if (username.EqualsCaseInsensitive("SYSTEM"))
            {
                username = WindowsTaskScheduler.USER_SYSTEM;
                return true;
            }
            if (username.EqualsCaseInsensitive("LOCALSERVICE"))
            {
                username = WindowsTaskScheduler.USER_LOCALSERVICE;
                return true;
            }
            if (username.EqualsCaseInsensitive("NETWORKSERVICE"))
            {
                username = WindowsTaskScheduler.USER_NETWORKSERVICE;
                return true;
            }

            return false;
        }

        private TriggerDirective ParseDirective(string directive)
        {
            if (directive.EqualsCaseInsensitive("SUN")) directive = TriggerDirective.Sunday.ToString();
            if (directive.EqualsCaseInsensitive("MON")) directive = TriggerDirective.Monday.ToString();
            if (directive.EqualsCaseInsensitive("TUE")) directive = TriggerDirective.Tuesday.ToString();
            if (directive.EqualsCaseInsensitive("WED")) directive = TriggerDirective.Wednesday.ToString();
            if (directive.EqualsCaseInsensitive("THU")) directive = TriggerDirective.Thursday.ToString();
            if (directive.EqualsCaseInsensitive("FRI")) directive = TriggerDirective.Friday.ToString();
            if (directive.EqualsCaseInsensitive("SAT")) directive = TriggerDirective.Saturday.ToString();
            return Util.GetEnumItem<TriggerDirective>(directive);
        }

        public List<Trigger> CreateTriggers(params string[] triggerParts)
        {
            var triggerPartsQueue = new Queue<string>();
            foreach (var triggerPart in triggerParts.OrEmpty().TrimOrNull().WhereNotNull())
            {
                var parts = triggerPart.Split(' ').TrimOrNull().WhereNotNull();
                foreach (var part in parts)
                {
                    triggerPartsQueue.Enqueue(part);
                }
            }

            if (triggerPartsQueue.IsEmpty()) throw new Exception("No trigger provided");
            var directiveString = triggerPartsQueue.Dequeue().ToUpper();
            if (triggerPartsQueue.IsEmpty()) throw new Exception("No trigger details provided for trigger " + directiveString);
            var directive = ParseDirective(directiveString);

            var triggers = new List<Trigger>();
            log.Debug("Parsing trigger directive " + directive + " " + triggerPartsQueue.ToStringGuessFormat());
            if (directive.In(TriggerDirective.Hourly))
            {
                while (triggerPartsQueue.IsNotEmpty())
                {
                    var time = triggerPartsQueue.Dequeue();
                    var mm = ParseTimeMM(time, directive);
                    var t = WindowsTaskScheduler.CreateTriggerInterval(TimeSpan.FromMinutes(mm));
                    triggers.Add(t);
                }
            }
            else if (directive.In(TriggerDirective.Daily))
            {
                while (triggerPartsQueue.IsNotEmpty())
                {
                    var time = triggerPartsQueue.Dequeue();
                    var hhmm = ParseTimeHHMM(time, directive);
                    var t = WindowsTaskScheduler.CreateTriggerDaily(hour: hhmm.hour, minute: hhmm.minute);
                    triggers.Add(t);
                }
            }
            else if (directive.In(TriggerDirective.Cron))
            {
                var cronParts = string.Join(" ", triggerPartsQueue);
                var cronTriggers = WindowsTaskScheduler.CreateTriggerCron(cronParts);
                triggers.AddRange(cronTriggers);
            }
            else if (directive.In(TriggerDirective.Sunday, TriggerDirective.Monday, TriggerDirective.Tuesday, TriggerDirective.Wednesday, TriggerDirective.Thursday, TriggerDirective.Friday, TriggerDirective.Saturday))
            {
                while (triggerPartsQueue.IsNotEmpty())
                {
                    var time = triggerPartsQueue.Dequeue();
                    var hhmm = ParseTimeHHMM(time, directive);
                    var dow = Util.GetEnumItem<DayOfWeek>(directive.ToString());
                    var t = WindowsTaskScheduler.CreateTriggerWeekly(dow.Yield(), hour: hhmm.hour, minute: hhmm.minute);
                    triggers.Add(t);
                }
            }
            else if (directive.In(TriggerDirective.Monthly))
            {
                while (triggerPartsQueue.IsNotEmpty())
                {
                    var time = triggerPartsQueue.Dequeue();
                    var (dayOfMonth, hour, minute) = ParseTimeDayOfMonthHHMM(time, directive);
                    var t = WindowsTaskScheduler.CreateTriggerMonthly(dayOfMonth, hour: hour, minute: minute);
                    triggers.Add(t);
                }
            }
            else
            {
                throw new NotImplementedException($"Trigger type {directive} has not been implemented yet");
            }

            for (int i = 0; i < triggers.Count; i++) log.Debug($"Created trigger[{i}]: " + triggers[i]);

            return triggers;
        }

        private byte ParseTimeMM(string time, TriggerDirective directive)
        {
            time = ("00" + time).Right(2);
            if (!time.ToByteTry(out var minute)) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected MM");

            return minute;
        }

        private (byte dayOfMonth, byte hour, byte minute) ParseTimeDayOfMonthHHMM(string time, TriggerDirective directive)
        {
            log.Trace($"Parsing dayOfMonth, hours, and minutes from {time}");
            var timeparts = time.Split(':').TrimOrNull().WhereNotNull().ToArray();
            if (timeparts.Length != 3) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected format dayOfMonth:HH:MM");
            if (!timeparts[0].ToByteTry(out var dayOfMonth)) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid day of month format, expected format dayOfMonth:HH:MM");
            if (!timeparts[1].ToByteTry(out var hour)) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid hour format, expected format dayOfMonth:HH:MM");
            if (!timeparts[2].ToByteTry(out var minute)) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid minute format, expected format dayOfMonth:HH:MM");
            log.Trace($"Parsed {dayOfMonth} day of month, {hour} hour, and {minute} minute from {time}");
            return (dayOfMonth, hour, minute);
        }

        private (byte hour, byte minute) ParseTimeHHMM(string time, TriggerDirective directive)
        {
            log.Trace($"Parsing hours and minutes from {time}");
            var timeparts = time.Split(':').TrimOrNull().WhereNotNull().ToArray();
            if (timeparts.Length < 2) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected HH:MM. Expected : character in time {time}");
            if (timeparts.Length > 2) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected HH:MM. Encountered multiple : characters in time {time} but only 1 is allowed seperating hours and minutes");
            var hours = timeparts[0].ToByte();
            var mins = timeparts[1].ToByte();
            log.Trace($"Parsed {hours} hours and {mins} minutes from {time}");
            return (hours, mins);
        }




    }
}
