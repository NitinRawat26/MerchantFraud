using System.Text.Json;
using System.Text.Json.Serialization;
using MerchantFraud.Api;
using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;
using MerchantFraud.Features;
using MerchantFraud.Ingestion;
using MerchantFraud.Persistence;
using MerchantFraud.Scoring;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<FraudStore>(_ => new FraudStore(builder.Configuration.GetConnectionString("FraudDb") ?? "Data Source=merchantfraud.db"));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/ingest/transactions", async (HttpRequest request, FraudStore store) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync();
        var result = TransactionFeedParser.ParseJson(json);
        foreach (var transaction in result.Transactions)
        {
            store.UpsertTransaction(transaction);
        }

        store.AddAuditEvent("api", "ingest", "transaction_feed", "transactions", $"stored={result.Transactions.Count};errors={result.Errors.Count}");
        return Results.Ok(new { stored = result.Transactions.Count, errors = result.Errors });
    }
    catch (Exception ex) when (ex is JsonException or FormatException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/score/merchant", (ScoreMerchantRequest request, FraudStore store) =>
{
    try
    {
        var transactions = store.GetTransactions(request.MerchantId);
        var disputes = store.GetDisputes(request.MerchantId);
        var features = MerchantFeatureExtractor.Extract(request.MerchantId, transactions, disputes, currency: request.Currency, asOf: request.AsOf);
        var scoring = MerchantScoringEngine.Score(features, scoredAt: DateTimeOffset.UtcNow);
        store.SaveScoreResult(scoring.ScoreResult);
        store.AddAuditEvent("api", "score", "merchant", request.MerchantId, $"score={scoring.ScoreResult.Score.Value};band={scoring.ScoreResult.Score.Band}");

        return Results.Ok(new
        {
            id = scoring.ScoreResult.Id,
            subjectType = scoring.ScoreResult.SubjectType.ToString(),
            subjectId = scoring.ScoreResult.SubjectId,
            value = scoring.ScoreResult.Score.Value,
            band = scoring.ScoreResult.Score.Band.ToString(),
            reasonCodes = scoring.ScoreResult.Score.ReasonCodes,
            recommendedAction = scoring.RuleEvaluation.RecommendedAction.ToString(),
            highestSeverity = scoring.RuleEvaluation.HighestSeverity.ToString(),
            signals = scoring.Signals
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/alerts", (FraudStore store) =>
{
    var alerts = store.GetScoreResults()
        .Where(score => score.Score.Value >= FraudRiskBands.HighThreshold)
        .OrderByDescending(score => score.Score.Value)
        .ThenByDescending(score => score.ScoredAt)
        .Take(100)
        .Select(score => new
        {
            id = score.Id,
            subjectType = score.SubjectType.ToString(),
            subjectId = score.SubjectId,
            value = score.Score.Value,
            band = score.Score.Band.ToString(),
            modelVersion = score.ModelVersion,
            scoredAt = score.ScoredAt,
            reasonCodes = score.Score.ReasonCodes
        })
        .ToArray();

    return Results.Ok(alerts);
});

app.MapGet("/api/cases", (string? merchantId, FraudStore store) =>
{
    var cases = string.IsNullOrWhiteSpace(merchantId) ? store.GetCases() : store.GetCases(merchantId);
    return Results.Ok(cases.Select(fraudCase => new
    {
        id = fraudCase.Id,
        merchantId = fraudCase.MerchantId,
        title = fraudCase.Title,
        priority = fraudCase.Priority.ToString(),
        status = fraudCase.Status.ToString(),
        assignedTo = fraudCase.AssignedTo,
        openedAt = fraudCase.OpenedAt,
        closedAt = fraudCase.ClosedAt,
        externalRef = fraudCase.ExternalRef
    }));
});

app.MapPost("/api/cases", (CreateCaseRequest request, FraudStore store) =>
{
    var fraudCase = new FraudCase(
        $"CASE-{Guid.NewGuid():N}",
        request.MerchantId,
        request.Title,
        request.Priority,
        request.Status,
        request.AssignedTo,
        DateTimeOffset.UtcNow,
        externalRef: request.ExternalRef);

    store.UpsertCase(fraudCase);
    store.AddAuditEvent("api", "create_case", "case", fraudCase.Id, $"merchant={fraudCase.MerchantId};title={fraudCase.Title}");

    return Results.Ok(new
    {
        id = fraudCase.Id,
        merchantId = fraudCase.MerchantId,
        title = fraudCase.Title,
        priority = fraudCase.Priority.ToString(),
        status = fraudCase.Status.ToString(),
        assignedTo = fraudCase.AssignedTo,
        openedAt = fraudCase.OpenedAt,
        closedAt = fraudCase.ClosedAt,
        externalRef = fraudCase.ExternalRef
    });
});

if (app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists)
{
    app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");
}

app.Run();

public partial class Program;
