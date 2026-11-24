using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MerchantFraud.Tests.Api;

public class MerchantFraudApiTests
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var factory = new MerchantFraudApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task IngestAndScore_Merchant_ReturnsScoreAndSupportsCasesAndAlerts()
    {
        using var factory = new MerchantFraudApiFactory();
        using var client = factory.CreateClient();

        var ingestResponse = await client.PostAsJsonAsync("/api/ingest/transactions", new
        {
            transactions = new[]
            {
                new
                {
                    id = "T1",
                    merchantId = "MER-1",
                    occurredAt = "2026-01-01T12:00:00Z",
                    amount = 42.50m,
                    currency = "USD",
                    channel = "CardPresent",
                    status = "Captured"
                }
            }
        });

        ingestResponse.EnsureSuccessStatusCode();
        var ingestJson = await ingestResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, ingestJson.GetProperty("stored").GetInt32());
        Assert.Equal(0, ingestJson.GetProperty("errors").GetArrayLength());

        var scoreResponse = await client.PostAsJsonAsync("/api/score/merchant", new
        {
            merchantId = "MER-1",
            asOf = "2026-01-02T00:00:00Z"
        });

        scoreResponse.EnsureSuccessStatusCode();
        var scoreJson = await scoreResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MER-1", scoreJson.GetProperty("subjectId").GetString());
        Assert.Equal("Merchant", scoreJson.GetProperty("subjectType").GetString());
        Assert.Equal(0, scoreJson.GetProperty("value").GetInt32());
        Assert.Equal("Low", scoreJson.GetProperty("band").GetString());
        Assert.Equal("None", scoreJson.GetProperty("recommendedAction").GetString());

        var alerts = await client.GetFromJsonAsync<JsonElement>("/api/alerts");
        Assert.Equal(0, alerts.GetArrayLength());

        var createCaseResponse = await client.PostAsJsonAsync("/api/cases", new
        {
            merchantId = "MER-1",
            title = "Review merchant",
            priority = "High",
            status = "Open",
            assignedTo = "analyst-1"
        });

        createCaseResponse.EnsureSuccessStatusCode();
        var caseJson = await createCaseResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MER-1", caseJson.GetProperty("merchantId").GetString());
        Assert.Equal("High", caseJson.GetProperty("priority").GetString());
        Assert.Equal("Open", caseJson.GetProperty("status").GetString());

        var cases = await client.GetFromJsonAsync<JsonElement>("/api/cases?merchantId=MER-1");
        Assert.Equal(1, cases.GetArrayLength());
        Assert.Equal("Review merchant", cases[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Ingest_InvalidJson_ReturnsBadRequest()
    {
        using var factory = new MerchantFraudApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ingest/transactions", new StringContent("not-json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Presets_ListAndRun_PopulateAlertsAndCases()
    {
        using var factory = new MerchantFraudApiFactory();
        using var client = factory.CreateClient();

        var presets = await client.GetFromJsonAsync<JsonElement>("/api/presets");
        Assert.Contains(presets.EnumerateArray(), preset => preset.GetProperty("id").GetString() == "full-platform");

        var runResponse = await client.PostAsync("/api/presets/full-platform/run", null);
        runResponse.EnsureSuccessStatusCode();
        var run = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, run.GetProperty("stages").GetArrayLength());
        Assert.True(run.GetProperty("persisted").GetBoolean());

        var alerts = await client.GetFromJsonAsync<JsonElement>("/api/alerts");
        Assert.True(alerts.GetArrayLength() > 0);
        var cases = await client.GetFromJsonAsync<JsonElement>("/api/cases");
        Assert.True(cases.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Presets_DryRun_DoesNotPersist()
    {
        using var factory = new MerchantFraudApiFactory();
        using var client = factory.CreateClient();

        var runResponse = await client.PostAsync("/api/presets/card-testing/run?persist=false", null);
        runResponse.EnsureSuccessStatusCode();
        var run = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(run.GetProperty("persisted").GetBoolean());

        var alerts = await client.GetFromJsonAsync<JsonElement>("/api/alerts");
        Assert.Equal(0, alerts.GetArrayLength());
    }

    [Fact]
    public async Task Presets_UnknownId_ReturnsNotFound()
    {
        using var factory = new MerchantFraudApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/presets/does-not-exist/run", null);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class MerchantFraudApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FraudDb"] = "Data Source=:memory:"
                });
            });
        }
    }
}
