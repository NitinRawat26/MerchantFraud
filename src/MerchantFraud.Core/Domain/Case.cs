namespace MerchantFraud.Core.Domain;

public sealed record FraudCase
{
    public FraudCase(
        string id,
        string merchantId,
        string title,
        CasePriority priority = CasePriority.Medium,
        CaseStatus status = CaseStatus.Open,
        string? assignedTo = null,
        DateTimeOffset? openedAt = null,
        DateTimeOffset? closedAt = null,
        string? externalRef = null)
    {
        Id = DomainGuard.Required(id, nameof(id));
        MerchantId = DomainGuard.Required(merchantId, nameof(merchantId));
        Title = DomainGuard.Required(title, nameof(title));
        Priority = priority;
        Status = status;
        AssignedTo = DomainGuard.Optional(assignedTo);
        OpenedAt = openedAt;
        ClosedAt = closedAt;
        ExternalRef = DomainGuard.Optional(externalRef);
    }

    public string Id { get; init; }

    public string MerchantId { get; init; }

    public string Title { get; init; }

    public CasePriority Priority { get; init; }

    public CaseStatus Status { get; init; }

    public string? AssignedTo { get; init; }

    public DateTimeOffset? OpenedAt { get; init; }

    public DateTimeOffset? ClosedAt { get; init; }

    public string? ExternalRef { get; init; }
}
