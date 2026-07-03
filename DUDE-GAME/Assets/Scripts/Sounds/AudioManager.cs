using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
public class AudioManager : MonoBehaviour
{
    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;

    private EventInstance ambienceEventInstance;
    private EventInstance musicEventInstance;
    public static AudioManager Instance { get; private set; }

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
        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();
    }
    private void Start()
    {
        // Un AudioManager DUPLICADO (de una escena nueva) se marca para Destroy
        // en Awake, pero Unity IGUAL le llama Start ese mismo frame. Sin esta
        // guarda, creaba una segunda instancia de musica huerfana e imparable
        // (sonaba el track 0 = MENU encimado con el de la escena, solo en build).
        if (Instance != this) return;

        //InitializeAmbience(FMODEvents.Instance.VoiceSay3);
        InitializeMusic(FMODEvents.Instance.MusicTracks);
    }
    private void InitializeAmbience(EventReference ambienceEventReference)
    {
        ambienceEventInstance = RuntimeManager.CreateInstance(ambienceEventReference);
        ambienceEventInstance.start();
    }
    public void InitializeMusic(EventReference musicEventReference)
    {
        // Idempotente: si ya hay musica sonando, se corta antes de crear otra,
        // para que nunca puedan quedar dos instancias encimadas.
        if (musicEventInstance.isValid())
        {
            musicEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicEventInstance.release();
        }
        musicEventInstance = RuntimeManager.CreateInstance(musicEventReference);
        musicEventInstance.start();
    }
    public void PlaySound(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }
    // Like PlaySound, but returns a handle so the caller can stop the sound early
    // (PlayOneShot is fire-and-forget and can't be stopped). The instance is marked
    // for release immediately, so FMOD frees it when it finishes naturally; the
    // returned handle stays valid for an early stop until then.
    public EventInstance PlayStoppableSound(EventReference sound, Vector3 worldPos)
    {
        EventInstance instance = RuntimeManager.CreateInstance(sound);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPos));
        instance.start();
        instance.release();
        return instance;
    }
    public void SetMusicArea(MusicTracks trackIndex)
    {
        // Si la instancia fue detenida y LIBERADA (CleanUpMusic al salir del menu),
        // isValid() queda false para siempre y sin esto la musica no volveria a
        // sonar en ninguna escena. La recreamos aqui: este metodo es el punto de
        // entrada de todas las escenas (MENU, CHARSELECT, FIGHT).
        if (!musicEventInstance.isValid())
            InitializeMusic(FMODEvents.Instance.MusicTracks);

        // Igual que en FMOD: el parametro se llama "MusicTrack" y el valor es el
        // indice del enum MusicTracks.
        musicEventInstance.setParameterByName("MusicTrack", (float)trackIndex);
    }
   public EventInstance CreateSoundInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }
    public StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitterGameObject)
    {
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }
    
    public void Cleanup()
    {
        foreach(EventInstance instance in eventInstances)
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
        }
        foreach(StudioEventEmitter emitter in eventEmitters)
        {
            emitter.Stop();
        }
    }
    public void CleanUpMusic()
    {
        if (musicEventInstance.isValid())
        {
            musicEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicEventInstance.release();
        }
    }
    private void OnDestroy()
    {
        Cleanup();
    }
    
}
