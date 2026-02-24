using BikMetric.Application.Ai.Common;

namespace BikMetric.Application.Common.Interfaces;

public interface IOllamaService
{
    Task<SqlGenerationResult> GenerateSqlAsync(string question);
    Task<RuleGenerationResult> GenerateRulesAsync(string question);
}
