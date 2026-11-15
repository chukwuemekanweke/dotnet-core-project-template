namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IStepUpMfaProofStore
{
    Task<StepUpMfaProofIssueResult> IssueAsync(
        StepUpMfaProof proof,
        CancellationToken cancellationToken);

    Task<StepUpMfaProofConsumptionStatus> ConsumeAsync(
        string authorizationToken,
        StepUpMfaProofConsumption expected,
        CancellationToken cancellationToken);

    Task<bool> TryClaimAuthenticatorCodeAsync(
        Guid appUserId,
        string authenticatorCode,
        TimeSpan replayWindow,
        CancellationToken cancellationToken);
}
