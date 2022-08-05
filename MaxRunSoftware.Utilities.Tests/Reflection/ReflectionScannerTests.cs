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

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public class ReflectionScannerTests : TestBase
{
    public ReflectionScannerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    [Fact]
    public void EqualsWork()
    {
        /*
        var rs = new ReflectionScanner();
        //rs.ScanVisibleAssemblies();
        var assemblies = rs.AssembliesScanned.OrderBy(o => o).ToList();
        Info($"Assemblies {assemblies.Count}");
        foreach (var a in assemblies) Debug("  " + a);

        Info("");

        var types = rs.Types
            .OrderBy(o => o.Key)
            .Where(o => o.Key.ToString() == "MaxRunSoftware.Utilities")
            //.Where(o => o.ToString().Contains("IBucketReadOnly"))
            //.Where(o => o.ToString().Contains("WhereNotNull"))
            //.Where(o => o.Type.IsCompilerGenerated())
            //.Where(o => !o.name.Contains("+<>c"))
            .SelectMany(o => o.Value)
            .ToList();
        Info($"Types {types.Count}");
        var i = 0;
        foreach (var t in types)
        {
            i++;
            var tt = t.Type;
            var items = new object[]
            {
                tt.Name,
                tt.FullName,
                tt.DeclaringType,
                "-----",
                //tt.DeclaringMethod,
                tt.MemberType,
                tt.ReflectedType,
                tt.AssemblyQualifiedName,
                "-----",
                tt.IsGenericTypeDefinition,
                tt.IsTypeDefinition,
                tt.IsGenericTypeParameter,
                "-----"
                //tt.IsCompilerGenerated()
            };

            Info($"  [{i}] {t}");
            foreach (var item in items) Info("          " + item);

            */
    }
}
