using UnityEngine;

public class BulletBlackHoleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject blackHolePrefab;

    public void SpawnBlackHole()
    {
        if (blackHolePrefab == null) return;
        Instantiate(blackHolePrefab, transform.position, Quaternion.identity);
    }
}
