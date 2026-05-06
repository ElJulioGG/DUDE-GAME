using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator anim;
    [SerializeField] private string animationName;

    private bool hasStarted = false;   // prevents spamming

    void Start()
    {
        //SoundFXManager.instance.PlayMusic("MainMenu", transform, 0.9f, 1f, true);
        AudioManager.Instance.SetMusicArea(MusicTracks.MENU);
    }

    public void Play()
    {
        if (hasStarted) return;     // << stop spamming
        hasStarted = true;

        //SoundFXManager.instance.StopMusic();
        //SoundFXManager.instance.PlaySoundByName("ButtonPress", transform, 1f, 1f, false);
        AudioManager.Instance.CleanUpMusic();
        PlayClickSound();
        

        StartCoroutine(PlayAnimationAndLoad());
    }

    public void ExitGame()
    {
        //SoundFXManager.instance.PlaySoundByName("ButtonPress", transform, 1f, 1f, false);
        PlayClickSound();
        Application.Quit();
    }

    public void PlayClickSound()
    {
        AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectStart, transform.position);
    }

    private IEnumerator PlayAnimationAndLoad()
    {
        anim.Play(animationName);
        //SoundFXManager.instance.PlaySoundByName("DoorSlideClose", transform, 1f, 1f, false);
        AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorClose, transform.position);

        yield return new WaitForEndOfFrame();

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        float clipLength = info.length;

        yield return new WaitForSeconds(clipLength);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
