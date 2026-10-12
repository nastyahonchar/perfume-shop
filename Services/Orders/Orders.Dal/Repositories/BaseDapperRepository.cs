using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Errors;
using Orders.Dal.Interfaces;

namespace Orders.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected string TableName { get; }

    protected BaseDapperRepository(
        SqlConnection connection, Func<SqlTransaction?> currentTransaction, string tableName)
        : base(connection, currentTransaction)
    {
        TableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(
            $"SELECT * FROM {TableName} WHERE Id = @Id",
            new { Id = id }, Transaction, cancellationToken: ct));
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<T>(new CommandDefinition(
            $"SELECT * FROM {TableName}", transaction: Transaction, cancellationToken: ct));
        return rows.ToList();
    }

    public virtual async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(
            $"DELETE FROM {TableName} WHERE Id = @Id",
            new { Id = id }, Transaction, cancellationToken: ct)));
        return rows > 0;
    }

    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct = default);

    protected static async Task<TResult> TranslateAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex) when (SqlErrorTranslator.Translate(ex) is not null)
        {
            throw SqlErrorTranslator.Translate(ex)!;
        }
    }
}