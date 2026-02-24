namespace BikMetric.Application.Metadata.Common;

public record ColumnMetaDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}

public record TableMetaDto
{
    public string TableName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<ColumnMetaDto> Columns { get; init; } = new();
}
