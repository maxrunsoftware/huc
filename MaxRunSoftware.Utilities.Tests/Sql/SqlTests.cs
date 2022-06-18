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

using System.Data;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using Xunit.Abstractions;

namespace MaxRunSoftware.Utilities.Tests;

public abstract class SqlTestBase<TSql> where TSql : Sql, new()
{
    protected readonly ITestOutputHelper output;

    protected SqlTestBase(ITestOutputHelper testOutputHelper) { output = testOutputHelper; }

    protected abstract IDbConnection CreateConnection();

    protected abstract IReadOnlyList<string> Queries { get; }

    private TSql CreateSql() => new TSql { ConnectionFactory = CreateConnection };
    
    [Fact]
    public void QueryTests()
    {
        var c = CreateSql();
        
        var src = c.ExecuteQuery(Queries[0]);
        Assert.Single(src);
        var sr = src[0];
        output.WriteLine(sr.Columns.Names.ToStringDelimited(", "));
        Assert.Equal(typeof(int), sr.Columns[0].DataType);

        src = c.ExecuteQuery(Queries[1]);
        Assert.Single(src);
        sr = src[0];
        output.WriteLine(sr.Columns.Names.ToStringDelimited(", "));
        Assert.Equal(typeof(int), sr.Columns[0].DataType);
        
        
        
        
    }

   

    [Fact]
    public void GetCurrentDatabaseName()
    {
        var c = CreateSql();
        Assert.NotNull(c.GetCurrentDatabaseName());
    }
    
    [Fact]
    public void GetCurrentSchemaName()
    {
        var c = CreateSql();
        Assert.NotNull(c.GetCurrentSchemaName());
    }
    
    [Fact]
    public void GetDatabases()
    {
        var c = CreateSql();
        Assert.NotEmpty(c.GetDatabases());
    }
    
    [Fact]
    public void GetSchemas()
    {
        var c = CreateSql();
        Assert.NotEmpty(c.GetSchemas());
    }

    [Fact]
    public void GetTables()
    {
        var c = CreateSql();
        Assert.NotEmpty(c.GetTables());
    }

    [Fact]
    public void GetTableColumns()
    {
        var c = CreateSql();
        var o = c.GetTableColumns();
        Assert.NotEmpty(o);
    }
    
}

public class MsSqlTests : SqlTestBase<SqlMsSql>
{
    protected override IDbConnection CreateConnection() => new SqlConnection(Config.Sql_MsSql_ConnectionString);

    public MsSqlTests(ITestOutputHelper output) : base(output) { }

    protected override IReadOnlyList<string> Queries => new[]
    {
        "SELECT * FROM NORTHWIND.dbo.ORDERS",
        "SELECT * FROM NORTHWIND.dbo.ORDERS WHERE 1=0",
        
    };
}

public class MySqlTests : SqlTestBase<SqlMySql>
{
    protected override IDbConnection CreateConnection() => new MySqlConnection(Config.Sql_MySql_ConnectionString);

    public MySqlTests(ITestOutputHelper output) : base(output) { }

    protected override IReadOnlyList<string> Queries => new[]
    {
        "SELECT * FROM NORTHWIND.ORDERS",
        "SELECT * FROM NORTHWIND.ORDERS WHERE 1=0",
    };
}

public class OracleTests : SqlTestBase<SqlOracle>
{
    protected override IDbConnection CreateConnection() => new OracleConnection(Config.Sql_Oracle_ConnectionString);

    public OracleTests(ITestOutputHelper output) : base(output) { }

    protected override IReadOnlyList<string> Queries => new[]
    {
        "SELECT * FROM NORTHWIND.ORDERS",
        "SELECT * FROM NORTHWIND.ORDERS WHERE 1=0",
    };
}
