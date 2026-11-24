using MerchantFraud.Scoring;

namespace MerchantFraud.Integrations;

public static class MerchantIntelligenceSignalMapper
{
    public static IReadOnlyList<ScoreSignal> Map(MerchantIntelligenceAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        var signals = new List<ScoreSignal>();
        switch (assessment.Decision)
        {
            case MerchantIntelligenceDecision.Decline:
                signals.Add(new ScoreSignal("MI_DECISION_DECLINE", "MerchantIntelligence decision is Decline.", 250));
                break;
            case MerchantIntelligenceDecision.Refer:
                signals.Add(new ScoreSignal("MI_DECISION_REFER", "MerchantIntelligence decision is Refer.", 120));
                break;
        }

        var riskScore = Math.Clamp(assessment.RiskScore, 0, 1000);
        if (riskScore >= 750)
        {
            signals.Add(new ScoreSignal("MI_RISK_HIGH", "MerchantIntelligence risk score is high.", 150));
        }
        else if (riskScore >= 500)
        {
            signals.Add(new ScoreSignal("MI_RISK_MEDIUM", "MerchantIntelligence risk score is medium.", 80));
        }
        else if (riskScore >= 250)
        {
            signals.Add(new ScoreSignal("MI_RISK_LOW", "MerchantIntelligence risk score is elevated.", 30));
        }

        if (assessment.ReasonCodes.Count > 0)
        {
            signals.Add(new ScoreSignal(
                "MI_REASON_CODES",
                $"MerchantIntelligence returned {assessment.ReasonCodes.Count} reason code(s).",
                Math.Min(30, assessment.ReasonCodes.Count * 10)));
        }

        return signals
            .OrderByDescending(signal => signal.Points)
            .ThenBy(signal => signal.Code, StringComparer.Ordinal)
            .ToArray();
    }
}
