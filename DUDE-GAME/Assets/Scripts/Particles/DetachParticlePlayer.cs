using System.Collections.Generic;
using UnityEngine;

// Evita que las particulas de este objeto desaparezcan en el aire cuando el
// objeto se DESACTIVA, sin que nadie tenga que llamar nada antes.
//
// Truco: las particulas NUNCA son hijas del objeto. En Start se sueltan al
// mundo de forma permanente y este script las "sigue" cada frame como si
// fueran hijas. Como Unity solo apaga a los HIJOS de un objeto desactivado:
//  - Al desactivar (OnDisable): solo dejan de emitir; las vivas se desvanecen.
//  - Al reactivar (OnEnable): se recolocan y vuelven a emitir.
//  - Al destruir de verdad (OnDestroy): se auto-limpian via ParticleSelfDestruct.
public class DetachParticlePlayer : MonoBehaviour
{
    [Tooltip("Sistemas de particulas hijos. Si lo dejas VACIO, se llena solo con los hijos directos al iniciar.")]
    [SerializeField] private ParticleSystem[] particleSystems;

    // Pose local original de cada particula (donde vivia dentro del padre).
    private Vector3[] _localPos;
    private Quaternion[] _localRot;
    private bool _detached; // ya se soltaron (Start corrio)

    void Start()
    {
        // Auto-llenado: todos los ParticleSystem en hijos DIRECTOS del padre.
        if (particleSystems == null || particleSystems.Length == 0)
        {
            var found = new List<ParticleSystem>();
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out ParticleSystem ps))
                    found.Add(ps);
            }
            particleSystems = found.ToArray();
        }

        _localPos = new Vector3[particleSystems.Length];
        _localRot = new Quaternion[particleSystems.Length];
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null) continue;
            _localPos[i] = particleSystems[i].transform.localPosition;
            _localRot[i] = particleSystems[i].transform.localRotation;
            // Soltado PERMANENTE (en Start: Unity no permite tocar la jerarquia
            // durante la activacion/desactivacion, por eso no va en OnEnable/OnDisable).
            particleSystems[i].transform.SetParent(null, true);
        }
        _detached = true;
    }

    // Las mantiene pegadas al padre como si fueran hijas (posicion y rotacion).
    void LateUpdate() => Follow();

    private void Follow()
    {
        if (!_detached) return;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            if (ps == null) continue;
            ps.transform.SetPositionAndRotation(
                transform.TransformPoint(_localPos[i]),
                transform.rotation * _localRot[i]);
        }
    }

    void OnEnable()
    {
        if (!_detached) return; // primera activacion: aun son hijas, no hay nada que hacer
        Follow(); // recoloca por si el padre se movio mientras estaba apagado
        foreach (var ps in particleSystems)
            if (ps != null) ps.Play(true);
    }

    void OnDisable()
    {
        if (!_detached) return;
        // Solo dejar de emitir: como NO son hijas, no se apagan con el padre y
        // las particulas vivas terminan su vida normalmente.
        foreach (var ps in particleSystems)
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void OnDestroy()
    {
        // Muerte real del dueno: que las particulas sueltas se limpien solas.
        // (Si la escena se esta descargando, Unity las destruye de todos modos
        // y no permite AddComponent en ese momento.)
        if (!_detached || !gameObject.scene.isLoaded) return;
        foreach (var ps in particleSystems)
            if (ps != null) ps.gameObject.AddComponent<ParticleSelfDestruct>();
    }
}
