namespace MusicRec.BuildingBlocks.Contracts.Recommendations;

public sealed record RecommendationEvaluationDto(
    Guid UserId,
    int CandidateCount,
    int HoldoutCount,
    double PrecisionAt5,
    double RecallAt5,
    double PrecisionAt10,
    double RecallAt10,
    string Notes);
