using MusicRec.Services.Recommendation.Api.Data.Entities;

namespace MusicRec.Services.Recommendation.Api.Services;

public sealed record RecommendationContext(
    Guid UserId,
    IReadOnlyList<UserSongPreference> Preferences,
    IReadOnlyList<UserBehaviorEvent> BehaviorEvents,
    IReadOnlyList<UserSearchHistory> SearchHistories,
    HashSet<Guid> ExcludedSongIds);
