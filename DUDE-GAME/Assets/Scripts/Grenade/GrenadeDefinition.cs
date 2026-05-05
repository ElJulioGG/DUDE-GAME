using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeDefinition", menuName = "Grenade/Definition")]
public class GrenadeDefinition : ScriptableObject
{
    [Header("Core")]
    public GrenadeEffect[] effects;

    [Tooltip("Segundos desde que se arma hasta que explota")]
    public float fuseSeconds = 2f;

    [Tooltip("Velocidad base del lanzamiento")]
    public float throwSpeed = 12f;

    [Tooltip("Aumento extra de velocidad (por carga, si la usas)")]
    public float maxExtraThrow = 6f;

    [Range(0f, 1f)] public float bounciness = 0.2f;
    [Range(0f, 1f)] public float friction = 0.4f;

    [Header("Per-Definition Explosion VFX")]
    [Tooltip("Prefab de VFX para ESTA granada (Sprite/Animator/PS). Déjalo vacío si no quieres VFX.")]
    public GameObject explosionPrefab;

    [Tooltip("Duración a destruir el VFX instanciado (segundos)")]
    public float explosionLifetime = 0.6f;

    [Tooltip("Si true, el VFX copia la sorting layer del sprite de la granada y le suma +1")]
    public bool matchSortingForExplosion = true;
}
