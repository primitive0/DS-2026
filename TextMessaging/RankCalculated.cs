using System.Text.Json.Serialization;

namespace TextMessaging;

public sealed record RankCalculated(
    [property: JsonRequired] Guid TextId,
    [property: JsonRequired] double Rank);
