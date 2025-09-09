using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeDefinition", menuName = "Grenades/Grenade Definition")]
public class GrenadeDefinition : ScriptableObject
{
    [Header("Fuse")]
    public bool canCook = true;
    public float fuseSeconds = 1.2f;

    [Header("Throw")]
    public float throwSpeed = 12f;
    public float maxExtraThrow = 6f; 

    [Header("Physics")]
    public float bounciness = 0.6f;
    public float friction = 0.2f;

    [Header("Effects (en orden)")]
    public List<GrenadeEffect> effects = new();
}
