namespace MerchantFraud.Core.Domain;

public sealed record FraudApplication
{
    public FraudApplication(
        string id,
        string merchantId,
        int merchantCategoryCode,
        Money annualVolume,
        Money averageTicket,
        Money highestTicket,
        string? businessDescription = null,
        int? deliveryDays = null,
        double cardNotPresentShare = 1.0,
        bool offersSubscriptions = false,
        bool offersFreeTrials = false,
        string? externalRef = null)
    {
        ArgumentNullException.ThrowIfNull(annualVolume);
        ArgumentNullException.ThrowIfNull(averageTicket);
        ArgumentNullException.ThrowIfNull(highestTicket);

        Id = DomainGuard.Required(id, nameof(id));
        MerchantId = DomainGuard.Required(merchantId, nameof(merchantId));

        if (merchantCategoryCode is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(merchantCategoryCode), merchantCategoryCode, "MCC must be between 1 and 9999.");
        }

        if (deliveryDays.HasValue && deliveryDays.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryDays), deliveryDays, "Delivery days cannot be negative.");
        }

        if (cardNotPresentShare is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cardNotPresentShare), cardNotPresentShare, "Card-not-present share must be between 0 and 1.");
        }

        if (annualVolume.Currency != averageTicket.Currency || annualVolume.Currency != highestTicket.Currency)
        {
            throw new ArgumentException("Volume and ticket amounts must use the same currency.", nameof(annualVolume));
        }

        MerchantCategoryCode = merchantCategoryCode;
        AnnualVolume = annualVolume;
        AverageTicket = averageTicket;
        HighestTicket = highestTicket;
        BusinessDescription = DomainGuard.Optional(businessDescription);
        DeliveryDays = deliveryDays;
        CardNotPresentShare = cardNotPresentShare;
        OffersSubscriptions = offersSubscriptions;
        OffersFreeTrials = offersFreeTrials;
        ExternalRef = DomainGuard.Optional(externalRef);
    }

    public string Id { get; init; }

    public string MerchantId { get; init; }

    public int MerchantCategoryCode { get; init; }

    public Money AnnualVolume { get; init; }

    public Money AverageTicket { get; init; }

    public Money HighestTicket { get; init; }

    public string? BusinessDescription { get; init; }

    public int? DeliveryDays { get; init; }

    public double CardNotPresentShare { get; init; }

    public bool OffersSubscriptions { get; init; }

    public bool OffersFreeTrials { get; init; }

    public string? ExternalRef { get; init; }
}
