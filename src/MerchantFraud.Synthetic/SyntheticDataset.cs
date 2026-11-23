using MerchantFraud.Core.Domain;

namespace MerchantFraud.Synthetic;

public enum SyntheticFraudType
{
    None,
    TransactionFraud,
    ChargebackAbuse,
    ApplicationFraud,
    NetworkRing
}

public sealed record SyntheticFraudLabel(SyntheticFraudType FraudType, string Rationale)
{
    public bool IsFraudulent => FraudType != SyntheticFraudType.None;
}

public sealed record SyntheticMerchantScenario(
    Merchant Merchant,
    IReadOnlyList<FraudTransaction> Transactions,
    IReadOnlyList<FraudDispute> Disputes,
    SyntheticFraudLabel Label);

public sealed record SyntheticDataset(IReadOnlyList<SyntheticMerchantScenario> Merchants)
{
    public int MerchantCount => Merchants.Count;

    public int TransactionCount => Merchants.Sum(merchant => merchant.Transactions.Count);

    public int DisputeCount => Merchants.Sum(merchant => merchant.Disputes.Count);

    public int FraudulentMerchantCount => Merchants.Count(merchant => merchant.Label.IsFraudulent);
}
