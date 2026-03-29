namespace JuniperFox.Contracts.Auth;

public sealed class LoginRequest
{
    public required string UserName { get; init; }

    public required string Pin { get; init; }
}
