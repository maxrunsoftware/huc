/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Text;
using System.Xml;

namespace HavokMultimedia.Utilities
{
    public class XmlWriter : IDisposable
    {
        private class ElementToken : IDisposable
        {
            private readonly XmlWriter writer;
            public ElementToken(XmlWriter writer)
            {
                this.writer = writer;
            }
            public void Dispose()
            {
                writer.EndElement();
            }
        }

        private StringBuilder stream;
        private System.Xml.XmlWriter writer;
        private string toString;
        private SingleUse isDisposed = new SingleUse();
        public XmlWriter(bool formatted = false)
        {
            stream = new StringBuilder();
            var settings = new XmlWriterSettings();
            settings.Encoding = Constant.ENCODING_UTF8_WITHOUT_BOM;
            settings.Indent = formatted;
            settings.NewLineOnAttributes = false;
            settings.OmitXmlDeclaration = true;
            writer = System.Xml.XmlWriter.Create(stream, settings);
        }

        public void Dispose()
        {
            if (!isDisposed.TryUse()) return;
            ToString();
            writer.Dispose();
        }

        public override string ToString()
        {
            if (!isDisposed.IsUsed)
            {
                writer.Flush();
                toString = stream.ToString();
            }
            return toString;
        }

        public IDisposable Element(string elementName, params (string attributeName, object attributeValue)[] attributes)
        {
            writer.WriteStartElement(elementName);
            foreach (var attr in attributes)
            {
                Attribute(attr.attributeName, attr.attributeValue);
            }
            return new ElementToken(this);
        }

        public void EndElement() => writer.WriteEndElement();

        public void Attribute(string attributeName, object attributeValue) => writer.WriteAttributeString(attributeName, attributeValue.ToStringGuessFormat());
        public void Value(string value) => writer.WriteString(value);



    }
}
