namespace MusicRec.BuildingBlocks.Contracts.Auth;

public sealed record AuthResponse(Guid UserId, string UserName, string Token, DateTimeOffset ExpiresAtUtc);
