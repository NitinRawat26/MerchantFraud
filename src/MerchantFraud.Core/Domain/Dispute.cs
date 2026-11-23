namespace MerchantFraud.Core.Domain;

public sealed record FraudDispute
{
    public FraudDispute(
        string id,
        string transactionId,
        string merchantId,
        DateTimeOffset openedAt,
        Money amount,
        string reason,
        DisputeStatus status = DisputeStatus.Open,
        DateTimeOffset? closedAt = null,
        string? externalRef = null)
    {
        ArgumentNullException.ThrowIfNull(amount);

        Id = DomainGuard.Required(id, nameof(id));
        TransactionId = DomainGuard.Required(transactionId, nameof(transactionId));
        MerchantId = DomainGuard.Required(merchantId, nameof(merchantId));
        OpenedAt = openedAt;
        Amount = amount;
        Reason = DomainGuard.Required(reason, nameof(reason));
        Status = status;
        ClosedAt = closedAt;
        ExternalRef = DomainGuard.Optional(externalRef);
    }

    public string Id { get; init; }

    public string TransactionId { get; init; }

    public string MerchantId { get; init; }

    public DateTimeOffset OpenedAt { get; init; }

    public Money Amount { get; init; }

    public string Reason { get; init; }

    public DisputeStatus Status { get; init; }

    public DateTimeOffset? ClosedAt { get; init; }

    public string? ExternalRef { get; init; }
}
