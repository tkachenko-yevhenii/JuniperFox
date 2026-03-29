using JuniperFox.Infrastructure.Identity;
using JuniperFox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JuniperFox.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure (
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            services.AddDbContext<JuniperFoxDbContext>(options =>
                options.UseNpgsql(connectionString));

            services
                .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                {
                    options.Password.RequiredLength = 4;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredUniqueChars = 1;
                    options.User.RequireUniqueEmail = false;
                    options.SignIn.RequireConfirmedEmail = false;
                })
                .AddEntityFrameworkStores<JuniperFoxDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IPasswordValidator<ApplicationUser>, PinPasswordValidator>();

            return services;
        }
    }
}