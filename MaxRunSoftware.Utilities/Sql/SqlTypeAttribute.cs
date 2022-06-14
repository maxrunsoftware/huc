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

[AttributeUsage(AttributeTargets.Field)]
public class SqlTypeAttribute : Attribute
{
    public DbType DbType { get; }
    public Type DotNetType { get; set; }

    public string SqlTypeNames { get; set; }
    public object ActualSqlType { get; set; }

    public SqlTypeAttribute(DbType dbType) { DbType = dbType; }
}
