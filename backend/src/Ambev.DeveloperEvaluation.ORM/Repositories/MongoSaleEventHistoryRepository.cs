using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public sealed class MongoSaleEventHistoryRepository : ISaleEventHistoryWriter, ISaleEventHistoryReader
{
    private readonly IMongoCollection<SaleHistoryEventDocument> _collection;
    private static readonly object IndexLock = new();
    private static bool _indexesCreated;

    public MongoSaleEventHistoryRepository(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("Mongo connection string is required.");

        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<SaleHistoryEventDocument>(settings.SalesHistoryCollectionName);

        EnsureIndexes(_collection);
    }

    public async Task AppendAsync(IReadOnlyCollection<SaleHistoryEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
            return;

        var docs = events.Select(x => new SaleHistoryEventDocument
        {
            SaleId = x.SaleId.ToString(),
            EventType = x.EventType,
            OccurredOn = x.OccurredOn,
            SaleNumber = x.SaleNumber,
            TotalAmount = x.TotalAmount,
            SaleItemId = x.SaleItemId?.ToString()
        });

        await _collection.InsertManyAsync(docs, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SaleHistoryEvent>> ListBySaleIdAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var docs = await _collection
            .Find(x => x.SaleId == saleId.ToString())
            .SortByDescending(x => x.OccurredOn)
            .ToListAsync(cancellationToken);

        return docs.Select(x => new SaleHistoryEvent
        {
            SaleId = Guid.TryParse(x.SaleId, out var parsedSaleId) ? parsedSaleId : Guid.Empty,
            EventType = x.EventType,
            OccurredOn = x.OccurredOn,
            SaleNumber = x.SaleNumber,
            TotalAmount = x.TotalAmount,
            SaleItemId = Guid.TryParse(x.SaleItemId, out var parsedSaleItemId) ? parsedSaleItemId : null
        }).ToArray();
    }

    private static void EnsureIndexes(IMongoCollection<SaleHistoryEventDocument> collection)
    {
        if (_indexesCreated)
            return;

        lock (IndexLock)
        {
            if (_indexesCreated)
                return;

            var saleAndDate = Builders<SaleHistoryEventDocument>.IndexKeys
                .Ascending(x => x.SaleId)
                .Descending(x => x.OccurredOn);
            collection.Indexes.CreateOne(new CreateIndexModel<SaleHistoryEventDocument>(saleAndDate));

            var eventType = Builders<SaleHistoryEventDocument>.IndexKeys
                .Ascending(x => x.EventType);
            collection.Indexes.CreateOne(new CreateIndexModel<SaleHistoryEventDocument>(eventType));

            _indexesCreated = true;
        }
    }

    private sealed class SaleHistoryEventDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; init; } = string.Empty;

        public string SaleId { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public DateTime OccurredOn { get; init; }
        public string? SaleNumber { get; init; }
        public decimal? TotalAmount { get; init; }
        public string? SaleItemId { get; init; }
    }
}
