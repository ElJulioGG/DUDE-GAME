#if GRENADE_DEBUG
using UnityEditor;
using UnityEngine;

public static class PickupPrefabAudit
{
    [MenuItem("Tools/Audit/WeaponPickups")]
    public static void AuditWeaponPickups()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int inspected = 0;
        int issues = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var pickup = prefab.GetComponent<WeaponPickup>();
            if (pickup == null) continue;

            inspected++;

            var grenade = prefab.GetComponentInChildren<Grenade>(true);
            if (grenade != null)
            {
                issues++;
                Debug.LogWarning($"[PREFAB_AUDIT] WeaponPickup prefab '{path}' contains child Grenade component (id ref unavailable in edit mode). Remove live grenade references.", prefab);
            }
        }

        Debug.Log($"[PREFAB_AUDIT] Inspected {inspected} WeaponPickup prefabs. Issues found: {issues}.");
    }
}
#endif
