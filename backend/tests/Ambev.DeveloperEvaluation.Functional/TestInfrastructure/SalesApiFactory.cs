using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ambev.DeveloperEvaluation.Functional.TestInfrastructure;

public sealed class SalesApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DefaultContext>>();
            services.RemoveAll<MongoSaleEventHistoryRepository>();
            services.RemoveAll<ISaleEventHistoryWriter>();
            services.RemoveAll<ISaleEventHistoryReader>();
            services.RemoveAll<ISaleEventHistoryRecorder>();

            services.AddDbContext<DefaultContext>(options =>
            {
                options.UseSqlite(_connection);
            });
            services.AddSingleton<InMemorySaleHistoryStore>();
            services.AddScoped<ISaleEventHistoryWriter>(provider => provider.GetRequiredService<InMemorySaleHistoryStore>());
            services.AddScoped<ISaleEventHistoryReader>(provider => provider.GetRequiredService<InMemorySaleHistoryStore>());
            services.AddScoped<ISaleEventHistoryRecorder>(provider => provider.GetRequiredService<InMemorySaleHistoryStore>());

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            _connection.Open();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private sealed class InMemorySaleHistoryStore : ISaleEventHistoryWriter, ISaleEventHistoryReader, ISaleEventHistoryRecorder
    {
        private readonly List<SaleHistoryEvent> _events = [];
        private readonly object _lock = new();

        public Task AppendAsync(IReadOnlyCollection<SaleHistoryEvent> events, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _events.AddRange(events);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SaleHistoryEvent>> ListBySaleIdAsync(Guid saleId, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var result = _events
                    .Where(x => x.SaleId == saleId)
                    .OrderByDescending(x => x.OccurredOn)
                    .ToArray();
                return Task.FromResult<IReadOnlyList<SaleHistoryEvent>>(result);
            }
        }

        public Task RecordAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            var historyEvents = domainEvents.Select(MapEvent).ToArray();
            return AppendAsync(historyEvents, cancellationToken);
        }

        private static SaleHistoryEvent MapEvent(IDomainEvent domainEvent)
        {
            return domainEvent switch
            {
                SaleCreatedEvent e => new SaleHistoryEvent
                {
                    SaleId = e.SaleId,
                    EventType = nameof(SaleCreatedEvent),
                    OccurredOn = e.OccurredOn,
                    SaleNumber = e.SaleNumber
                },
                SaleModifiedEvent e => new SaleHistoryEvent
                {
                    SaleId = e.SaleId,
                    EventType = nameof(SaleModifiedEvent),
                    OccurredOn = e.OccurredOn,
                    TotalAmount = e.TotalAmountAfterChange
                },
                SaleCancelledEvent e => new SaleHistoryEvent
                {
                    SaleId = e.SaleId,
                    EventType = nameof(SaleCancelledEvent),
                    OccurredOn = e.OccurredOn
                },
                ItemCancelledEvent e => new SaleHistoryEvent
                {
                    SaleId = e.SaleId,
                    EventType = nameof(ItemCancelledEvent),
                    OccurredOn = e.OccurredOn,
                    SaleItemId = e.SaleItemId
                },
                _ => new SaleHistoryEvent
                {
                    EventType = domainEvent.GetType().Name,
                    OccurredOn = DateTime.UtcNow
                }
            };
        }
    }
}
