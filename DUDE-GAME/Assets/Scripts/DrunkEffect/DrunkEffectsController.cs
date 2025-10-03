// DrunkEffectsController.cs
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class DrunkEffectsController : MonoBehaviour
{
    [Header("Refs")]
    public DrunkCinemachineExtension cmDrunk;   // arrastra aquí la extensión del VCam

    [Header("SFX (nombres del SoundFXManager)")]
    public string sfxEnter1 = "Hic";
    public string sfxEnter2 = "Wah";
    public string sfxEnter3 = "Glug";
    public string sfxExit = "Burp";
    [Range(0f, 1f)] public float sfxVol = 0.8f;

    [Header("Estado 1 – Sway + Roll (sutil)")]
    public float swayAmpX = 0.35f;    // unidades de mundo
    public float swayAmpY = 0.20f;
    public float swayPeriod = 2.2f;   // seg por ciclo
    public float rollDegrees = 3.5f;  // grados

    [Header("Estado 2 – Breathing (zoom rítmico)")]
    public float zoomDelta = 0.35f;
    public float zoomPeriod = 2.4f;

    [Header("Estado 3 – World Tilt (impulso/relax suave)")]
    public float tiltDegrees = 12f;        // pico
    public float tiltDuration = 1.2f;      // ida+vuelta
    public AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Coroutines
    private Coroutine _coSway;
    private Coroutine _coBreath;
    private Coroutine _coTilt;

    void Reset()
    {
        if (!cmDrunk)
        {
#if CINEMACHINE_3_0_0_OR_NEWER
            var vcam = FindFirstObjectByType<CinemachineCamera>();
#else
            var vcam = FindFirstObjectByType<CinemachineVirtualCamera>();
#endif
            if (vcam) cmDrunk = vcam.GetComponent<DrunkCinemachineExtension>();
        }
    }

    // ==== API pedida por tu Hotkeys ====
    public void ActivateSway()
    {
        Play(sfxEnter1);
        StopCoroutineSafe(ref _coSway);
        _coSway = StartCoroutine(CoSway());
    }

    public void ActivateBreathing()
    {
        Play(sfxEnter2);
        StopCoroutineSafe(ref _coBreath);
        _coBreath = StartCoroutine(CoBreathing());
    }

    public void ActivateWorldTilt()
    {
        Play(sfxEnter3);
        StopCoroutineSafe(ref _coTilt);
        _coTilt = StartCoroutine(CoWorldTilt());
    }

    public void StopAllEffects()
    {
        StopCoroutineSafe(ref _coSway);
        StopCoroutineSafe(ref _coBreath);
        StopCoroutineSafe(ref _coTilt);
        ResetOffsets();
        Play(sfxExit);
    }

    // ==== Coroutines ====
    private IEnumerator CoSway()
    {
        if (!cmDrunk) yield break;

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float ang = t * (Mathf.PI * 2f / swayPeriod);

            cmDrunk.SwayOffsetX = Mathf.Sin(ang) * swayAmpX;
            cmDrunk.SwayOffsetY = Mathf.Sin(ang * 0.5f + 0.7f) * swayAmpY; // fase diferente
            cmDrunk.RollDegrees = Mathf.Sin(ang * 0.7f) * rollDegrees;

            yield return null;
        }
    }

    private IEnumerator CoBreathing()
    {
        if (!cmDrunk) yield break;

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float ang = t * (Mathf.PI * 2f / zoomPeriod);
            cmDrunk.ZoomOffset = Mathf.Sin(ang) * zoomDelta;
            yield return null;
        }
    }

    private IEnumerator CoWorldTilt()
    {
        if (!cmDrunk) yield break;

        float half = Mathf.Max(0.01f, tiltDuration * 0.5f);

        // ida
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            cmDrunk.RollDegrees = Mathf.Lerp(0f, tiltDegrees, tiltCurve.Evaluate(k));
            yield return null;
        }

        // vuelta
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            cmDrunk.RollDegrees = Mathf.Lerp(tiltDegrees, 0f, tiltCurve.Evaluate(k));
            yield return null;
        }

        cmDrunk.RollDegrees = 0f;
        _coTilt = null;
    }

    // ==== Helpers ====
    private void StopCoroutineSafe(ref Coroutine co)
    {
        if (co != null) { StopCoroutine(co); co = null; }
    }

    private void ResetOffsets()
    {
        if (!cmDrunk) return;
        cmDrunk.SwayOffsetX = 0f;
        cmDrunk.SwayOffsetY = 0f;
        cmDrunk.RollDegrees = 0f;
        cmDrunk.ZoomOffset = 0f;
    }

    private void Play(string name)
    {
        if (SoundFXManager.instance != null && !string.IsNullOrEmpty(name))
            SoundFXManager.instance.PlaySoundByName(name, transform, sfxVol, 1f, false);
    }
}
