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

using MaxRunSoftware.Utilities.CommandLine;

namespace MaxRunSoftware.Utilities.Tests.CommandLine;

public abstract class OptionTestsBase : CommandTestsBase
{
    // ReSharper disable once PublicConstructorInAbstractClass
    public OptionTestsBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
}

internal static class OptionTestsExtensions
{
    public static OptionDetail Single(this IEnumerable<OptionDetail> details, string name)
    {
        var items = details.Where(o => StringComparer.Ordinal.Equals(o.Name, name)).ToList();
        Assert.Single(items);
        return items[0];
    }

    public static OptionDetail Single(this IEnumerable<OptionDetail> details) => details.Single(CommandTestsBase.PROPERTY);


    public static OptionDetailWrapped Single(this IEnumerable<OptionDetailWrapped> details, string name)
    {
        var detailsWrapped = details as ICollection<OptionDetailWrapped> ?? details.ToList();
        var items = detailsWrapped.Where(o => StringComparer.Ordinal.Equals(o.Detail?.Name, name)).ToList();
        if (items.Count == 0) items = detailsWrapped.Where(o => StringComparer.Ordinal.Equals(o.Info.Name, name)).ToList();
        Assert.Single(items);
        return items[0];
    }

    public static OptionDetailWrapped Single(this IEnumerable<OptionDetailWrapped> details) => details.Single(CommandTestsBase.PROPERTY);

    public static T SetValueNew<T>(this OptionDetail detail, string? valueString) where T : new()
    {
        var o = new T();
        detail.SetValue(o, valueString);
        return o;
    }

    // ReSharper disable once UnusedParameter.Global
    public static T SetValueNew<T>(this OptionDetail detail, T oOld, string? valueString) where T : new()
    {
        var o = new T();
        detail.SetValue(o, valueString);
        return o;
    }

    public static void ShowErrors(this CommandScannerResult r, Action<object> info)
    {
        if (r.CommandsInvalid.Count <= 0) return;
        foreach (var ci in r.CommandsInvalid)
        {
            if (ci.Exception != null) info(ci.Type.Type.Name + ": " + ci.Exception);
            foreach (var detailWrapped in ci.Options)
            {
                if (detailWrapped.Exception != null) info(detailWrapped.Type.Type.Name + "." + detailWrapped.Info.Name + ": " + detailWrapped.Exception);
            }

            foreach (var detailWrapped in ci.Arguments)
            {
                if (detailWrapped.Exception != null) info(detailWrapped.Type.Type.Name + "." + detailWrapped.Info.Name + ": " + detailWrapped.Exception);
            }
        }
    }
}
