namespace MerchantFraud.Core.Domain;

public enum MerchantLifecycle
{
    Prospect,
    Active,
    Suspended,
    Terminated
}

public enum TransactionChannel
{
    Unknown,
    CardPresent,
    CardNotPresent,
    Moto,
    Ecommerce
}

public enum TransactionStatus
{
    Authorized,
    Captured,
    Settled,
    Declined,
    Refunded,
    ChargedBack
}

public enum DisputeStatus
{
    Open,
    UnderReview,
    Won,
    Lost,
    Accepted
}

public enum CasePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum CaseStatus
{
    Open,
    InReview,
    PendingInformation,
    Resolved,
    Closed
}

public enum FraudScoreSubjectType
{
    Merchant,
    Application,
    Transaction,
    Case,
    MerchantNetwork
}
