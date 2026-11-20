namespace Reviews.Domain.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default);

    Task<bool> TryMarkAsync(Guid eventId, string eventType, CancellationToken ct = default);
}