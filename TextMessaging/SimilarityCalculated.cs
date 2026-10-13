using System.Text.Json.Serialization;

namespace TextMessaging;

public sealed record SimilarityCalculated(
    [property: JsonRequired] Guid TextId,
    [property: JsonRequired] double Similarity);
