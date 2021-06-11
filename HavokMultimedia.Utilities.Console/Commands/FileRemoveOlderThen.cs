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
using System.IO;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class FileRemoveOlderThenBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Removes file older then <" + AmountType + "> in a directory");
            help.AddParameter("recursive", "r", "Recursively remove files (false)");
            help.AddValue("<# of " + AmountType + "> <target directory>");
            help.AddExample("7 MyDirectory");
        }

        protected abstract string AmountType { get; }
        protected abstract DateTime CalcThreshold(DateTime now, int numberOf);

        protected override void ExecuteInternal()
        {
            var recursive = GetArgParameterOrConfigBool("recursive", "r", false);

            var numberOf = GetArgValueTrimmed(0);
            numberOf.CheckValueNotNull(nameof(numberOf), log);
            var numberOfInt = int.Parse(numberOf);
            log.DebugParameter(nameof(numberOfInt), numberOfInt);

            var targetDirectory = GetArgValueDirectory(1);

            var files = Util.FileListFiles(targetDirectory, recursive: recursive);

            var now = DateTime.UtcNow;
            var threshold = CalcThreshold(now, numberOfInt);

            log.Debug("Now UTC: " + now.ToStringYYYYMMDDHHMMSS());
            log.Debug("Date threshold UTC: " + threshold.ToStringYYYYMMDDHHMMSS());
            foreach (var file in files)
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(file);
                log.Debug(file + " [" + lastWriteTime.ToStringYYYYMMDDHHMMSS() + "]");
                if (lastWriteTime < threshold)
                {
                    log.Info("Removing file: " + file);
                    File.Delete(file);
                }
            }
        }
    }

    public class FileRemoveOlderThenDays : FileRemoveOlderThenBase
    {
        protected override string AmountType => "days";
        protected override DateTime CalcThreshold(DateTime now, int numberOf) => now.AddDays(numberOf * (-1));
    }

    public class FileRemoveOlderThenWeeks : FileRemoveOlderThenBase
    {
        protected override string AmountType => "weeks";
        protected override DateTime CalcThreshold(DateTime now, int numberOf) => now.AddDays((numberOf * 7) * (-1));
    }

    public class FileRemoveOlderThenMonths : FileRemoveOlderThenBase
    {
        protected override string AmountType => "months";
        protected override DateTime CalcThreshold(DateTime now, int numberOf) => now.AddMonths(numberOf * (-1));
    }

    public class FileRemoveOlderThenYears : FileRemoveOlderThenBase
    {
        protected override string AmountType => "years";
        protected override DateTime CalcThreshold(DateTime now, int numberOf) => now.AddYears(numberOf * (-1));
    }
}
