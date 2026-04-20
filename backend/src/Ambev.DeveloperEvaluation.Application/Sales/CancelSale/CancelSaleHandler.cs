using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public sealed class CancelSaleHandler : IRequestHandler<CancelSaleCommand, SaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ISaleEventPublisher _eventPublisher;
    private readonly ISaleEventHistoryRecorder _historyRecorder;

    public CancelSaleHandler(
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

    public async Task<SaleResponse> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdForUpdateAsync(request.SaleId, cancellationToken);
        if (sale is null)
            throw new KeyNotFoundException($"Sale with ID {request.SaleId} was not found.");

        sale.Cancel();

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        IReadOnlyCollection<IDomainEvent> eventsSnapshot = sale.DomainEvents.ToArray();
        await _eventPublisher.PublishAsync(eventsSnapshot, cancellationToken);
        await _historyRecorder.RecordAsync(eventsSnapshot, cancellationToken);
        sale.ClearDomainEvents();

        return _mapper.Map<SaleResponse>(sale);
    }
}
