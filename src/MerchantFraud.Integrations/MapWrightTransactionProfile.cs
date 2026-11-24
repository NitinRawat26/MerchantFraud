namespace MerchantFraud.Integrations;

public sealed record MapWrightTransactionProfile(
    string Name,
    IReadOnlyDictionary<string, string> FieldMappings,
    IReadOnlyDictionary<string, string>? Constants = null);
