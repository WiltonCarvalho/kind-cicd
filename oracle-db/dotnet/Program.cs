/*
 * Query Oracle AI Database through ADO.NET.
 *
 * This example requires the .NET 8 SDK and the Oracle.ManagedDataAccess.Core NuGet package.
 * Set ORACLE_USER and ORACLE_PASSWORD before running it.
 * The optional ORACLE_DATA_SOURCE defaults to localhost:1521/FREEPDB1.
 */

using System;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

const string DefaultDataSource = "localhost:1521/FREEPDB1";

string user = RequiredEnvironmentVariable("ORACLE_USER");
string password = RequiredEnvironmentVariable("ORACLE_PASSWORD");

// Note: ADO.NET uses a "Data Source" format rather than a JDBC URL format.
string dataSource = Environment.GetEnvironmentVariable("ORACLE_DATA_SOURCE") ?? DefaultDataSource;
string connectionString = $"Data Source={dataSource};User Id={user};Password={password};";

await using OracleConnection connection = new OracleConnection(connectionString);
await connection.OpenAsync();

await using OracleCommand command = connection.CreateCommand();
command.CommandText = "SELECT banner FROM v$version";

await using DbDataReader reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    // ADO.NET data readers are 0-indexed, whereas JDBC ResultSets are 1-indexed
    Console.WriteLine(reader.GetString(0));
}

static string RequiredEnvironmentVariable(string name)
{
    string? value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Set {name} before running this example.");
    }
    return value;
}