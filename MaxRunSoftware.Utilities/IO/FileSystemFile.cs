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

public sealed class FileSystemFile : FileSystemObject
{
    //private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly Lazy<long> size;

    public override bool IsExist => File.Exists(Path);
    public override long Size => size.Value;
    public override bool IsDirectory => false;

    internal FileSystemFile(string path) : base(path)
    {
        size = new Lazy<long>(() =>
        {
            if (!IsReparsePoint) return FileInfo.Length;

            // https://stackoverflow.com/a/57454136
            using (Stream fs = Util.FileOpenRead(Path)) { return fs.Length; }
        }, LazyThreadSafetyMode.PublicationOnly);
    }

    public byte[] Read() => Util.FileRead(Path);

    public string Read(Encoding encoding) => encoding.GetString(Read());
}
