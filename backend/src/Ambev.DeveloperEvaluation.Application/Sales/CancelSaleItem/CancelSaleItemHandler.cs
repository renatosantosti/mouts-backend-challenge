using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public sealed class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, SaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ISaleEventPublisher _eventPublisher;
    private readonly ISaleEventHistoryRecorder _historyRecorder;

    public CancelSaleItemHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        ISaleEventPublisher eventPublisher,
        ISaleEventHistoryRecorder historyRecorder)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _historyRecorder = historyRecorder;
    }

    public async Task<SaleResponse> Handle(CancelSaleItemCommand request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdForUpdateAsync(request.SaleId, cancellationToken);
        if (sale is null)
            throw new KeyNotFoundException($"Sale with ID {request.SaleId} was not found.");

        try
        {
            sale.CancelItem(request.ItemId);
        }
        catch (DomainException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException(
                $"Sale item with ID {request.ItemId} was not found in sale {request.SaleId}.",
                ex);
        }

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        IReadOnlyCollection<IDomainEvent> eventsSnapshot = sale.DomainEvents.ToArray();
        await _eventPublisher.PublishAsync(eventsSnapshot, cancellationToken);
        await _historyRecorder.RecordAsync(eventsSnapshot, cancellationToken);
        sale.ClearDomainEvents();

        return _mapper.Map<SaleResponse>(sale);
    }
}
