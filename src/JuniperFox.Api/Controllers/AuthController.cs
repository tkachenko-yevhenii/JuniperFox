using System.Security.Claims;
using JuniperFox.Contracts.Auth;
using JuniperFox.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JuniperFox.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        if (string.IsNullOrEmpty(userName))
            return BadRequest("User name is required.");

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@users.local",
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, request.Pin);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description).ToList());

        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new CurrentUserResponse { Id = user.Id, UserName = user.UserName! });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null)
            return Unauthorized();

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Pin,
            isPersistent: true,
            lockoutOnFailure: true);

        if (!result.Succeeded)
            return Unauthorized();

        return Ok(new CurrentUserResponse { Id = user.Id, UserName = user.UserName! });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserResponse> Me()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim is null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized();

        var name = User.Identity?.Name;
        if (string.IsNullOrEmpty(name))
            return Unauthorized();

        return Ok(new CurrentUserResponse { Id = id, UserName = name });
    }
}
