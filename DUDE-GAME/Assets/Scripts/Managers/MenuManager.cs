using UnityEngine;

public class MenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundFXManager.instance.PlayMusic("BattleTheme", gameObject.transform, 0.7f, 1f, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySoundLmao()
    {
        SoundFXManager.instance.PlaySoundByName("Pablo",gameObject.transform, 1f, 1f, false);
    }
}
