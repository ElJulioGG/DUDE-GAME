using TMPro;
using UnityEngine;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TMP_Text text;

    void Update()
    {
        if (playerStats != null && text != null)
        {
            text.text = $"P{playerStats.GetPlayerIndex() + 1} HP: {playerStats.GetPlayerHealth()}";
        }
    }
}
