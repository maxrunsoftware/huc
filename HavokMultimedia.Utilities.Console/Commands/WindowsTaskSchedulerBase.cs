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


        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter("host", "h", "Server hostname or IP");
            help.AddParameter("username", "u", "Server username");
            help.AddParameter("password", "p", "Server password");
            help.AddParameter("forceV1", "v1", "Server force version 1 task scheduler implementation (false)");
        }

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
            host = GetArgParameterOrConfigRequired("host", "h").TrimOrNull();

            username = GetArgParameterOrConfigRequired("username", "u").TrimOrNull();

            password = GetArgParameterOrConfigRequired("password", "p").TrimOrNull();

            forceV1 = GetArgParameterOrConfigBool("forceV1", "v1", false);
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

            if (triggerPartsQueue.Count < 1) throw new Exception("No trigger provided");
            var directive = triggerPartsQueue.Dequeue().ToUpper();
            if (triggerPartsQueue.Count < 1) throw new Exception("No trigger details provided for trigger " + directive);

            var triggers = new List<Trigger>();
            log.Trace("Parsing trigger directive " + directive + " with " + triggerPartsQueue.Count + " parts");
            if (directive.Equals("HOURLY"))
            {
                while (triggerPartsQueue.Count > 0)
                {
                    var time = triggerPartsQueue.Dequeue();
                    var mm = ParseTimeMM(time, directive);
                    var t = WindowsTaskScheduler.CreateTriggerInterval(TimeSpan.FromMinutes(mm));
                    triggers.Add(t);
                }
            }
            else if (directive.Equals("DAILY"))
            {
                while (triggerPartsQueue.Count > 0)
                {
                    var time = triggerPartsQueue.Dequeue();
                    var hhmm = ParseTimeHHMM(time, directive);
                    var t = WindowsTaskScheduler.CreateTriggerDaily(hour: hhmm.hour, minute: hhmm.minute);
                    triggers.Add(t);
                }
            }
            else if (directive.Equals("CRON"))
            {
                var cronParts = string.Join(" ", triggerPartsQueue);
                var cronTriggers = WindowsTaskScheduler.CreateTriggerCron(cronParts);
                triggers.AddRange(cronTriggers);
            }
            else if (directive.In(Util.GetEnumItems<DayOfWeek>().Select(o => o.ToString().ToUpper())))
            {
                while (triggerPartsQueue.Count > 0)
                {
                    var time = triggerPartsQueue.Dequeue();
                    var hhmm = ParseTimeHHMM(time, directive);
                    var dow = Util.GetEnumItem<DayOfWeek>(directive);
                    var t = WindowsTaskScheduler.CreateTriggerWeekly(dow.Yield(), hour: hhmm.hour, minute: hhmm.minute);
                    triggers.Add(t);
                }
            }
            else if (directive.Equals("MONTHLY"))
            {
                while (triggerPartsQueue.Count > 0)
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

        private byte ParseTimeMM(string time, string directive)
        {
            time = ("00" + time).Right(2);
            if (!time.ToByteTry(out var minute)) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected MM");

            return minute;
        }

        private (byte dayOfMonth, byte hour, byte minute) ParseTimeDayOfMonthHHMM(string time, string directive)
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

        private (byte hour, byte minute) ParseTimeHHMM(string time, string directive)
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
