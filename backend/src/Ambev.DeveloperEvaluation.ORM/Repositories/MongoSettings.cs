namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public sealed class MongoSettings
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "DeveloperEvaluation";
    public string SalesHistoryCollectionName { get; init; } = "sales_history";
}
