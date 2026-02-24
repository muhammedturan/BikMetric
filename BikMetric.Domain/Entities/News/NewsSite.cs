using BikMetric.Core.BaseClasses;

namespace BikMetric.Domain.Entities.News;

public class NewsSite : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Country { get; set; } = "Turkiye";
    public string Language { get; set; } = "Turkce";
    public ulong MonthlyVisitors { get; set; }
}
