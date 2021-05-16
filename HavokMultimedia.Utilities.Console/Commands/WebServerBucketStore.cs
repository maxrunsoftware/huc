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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using EmbedIO;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServerBucketStore : WebServerBase
    {
        private static string GenerateRandomData(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

            using (var srandom = RandomNumberGenerator.Create())
            {
                var sb = new StringBuilder();
                for (int i = 0; i < length; i++)
                {
                    sb.Append(srandom.Pick(chars));
                }
                return sb.ToString();
            }
        }
        private readonly BucketStoreMemoryString store = new BucketStoreMemoryString();
        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();
            var config = GetConfig();

            store["store1"][GenerateRandomData(3)] = GenerateRandomData(20);
            store["store1"][GenerateRandomData(3)] = GenerateRandomData(20);
            store["store2"][GenerateRandomData(3)] = GenerateRandomData(20);
            store["store2"][GenerateRandomData(3)] = GenerateRandomData(20);
            store["store2"][GenerateRandomData(3)] = GenerateRandomData(20);
            store["store3"][GenerateRandomData(3)] = GenerateRandomData(20);
            store["store3"][GenerateRandomData(3)] = GenerateRandomData(20);

            config.AddPathHandler("/", HttpVerbs.Any, Index);

            using (var server = GetWebServer(config))
            {
                foreach (var ipa in config.UrlPrefixes) log.Info("  " + ipa);
                log.Info("WebServer running, press ESC or Q to quit");
                while (true)
                {
                    Thread.Sleep(50);
                    var cki = System.Console.ReadKey(true);

                    if (cki.Key.In(ConsoleKey.Escape, ConsoleKey.Q)) break;
                }
            }

            log.Info("WebServer shutdown");
        }

        private object Serialize(object o) => Swan.Formatters.Json.Serialize(o, format: true);



        private object Index(IHttpContext context)
        {
            var name = context.GetParameterString("name").TrimOrNull();
            var key = context.GetParameterString("key").TrimOrNull();
            var value = context.GetParameterString("value").TrimOrNull();
            if (context.HasParameter("value"))
            {
                if (name != null && key != null) store[name][key] = value;
                return string.Empty;
            }

            if (name == null) return Serialize(store.Buckets.ToArray());
            var bucket = store[name];
            if (key != null) return bucket[key];

            var list = new List<string[]>();
            foreach (var k in bucket.Keys)
            {
                list.Add(new string[] { k, bucket[k] });
            }
            return Serialize(list.ToArray());


        }

    }


}

