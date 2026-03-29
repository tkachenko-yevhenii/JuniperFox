namespace JuniperFox.Contracts.Auth;

public sealed class RegisterRequest
{
    public required string UserName { get; init; }

    /// <summary>Four-digit PIN (validated server-side).</summary>
    public required string Pin { get; init; }
}
