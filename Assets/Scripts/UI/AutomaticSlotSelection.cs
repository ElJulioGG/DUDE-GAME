using UnityEngine;
using UnityEngine.UI;

public class AutomaticSlotSelection : MonoBehaviour
{
    void Start()
    {
        RectTransform artworkRect = transform.Find("artwork").GetComponent<RectTransform>();
        Vector2 originalSize = artworkRect.sizeDelta;

        // Random.Range upper bound is exclusive — no -1 needed
        int random = Random.Range(0, SmashCSS.instance.characters.Count);
        Character randomChar = SmashCSS.instance.characters[random];

        SmashCSS.instance.ShowCharacterInSlot(transform.GetSiblingIndex(), randomChar);

        artworkRect.sizeDelta = originalSize;
        artworkRect.sizeDelta *= randomChar.zoom;
    }
}
