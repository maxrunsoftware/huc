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
    /// <summary>
    /// UTF8 encoding WITHOUT the Byte Order Marker
    /// </summary>
    public static readonly Encoding Encoding_UTF8 = new UTF8Encoding(false); // Thread safe according to https://stackoverflow.com/a/3024405

    /// <summary>
    /// UTF8 encoding WITH the Byte Order Marker
    /// </summary>
    public static readonly Encoding Encoding_UTF8_BOM = new UTF8Encoding(true); // Thread safe according to https://stackoverflow.com/a/3024405
}
