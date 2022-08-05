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

namespace MaxRunSoftware.Utilities.Tests.Sql;

[Trait("Type", "Sql")]
public abstract class SqlTestBase : TestBase
{
    protected SqlTestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
}

public abstract class SqlTestBase<TSql> : SqlTestBase where TSql : Utilities.Sql, new()
{
    protected SqlTestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected abstract IDbConnection CreateConnection();

    protected abstract IReadOnlyList<string> Queries { get; }

    protected TSql CreateSql() => new() { ConnectionFactory = CreateConnection };

    [TestFact]
    public void QueryTests()
    {
        var c = CreateSql();

        var src = c.ExecuteQuery(Queries[0]);
        Assert.Single(src);
        var sr = src[0];
        Info(sr.Columns.Names.ToStringDelimited(", "));
        Assert.Equal(typeof(int), sr.Columns[0].DataType);

        src = c.ExecuteQuery(Queries[1]);
        Assert.Single(src);
        sr = src[0];
        Info(sr.Columns.Names.ToStringDelimited(", "));
        Assert.Equal(typeof(int), sr.Columns[0].DataType);
    }


    [TestFact]
    public void GetCurrentDatabaseName()
    {
        var o = CreateSql().GetCurrentDatabaseName();
        Info(o);
        Assert.NotNull(o);
    }

    [TestFact]
    public void GetCurrentSchemaName()
    {
        var o = CreateSql().GetCurrentSchemaName();
        Info(o);
        Assert.NotNull(o);
    }

    [TestFact]
    public void GetDatabases()
    {
        var os = CreateSql().GetDatabases();
        Info(os);
        Assert.NotEmpty(os);
    }

    [TestFact]
    public void GetSchemas()
    {
        var os = CreateSql().GetSchemas();
        Info(os);
        Assert.NotEmpty(os);
    }

    [TestFact]
    public void GetTables()
    {
        var os = CreateSql().GetTables();
        Info(os);
        Assert.NotEmpty(os);
    }

    [TestFact]
    public void GetTableColumns()
    {
        var os = CreateSql().GetTableColumns();
        Info(os);
        Assert.NotEmpty(os);
    }
}
