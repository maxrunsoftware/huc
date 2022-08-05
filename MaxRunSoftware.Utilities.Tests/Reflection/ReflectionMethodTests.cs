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

// ReSharper disable InconsistentNaming

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public class ReflectionMethodTests : ReflectionTestBase
{
    public ReflectionMethodTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }


    public class TestClassA
    {
        public bool M1A_Called { get; private set; }
        public void M1() => M1A_Called = true;

        public bool M2A_Called { get; private set; }
        public virtual void M2() => M2A_Called = true;

        public bool M3A_Called { get; private set; }
        public virtual void M3() => M3A_Called = true;
    }

    public abstract class TestClassB : TestClassA
    {
        public bool M3B_Called { get; private set; }
        public override void M3() => M3B_Called = true;
    }

    public class TestClassC : TestClassB
    {
        public bool M2C_Called { get; private set; }
        public override void M2() => M2C_Called = true;

        public bool M3C_Called { get; private set; }
        public new virtual void M3() => M3C_Called = true;
    }

    public class TestClassD : TestClassC { }

    public class TestClassE : TestClassD
    {
        public bool M3E_Called { get; private set; }

        public override void M3() => M3E_Called = true;
    }

    [TestFact]
    public void BaseMethod_M1()
    {
        var o = new TestClassD();
        var ms = M(o, "M1");
        Assert.Single(ms);
        var md = ms[0];
        Info(md);
        Assert.Equal(MethodDeclarationType.None, md.Info.GetDeclarationType());

        //var c = md.Parent.BaseType;
        //for (var i = 0; i < c.Methods.Count; i++) Info($"c  {c.Name}.{nameof(ReflectionType.Methods)}[{i}]: " + c.Methods[i]);
        var mc = md.Base;
        Info(mc);
        Assert.NotNull(mc);
        Assert.True(mc.Parent.Equals(md.Parent.Base));
        Assert.Equal(MethodDeclarationType.None, mc.Info.GetDeclarationType());

        var mb = mc.Base;
        Info(mb);
        Assert.NotNull(mb);
        Assert.True(mb.Parent.Equals(mc.Parent.Base));
        Assert.Equal(MethodDeclarationType.None, mb.Info.GetDeclarationType());

        var ma = mb.Base;
        Info(ma);
        Assert.NotNull(ma);
        Assert.True(ma.Parent.Equals(mb.Parent.Base));
        Assert.Equal(MethodDeclarationType.None, ma.Info.GetDeclarationType());

        var mNull = ma.Base;
        Assert.Null(mNull);
    }

    [TestFact]
    public void BaseMethod_M2()
    {
        var o = new TestClassD();
        var ms = M(o, "M2");
        Assert.Single(ms);
        var md = ms[0];
        Info(md);
        Assert.Equal(MethodDeclarationType.None, md.Info.GetDeclarationType());

        //var c = md.Parent.BaseType;
        //for (var i = 0; i < c.Methods.Count; i++) Info($"c  {c.Name}.{nameof(ReflectionType.Methods)}[{i}]: " + c.Methods[i]);
        var mc = md.Base;
        Info(mc);
        Assert.NotNull(mc);
        Assert.True(mc.Parent.Equals(md.Parent.Base));
        Assert.Equal(MethodDeclarationType.Override, mc.Info.GetDeclarationType());

        var mb = mc.Base;
        Info(mb);
        Assert.NotNull(mb);
        Assert.True(mb.Parent.Equals(mc.Parent.Base));
        Assert.Equal(MethodDeclarationType.None, mb.Info.GetDeclarationType());

        var ma = mb.Base;
        Info(ma);
        Assert.NotNull(ma);
        Assert.True(ma.Parent.Equals(mb.Parent.Base));
        Assert.Equal(MethodDeclarationType.Virtual, ma.Info.GetDeclarationType());

        var mNull = ma.Base;
        Assert.Null(mNull);
    }

    [TestFact]
    public void BaseMethod_M3()
    {
        var o = new TestClassE();
        var t = T(o);
        Info(t.Methods);
        foreach (var methodInfo in t.Type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) Info("Reflected: " + methodInfo.DeclaringType.FullNameFormatted() + " " + methodInfo.Name);

        var ms = M(o, "M3");
        Info(ms);
        Assert.Equal(2, ms.Count);
        var md1 = ms[0];
        var md2 = ms[1];

        for (var i = 0; i < ms.Count; i++)
        {
            var oo = new TestClassE();
            var m = ms[i];
            m.Info.Invoke(o, Array.Empty<object>());
            Info(m);
            Info($"  {nameof(Type.DeclaringType)}: {m.Info.DeclaringType?.Name}  {nameof(Type.ReflectedType)}: {m.Info.ReflectedType?.Name}");
            Info($"  M3A:{o.M3A_Called.ToString(),5}  M3B:{o.M3B_Called.ToString(),5}  M3C:{o.M3C_Called.ToString(),5}  M3D:{o.M3E_Called.ToString(),5}");
            Info();
        }
    }

    [TestFact]
    public void List_Call_Details_E()
    {
        var types = new[] { typeof(TestClassA), typeof(TestClassB), typeof(TestClassC), typeof(TestClassD), typeof(TestClassE) };
        foreach (var type in types)
        {
            var ms = T(type).Methods.Named("M3");
            for (var i = 0; i < ms.Count; i++)
            {
                var o = new TestClassE();
                var m = ms[i];
                m.Info.Invoke(o, Array.Empty<object>());
                Info($"[{type.Name.Right(1)}:{i}] {m}");
                Info($"  {m.Info.Attributes}");
                Info($"  {nameof(Type.DeclaringType).Left(9)}: {m.DeclaringType?.Name.Right(1)}   {nameof(Type.ReflectedType).Left(9)}: {m.ReflectedType?.Name.Right(1)}");
                Info($"  M3A:{o.M3A_Called.ToString(),-5}  M3B:{o.M3B_Called.ToString(),-5}  M3C:{o.M3C_Called.ToString(),-5}  M3D:{false.ToString(),-5}  M3E:{o.M3E_Called.ToString(),-5}");

                Info();
            }

            Info();
        }
    }

    [TestFact]
    public void List_Call_Details_D()
    {
        var types = new[] { typeof(TestClassA), typeof(TestClassB), typeof(TestClassC), typeof(TestClassD) };
        foreach (var type in types)
        {
            var ms = T(type).Methods.Named("M3");
            for (var i = 0; i < ms.Count; i++)
            {
                var o = new TestClassD();
                var m = ms[i];
                m.Info.Invoke(o, Array.Empty<object>());
                Info($"[{type.Name.Right(1)}:{i}] {m}");
                Info($"  {m.Info.Attributes}");
                Info($"  {nameof(Type.DeclaringType).Left(9)}: {m.DeclaringType?.Name.Right(1)}   {nameof(Type.ReflectedType).Left(9)}: {m.ReflectedType?.Name.Right(1)}");
                Info($"  M3A:{o.M3A_Called.ToString(),-5}  M3B:{o.M3B_Called.ToString(),-5}  M3C:{o.M3C_Called.ToString(),-5}  M3D:{false.ToString(),-5}");

                Info();
            }

            Info();
        }
    }

    [TestFact]
    public void List_Call_Details_C()
    {
        var types = new[] { typeof(TestClassA), typeof(TestClassB), typeof(TestClassC) };
        foreach (var type in types)
        {
            var ms = T(type).Methods.Named("M3");
            for (var i = 0; i < ms.Count; i++)
            {
                var o = new TestClassC();
                var m = ms[i];
                m.Info.Invoke(o, Array.Empty<object>());
                Info($"[{type.Name.Right(1)}:{i}] {m}");
                Info($"  {m.Info.Attributes}");
                Info($"  {nameof(Type.DeclaringType).Left(9)}: {m.DeclaringType?.Name.Right(1)}   {nameof(Type.ReflectedType).Left(9)}: {m.ReflectedType?.Name.Right(1)}");
                Info($"  M3A:{o.M3A_Called.ToString(),-5}  M3B:{o.M3B_Called.ToString(),-5}  M3C:{o.M3C_Called.ToString(),-5}");

                Info();
            }

            Info();
        }
    }

    #region DeclaringType

    public class DeclaringType_A
    {
        public void M1() { }
        public virtual void M2() { }
        public virtual void M3() { }
        public virtual void M4() { }
    }

    public class DeclaringType_B : DeclaringType_A
    {
        public override void M2() { }
        public new virtual void M3() { }
        public sealed override void M4() { }
    }

    public class DeclaringType_C : DeclaringType_B
    {
        public new void M2() { }
        public override void M3() { }
        public new void M4() { }
    }

    private (IReflectionType t, Dictionary<string, IReadOnlyList<IReflectionMethod>> ms) GetMethods<T>(params string[] methodNames)
    {
        var t = T<T>();
        var d = new Dictionary<string, IReadOnlyList<IReflectionMethod>>();
        foreach (var methodName in methodNames) d.Add(methodName, t.Methods.Named(methodName));

        return (t, d);
    }

    [TestFact]
    public void DeclaringType()
    {
        (IReflectionType t, Dictionary<string, IReadOnlyList<IReflectionMethod>> ms) GetData<T>()
        {
            var t = T<T>();
            var d = new Dictionary<string, IReadOnlyList<IReflectionMethod>>();
            d.Add("M1", t.Methods.Named("M1"));
            d.Add("M2", t.Methods.Named("M2"));
            d.Add("M3", t.Methods.Named("M3"));
            d.Add("M4", t.Methods.Named("M4"));
            return (t, d);
        }

        var d = GetData<DeclaringType_A>();
        Assert.Single(d.ms["M1"]);
        Assert.Equal(T<DeclaringType_A>(), d.ms["M1"][0].DeclaringType);
        Assert.Single(d.ms["M2"]);
        Assert.Equal(T<DeclaringType_A>(), d.ms["M2"][0].DeclaringType);
        Assert.Single(d.ms["M3"]);
        Assert.Equal(T<DeclaringType_A>(), d.ms["M3"][0].DeclaringType);
        Assert.Single(d.ms["M4"]);
        Assert.Equal(T<DeclaringType_A>(), d.ms["M4"][0].DeclaringType);


        d = GetData<DeclaringType_B>();
        Assert.Single(d.ms["M1"]);
        Assert.Equal(T<DeclaringType_A>(), d.ms["M1"][0].DeclaringType);
        Assert.Single(d.ms["M2"]);
        Assert.Equal(T<DeclaringType_B>(), d.ms["M2"][0].DeclaringType);
        Assert.Equal(2, d.ms["M3"].Count); // original (A) and shadowed (B)
        Assert.Single(d.ms["M3"].Where(o => o.DeclaringType.Equals(T<DeclaringType_A>())));
        Assert.Single(d.ms["M3"].Where(o => o.DeclaringType.Equals(T<DeclaringType_B>())));
        Assert.Single(d.ms["M4"]);
        Assert.Equal(T<DeclaringType_B>(), d.ms["M4"][0].DeclaringType);


        d = GetData<DeclaringType_C>();
        Assert.Single(d.ms["M1"]);
        Assert.Equal(T<DeclaringType_A>(), d.ms["M1"][0].DeclaringType);
        Assert.Equal(2, d.ms["M2"].Count); // override (B) and shadowed (C)
        Assert.Single(d.ms["M2"].Where(o => o.DeclaringType.Equals(T<DeclaringType_B>())));
        Assert.Single(d.ms["M2"].Where(o => o.DeclaringType.Equals(T<DeclaringType_C>())));
        Assert.Equal(2, d.ms["M3"].Count); // original (A) and shadowed (B) overriden by (C)
        Assert.Single(d.ms["M3"].Where(o => o.DeclaringType.Equals(T<DeclaringType_A>())));
        Assert.Single(d.ms["M3"].Where(o => o.DeclaringType.Equals(T<DeclaringType_C>())));
        Assert.Equal(2, d.ms["M4"].Count); // override (B) and shadowed (C)
        Assert.Single(d.ms["M4"].Where(o => o.DeclaringType.Equals(T<DeclaringType_B>())));
        Assert.Single(d.ms["M4"].Where(o => o.DeclaringType.Equals(T<DeclaringType_C>())));
    }

    #endregion DeclaringType

    public class IsVirtual_A
    {
        public void M1() { }
        public virtual void M2() { }
        public virtual void M3() { }
        public virtual void M4() { }
    }

    public class IsVirtual_B : IsVirtual_A
    {
        public override void M2() { }
        public new virtual void M3() { }
        public sealed override void M4() { }
    }

    public class IsVirtual_C : IsVirtual_B
    {
        public new void M2() { }
        public override void M3() { }
        public new void M4() { }
    }

    [TestFact]
    public void IsVirtual_M1()
    {
        const string METHOD = "M1";

        var tms = M<IsVirtual_A>(METHOD);
        Assert.Single(tms);
        Assert.False(tms[0].IsVirtual());

        tms = M<IsVirtual_B>(METHOD);
        Assert.Single(tms);
        Assert.False(tms[0].IsVirtual());

        tms = M<IsVirtual_C>(METHOD);
        Assert.Single(tms);
        Assert.False(tms[0].IsVirtual());
    }

    [TestFact]
    public void IsVirtual_M2()
    {
        const string METHOD = "M2";

        var tms = M<IsVirtual_A>(METHOD);
        Assert.Single(tms);
        Assert.True(tms[0].IsVirtual());

        tms = M<IsVirtual_B>(METHOD);
        Assert.Single(tms);
        Assert.True(tms[0].IsVirtual());

        var t = T<IsVirtual_C>();
        tms = t.Methods.Named(METHOD);
        Assert.Equal(2, tms.Count);
        foreach (var tm in tms)
        {
            if (tm.DeclaringType.Equals(t) && tm.ReflectedType.Equals(t)) Assert.False(tm.IsVirtual()); // New
            else Assert.True(tm.IsVirtual()); // Original
        }
    }

    [TestFact]
    public void IsVirtual_M3()
    {
        const string METHOD = "M3";

        var tms = M<IsVirtual_A>(METHOD);
        Assert.Single(tms);
        Assert.True(tms[0].IsVirtual());

        tms = M<IsVirtual_B>(METHOD);
        Assert.Equal(2, tms.Count);
        var tmb = tms.First(o => o.DeclaringType.Equals(T<IsVirtual_B>()));
        Assert.True(tmb.IsVirtual());
        var tma = tms.First(o => o.DeclaringType.Equals(T<IsVirtual_A>()));
        Assert.True(tma.IsVirtual());

        var t = T<IsVirtual_C>();
        tms = t.Methods.Named(METHOD);
        Assert.Equal(2, tms.Count);
        foreach (var tm in tms) Info(tm.DeclaringType + "    " + tm.ReflectedType);
        var typeC = T<IsVirtual_C>();
        Assert.NotNull(typeC);
        tmb = tms.First(o => o.DeclaringType.Equals(typeC) && o.ReflectedType.Equals(typeC));
        Assert.True(tmb.IsVirtual());

        var typeA = T<IsVirtual_A>();
        Assert.NotNull(typeC);
        tma = tms.First(o => o.DeclaringType.Equals(typeA) && o.ReflectedType.Equals(typeC));
        Assert.True(tma.IsVirtual());
    }

    [TestFact]
    public void IsVirtual_M4()
    {
        const string METHOD = "M4";

        var tms = M<IsVirtual_A>(METHOD);
        Assert.Single(tms);
        Assert.True(tms[0].IsVirtual());

        tms = M<IsVirtual_B>(METHOD);
        Assert.Single(tms);
        Assert.True(tms[0].IsVirtual()); // Sealed override should still be virtual

        tms = M<IsVirtual_C>(METHOD);
        Assert.Equal(2, tms.Count);
        var tmb = tms.First(o => o.DeclaringType.Equals(T<IsVirtual_B>()));
        Assert.True(tmb.IsVirtual()); // Sealed override should still be virtual
        var tmc = tms.First(o => o.DeclaringType.Equals(T<IsVirtual_C>()));
        Assert.False(tmc.IsVirtual()); // new doesn't declare virtual
    }


    public class Method_Invoke_A
    {
        public string IM1_Invoked { get; private set; }
        public void IM1() => IM1_Invoked = "invoked";

        public string IM2_Invoked { get; private set; }
        public void IM2(string s) => IM2_Invoked = s;

        public string IM3_Invoked { get; private set; }

        public string IM3()
        {
            IM3_Invoked = "foo";
            return "foo";
        }
    }

    [TestFact]
    public void Method_Invoke_0_0()
    {
        var a = new Method_Invoke_A();
        var t = T(a);
        Assert.Null(a.IM1_Invoked);
        var m = t.Methods.Named("IM1").First();
        var r = m.Invoke(a);
        Assert.Null(r);
        Assert.NotNull(a.IM1_Invoked);
    }

    [TestFact]
    public void Method_Invoke_0_1()
    {
        var a = new Method_Invoke_A();
        var t = T(a);
        Assert.Null(a.IM2_Invoked);
        var m = t.Methods.Named("IM2").First();
        var r = m.Invoke(a, "foo");
        Assert.Null(r);
        Assert.Equal("foo", a.IM2_Invoked);
    }

    [TestFact]
    public void Method_Invoke_1_0()
    {
        var a = new Method_Invoke_A();
        var t = T(a);
        Assert.Null(a.IM2_Invoked);
        var m = t.Methods.Named("IM3").First();
        var r = m.Invoke(a);
        Assert.NotNull(r);
        Assert.Equal("foo", r);
        Assert.Equal("foo", a.IM3_Invoked);
    }

    public class CollectionSearch
    {
        public string PS1 { get; set; }
        private string PS2 { get; set; }
        public static string PS3 { private get; set; }

        public string MS1() => null;
        public static string MS2() => null;
        public static string MS3(int i) => null;
        private string MS4(int i) => null;

        public T1 MT1<T1>() => default;
        public void MT2<T1, T2>() { }
        public T2 MT3<T1, T2, T3>(T3 three) => default;
        public void MT4<T1, T2, T3>(T2 two) { }
    }


    [TestFact]
    public void Collection_ReturnType()
    {
        var t = T<CollectionSearch>();

        var ms = t.Methods;
        Info(ms.ToString());

        ms = ms.NotMethodsFrom<object>();
        Info(ms.ToString());

        ms = ms.NotProperties();
        Info(ms.ToString());

        Assert.Equal(4, ms.ReturnType<string>().Count);

        Assert.Equal(2, ms.ReturnType<string>().Parameters<int>().Count);

        Assert.Equal(2, ms.ReturnType(typeof(void)).Count);
        Assert.Equal(2, ms.ReturnType(typeof(void), false).Count);
    }

    [TestFact]
    public void Collection_GenericTypeArguments()
    {
        var t = T<CollectionSearch>();

        var ms = t.Methods;
        Info(ms.ToString());

        ms = ms.NotMethodsFrom<object>();
        Info(ms.ToString());

        ms = ms.NotProperties();
        Info(ms.ToString());

        //Assert.True(ms.GenericTypeArguments(0).Count > 3);
        Assert.DoesNotContain(ms.GenericParameters(0), o => o.GenericParameters.Count > 0);

        //Assert.Equal(2, ms.GenericTypeArguments(3).Count);
        Assert.DoesNotContain(ms.GenericParameters(3), o => o.GenericParameters.Count != 3);
    }


    private class IsPropertyMethod_A
    {
        public string Prop_Get_Init { get; }
        public string Prop_PGet_Init { private get; init; }
        public string Prop_Get_Set { get; set; }
        public string Prop_PGet_Set { private get; set; }
        public string Prop_Get_PSet { get; private set; }
    }

    [TestFact]
    public void IsPropertyMethod()
    {
        var ms = M<IsPropertyMethod_A>().Where(o => o.Name.Contains(nameof(IsPropertyMethod_A.Prop_Get_Set)));
        Assert.Equal(2, ms.Count);
        Assert.True(ms[0].IsPropertyMethod());
        Assert.True(ms[1].IsPropertyMethod());

        var m = ms.First(o => o.Name.StartsWith("get_"));
        Assert.True(m.IsPropertyMethod());
        Assert.True(m.IsPropertyMethodGet());
        Assert.False(m.IsPropertyMethodSet());
    }
}
