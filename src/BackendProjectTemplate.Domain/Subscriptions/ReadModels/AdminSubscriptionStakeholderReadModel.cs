namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionStakeholderReadModel(
    Guid StakeholderId,
    string FirstName,
    string LastName);
