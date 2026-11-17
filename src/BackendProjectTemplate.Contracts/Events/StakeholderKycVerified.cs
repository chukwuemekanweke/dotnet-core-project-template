namespace BackendProjectTemplate.Contracts.Events;

public sealed record StakeholderKycVerified : BaseEvent
{
    public Guid KycVerificationId { get; init; }
}
