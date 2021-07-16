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

using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace MaxRunSoftware.Utilities.External
{
    public class Zip
    {
        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void AddFileToZip(FileInfo file, DirectoryInfo baseDirectoryToRemove, ZipOutputStream zos, int bufferSize, string zipFileName, bool encrypt = false)
        {
            var entryPath = CleanName(file, baseDirectoryToRemove, false);
            log.Debug($"Adding: {file.FullName} --> {zipFileName}/{entryPath}");
            var newEntry = new ZipEntry(entryPath)
            {
                DateTime = file.LastWriteTime,
                Size = file.Length
            };
            if (encrypt) newEntry.AESKeySize = 256;
            zos.PutNextEntry(newEntry);
            //var buffer = new byte[bufferSize];
            using (var fs = Util.FileOpenRead(file.FullName))
            {
                //StreamUtils.Copy(fs, zos, buffer);
                fs.CopyTo(zos);
            }
            zos.CloseEntry();
            log.Info($"Added: {file.FullName} --> {zipFileName}/{entryPath}");
        }

        public static void AddDirectoryToZip(DirectoryInfo directory, DirectoryInfo baseDirectoryToRemove, ZipOutputStream zos, string zipFileName, bool encrypt = false)
        {
            var entryPath = CleanName(directory, baseDirectoryToRemove, true);
            log.Debug($"Adding: {directory.FullName} --> {zipFileName}/{entryPath}");
            var newEntry = new ZipEntry(entryPath)
            {
                DateTime = directory.LastWriteTime,
            };
            if (encrypt) newEntry.AESKeySize = 256;
            zos.PutNextEntry(newEntry);
            zos.CloseEntry();
            log.Info($"Added: {directory.FullName} --> {zipFileName}/{entryPath}");
        }

        private static string CleanName(FileSystemInfo item, DirectoryInfo baseDirectoryToRemove, bool isDirectory)
        {
            var pathParts = item.RemoveBase(baseDirectoryToRemove);
            var newPath = pathParts.ToStringDelimited("/");
            if (isDirectory) newPath = newPath + "/";
            newPath = ZipEntry.CleanName(newPath);
            return newPath;
        }
    }
}
