using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FireShaderTester : MonoBehaviour
{
    public float speed = 1f;
    public float noiseScale = 1f;
    public float alphaThreshold = 0.2f;
    public Color baseColor = new Color(1f, 0.3f, 0.1f, 1f);
    public Color flameColor = new Color(1f, 1f, 0.3f, 1f);

    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;

        ApplyValues();
    }

    void Update()
    {
        // Para hacer ajustes en tiempo real desde el Inspector
        ApplyValues();
    }

    void ApplyValues()
    {
        mat.SetFloat("_Speed", speed);
        mat.SetFloat("_NoiseScale", noiseScale);
        mat.SetFloat("_Threshold", alphaThreshold);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_FlameColor", flameColor);
    }
}
