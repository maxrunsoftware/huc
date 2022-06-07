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

using System.Threading;

namespace MaxRunSoftware.Utilities;

public class StreamReaderThread : ThreadBase
{
    private readonly StreamReader reader;
    private readonly Action<char> output;
    public StreamReaderThread(StreamReader reader, Action<char> output)
    {
        this.reader = reader.CheckNotNull(nameof(reader));
        this.output = output.CheckNotNull(nameof(output));
    }
    protected override void Work()
    {
        while (true)
        {
            Thread.Sleep(5);
            var i = reader.Read();
            if (i < 0) continue;
            var c = (char)i;
            output(c);
        }
    }
}
