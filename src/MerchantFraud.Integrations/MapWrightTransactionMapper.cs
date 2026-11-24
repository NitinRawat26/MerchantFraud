namespace MerchantFraud.Integrations;

public static class MapWrightTransactionMapper
{
    public static IReadOnlyDictionary<string, string?> Map(
        IReadOnlyDictionary<string, string?> partnerFields,
        MapWrightTransactionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(partnerFields);
        ArgumentNullException.ThrowIfNull(profile);

        var lookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in partnerFields)
        {
            lookup[pair.Key] = pair.Value;
        }

        var mapped = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (profile.Constants is not null)
        {
            foreach (var constant in profile.Constants)
            {
                if (!string.IsNullOrWhiteSpace(constant.Value))
                {
                    mapped[constant.Key] = constant.Value.Trim();
                }
            }
        }

        foreach (var mapping in profile.FieldMappings)
        {
            if (lookup.TryGetValue(mapping.Value, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                mapped[mapping.Key] = value.Trim();
            }
        }

        return mapped;
    }
}
