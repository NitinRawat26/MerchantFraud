namespace MerchantFraud.Core.Domain;

public sealed record FraudOwner
{
    public FraudOwner(
        string id,
        string fullName,
        double? ownershipPercent = null,
        string? role = null,
        DateOnly? dateOfBirth = null,
        string? nationality = null,
        string? address = null)
    {
        if (ownershipPercent.HasValue && (ownershipPercent.Value < 0 || ownershipPercent.Value > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipPercent), ownershipPercent, "Ownership percent must be between 0 and 100.");
        }

        Id = DomainGuard.Required(id, nameof(id));
        FullName = DomainGuard.Required(fullName, nameof(fullName));
        OwnershipPercent = ownershipPercent;
        Role = DomainGuard.Optional(role);
        DateOfBirth = dateOfBirth;
        Nationality = DomainGuard.Optional(nationality)?.ToUpperInvariant();
        Address = DomainGuard.Optional(address);
    }

    public string Id { get; init; }

    public string FullName { get; init; }

    public double? OwnershipPercent { get; init; }

    public string? Role { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public string? Nationality { get; init; }

    public string? Address { get; init; }
}
