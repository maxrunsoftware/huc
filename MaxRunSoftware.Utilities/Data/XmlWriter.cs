/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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

using System.Xml;
using System.Xml.Xsl;

namespace MaxRunSoftware.Utilities;

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
        settings.Encoding = Constant.ENCODING_UTF8;
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

    public IDisposable Element(string elementName, params (string attributeName, object attributeValue)[] attributes) => Element(elementName, null, attributes: attributes);

    public IDisposable Element(string elementName, string elementValue, params (string attributeName, object attributeValue)[] attributes)
    {
        writer.WriteStartElement(elementName);
        foreach (var attr in attributes)
        {
            Attribute(attr.attributeName, attr.attributeValue);
        }
        if (elementValue != null) writer.WriteValue(elementValue);
        return new ElementToken(this);
    }

    public void EndElement() => writer.WriteEndElement();

    public void Attribute(string attributeName, object attributeValue) => writer.WriteAttributeString(attributeName, attributeValue.ToStringGuessFormat());
    public void Value(string value) => writer.WriteString(value);

    public static string ApplyXslt(string xslt, string xml)
    {
        var xmlReader = new StringReader(xml);
        var xmlXmlReader = System.Xml.XmlReader.Create(xmlReader);

        var transformedContent = new StringBuilder();
        var xmlWriter = System.Xml.XmlWriter.Create(transformedContent);

        var xsltReader = new StringReader(xslt);
        var xsltXmlReader = System.Xml.XmlReader.Create(xsltReader);
        var myXslTrans = new XslCompiledTransform();

        myXslTrans.Load(xsltXmlReader);
        myXslTrans.Transform(xmlXmlReader, xmlWriter);
        xmlWriter.Flush();

        var data = transformedContent.ToString();
        return data;
    }
}
