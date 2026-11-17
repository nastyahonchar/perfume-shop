namespace Platform.EventBus.Events;

// Інтеграційна подія незмінна: це record з init-only властивостями, після створення її не можна змінити.
// EventId унікально ідентифікує подію (на ньому тримається ідемпотентність споживачів),
// CorrelationId зв'язує подію з вихідним HTTP-запитом, OccurredAt фіксує момент зміни в джерелі.
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
}