namespace MerchantFraud.Persistence;

public sealed record FraudAuditEvent(
    string Id,
    DateTimeOffset OccurredAt,
    string Actor,
    string Action,
    string EntityType,
    string EntityId,
    string? Details);
