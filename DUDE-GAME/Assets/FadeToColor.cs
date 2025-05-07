using UnityEngine;

public class FadeToColor : MonoBehaviour
{
    public SpriteRenderer grayscaleSprite; // la que está encima
    public float duration = 2f;

    private void Start()
    {
        StartCoroutine(FadeOutGray());
    }

    private System.Collections.IEnumerator FadeOutGray()
    {
        float elapsed = 0f;
        Color original = grayscaleSprite.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            grayscaleSprite.color = new Color(original.r, original.g, original.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        grayscaleSprite.gameObject.SetActive(false); // opcional
    }
}
