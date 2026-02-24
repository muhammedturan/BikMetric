using BikMetric.Application.Metadata.Common;
using BikMetric.Application.Query.Common;

namespace BikMetric.Application.Common.Interfaces;

public interface IDynamicQueryService
{
    List<TableMetaDto> GetTablesMetadata();
    Task<QueryResultDto> ExecuteDynamicQueryAsync(string tableName, List<FilterRuleDto> filters);
    Task<QueryResultDto> ExecuteRawSqlAsync(string sql);
}
