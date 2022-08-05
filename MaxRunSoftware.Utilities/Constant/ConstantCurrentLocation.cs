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

using System.Diagnostics;

namespace MaxRunSoftware.Utilities;

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    public static readonly ImmutableArray<string> Path_Current_Locations = ImmutableArray.Create(Path_Current_Locations_Create().ToArray());

    private static List<string> Path_Current_Locations_Create()
    {
        // https://stackoverflow.com/questions/616584/how-do-i-get-the-name-of-the-current-executable-in-c

        var list = new List<string>();

        try { list.Add(Environment.CurrentDirectory); }
        catch { }

        try { list.Add(Process.GetCurrentProcess().MainModule?.FileName); }
        catch { }

        try { list.Add(AppDomain.CurrentDomain.FriendlyName); }
        catch { }

        try { list.Add(Process.GetCurrentProcess().ProcessName); }
        catch { }

        try { list.Add(typeof(Constant).Assembly.Location); }
        catch { }

        try { list.Add(Path.GetFullPath(".")); }
        catch { }

        var set = new HashSet<string>(Path_StringComparer);

        var list2 = new List<string>();
        foreach (var item in list)
        {
            var item2 = TrimOrNull(item);
            if (item2 == null) continue;

            try { item2 = Path.GetFullPath(item2); }
            catch { }

            try
            {
                if (!File.Exists(item2) && !Directory.Exists(item2))
                {
                    if (File.Exists(item2 + ".exe"))
                        item2 += ".exe";
                    else
                        continue;
                }
            }
            catch { }

            if (!set.Add(item2)) continue;

            list2.Add(item2);
        }

        return list2;
    }

    public static readonly ImmutableArray<string> Path_Current_Directories = ImmutableArray.Create(GetCurrentLocationsDirectory().ToArray());

    private static List<string> GetCurrentLocationsDirectory()
    {
        var list = new List<string>();
        var set = new HashSet<string>(Path_IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (var location in Path_Current_Locations_Create())
        {
            try
            {
                if (Directory.Exists(location))
                {
                    if (set.Add(location)) list.Add(location);
                }
                else if (File.Exists(location))
                {
                    var location2 = Path.GetDirectoryName(location);
                    if (location2 != null)
                    {
                        location2 = Path.GetFullPath(location2);
                        if (Directory.Exists(location2))
                            if (set.Add(location2))
                                list.Add(location2);
                    }
                }
            }
            catch { }
        }

        return list;
    }

    public static readonly string Path_Current_Directory = Path_Current_Directories.FirstOrDefault();

    public static readonly ImmutableArray<string> Path_Current_Files = ImmutableArray.Create(GetCurrentLocationsFile().ToArray());

    private static List<string> GetCurrentLocationsFile()
    {
        var list = new List<string>();
        var set = new HashSet<string>(Path_IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (var location in Path_Current_Locations_Create())
        {
            try
            {
                if (File.Exists(location))
                    if (set.Add(location))
                        list.Add(location);
            }
            catch { }
        }

        return list;
    }

    /// <summary>
    /// The current EXE file name. Could be a full file path, or a partial file path, or null
    /// </summary>
    public static readonly string Path_Current_File = Path_Current_Files.FirstOrDefault();

    /// <summary>
    /// Are we executing via a batch file or script or running the command directly from the console window?
    /// </summary>
    public static readonly bool IsScriptExecuted = IS_BATCHFILE_EXECUTED_get();

    private static bool IS_BATCHFILE_EXECUTED_get()
    {
        try
        {
            // http://stackoverflow.com/questions/3453220/how-to-detect-if-console-in-stdin-has-been-redirected?lq=1
            //return (0 == (System.Console.WindowHeight + System.Console.WindowWidth)) && System.Console.KeyAvailable;
            if (Console.WindowHeight != 0) return false;

            if (Console.WindowWidth != 0) return false;

            if (!Console.KeyAvailable) return false;

            return true;
        }
        catch (Exception e) { LogError(e); }

        return false;
    }
}
