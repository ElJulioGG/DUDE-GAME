using FishNet;
using UnityEngine;

/// <summary>
/// Debug overlay (toggle with F3) showing role, ping and tick rate.
/// Auto-created by NetworkGameManager when a client starts. Use it to tell at a
/// glance whether a rough session is the code or the connection: a high/spiky
/// ping means Steam relay, a steady low ping means look at the game.
/// </summary>
public class NetworkStatsHud : MonoBehaviour
{
    private bool _visible;
    private GUIStyle _style;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible || !GameSession.IsOnline) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            _style.normal.textColor = Color.white;
        }

        var tm = InstanceFinder.TimeManager;
        string role = InstanceFinder.IsHostStarted ? "HOST"
                    : InstanceFinder.IsClientStarted ? "CLIENT"
                    : "OFFLINE";
        long ping = tm != null ? tm.RoundTripTime : 0;
        ushort tick = tm != null ? tm.TickRate : (ushort)0;

        GUI.Box(new Rect(8, 8, 220, 26), GUIContent.none);
        GUI.Label(new Rect(14, 11, 220, 22), $"{role}   ping {ping} ms   tick {tick}", _style);
    }
}
