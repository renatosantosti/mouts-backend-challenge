using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;

public sealed class AddItemToSaleHandler : IRequestHandler<AddItemToSaleCommand, SaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AddItemToSaleHandler> _logger;

    public AddItemToSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        ILogger<AddItemToSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResponse> Handle(AddItemToSaleCommand request, CancellationToken cancellationToken)
    {
        var validator = new AddItemToSaleValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdForUpdateAsync(request.SaleId, cancellationToken);
        if (sale is null)
            throw new KeyNotFoundException($"Sale with ID {request.SaleId} was not found.");

        sale.AddItem(request.ProductId, request.ProductName, request.Quantity, request.UnitPrice);

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        SaleDomainEventLogger.LogAndClear(_logger, sale);

        return _mapper.Map<SaleResponse>(sale);
    }
}
