using MerchantFraud.Core.Domain;

namespace MerchantFraud.Network;

public static class MerchantNetworkAnalyzer
{
    public static MerchantNetworkAnalysis Analyze(IEnumerable<Merchant> merchants, IEnumerable<FraudTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        ArgumentNullException.ThrowIfNull(transactions);

        var transactionList = transactions.ToList();
        var occurrences = new Dictionary<string, List<NetworkOccurrence>>(StringComparer.Ordinal);

        foreach (var transaction in transactionList)
        {
            AddOccurrence(occurrences, NetworkLinkType.SharedDevice, transaction.DeviceId, transaction);
            AddOccurrence(occurrences, NetworkLinkType.SharedIpAddress, transaction.IpAddress, transaction);
            AddOccurrence(occurrences, NetworkLinkType.SharedCustomer, transaction.CustomerId, transaction);
            AddOccurrence(occurrences, NetworkLinkType.SharedCardFingerprint, CardFingerprint(transaction), transaction);
        }

        var links = new Dictionary<string, MerchantNetworkLink>(StringComparer.Ordinal);
        foreach (var occurrenceGroup in occurrences.Values)
        {
            if (occurrenceGroup.Count == 0)
            {
                continue;
            }

            var linkType = occurrenceGroup[0].LinkType;
            var sharedValue = occurrenceGroup[0].SharedValue;
            var byMerchant = occurrenceGroup
                .GroupBy(occurrence => occurrence.MerchantId, StringComparer.Ordinal)
                .Select(group => new { MerchantId = group.Key, Count = group.Select(occurrence => occurrence.TransactionId).Distinct(StringComparer.Ordinal).Count() })
                .Where(group => group.Count > 0)
                .OrderBy(group => group.MerchantId, StringComparer.Ordinal)
                .ToArray();

            if (byMerchant.Length < 2)
            {
                continue;
            }

            for (var left = 0; left < byMerchant.Length; left++)
            {
                for (var right = left + 1; right < byMerchant.Length; right++)
                {
                    var merchantA = byMerchant[left].MerchantId;
                    var merchantB = byMerchant[right].MerchantId;
                    var key = $"{merchantA}\u001f{merchantB}\u001f{linkType}\u001f{sharedValue}";
                    if (!links.ContainsKey(key))
                    {
                        links[key] = new MerchantNetworkLink(
                            merchantA,
                            merchantB,
                            linkType,
                            sharedValue,
                            Math.Min(byMerchant[left].Count, byMerchant[right].Count));
                    }
                }
            }
        }

        var orderedLinks = links.Values
            .OrderBy(link => link.MerchantAId, StringComparer.Ordinal)
            .ThenBy(link => link.MerchantBId, StringComparer.Ordinal)
            .ThenBy(link => link.LinkType)
            .ThenBy(link => link.SharedValue, StringComparer.Ordinal)
            .ToArray();

        return new MerchantNetworkAnalysis(orderedLinks, BuildScores(orderedLinks));
    }

    private static IReadOnlyList<MerchantNetworkScore> BuildScores(IReadOnlyList<MerchantNetworkLink> links)
    {
        var scores = new Dictionary<string, MerchantScoreBuilder>(StringComparer.Ordinal);

        foreach (var link in links)
        {
            AddScore(scores, link.MerchantAId, link.MerchantBId, link);
            AddScore(scores, link.MerchantBId, link.MerchantAId, link);
        }

        return scores.Values
            .Select(builder => new MerchantNetworkScore(
                builder.MerchantId,
                builder.LinkedMerchantIds.Count,
                builder.Links.Count,
                Math.Min(1000, builder.RiskScore + builder.LinkedMerchantIds.Count * 25),
                builder.ReasonCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray(),
                builder.Links.OrderBy(link => link.MerchantAId, StringComparer.Ordinal)
                    .ThenBy(link => link.MerchantBId, StringComparer.Ordinal)
                    .ThenBy(link => link.LinkType)
                    .ThenBy(link => link.SharedValue, StringComparer.Ordinal)
                    .ToArray()))
            .OrderByDescending(score => score.RiskScore)
            .ThenBy(score => score.MerchantId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddScore(Dictionary<string, MerchantScoreBuilder> scores, string merchantId, string linkedMerchantId, MerchantNetworkLink link)
    {
        if (!scores.TryGetValue(merchantId, out var builder))
        {
            builder = new MerchantScoreBuilder(merchantId);
            scores[merchantId] = builder;
        }

        builder.LinkedMerchantIds.Add(linkedMerchantId);
        builder.Links.Add(link);
        builder.RiskScore += Weight(link.LinkType) * 10 + link.SharedTransactionCount * 2;
        builder.ReasonCodes.Add(ReasonCodeFor(link.LinkType));
        if (builder.LinkedMerchantIds.Count >= 3)
        {
            builder.ReasonCodes.Add("NETWORK_RING_SIZE");
        }
    }

    private static void AddOccurrence(
        Dictionary<string, List<NetworkOccurrence>> occurrences,
        NetworkLinkType linkType,
        string? value,
        FraudTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();
        var key = $"{linkType}:{normalized}";
        if (!occurrences.TryGetValue(key, out var list))
        {
            list = new List<NetworkOccurrence>();
            occurrences[key] = list;
        }

        list.Add(new NetworkOccurrence(linkType, normalized, transaction.MerchantId, transaction.Id));
    }

    private static string? CardFingerprint(FraudTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(transaction.CardBin) || string.IsNullOrWhiteSpace(transaction.CardLast4))
        {
            return null;
        }

        return $"{transaction.CardBrand ?? "unknown"}:{transaction.CardBin}:{transaction.CardLast4}";
    }

    private static int Weight(NetworkLinkType linkType) => linkType switch
    {
        NetworkLinkType.SharedDevice => 5,
        NetworkLinkType.SharedCustomer => 4,
        NetworkLinkType.SharedCardFingerprint => 3,
        NetworkLinkType.SharedIpAddress => 2,
        _ => 1
    };

    private static string ReasonCodeFor(NetworkLinkType linkType) => linkType switch
    {
        NetworkLinkType.SharedDevice => "NETWORK_SHARED_DEVICE",
        NetworkLinkType.SharedIpAddress => "NETWORK_SHARED_IP",
        NetworkLinkType.SharedCustomer => "NETWORK_SHARED_CUSTOMER",
        NetworkLinkType.SharedCardFingerprint => "NETWORK_SHARED_CARD",
        _ => "NETWORK_SHARED_IDENTIFIER"
    };

    private sealed record NetworkOccurrence(NetworkLinkType LinkType, string SharedValue, string MerchantId, string TransactionId);

    private sealed class MerchantScoreBuilder
    {
        public MerchantScoreBuilder(string merchantId)
        {
            MerchantId = merchantId;
        }

        public string MerchantId { get; }

        public HashSet<string> LinkedMerchantIds { get; } = new(StringComparer.Ordinal);

        public List<MerchantNetworkLink> Links { get; } = new();

        public HashSet<string> ReasonCodes { get; } = new(StringComparer.Ordinal);

        public int RiskScore { get; set; }
    }
}
