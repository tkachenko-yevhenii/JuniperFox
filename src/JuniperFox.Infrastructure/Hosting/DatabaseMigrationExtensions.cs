using JuniperFox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JuniperFox.Infrastructure.Hosting;

/// <summary>
/// Applies pending EF Core migrations embedded in the Infrastructure assembly.
/// <see cref="DatabaseFacade.MigrateAsync"/> reads the <c>__EFMigrationsHistory</c> table and runs only new migration scripts.
/// Call from each host that starts the app with a database (<b>Web</b> and/or <b>Api</b>). Already-applied migrations are skipped.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations before the app serves requests. Safe to call on every startup: already-applied migrations are skipped.
    /// Set configuration <c>Database:ApplyMigrationsOnStartup</c> to <c>false</c> to skip (e.g. when migrations run only in CI/CD).
    /// </summary>
    public static async Task ApplyDatabaseMigrationsIfEnabledAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        var raw = app.Configuration["Database:ApplyMigrationsOnStartup"];
        if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JuniperFoxDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
