namespace MerchantFraud.Network;

public enum NetworkLinkType
{
    SharedDevice,
    SharedIpAddress,
    SharedCustomer,
    SharedCardFingerprint
}

public sealed record MerchantNetworkLink(
    string MerchantAId,
    string MerchantBId,
    NetworkLinkType LinkType,
    string SharedValue,
    int SharedTransactionCount);

public sealed record MerchantNetworkScore(
    string MerchantId,
    int LinkedMerchantCount,
    int LinkCount,
    int RiskScore,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<MerchantNetworkLink> Links);

public sealed record MerchantNetworkAnalysis(
    IReadOnlyList<MerchantNetworkLink> Links,
    IReadOnlyList<MerchantNetworkScore> Scores);
