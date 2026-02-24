using BikMetric.Application.Query.Common;

namespace BikMetric.Application.Ai.Common;

public record SqlGenerationResult
{
    public string Sql { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public string Raw { get; init; } = string.Empty;
}

public record RuleGenerationResult
{
    public string TableName { get; init; } = string.Empty;
    public List<FilterRuleDto> Filters { get; init; } = new();
    public string Explanation { get; init; } = string.Empty;
}

public record AiQueryResultDto
{
    public string Question { get; init; } = string.Empty;
    public string GeneratedSql { get; init; } = string.Empty;
    public List<string> Columns { get; init; } = new();
    public List<Dictionary<string, object?>> Rows { get; init; } = new();
    public int TotalCount { get; init; }
    public string Explanation { get; init; } = string.Empty;
}

public record AiRuleResultDto
{
    public string Question { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public List<FilterRuleDto> Filters { get; init; } = new();
    public string Explanation { get; init; } = string.Empty;
}
