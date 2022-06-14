// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MaxRunSoftware.Utilities.Console;

public class Program
{
    private static readonly ILogger log = LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
    private ConfigFile config;

    public static ILogFactory LogFactory => Utilities.LogFactory.LogFactoryImpl;

    public static List<Type> CommandTypes => typeof(Program).Assembly
        .GetTypesOf<ICommand>(requireNoArgConstructor: true)
        .OrderBy(o => o.FullNameFormatted())
        .ToList();

    public static List<ICommand> CommandObjects => CommandTypes
        .Select(CreateCommand)
        .ToList();

    private static ICommand CreateCommand(Type type)
    {
        return (ICommand)Activator.CreateInstance(type);
    }

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
        var logLevel = LogLevel.Info;
        if (a.IsTrace)
        {
            logLevel = LogLevel.Trace;
        }
        else if (a.IsDebug)
        {
            logLevel = LogLevel.Debug;
        }

        Utilities.LogFactory.LogFactoryImpl.SetupConsole(logLevel);
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

        config = new ConfigFile();

        var logFileName = config.LogFileName.TrimOrNull();
        if (logFileName != null)
        {
            var logFileLevel = config.LogFileLevel.TrimOrNull() ?? "info";
            logFileLevel = logFileLevel.ToLower();
            LogLevel level;
            if (logFileLevel.EqualsCaseInsensitive(LogLevel.Critical.ToString()))
            {
                level = LogLevel.Critical;
            }
            else if (logFileLevel.EqualsCaseInsensitive(LogLevel.Error.ToString()))
            {
                level = LogLevel.Error;
            }
            else if (logFileLevel.EqualsCaseInsensitive(LogLevel.Warn.ToString()))
            {
                level = LogLevel.Warn;
            }
            else if (logFileLevel.EqualsCaseInsensitive(LogLevel.Info.ToString()))
            {
                level = LogLevel.Info;
            }
            else if (logFileLevel.EqualsCaseInsensitive(LogLevel.Debug.ToString()))
            {
                level = LogLevel.Debug;
            }
            else if (logFileLevel.EqualsCaseInsensitive(LogLevel.Trace.ToString()))
            {
                level = LogLevel.Trace;
            }
            else
            {
                throw new Exception("Unrecognized file log level in config file: " + logFileLevel + "   valid values are TRACE/DEBUG/INFO/WARN/ERROR/CRITICAL");
            }

            Utilities.LogFactory.LogFactoryImpl.SetupFile(level, logFileName);
        }


        void ListCommand(ICommand c)
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
            foreach (var c in CommandObjects)
            {
                ListCommand(c);
            }

            return 2;
        }

        if (a.Command.Contains("*") || a.Command.Contains("?"))
        {
            foreach (var c in CommandObjects)
            {
                if (c.Name.EqualsWildcard(a.Command))
                {
                    ListCommand(c);
                }
            }

            return 5;
        }

        var command = commandTypes.FirstOrDefault(o => o.Name.Equals(a.Command, StringComparison.OrdinalIgnoreCase));
        if (command == null)
        {
            ShowBanner(a, null);
            log.Info($"Command '{a.Command}' does not exist");
            log.Info("Commands: ");
            foreach (var c in CommandObjects)
            {
                ListCommand(c);
            }

            return 3;
        }

        if (a.IsHelp)
        {
            ShowBanner(a, command);
            log.Info(CreateCommand(command).HelpDetails);
            return 4;
        }

        var cmd = CreateCommand(command);
        if (!cmd.SuppressBanner)
        {
            ShowBanner(a, command);
        }

        cmd.Args = args;
        cmd.Execute();
        return 0;
    }


    private void ShowBanner(Args a, Type command)
    {
        if (a.IsNoBanner)
        {
            return;
        }

        var suppressBanner = false;
        var programSuppressBanner = config.ProgramSuppressBanner.TrimOrNull();
        if (programSuppressBanner != null)
        {
            programSuppressBanner = programSuppressBanner.ToLower();
            if (programSuppressBanner.In("t", "true", "y", "yes", "1"))
            {
                suppressBanner = true;
            }
        }

        if (suppressBanner)
        {
            return;
        }

        var os = "";
        if (Constant.OS_WINDOWS)
        {
            os = " (Windows)";
        }
        else if (Constant.OS_UNIX)
        {
            os = " (Linux)";
        }
        else if (Constant.OS_MAC)
        {
            os = " (Mac)";
        }

        if (command == null)
        {
            log.Info(typeof(Program).Namespace + " " + Version.Value + os + " " + DateTime.Now.ToStringYYYYMMDDHHMMSS());
        }
        else
        {
            log.Info(typeof(Program).Namespace + " " + Version.Value + " : " + command.Name + os + " " + DateTime.Now.ToStringYYYYMMDDHHMMSS());
        }
    }
}
