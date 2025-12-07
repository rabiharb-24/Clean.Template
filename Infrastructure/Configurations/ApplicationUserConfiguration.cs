using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable(Constants.Database.TableNames.Users, Constants.Database.Schemas.Identity);

        builder.Property(x => x.TwoFactorType).HasConversion(new EnumToStringConverter<TwoFactorTypes>());
    }
}