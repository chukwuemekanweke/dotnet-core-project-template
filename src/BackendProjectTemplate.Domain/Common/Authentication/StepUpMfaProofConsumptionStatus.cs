namespace BackendProjectTemplate.Domain.Common.Authentication;

public enum StepUpMfaProofConsumptionStatus
{
    Consumed = 1,
    Invalid = 2,
    Expired = 3,
    AlreadyConsumed = 4,
    ActorMismatch = 5,
    SessionMismatch = 6,
    OperationMismatch = 7,
    RequestMismatch = 8,
    SecurityStampMismatch = 9
}
