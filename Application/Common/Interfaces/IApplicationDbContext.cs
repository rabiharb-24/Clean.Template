using Duende.IdentityServer.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<PersistedGrant> UserGrants { get; }

    DbSet<Candidate> Candidate { get; }
}