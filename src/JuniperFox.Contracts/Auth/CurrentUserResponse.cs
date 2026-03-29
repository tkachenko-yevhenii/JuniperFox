namespace JuniperFox.Contracts.Auth;

public sealed class CurrentUserResponse
{
    public required Guid Id { get; init; }

    public required string UserName { get; init; }
}
