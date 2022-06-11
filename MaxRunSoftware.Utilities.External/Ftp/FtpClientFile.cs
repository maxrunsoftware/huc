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

namespace MaxRunSoftware.Utilities.External;

public class FtpClientFile
{
    public string Name { get; }

    public string FullName { get; }

    public FtpClientFileType Type { get; }

    public FtpClientFile(string name, string fullName, FtpClientFileType type)
    {
        Name = name;
        FullName = fullName;
        Type = type;
    }

    /// <summary>
    /// Checks to see if our FullName matches a pathOrPattern value.
    /// if FullName=/dir1/file1.txt  pathOrPattern=/*/file?.txt  isCaseSensitive=true  IsMatch=true
    /// if FullName=/dir1/file1.txt  pathOrPattern=/*/FILE?.TXT  isCaseSensitive=true  IsMatch=false
    /// </summary>
    /// <param name="pathOrPattern"></param>
    /// <param name="isCaseSensitive"></param>
    /// <returns></returns>
    public bool IsMatch(string pathOrPattern, bool isCaseSensitive)
    {
        pathOrPattern = pathOrPattern.CheckNotNullTrimmed(nameof(pathOrPattern));
        var source = pathOrPattern.StartsWith("/") ? FullName : Name;
        return source.EqualsWildcard(pathOrPattern, !isCaseSensitive);
    }
}