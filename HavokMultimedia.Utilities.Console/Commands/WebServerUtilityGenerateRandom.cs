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
using System.Security.Cryptography;
using System.Text;
using EmbedIO;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServerUtilityGenerateRandom : WebServerUtilityBase
    {
        public string Handle()
        {
            var sb = new StringBuilder();
            using (var srandom = RandomNumberGenerator.Create())
            {
                var length = GetParameterInt("length", 100);
                var chars = GetParameterString("chars", Constant.CHARS_A_Z_LOWER + Constant.CHARS_0_9);

                for (int i = 0; i < length; i++)
                {
                    var c = chars[srandom.Next(chars.Length)];
                    sb.Append(c);
                }

            }
            return sb.ToString();
        }

        public override string HandleHtml() => Handle();

        public override string HandleJson()
        {
            return base.HandleJson();
        }
    }
}
