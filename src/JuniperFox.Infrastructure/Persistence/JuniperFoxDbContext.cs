using JuniperFox.Domain;
using JuniperFox.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JuniperFox.Infrastructure.Persistence;

public sealed class JuniperFoxDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public JuniperFoxDbContext(DbContextOptions<JuniperFoxDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProductList> ProductLists => Set<ProductList>();

    public DbSet<ProductListItem> ProductListItems => Set<ProductListItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JuniperFoxDbContext).Assembly);
    }
}
