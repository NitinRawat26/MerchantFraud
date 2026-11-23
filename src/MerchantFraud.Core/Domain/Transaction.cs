namespace MerchantFraud.Core.Domain;

public sealed record FraudTransaction
{
    public FraudTransaction(
        string id,
        string merchantId,
        DateTimeOffset occurredAt,
        Money amount,
        TransactionChannel channel = TransactionChannel.Unknown,
        TransactionStatus status = TransactionStatus.Authorized,
        string? cardBrand = null,
        string? cardBin = null,
        string? cardLast4 = null,
        string? customerId = null,
        string? deviceId = null,
        string? ipAddress = null,
        string? externalRef = null)
    {
        ArgumentNullException.ThrowIfNull(amount);

        Id = DomainGuard.Required(id, nameof(id));
        MerchantId = DomainGuard.Required(merchantId, nameof(merchantId));
        OccurredAt = occurredAt;
        Amount = amount;
        Channel = channel;
        Status = status;
        CardBrand = DomainGuard.Optional(cardBrand);
        CardBin = DomainGuard.Optional(cardBin);
        CardLast4 = DomainGuard.Optional(cardLast4);
        CustomerId = DomainGuard.Optional(customerId);
        DeviceId = DomainGuard.Optional(deviceId);
        IpAddress = DomainGuard.Optional(ipAddress);
        ExternalRef = DomainGuard.Optional(externalRef);

        if (CardBin is not null && (CardBin.Length is < 6 or > 8 || !CardBin.All(char.IsDigit)))
        {
            throw new ArgumentException("Card BIN must be 6-8 digits.", nameof(cardBin));
        }

        if (CardLast4 is not null && (CardLast4.Length != 4 || !CardLast4.All(char.IsDigit)))
        {
            throw new ArgumentException("Card last4 must be exactly 4 digits.", nameof(cardLast4));
        }
    }

    public string Id { get; init; }

    public string MerchantId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public Money Amount { get; init; }

    public TransactionChannel Channel { get; init; }

    public TransactionStatus Status { get; init; }

    public string? CardBrand { get; init; }

    public string? CardBin { get; init; }

    public string? CardLast4 { get; init; }

    public string? CustomerId { get; init; }

    public string? DeviceId { get; init; }

    public string? IpAddress { get; init; }

    public string? ExternalRef { get; init; }
}
