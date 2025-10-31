#if GRENADE_DEBUG
using System.Collections;
using UnityEngine;

public class HotPotatoAutoTest : MonoBehaviour
{
    [Header("Test Setup")]
    [SerializeField] private Grenade grenadePrefab;
    [SerializeField] private GrenadeDefinition grenadeDefinition;
    [SerializeField] private Transform throwTarget;
    [SerializeField] private GrenadeWeapon[] participants;
    [SerializeField] [Range(1, 20)] private int roundsToRun = 8;
    [SerializeField] private float adoptDelay = 0.25f;
    [SerializeField] private float roundDelay = 0.75f;

    private void Start()
    {
        if (grenadePrefab == null || grenadeDefinition == null || participants == null || participants.Length == 0)
        {
            Debug.LogWarning("[HOTPOTATO_TEST] Missing setup (prefab/definition/participants). Test aborted.", this);
            return;
        }

        foreach (var weapon in participants)
        {
            weapon?.InitializeWeapon(true);
        }

        StartCoroutine(RunHotPotatoSequence());
    }

    private IEnumerator RunHotPotatoSequence()
    {
        for (int round = 1; round <= roundsToRun; round++)
        {
            Grenade initial = Instantiate(grenadePrefab, participants[0].transform.position, Quaternion.identity);
            initial.Init(grenadeDefinition, participants[0].OwnerPlayerIndex);
            initial.Arm();

            Vector2 throwDir = Vector2.right;
            if (throwTarget != null)
            {
                throwDir = ((Vector2)throwTarget.position - (Vector2)participants[0].transform.position).normalized;
            }

            initial.Throw(throwDir);
            int grenadeId = initial.Id;
            string defName = initial.Definition != null ? initial.Definition.name : "null";
            float startTime = Time.time;

            Grenade current = initial;

            int hops = Mathf.Min(participants.Length, 3);
            for (int i = 0; i < hops; i++)
            {
                if (current == null) break;
                var weapon = participants[i];
                if (weapon == null) continue;

                weapon.AdoptExistingGrenade(current);
                yield return new WaitForSeconds(adoptDelay);

                Vector2 dir = throwTarget != null
                    ? ((Vector2)throwTarget.position - (Vector2)weapon.transform.position).normalized
                    : weapon.transform.right;

                weapon.TryThrowCooked(dir);
                yield return new WaitForSeconds(adoptDelay);
            }

            while (current != null && current.CurrentState != Grenade.State.Exploded)
            {
                yield return null;
            }

            float elapsed = Time.time - startTime;
            string result = current == null ? "Destroyed" : "Exploded";
            Debug.Log($"[HOTPOTATO_TEST] round={round} grenadeId={grenadeId} def={defName} hops={hops} result={result}@{elapsed:F2}s");

            yield return new WaitForSeconds(roundDelay);
        }
    }
}
#endif
