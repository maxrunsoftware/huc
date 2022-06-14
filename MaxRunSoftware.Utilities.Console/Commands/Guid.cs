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

namespace MaxRunSoftware.Utilities.Console.Commands
{
    [SuppressBanner]
    public class Guid : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Generates GUIDs");
            help.AddValue("<number of GUIDs>");
            help.AddExample("");
            help.AddExample("6");
        }

        protected override void ExecuteInternal()
        {
            var numberOfGuidsString = GetArgValueTrimmed(0);
            if (numberOfGuidsString == null) numberOfGuidsString = "1";
            var numberOfGuids = numberOfGuidsString.ToInt();

            for (int i = 0; i < numberOfGuids; i++)
            {
                log.Info(System.Guid.NewGuid().ToString());
            }
        }
    }
}
