using UnityEngine;

// Ponlo en una SEGUNDA camara (hija de la principal). Renderiza la escena a un
// RenderTexture y lo publica como textura global _SceneTex para que el shader
// del agujero (BlackHole2D_URP_RT) la lea y deforme TODO lo que hay detras.
//
// SETUP:
//  - Crea una camara hija de tu Main Camera (misma posicion/rotacion).
//  - En esa camara: Culling Mask = Everything EXCEPTO la capa del agujero ("BlackHole").
//    (asi no se muestrea a si misma y no hay feedback).
//  - Depth MENOR que la Main Camera (se dibuja antes).
//  - Clear Flags / Background iguales a tu Main Camera.
//  - Arrastra tu Main Camera al campo Source Camera.
[RequireComponent(typeof(Camera))]
[ExecuteAlways] // tambien en modo edicion, para que la aux nunca aparezca en Game view
public class BlackHoleSceneCapture : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera; // tu Main Camera
    [Range(0.25f, 1f)]
    [SerializeField] private float resolutionScale = 1f; // baja a 0.5 si quieres ahorrar

    private Camera _cam;
    private RenderTexture _rt;
    private static readonly int SceneTexID = Shader.PropertyToID("_SceneTex");

    // Captura activa (la haya puesto a mano en la escena o la haya creado EnsureExists).
    public static BlackHoleSceneCapture Active { get; private set; }

    public void SetSource(Camera c) => sourceCamera = c;

    // Garantiza que exista una captura en CUALQUIER escena donde se instancie un agujero.
    // Si ya hay una (puesta a mano o creada antes), no hace nada. Llamar desde BlackHoleEntity.
    public static void EnsureExists()
    {
        if (Active != null) return;            // ya hay una viva (== de Unity ignora destruidas)
        var main = Camera.main;
        if (main == null) return;              // sin MainCamera no hay nada que capturar

        var go = new GameObject("BlackHoleSceneCapture (auto)");
        go.transform.SetParent(main.transform, false); // hija de la Main, en su misma pose

        var cam = go.AddComponent<Camera>();
        cam.CopyFrom(main);
        // Excluye la capa del agujero (feedback) y la UI (no deformar el HUD).
        int exclude = 0;
        int bh = LayerMask.NameToLayer("BlackHole"); if (bh >= 0) exclude |= 1 << bh;
        int ui = LayerMask.NameToLayer("UI");        if (ui >= 0) exclude |= 1 << ui;
        cam.cullingMask = main.cullingMask & ~exclude;

        var cap = go.AddComponent<BlackHoleSceneCapture>(); // su OnEnable setea Active y Alloc()
        cap.SetSource(main);
    }

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        if (sourceCamera == null) sourceCamera = Camera.main; // autocompleta si no se asigno
        Active = this;
        Alloc();
    }

    void OnDisable()
    {
        if (Active == this) Active = null;
        Release();
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();

        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));
        if (_rt == null || _rt.width != w || _rt.height != h)
        {
            Release();
            Alloc();
        }

        // CLAVE: esta camara SIEMPRE debe renderizar a su RenderTexture, nunca a pantalla.
        // Si algo se la quita (modo edicion, reimport, etc.) se la reasignamos cada frame,
        // asi nunca veras "solo la camara aux".
        if (_rt != null && _cam.targetTexture != _rt)
            _cam.targetTexture = _rt;

        // Sigue el zoom/proyeccion de la camara principal y se queda DETRAS en depth.
        if (sourceCamera != null)
        {
            _cam.orthographic     = sourceCamera.orthographic;
            _cam.orthographicSize = sourceCamera.orthographicSize;
            _cam.fieldOfView      = sourceCamera.fieldOfView;
            _cam.depth            = sourceCamera.depth - 1; // se dibuja antes que la Main
        }
    }

    void Alloc()
    {
        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));
        _rt = new RenderTexture(w, h, 0, RenderTextureFormat.Default) { name = "BlackHoleSceneRT" };
        _cam.targetTexture = _rt;
        Shader.SetGlobalTexture(SceneTexID, _rt);
    }

    void Release()
    {
        if (_cam != null) _cam.targetTexture = null;
        if (_rt != null)
        {
            _rt.Release();
            // DestroyImmediate fuera de Play ([ExecuteAlways]); Destroy en runtime.
            if (Application.isPlaying) Destroy(_rt); else DestroyImmediate(_rt);
            _rt = null;
        }
    }
}
