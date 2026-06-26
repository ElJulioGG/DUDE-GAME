using UnityEngine;
using FMODUnity;
public class FMODEvents : MonoBehaviour
{
    [field: Header("VoicesSFX")]
    [field: SerializeField] public EventReference VoiceSay3 { get; private set; }
    [field: SerializeField] public EventReference VoiceSay2 { get; private set; }
    [field: SerializeField] public EventReference VoiceSay1 { get; private set; }
    [field: SerializeField] public EventReference VoiceSayFight { get; private set; }
    [field: SerializeField] public EventReference VoiceInstakill { get; private set; }
    [field: SerializeField] public EventReference VoiceDoublePoints { get; private set; }
    [field: SerializeField] public EventReference VoicePablo { get; private set; }


    [field: Header("WeaponSFX")]
    [field: SerializeField] public EventReference ReloadShotGun { get; private set; }
    [field: SerializeField] public EventReference ReloadPNK { get; private set; }
    [field: SerializeField] public EventReference ReloadBH { get; private set; }
    [field: SerializeField] public EventReference ReloadRailGun { get; private set; }
    [field: SerializeField] public EventReference EmptyMag { get; private set; }
    [field: SerializeField] public EventReference BHBounce { get; private set; }
    [field: SerializeField] public EventReference Throw { get; private set; }
    [field: SerializeField] public EventReference SwingPunch { get; private set; }
    [field: SerializeField] public EventReference GranadePin { get; private set; }
    [field: SerializeField] public EventReference GranadeBeep { get; private set; }
    [field: SerializeField] public EventReference Explosion { get; private set; }
    [field: SerializeField] public EventReference WeaponPickup { get; private set; }
    

    [field: Header("PlayerSFX")]
    [field: SerializeField] public EventReference PlayerGetHit { get; private set; }
    [field: SerializeField] public EventReference PlayerCircle{ get; private set; }
    [field: SerializeField] public EventReference PointGain{ get; private set; }
    [field: SerializeField] public EventReference PlayerDeath{ get; private set; }

    [field: Header("ChickenSFX")]
    [field: SerializeField] public EventReference ChickenHit { get; private set; }
    [field: SerializeField] public EventReference ChickenDeath { get; private set; }
    [field: SerializeField] public EventReference ChickenSpawn { get; private set; }
    [field: Header("MutatorSFX")]
    [field: SerializeField] public EventReference SpawnAreaMutatorAlert { get; private set; }

    [field: Header("PowerUpSFX")]
    [field: SerializeField] public EventReference NoPowerUp { get; private set; }
    [field: SerializeField] public EventReference PowerUp1 { get; private set; }
    [field: SerializeField] public EventReference PowerUp2 { get; private set; }
    [field: SerializeField] public EventReference PickUpPowerUp { get; private set; }

    [field: Header("EntitiesSFX")]
    [field: SerializeField] public EventReference BoxBreak { get; private set; }
    [field: SerializeField] public EventReference BHLinger { get; private set; }

    [field: Header("DoorSFX")]
    [field: SerializeField] public EventReference DoorOpen { get; private set; }
    [field: SerializeField] public EventReference DoorClose { get; private set; }
    [field: SerializeField] public EventReference DoorSlide { get; private set; }

    [field: Header("Music")]
    [field: SerializeField] public EventReference MusicTracks { get; private set; }
    [field: Header("UI")]
    [field: SerializeField] public EventReference CharSelectSelect { get; private set; }
    [field: SerializeField] public EventReference CharSelectDeselect { get; private set; }
    [field: SerializeField] public EventReference CharSelectStart{ get; private set; }

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
