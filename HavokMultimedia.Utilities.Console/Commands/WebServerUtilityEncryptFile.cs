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

using System.Linq;
using EmbedIO;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServerUtilityEncryptFile : WebServerUtilityBase
    {
        public override HttpVerbs Verbs => HttpVerbs.Any;

        public override string HandleHtml()
        {
            var html = new HtmlBuilder();
            html.Title = "Encrypt File";
            var files = Files;
            var password = GetParameterString("password").TrimOrNull();
            if (files.Count == 0 || password == null)
            {
                html.Form(action: "?");
                html.P();
                html.InputPassword("password", label: "Password ");
                html.PEnd();
                html.P("Click on the 'Choose File' button to upload a file");
                html.InputFile("file");
                html.InputSubmit("Encrypt");
                html.FormEnd();
            }
            else
            {
                var file = files.First();
                var fileName = file.Key;
                var fileData = file.Value;
                var encryptedBytes = Encryption.Encrypt(Constant.ENCODING_UTF8_WITHOUT_BOM.GetBytes(password), fileData);
                Context.SendFile(encryptedBytes, fileName);
                html.P("File encrypted");
            }
            return html.ToString();
        }
    }
}
