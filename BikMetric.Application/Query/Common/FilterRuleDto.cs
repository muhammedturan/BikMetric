namespace BikMetric.Application.Query.Common;

public record FilterRuleDto
{
    public string Column { get; init; } = string.Empty;
    public string Operator { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
