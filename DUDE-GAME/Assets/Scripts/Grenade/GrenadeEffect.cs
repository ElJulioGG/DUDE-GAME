using UnityEngine;

public abstract class GrenadeEffect : ScriptableObject
{
    // Se ejecuta al detonar.
    public abstract void ApplyEffect(Vector2 center, int ownerPlayerIndex);
}
