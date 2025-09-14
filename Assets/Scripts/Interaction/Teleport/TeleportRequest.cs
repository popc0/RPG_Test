public static class TeleportRequest
{
    public static bool hasPending;
    public static string sceneName;
    public static string spawnId;

    public static void Clear()
    {
        hasPending = false;
        sceneName = null;
        spawnId = null;
    }
}
