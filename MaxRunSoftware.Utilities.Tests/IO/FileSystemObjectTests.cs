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

namespace MaxRunSoftware.Utilities.Tests.IO;

/*
// ReSharper disable StringLiteralTypo
public abstract class FileSystemObjectTestBase : TestBase
{
    protected FileSystemObjectTestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected FileSystemObject O(string path, bool create = false) => path.EndsWithAny(StringComparison.OrdinalIgnoreCase, ".txt") ? F(path, create) : D(path, create);

    protected FileSystemDirectory D(string path, bool create = false)
    {
        if (create) CreateDirectory(path);
        return FileSystemObject.GetDirectory(GetPath(path));
    }

    protected FileSystemFile F(string path, bool create = false)
    {
        if (create) WriteFile(path, RandomText());
        return FileSystemObject.GetFile(GetPath(path));
    }
}

public class FileSystemObjectTests : FileSystemObjectTestBase
{
    public FileSystemObjectTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    [TestTheory]
    [InlineData("..", ".")]
    [InlineData("/one", "/one/two")]
    [InlineData("/one", "/one/two/three")]
    [InlineData("/one/two", "/one/two/three")]
    [InlineData("/one", "/two", false)]
    [InlineData("/one/two/three", "/one/four/five", false)]
    public void IsParentOf(string parent, string child, bool expected = true)
    {
        var p = O(parent, true);
        var c = O(child, true);

        if (expected)
        {
            Assert.True(p.IsParentOf(c));
            Assert.False(c.IsParentOf(p));
        }
        else
        {
            Assert.False(p.IsParentOf(c));
            Assert.False(c.IsParentOf(p));
        }
    }


    [TestTheory]
    [InlineData(0, 0, "/")]
    [InlineData(1, 1, "/", "/one")]
    [InlineData(1, 2, "/", "/one/two")]
    [InlineData(1, 3, "/", "/one/two/three")]
    [InlineData(2, 4, "/", "/one/two/three", "/four")]
    [InlineData(2, 5, "/", "/one/two/three", "/four/five")]
    [InlineData(2, 6, "/", "/one/two/three", "/four/five/six")]
    [InlineData(3, 7, "/", "/one/two/three", "/four/five/six", "/seven")]
    [InlineData(3, 8, "/", "/one/two/three", "/four/five/six", "/seven/eight")]
    [InlineData(3, 9, "/", "/one/two/three", "/four/five/six", "/seven/eight/nine")]
    public void Directories(int count, int countRecursive, string basePath, params string[] paths)
    {
        var b = D(basePath);

        foreach (var path in paths) O(path, true);
        Assert.Equal(count, b.Directories.Count);
        Assert.Equal(countRecursive, b.DirectoriesRecursive.Count);
    }
}

public class FileSystemFileTests : FileSystemObjectTestBase
{
    public FileSystemFileTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
}

public class FileSystemDirectoryTests : FileSystemObjectTestBase
{
    public FileSystemDirectoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
}
*/
