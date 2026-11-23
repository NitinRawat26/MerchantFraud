namespace MerchantFraud.Scoring;

public sealed record ScoreSignal(string Code, string Description, int Points);
