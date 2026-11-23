using MerchantFraud.Core.Domain;

namespace MerchantFraud.Api;

public sealed record ScoreMerchantRequest(string MerchantId, DateTimeOffset? AsOf = null, string? Currency = null);

public sealed record CreateCaseRequest(
    string MerchantId,
    string Title,
    CasePriority Priority = CasePriority.Medium,
    CaseStatus Status = CaseStatus.Open,
    string? AssignedTo = null,
    string? ExternalRef = null);
