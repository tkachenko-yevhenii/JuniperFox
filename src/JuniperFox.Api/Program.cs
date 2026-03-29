using JuniperFox.Infrastructure;
using JuniperFox.Infrastructure.Hosting;
using JuniperFox.Infrastructure.Persistence;
using JuniperFox.Api.Hubs;
using Serilog;

namespace JuniperFox.Api;

/// <summary>API host for REST, SignalR (later), and Blazor WebAssembly clients.</summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Avoid duplicate lines in the debugger: default Console + Debug providers plus Serilog.
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddOpenApi();
        builder.Services.AddInfrastructure(builder.Configuration);

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();
        var crossOriginCookies = builder.Configuration.GetValue("Auth:CrossOriginCookies", false);

        if (allowedOrigins.Length > 0)
        {
            builder.Services.AddCors(options => options.AddPolicy(
                "Wasm",
                policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));
        }

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "JuniperFox.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = crossOriginCookies ? SameSiteMode.None : SameSiteMode.Lax;
            options.Cookie.SecurePolicy = crossOriginCookies
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<JuniperFoxDbContext>(tags: ["db"]);

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        await app.ApplyDatabaseMigrationsIfEnabledAsync();

        app.UseHttpsRedirection();

        if (allowedOrigins.Length > 0)
            app.UseCors("Wasm");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<ListUpdatesHub>("/hubs/lists");
        app.MapHealthChecks("/health");

        try
        {
            await app.RunAsync();
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
