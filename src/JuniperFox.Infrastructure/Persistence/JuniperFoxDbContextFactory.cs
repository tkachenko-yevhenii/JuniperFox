using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JuniperFox.Infrastructure.Persistence;

/// <summary>Used by dotnet ef migrations when the startup project does not supply configuration.</summary>
public sealed class JuniperFoxDbContextFactory : IDesignTimeDbContextFactory<JuniperFoxDbContext>
{
    public JuniperFoxDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("JUNIPERFOX_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=juniperfox;Username=juniperfox;Password=juniperfox_dev";

        var optionsBuilder = new DbContextOptionsBuilder<JuniperFoxDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new JuniperFoxDbContext(optionsBuilder.Options);
    }
}
