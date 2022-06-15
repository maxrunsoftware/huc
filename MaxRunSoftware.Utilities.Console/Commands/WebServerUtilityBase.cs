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

using System;
using System.Collections.Generic;
using System.IO;
using EmbedIO;
using HttpMultipartParser;
using MaxRunSoftware.Utilities.External;
using Swan.Formatters;

namespace MaxRunSoftware.Utilities.Console.Commands;

public abstract class WebServerUtilityBase
{
    protected enum Format
    {
        Html,
        Json,
        Xml
    }

    protected IHttpContext Context { get; private set; }

    protected string GetParameterString(string name) => Context.GetParameterString(name);

    protected string GetParameterString(string name, string defaultValue) => Context.GetParameterString(name, defaultValue);

    protected int? GetParameterInt(string name) => Context.GetParameterInt(name);

    protected int GetParameterInt(string name, int defaultValue) => Context.GetParameterInt(name, defaultValue);

    protected Format ResponseFormat { get; private set; }

    private MultipartFormDataParser FormParser
    {
        get
        {
            var context = Context;
            if (context == null) return null;

            var request = context.Request;
            //if (request == null) return null;
            var inputStream = request.InputStream;
            //if (inputStream == null) return null;

            try
            {
                if (!inputStream.CanRead) return null;

                if (inputStream.Length < 1) return null;

                var parser = MultipartFormDataParser.Parse(inputStream);
                return parser;
            }
            catch (Exception e)
            {
                log.Warn("Error creating MultipartFormDataParser", e);
                return null;
            }
        }
    }

    protected IReadOnlyDictionary<string, byte[]> Files
    {
        get
        {
            var d = new Dictionary<string, byte[]>();
            var parser = FormParser;
            if (parser == null) return d;

            foreach (var file in parser.Files)
            {
                using (var ms = new MemoryStream())
                {
                    file.Data.CopyTo(ms);
                    d[file.FileName] = ms.ToArray();
                }
            }

            return d;
        }
    }

    protected IReadOnlyDictionary<string, string> FormValues
    {
        get
        {
            var d = new Dictionary<string, string>();
            var parser = FormParser;
            if (parser == null) return d;

            foreach (var p in parser.Parameters) d[p.Name] = p.Data;

            return d;
        }
    }

    public object Handle(IHttpContext context)
    {
        Context = context;
        var responseFormat = GetParameterString("format", "html").TrimOrNull() ?? "html";
        if (responseFormat.EqualsCaseInsensitive(nameof(Format.Json))) { ResponseFormat = Format.Json; }
        else if (responseFormat.EqualsCaseInsensitive(nameof(Format.Xml))) { ResponseFormat = Format.Xml; }
        else { ResponseFormat = Format.Html; }

        if (ResponseFormat == Format.Json) return HandleJson();

        if (ResponseFormat == Format.Xml) return HandleXml();

        return HandleHtml();
    }

    public abstract string HandleHtml();

    public virtual string HandleJson() => throw new NotImplementedException("Format=JSON not implemented");

    public virtual string HandleXml() => throw new NotImplementedException("Format=XML not implemented");

    public virtual HttpVerbs Verbs => HttpVerbs.Get;
    protected readonly ILogger log;

    protected WebServerUtilityBase() { log = Program.LogFactory.GetLogger(GetType()); }


    public string ToJson(object o) => Json.Serialize(o, true);
}
