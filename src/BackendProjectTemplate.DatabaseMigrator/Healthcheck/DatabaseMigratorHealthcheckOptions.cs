namespace BackendProjectTemplate.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigratorHealthcheckOptions
{
    public const string SectionName = "Healthcheck";

    public string ReadyFilePath { get; set; } = "/tmp/database-migrator/ready";
    public string FailedFilePath { get; set; } = "/tmp/database-migrator/failed";
}
