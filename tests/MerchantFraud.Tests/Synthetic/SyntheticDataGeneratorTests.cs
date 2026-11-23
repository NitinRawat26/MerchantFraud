using MerchantFraud.Core.Domain;
using MerchantFraud.Synthetic;

namespace MerchantFraud.Tests.Synthetic;

public class SyntheticDataGeneratorTests
{
    [Fact]
    public void Generate_IsDeterministic()
    {
        var first = SyntheticDataGenerator.Generate(merchantCount: 8, seed: 123);
        var second = SyntheticDataGenerator.Generate(merchantCount: 8, seed: 123);

        Assert.Equal(first.MerchantCount, second.MerchantCount);
        Assert.Equal(first.TransactionCount, second.TransactionCount);
        Assert.Equal(first.DisputeCount, second.DisputeCount);
        Assert.Equal(first.FraudulentMerchantCount, second.FraudulentMerchantCount);
        Assert.Equal(
            first.Merchants.Select(merchant => merchant.Label.FraudType).ToArray(),
            second.Merchants.Select(merchant => merchant.Label.FraudType).ToArray());
        Assert.Equal(first.Merchants[0].Transactions[0], second.Merchants[0].Transactions[0]);
    }

    [Fact]
    public void Generate_ProducesMixedLabelsAndActivity()
    {
        var dataset = SyntheticDataGenerator.Generate(merchantCount: 12, seed: 42);

        Assert.Equal(12, dataset.MerchantCount);
        Assert.True(dataset.TransactionCount > 0);
        Assert.True(dataset.DisputeCount > 0);
        Assert.True(dataset.FraudulentMerchantCount > 0);
        Assert.True(dataset.FraudulentMerchantCount < dataset.MerchantCount);
        Assert.Contains(dataset.Merchants, merchant => merchant.Label.FraudType == SyntheticFraudType.None);
        Assert.Contains(dataset.Merchants, merchant => merchant.Label.FraudType == SyntheticFraudType.TransactionFraud);
    }

    [Fact]
    public void Generate_FraudulentScenarios_HaveRiskSignals()
    {
        var dataset = SyntheticDataGenerator.Generate(merchantCount: 16, seed: 7);
        var fraudulent = dataset.Merchants.Where(merchant => merchant.Label.IsFraudulent).ToArray();

        Assert.Contains(fraudulent, merchant => merchant.Disputes.Count > 0);
        Assert.Contains(fraudulent, merchant => merchant.Transactions.Count(transaction => transaction.Channel == TransactionChannel.CardNotPresent) >= 10);
        Assert.Contains(fraudulent, merchant => merchant.Transactions.Any(transaction => transaction.Amount.Amount >= 100m && transaction.Amount.Amount % 100m == 0m));
    }
}
