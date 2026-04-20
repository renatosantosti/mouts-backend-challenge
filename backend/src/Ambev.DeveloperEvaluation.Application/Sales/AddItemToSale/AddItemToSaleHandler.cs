using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;

public sealed class AddItemToSaleHandler : IRequestHandler<AddItemToSaleCommand, SaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ISaleEventPublisher _eventPublisher;

    public AddItemToSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        ISaleEventPublisher eventPublisher)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<SaleResponse> Handle(AddItemToSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdForUpdateAsync(request.SaleId, cancellationToken);
        if (sale is null)
            throw new KeyNotFoundException($"Sale with ID {request.SaleId} was not found.");

        sale.AddItem(request.ProductId, request.ProductName, request.Quantity, request.UnitPrice);

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        IReadOnlyCollection<IDomainEvent> eventsSnapshot = sale.DomainEvents.ToArray();
        await _eventPublisher.PublishAsync(eventsSnapshot, cancellationToken);
        sale.ClearDomainEvents();

        return _mapper.Map<SaleResponse>(sale);
    }
}
