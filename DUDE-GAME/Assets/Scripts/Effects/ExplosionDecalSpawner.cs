using UnityEngine;

// Va en el sprite de la explosion (el que se instancia al explotar la granada).
// Al aparecer, deja el decal de explosion (mancha/quemadura) en el suelo,
// SUELTO en la escena (no como hijo), para que sobreviva cuando el sprite de
// la explosion se destruya. El decal se limpia solo al pasar de ronda via
// RoundCleanup (mismo patron que balas / granadas / black holes).
public class ExplosionDecalSpawner : MonoBehaviour
{
    [Header("Decal")]
    [Tooltip("Prefab de la mancha que queda en el suelo (SpriteRenderer con su sorting layer por DEBAJO de los jugadores)")]
    [SerializeField] private GameObject decalPrefab;

    [Header("Variacion (para que no se vean clonadas)")]
    [SerializeField] private bool randomRotation = true;
    [Tooltip("Escala aleatoria min/max multiplicada sobre la del prefab")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.9f, 1.15f);

    void Start()
    {
        if (decalPrefab == null)
        {
            Debug.LogWarning("[ExplosionDecalSpawner] Falta asignar decalPrefab en el prefab de la explosion.", this);
            return;
        }

        Quaternion rot = randomRotation
            ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
            : Quaternion.identity;

        var decal = Instantiate(decalPrefab, transform.position, rot);
        decal.transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);

        // Limpieza por ronda sin tener que tocar el prefab del decal.
        if (decal.GetComponent<RoundCleanup>() == null)
            decal.AddComponent<RoundCleanup>();
    }
}
