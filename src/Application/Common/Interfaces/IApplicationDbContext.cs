using GoGoClaimApi.Domain.Entities;

namespace GoGoClaimApi.Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<Claimed> Claimeds { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
