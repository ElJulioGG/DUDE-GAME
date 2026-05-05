using UnityEngine;
using DG.Tweening;

public class WaveEffect : MonoBehaviour
{
    public Material mat;
    public float duration = 2f;
    public float targetRadius = 1.5f;
    public Vector2 waveOrigin = new Vector2(0.5f, 0.5f); // Centro (UV)

    void Start()
    {
        if (mat == null)
        {
            Debug.LogError("No material assigned!");
            return;
        }

        mat.SetVector("_WaveCenter", waveOrigin);
        mat.SetFloat("_WaveRadius", 0f);

        DOTween.To(() => 0f, r => mat.SetFloat("_WaveRadius", r), targetRadius, duration)
               .SetEase(Ease.OutSine);
    }
}
