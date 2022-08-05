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

using System.Runtime.InteropServices;

namespace MaxRunSoftware.Utilities;

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    /// <summary>
    /// Operating System are we currently running
    /// </summary>
    public static readonly OSPlatform OS = OS_Create();

    private static OSPlatform OS_Create()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OSPlatform.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OSPlatform.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OSPlatform.OSX;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return OSPlatform.FreeBSD;
        }
        catch (Exception e) { LogError(e); }

        // Unknown OS
        return OSPlatform.Windows;
    }

    /// <summary>
    /// Are we running on a Windows platform?
    /// </summary>
    public static readonly bool OS_Windows = OS == OSPlatform.Windows;

    /// <summary>
    /// Are we running on a UNIX/LINUX platform?
    /// </summary>
    public static readonly bool OS_Unix = OS == OSPlatform.Linux || OS == OSPlatform.FreeBSD;

    /// <summary>
    /// Are we running on a Mac/Apple platform?
    /// </summary>
    public static readonly bool OS_Mac = OS == OSPlatform.OSX;

    /// <summary>
    /// Are we running on a 32-bit operating system?
    /// </summary>
    public static readonly bool OS_x32 = !Environment.Is64BitOperatingSystem;

    /// <summary>
    /// Are we running on a 64-bit operating system?
    /// </summary>
    public static readonly bool OS_x64 = Environment.Is64BitOperatingSystem;
}
