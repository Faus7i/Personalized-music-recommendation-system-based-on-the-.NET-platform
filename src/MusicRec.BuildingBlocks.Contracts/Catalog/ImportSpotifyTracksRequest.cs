namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record ImportSpotifyTracksRequest(
    IReadOnlyList<string> SpotifyTrackIds,
    string GenreName,
    bool MarkAsColdStartCandidate);
