using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ISaleEventPublisher _eventPublisher = Substitute.For<ISaleEventPublisher>();
    private readonly ISaleEventHistoryRecorder _historyRecorder = Substitute.For<ISaleEventHistoryRecorder>();
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _handler = new CreateSaleHandler(_saleRepository, _mapper, _eventPublisher, _historyRecorder);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAndReturnsMappedSale()
    {
        var command = SaleTestData.GenerateCreateSaleCommand(saleNumber: "SALE-UNIT-1");
        Sale? persistedSale = null;

        var expected = new SaleResponse { Id = Guid.NewGuid(), SaleNumber = command.SaleNumber };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                persistedSale = ci.Arg<Sale>();
                return Task.FromResult(ci.Arg<Sale>());
            });

        _mapper.Map<SaleResponse>(Arg.Any<Sale>()).Returns(expected);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _mapper.Received(1).Map<SaleResponse>(Arg.Any<Sale>());
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events => events.Count > 0),
            Arg.Any<CancellationToken>());
        await _historyRecorder.Received(1).RecordAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events => events.Count > 0),
            Arg.Any<CancellationToken>());
        persistedSale.Should().NotBeNull();
        persistedSale!.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InvalidCommand_ThrowsDomainException()
    {
        var command = SaleTestData.GenerateCreateSaleCommand(
            saleNumber: string.Empty,
            customerId: Guid.Empty,
            customerName: string.Empty,
            branchId: Guid.Empty,
            branchName: string.Empty);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
