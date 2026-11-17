namespace BackendProjectTemplate.Domain.Kyc;

public enum KycVerificationTransition
{
    NoChange = 1,
    InProgress = 2,
    Verified = 3,
    Rejected = 4
}
