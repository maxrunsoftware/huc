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

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class XSLT : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Applies an XSLT transform to an XML file");
            help.AddExample("mytransform.xslt myxml.xml");
            help.AddValue("<XSLT file> <XML file>");
        }

        protected override void ExecuteInternal()
        {
            var xsltFile = GetArgValueTrimmed(0);
            log.Debug($"{nameof(xsltFile)}: {xsltFile}");
            var xsltContent = ReadFile(xsltFile);
            var xsltReader = new StringReader(xsltContent);
            var xsltXmlReader = XmlReader.Create(xsltReader);

            var xmlFile = GetArgValueTrimmed(1);
            log.Debug($"{nameof(xmlFile)}: {xmlFile}");
            var xmlFiles = ParseInputFiles(xmlFile.Yield());
            log.Debug(xmlFiles, nameof(xmlFiles));

            foreach (var xml in xmlFiles)
            {
                var xmlContent = ReadFile(xml);
                var xmlReader = new StringReader(xmlContent);
                var xmlXmlReader = XmlReader.Create(xmlReader);

                var transformedContent = new StringBuilder();
                var xmlWriter = System.Xml.XmlWriter.Create(transformedContent);

                var myXslTrans = new XslCompiledTransform();
                myXslTrans.Load(xsltXmlReader);
                myXslTrans.Transform(xmlXmlReader, xmlWriter);
                xmlWriter.Flush();

                var data = transformedContent.ToString();
                WriteFile(xml, data);
            }


        }
    }
}
