namespace BackendProjectTemplate.Domain.Kyc;

public enum KycVerificationStatus
{
    NotStarted = 1,
    Pending = 2,
    InProgress = 3,
    Verified = 4,
    Rejected = 5,
    Failed = 6,
    Expired = 7
}
