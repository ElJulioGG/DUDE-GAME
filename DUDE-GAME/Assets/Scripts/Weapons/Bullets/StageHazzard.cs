using System.Collections.Generic;
using UnityEngine;

public class StageHazzard : MonoBehaviour
{
    [SerializeField] private int damage = 100;
    [SerializeField] private Sprite[] bloodySprites;
    [SerializeField] private string spriteChildName = "Sprite";

    private SpriteRenderer baseRenderer;
    private readonly List<SpriteRenderer> spawnedLayers = new();
    private int killCount = 0;

    [SerializeField] private Color[] playerColors =
    {
        Color.red,
        Color.blue,
        Color.green,
        new(0.5f, 0f, 0.5f)   // purple
    };

    public int Damage => damage;

    private void Awake()
    {
        Transform spriteChild = transform.Find(spriteChildName);
        if (spriteChild != null)
            baseRenderer = spriteChild.GetComponent<SpriteRenderer>();
        else
            Debug.LogWarning($"StageHazzard on {gameObject.name}: child '{spriteChildName}' not found.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null) return;

            bool wasAlive = stats.playerAlive;
            stats.TakeDamage(damage);

            if (wasAlive && !stats.playerAlive)
                OnPlayerKilled(stats.playerIndex);

            return;
        }

        var dmg = other.GetComponentInParent<IDamageable>();
        dmg?.TakeDamage(damage);
    }

    private void OnPlayerKilled(int playerIndex)
    {
        if (bloodySprites == null || bloodySprites.Length == 0) return;

        Color color = playerIndex >= 0 && playerIndex < playerColors.Length
            ? playerColors[playerIndex]
            : Color.white;

        int layerIndex = killCount % bloodySprites.Length;

        if (killCount < bloodySprites.Length)
            SpawnLayer(layerIndex, color);
        else
            spawnedLayers[layerIndex].color = color;

        killCount++;
    }

    private void SpawnLayer(int index, Color color)
    {
        int baseOrder = baseRenderer != null ? baseRenderer.sortingOrder : 0;
        int baseSortingLayer = baseRenderer != null ? baseRenderer.sortingLayerID : 0;

        GameObject obj = new($"BloodyLayer_{index}");
        obj.transform.SetParent(baseRenderer.transform);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.transform.localScale = Vector3.one;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = bloodySprites[index];
        sr.sortingLayerID = baseSortingLayer;
        sr.sortingOrder = baseOrder + 1;
        sr.color = color;

        spawnedLayers.Add(sr);
    }
}
