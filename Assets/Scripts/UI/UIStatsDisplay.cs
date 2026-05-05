using UnityEngine;
using System.Collections.Generic;

public class UIStatsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] playerUIStats; // Size = 4
    [SerializeField] private GameObject spacingPanel;

    private readonly List<(int panelIndex, int displayOrder)> activePanels = new List<(int, int)>(4);

    void Update()
    {
        activePanels.Clear();

        for (int i = 0; i < playerUIStats.Length; i++)
        {
            bool shouldBeActive = false;
            int displayOrder = -1;

            switch (i)
            {
                case 0:
                    shouldBeActive = GameManager.instance.player1Playable;
                    displayOrder = GameManager.instance.player1DisplayOrder;
                    break;
                case 1:
                    shouldBeActive = GameManager.instance.player2Playable;
                    displayOrder = GameManager.instance.player2DisplayOrder;
                    break;
                case 2:
                    shouldBeActive = GameManager.instance.player3Playable;
                    displayOrder = GameManager.instance.player3DisplayOrder;
                    break;
                case 3:
                    shouldBeActive = GameManager.instance.player4Playable;
                    displayOrder = GameManager.instance.player4DisplayOrder;
                    break;
            }

            playerUIStats[i].SetActive(shouldBeActive);

            if (shouldBeActive)
                activePanels.Add((i, displayOrder));
        }

        // Sort by display order (device activation order) — lowest first = leftmost
        activePanels.Sort((a, b) => a.displayOrder.CompareTo(b.displayOrder));

        for (int i = 0; i < activePanels.Count; i++)
            playerUIStats[activePanels[i].panelIndex].transform.SetSiblingIndex(i);

        // Place spacingPanel at index 2 *among active elements*
        int targetIndex = Mathf.Min(activePanels.Count, 2);
        spacingPanel.transform.SetSiblingIndex(targetIndex);
    }
}
