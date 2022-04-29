// /*
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
// */
using System.Collections.Generic;

namespace MaxRunSoftware.Utilities
{
    public class XmlElement
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public IDictionary<string, string> Attributes { get; } = new Dictionary<string, string>();
        public IList<XmlElement> Children { get; } = new List<XmlElement>();
        public XmlElement Parent { get; set; }

        public string this[string attributeName] { get => GetAttributeValue(attributeName); }

        public string GetAttributeValue(string attributeName)
        {
            if (Attributes.TryGetValue(attributeName, out var val)) return val;
            foreach (var sc in Constant.LIST_StringComparison)
            {
                foreach (var kvp in Attributes)
                {
                    if (string.Equals(attributeName, kvp.Key, sc))
                    {
                        return kvp.Value;
                    }
                }
            }
            return null;
        }

        public IEnumerable<XmlElement> GetChildrenNamed(string name, bool caseSensitive = false)
        {
            foreach (var c in Children)
            {
                if (caseSensitive)
                {
                    if (string.Equals(name, c.Name)) yield return c;
                }
                else
                {
                    if (string.Equals(name, c.Name, System.StringComparison.OrdinalIgnoreCase)) yield return c;
                }
            }
        }
    }
}
