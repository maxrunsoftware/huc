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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32.TaskScheduler;

namespace MaxRunSoftware.Utilities.External;

public class WindowsTaskSchedulerPath : IEquatable<WindowsTaskSchedulerPath>, IComparable<WindowsTaskSchedulerPath>
{
    private static readonly IReadOnlyList<string> pathParseCharacters = (new[] { "/", "\\" }).ToList().AsReadOnly();

    public IReadOnlyList<string> PathFull { get; }
    public IReadOnlyList<string> Path
    {
        get
        {
            var path = PathFull.ToList();
            if (path.IsEmpty()) return path;
            path.PopTail();
            return path;
        }
    }
    public string Name
    {
        get
        {
            var path = PathFull.ToList();
            if (path.IsEmpty()) return null;
            return path.PopTail();
        }
    }
    public WindowsTaskSchedulerPath Parent
    {
        get
        {
            if (PathFull.IsEmpty()) return null;
            return new WindowsTaskSchedulerPath(Path);
        }
    }
    public WindowsTaskSchedulerPath(Task task) : this(task.Folder.Path + "/" + task.Name) { }
    public WindowsTaskSchedulerPath(IEnumerable<string> pathParts) => PathFull = pathParts.ToList();
    public WindowsTaskSchedulerPath(TaskFolder folder) : this(folder.Path) { }
    public WindowsTaskSchedulerPath(string path) : this(Util.PathParse(path ?? string.Empty, pathParseCharacters).TrimOrNull().WhereNotNull()) { }

    public override string ToString() => "/" + PathFull.ToStringDelimited("/");

    public bool Equals(WindowsTaskSchedulerPath other) => CompareTo(other) == 0;

    public int CompareTo(WindowsTaskSchedulerPath other)
    {
        if (other == null) return 1;
        var p1 = PathFull;
        var p2 = other.PathFull;
        if (ReferenceEquals(p1, p2)) return 0;
        if (p1 == null) return -1;
        if (p2 == null) return 1;
        return Util.Compare(p1, p2, StringComparer.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj) => Equals(obj as WindowsTaskSchedulerPath);
    public override int GetHashCode() => Util.GenerateHashCode(PathFull.Select(o => o.ToUpper()));

    public WindowsTaskSchedulerPath Add(string name) => new WindowsTaskSchedulerPath(PathFull.ToArray().Add(name));
}