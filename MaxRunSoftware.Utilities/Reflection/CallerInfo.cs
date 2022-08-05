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

public sealed class CallerInfo : ImmutableObjectBase<CallerInfo>
{
    public string? FilePath { get; }
    public int? LineNumber { get; }
    public string? MemberName { get; }
    public string? ArgumentExpression { get; }

    // , [CallerFilePath] string? filePath = null, [CallerLineNumber] int? lineNumber = null, [CallerMemberName] string? memberName = null)
    public CallerInfo(string? callerFilePath, int? callerLineNumber, string? callerMemberName) : this(callerFilePath, callerLineNumber, callerMemberName, null) { }

    // , [CallerFilePath] string? filePath = null, [CallerLineNumber] int? lineNumber = null, [CallerMemberName] string? memberName = null, [CallerArgumentExpression("condition")] string? callerArgumentExpression = null)
    public CallerInfo(string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        // https://blog.jetbrains.com/dotnet/2021/11/04/caller-argument-expressions-in-csharp-10/
        // https://weblogs.asp.net/dixin/csharp-10-new-feature-callerargumentexpression-argument-check-and-more

        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information
        FilePath = callerFilePath.TrimOrNull();
        LineNumber = callerLineNumber;
        MemberName = callerMemberName.TrimOrNull();
        ArgumentExpression = callerArgumentExpression.TrimOrNull();
    }

    protected override int GetHashCode_Build() => Hash(FilePath, LineNumber, MemberName, ArgumentExpression);
    protected override string ToString_Build() => $"{nameof(CallerInfo)}({nameof(MemberName)}={MemberName}, {nameof(FilePath)}={FilePath}, {nameof(LineNumber)}={LineNumber}, {nameof(ArgumentExpression)}={ArgumentExpression})";
    protected override bool Equals_Internal(CallerInfo other) =>
        IsEqual(FilePath, other.FilePath) &&
        IsEqual(LineNumber, other.LineNumber) &&
        IsEqual(MemberName, other.MemberName) &&
        IsEqual(ArgumentExpression, other.ArgumentExpression);

    protected override int CompareTo_Internal(CallerInfo other)
    {
        int c;
        if (0 != (c = Compare(FilePath, other.FilePath))) return c;
        if (0 != (c = Compare(LineNumber, other.LineNumber))) return c;
        if (0 != (c = Compare(MemberName, other.MemberName))) return c;
        if (0 != (c = Compare(ArgumentExpression, other.ArgumentExpression))) return c;
        return 0;
    }
}
