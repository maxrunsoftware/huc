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

public sealed class XUnitReflectionContainer
{

    private XUnitReflectionContainer()
    {

    }


    private sealed class XType
    {
        public TypeSlim Type { get; }

        public XType(TypeSlim type)
        {
            Type = type;

        }
    }

    private sealed class XTrait
    {
        public string Name { get; }
        public string Value { get; }

        private XTrait()
        public static List<XTrait> GetTraits(MemberInfo info)
        {
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

                var t = new XTrait(cArg1V, cArg2V);
                yield return new XUnitTrait
            }
        }
    }


}
