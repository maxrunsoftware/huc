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

namespace MaxRunSoftware.Utilities.Console
{

    public class Program
    {
        private static readonly ILogger log = LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static ILogFactory LogFactory { get { return Utilities.LogFactory.LogFactoryImpl; } }

        public static List<Type> CommandTypes => typeof(Program).Assembly
            .GetTypesOf<ICommand>(requireNoArgConstructor: true)
            .OrderBy(o => o.FullNameFormatted())
            .ToList();

        public static List<ICommand> CommandObjects => CommandTypes
            .Select(o => CreateCommand(o))
            .ToList();

        private static ICommand CreateCommand(Type type) => (ICommand)Activator.CreateInstance(type);

        public static int Main(string[] args)
        {
            int returnValue;
            try
            {
                var p = new Program();
                returnValue = p.Run(args);
            }
            catch (Exception e)
            {
                returnValue = 1;
                log.Error("Error", e);
            }

            return returnValue;
        }

        private int Run(string[] args)
        {
            var a = new Args(args);
            Utilities.LogFactory.LogFactoryImpl.SetupConsole(a.IsDebug);
            log.Debug(a.ToString());

            var commandTypes = CommandTypes;

            try
            {
                ConfigFile.CreateDefaultPropertiesFile();
            }
            catch (Exception e)
            {
                log.Warn("Could not create default properties file", e);
            }

            void listCommand(ICommand c)
            {
                if (a.IsShowHidden || !c.IsHidden)
                {
                    log.Info((c.IsHidden ? "* " : "  ") + c.HelpSummary);
                }
            }

            if (a.IsVersion || (a.Command != null && a.Command.EqualsCaseInsensitive("VERSION")))
            {
                ShowBanner(a, null);
                return 6;
            }
            if (a.Command == null)
            {
                ShowBanner(a, null);
                log.Info("No command specified");
                log.Info("Commands: ");
                foreach (var c in CommandObjects) listCommand(c);
                return 2;
            }
            if (a.Command.Contains("*") || a.Command.Contains("?"))
            {
                foreach (var c in CommandObjects) if (c.Name.EqualsWildcard(a.Command)) listCommand(c);
                return 5;
            }
            var command = commandTypes.Where(o => o.Name.Equals(a.Command, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (command == null)
            {
                ShowBanner(a, null);
                log.Info($"Command '{a.Command}' does not exist");
                log.Info("Commands: ");
                foreach (var c in CommandObjects) listCommand(c);
                return 3;
            }
            if (a.IsHelp)
            {
                ShowBanner(a, command);
                log.Info(CreateCommand(command).HelpDetails);
                return 4;
            }

            var cmd = CreateCommand(command);
            if (!cmd.SuppressBanner) ShowBanner(a, command);

            cmd.Args = args;
            cmd.Execute();
            return 0;
        }


        private static void ShowBanner(Args a, Type command)
        {
            if (a.IsNoBanner) return;
            string os = "";
            if (Constant.OS_WINDOWS) os = " (Windows)";
            else if (Constant.OS_UNIX) os = " (Linux)";
            else if (Constant.OS_MAC) os = " (Mac)";

            if (command == null) log.Info(typeof(Program).Namespace + " " + MaxRunSoftware.Utilities.Console.Version.Value + os);
            else log.Info(typeof(Program).Namespace + " " + MaxRunSoftware.Utilities.Console.Version.Value + " : " + command.Name + os);

        }


    }

}
