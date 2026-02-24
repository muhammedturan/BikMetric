using FluentValidation;
using MediatR;
using BikMetric.Application.Ai.Common;
using BikMetric.Application.Common.Interfaces;

namespace BikMetric.Application.Ai.Commands.AiQuery;

public record AiQueryCommand : IRequest<AiQueryResultDto>
{
    public string Question { get; init; } = string.Empty;
}

public class AiQueryCommandValidator : AbstractValidator<AiQueryCommand>
{
    public AiQueryCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MinimumLength(3);
    }
}

public class AiQueryCommandHandler : IRequestHandler<AiQueryCommand, AiQueryResultDto>
{
    private readonly IOllamaService _ollamaService;
    private readonly IDynamicQueryService _queryService;

    public AiQueryCommandHandler(IOllamaService ollamaService, IDynamicQueryService queryService)
    {
        _ollamaService = ollamaService;
        _queryService = queryService;
    }

    public async Task<AiQueryResultDto> Handle(AiQueryCommand request, CancellationToken cancellationToken)
    {
        var sqlResult = await _ollamaService.GenerateSqlAsync(request.Question);

        if (string.IsNullOrWhiteSpace(sqlResult.Sql))
        {
            return new AiQueryResultDto
            {
                Question = request.Question,
                GeneratedSql = "",
                Explanation = sqlResult.Explanation,
            };
        }

        var queryResult = await _queryService.ExecuteRawSqlAsync(sqlResult.Sql);

        return new AiQueryResultDto
        {
            Question = request.Question,
            GeneratedSql = sqlResult.Sql,
            Columns = queryResult.Columns,
            Rows = queryResult.Rows,
            TotalCount = queryResult.TotalCount,
            Explanation = sqlResult.Explanation,
        };
    }
}
