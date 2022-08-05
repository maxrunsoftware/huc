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

using MaxRunSoftware.Utilities.CommandLine;

namespace MaxRunSoftware.Utilities.Tests.CommandLine;

public abstract class CommandTestsBase : TestBase
{
    // ReSharper disable once PublicConstructorInAbstractClass
    public CommandTestsBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    public const string NAME = "My Command";
    public const string DESCRIPTION = "Some Description";
    public const string PROPERTY = "Value";


    protected (T o, CommandScannerResult r) Scan<T>() where T : new()
    {
        var o = new T();
        var r = new CommandScanner().AddType(o.GetType()).Scan();
        r.ShowErrors(Info);
        return (o, r);
    }
}
