using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Extensión sutil: sway de posición (en espacio cámara), roll (Z) y zoom (offset sobre baseline).
/// Compatible con CM3 (CinemachineCamera) y CM2 (CinemachineVirtualCamera) sin defines.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("Cinemachine/Extensions/Drunk Extension (Sway/Roll/Zoom)")]
public class DrunkCinemachineExtension : CinemachineExtension
{
    [Header("Runtime Offsets (los setea el controller)")]
    [SerializeField] private Vector2 swayOffset = Vector2.zero;  // unidades de mundo en ejes de cámara
    [SerializeField] private float rollDegrees = 0f;            // grados (Z)
    [SerializeField] private float zoomOffset = 0f;            // delta respecto al baseline

    [Header("Clamps")]
    public float maxRollAbs = 25f;
    public float maxZoomAbs = 3f;

    // === getters/setters que usa el controller ===
    public float SwayOffsetX { get => swayOffset.x; set => swayOffset.x = value; }
    public float SwayOffsetY { get => swayOffset.y; set => swayOffset.y = value; }
    public float RollDegrees { get => rollDegrees; set => rollDegrees = Mathf.Clamp(value, -maxRollAbs, maxRollAbs); }
    public float ZoomOffset { get => zoomOffset; set => zoomOffset = Mathf.Clamp(value, -maxZoomAbs, maxZoomAbs); }

    // baseline de lente (para que el zoom NO acumule)
    private bool _cachedBaseline;
    private float _baseOrthoSize = 10f;
    private float _baseFov = 60f;

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheLensBaseline();
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (!_cachedBaseline) CacheLensBaseline();

        // --- 1) SWAY en Body: offset en espacio de cámara ---
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 local = new Vector3(swayOffset.x, swayOffset.y, 0f);
            // evitar API de estado final: usar rotación actual del vcam (suficientemente correcta)
            Quaternion camRot = vcam != null ? vcam.transform.rotation : Quaternion.identity;
            Vector3 worldOffset = camRot * local;
            state.PositionCorrection += worldOffset;
        }

        // --- 2) ROLL en Aim (no acumulativo) ---
        if (stage == CinemachineCore.Stage.Aim)
        {
            float clamped = Mathf.Clamp(rollDegrees, -maxRollAbs, maxRollAbs);
            Quaternion rollQ = Quaternion.AngleAxis(clamped, Vector3.forward);
            state.OrientationCorrection = state.OrientationCorrection * rollQ;
        }

        // --- 3) ZOOM al final: baseline + offset, sin acumular ---
        if (stage == CinemachineCore.Stage.Finalize)
        {
            float z = Mathf.Clamp(zoomOffset, -maxZoomAbs, maxZoomAbs);

            // CM3
            var cm3 = vcam as CinemachineCamera;
            if (cm3 != null)
            {
                var lens = cm3.Lens;
                // no chequeamos modo: escribimos ambos campos
                lens.OrthographicSize = Mathf.Max(0.01f, _baseOrthoSize + z);
                lens.FieldOfView = Mathf.Clamp(_baseFov + z * 10f, 10f, 170f);
                cm3.Lens = lens;
            }

            // CM2 (obsoleto pero aún presente)
            var cm2 = vcam as CinemachineVirtualCamera;
            if (cm2 != null)
            {
                var lens = cm2.m_Lens;
                lens.OrthographicSize = Mathf.Max(0.01f, _baseOrthoSize + z);
                lens.FieldOfView = Mathf.Clamp(_baseFov + z * 10f, 10f, 170f);
                cm2.m_Lens = lens;
            }
        }
    }

    private void CacheLensBaseline()
    {
        _cachedBaseline = false;

        // Busca el componente en el mismo objeto (la extensión va en el mismo GO del vcam)
        var cm3 = GetComponent<CinemachineCamera>();
        if (cm3 != null)
        {
            var lens = cm3.Lens;
            _baseOrthoSize = lens.OrthographicSize;
            _baseFov = lens.FieldOfView;
            _cachedBaseline = true;
            return;
        }

        var cm2 = GetComponent<CinemachineVirtualCamera>();
        if (cm2 != null)
        {
            var lens = cm2.m_Lens;
            _baseOrthoSize = lens.OrthographicSize;
            _baseFov = lens.FieldOfView;
            _cachedBaseline = true;
        }
    }
}
