using System.Data;

namespace BikMetric.Application.Common.Interfaces;

public interface IDapperRepository
{
    IDbConnection GetConnection();
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);
}
