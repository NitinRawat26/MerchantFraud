namespace MerchantFraud.Ingestion;

public sealed record TransactionIngestionOptions(bool MaskSensitiveData = true);
