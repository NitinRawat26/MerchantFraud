using MerchantFraud.Features;
using MerchantFraud.Synthetic;

namespace MerchantFraud.Ml;

public static class FraudDatasetBuilder
{
    private static readonly DateTimeOffset DefaultAsOf = new(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<FraudTrainingExample> Build(SyntheticDataset dataset, DateTimeOffset? asOf = null)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var examples = new List<FraudTrainingExample>(dataset.Merchants.Count);
        foreach (var scenario in dataset.Merchants)
        {
            var scenarioAsOf = asOf ?? LatestActivity(scenario);
            var featureSet = MerchantFeatureExtractor.Extract(
                scenario.Merchant.Id,
                scenario.Transactions,
                scenario.Disputes,
                asOf: scenarioAsOf);
            examples.Add(new FraudTrainingExample(
                scenario.Merchant.Id,
                scenarioAsOf,
                scenario.Label.IsFraudulent,
                scenario.Label.FraudType,
                FraudFeatureMapper.Map(featureSet)));
        }

        return examples;
    }

    private static DateTimeOffset LatestActivity(SyntheticMerchantScenario scenario)
    {
        var latest = DefaultAsOf;
        foreach (var transaction in scenario.Transactions)
        {
            if (transaction.OccurredAt > latest)
            {
                latest = transaction.OccurredAt;
            }
        }

        foreach (var dispute in scenario.Disputes)
        {
            if (dispute.OpenedAt > latest)
            {
                latest = dispute.OpenedAt;
            }
        }

        return latest;
    }
}
