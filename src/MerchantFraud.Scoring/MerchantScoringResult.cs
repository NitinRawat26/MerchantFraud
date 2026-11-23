using MerchantFraud.Core.Domain;
using MerchantFraud.Features;
using MerchantFraud.Rules;

namespace MerchantFraud.Scoring;

public sealed record MerchantScoringResult(
    FraudScoreResult ScoreResult,
    FraudRuleEvaluation RuleEvaluation,
    MerchantFeatureSet Features,
    IReadOnlyList<ScoreSignal> Signals,
    int RawScore);
