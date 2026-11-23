using MerchantFraud.Core.Domain;

namespace MerchantFraud.Ingestion;

public sealed record TransactionFeedResult(IReadOnlyList<FraudTransaction> Transactions, IReadOnlyList<string> Errors);
