namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed class StepUpMfaOptions
{
    public const string SectionName = "Authentication:StepUpMfa";

    public TimeSpan ProofLifetime { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan ReplayWindow { get; init; } = TimeSpan.FromMinutes(2);

    public void Validate()
    {
        if (ProofLifetime != TimeSpan.FromMinutes(2))
            throw new InvalidOperationException("Step-up MFA proof lifetime must be exactly two minutes.");
        if (ReplayWindow < TimeSpan.FromSeconds(30) || ReplayWindow > TimeSpan.FromMinutes(5))
            throw new InvalidOperationException("Step-up MFA replay window must be between thirty seconds and five minutes.");
    }
}
