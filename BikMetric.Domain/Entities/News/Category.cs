using BikMetric.Core.BaseClasses;

namespace BikMetric.Domain.Entities.News;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
