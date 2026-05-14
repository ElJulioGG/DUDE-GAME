using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row in the public lobby browser list.
/// Assign this to the lobbyRowPrefab used by OnlineMenuUI.
///
/// Prefab structure:
///   LobbyRow (this script + Button)
///   ├── NameText     (TextMeshProUGUI)
///   ├── CountText    (TextMeshProUGUI)   e.g. "2 / 4"
///   └── JoinButton   (Button)
/// </summary>
public class LobbyRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button          joinButton;

    private LobbyInfo _info;
    private Action<LobbyInfo> _onJoin;

    public void Initialize(LobbyInfo info, Action<LobbyInfo> onJoin)
    {
        _info   = info;
        _onJoin = onJoin;

        if (nameText)  nameText.text  = string.IsNullOrEmpty(info.Name) ? "Unknown" : info.Name;
        if (countText) countText.text = $"{info.CurrentMembers} / {info.MaxMembers}";

        if (joinButton)
        {
            joinButton.interactable = info.HasOpenSlot;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => _onJoin?.Invoke(_info));
        }
    }
}
