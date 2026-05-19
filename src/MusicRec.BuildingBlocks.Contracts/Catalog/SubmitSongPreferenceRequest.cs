namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SubmitSongPreferenceRequest(Guid UserId, Guid SongId, string FeedbackType, string Source);
