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
using System.Globalization;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class Time : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Retrieves the current time from the internet");
            help.AddParameter(nameof(drift), "d", "Show the difference between internet time and local time (false)");
            help.AddExample("");
            help.AddExample("-d");
        }

        private bool drift;

        protected override void ExecuteInternal()
        {
            drift = GetArgParameterOrConfigBool(nameof(drift), "d", false);

            var internetTime = Util.NetGetInternetDateTime();
            // Strip milliseconds
            internetTime = DateTime.ParseExact(internetTime.ToString("yyyy-MM-dd HH:mm:ss"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            var localTime = DateTime.Now;
            // Strip milliseconds
            localTime = DateTime.ParseExact(localTime.ToString("yyyy-MM-dd HH:mm:ss"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);



            if (!drift)
            {
                log.Info(internetTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                log.Info("Remote: " + internetTime.ToString("yyyy-MM-dd HH:mm:ss"));
                log.Info("Local:  " + localTime.ToString("yyyy-MM-dd HH:mm:ss"));
                var dr = internetTime - localTime;
                var msg = "Drift: ";
                if (dr.Ticks < 0) msg = msg + "+";
                else if (dr.Ticks > 0) msg = msg + "-";
                msg = msg + dr.Duration().TotalSeconds.Round(MidpointRounding.AwayFromZero, 0);
                msg = msg + " seconds";
                log.Info(msg);
            }


        }
    }
}
