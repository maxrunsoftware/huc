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

namespace MaxRunSoftware.Utilities.CommandLine;

public class ArgumentDetailWrapped : PropertyDetailWrapped<ArgumentAttribute, ArgumentDetail>
{
    public ArgumentDetailWrapped(TypeSlim type, PropertyInfo info, ArgumentAttribute attribute, ArgumentDetail? detail, Exception? exception) : base(type, info, attribute, detail, exception) { }

    public static List<ArgumentDetailWrapped> Scan(TypeSlim type, BindingFlags flags)
    {
        var list = new List<ArgumentDetailWrapped>();
        foreach (var info in type.Type.GetProperties(flags))
        {
            var attr = CommandUtil.GetAttribute<ArgumentAttribute>(info);
            if (attr == null) continue;

            ArgumentDetail? detail = null;
            Exception? exception = null;

            try
            {
                detail = new ArgumentDetail(type, info, attr);
            }
            catch (Exception e)
            {
                exception = e;
            }

            list.Add(new ArgumentDetailWrapped(type, info, attr, detail, exception));
        }

        return list;
    }
}
