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

public class SingleUse
{
    private readonly AtomicBoolean boolean = false;

    public bool IsUsed => boolean;

    /// <summary>
    /// Attempts to 'use' this instance. If this is the first time using it, we will return
    /// true. Otherwise we return false if we have already been used.
    /// </summary>
    /// <returns>true if we have never used before, false if we have already been used</returns>
    public bool TryUse() => boolean.SetTrue();
}
