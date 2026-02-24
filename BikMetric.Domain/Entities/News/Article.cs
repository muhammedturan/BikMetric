using BikMetric.Core.BaseClasses;

namespace BikMetric.Domain.Entities.News;

public class Article : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public uint WordCount { get; set; }
    public byte HasImage { get; set; } = 1;
    public byte HasVideo { get; set; }
}
