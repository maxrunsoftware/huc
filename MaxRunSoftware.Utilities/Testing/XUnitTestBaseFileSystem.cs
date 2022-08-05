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

namespace MaxRunSoftware.Utilities;

public abstract partial class XUnitTestBase
{
    private string workingDirectoryCached;

    protected string WorkingDirectory
    {
        get
        {
            lock (lockerStatic)
            {
                if (workingDirectoryCached != null) return workingDirectoryCached;

                var tempDirectory = ConfigTempDirectory.CheckPropertyNotNull(nameof(ConfigTempDirectory), getType);
                tempDirectory = Path.GetFullPath(tempDirectory);
                tempDirectory.CheckDirectoryExists(nameof(ConfigTempDirectory));

                var workingDirectoryName = TestNameClass + "_" + TestNameMethod;
                var workingDirectoryPath = Path.GetFullPath(Path.Join(tempDirectory, workingDirectoryName));

                if (Directory.Exists(workingDirectoryPath))
                {
                    Warn("WorkingDirectory already exists so deleting " + workingDirectoryPath);
                    Directory.Delete(workingDirectoryPath, true);
                }

                Debug("Creating WorkingDirectory " + workingDirectoryPath);
                var di = Directory.CreateDirectory(workingDirectoryPath);

                workingDirectoryCached = di.FullName;

                disposeAlso.Add(() =>
                {
                    Debug("Deleting WorkingDirectory " + workingDirectoryCached);
                    Directory.Delete(workingDirectoryCached, true);
                });

                return workingDirectoryCached;
            }
        }
    }

    public string NextFileName() => NextFileName(null);
    public string NextFileName(string extension) => "test" + NextInt() + (extension == null ? "" : "." + extension);


    protected string WriteFile(string path, string text, Encoding encoding = null, bool generateFileName = false) =>
        WriteFile(
            path,
            (encoding ?? Constant.Encoding_UTF8).GetBytes(text),
            generateFileName,
            "txt"
        );

    protected string WriteFile(string path, byte[] data, bool generateFileName = false, string generatedFileExtension = "dat")
    {
        if (generateFileName)
        {
            string p;
            do { p = GetPath(path + "/" + NextFileName(generatedFileExtension)); } while (File.Exists(p) || Directory.Exists(p));

            path = p;
        }
        else
        {
            path = GetPath(path);
            if (string.Equals(path, WorkingDirectory, Constant.Path_StringComparison)) throw new Exception("No filename specified");
        }

        var filename = Path.GetFileName(path);
        if (filename == null) throw new Exception("No filename specified");

        var directory = Path.GetDirectoryName(path);
        if (directory == null) throw new Exception("Could not determine directory from path " + path);

        if (!Directory.Exists(directory))
        {
            Debug("Creating directory " + directory);
            Directory.CreateDirectory(directory);
        }

        //var path = Path.GetFullPath(Path.Combine(pathDir, fileName.ToString()));
        if (DoesFileExist(path)) { Debug($"Overwriting existing file ({data.Length})  " + path); }
        else { Debug($"Writing data to file ({data.Length})  {path}"); }

        //File.WriteAllBytes(path, data);
        Util.FileWrite(path, data);

        return path;
    }

    protected bool DoesFileExist(string path) => File.Exists(GetPath(path));

    protected bool DoesDirectoryExist(string path) => Directory.Exists(GetPath(path));

    protected string GetPath(string relativePath) =>
        Path.GetFullPath(
            Path.Combine(
                WorkingDirectory
                    .Yield()
                    .ToArray()
                    .AppendTail(
                        relativePath.OrEmpty()
                            .SplitOnDirectorySeparator()
                            .Where(o => o.TrimOrNull() != null)
                            .ToArray()
                    )));

    protected string CreateDirectory(string path)
    {
        var pathFull = GetPath(path);
        if (pathFull == null) throw new IOException("Could not determine full path from " + path);
        if (Directory.Exists(pathFull))
        {
            Debug("Skipping creating directories because they already exist " + pathFull);
            return pathFull;
        }

        Debug("Creating directories " + pathFull);
        return Directory.CreateDirectory(pathFull).FullName;
    }
}
