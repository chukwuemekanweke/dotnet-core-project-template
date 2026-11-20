namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record ActiveUserAnalyticsDailyRow(DateOnly Date, long Count);
