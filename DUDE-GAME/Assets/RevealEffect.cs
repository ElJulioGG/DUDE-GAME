using UnityEngine;

public class RevealEffect : MonoBehaviour
{
    public Material material;
    public float speed = 0.5f;
    public float radius = 0f;

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            radius += Time.deltaTime * speed;
            radius = Mathf.Clamp01(radius);
            material.SetFloat("_Radius", radius);
        }
    }
}
