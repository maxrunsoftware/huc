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
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace MaxRunSoftware.Utilities.External;

public class WindowsTaskSchedulerTrigger
{
    public enum Directive
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
    private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    private static void CheckTime(int hour, int minute, int second)
    {
        if (hour < 0 || hour > 23) throw new ArgumentOutOfRangeException(nameof(hour), hour, $"Argument [{nameof(hour)}] must be between 0 - 23");
        if (minute < 0 || minute > 59) throw new ArgumentOutOfRangeException(nameof(minute), minute, $"Argument [{nameof(minute)}] must be between 0 - 59");
        if (second < 0 || second > 59) throw new ArgumentOutOfRangeException(nameof(second), second, $"Argument [{nameof(second)}] must be between 0 - 59");
    }

    private static Directive ParseDirective(string directive)
    {
        if (directive.EqualsCaseInsensitive("SUN")) directive = Directive.Sunday.ToString();
        if (directive.EqualsCaseInsensitive("MON")) directive = Directive.Monday.ToString();
        if (directive.EqualsCaseInsensitive("TUE")) directive = Directive.Tuesday.ToString();
        if (directive.EqualsCaseInsensitive("WED")) directive = Directive.Wednesday.ToString();
        if (directive.EqualsCaseInsensitive("THU")) directive = Directive.Thursday.ToString();
        if (directive.EqualsCaseInsensitive("FRI")) directive = Directive.Friday.ToString();
        if (directive.EqualsCaseInsensitive("SAT")) directive = Directive.Saturday.ToString();
        return Util.GetEnumItem<Directive>(directive);
    }

    public static IEnumerable<Trigger> CreateTriggers(string line, string logPrefix = null)
    {
        if (logPrefix != null) logPrefix += " ";
        logPrefix ??= string.Empty;

        var triggerParts = line.SplitOnWhiteSpace().TrimOrNull().WhereNotNull();
        var triggerPartsQueue = new Queue<string>();
        triggerParts.ForEach(triggerPartsQueue.Enqueue);

        if (triggerPartsQueue.IsEmpty()) throw new Exception("No trigger provided");
        var directiveString = triggerPartsQueue.Dequeue().ToUpper();
        if (triggerPartsQueue.IsEmpty()) throw new Exception("No trigger details provided for trigger " + directiveString);
        var directive = ParseDirective(directiveString);

        var triggers = new List<Trigger>();
        log.Debug("Parsing trigger directive " + directive + " " + triggerPartsQueue.ToStringGuessFormat());
        if (directive.In(Directive.Hourly))
        {
            while (triggerPartsQueue.IsNotEmpty())
            {
                var time = triggerPartsQueue.Dequeue();
                var mm = ParseTimeMM(time, directive);
                var t = CreateTriggerInterval(TimeSpan.FromMinutes(mm));
                triggers.Add(t);
                log.Debug($"{logPrefix}{directive} created at times " + string.Join($":{mm}:00 ", Enumerable.Range(0, 24)) + $":{mm}:00 --> " + t);

            }
        }
        else if (directive.In(Directive.Daily))
        {
            while (triggerPartsQueue.IsNotEmpty())
            {
                var time = triggerPartsQueue.Dequeue();
                var hhmm = ParseTimeHHMM(time, directive);
                var t = CreateTriggerDaily(hour: hhmm.hour, minute: hhmm.minute);
                triggers.Add(t);
                log.Debug($"{logPrefix}{directive} created at time {hhmm.hour}:{hhmm.minute} --> " + t);
            }
        }
        else if (directive.In(Directive.Cron))
        {
            var cronParts = string.Join(" ", triggerPartsQueue);
            var cronTriggers = CreateTriggerCron(cronParts);
            triggers.AddRange(cronTriggers);
            log.Debug($"{logPrefix}{directive} created {triggers.Count} triggers");
            for (int i = 0; i < triggers.Count; i++)
            {
                var t = triggers[i];
                log.Debug("  trigger[" + i + "]: " + t);
            }
        }
        else if (directive.In(Directive.Sunday, Directive.Monday, Directive.Tuesday, Directive.Wednesday, Directive.Thursday, Directive.Friday, Directive.Saturday))
        {
            while (triggerPartsQueue.IsNotEmpty())
            {
                var time = triggerPartsQueue.Dequeue();
                var hhmm = ParseTimeHHMM(time, directive);
                var dow = Util.GetEnumItem<DayOfWeek>(directive.ToString());
                var t = CreateTriggerWeekly(dow.Yield(), hour: hhmm.hour, minute: hhmm.minute);
                triggers.Add(t);
                log.Debug($"{logPrefix}{directive} created at time {hhmm.hour}:{hhmm.minute} --> " + t);
            }
        }
        else if (directive.In(Directive.Monthly))
        {
            while (triggerPartsQueue.IsNotEmpty())
            {
                var time = triggerPartsQueue.Dequeue();
                var (dayOfMonth, hour, minute) = ParseTimeDayOfMonthHHMM(time, directive);
                var t = CreateTriggerMonthly(dayOfMonth, hour: hour, minute: minute);
                triggers.Add(t);
                log.Debug($"{logPrefix}{directive} created for {dayOfMonth} {hour}:{minute} --> " + t);
            }
        }
        else
        {
            throw new NotImplementedException($"Trigger type {directive} has not been implemented yet");
        }

        for (int i = 0; i < triggers.Count; i++) log.Debug($"Created trigger[{i}]: " + triggers[i]);

        return triggers;
    }

    private static byte ParseTimeMM(string time, Directive directive)
    {
        time = ("00" + time).Right(2);
        if (!time.ToByteTry(out var minute)) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected MM");

        return minute;
    }

    private static (byte dayOfMonth, byte hour, byte minute) ParseTimeDayOfMonthHHMM(string time, Directive directive)
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

    private static (byte hour, byte minute) ParseTimeHHMM(string time, Directive directive)
    {
        log.Trace($"Parsing hours and minutes from {time}");
        var timeparts = time.Split(':').TrimOrNull().WhereNotNull().ToArray();
        if (timeparts.Length < 2) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected HH:MM. Expected : character in time {time}");
        if (timeparts.Length > 2) throw new Exception($"Error creating {directive} triggers from value {time}. Invalid time format, expected HH:MM. Encountered multiple : characters in time {time} but only 1 is allowed separating hours and minutes");
        var hours = timeparts[0].ToByte();
        var mins = timeparts[1].ToByte();
        log.Trace($"Parsed {hours} hours and {mins} minutes from {time}");
        return (hours, mins);
    }

    // ReSharper disable once UnusedParameter.Global
    public static MonthlyTrigger CreateTriggerMonthly(int dayOfMonth, int hour = 0, int minute = 0, int second = 0)
    {
        // TODO: Check daysOfMonth
        var t = new MonthlyTrigger(monthsOfYear: MonthsOfTheYear.AllMonths)
        {
            DaysOfMonth = new[] { dayOfMonth },
            StartBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute)
        };
        return t;
    }

    public static Trigger CreateTriggerInterval(TimeSpan interval)
    {
        var now = DateTime.Now;

        // remove everything but hour
        var nowHour = now.AddMinutes(now.Minute * -1).AddSeconds(now.Second * -1).AddMilliseconds(now.Millisecond * -1);

        // if it is 6:45 right now but our nowHour (6:00) + interval (22) has already passed then add an hour (7:00)
        if (now > (nowHour + interval)) nowHour = nowHour.AddHours(1);

        var trigger = new TimeTrigger();
        trigger.StartBoundary = nowHour;
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
}