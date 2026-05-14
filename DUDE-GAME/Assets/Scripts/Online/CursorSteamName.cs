using Steamworks;
using TMPro;
using UnityEngine;

/// <summary>
/// Place this on the same GameObject as PlayerCursor (or a child of it).
/// It shows the local Steam display name below the cursor on the CSS screen.
///
/// All cursors on the same machine show the same name because they share one
/// Steam account. Two couch players on one PC are the same Steam user.
///
/// Requires a TextMeshProUGUI reference — create a TMP Text as a child of the
/// cursor prefab, position it below the cursor image, and assign it here.
/// </summary>
[RequireComponent(typeof(PlayerCursor))]
public class CursorSteamName : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;

    private void Start()
    {
        if (nameLabel == null)
        {
            Debug.LogWarning("[CursorSteamName] No nameLabel assigned.");
            return;
        }

        if (!SteamManager.Initialized)
        {
            nameLabel.text = string.Empty;
            return;
        }

        nameLabel.text = SteamFriends.GetPersonaName();
    }
}
