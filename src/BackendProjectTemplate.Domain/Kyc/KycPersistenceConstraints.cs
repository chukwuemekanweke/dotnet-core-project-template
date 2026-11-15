namespace BackendProjectTemplate.Domain.Kyc;

public static class KycPersistenceConstraints
{
    public const string ActiveStakeholderUniqueIndexName = "UX_KycVerifications_ActiveStakeholder";
    public const string WebhookRequestUniqueIndexName = "UX_KycWebhookInboxes_ProviderRequest";
}
