using UnityEngine;

// Fuerza la rotacion de MUNDO a la default de Unity (0,0,0) en todo momento:
// da igual que el padre rote, que el prefab se instancie rotado, o que alguien
// la cambie en el inspector. Ademas ancla el objeto al CENTRO de su padre mas
// un offset hacia arriba (en mundo) configurable en el inspector.
// [ExecuteAlways] hace que tambien aplique en modo edicion, no solo en Play.
[ExecuteAlways]
[DisallowMultipleComponent]
public class ObjRotationLock : MonoBehaviour
{
    [Tooltip("Distancia hacia ARRIBA (Y de mundo) desde el centro del padre")]
    [SerializeField] private float upOffset = 0f;

    void OnEnable()
    {
        // Al activarse/instanciarse: antes que Start, asi ni el primer frame se ve mal.
        Apply();
    }

    void LateUpdate()
    {
        // Cada frame, DESPUES de animaciones/tweens/padres: nadie lo puede mover ni rotar.
        Apply();
    }

    void Apply()
    {
        // (Comparar primero evita marcar el transform como sucio cuando ya esta bien.)
        if (transform.rotation != Quaternion.identity)
            transform.rotation = Quaternion.identity;

        if (transform.parent == null) return;
        Vector3 target = transform.parent.position + Vector3.up * upOffset;
        if (transform.position != target)
            transform.position = target;
    }
}
