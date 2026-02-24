using MediatR;
using BikMetric.Application.Common.Interfaces;
using BikMetric.Application.Metadata.Common;

namespace BikMetric.Application.Metadata.Queries.GetTables;

public record GetTablesQuery : IRequest<List<TableMetaDto>>;

public class GetTablesQueryHandler : IRequestHandler<GetTablesQuery, List<TableMetaDto>>
{
    private readonly IDynamicQueryService _queryService;

    public GetTablesQueryHandler(IDynamicQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<List<TableMetaDto>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_queryService.GetTablesMetadata());
    }
}
