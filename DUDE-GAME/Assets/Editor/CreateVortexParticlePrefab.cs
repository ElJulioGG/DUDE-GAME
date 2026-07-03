using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Crea Assets/Prefab/Particles/BlackHoleVortex.prefab: viento en espiral
// succionado hacia el centro desde todas las direcciones (para el black hole).
//
// USO: menu Tools > DUDE > Create BlackHole Vortex Prefab. Correlo UNA vez;
// despues afina el prefab en el inspector como cualquier particle system.
// Si lo vuelves a correr, SOBRESCRIBE el prefab con estos valores base.
//
// Como funciona el efecto:
//  - Shape Circle con radiusThickness bajo -> nacen cerca del borde, en 360.
//  - Velocity over Lifetime: radial NEGATIVO (succion al centro) + orbital Z
//    (giro) = trayectoria en espiral. speedModifier crece durante la vida ->
//    se aceleran al acercarse, como succion real.
//  - Renderer en modo Stretch -> cada particula se estira segun su velocidad,
//    dibujando "lineas de viento" en vez de puntos.
//  - Size/Color over Lifetime -> aparecen suaves, se encogen y desvanecen al
//    llegar al centro (el agujero se las "come").
public static class CreateVortexParticlePrefab
{
    private const string PrefabPath = "Assets/Prefab/Particles/BlackHoleVortex.prefab";

    [MenuItem("Tools/DUDE/Create BlackHole Vortex Prefab")]
    public static void Create()
    {
        var go = new GameObject("BlackHoleVortex");
        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null) ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.5f);
        main.startSpeed = 0f; // el movimiento lo pone Velocity over Lifetime
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new Color(0.78f, 0.87f, 1f, 0.9f); // azul-blanco tenue
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // sigue al agujero

        var emission = ps.emission;
        emission.rateOverTime = 90f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.5f;          // = radio "de captura" del viento
        shape.radiusThickness = 0.25f; // solo cerca del borde (0 = puro borde, 1 = todo el disco)

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalZ = 2.5f;   // giro alrededor del centro (rad/s); negativo = otro sentido
        vel.radial = -1.15f;   // succion hacia el centro (unidades/s)
        // Se acelera conforme es succionado (1x al nacer -> ~2.2x al final).
        vel.speedModifier = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 2.2f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.6f, 0.75f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),     // nace invisible
                new GradientAlphaKey(1f, 0.15f),  // aparece rapido
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)      // el centro se la traga
            });
        col.color = grad;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; // rayitas de viento
        renderer.lengthScale = 4f;                              // largo del trazo
        renderer.sharedMaterial = GetOrCreateVortexMaterial();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[CreateVortexParticlePrefab] Prefab creado/actualizado en {PrefabPath}");
    }

    // Repara SOLO el material del prefab ya existente (no toca tu tuning).
    // Para cuando el prefab quedo con el material built-in (bloques rosados en URP).
    [MenuItem("Tools/DUDE/Fix Vortex Particle Material")]
    public static void FixMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            Debug.LogError($"[CreateVortexParticlePrefab] No existe {PrefabPath}; usa primero Create.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var mat = GetOrCreateVortexMaterial();
        // Repara TODOS los renderers de particulas del prefab (por si anadiste
        // sub-emisores) y AMBOS slots: el de billboards y el de trails — el
        // modulo Trails usa su propio material y es el tipico que queda magenta.
        foreach (var r in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            r.sharedMaterial = mat;
            r.trailMaterial = mat;
        }
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[CreateVortexParticlePrefab] Material URP asignado (billboards + trails) al prefab del vortice.");
    }

    // Material URP para las particulas: Particles/Unlit transparente con la
    // textura suave default. El "defaultParticleMaterial" del pipeline puede
    // devolver el de built-in (shader incompatible -> magenta), por eso lo
    // creamos nosotros como asset junto al prefab.
    private static Material GetOrCreateVortexMaterial()
    {
        const string matPath = "Assets/Prefab/Particles/BlackHoleVortexMat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogError("[CreateVortexParticlePrefab] No se encontro el shader URP Particles/Unlit.");
            return null;
        }

        mat = new Material(shader);
        mat.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        // Superficie transparente (alpha blend), sin escribir profundidad.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
}
