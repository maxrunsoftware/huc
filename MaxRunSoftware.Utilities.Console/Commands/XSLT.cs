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

namespace MaxRunSoftware.Utilities.Console.Commands
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
            xsltFile.CheckValueNotNull(nameof(xsltFile), log);
            var xsltContent = ReadFile(xsltFile);

            var xmlFile = GetArgValueTrimmed(1);
            xmlFile.CheckValueNotNull(nameof(xmlFile), log);
            var xmlFiles = ParseInputFiles(xmlFile.Yield());
            log.Debug(xmlFiles, nameof(xmlFiles));

            foreach (var xml in xmlFiles)
            {
                var xmlContent = ReadFile(xml);
                var data = XmlWriter.ApplyXslt(xsltContent, xmlContent);
                WriteFile(xml, data);
            }


        }
    }
}
