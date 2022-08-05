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


public class CommandDetailWrapped
{
    public CommandDetailWrapped(TypeSlim type, CommandAttribute attribute, CommandDetail? detail, Exception? exception)
    {
        Type = type;
        Attribute = attribute;
        Detail = detail;
        Exception = exception;
    }

    public TypeSlim Type { get; }
    public CommandAttribute Attribute { get; }
    public CommandDetail? Detail { get; }
    public Exception? Exception { get; }

    public List<OptionDetailWrapped> Options { get; } = new();
    public List<ArgumentDetailWrapped> Arguments { get; } = new();

    public static List<CommandDetailWrapped> Scan(IEnumerable<TypeSlim> types, BindingFlags flags)
    {
        var list = new List<CommandDetailWrapped>();
        foreach (var type in types)
        {
            var attr = CommandUtil.GetAttribute<CommandAttribute>(type.Type);
            if (attr == null) continue;

            CommandDetail? detail = null;
            Exception? exception = null;

            try
            {
                detail = new CommandDetail(type, attr);
            }
            catch (Exception e)
            {
                exception = e;
            }

            var cdw = new CommandDetailWrapped(type, attr, detail, exception);
            cdw.Options.AddRange(OptionDetailWrapped.Scan(type, flags));
            cdw.Arguments.AddRange(ArgumentDetailWrapped.Scan(type, flags));

            if (detail != null)
            {
                detail.Options.AddRange(cdw.Options.Where(o => o.Detail != null && o.Exception == null).Select(o => o.Detail!));
                detail.Arguments.AddRange(cdw.Arguments.Where(o => o.Detail != null && o.Exception == null).Select(o => o.Detail!));
            }

            list.Add(cdw);
        }

        return list;
    }
}
