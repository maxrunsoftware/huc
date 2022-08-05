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

public sealed class XUnitTrait
{
    private static readonly object lockerStatic = new();
    private static readonly Dictionary<IReflectionMethod, IReadOnlyList<XUnitTrait>> cacheMethods = new();
    private static readonly Dictionary<IReflectionType, IReadOnlyList<XUnitTrait>> cacheTypes = new();

    public string Name { get; }
    public string Value { get; }

    public XUnitTrait(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public static IReadOnlyList<XUnitTrait> CleanTraits(IEnumerable<XUnitTrait> traits)
    {
        var d = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var trait in traits.OrEmpty())
        {
            var k = trait.Name.TrimOrNull();
            if (k == null) continue;
            var v = trait.Value.TrimOrNull();
            if (v == null) continue;
            if (!d.TryGetValue(k, out var set)) d.Add(k, set = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            set.Add(v);
        }


        var list = new List<XUnitTrait>();
        foreach (var (k, vs) in d)
        {
            foreach (var v in vs) list.Add(new XUnitTrait(k, v));
        }

        return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ThenBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
    }

    public static IReadOnlyList<XUnitTrait> Get(IReflectionMethod info)
    {
        lock (lockerStatic)
        {
            if (!cacheMethods.TryGetValue(info, out var list))
            {
                list = CleanTraits(GetRaw(info));
                cacheMethods.Add(info, list);
            }

            return list;
        }
    }

    public static IReadOnlyList<XUnitTrait> Get(IReflectionType info)
    {
        lock (lockerStatic)
        {
            if (!cacheTypes.TryGetValue(info, out var list))
            {
                list = CleanTraits(GetRaw(info));
                cacheTypes.Add(info, list);
            }

            return list;
        }
    }

    private static IEnumerable<XUnitTrait> GetRaw(IReflectionMethod info)
    {
        var current = info;
        while (current != null)
        {
            foreach (var attr in Parse(current.Info)) yield return attr;
            current = current.Base;
        }

        foreach (var item in Get(info.Parent)) yield return item;
    }

    private static IEnumerable<XUnitTrait> GetRaw(IReflectionType info)
    {
        var current = info;
        while (current != null)
        {
            foreach (var attr in Parse(current.Type)) yield return attr;
            current = current.Base;
        }
    }

    private static IEnumerable<XUnitTrait> Parse(MemberInfo info)
    {
        if (info == null) yield break;

        foreach (var attributeData in info.GetCustomAttributesData().WhereNotNull())
        {
            var an = attributeData.AttributeType.FullNameFormatted().TrimOrNull();
            if (an == null) continue;
            if (!an.EqualsIgnoreCase("Xunit.TraitAttribute")) continue;

            var cArgs = attributeData.ConstructorArguments;
            if (cArgs.Count != 2) continue;
            var cArg1 = cArgs[0];
            if (cArg1.ArgumentType != typeof(string)) continue;
            var cArg1V = cArg1.Value.ToStringGuessFormat().TrimOrNull();
            if (cArg1V == null) continue;

            var cArg2 = cArgs[1];
            if (cArg2.ArgumentType != typeof(string)) continue;
            var cArg2V = cArg2.Value.ToStringGuessFormat().TrimOrNull();
            if (cArg2V == null) continue;

            yield return new XUnitTrait(cArg1V, cArg2V);
        }
    }
}
