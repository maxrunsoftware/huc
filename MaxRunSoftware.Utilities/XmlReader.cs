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
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MaxRunSoftware.Utilities
{
    public class XmlReader
    {
        public static XmlElement Read(string xml)
        {
            var document = new XmlDocument();
            document.LoadXml(xml);
            return Process(document);
        }

        private static XmlElement Process(XmlDocument document)
        {
            var elementRoot = document.DocumentElement;
            return ProcessElement(elementRoot, null);
        }

        private static XmlElement ProcessElement(System.Xml.XmlElement element, XmlElement parent)
        {
            var newElement = new XmlElement();
            newElement.Name = element.Name;
            newElement.Parent = parent;
            var attrs = element.Attributes;
            if (attrs != null)
            {
                foreach (XmlAttribute attr in attrs)
                {
                    var name = (attr.Name ?? attr.LocalName).TrimOrNull();
                    if (name == null) continue;

                    var value = attr.Value;
                    if (value == null) continue;

                    newElement.Attributes[name] = value;
                }
            }

            var cn = element.ChildNodes;
            if (cn != null)
            {
                var values = new List<string>();
                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child.NodeType.In(XmlNodeType.Element))
                    {
                        var childElement = (System.Xml.XmlElement)child;
                        var newChild = ProcessElement(childElement, newElement);
                        newElement.Children.Add(newChild);
                    }
                    else if (child.NodeType.In(XmlNodeType.Text))
                    {
                        var v = child.Value;
                        if (v != null) values.Add(v);
                    }
                }
                if (values.IsNotEmpty()) newElement.Value = values.ToStringDelimited("");
            }

            return newElement;
        }


    }
}
