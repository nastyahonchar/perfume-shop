namespace Platform.EventBus.Events;

public sealed record ProductPriceChangedEvent(
    int ProductId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice) : IntegrationEvent;

public sealed record ProductRenamedEvent(
    int ProductId,
    string OldName,
    string NewName) : IntegrationEvent;