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

using System.Linq;
using EmbedIO;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class WebServerBucketStore : WebServerBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);

        help.AddSummary("Creates a web server to host a name+key+value store");
        help.AddExample("-o=80");
        help.AddDetail("Store a value: http://192.168.1.5?name=store1&key=mykey&value=abc");
        help.AddDetail("Retrieve a value: http://192.168.1.5?name=store1&key=mykey");
        help.AddDetail("Retrieve all values in bucket: http://192.168.1.5?name=store1");
        help.AddDetail("Retrieve bucket names: http://192.168.1.5");
    }

    private readonly BucketStoreMemoryString store = new();

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();
        var config = GetConfig();

        config.AddPathHandler("/", HttpVerbs.Any, Index);

        LoopUntilKey(config);
    }

    private object Index(IHttpContext context)
    {
        var name = context.GetParameterString("name").TrimOrNull();
        var key = context.GetParameterString("key").TrimOrNull();
        var value = context.GetParameterString("value").TrimOrNull();
        if (context.HasParameter("value"))
        {
            // store value
            if (name != null && key != null) store[name][key] = value;

            using (var w = new JsonWriter(true))
            {
                using (w.Object())
                {
                    w.Property("key", key);
                    w.Property("value", value);
                }

                return w.ToString();
            }
        }

        if (name == null)
            // show all buckets
            /*
                {
                    "bucketsNames":
                    [
                        "store1",
                        "store2"
                    ]
                }
                */
            using (var w = new JsonWriter(true))
            {
                using (w.Object()) { w.Array("bucketNames", store.Buckets.ToArray()); }

                return w.ToString();
            }

        var bucket = store[name];
        if (key != null)
            // return value
            /*
                {   
                    key="a98"
                    value="dfa"
                }
                */
            using (var w = new JsonWriter(true))
            {
                using (w.Object())
                {
                    w.Property("key", key);
                    w.Property("value", bucket[key]);
                }

                return w.ToString();
            }


        // return name+key value
        /*
        {
            "buckets":
            [
                { key="a98", value="dfa" },
                { key="kdf", value="39f"
            ]
        }
        */
        using (var w = new JsonWriter(true))
        {
            using (w.Object())
            {
                using (w.Array("buckets"))
                {
                    foreach (var keyName in bucket.Keys)
                    {
                        using (w.Object())
                        {
                            w.Property("key", keyName);
                            w.Property("value", bucket[keyName]);
                        }
                    }
                }
            }

            return w.ToString();
        }
    }
}
