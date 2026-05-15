using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any GameObject in the GameScene.
/// Set ghostContainer to the RectTransform of the canvas/panel that contains
/// the PlayerCursor objects (the same one ControllerMapper calls mapperCanvas).
///
/// During the CSS phase, each machine broadcasts its local cursor positions at
/// ~20 fps. The server relays them to all other machines, which render ghost
/// cursors so every player can see where remote cursors are.
/// </summary>
public class CSSCursorSync : MonoBehaviour
{
    [SerializeField] private RectTransform ghostContainer;

    // Unique ID for this process — never changes while the game runs.
    // Used to filter our own echoed broadcasts instead of relying on FishNet's ClientId
    // (FishySteamworks assigns 32767 to all local connections, which breaks ID-based filtering).
    private static readonly int LocalSessionId = System.Guid.NewGuid().GetHashCode();

    private readonly float _sendInterval = 0.05f;
    private float _sendTimer;

    private readonly Dictionary<int, GhostCursorSet> _remotes = new();

    private static readonly Color[] PlayerColors =
    {
        Color.red, Color.blue, Color.green, new Color(0.5f, 0f, 0.5f)
    };

    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.RegisterBroadcast<NetworkGameManager.CSSCursorStateBroadcast>(OnReceiveRemote);
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.RegisterBroadcast<NetworkGameManager.CSSCursorStateBroadcast>(OnServerReceiveCursor);
    }

    private void OnDisable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.UnregisterBroadcast<NetworkGameManager.CSSCursorStateBroadcast>(OnReceiveRemote);
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.UnregisterBroadcast<NetworkGameManager.CSSCursorStateBroadcast>(OnServerReceiveCursor);
        ClearAllGhosts();
    }

    private void Update()
    {
        if (!GameSession.IsOnline || !InstanceFinder.IsClientStarted) return;

        bool inCSS = GameManager.instance != null && GameManager.instance.assignController;
        if (!inCSS)
        {
            ClearAllGhosts();
            return;
        }

        _sendTimer -= Time.unscaledDeltaTime;
        if (_sendTimer > 0f) return;
        _sendTimer = _sendInterval;

        SendLocalState();
    }

    // -------------------------------------------------------------------------

    private bool _loggedSend;
    private void SendLocalState()
    {
        var msg = new NetworkGameManager.CSSCursorStateBroadcast { ClientId = LocalSessionId };
        var cursors = PlayerCursor.All;
        bool anyActive = false;

        if (cursors != null)
        {
            for (int i = 0; i < cursors.Length && i < 3; i++)
            {
                var c = cursors[i];
                if (c == null || !c.gameObject.activeSelf) continue;
                anyActive = true;
                var rt = c.GetComponent<RectTransform>();
                float x = rt != null ? rt.anchoredPosition.x : 0f;
                float y = rt != null ? rt.anchoredPosition.y : 0f;

                switch (i)
                {
                    case 0: msg.Active0 = true; msg.X0 = x; msg.Y0 = y; msg.Assigned0 = c.IsAssigned; msg.PlayerIndex0 = (sbyte)c.AssignedPlayerIndex; break;
                    case 1: msg.Active1 = true; msg.X1 = x; msg.Y1 = y; msg.Assigned1 = c.IsAssigned; msg.PlayerIndex1 = (sbyte)c.AssignedPlayerIndex; break;
                    case 2: msg.Active2 = true; msg.X2 = x; msg.Y2 = y; msg.Assigned2 = c.IsAssigned; msg.PlayerIndex2 = (sbyte)c.AssignedPlayerIndex; break;
                }
            }
        }

        if (!_loggedSend)
        {
            _loggedSend = true;
            Debug.Log($"[CSSCursorSync] SendLocalState: anyActive={anyActive}, LocalSessionId={LocalSessionId}, cursors={(cursors == null ? "null" : cursors.Length.ToString())}");
        }

        InstanceFinder.ClientManager.Broadcast(msg);
    }

    private void OnReceiveRemote(NetworkGameManager.CSSCursorStateBroadcast msg, Channel channel)
    {
        Debug.Log($"[CSSCursorSync] OnReceiveRemote: clientId={msg.ClientId}, localId={LocalSessionId}, active={msg.Active0}/{msg.Active1}/{msg.Active2}");
        if (msg.ClientId == LocalSessionId) return; // own echo

        bool inCSS = GameManager.instance != null && GameManager.instance.assignController;
        if (!inCSS || ghostContainer == null) return;

        if (!_remotes.TryGetValue(msg.ClientId, out var set))
        {
            set = new GhostCursorSet(ghostContainer);
            _remotes[msg.ClientId] = set;
        }

        set.ApplySlot(0, msg.Active0, msg.X0, msg.Y0, msg.Assigned0, msg.PlayerIndex0);
        set.ApplySlot(1, msg.Active1, msg.X1, msg.Y1, msg.Assigned1, msg.PlayerIndex1);
        set.ApplySlot(2, msg.Active2, msg.X2, msg.Y2, msg.Assigned2, msg.PlayerIndex2);
    }

    // Server-side handler — FishNet's ServerManager.Broadcast doesn't loop back to the
    // host's own local client, so the host processes remote cursor data here directly.
    private void OnServerReceiveCursor(FishNet.Connection.NetworkConnection conn, NetworkGameManager.CSSCursorStateBroadcast msg, FishNet.Transporting.Channel channel)
    {
        Debug.Log($"[CSSCursorSync] OnServerReceiveCursor fired: clientId={msg.ClientId} isHost={InstanceFinder.IsServerStarted && InstanceFinder.IsClientStarted}");
        if (!InstanceFinder.IsServerStarted || !InstanceFinder.IsClientStarted) return; // host only
        if (msg.ClientId == LocalSessionId) return;

        bool inCSS = GameManager.instance != null && GameManager.instance.assignController;
        if (!inCSS || ghostContainer == null) return;

        if (!_remotes.TryGetValue(msg.ClientId, out var set))
        {
            set = new GhostCursorSet(ghostContainer);
            _remotes[msg.ClientId] = set;
        }

        set.ApplySlot(0, msg.Active0, msg.X0, msg.Y0, msg.Assigned0, msg.PlayerIndex0);
        set.ApplySlot(1, msg.Active1, msg.X1, msg.Y1, msg.Assigned1, msg.PlayerIndex1);
        set.ApplySlot(2, msg.Active2, msg.X2, msg.Y2, msg.Assigned2, msg.PlayerIndex2);
    }

    private void ClearAllGhosts()
    {
        foreach (var set in _remotes.Values)
            set.DestroyAll();
        _remotes.Clear();
    }

    // =========================================================================
    // Inner types
    // =========================================================================

    private class GhostCursorSet
    {
        private readonly RectTransform _parent;
        private readonly GhostEntry[] _slots = new GhostEntry[3];

        public GhostCursorSet(RectTransform parent) { _parent = parent; }

        public void ApplySlot(int i, bool active, float x, float y, bool assigned, sbyte playerIndex)
        {
            if (!active) { _slots[i]?.SetVisible(false); return; }
            _slots[i] ??= new GhostEntry(_parent);
            _slots[i].SetVisible(true);
            _slots[i].SetPosition(x, y);
            _slots[i].SetState(assigned, playerIndex);
        }

        public void DestroyAll()
        {
            foreach (var s in _slots) s?.Destroy();
        }
    }

    private class GhostEntry
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private readonly Image _img;
        private readonly TextMeshProUGUI _label;

        public GhostEntry(RectTransform parent)
        {
            _go = new GameObject("RemoteCursor", typeof(RectTransform));
            _go.transform.SetParent(parent, false);

            _rt = _go.GetComponent<RectTransform>();
            _rt.sizeDelta = new Vector2(28f, 28f);
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);

            _img = _go.AddComponent<Image>();
            _img.color = new Color(1f, 1f, 1f, 0.55f);
            _img.raycastTarget = false;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(_go.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0f, -22f);
            lrt.sizeDelta = new Vector2(80f, 28f);

            _label = labelGO.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 10f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.raycastTarget = false;
        }

        public void SetVisible(bool v) => _go.SetActive(v);
        public void SetPosition(float x, float y) => _rt.anchoredPosition = new Vector2(x, y);

        public void SetState(bool assigned, sbyte playerIndex)
        {
            if (assigned && playerIndex >= 0 && playerIndex < PlayerColors.Length)
            {
                var c = PlayerColors[playerIndex];
                c.a = 0.75f;
                _img.color = c;
                _label.text = $"P{playerIndex + 1}";
            }
            else
            {
                _img.color = new Color(1f, 1f, 1f, 0.45f);
                _label.text = "?";
            }
        }

        public void Destroy() { if (_go != null) Object.Destroy(_go); }
    }
}
