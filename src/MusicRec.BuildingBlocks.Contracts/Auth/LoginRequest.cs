namespace MusicRec.BuildingBlocks.Contracts.Auth;

public sealed record LoginRequest(string UserNameOrEmail, string Password);
