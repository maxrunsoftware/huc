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

using Xunit.Sdk;

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public class ReflectionAssemblyTests : TestBase
{
    private readonly ITestOutputHelper output;

    public ReflectionAssemblyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        output = testOutputHelper;
    }

    private record Domain_Scan_Full_Record(IReflectionAssembly Assembly, int TypesCount, int C, int E, int F, int M, int P, int MembersCount);

    private void Log<T>(ReflectionDomain domain)
    {
        LogDisablePrefix = true;
        Info();
        var msgLoaded = "";
        if (typeof(T) != typeof(DateOnly))
        {
            msgLoaded = "Loaded: " + typeof(T).FullNameFormatted();
            domain.GetType<T>();
        }


        var assemblies = domain.GetAssemblies().Values.ToList();
        var list = new List<Domain_Scan_Full_Record>();
        foreach (var assembly in assemblies.OrderByOrdinalThenOrdinalIgnoreCase(o => o!.Name))
        {
            var types = assembly!.GetTypesCache().Values.ToList();
            int c, e, f, m, p;
            c = e = f = m = p = 0;

            if (!assembly.IsSystemAssembly)
            {
                var typesFiltered = types.Where(o => o.IsRealType).ToList();
                c = typesFiltered.Sum(o => o.Constructors.Count);
                e = typesFiltered.Sum(o => o.Events.Count);
                f = typesFiltered.Sum(o => o.Fields.Count);
                m = typesFiltered.Sum(o => o.Methods.Count);
                p = typesFiltered.Sum(o => o.Properties.Count);
            }

            //if (types.Count < 1000) continue;
            var total = c + e + f + m + p;

            list.Add(new Domain_Scan_Full_Record(assembly, types.Count, c, e, f, m, p, total));
        }

        Info($"Assemblies: {assemblies.Count,-3}    {msgLoaded}");

        list = list.OrderBy(o => o.Assembly.Name).ToList();
        if (list.Count == 0) return;

        var padTypes = list.Max(o => o.TypesCount.ToString().Length);
        var padMembers = list.Max(o => o.MembersCount.ToString().Length);
        var padAssembly = list.Max(o => o.Assembly.Name.Length);
        var padC = list.Max(o => o.C.ToString().Length);
        var padE = list.Max(o => o.E.ToString().Length);
        var padF = list.Max(o => o.F.ToString().Length);
        var padM = list.Max(o => o.M.ToString().Length);
        var padP = list.Max(o => o.P.ToString().Length);

        foreach (var record in list)
        {
            var sb = new StringBuilder();
            sb.Append($"  {record.Assembly.Name.PadRight(padAssembly)}");
            sb.Append($"  {record.TypesCount.ToString().PadLeft(padTypes)}");
            if (record.MembersCount > 0)
            {
                sb.Append($"  {record.MembersCount.ToString().PadLeft(padMembers)}");
                sb.Append($"   C:{record.C.ToString().PadRight(padC)}");
                sb.Append($"  E:{record.E.ToString().PadRight(padE)}");
                sb.Append($"  F:{record.F.ToString().PadRight(padF)}");
                sb.Append($"  M:{record.M.ToString().PadRight(padM)}");
                sb.Append($"  P:{record.P.ToString().PadRight(padP)}");
            }

            if (record.MembersCount > 0) Info(sb.ToString());
        }
    }

    [TestFact]
    public void Domain_Scan_Full()
    {
        var domain = new ReflectionDomain();

        Log<DateOnly>(domain);
        Log<ReflectionAssemblyTests>(domain);
        Log<IReflectionAssembly>(domain);
        Log<FactAttribute>(domain);
        Log<FactDiscoverer>(domain);
        Log<int>(domain);
        Log<DateOnly>(domain);
    }

    [TestFact]
    public void Domain_Scan_None()
    {
        var domain = new ReflectionDomain();

        Log<DateOnly>(domain);
        Log<ReflectionAssemblyTests>(domain);
        Log<IReflectionAssembly>(domain);
        Log<FactAttribute>(domain);
        Log<FactDiscoverer>(domain);
        Log<int>(domain);
        Log<DateOnly>(domain);
    }
}
