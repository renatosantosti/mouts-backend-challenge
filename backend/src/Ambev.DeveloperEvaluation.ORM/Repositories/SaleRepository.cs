using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    [Obsolete("Use sale cancellation in domain/application flows. Physical delete is legacy compatibility.")]
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Legacy compatibility path: business flow for Sales should prefer Cancel() at aggregate level.
        var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale is null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        SaleListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.Size, 1, 100);

        var baseQuery = _context.Sales
            .AsNoTracking()
            .AsQueryable();

        if (query.Filters is not null
            && query.Filters.TryGetValue("customerId", out var customerIdRaw)
            && Guid.TryParse(customerIdRaw, out var customerId))
            baseQuery = baseQuery.Where(s => s.CustomerId == customerId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = ApplyOrdering(baseQuery, query.Order);

        var pageItems = await ordered
            .Skip((page - 1) * size)
            .Take(size)
            .Include(s => s.Items)
            .ToListAsync(cancellationToken);

        return (pageItems, totalCount);
    }

    private static IQueryable<Sale> ApplyOrdering(IQueryable<Sale> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderByDescending(s => s.Date);

        var o = order.Trim();
        if (o.Equals("date", StringComparison.OrdinalIgnoreCase)
            || o.Equals("date_asc", StringComparison.OrdinalIgnoreCase))
            return query.OrderBy(s => s.Date);

        if (o.Equals("date_desc", StringComparison.OrdinalIgnoreCase))
            return query.OrderByDescending(s => s.Date);

        return query.OrderByDescending(s => s.Date);
    }
}
