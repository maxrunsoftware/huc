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

// ReSharper disable ConvertToAutoProperty
// ReSharper disable StringLiteralTypo
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public class ReflectionTypeTests : ReflectionTestBase
{
    public ReflectionTypeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    public class TestClass
    {
        public string myField;
        private string myFieldPrivate;

        public string MyFieldPrivate
        {
            get => myFieldPrivate;
            set => myFieldPrivate = value;
        }

        public readonly string myFieldReadonly;

        public string MyProperty { get; set; }
        private string MyPropertyPrivate { get; set; }

        public string MyPropertyPrivateValue
        {
            get => MyPropertyPrivate;
            set => MyPropertyPrivate = value;
        }

        public TestClass() { }

        public TestClass(string myFieldReadonly)
        {
            this.myFieldReadonly = myFieldReadonly;
        }
    }


    [TestFact]
    public void Types()
    {
        var o = new TestClass();
        var rt = T(o);
        Assert.NotNull(rt);
        var a = rt.Parent;
        Assert.NotNull(a);

        Assert.Contains(rt, a.Types);
    }

    [TestFact]
    public void Types_List()
    {
        var t = typeof(TestClass);
        var a = t.Assembly;

        var hs = new HashSet<Type>();
        //foreach (var type in a.DefinedTypes) hs.Add(type);
        //foreach (var type in a.ExportedTypes) hs.Add(type);
        foreach (var type in a.GetTypes()) hs.Add(type);
        //foreach (var type in a.GetExportedTypes()) hs.Add(type);
        //foreach (var type in a.GetForwardedTypes()) hs.Add(type);

        var items = hs.Select(type => (type.FullNameFormatted(), type)).OrderBy(o => o.Item1, StringComparer.OrdinalIgnoreCase).ThenBy(o => o.Item1, StringComparer.Ordinal).ToList();
        Info("Total Types: " + items.Count);
        foreach (var item in items)
        {
            var i = 0;
            var type = item.type;
            while (type != null)
            {
                if (type == typeof(object)) break;
                Info(new string(' ', i * 2) + type.FullNameFormatted());
                type = type.BaseType;
                i++;
            }

            Info("");
        }
    }

    [TestFact]
    public void Types_Has_Base_Classes()
    {
        var o = new TestClass();
        var rt = T(o);
        Assert.NotNull(rt);
        var a = rt.Parent;
        Assert.NotNull(a);

        var types = a.Types.ToList();
        var _ = types.Select(oo => oo.Bases).ToList(); // Initialize bases
        types = a.Types.ToList();

        //Info("Types:");
        //foreach (var t in types) Info($"  {TypeToString(t)}");

        var funcs = new List<Func<IReflectionType, string>>();
        funcs.Add(type => type.Parent.Name.TrimOrNull() ?? "");
        funcs.Add(type => type.Type.Namespace.TrimOrNull() ?? "");
        funcs.Add(type => nameof(IReflectionType) + "." + nameof(IReflectionType.Name) + ":" + type.Name);
        funcs.Add(type => nameof(Type) + "." + nameof(Type.Name) + ":" + type.Type.Name);
        funcs.Add(type => nameof(Type) + ".NameFormatted():" + type.Type.NameFormatted());
        funcs.Add(type => nameof(IReflectionType) + "." + nameof(IReflectionType.NameFull) + ":" + type.NameFull);
        funcs.Add(type => nameof(Type) + "." + nameof(Type.FullName) + ":" + type.Type.FullName);
        funcs.Add(type => nameof(Type) + ".FullNameFormatted():" + type.Type.FullNameFormatted());

        var funcsPaddings = new int[funcs.Count];
        for (var i = 0; i < funcsPaddings.Length; i++)
        {
            funcsPaddings[i] = types.Max(oo => funcs[i](oo).Length);
        }

        foreach (var type in types.OrderBy(oo => !oo.IsRealType)
                     .ThenBy(oo => oo.Parent)
                     .ThenBy(oo => oo.Type.Namespace.TrimOrNull() ?? "")
                     .ThenBy(oo => oo))
        {
            var sb = new StringBuilder();
            sb.Append(type.IsRealType ? "[Real]   " : "[NotReal]");
            for (var i = 0; i < funcs.Count; i++)
            {
                sb.Append("  " + funcs[i](type).PadRight(funcsPaddings[i]));
            }

            Info(sb.ToString());
        }


        // Can't use nameof because that could load the assembly accidentally
        var foundTypes = types.Where(oo => oo.Type.NameFormatted().Equals("SqlTestBase<SqlMsSql>")).ToList();
        Assert.Single(foundTypes);

        foundTypes = types.Where(oo => oo.Type.NameFormatted().Equals("SqlTestBase<SqlMySql>")).ToList();
        Assert.Single(foundTypes);

        foundTypes = types.Where(oo => oo.Type.NameFormatted().Equals("SqlTestBase<SqlOracle>")).ToList();
        Assert.Single(foundTypes);
    }

    [TestFact]
    public void Has_Parent_Types()
    {
        var type = typeof(ReflectionType);

        var baseTypes = type.BaseTypes();
        foreach (var typeBase in baseTypes) Info(typeBase);


        Info("");
        var t = Domain.GetType(type);
        var bases = t.Bases;
        for (var i = 0; i < bases.Count; i++) Info($"[{i}]  {bases[i]}");

        Assert.Equal(baseTypes.Length, bases.Count);
        for (var i = 0; i < bases.Count; i++)
        {
            var reflectionBaseType = bases[i];
            var baseType = baseTypes[i];
            Assert.Equal(baseType, reflectionBaseType.Type);
        }
    }
}
