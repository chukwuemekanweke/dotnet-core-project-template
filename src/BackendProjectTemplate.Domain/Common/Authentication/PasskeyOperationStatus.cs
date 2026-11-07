namespace BackendProjectTemplate.Domain.Common.Authentication;

public enum PasskeyOperationStatus
{
    Success = 1,
    InvalidFlow = 2,
    ExpiredFlow = 3,
    ConsumedFlow = 4,
    InvalidCredential = 5,
    DuplicateCredential = 6,
    NotFound = 7,
    Failed = 8
}
