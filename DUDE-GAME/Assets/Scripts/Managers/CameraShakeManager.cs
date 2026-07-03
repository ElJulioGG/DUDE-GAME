using System.Collections.Generic;
using UnityEngine;

// CinemachineBrain (CM3) runs at +100 — we must run after it.
[DefaultExecutionOrder(200)]
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    private struct ShakeInstance
    {
        public float strength;
        public float duration;
        public float elapsed;
    }

    private readonly List<ShakeInstance> _shakes = new();

    // Continuous sources (e.g. a lingering black hole): no duration/decay.
    // The owner re-sets its strength every frame (values are cheap to overwrite)
    // and MUST remove itself when it dies. Keyed by owner so sources stack.
    private readonly Dictionary<object, float> _continuous = new();

    public void SetContinuousShake(object source, float strength)
    {
        _continuous[source] = Mathf.Max(0f, strength);
    }

    public void RemoveContinuousShake(object source)
    {
        _continuous.Remove(source);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Shake(float strength, float duration)
    {
        if (duration <= 0f || strength <= 0f) return;
        _shakes.Add(new ShakeInstance { strength = strength, duration = duration, elapsed = 0f });
    }

    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || (_shakes.Count == 0 && _continuous.Count == 0)) return;

        Vector3 offset = Vector3.zero;

        foreach (float strength in _continuous.Values)
        {
            if (strength <= 0f) continue;
            float a = Random.Range(0f, Mathf.PI * 2f);
            offset.x += Mathf.Cos(a) * strength;
            offset.y += Mathf.Sin(a) * strength;
        }

        for (int i = _shakes.Count - 1; i >= 0; i--)
        {
            var s = _shakes[i];
            s.elapsed += Time.deltaTime;

            if (s.elapsed >= s.duration)
            {
                _shakes.RemoveAt(i);
                continue;
            }

            _shakes[i] = s;

            float decay = 1f - (s.elapsed / s.duration);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            offset.x += Mathf.Cos(angle) * s.strength * decay;
            offset.y += Mathf.Sin(angle) * s.strength * decay;
        }

        cam.transform.position += offset;
    }
}
