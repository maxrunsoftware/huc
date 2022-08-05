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

public static class OptionExtensions
{
    private static readonly StringComparer co = StringComparer.Ordinal;
    private static readonly StringComparer coc = StringComparer.OrdinalIgnoreCase;

    public static OptionDetail? Named(this IEnumerable<OptionDetail> details, string name)
    {
        var items = details as ICollection<OptionDetail> ?? details.ToList();
        return items.FirstOrDefault(o => co.Equals(o.Name, name)) ??
               items.FirstOrDefault(o => coc.Equals(o.Name, name));
    }

    public static OptionDetailWrapped? Named(this IEnumerable<OptionDetailWrapped> details, string name)
    {
        var items = details as ICollection<OptionDetailWrapped> ?? details.ToList();
        return items.FirstOrDefault(o => co.Equals(o.Detail?.Name, name)) ??
               items.FirstOrDefault(o => coc.Equals(o.Detail?.Name, name)) ??
               items.FirstOrDefault(o => co.Equals(o.Attribute.Name, name)) ??
               items.FirstOrDefault(o => coc.Equals(o.Attribute.Name, name));
    }
}
