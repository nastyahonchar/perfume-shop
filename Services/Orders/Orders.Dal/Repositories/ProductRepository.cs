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
}