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

using System.Net;

namespace MaxRunSoftware.Utilities;

public static partial class Constant
{
    public static readonly IPAddress IPAddress_Min = IPAddress.Any;
    public static readonly IPAddress IPAddress_Max = IPAddress.Broadcast;

    public static readonly Guid Guid_Min = new("00000000-0000-0000-0000-000000000000");
    public static readonly Guid Guid_Max = new("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

    public const float Float_Zero = 0;
    public const double Double_Zero = 0;
}
