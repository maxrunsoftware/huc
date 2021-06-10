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
    public class FileRemoveOlderThenDays : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Removes file older then <days> in a directory");
            help.AddParameter("recursive", "r", "Recursively remove files (false)");
            help.AddValue("<# of days> <target directory>");
            help.AddExample("7 MyDirectory");
        }

        protected override void ExecuteInternal()
        {
            var recursive = GetArgParameterOrConfigBool("recursive", "r", false);

            var numberOf = GetArgValueTrimmed(0);
            log.Debug(nameof(numberOf) + ": " + numberOf);
            if (numberOf == null) throw ArgsException.ValueNotSpecified(nameof(numberOf));
            var numberOfInt = int.Parse(numberOf);
            log.Debug(nameof(numberOfInt) + ": " + numberOfInt);

            var targetDirectory = GetArgValueTrimmed(1);
            log.Debug(nameof(targetDirectory) + ": " + targetDirectory);
            if (targetDirectory == null) throw ArgsException.ValueNotSpecified(nameof(targetDirectory));
            targetDirectory = Path.GetFullPath(targetDirectory);
            log.Debug(nameof(targetDirectory) + ": " + targetDirectory);
            if (!Directory.Exists(targetDirectory)) throw new ArgsException(nameof(targetDirectory), "Target directory " + targetDirectory + " not found");
            var files = Util.FileListFiles(targetDirectory, recursive: recursive);

            var now = DateTime.UtcNow;
            var then = now.AddDays(numberOfInt * (-1));
            log.Debug("Date threshold UTC: " + then.ToStringYYYYMMDDHHMMSS());
            foreach (var file in files)
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(file);
                log.Debug(file + " [" + lastWriteTime.ToStringYYYYMMDDHHMMSS() + "]");
                if (lastWriteTime < then)
                {
                    log.Info("Removing file: " + file);
                    File.Delete(file);
                }


            }

        }
    }
}
