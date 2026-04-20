using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact]
    public void Create_WithEmptySaleNumber_ShouldThrow()
    {
        var data = SaleTestData.GenerateCreateSaleCommand(
            saleNumber: "  ",
            customerName: "C",
            branchName: "B");
        var act = () => Sale.Create(
            data.SaleNumber,
            data.Date,
            data.CustomerId,
            data.CustomerName,
            data.BranchId,
            data.BranchName);
        act.Should().Throw<DomainException>().WithMessage("*Sale number*");
    }

    [Fact]
    public void Create_ShouldRaiseSaleCreatedEvent_AndHaveInitialState()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.DomainEvents.Should().ContainSingle(e => e is SaleCreatedEvent);
        sale.TotalAmount.Should().Be(0);
        sale.Items.Should().BeEmpty();
        sale.IsCancelled.Should().BeFalse();
    }

    [Theory]
    [InlineData(3, 10)] // no discount below 4
    [InlineData(4, 10)] // 10% tier
    [InlineData(9, 100)]
    [InlineData(10, 100)] // 20% tier
    [InlineData(20, 50)]
    public void AddItem_ShouldApplyDiscountTiers(int qty, decimal unitPrice)
    {
        var (expectedDiscount, expectedLineTotal) = SalePricingExpectations.ExpectedForActiveLine(qty, unitPrice);
        var sale = SaleTestData.CreateValidSale();
        sale.ClearDomainEvents();

        sale.AddItem(Guid.NewGuid(), "Product", qty, unitPrice);

        var item = sale.Items.Single();
        item.Discount.Should().Be(expectedDiscount);
        item.TotalAmount.Should().Be(expectedLineTotal);
        sale.TotalAmount.Should().Be(expectedLineTotal);
        var modified = sale.DomainEvents.OfType<SaleModifiedEvent>().Single();
        modified.TotalAmountAfterChange.Should().Be(sale.TotalAmount);
    }

    [Fact]
    public void AddItem_WhenQuantityLessThanOne_ShouldThrow()
    {
        var sale = SaleTestData.CreateValidSale();
        var act = () => sale.AddItem(Guid.NewGuid(), "P", 0, 1m);
        act.Should().Throw<DomainException>().WithMessage("*Quantity must be at least 1*");
    }

    [Fact]
    public void AddItem_WhenQuantityAbove20_ShouldThrow()
    {
        var sale = SaleTestData.CreateValidSale();
        var act = () => sale.AddItem(Guid.NewGuid(), "P", 21, 1m);
        act.Should().Throw<DomainException>().WithMessage("*20*");
    }

    [Fact]
    public void AddItem_WhenUnitPriceNotPositive_ShouldThrow()
    {
        var sale = SaleTestData.CreateValidSale();
        var act = () => sale.AddItem(Guid.NewGuid(), "P", 1, 0m);
        act.Should().Throw<DomainException>().WithMessage("*Unit price*");
    }

    [Fact]
    public void CancelSale_ShouldBlockFurtherMutations()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.AddItem(Guid.NewGuid(), "P", 1, 10m);
        sale.Cancel();

        sale.Invoking(s => s.AddItem(Guid.NewGuid(), "X", 1, 1m))
            .Should().Throw<DomainException>();
        sale.Invoking(s => s.CancelItem(sale.Items.First().Id))
            .Should().Throw<DomainException>();
        sale.Invoking(s => s.Cancel())
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.Cancel();

        sale.Invoking(s => s.Cancel())
            .Should().Throw<DomainException>().WithMessage("*cancelled sale*");
    }

    [Fact]
    public void CancelSale_ShouldZeroTotalAndRaiseSaleCancelled()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.AddItem(Guid.NewGuid(), "P", 2, 50m);
        sale.ClearDomainEvents();

        sale.Cancel();

        sale.TotalAmount.Should().Be(0);
        sale.IsCancelled.Should().BeTrue();
        sale.DomainEvents.Should().ContainSingle(e => e is SaleCancelledEvent);
    }

    [Fact]
    public void CancelItem_ShouldRemoveContributionFromTotal_AndRaiseItemCancelledOnly()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.AddItem(Guid.NewGuid(), "A", 2, 10m);
        sale.AddItem(Guid.NewGuid(), "B", 2, 5m);
        sale.ClearDomainEvents();

        var itemId = sale.Items.First().Id;
        sale.CancelItem(itemId);

        var (_, expectedRemaining) = SalePricingExpectations.ExpectedForActiveLine(2, 5m);
        sale.TotalAmount.Should().Be(expectedRemaining);
        sale.Items.First(i => i.Id == itemId).IsCancelled.Should().BeTrue();
        sale.DomainEvents.Should().ContainSingle(e => e is ItemCancelledEvent);
        sale.DomainEvents.Should().NotContain(e => e is SaleModifiedEvent);
    }

    // Idempotent by design: a second cancel on the same item does not throw, so ItemCancelled is not duplicated.
    [Fact]
    public void CancelItem_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.AddItem(Guid.NewGuid(), "P", 2, 10m);
        var itemId = sale.Items.First().Id;
        sale.CancelItem(itemId);
        sale.ClearDomainEvents();

        sale.CancelItem(itemId);

        sale.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CancelItem_WhenItemNotFound_ShouldThrow()
    {
        var sale = SaleTestData.CreateValidSale();

        sale.Invoking(s => s.CancelItem(Guid.NewGuid()))
            .Should().Throw<DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public void AddItem_TwoActiveLines_ShouldSumTotalAmount()
    {
        var sale = SaleTestData.CreateValidSale();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();

        sale.AddItem(productA, "A", 2, 10m);
        sale.AddItem(productB, "B", 3, 5m);

        var (_, lineA) = SalePricingExpectations.ExpectedForActiveLine(2, 10m);
        var (_, lineB) = SalePricingExpectations.ExpectedForActiveLine(3, 5m);
        var expectedTotal = lineA + lineB;

        sale.Items.Count(i => !i.IsCancelled).Should().Be(2);
        sale.TotalAmount.Should().Be(expectedTotal);
    }

    [Fact]
    public void MonetaryRounding_ShouldUseAwayFromZero()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.AddItem(Guid.NewGuid(), "P", 4, 0.333m);
        var item = sale.Items.Single();
        var (_, expectedLineTotal) = SalePricingExpectations.ExpectedForActiveLine(4, 0.333m);
        item.TotalAmount.Should().Be(expectedLineTotal);
    }

    [Fact]
    public void AddItem_ShouldMergeQuantity_WhenSameProductIdAndActiveLineExists()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();

        sale.AddItem(productId, "Widget", 3, 10m);
        sale.AddItem(productId, "Widget", 2, 10m);

        var active = sale.Items.Where(i => !i.IsCancelled).ToList();
        active.Should().ContainSingle();
        active[0].Quantity.Should().Be(5);
        active[0].UnitPrice.Should().Be(10m);
        var (expectedDiscount, _) = SalePricingExpectations.ExpectedForActiveLine(5, 10m);
        active[0].Discount.Should().Be(expectedDiscount);
    }

    [Fact]
    public void AddItem_ShouldApplyLastUnitPrice_WhenMergingSameProduct()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();

        sale.AddItem(productId, "Widget", 2, 10m);
        sale.AddItem(productId, "Widget X", 3, 11m);

        var line = sale.Items.Single(i => !i.IsCancelled);
        line.Quantity.Should().Be(5);
        line.UnitPrice.Should().Be(11m);
        line.ProductName.Should().Be("Widget X");
    }

    [Fact]
    public void AddItem_WhenMergedQuantityWouldExceed20_ShouldThrow()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "P", 15, 1m);

        var act = () => sale.AddItem(productId, "P", 6, 1m);

        act.Should().Throw<DomainException>().WithMessage("*20*");
        sale.Items.Single(i => !i.IsCancelled).Quantity.Should().Be(15);
    }

    [Fact]
    public void AddItem_AfterLineCancelled_ShouldCreateNewActiveLine()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "P", 2, 10m);
        var firstId = sale.Items.First().Id;
        sale.CancelItem(firstId);

        sale.AddItem(productId, "P", 3, 10m);

        sale.Items.Should().HaveCount(2);
        sale.Items.Single(i => i.Id == firstId).IsCancelled.Should().BeTrue();
        var active = sale.Items.Single(i => !i.IsCancelled);
        active.Quantity.Should().Be(3);
        active.Id.Should().NotBe(firstId);
    }

    [Fact]
    public void AddItem_WhenUnitPriceZero_ShouldThrow_BeforeMerge()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "P", 1, 10m);

        var act = () => sale.AddItem(productId, "P", 1, 0m);

        act.Should().Throw<DomainException>().WithMessage("*Unit price*");
        sale.Items.Single(i => !i.IsCancelled).Quantity.Should().Be(1);
    }
}
