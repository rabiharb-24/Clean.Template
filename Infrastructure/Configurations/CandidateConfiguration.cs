using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.Property(x => x.Gender).HasConversion(new EnumToStringConverter<Gender>());
        builder.Property(x => x.MaritalStatus).HasConversion(new EnumToStringConverter<MaritalStatus>());
    }
}