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

namespace MaxRunSoftware.Utilities.Tests.Extensions;

public class ExtensionsReflectionTests : TestBase
{
    public ExtensionsReflectionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    public interface IText
    {
        string? Text { get; }
    }

    public class MyComp : IText, IComparable
    {
        public string? Text { get; set; }
        public int CompareTo(object? o) => o is MyComp oo ? StringComparer.Ordinal.Compare(Text, oo.Text) : 1;
        public int CompareTo(DateTime dateTime) => DateTime.Now.CompareTo(dateTime);
    }

    public class MyCompGeneric : IText, IComparable<MyCompGeneric>
    {
        public string? Text { get; set; }
        public int CompareTo(MyCompGeneric? o) => o is MyCompGeneric oo ? StringComparer.Ordinal.Compare(Text, oo.Text) : 1;
        public int CompareTo(DateTime dateTime) => DateTime.Now.CompareTo(dateTime);
    }

    public interface ITyped
    {
        TAttribute GetAttribute<TAttribute, TClass>(TClass item);
    }

    public class MyCompGenericTyped<TItem> : IText, IComparable<MyCompGeneric>, ITyped
    {
        public string? Text { get; set; }
        public int CompareTo(MyCompGeneric? o) => o is MyCompGeneric oo ? StringComparer.Ordinal.Compare(Text, oo.Text) : 1;
        public int CompareTo(DateTime dateTime) => DateTime.Now.CompareTo(dateTime);
        public TAttribute GetAttribute<TAttribute, TClass>(TClass item) => throw new NotImplementedException();
        public TItem GetItem() => throw new NotImplementedException();
    }

    public class MyCompMultiple : IComparable<int>, IComparable<string>, IComparable<object>, IComparable
    {
        public int CompareTo(int other) => throw new NotImplementedException();
        public int CompareTo(string? other) => throw new NotImplementedException();
        int IComparable<object>.CompareTo(object? other) => throw new NotImplementedException();
        int IComparable.CompareTo(object? obj) => throw new NotImplementedException();
    }


    [Fact]
    public void Find_Impl_Method_From_Interface()
    {
        Info();
        Info();
        var x = new MyCompGeneric { Text = "X" };
        var y = new MyCompGeneric { Text = "Y" };

        var z = new MyCompMultiple();
        var typeObject = z.GetType();

        foreach (var interfaceImplemented in typeObject.GetInterfaces())
        {
            var map = typeObject.GetInterfaceMap(interfaceImplemented);

            Info($"Mapping of {map.InterfaceType.NameFormatted()} to {map.TargetType.NameFormatted()}");
            for (var ctr = 0; ctr < map.InterfaceMethods.Length; ctr++)
            {
                var im = map.InterfaceMethods[ctr];
                var tm = map.TargetMethods[ctr];
                //tm.GetGenericSignature()
                Info($"   {im.GetSignature(false)} --> {tm.GetSignature(false)}");
            }

            Info();
        }
    }
}
