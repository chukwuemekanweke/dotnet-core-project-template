using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SignUpOtpConfiguration : IEntityTypeConfiguration<SignUpOtp>
{
    public void Configure(EntityTypeBuilder<SignUpOtp> builder)
    {
        builder.ToTable("SignUpOtps", SchemaNames.Authentication);
        builder.HasKey(otp => otp.Id);

        builder.Property(otp => otp.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(otp => otp.CodeHash).HasMaxLength(256).IsRequired();

        builder.HasIndex(otp => otp.NormalizedEmail);
    }
}
