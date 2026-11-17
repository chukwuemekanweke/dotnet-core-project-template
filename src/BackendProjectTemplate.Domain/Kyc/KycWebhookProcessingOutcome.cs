namespace BackendProjectTemplate.Domain.Kyc;

public enum KycWebhookProcessingOutcome
{
    Pending = 1,
    StateTransitioned = 2,
    NoStateTransition = 3,
    UnknownVerification = 4,
    ReferenceMismatch = 5,
    TerminalVerificationIgnored = 6
}
