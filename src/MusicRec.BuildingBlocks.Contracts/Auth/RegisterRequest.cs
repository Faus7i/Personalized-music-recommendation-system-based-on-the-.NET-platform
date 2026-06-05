namespace MusicRec.BuildingBlocks.Contracts.Auth;

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string? PhoneNumber,
    string Password,
    string ConfirmPassword);
