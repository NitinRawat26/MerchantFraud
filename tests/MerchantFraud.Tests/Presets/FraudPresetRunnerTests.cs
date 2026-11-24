using System.Text.Json;
using MerchantFraud.Persistence;
using MerchantFraud.Presets;

namespace MerchantFraud.Tests.Presets;

public class FraudPresetRunnerTests
{
    [Fact]
    public void FullPlatform_RunsEveryComponentInOrder()
    {
        var run = FraudPresetRunner.Run(FraudPresetCatalog.Find("full-platform")!);

        Assert.Equal(
            new[] { "synthetic", "mapwright", "ingestion", "features", "rules", "network", "merchant-intelligence", "ml", "scoring", "persistence" },
            run.Stages.Select(stage => stage.Key));
        Assert.Equal(Enumerable.Range(1, 10), run.Stages.Select(stage => stage.Order));
        Assert.Equal(7, run.Merchants.Count);
        Assert.False(run.Persisted);
        Assert.Equal("skipped", run.Stages[^1].Status);
    }

    [Fact]
    public void FullPlatform_SeparatesFraudScenariosFromCleanControl()
    {
        var run = FraudPresetRunner.Run(FraudPresetCatalog.Find("full-platform")!);
        var byId = run.Merchants.ToDictionary(merchant => merchant.MerchantId);

        Assert.Equal("Low", byId[DemoMerchantScenarios.Bakery].Band);
        Assert.Null(byId[DemoMerchantScenarios.Bakery].CaseId);
        Assert.Contains(byId[DemoMerchantScenarios.GiftCards].Band, new[] { "High", "Critical" });
        Assert.Contains("VELOCITY_24H_HIGH", byId[DemoMerchantScenarios.GiftCards].ReasonCodes);
        Assert.Contains(byId[DemoMerchantScenarios.Gadgets].Band, new[] { "High", "Critical" });
        Assert.All(DemoMerchantScenarios.RingMerchantIds, ringId =>
        {
            Assert.Equal(3, byId[ringId].NetworkLinkedMerchants);
            Assert.Contains("NETWORK_LINKED_MERCHANTS", byId[ringId].ReasonCodes);
        });
    }

    [Fact]
    public void Ingestion_RejectsBrokenRowAndNeverExposesFullPan()
    {
        var run = FraudPresetRunner.Run(FraudPresetCatalog.Find("card-testing")!);
        var ingestion = run.Stages.Single(stage => stage.Key == "ingestion");

        Assert.Equal("warning", ingestion.Status);
        Assert.Contains(ingestion.Metrics, metric => metric.Label == "Rejected" && metric.Value == "1");
        var json = JsonSerializer.Serialize(run);
        Assert.DoesNotMatch(@"\d{13,19}", json);
    }

    [Fact]
    public void CleanMerchant_HasNoNetworkLinksOrCases()
    {
        var run = FraudPresetRunner.Run(FraudPresetCatalog.Find("clean-merchant")!);

        var merchant = Assert.Single(run.Merchants);
        Assert.Equal("Low", merchant.Band);
        Assert.Equal(0, merchant.NetworkLinkedMerchants);
        Assert.Contains(run.Stages.Single(stage => stage.Key == "network").Metrics, metric => metric.Label == "Links" && metric.Value == "0");
    }

    [Fact]
    public void Persist_WritesScoresCasesAndAuditTrail_AndIsRepeatable()
    {
        using var store = new FraudStore("Data Source=:memory:");
        var preset = FraudPresetCatalog.Find("full-platform")!;

        var first = FraudPresetRunner.Run(preset, store);
        var run = FraudPresetRunner.Run(preset, store);

        Assert.True(run.Persisted);
        Assert.Equal(7, store.GetMerchants().Count);
        Assert.Equal(7, store.GetScoreResults().Count);
        var cases = store.GetCases();
        Assert.NotEmpty(cases);
        Assert.All(cases, fraudCase => Assert.StartsWith("CASE-PRESET-", fraudCase.Id));
        Assert.Equal(run.Merchants.Count(merchant => merchant.CaseId is not null), cases.Count);
        Assert.NotEmpty(store.GetAuditEvents("full-platform"));
        Assert.Contains(first.Stages[^1].Metrics, metric => metric.Label == "Cases opened" && metric.Value == cases.Count.ToString());
        Assert.Contains(run.Stages[^1].Metrics, metric => metric.Label == "Cases opened" && metric.Value == "0");
        Assert.Contains(run.Stages[^1].Metrics, metric => metric.Label == "Cases already open" && metric.Value == cases.Count.ToString());
        Assert.Contains("0 new case(s) opened", run.Stages[^1].Summary);
    }
}
