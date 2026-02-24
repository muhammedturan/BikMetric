using FluentValidation;
using MediatR;
using BikMetric.Application.Common.Interfaces;
using BikMetric.Application.Query.Common;

namespace BikMetric.Application.Query.Commands.ExecuteQuery;

public record ExecuteQueryCommand : IRequest<QueryResultDto>
{
    public string TableName { get; init; } = string.Empty;
    public List<FilterRuleDto> Filters { get; init; } = new();
}

public class ExecuteQueryCommandValidator : AbstractValidator<ExecuteQueryCommand>
{
    public ExecuteQueryCommandValidator()
    {
        RuleFor(x => x.TableName).NotEmpty();
    }
}

public class ExecuteQueryCommandHandler : IRequestHandler<ExecuteQueryCommand, QueryResultDto>
{
    private readonly IDynamicQueryService _queryService;

    public ExecuteQueryCommandHandler(IDynamicQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<QueryResultDto> Handle(ExecuteQueryCommand request, CancellationToken cancellationToken)
    {
        return await _queryService.ExecuteDynamicQueryAsync(request.TableName, request.Filters);
    }
}
