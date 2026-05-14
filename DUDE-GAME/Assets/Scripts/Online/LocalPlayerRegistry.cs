using UnityEngine;

/// <summary>
/// Stores which global player slots (0-3) belong to THIS machine after the server assigns them.
/// Supports up to 3 local players per machine.
/// </summary>
public class LocalPlayerRegistry : MonoBehaviour
{
    public static LocalPlayerRegistry Instance { get; private set; }

    // Global indices assigned to this machine's local players (-1 = not assigned)
    public int GlobalIndex0 { get; private set; } = -1;
    public int GlobalIndex1 { get; private set; } = -1;
    public int GlobalIndex2 { get; private set; } = -1;

    public int LocalPlayerCount
    {
        get
        {
            int n = 0;
            if (GlobalIndex0 >= 0) n++;
            if (GlobalIndex1 >= 0) n++;
            if (GlobalIndex2 >= 0) n++;
            return n;
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by NetworkGameManager when the server sends back the assignment
    public void SetAssignment(int index0, int index1, int index2)
    {
        GlobalIndex0 = index0;
        GlobalIndex1 = index1;
        GlobalIndex2 = index2;
    }

    // True if this machine owns the given global slot
    public bool OwnsSlot(int globalIndex) =>
        globalIndex == GlobalIndex0 ||
        globalIndex == GlobalIndex1 ||
        globalIndex == GlobalIndex2;

    // Which local device index (0, 1, or 2) controls this global player?
    // Returns -1 if this machine doesn't own it.
    public int GetLocalDeviceIndex(int globalIndex)
    {
        if (globalIndex == GlobalIndex0) return 0;
        if (globalIndex == GlobalIndex1) return 1;
        if (globalIndex == GlobalIndex2) return 2;
        return -1;
    }

    // The global index at a given local device position, or -1 if none
    public int GetGlobalIndex(int localDeviceIndex)
    {
        return localDeviceIndex switch
        {
            0 => GlobalIndex0,
            1 => GlobalIndex1,
            2 => GlobalIndex2,
            _ => -1,
        };
    }
}
