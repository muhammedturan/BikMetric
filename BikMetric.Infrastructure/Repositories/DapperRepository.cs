using System.Data;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using BikMetric.Application.Common.Interfaces;

namespace BikMetric.Infrastructure.Repositories;

public class DapperRepository : IDapperRepository
{
    private readonly string _connectionString;

    static DapperRepository()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public DapperRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<T>(sql, param, transaction);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, param, transaction);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<T>(sql, param, transaction);
    }
}
