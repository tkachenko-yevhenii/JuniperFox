using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace JuniperFox.Infrastructure.Identity;

/// <summary>Only allows a 4-digit numeric PIN (stored hashed by Identity).</summary>
public sealed class PinPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly Regex PinRegex = new(@"^\d{4}$", RegexOptions.Compiled);

    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (password is null || !PinRegex.IsMatch(password))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidPin",
                Description = "PIN must be exactly 4 digits.",
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
