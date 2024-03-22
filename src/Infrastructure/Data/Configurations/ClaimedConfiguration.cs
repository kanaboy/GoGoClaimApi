using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using GoGoClaimApi.Domain.Entities;

namespace GoGoClaimApi.Infrastructure.Data.Configurations;

public class ClaimedConfiguration : IEntityTypeConfiguration<Claimed>
{
    public void Configure(EntityTypeBuilder<Claimed> builder)
    {
        builder.ToTable("Claimeds", "gogoclaim");

        builder.Property(t => t.VisitId)
            .IsRequired();

        builder.Property(t => t.Dir)
            .IsRequired();
    }
}
