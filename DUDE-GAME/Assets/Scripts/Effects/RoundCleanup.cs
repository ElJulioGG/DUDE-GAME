using UnityEngine;

// Destruye el objeto al pasar de ronda, usando el mismo flag que ya usan
// BulletBehaviour, GranadePin y BlackHoleEntity (GameController lo pone en
// true al terminar la ronda y en false al arrancar la siguiente).
// Se puede poner en un prefab o anadirse por codigo (AddComponent).
public class RoundCleanup : MonoBehaviour
{
    void Update()
    {
        if (GameManager.instance == null || GameManager.instance.destroyProyectiles)
            Destroy(gameObject);
    }
}
