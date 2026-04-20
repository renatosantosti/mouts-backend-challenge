using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        ILogger<CreateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResponse> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = Sale.Create(
            request.SaleNumber,
            request.Date,
            request.CustomerId,
            request.CustomerName,
            request.BranchId,
            request.BranchName);

        await _saleRepository.CreateAsync(sale, cancellationToken);

        SimulatedSalesEventBroker.PublishAndClear(_logger, sale);

        return _mapper.Map<SaleResponse>(sale);
    }
}
