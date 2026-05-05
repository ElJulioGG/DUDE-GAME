using UnityEngine;

public class UIFollower : MonoBehaviour
{
    public RectTransform mainUIObject;      // El que tiene RectTransform (UI)
    public Transform followerObject;        // El que sigue (tiene Transform)
    public Vector3 worldOffset;             // Offset en coordenadas del mundo

    private Camera uiCamera;

    void Start()
    {
        // Usa la cámara principal o la de UI si tienes una específica
        uiCamera = Camera.main;
    }

    void Update()
    {
        if (mainUIObject == null || followerObject == null || uiCamera == null) return;

        // Convertir la posición de la UI (anchoredPosition) a pantalla, luego a mundo
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, mainUIObject.position);
        Vector3 worldPos = uiCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = followerObject.position.z; // Mantener la misma profundidad

        // Aplicar offset
        followerObject.position = worldPos + worldOffset;
    }
}
