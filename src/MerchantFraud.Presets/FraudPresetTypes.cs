namespace MerchantFraud.Presets;

public sealed record FraudPreset(
    string Id,
    string Name,
    string Description,
    string FocusMerchantId,
    IReadOnlyList<string> MerchantIds);

public sealed record PresetMetric(string Label, string Value);

public sealed record PresetTable(string Title, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record PresetStage(
    int Order,
    string Key,
    string Title,
    string Component,
    string Status,
    string Summary,
    IReadOnlyList<PresetMetric> Metrics,
    IReadOnlyList<PresetTable> Tables);

public sealed record PresetMerchantOutcome(
    string MerchantId,
    string Name,
    string Scenario,
    int Score,
    string Band,
    string RecommendedAction,
    int RuleHits,
    int NetworkLinkedMerchants,
    double MlFraudProbability,
    string MerchantIntelligenceDecision,
    IReadOnlyList<string> ReasonCodes,
    string? CaseId);

public sealed record FraudPresetRun(
    string PresetId,
    string PresetName,
    string Description,
    string FocusMerchantId,
    DateTimeOffset AsOf,
    bool Persisted,
    IReadOnlyList<PresetMerchantOutcome> Merchants,
    IReadOnlyList<PresetStage> Stages);
