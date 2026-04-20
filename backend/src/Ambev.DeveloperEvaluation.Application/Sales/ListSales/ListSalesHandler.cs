using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public sealed class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        var domainQuery = new SaleListQuery
        {
            Page = request.Page,
            Size = request.Size,
            Order = request.Order,
            Filters = request.Filters
        };

        var (items, totalCount) = await _saleRepository.ListAsync(domainQuery, cancellationToken);

        return new ListSalesResult
        {
            Items = _mapper.Map<IReadOnlyList<SaleResponse>>(items),
            TotalCount = totalCount,
            Page = Math.Max(1, request.Page),
            Size = Math.Clamp(request.Size, 1, 100)
        };
    }
}
