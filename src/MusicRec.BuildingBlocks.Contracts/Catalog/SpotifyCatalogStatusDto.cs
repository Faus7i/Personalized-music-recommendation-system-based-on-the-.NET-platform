namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SpotifyCatalogStatusDto(
    bool Configured,
    string Message);
