namespace Orders.Dal.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> TryMarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}