using UnityEngine;

[CreateAssetMenu(fileName = "GE_Teleport", menuName = "Grenades/Effects/Teleport")]
public class GE_Teleport : GrenadeEffect
{
    [Header("Placement")]
    [Tooltip("Pequeño offset para no quedar incrustado en el suelo/pared")]
    public Vector2 offset = Vector2.up * 0.2f;

    [Tooltip("Radio mínimo libre alrededor del jugador para considerar un lugar 'seguro'")]
    public float clearanceRadius = 0.35f;

    [Tooltip("Capas sólidas (paredes/suelo) que bloquean el spawn")]
    public LayerMask obstacleMask;

    [Tooltip("Si true, intenta ajustar a un punto cercano cuando el centro está bloqueado")]
    public bool useSafePlacement = true;

    [Tooltip("Pasos radiales para buscar ubicación segura (si useSafePlacement)")]
    public int ringSteps = 12;

    [Tooltip("Anillos a explorar alrededor (si useSafePlacement)")]
    public int ringCount = 3;

    [Tooltip("Separación entre anillos en unidades")]
    public float ringStepDistance = 0.25f;

    [Header("Movimiento")]
    [Tooltip("Si false, anula la velocidad al teletransportar")]
    public bool keepVelocity = false;

    [Tooltip("Si true, solo teletransporta si el dueño está vivo")]
    public bool requireOwnerAlive = true;

    public override void ApplyEffect(Vector2 center, int ownerPlayerIndex)
    {
        // Busca al dueño por playerIndex
        var players = Object.FindObjectsOfType<PlayerStats>();
        PlayerStats owner = null;
        foreach (var p in players)
        {
            if (p.GetPlayerIndex() == ownerPlayerIndex)
            {
                owner = p;
                break;
            }
        }
        if (owner == null) return;
        if (requireOwnerAlive && !owner.playerAlive) return;

        var rb = owner.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Posición objetivo
        Vector2 target = center + offset;

        // Si pedimos ubicación segura, ajustamos si está bloqueado
        if (useSafePlacement && Physics2D.OverlapCircle(target, clearanceRadius, obstacleMask) != null)
        {
            // Explora en anillos concéntricos
            for (int ring = 1; ring <= ringCount; ring++)
            {
                float radius = ring * ringStepDistance;
                for (int i = 0; i < ringSteps; i++)
                {
                    float ang = (i / (float)ringSteps) * Mathf.PI * 2f;
                    Vector2 cand = target + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                    if (!Physics2D.OverlapCircle(cand, clearanceRadius, obstacleMask))
                    {
                        target = cand;
                        ring = ringCount + 1; // break both loops
                        break;
                    }
                }
            }
        }

        // Teletransportar
        rb.position = target;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        if (!keepVelocity) rb.linearVelocity = Vector2.zero;
    }
}
