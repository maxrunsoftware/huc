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
using System.Collections.Generic;
using System.Text;
using EmbedIO;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class WebServerUtilityBase
    {
        protected enum Format { Html, Json, Xml }
        protected IHttpContext Context { get; private set; }
        protected string GetParameterString(string name) => Context.GetParameterString(name);
        protected string GetParameterString(string name, string defaultValue) => Context.GetParameterString(name, defaultValue);
        protected int? GetParameterInt(string name) => Context.GetParameterInt(name);
        protected int GetParameterInt(string name, int defaultValue) => Context.GetParameterInt(name, defaultValue);
        protected Format ResponseFormat { get; private set; }

        public object Handle(IHttpContext context)
        {
            Context = context;
            var responseformat = GetParameterString("format", "html").TrimOrNull();
            if (responseformat == null) responseformat = "html";
            if (responseformat.EqualsCaseInsensitive(nameof(Format.Json))) ResponseFormat = Format.Json;
            else if (responseformat.EqualsCaseInsensitive(nameof(Format.Xml))) ResponseFormat = Format.Xml;
            else ResponseFormat = Format.Html;

            if (ResponseFormat == Format.Json) return HandleJson();
            else if (ResponseFormat == Format.Xml) return HandleXml();
            else return HandleHtml();
        }

        public abstract string HandleHtml();
        public virtual string HandleJson() => throw new NotImplementedException("Format=JSON not implemented");
        public virtual string HandleXml() => throw new NotImplementedException("Format=XML not implemented");
        public virtual HttpVerbs Verbs => HttpVerbs.Get;
        protected readonly ILogger log;

        protected WebServerUtilityBase()
        {
            log = Program.LogFactory.GetLogger(GetType());
        }

        public void WriteResponseFile(byte[] bytes, string fileName)
        {
            Context.AddHeader("Content-Disposition", "attachment", "filename=\"" + fileName + "\"");

            using (var stream = Context.OpenResponseStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }
        public void WriteResponseFile(string data, string fileName, Encoding encoding = null)
        {
            if (encoding == null) encoding = Constant.ENCODING_UTF8_WITHOUT_BOM;
            var bytes = encoding.GetBytes(data);
            WriteResponseFile(bytes, fileName);
        }

        public string ToJson(object o) => Swan.Formatters.Json.Serialize(o, format: true);
    }
}
