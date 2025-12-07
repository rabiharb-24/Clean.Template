using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PersistedGrantConfiguration : IEntityTypeConfiguration<PersistedGrant>
{
    public void Configure(EntityTypeBuilder<PersistedGrant> builder)
    {
        builder.HasKey(x => x.Key);
    }
}