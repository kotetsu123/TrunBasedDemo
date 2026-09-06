using System.Collections.Generic;

public static class FieldAutoEventRuntimeState
{
    private static readonly HashSet<string> completedEventIds = new HashSet<string>();

    public static bool IsCompleted(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        return completedEventIds.Contains(eventId);
    }

    public static void MarkCompleted(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return;

        completedEventIds.Add(eventId);
    }

    public static void Clear()
    {
        completedEventIds.Clear();
    }
}
