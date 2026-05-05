using UnityEngine;

public class MapObjectManager : MonoBehaviour
{
    [Header("Object Management")]
    [SerializeField] private GameObject[] originalSceneObjects; // Drag your scene objects here
    [SerializeField] private bool hideOriginalsOnStart = true;

    private GameObject[] objectCopies;
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeObjectCopies();
    }

    private void InitializeObjectCopies()
    {
        if (isInitialized) return;

        // Initialize arrays
        objectCopies = new GameObject[originalSceneObjects.Length];
        originalPositions = new Vector3[originalSceneObjects.Length];
        originalRotations = new Quaternion[originalSceneObjects.Length];

        for (int i = 0; i < originalSceneObjects.Length; i++)
        {
            if (originalSceneObjects[i] == null) continue;

            // Save original transform data
            originalPositions[i] = originalSceneObjects[i].transform.position;
            originalRotations[i] = originalSceneObjects[i].transform.rotation;

            // Create copy
            objectCopies[i] = Instantiate(
                originalSceneObjects[i],
                originalPositions[i],
                originalRotations[i],
                transform
            );

            // Name it for identification
            objectCopies[i].name = $"{originalSceneObjects[i].name}_Copy";

            // Hide original if requested
            if (hideOriginalsOnStart)
            {
                originalSceneObjects[i].SetActive(false);
            }
        }

        isInitialized = true;
    }

    public void SetMapActive(bool active)
    {
        if (!isInitialized) InitializeObjectCopies();

        for (int i = 0; i < objectCopies.Length; i++)
        {
            if (originalSceneObjects[i] == null) continue;

            if (active)
            {
                // If copy was destroyed, recreate it
                if (objectCopies[i] == null)
                {
                    objectCopies[i] = Instantiate(
                        originalSceneObjects[i],
                        originalPositions[i],
                        originalRotations[i],
                        transform
                    );
                    objectCopies[i].name = $"{originalSceneObjects[i].name}_Copy";
                }

                objectCopies[i].SetActive(true);
            }
            else
            {
                if (objectCopies[i] != null)
                {
                    objectCopies[i].SetActive(false);
                }
            }
        }
    }

    private void OnEnable()
    {
        SetMapActive(true);
    }

    private void OnDisable()
    {
        SetMapActive(false);
    }

    // Cleanup when this object is destroyed
    private void OnDestroy()
    {
        // Destroy all copies
        if (objectCopies != null)
        {
            foreach (var copy in objectCopies)
            {
                if (copy != null) Destroy(copy);
            }
        }

        // Reactivate originals if we hid them
        if (hideOriginalsOnStart && originalSceneObjects != null)
        {
            foreach (var original in originalSceneObjects)
            {
                if (original != null) original.SetActive(true);
            }
        }
    }
}