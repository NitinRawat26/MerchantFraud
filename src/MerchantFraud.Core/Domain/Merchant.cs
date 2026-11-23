namespace MerchantFraud.Core.Domain;

public sealed record Merchant
{
    public Merchant(
        string id,
        string legalName,
        string? tradingName = null,
        string? country = null,
        string? websiteUrl = null,
        MerchantLifecycle lifecycle = MerchantLifecycle.Active)
    {
        Id = DomainGuard.Required(id, nameof(id));
        LegalName = DomainGuard.Required(legalName, nameof(legalName));
        TradingName = DomainGuard.Optional(tradingName);
        Country = DomainGuard.Optional(country)?.ToUpperInvariant();
        WebsiteUrl = DomainGuard.Optional(websiteUrl);
        Lifecycle = lifecycle;
    }

    public string Id { get; init; }

    public string LegalName { get; init; }

    public string? TradingName { get; init; }

    public string? Country { get; init; }

    public string? WebsiteUrl { get; init; }

    public MerchantLifecycle Lifecycle { get; init; }
}
