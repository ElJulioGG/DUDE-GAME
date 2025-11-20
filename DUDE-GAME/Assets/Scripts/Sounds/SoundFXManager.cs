using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private AudioSource soundFXPrefab;   // SFX source prefab
    [SerializeField] private AudioSource musicPrefab;     // Music source prefab

    [Header("Clips")]
    [SerializeField] private AudioClip[] soundFXClips;    // All SFX clips
    [SerializeField] private AudioClip[] musicClips;      // All music clips

    // Lookups
    private Dictionary<string, AudioClip> sfxLookup;
    private Dictionary<string, AudioClip> musicLookup;

    // SFX pooling
    private Dictionary<string, Queue<AudioSource>> sfxPool;
    private Dictionary<string, List<AudioSource>> activeSFX;

    // Music
    private AudioSource musicA;
    private AudioSource musicB;
    private bool usingA = true;

    private Transform poolParent;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        poolParent = new GameObject("Audio_Pool").transform;
        poolParent.SetParent(transform);

        // Lookup tables
        sfxLookup = new Dictionary<string, AudioClip>();
        musicLookup = new Dictionary<string, AudioClip>();

        foreach (var clip in soundFXClips)
            if (clip != null && !sfxLookup.ContainsKey(clip.name))
                sfxLookup.Add(clip.name, clip);

        foreach (var clip in musicClips)
            if (clip != null && !musicLookup.ContainsKey(clip.name))
                musicLookup.Add(clip.name, clip);

        sfxPool = new Dictionary<string, Queue<AudioSource>>();
        activeSFX = new Dictionary<string, List<AudioSource>>();

        // Music sources
        musicA = Instantiate(musicPrefab, poolParent);
        musicB = Instantiate(musicPrefab, poolParent);

        musicA.loop = true;
        musicB.loop = true;

        musicA.volume = 0;
        musicB.volume = 0;

        musicA.gameObject.SetActive(false);
        musicB.gameObject.SetActive(false);
    }

    // =========================================================
    //                       PLAY SFX
    // =========================================================

    public void PlaySoundByName(string clipName, Transform spawnTransform, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (!sfxLookup.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"SFX '{clipName}' not found.");
            return;
        }

        AudioSource src = GetSFXSource(clipName, spawnTransform.position);
        if (src == null) return;

        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = loop;
        src.Play();

        if (!activeSFX.ContainsKey(clipName))
            activeSFX[clipName] = new List<AudioSource>();

        activeSFX[clipName].Add(src);

        if (!loop)
            StartCoroutine(ReturnSFXToPool(src, clipName, clip.length));
    }

    private AudioSource GetSFXSource(string clipName, Vector3 position)
    {
        if (!sfxPool.TryGetValue(clipName, out Queue<AudioSource> pool))
        {
            pool = new Queue<AudioSource>();
            sfxPool.Add(clipName, pool);
        }

        AudioSource src = null;

        while (pool.Count > 0 && src == null)
        {
            src = pool.Dequeue();
        }

        if (src == null)
        {
            GameObject obj = Instantiate(soundFXPrefab.gameObject, position, Quaternion.identity, poolParent);
            src = obj.GetComponent<AudioSource>();
        }

        src.transform.position = position;
        src.gameObject.SetActive(true);

        return src;
    }

    private IEnumerator ReturnSFXToPool(AudioSource src, string clipName, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (src == null) yield break;

        if (activeSFX.ContainsKey(clipName))
            activeSFX[clipName].Remove(src);

        src.Stop();
        src.loop = false;
        src.gameObject.SetActive(false);

        sfxPool[clipName].Enqueue(src);
    }

    public void StopSoundByName(string clipName)
    {
        if (!activeSFX.ContainsKey(clipName)) return;

        foreach (var src in activeSFX[clipName])
        {
            if (src == null) continue;

            src.Stop();
            src.loop = false;
            src.gameObject.SetActive(false);
            sfxPool[clipName].Enqueue(src);
        }

        activeSFX[clipName].Clear();
    }

    public bool IsSoundPlaying(string clipName)
    {
        if (activeSFX.TryGetValue(clipName, out List<AudioSource> sources))
        {
            foreach (var src in sources)
            {
                if (src != null && src.gameObject.activeSelf && src.isPlaying)
                    return true;
            }
        }
        return false;
    }

    public void PlayMusic(string clipName, Transform spawnTransform, float volume = 1f, float pitch = 1f, bool loop = true)
    {
        if (!musicLookup.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"Music '{clipName}' not found.");
            return;
        }

        // Stop both sources instantly
        musicA.Stop();
        musicB.Stop();
        musicA.gameObject.SetActive(false);
        musicB.gameObject.SetActive(false);

        // Select the active music source (flip for next time)
        AudioSource next = usingA ? musicA : musicB;
        usingA = !usingA;

        // Apply settings
        next.clip = clip;
        next.loop = loop;
        next.volume = volume;
        next.pitch = pitch;  // now pitch is respected if you want it
        next.gameObject.SetActive(true);

        // Play immediately, no fade
        next.Play();
    }


    public void StopMusic()
    {
        musicA.Stop();
        musicB.Stop();
        musicA.gameObject.SetActive(false);
        musicB.gameObject.SetActive(false);
    }
    public void StopMusicByName(string clipName)
    {
        if (!musicLookup.ContainsKey(clipName))
            return;

        AudioClip target = musicLookup[clipName];

        if (musicA.isPlaying && musicA.clip == target)
        {
            musicA.Stop();
            musicA.gameObject.SetActive(false);
        }

        if (musicB.isPlaying && musicB.clip == target)
        {
            musicB.Stop();
            musicB.gameObject.SetActive(false);
        }
    }

    public bool IsMusicPlaying(string clipName)
    {
        if (!musicLookup.ContainsKey(clipName))
            return false;

        AudioClip target = musicLookup[clipName];

        bool aMatch = musicA.isPlaying && musicA.clip == target;
        bool bMatch = musicB.isPlaying && musicB.clip == target;

        return aMatch || bMatch;
    }

}
