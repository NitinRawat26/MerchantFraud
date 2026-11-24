namespace MerchantFraud.Integrations;

public enum MerchantIntelligenceDecision
{
    Unknown,
    Approve,
    Refer,
    Decline
}

public sealed record MerchantIntelligenceAssessment(
    string MerchantId,
    string AssessmentId,
    MerchantIntelligenceDecision Decision,
    int RiskScore,
    IReadOnlyList<string> ReasonCodes,
    DateTimeOffset AssessedAt,
    string Source = "MerchantIntelligence");
