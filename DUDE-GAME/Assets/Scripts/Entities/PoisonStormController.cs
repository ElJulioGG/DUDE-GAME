using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PoisonStormController : MonoBehaviour
{
    [Header("Referencia al centro de la zona segura")]
    [Tooltip("Centro del c�rculo seguro. Puede ser un Empty en el centro del mapa (por ejemplo, CenterScreenForCVCam1). " +
             "Si se deja vac�o, usar� la posici�n de este GameObject.")]
    [SerializeField] private Transform safeZoneCenter;

    [Header("Jugadores a los que se aplicar� el da�o")]
    [Tooltip("Lista de PlayerStats en la partida. Arrastra aqu� los 4 jugadores de la escena en el orden que quieras.")]
    [SerializeField] private PlayerStats[] players;

    [Header("Radio de la zona segura")]
    [Tooltip("Radio inicial del c�rculo seguro al comienzo de la partida (en unidades de mundo).")]
    [SerializeField] private float initialRadius = 25f;

    [Tooltip("Radio m�nimo al que puede llegar la zona segura.")]
    [SerializeField] private float finalRadius = 3f;

    [Header("Configuraci�n de la tormenta")]
    [Tooltip("�La tormenta comienza autom�ticamente cuando el GameController inicia el match?")]
    [SerializeField] private bool autoStartOnMatchBegin = true;

    [Tooltip("Tiempo de espera (en segundos) antes de que comience a cerrarse la tormenta despu�s de iniciar el match.")]
    [SerializeField] private float initialDelay = 10f;

    [Tooltip("Da�o que se aplica por cada tick a los jugadores que est�n FUERA de la zona segura.")]
    [SerializeField] private int damagePerTick = 5;

    [Tooltip("Tiempo entre aplicaciones de da�o (en segundos). Por ejemplo, 0.5 = dos veces por segundo.")]
    [SerializeField] private float tickInterval = 0.5f;

    [Tooltip("Curva que define c�mo se interpola entre radio actual y radio objetivo en cada fase.")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Fases de cierre de la tormenta")]
    [Tooltip("Lista de fases. Cada fase define un radio objetivo, cu�nto tarda en llegar a �l y cu�nto pausa antes de la siguiente fase.")]
    [SerializeField] private StormPhase[] phases;

    [System.Serializable]
    public class StormPhase
    {
        [Tooltip("Radio objetivo de la zona segura en esta fase.")]
        public float targetRadius = 15f;

        [Tooltip("Tiempo que tarda en reducirse desde el radio actual hasta el targetRadius.")]
        public float shrinkDuration = 10f;

        [Tooltip("Pausa despu�s de llegar al targetRadius antes de empezar la siguiente fase.")]
        public float pauseDuration = 3f;
    }

    // === Estado interno ===
    private float currentRadius;          // radio actual de la zona segura
    private int currentPhaseIndex = 0;    // fase actual
    private bool isStormRunning = false;  // si la tormenta est� activa
    private float damageTimer = 0f;       // acumulador para ticks de da�o

    private Coroutine stormRoutine;       // referencia a la coroutine de la tormenta

    // === PROPIEDADES P�BLICAS �TILES (solo lectura) ===
    public float CurrentRadius => currentRadius;
    public bool IsStormRunning => isStormRunning;

    private void Awake()
    {
        // Aseguramos un centro por defecto
        if (safeZoneCenter == null)
            safeZoneCenter = transform;

        // Inicializamos el radio con el valor inicial
        currentRadius = initialRadius;
    }

    /// <summary>
    /// Llamado por GameController cuando se resetea un match (NextMatch).
    /// Resetea estado interno de la tormenta.
    /// </summary>
    public void OnMatchReset()
    {
        // Detenemos la coroutine si estaba corriendo
        if (stormRoutine != null)
        {
            StopCoroutine(stormRoutine);
            stormRoutine = null;
        }

        // Reset total de variables
        currentRadius = initialRadius;
        currentPhaseIndex = 0;
        isStormRunning = false;
        damageTimer = 0f;
    }

    
    /// Llamado por GameController cuando el match comienza (MatchBegin).
        public void OnMatchStarted()
    {
        if (!autoStartOnMatchBegin)
            return;

        // Por si acaso venimos de otro match, reseteamos primero
        OnMatchReset();

        // Lanzamos la rutina principal de la tormenta
        stormRoutine = StartCoroutine(StormRoutine());
    }

    private IEnumerator StormRoutine()
    {
        // Espera inicial antes de que empiece a cerrar
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        isStormRunning = true;

        // Recorremos todas las fases
        for (currentPhaseIndex = 0; currentPhaseIndex < phases.Length; currentPhaseIndex++)
        {
            StormPhase phase = phases[currentPhaseIndex];

            float startRadius = currentRadius;
            float targetRadius = Mathf.Max(phase.targetRadius, finalRadius); // nunca menor que finalRadius
            float duration = Mathf.Max(phase.shrinkDuration, 0.01f);

            float t = 0f;

            // Fase de reducci�n del radio
            while (t < duration)
            {
                // Si el juego est� en pausa, no avanzamos el tiempo ni movemos la tormenta
                if (GameManager.instance != null && GameManager.instance.gamePaused)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / duration);
                float curveFactor = shrinkCurve.Evaluate(normalized);

                currentRadius = Mathf.Lerp(startRadius, targetRadius, curveFactor);

                yield return null;
            }

            // Aseguramos que el radio actual sea exactamente el target al terminar la fase
            currentRadius = targetRadius;

            // Pausa antes de la siguiente fase
            if (phase.pauseDuration > 0f)
            {
                float pauseT = 0f;
                while (pauseT < phase.pauseDuration)
                {
                    if (GameManager.instance != null && GameManager.instance.gamePaused)
                    {
                        yield return null;
                        continue;
                    }

                    pauseT += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // Cuando terminan todas las fases, dejamos la zona en el radio final
        currentRadius = Mathf.Max(finalRadius, currentRadius);
        isStormRunning = true; // sigue aplicando da�o aunque ya no se reduzca
        stormRoutine = null;
    }

    private void Update()
    {
        // Si no hay tormenta corriendo, no hacemos nada
        if (!isStormRunning)
            return;

        // Si el juego est� en pausa, no aplicar da�o
        if (GameManager.instance != null && GameManager.instance.gamePaused)
            return;

        // Acumulador para ticks de da�o
        damageTimer += Time.deltaTime;
        if (damageTimer >= tickInterval)
        {
            damageTimer -= tickInterval; // restamos, no ponemos a 0 para no acumular error
            ApplyStormDamage();
        }
    }

    /// <summary>
    /// Aplica da�o a todos los PlayerStats que est�n fuera del c�rculo seguro.
    /// </summary>
    private void ApplyStormDamage()
    {
        if (players == null || players.Length == 0)
            return;

        Vector2 center = safeZoneCenter != null ? (Vector2)safeZoneCenter.position : (Vector2)transform.position;

        foreach (var ps in players)
        {
            if (ps == null)
                continue;

            if (!ps.playerAlive)
                continue;

            if (!ps.gameObject.activeInHierarchy)
                continue;

            Vector2 pos = ps.transform.position;
            float dist = Vector2.Distance(pos, center);

            // Si est� fuera del c�rculo seguro, aplicamos da�o
            if (dist > currentRadius)
            {
                ps.TakeDamage(damagePerTick);
            }
        }
    }

    // === Solo para debug visual en el editor ===
    private void OnDrawGizmosSelected()
    {
        // Centro de la zona segura
        Vector3 center = safeZoneCenter != null ? safeZoneCenter.position : transform.position;

        // Color para el radio inicial
        Gizmos.color = new Color(0f, 0.6f, 0f, 0.4f);
        Gizmos.DrawWireSphere(center, initialRadius);

        // Color para el radio actual (si estamos en Play)
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(center, currentRadius);
        }

        // Color para el radio final m�nimo
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);
        Gizmos.DrawWireSphere(center, finalRadius);
    }
}
