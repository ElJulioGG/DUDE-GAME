using UnityEngine;
using UnityEngine.InputSystem; // Required for Gamepad support

public class UIStatsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] playerUIStats; // Size = 4
    [SerializeField] private GameObject spacingPanel;

    void Update()
    {
        int activeCount = 0;

        for (int i = 0; i < playerUIStats.Length; i++)
        {
            bool shouldBeActive = false;

            // Determine if this player should be shown
            switch (i)
            {
                case 0: shouldBeActive = GameManager.instance.player1Playable; break;
                case 1: shouldBeActive = GameManager.instance.player2Playable; break;
                case 2: shouldBeActive = GameManager.instance.player3Playable; break;
                case 3: shouldBeActive = GameManager.instance.player4Playable; break;
            }

            // Activate/deactivate the UI and assign proper sibling index if active
            playerUIStats[i].SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                playerUIStats[i].transform.SetSiblingIndex(activeCount);
                activeCount++;
            }
        }

        // Place spacingPanel at index 2 *among active elements*
        int targetIndex = Mathf.Min(activeCount, 2);
        spacingPanel.transform.SetSiblingIndex(targetIndex);
    }
}
