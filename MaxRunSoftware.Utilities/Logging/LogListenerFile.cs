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

public class LogListenerFile
{
    public LogLevel ListenerLevel { get; }

    private readonly string filename;
    private readonly FileInfo file;
    private readonly string id;

    public LogListenerFile(LogLevel listenerLevel, string filename)
    {
        ListenerLevel = listenerLevel;
        this.filename = Path.GetFullPath(filename.CheckNotNullTrimmed(nameof(filename)));
        file = new FileInfo(filename);
        id = Guid.NewGuid().ToString().Replace("-", "");
    }

    public void Log(LogEventArgs args)
    {
        if (args == null) return;

        using (MutexLock.CreateGlobal(TimeSpan.FromSeconds(10), file)) { Util.FileWrite(filename, args.ToStringDetailed(id: id) + Environment.NewLine, Constant.Encoding_UTF8, true); }
    }
}
