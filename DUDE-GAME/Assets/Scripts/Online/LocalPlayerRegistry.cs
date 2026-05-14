using UnityEngine;

/// <summary>
/// Stores which global player slots belong to THIS machine.
/// Phase 4 will wire PlayerInputHandler to query this and route input
/// through NetworkPlayerController instead of directly to PlayerMovement.
/// </summary>
public class LocalPlayerRegistry : MonoBehaviour
{
    public static LocalPlayerRegistry Instance { get; private set; }

    // Global indices assigned to this machine's local players (-1 = not assigned)
    public int GlobalIndex0 { get; private set; } = -1;
    public int GlobalIndex1 { get; private set; } = -1;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by NetworkGameManager when the server sends back the assignment
    public void SetAssignment(int index0, int index1)
    {
        GlobalIndex0 = index0;
        GlobalIndex1 = index1;
    }

    // True if this machine owns the given global player slot
    public bool OwnsSlot(int globalIndex) =>
        globalIndex == GlobalIndex0 || globalIndex == GlobalIndex1;

    // Which local device index (0 or 1) controls this global player?
    // Returns -1 if this machine doesn't own it.
    public int GetLocalDeviceIndex(int globalIndex)
    {
        if (globalIndex == GlobalIndex0) return 0;
        if (globalIndex == GlobalIndex1) return 1;
        return -1;
    }
}
