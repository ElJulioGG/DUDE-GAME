using UnityEngine;
using FMODUnity;
public class FMODEvents : MonoBehaviour
{
    [field: Header("VoicesSFX")]
    [field: SerializeField] public EventReference VoiceSay3 { get; private set; }

    [field: Header("WeaponSFX")]
    [field: SerializeField] public EventReference Reload { get; private set; }

    [field: Header("PlayerSFX")]
    [field: SerializeField] public EventReference PlayerGetHit { get; private set; }

    public static FMODEvents Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
