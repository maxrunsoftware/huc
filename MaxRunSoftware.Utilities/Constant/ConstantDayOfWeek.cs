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
    public static readonly ImmutableArray<string> DayOfWeek_Sunday_Strings = ImmutableArray.Create("U,SU,SUN,SUND,SUNDA,SUNDAY".Split(','));
    public static readonly ImmutableArray<string> DayOfWeek_Monday_Strings = ImmutableArray.Create("M,MO,MON,MOND,MONDA,MONDAY".Split(','));
    public static readonly ImmutableArray<string> DayOfWeek_Tuesday_Strings = ImmutableArray.Create("T,TU,TUE,TUES,TUESD,TUESDA,TUESDAY".Split(','));
    public static readonly ImmutableArray<string> DayOfWeek_Wednesday_Strings = ImmutableArray.Create("W,WE,WED,WEDN,WEDNE,WEDNES,WEDNESD,WEDNESDA,WEDNESDAY".Split(','));
    public static readonly ImmutableArray<string> DayOfWeek_Thursday_Strings = ImmutableArray.Create("R,TH,THU,THUR,THURS,THURSD,THURSDA,THURSDAY".Split(','));
    public static readonly ImmutableArray<string> DayOfWeek_Friday_Strings = ImmutableArray.Create("F,FR,FRI,FRID,FRIDA,FRIDAY".Split(','));
    public static readonly ImmutableArray<string> DayOfWeek_Saturday_Strings = ImmutableArray.Create("S,SA,SAT,SATU,SATUR,SATURD,SATURDA,SATURDAY".Split(','));
    public static readonly ImmutableArray<DayOfWeek> DaysOfWeek = ImmutableArray.Create(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);
    public static readonly ImmutableDictionary<string, DayOfWeek> String_DayOfWeek = String_DayOfWeek_Create();

    private static ImmutableDictionary<string, DayOfWeek> String_DayOfWeek_Create()
    {
        var strings = new[] { DayOfWeek_Sunday_Strings, DayOfWeek_Monday_Strings, DayOfWeek_Tuesday_Strings, DayOfWeek_Wednesday_Strings, DayOfWeek_Thursday_Strings, DayOfWeek_Friday_Strings, DayOfWeek_Saturday_Strings };
        var b = ImmutableDictionary.CreateBuilder<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < DaysOfWeek.Length; i++)
        {
            foreach (var s in strings[i]) b.Add(s, DaysOfWeek[i]);
        }

        return b.ToImmutable();
    }
}
