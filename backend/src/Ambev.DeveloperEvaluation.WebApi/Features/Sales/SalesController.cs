using Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSaleById;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.AddItem;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// Controller for managing sales operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SalesController : ControllerBase
{
    private static readonly HashSet<string> ReservedQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "_page",
        "_size",
        "_order"
    };

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of <see cref="SalesController"/>.
    /// </summary>
    /// <param name="mediator">Mediator used to dispatch sales commands and queries.</param>
    /// <param name="mapper">Mapper used to transform API contracts into application contracts.</param>
    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new sale header.
    /// </summary>
    /// <param name="request">Sale creation payload containing header fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sale representation.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateSaleCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);
        var payload = _mapper.Map<SaleResponse>(response);

        return CreatedAtAction(nameof(GetSaleById), new { id = payload.Id }, payload);
    }

    /// <summary>
    /// Retrieves a sale by its unique identifier.
    /// </summary>
    /// <param name="id">Sale identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested sale if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSaleById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetSaleByIdQuery(id), cancellationToken);
        return Ok(_mapper.Map<SaleResponse>(response));
    }

    /// <summary>
    /// Lists sales with pagination, ordering, and optional filters.
    /// </summary>
    /// <param name="page">Page number (`_page`) starting at 1.</param>
    /// <param name="size">Page size (`_size`) between 1 and 100.</param>
    /// <param name="order">Ordering expression (`_order`), e.g. `date_desc`.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of sales.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ListSalesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListSales(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var filters = Request.Query
            .Where(kvp => !ReservedQueryKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var query = new ListSalesQuery(
            Page: page,
            Size: size,
            Order: order,
            Filters: filters.Count == 0 ? null : filters);

        var result = await _mediator.Send(query, cancellationToken);

        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)result.Size);

        return Ok(new ListSalesResponse
        {
            Data = _mapper.Map<IReadOnlyList<SaleResponse>>(result.Items),
            TotalItems = result.TotalCount,
            CurrentPage = result.Page,
            TotalPages = totalPages
        });
    }

    /// <summary>
    /// Adds an item to an existing sale.
    /// </summary>
    /// <param name="saleId">Sale identifier.</param>
    /// <param name="request">Payload for the sale item to be added.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sale after applying item rules and totals.</returns>
    [HttpPost("{saleId:guid}/items")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddItem(
        [FromRoute] Guid saleId,
        [FromBody] AddSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddItemToSaleCommand(
            saleId,
            request.ProductId,
            request.ProductName,
            request.Quantity,
            request.UnitPrice);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(_mapper.Map<SaleResponse>(result));
    }

    /// <summary>
    /// Cancels a sale.
    /// </summary>
    /// <param name="saleId">Sale identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled sale with recalculated totals.</returns>
    [HttpPost("{saleId:guid}/cancel")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid saleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleCommand(saleId), cancellationToken);
        return Ok(_mapper.Map<SaleResponse>(result));
    }

    /// <summary>
    /// Cancels a specific item in a sale.
    /// </summary>
    /// <param name="saleId">Sale identifier.</param>
    /// <param name="itemId">Sale item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sale after item cancellation.</returns>
    [HttpPost("{saleId:guid}/items/{itemId:guid}/cancel")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelSaleItem(
        [FromRoute] Guid saleId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleItemCommand(saleId, itemId), cancellationToken);
        return Ok(_mapper.Map<SaleResponse>(result));
    }
}
