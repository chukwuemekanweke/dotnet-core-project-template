using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class BillingAccountConfiguration : IEntityTypeConfiguration<BillingAccount>
{
    public const string TenantOwnerUniqueIndexName = "UX_BillingAccounts_TenantOwner";
    public const string StakeholderOwnerUniqueIndexName = "UX_BillingAccounts_StakeholderOwner";

    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        builder.ToTable(
            "BillingAccounts",
            SchemaNames.Payments,
            tableBuilder => tableBuilder.HasCheckConstraint(
                "CK_BillingAccounts_Owner",
                $"(\"OwnerType\" = {(int)BillingAccountOwnerType.Tenant} AND \"StakeholderId\" IS NULL) OR "
                + $"(\"OwnerType\" = {(int)BillingAccountOwnerType.Stakeholder} AND \"StakeholderId\" IS NOT NULL)"));

        builder.HasKey(account => account.Id);
        builder.HasAlternateKey(account => new { account.TenantId, account.Id })
            .HasName("AK_BillingAccounts_TenantId_Id");

        builder.Property(account => account.TenantId).IsRequired();
        builder.Property(account => account.OwnerType).IsRequired();
        builder.Property(account => account.DisplayName)
            .HasMaxLength(BillingAccount.MaxDisplayNameLength)
            .IsRequired();

        builder.HasIndex(account => account.TenantId)
            .IsUnique()
            .HasDatabaseName(TenantOwnerUniqueIndexName)
            .HasFilter($"\"OwnerType\" = {(int)BillingAccountOwnerType.Tenant}");

        builder.HasIndex(account => new { account.TenantId, account.StakeholderId })
            .IsUnique()
            .HasDatabaseName(StakeholderOwnerUniqueIndexName)
            .HasFilter($"\"OwnerType\" = {(int)BillingAccountOwnerType.Stakeholder}");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(account => account.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(account => new { account.TenantId, account.StakeholderId })
            .HasPrincipalKey(stakeholder => new { stakeholder.TenantId, stakeholder.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
