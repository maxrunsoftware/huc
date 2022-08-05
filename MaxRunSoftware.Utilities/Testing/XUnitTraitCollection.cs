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

public sealed class XUnitTraitCollection : ReadOnlyListBase<XUnitTrait>
{
    private static readonly object lockerStatic = new();
    private static readonly Dictionary<IReflectionMethod, XUnitTraitCollection> cacheMethods = new();
    private static readonly Dictionary<IReflectionType, XUnitTraitCollection> cacheTypes = new();

    public IReflectionType Type { get; }
    public IReflectionMethod Method { get; }


    public static XUnitTraitCollection Get(IReflectionMethod info)
    {
        //return new XUnitTraitCollection();
        lock (lockerStatic)
        {
            if (!cacheMethods.TryGetValue(info, out var list))
            {
                list = new XUnitTraitCollection(XUnitTrait.Get(info), info);
                cacheMethods.Add(info, list);
            }

            return list;
        }
    }

    public static XUnitTraitCollection Get(IReflectionType info)
    {
        //return new XUnitTraitCollection();
        lock (lockerStatic)
        {
            if (!cacheTypes.TryGetValue(info, out var list))
            {
                list = new XUnitTraitCollection(XUnitTrait.Get(info), info);
                cacheTypes.Add(info, list);
            }

            return list;
        }
    }


    public XUnitTraitCollection(IEnumerable<XUnitTrait> traitEnumerable, IReflectionMethod method) : base(XUnitTrait.CleanTraits(traitEnumerable))
    {
        Method = method;
        Type = method.Parent;
    }

    public XUnitTraitCollection(IEnumerable<XUnitTrait> traitEnumerable, IReflectionType type) : base(XUnitTrait.CleanTraits(traitEnumerable))
    {
        Method = null;
        Type = type;
    }


    public bool Has(params Tuple<string, string>[] traits) => traits.OrEmpty().Any(o => Has(o.Item1, o.Item2));
    public bool Has(params (string name, string value)[] traits) => traits.OrEmpty().Any(o => Has(o.name, o.value));
    public bool Has(IEnumerable<Tuple<string, string>> traits) => traits.OrEmpty().Any(o => Has(o.Item1, o.Item2));
    public bool Has(IEnumerable<(string name, string value)> traits) => traits.OrEmpty().Any(o => Has(o.name, o.value));

    public bool Has(string name, string value)
    {
        name = name.TrimOrNull();
        if (name == null) return false;

        value = value.TrimOrNull();
        if (value == null) return false;

        foreach (var trait in this)
        {
            if (name.EqualsIgnoreCase(trait.Name) && value.EqualsIgnoreCase(trait.Value)) return true;
        }

        //return TestTraits.TryGetValue(name, out var set) && set.Contains(value);
        return false;
    }

    public override string ToString() => (Method?.ToString() ?? Type.ToString()) + " [" + this.Select(o => o.Name + "=" + o.Value).ToStringDelimited(", ") + "]";
}
