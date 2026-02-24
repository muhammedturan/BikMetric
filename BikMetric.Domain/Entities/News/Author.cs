using BikMetric.Core.BaseClasses;

namespace BikMetric.Domain.Entities.News;

public class Author : BaseEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
}
