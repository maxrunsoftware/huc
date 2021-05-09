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
using System.Security.Cryptography;
using System.Text;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServerUtilityGenerateKeyPair : WebServerUtilityBase
    {

        public (bool success, string publicKey, string privateKey) Handle()
        {
            var lengthN = GetParameterInt("length");
            if (lengthN == null) return (false, null, null);
            var length = lengthN.Value;

            var keyPair = Encryption.GenerateKeyPair(length: length);
            return (true, keyPair.publicKey, keyPair.privateKey);

        }

        public override string HandleHtml()
        {
            try
            {
                var result = Handle();

                var html = $@"
<form>
<p>
    <label for='length'>Length </label>
    <input type='text' id='length' name='length' size='80' value='1024'>
    <br><br>

    <input type='submit' value='Generate'>
    <br><br>

</p>
</form>
";
                if (result.success)
                {
                    html += $@"
<br><br>
<h2>Public Key</h2>
<textarea id='publicKey' name='publicKey' rows='12' cols='80'>{result.publicKey}</textarea>
<br><br>

<h2>Private Key</h2>
<textarea id='privateKey' name='privateKey' rows='12' cols='80'>{result.privateKey}</textarea>
";
                }

                return External.WebServer.HtmlMessage("Asymetric Key Pair", html.Replace("'", "\""));

            }
            catch (Exception e)
            {
                return External.WebServer.HtmlMessage(e.GetType().FullNameFormatted(), e.ToString());
            }
        }


    }
}
