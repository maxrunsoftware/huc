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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;



namespace HavokMultimedia.Utilities.Console
{

    public class Program
    {
        public static ILogFactory LOGFACTORY { get { return LogFactory.LogFactoryImpl; } }
        private static readonly ILogger log = LOGFACTORY.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);




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
            LogFactory.LogFactoryImpl.SetupConsole(a.IsDebug);
            log.Debug(a.ToString());

            var commandTypes = GetCommandTypes();

            try
            {
                ConfigFile.CreateDefaultPropertiesFile();
            }
            catch (Exception e)
            {
                log.Warn("Could not create default properties file", e);
            }

            if (a.Command == null)
            {
                log.Info("No command specified");
                log.Info("Commands: ");
                foreach (var c in GetCommandObjects()) log.Info("  " + c.HelpSummary);
                return 2;
            }
            var command = commandTypes.Where(o => o.Name.Equals(a.Command, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (command == null)
            {
                log.Info($"Command '{a.Command}' does not exist");
                log.Info("Commands: ");
                foreach (var c in GetCommandObjects()) log.Info("  " + c.HelpSummary);
                return 3;
            }
            if (a.Values.IsEmpty() && a.Parameters.IsEmpty())
            {
                log.Info(CreateCommand(command).HelpDetails);
                return 4;
            }
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            log.Info(typeof(Program).Namespace + " " + version + " (" + command.Name + ")");
            var cmd = CreateCommand(command);
            cmd.Execute(args);
            return 0;
        }

        private static ICommand CreateCommand(Type type) => (ICommand)Activator.CreateInstance(type);


        public static List<Type> GetCommandTypes() => typeof(Program).Assembly
                .GetTypes()
                .Where(o => typeof(ICommand).IsAssignableFrom(o))
                .Where(o => !o.IsInterface)
                .Where(o => !o.IsAbstract)
                .OrderBy(o => o.FullNameFormatted())
                .ToList();

        public static List<ICommand> GetCommandObjects() => GetCommandTypes()
            .Select(o => CreateCommand(o))
            .ToList();

    }

}
