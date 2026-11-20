using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    public ProductRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Products") { }

    public override async Task<long> AddAsync(Product entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO dbo.Products (Id, Name, BrandName, Season, Price, Stock, IsActive)
            VALUES (@Id, @Name, @BrandName, @Season, @Price, @Stock, @IsActive)",
            entity, Transaction, cancellationToken: ct)));
        return entity.Id;
    }

    public override async Task<bool> UpdateAsync(Product entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(@"
            UPDATE dbo.Products
            SET Name = @Name, BrandName = @BrandName, Season = @Season,
                Price = @Price, Stock = @Stock, IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id",
            entity, Transaction, cancellationToken: ct)));
        return rows > 0;
    }

    public async Task<IReadOnlyList<Product>> GetBySeasonAsync(string season, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Product>(new CommandDefinition(
            "SELECT * FROM dbo.Products WHERE Season = @Season AND IsActive = 1 ORDER BY Name",
            new { Season = season }, Transaction, cancellationToken: ct));
        return rows.ToList();
    }

    // Orders тримає локальну копію Products: Name і Price оновлюються подіями з Каталогу.
    // Умова SourceUpdatedAt <= @SourceUpdatedAt захищає від подій, що прийшли не по порядку:
    // старіша подія не перезапише новіші дані. Знімки в OrderItems (UnitPrice тощо) не чіпаються,
    // бо вони фіксують ціну на момент замовлення.
    public Task<SnapshotUpdateResult> UpdatePriceSnapshotAsync(
        int productId, decimal price, DateTime sourceUpdatedAtUtc, CancellationToken ct = default)
        => UpdateSnapshotAsync(
            productId, sourceUpdatedAtUtc, "Price = @Value", new { Value = price }, ct);

    public Task<SnapshotUpdateResult> UpdateNameSnapshotAsync(
        int productId, string name, DateTime sourceUpdatedAtUtc, CancellationToken ct = default)
        => UpdateSnapshotAsync(
            productId, sourceUpdatedAtUtc, "Name = @Value", new { Value = name }, ct);

    private async Task<SnapshotUpdateResult> UpdateSnapshotAsync(
        int productId, DateTime sourceUpdatedAtUtc, string assignment, object value, CancellationToken ct)
    {
        await EnsureOpenAsync(ct);

        var exists = await Connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM dbo.Products WHERE Id = @Id",
            new { Id = productId }, Transaction, cancellationToken: ct));
        if (exists == 0)
            return SnapshotUpdateResult.NotFound;

        var parameters = new DynamicParameters(value);
        parameters.Add("Id", productId);
        parameters.Add("SourceUpdatedAt", sourceUpdatedAtUtc);

        var rows = await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition($@"
            UPDATE dbo.Products
            SET {assignment}, SourceUpdatedAt = @SourceUpdatedAt, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND (SourceUpdatedAt IS NULL OR SourceUpdatedAt <= @SourceUpdatedAt)",
            parameters, Transaction, cancellationToken: ct)));

        return rows > 0 ? SnapshotUpdateResult.Updated : SnapshotUpdateResult.Stale;
    }
}