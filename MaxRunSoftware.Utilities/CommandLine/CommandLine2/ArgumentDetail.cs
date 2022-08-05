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

public class ArgumentDetail : PropertyDetail<ArgumentAttribute>
{
    public ushort Index { get; }
    public int MinCount { get; }

    public ArgumentDetail(TypeSlim type, PropertyInfo info, ArgumentAttribute attribute) : base(type, info, attribute)
    {
        Index = (ushort)Attribute.Index.CheckMin(nameof(Index), 0).CheckMax(nameof(Index), ushort.MaxValue);
        MinCount = Attribute.MinCount ?? (Info.IsNullable() ? 0 : 1);
    }
}
