using MerchantFraud.Core.Scoring;

namespace MerchantFraud.Core.Domain;

public sealed record FraudScoreResult
{
    public FraudScoreResult(
        string id,
        FraudScoreSubjectType subjectType,
        string subjectId,
        FraudRiskScore score,
        DateTimeOffset scoredAt,
        string? modelVersion = null)
    {
        ArgumentNullException.ThrowIfNull(score);

        Id = DomainGuard.Required(id, nameof(id));
        SubjectType = subjectType;
        SubjectId = DomainGuard.Required(subjectId, nameof(subjectId));
        Score = score;
        ScoredAt = scoredAt;
        ModelVersion = DomainGuard.Optional(modelVersion);
    }

    public string Id { get; init; }

    public FraudScoreSubjectType SubjectType { get; init; }

    public string SubjectId { get; init; }

    public FraudRiskScore Score { get; init; }

    public DateTimeOffset ScoredAt { get; init; }

    public string? ModelVersion { get; init; }
}
