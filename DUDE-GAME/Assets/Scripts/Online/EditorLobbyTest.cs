#if UNITY_EDITOR
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEngine;

/// <summary>
/// Attach to any GameObject in the scene.
/// In the Editor, use the on-screen buttons to start as Host or Client
/// without needing Steam running. Uses Tugboat (UDP) transport on localhost.
///
/// Make sure your NetworkManager has a Tugboat transport component added to it,
/// or this script will log an error and do nothing.
/// </summary>
public class EditorLobbyTest : MonoBehaviour
{
    [SerializeField] private string localAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7770;

    private Tugboat _tugboat;

    private void Awake()
    {
        _tugboat = InstanceFinder.NetworkManager?.GetComponent<Tugboat>();
        if (_tugboat == null)
            Debug.LogWarning("[EditorLobbyTest] No Tugboat component found on NetworkManager. Add one to test in the Editor.");
    }

    private void OnGUI()
    {
        if (!Application.isEditor) return;

        float btnW = 180f, btnH = 40f, margin = 10f;
        float startX = margin;
        float startY = Screen.height - (btnH + margin) * 4 - margin;

        GUI.Box(new Rect(startX - 5, startY - 5, btnW + 10, (btnH + margin) * 4 + 10), "Editor Lobby Test");

        if (GUI.Button(new Rect(startX, startY + (btnH + margin) * 0, btnW, btnH), "Start Host (Server+Client)"))
            StartHost();

        if (GUI.Button(new Rect(startX, startY + (btnH + margin) * 1, btnW, btnH), "Start Client (localhost)"))
            StartClient();

        if (GUI.Button(new Rect(startX, startY + (btnH + margin) * 2, btnW, btnH), "Stop All"))
            StopAll();

        GUI.Label(new Rect(startX, startY + (btnH + margin) * 3, btnW, btnH),
            $"Server: {InstanceFinder.IsServerStarted}  Client: {InstanceFinder.IsClientStarted}");
    }

    private void StartHost()
    {
        if (_tugboat == null) { Debug.LogError("[EditorLobbyTest] Tugboat not found."); return; }

        SwapToTugboat();
        InstanceFinder.NetworkManager.ServerManager.StartConnection();
        InstanceFinder.NetworkManager.ClientManager.StartConnection();
        Debug.Log("[EditorLobbyTest] Started as Host on Tugboat.");
    }

    private void StartClient()
    {
        if (_tugboat == null) { Debug.LogError("[EditorLobbyTest] Tugboat not found."); return; }

        SwapToTugboat();
        InstanceFinder.NetworkManager.ClientManager.StartConnection(localAddress);
        Debug.Log($"[EditorLobbyTest] Started Client → {localAddress}:{port}.");
    }

    private void StopAll()
    {
        if (InstanceFinder.IsServerStarted)
            InstanceFinder.NetworkManager.ServerManager.StopConnection(true);
        if (InstanceFinder.IsClientStarted)
            InstanceFinder.NetworkManager.ClientManager.StopConnection();
        Debug.Log("[EditorLobbyTest] Stopped all connections.");
    }

    private void SwapToTugboat()
    {
        _tugboat.SetPort(port);
        InstanceFinder.NetworkManager.TransportManager.Transport = _tugboat;
    }
}
#endif
