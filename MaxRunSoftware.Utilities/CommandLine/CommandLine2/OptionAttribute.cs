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

using System.Runtime.CompilerServices;

namespace MaxRunSoftware.Utilities.CommandLine;

[AttributeUsage(AttributeTargets.Property)]
public class OptionAttribute : PropertyAttribute
{
    public string? ShortName { get; set; }
    public object? Default { get; set; }
    public object? Min { get; set; }
    public object? Max { get; set; }
    public bool NoShortName { get; set; }
    public bool? IsRequired { get; set; }
    public bool IsHidden { get; set; }

    public OptionAttribute(string description, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = int.MinValue, [CallerMemberName] string? memberName = null) : base(description, filePath, lineNumber, memberName) { }
}
