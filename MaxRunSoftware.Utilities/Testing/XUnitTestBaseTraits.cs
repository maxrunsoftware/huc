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

public abstract partial class XUnitTestBase
{
    private readonly Lzy<XUnitTraitCollection> testTraits;

    protected XUnitTraitCollection TestTraits => testTraits.Value;


    private static readonly Dictionary<TypeSlim, List<MethodInfo>> factAttributeMethods = new();

    private static List<MethodInfo> GetFactAttributeMethods(TypeSlim factAttributeType)
    {
        lock (lockerStatic)
        {
            if (factAttributeMethods.TryGetValue(factAttributeType, out var list)) return list;

            //staticMessagesLog.Add("Looking for methods with attribute: " + factAttributeType);
            var methods = new List<MethodInfo>();
            factAttributeMethods.Add(factAttributeType, methods);

            var assembly = factAttributeType.Parent;
            foreach (var reflectionType in assembly.Types)
            {
                foreach (var reflectionMethod in reflectionType.Methods)
                {
                    if (reflectionMethod.Attributes.Any(o => o.Type.Equals(factAttributeType))) methods.Add(reflectionMethod);
                    //staticMessagesLog.Add($"Found {factAttributeType} on " + reflectionMethod);
                }
            }

            return methods;
        }
    }

    private static readonly Dictionary<TypeSlim, Dictionary<CallerInfo, MethodInfo>> callerMethods = new();

    public static XUnitTraitCollection GetAttributeTraits<TAttribute>(TAttribute factAttribute, Func<TAttribute, CallerInfo> getCaller) where TAttribute : Attribute
    {
        var factAttributeTypeReflection = Reflection.GetType(factAttribute.GetType());
        lock (lockerStatic)
        {
            if (!callerMethods.TryGetValue(factAttributeTypeReflection, out var d)) callerMethods.Add(factAttributeTypeReflection, d = new Dictionary<CallerInfo, MethodInfo>());
            var caller = getCaller(factAttribute);
            if (!d.TryGetValue(caller, out var method))
            {
                var methods = GetFactAttributeMethods(factAttributeTypeReflection);
                foreach (var m in methods)
                {
                    var attrs = m.Attributes.Where(o => o.Type.Equals(factAttributeTypeReflection)).Select(o => o.Attribute);
                    foreach (var attr in attrs)
                    {
                        if (attr is not TAttribute attrT) continue;
                        var attrCaller = getCaller(attrT);
                        if (attrCaller == null) continue;
                        if (caller.Equals(attrCaller)) method = m;

                        if (method != null) break;
                    }

                    if (method != null) break;
                }

                if (method == null)
                {
                    //staticMessagesLog.Add("Could not find method  " + caller);
                }

                d[caller] = method;
            }

            return XUnitTraitCollection.Get(method);
        }
    }
}
