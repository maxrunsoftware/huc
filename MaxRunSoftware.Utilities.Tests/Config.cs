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

// ReSharper disable InconsistentNaming

namespace MaxRunSoftware.Utilities.Tests;

/// <summary>
/// TODO: Make these based off config file or environment vars or something so other people can test
/// </summary>
public static class Config
{
    public static string Sql_MsSql_ConnectionString => "Server=172.16.46.3;Database=NorthWind;User Id=testuser;Password=testpass;TrustServerCertificate=True;";

    public static string Sql_MySql_ConnectionString => "Server=172.16.46.3;Database=NorthWind;User Id=testuser;Password=testpass;";

    //public static string Sql_Oracle_ConnectionString => "Data Source=172.16.46.9;User Id=system;Password=oracle;";
    public static string Sql_Oracle_ConnectionString => "Data Source=172.16.46.9:1521/orcl;User Id=testuser;Password=testpass;";

    public static string Temp_Directory => "/Users/user/github/huc/huc/temp";

    public static IDisposable CreateTempDirectory(out string path) => Util.CreateTempDirectory(Temp_Directory, out path);

    public static IEnumerable<Tuple<string, string>> SkipTraits => new[]
    {
        ("SqlType", "Oracle")
    }.Select(o => new Tuple<string, string>(o.Item1, o.Item2));
}
