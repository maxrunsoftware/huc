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

namespace MaxRunSoftware.Utilities;

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    public const int BufferSize_Min = 1024 * 4;

    /// <summary>
    /// We pick a value that is the largest multiple of 4096 that is still smaller than the
    /// large object heap threshold (85K). The CopyTo/CopyToAsync buffer is short-lived and is
    /// likely to be collected at Gen0, and it offers a significant improvement in Copy performance.
    /// </summary>
    public const int BufferSize_Optimal = 81920;
}
