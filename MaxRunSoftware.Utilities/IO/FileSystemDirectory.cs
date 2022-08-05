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

using System.Threading;

namespace MaxRunSoftware.Utilities;

public sealed class FileSystemDirectory : FileSystemObject
{
    private class RecursiveObjects
    {
        public readonly ICollection<FileSystemObject> objectsRecursive;
        public readonly ICollection<FileSystemDirectory> directoriesRecursive;
        public readonly ICollection<FileSystemFile> filesRecursive;

        public RecursiveObjects(ICollection<FileSystemObject> objectsRecursive, ICollection<FileSystemDirectory> directoriesRecursive, ICollection<FileSystemFile> filesRecursive)
        {
            this.objectsRecursive = objectsRecursive;
            this.directoriesRecursive = directoriesRecursive;
            this.filesRecursive = filesRecursive;
        }
    }


    private static readonly ILogger log = Constant.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly Lazy<long> size;

    public override bool IsExist => Directory.Exists(Path);
    public override long Size => size.Value;
    public override bool IsDirectory => true;

    private readonly Lazy<long> sizeRecursive;
    public long SizeRecursive => sizeRecursive.Value;

    private readonly Lazy<IReadOnlyCollection<FileSystemFile>> files;
    public IReadOnlyCollection<FileSystemFile> Files => files.Value;

    private readonly Lazy<IReadOnlyCollection<FileSystemDirectory>> directories;
    public IReadOnlyCollection<FileSystemDirectory> Directories => directories.Value;

    private readonly Lazy<IReadOnlyCollection<FileSystemObject>> objects;
    public IReadOnlyCollection<FileSystemObject> Objects => objects.Value;

    private readonly Lazy<RecursiveObjects> recursiveObjects;
    public ICollection<FileSystemObject> ObjectsRecursive => recursiveObjects.Value.objectsRecursive;
    public ICollection<FileSystemDirectory> DirectoriesRecursive => recursiveObjects.Value.directoriesRecursive;
    public ICollection<FileSystemFile> FilesRecursive => recursiveObjects.Value.filesRecursive;

    internal FileSystemDirectory(string path) : base(path)
    {
        files = new Lazy<IReadOnlyCollection<FileSystemFile>>(() => Directory.GetFiles(Path).Select(o => new FileSystemFile(o)).ToList(), LazyThreadSafetyMode.PublicationOnly);
        directories = new Lazy<IReadOnlyCollection<FileSystemDirectory>>(() => Directory.GetDirectories(Path).Select(o => new FileSystemDirectory(o)).ToList(), LazyThreadSafetyMode.PublicationOnly);
        objects = new Lazy<IReadOnlyCollection<FileSystemObject>>(() =>
        {
            var objs = new List<FileSystemObject>(Files.Count + Directories.Count);
            objs.AddRange(Files);
            objs.AddRange(Directories);
            return objs;
        }, LazyThreadSafetyMode.PublicationOnly);

        size = new Lazy<long>(() => GetSizes(Files), LazyThreadSafetyMode.PublicationOnly);
        sizeRecursive = new Lazy<long>(() => Size + GetSizes(DirectoriesRecursive), LazyThreadSafetyMode.PublicationOnly);
        recursiveObjects = new Lazy<RecursiveObjects>(GetObjectsRecursive, LazyThreadSafetyMode.PublicationOnly);
    }

    private long GetSizes(IEnumerable<FileSystemObject> enumerable)
    {
        long s = 0;
        foreach (var obj in enumerable)
        {
            try { s += obj.Size; }
            catch (Exception e) { log.Warn($"Error reading file size from {Path}  --> {e.Message}"); }
        }

        return s;
    }

    private RecursiveObjects GetObjectsRecursive()
    {
        var setDirectories = new HashSet<FileSystemDirectory>();
        var setFiles = new HashSet<FileSystemFile>();

        var queue = new Queue<FileSystemDirectory>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var currentDirectory = queue.Dequeue();

            // ignore links to other paths
            if (!IsParentOf(currentDirectory)) continue;

            IReadOnlyCollection<FileSystemDirectory> currentDirectoryDirectories = null;
            try { currentDirectoryDirectories = currentDirectory.Directories; }
            catch (Exception e) { log.Warn($"Error reading directory list from {currentDirectory.Path}  --> {e.Message}"); }

            IReadOnlyCollection<FileSystemFile> currentDirectoryFiles = null;
            try { currentDirectoryFiles = currentDirectory.Files; }
            catch (Exception e) { log.Warn($"Error reading directory list from {currentDirectory.Path}  --> {e.Message}"); }

            foreach (var d in currentDirectoryDirectories.OrEmpty())
            {
                if (ReferenceEquals(this, d)) continue;
                if (!IsParentOf(d)) continue;
                if (!setDirectories.Add(d)) continue;
                queue.Enqueue(d);
            }

            foreach (var f in currentDirectoryFiles.OrEmpty())
            {
                if (!IsParentOf(f)) continue;
                setFiles.Add(f);
            }
        }

        var listObjects = new List<FileSystemObject>(setDirectories.Count + setFiles.Count);
        listObjects.AddRange(setDirectories);
        listObjects.AddRange(setFiles);

        return new RecursiveObjects(listObjects, setDirectories, setFiles);
    }
}
