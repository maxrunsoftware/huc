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

using System.Collections.Generic;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console;

public class CommandHelpBuilder
{
    public CommandHelpBuilder(string name) { Name = name; }

    public string Name { get; }

    private readonly List<string> summary = new();
    public string Summary => summary.FirstOrDefault();

    private readonly List<(string p1, string p2, string description)> parameters = new();
    public IReadOnlyList<(string p1, string p2, string description)> Parameters => parameters;

    private readonly List<string> values = new();
    public IReadOnlyList<string> Values => values;

    private readonly List<string> details = new();
    public IReadOnlyList<string> Details => details;

    private readonly List<string> examples = new();
    public IReadOnlyList<string> Examples => examples;

    public void AddSummary(string msg) => summary.Add(msg);

    public void AddValue(string msg) => values.Add(msg);

    public void AddDetail(string msg) => details.Add(msg);

    public void AddParameter(string p1, string p2, string description) => parameters.Add((p1, p2, description));

    public void AddExample(string example) => examples.Add(example.Replace("`", "\""));
}
