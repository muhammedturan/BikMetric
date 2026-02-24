namespace BikMetric.Application.Query.Common;

public record QueryResultDto
{
    public List<string> Columns { get; init; } = new();
    public List<Dictionary<string, object?>> Rows { get; init; } = new();
    public int TotalCount { get; init; }
}
