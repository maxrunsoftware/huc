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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External
{
    public abstract class VMwareObject
    {
        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public JToken QueryValueObjectSafe(VMware vmware, string path)
        {
            try
            {
                return vmware.GetValue(path);
            }
            catch (Exception e)
            {
                log.Warn("Error querying " + path, e);
            }
            return null;
        }

        public IEnumerable<JToken> QueryValueArraySafe(VMware vmware, string path)
        {
            try
            {
                return vmware.GetValueArray(path);
            }
            catch (Exception e)
            {
                log.Warn("Error querying " + path, e);
            }
            return Array.Empty<JObject>();
        }

        protected PropertyInfo[] GetProperties()
        {
            var list = new List<PropertyInfo>();
            foreach (var prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                list.Add(prop);
            }
            return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetType().NameFormatted());

            foreach (var property in ClassReaderWriter.GetProperties(GetType(), isInstance: true, canGet: true))
            {
                var val = property.GetValue(this);
                if (val == null)
                {
                    sb.AppendLine("  " + property.Name + ": ");
                }
                else if (val is string)
                {
                    sb.AppendLine("  " + property.Name + ": " + val.ToStringGuessFormat());
                }
                else if (val is IEnumerable)
                {
                    int count = 0;
                    foreach (var item in (IEnumerable)val)
                    {
                        var vitem = (VMwareObject)item;
                        var vItemType = vitem.GetType();
                        sb.AppendLine("  " + vItemType.NameFormatted() + "[" + count + "]");
                        foreach (var prop in ClassReaderWriter.GetProperties(vItemType, canGet: true, isInstance: true))
                        {
                            sb.AppendLine("    " + prop.Name + ": " + prop.GetValue(vitem).ToStringGuessFormat());
                        }
                        count++;
                    }
                }
                else
                {
                    sb.AppendLine("  " + property.Name + ": " + val.ToStringGuessFormat());
                }
            }

            return sb.ToString();
        }
    }

}
