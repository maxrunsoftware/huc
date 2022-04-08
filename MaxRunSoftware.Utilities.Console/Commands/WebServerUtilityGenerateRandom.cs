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

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using EmbedIO;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class WebServerUtilityGenerateRandom : WebServerUtilityBase
    {
        private string HtmlPage()
        {
            var html = $@"
<form>
<p>
    <label for='length'>Length </label>
    <input type='text' id='length' name='length' size='80' value='100'>
    <br><br>

    <label for='characters'>Characters </label>
    <input type='text' id='characters' name='characters' size='80' value='{Constant.CHARS_0_9 + Constant.CHARS_A_Z_LOWER}'>
    <br><br>

    <label for='count'>Count </label>
    <input type='text' id='count' name='count' size='80' value='1'>
    <br><br>

    <input type='submit' value='Generate'>
    <br><br>

</p>
</form>
";
            return html.Replace("'", "\"");
        }
        public string[] Handle()
        {
            var lengthN = GetParameterInt("length");
            if (lengthN == null) return null;
            var length = lengthN.Value;
            var chars = GetParameterString("characters");
            if (chars == null) return null;

            var countN = GetParameterInt("count");
            if (countN == null) return null;
            var count = countN.Value;

            var list = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder();
                using (var srandom = RandomNumberGenerator.Create())
                {

                    for (int j = 0; j < length; j++)
                    {
                        var c = chars[srandom.Next(chars.Length)];
                        sb.Append(c);
                    }

                }
                list.Add(sb.ToString());

            }

            return list.ToArray();
        }

        public override string HandleHtml()
        {
            try
            {
                var result = Handle();
                if (result == null)
                {
                    return External.WebServer.HtmlMessage("Generate Random", HtmlPage());
                }
                var body = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    body.Append("<p>");
                    body.Append(result[i]);
                    body.Append("</p>");
                }
                return External.WebServer.HtmlMessage("Random Data", body.ToString());
            }
            catch (Exception e)
            {
                return External.WebServer.HtmlMessage(e.GetType().FullNameFormatted(), e.ToString());
            }
        }


    }
}
