namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAccountStatementNotificationSettings(
    bool SmsNotification,
    bool EmailNotification,
    bool EmailMonthlyStatement,
    bool SmsMonthlyStatement);
