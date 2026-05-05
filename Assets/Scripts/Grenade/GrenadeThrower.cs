using UnityEngine;

public class GrenadeThrower : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Grenade grenadePrefab;
    [SerializeField] private GrenadeDefinition definition;
    [SerializeField] private Transform hand;
    [SerializeField] private int ownerPlayerIndex = 0;

    [Header("Options")]
    [Tooltip("Crea automáticamente una granada cerrada en la mano al iniciar (pruebas).")]
    [SerializeField] private bool spawnClosedOnStart = true;

    private Grenade current; 
    private Vector2 aimDir = Vector2.right;

    private void Start()
    {
        if (spawnClosedOnStart)
            SpawnClosedInHand();
    }

    private void Update()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        aimDir = (mouseWorld - (Vector2)hand.position).normalized;

        if (Input.GetMouseButtonDown(0))
        {
            if (current == null)
            {
                SpawnClosedInHand();
            }
            else if (current.IsSafe)
            {
                current.Arm();
            }
            else if (current.IsArmed)
            {
                current.Throw(aimDir); 
                current = null;        
            }
        }
    }

    private void SpawnClosedInHand()
    {
        current = Instantiate(grenadePrefab, hand.position, Quaternion.identity, hand);
        current.Init(definition, ownerPlayerIndex);
    }
}
