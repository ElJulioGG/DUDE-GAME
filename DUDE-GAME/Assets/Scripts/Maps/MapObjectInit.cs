using UnityEngine;

/// <summary>
/// Manages map objects between rounds using SetActive — never Instantiate.
///
/// WHY: Calling Instantiate() on a GameObject that contains a NetworkObject duplicates
/// its serialized SceneId. FishNet uses SceneIds to reconcile scene objects between
/// server and client, so duplicates cause "[object] have the same sceneId" errors
/// and break multiplayer spawning. SetActive avoids this entirely.
///
/// WeaponBox handles its own network reset via a SyncVar when re-enabled.
/// </summary>
public class MapObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject[] originalSceneObjects;

    private Vector3[]    _initialPositions;
    private Quaternion[] _initialRotations;
    private bool         _cacheReady;

    private void Awake()
    {
        CacheInitialTransforms();
        SetObjectsActive(false);
    }

    private void CacheInitialTransforms()
    {
        if (_cacheReady) return;
        _initialPositions = new Vector3[originalSceneObjects.Length];
        _initialRotations = new Quaternion[originalSceneObjects.Length];
        for (int i = 0; i < originalSceneObjects.Length; i++)
        {
            if (originalSceneObjects[i] == null) continue;
            _initialPositions[i] = originalSceneObjects[i].transform.position;
            _initialRotations[i] = originalSceneObjects[i].transform.rotation;
        }
        _cacheReady = true;
    }

    private void SetObjectsActive(bool active)
    {
        if (!_cacheReady) CacheInitialTransforms();
        for (int i = 0; i < originalSceneObjects.Length; i++)
        {
            if (originalSceneObjects[i] == null) continue;
            if (active)
            {
                originalSceneObjects[i].transform.position = _initialPositions[i];
                originalSceneObjects[i].transform.rotation = _initialRotations[i];
            }
            originalSceneObjects[i].SetActive(active);
        }
    }

    private void OnEnable()  => SetObjectsActive(true);
    private void OnDisable() => SetObjectsActive(false);
}
