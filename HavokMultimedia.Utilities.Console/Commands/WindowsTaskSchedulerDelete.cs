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

using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WindowsTaskSchedulerDelete : WindowsTaskSchedulerBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Deletes a task from the Windows Task Scheduler");
            help.AddValue("<task path>");
            help.AddExample("-h=`localhost` -u=`administrator` -p=`password` MyTask");
        }

        protected override void Execute()
        {
            base.Execute();

            var taskPath = GetArgValueTrimmed(0);
            if (taskPath == null) throw new ArgsException(nameof(taskPath), "No task path specified");

            using (var scheduler = GetTaskScheduler())
            {
                var t = scheduler.GetTask(taskPath);
                if (t == null) throw new ArgsException(nameof(taskPath), "Task does not exist " + taskPath);
                log.Debug("Deleting task " + t.NameFull());
                scheduler.TaskDelete(t);
                log.Info("Successfully deleted task " + t.NameFull());
            }

        }




    }
}
