namespace TextMessaging;

public static class StorageKeys
{
    public static string Text(Guid id) => $"TEXT-{id}";
    public static string Rank(Guid id) => $"RANK-{id}";
    public static string Similarity(Guid id) => $"SIMILARITY-{id}";
}
