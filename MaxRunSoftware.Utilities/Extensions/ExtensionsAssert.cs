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

using System.Runtime.Serialization;

namespace MaxRunSoftware.Utilities;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
[Serializable]
public class DebugException : Exception
{
    public DebugException() { }
    protected DebugException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public DebugException(string? message) : base(message) { }
    public DebugException(string? message, Exception? innerException) : base(message, innerException) { }
}

[Serializable]
public class AssertException : DebugException
{
    public AssertException() { }
    protected AssertException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public AssertException(string? message) : base(message) { }
    public AssertException(string? message, Exception? innerException) : base(message, innerException) { }
}

public static class ExtensionsAssert
{
#if DEBUG
    private static readonly bool ENABLED = true;
#else
    private static readonly bool ENABLED = false;
#endif

    public static void AssertTrue(this bool obj)
    {
        if (ENABLED && !obj) throw new AssertException("Expected value to be true");
    }

    public static void AssertFalse(this bool obj)
    {
        if (ENABLED && obj) throw new AssertException("Expected value to be false");
    }

    public static void AssertNotNull(this object? obj)
    {
        if (ENABLED && obj == null) throw new AssertException("Expected value to not be null");
    }

    public static void AssertIsType(this object obj, bool isExactType, params Type[] types)
    {
        AssertNotNull(obj);
        if (obj == null) return; // don't throw NRE

        if (types == null || types.Length == 0) throw new AssertException("No types specified to match against");

        var objType = obj as Type ?? obj.GetType();
        foreach (var targetType in types)
        {
            if (targetType == null) continue;
            if (objType.IsAssignableTo(targetType)) return;
        }

        throw new AssertException($"Object {obj} is not assignable to any of the types ({types.Length}) specified: " + types.Select(o => o.NameFormatted()).ToStringDelimited(" | "));
    }
}
