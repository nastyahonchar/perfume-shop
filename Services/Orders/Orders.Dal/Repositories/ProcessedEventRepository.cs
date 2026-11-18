using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;

namespace Orders.Dal.Repositories;

public sealed class ProcessedEventRepository : RepositoryBase, IProcessedEventRepository
{
    public ProcessedEventRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction) { }

    public async Task<bool> TryMarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO dbo.ProcessedEvents (EventId, EventType)
            SELECT @EventId, @EventType
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.ProcessedEvents WITH (UPDLOCK, HOLDLOCK) WHERE EventId = @EventId)",
            new { EventId = eventId, EventType = eventType },
            Transaction, cancellationToken: ct));
        return rows > 0;
    }
}