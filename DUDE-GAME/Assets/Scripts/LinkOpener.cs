using UnityEngine;

public class LinkOpener : MonoBehaviour
{
   public void OpenFacebook()
    {
        Application.OpenURL("https://facebook.com/");
    }

    public void OpenYoutube()
    {
        Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }
}
