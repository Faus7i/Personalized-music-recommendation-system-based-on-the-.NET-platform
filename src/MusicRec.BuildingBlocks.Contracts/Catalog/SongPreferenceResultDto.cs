namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SongPreferenceResultDto(Guid UserId, Guid SongId, string FeedbackType, string Source, DateTimeOffset CreatedAtUtc);
