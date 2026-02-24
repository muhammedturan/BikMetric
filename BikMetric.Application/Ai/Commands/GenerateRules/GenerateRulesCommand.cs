using FluentValidation;
using MediatR;
using BikMetric.Application.Ai.Common;
using BikMetric.Application.Common.Interfaces;

namespace BikMetric.Application.Ai.Commands.GenerateRules;

public record GenerateRulesCommand : IRequest<AiRuleResultDto>
{
    public string Question { get; init; } = string.Empty;
}

public class GenerateRulesCommandValidator : AbstractValidator<GenerateRulesCommand>
{
    public GenerateRulesCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MinimumLength(3);
    }
}

public class GenerateRulesCommandHandler : IRequestHandler<GenerateRulesCommand, AiRuleResultDto>
{
    private readonly IOllamaService _ollamaService;

    public GenerateRulesCommandHandler(IOllamaService ollamaService)
    {
        _ollamaService = ollamaService;
    }

    public async Task<AiRuleResultDto> Handle(GenerateRulesCommand request, CancellationToken cancellationToken)
    {
        var result = await _ollamaService.GenerateRulesAsync(request.Question);

        return new AiRuleResultDto
        {
            Question = request.Question,
            TableName = result.TableName,
            Filters = result.Filters,
            Explanation = result.Explanation,
        };
    }
}
