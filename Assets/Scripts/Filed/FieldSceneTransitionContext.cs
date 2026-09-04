public static class FieldSceneTransitionContext
{
    private static string pendingSpawnPointId;

    public static bool HasPendingSpawnPoint => !string.IsNullOrWhiteSpace(pendingSpawnPointId);

    public static void SetPendingSpawnPoint(string spawnPointId)
    {
        pendingSpawnPointId = spawnPointId;
    }

    public static bool TryConsumePendingSpawnPoint(out string spawnPointId)
    {
        spawnPointId = pendingSpawnPointId;
        pendingSpawnPointId = null;

        return !string.IsNullOrWhiteSpace(spawnPointId);
    }

    public static void Clear()
    {
        pendingSpawnPointId = null;
    }
}
