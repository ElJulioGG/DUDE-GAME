/// <summary>
/// Global flag set before any network connection is established.
/// Stays false for local-only play; set to true when creating or joining a Steam lobby.
/// </summary>
public static class GameSession
{
    public static bool IsOnline { get; set; } = false;
}
