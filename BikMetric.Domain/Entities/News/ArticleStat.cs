using BikMetric.Core.BaseClasses;

namespace BikMetric.Domain.Entities.News;

public class ArticleStat : BaseEntity
{
    public Guid ArticleId { get; set; }
    public DateTime Date { get; set; }
    public ulong Views { get; set; }
    public ulong Clicks { get; set; }
    public uint Shares { get; set; }
    public uint Comments { get; set; }
    public double AvgReadTimeSeconds { get; set; }
}
