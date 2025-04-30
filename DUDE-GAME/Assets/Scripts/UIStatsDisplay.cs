using UnityEngine;
using UnityEngine.InputSystem; // Required for Gamepad support

public class UIStatsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] playerUIStats;

    void Update()
    {
        // Get connected gamepads (limit to 4 max)
        int connectedGamepads = Mathf.Min(Gamepad.all.Count, 4);

        // Enable only the UI stats for connected gamepads
        for (int i = 0; i < playerUIStats.Length; i++)
        {
            playerUIStats[i].SetActive(i < connectedGamepads);
        }

        // Optional debug log
        //Debug.Log($"Connected Gamepads (max 4): {connectedGamepads}");
    }
}
