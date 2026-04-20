using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ISaleEventPublisher _eventPublisher;
    private readonly ISaleEventHistoryRecorder _historyRecorder;

    public CreateSaleHandler(
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

        IReadOnlyCollection<IDomainEvent> eventsSnapshot = sale.DomainEvents.ToArray();
        await _eventPublisher.PublishAsync(eventsSnapshot, cancellationToken);
        await _historyRecorder.RecordAsync(eventsSnapshot, cancellationToken);
        sale.ClearDomainEvents();

        return _mapper.Map<SaleResponse>(sale);
    }
}
