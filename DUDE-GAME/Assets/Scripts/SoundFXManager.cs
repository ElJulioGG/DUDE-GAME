using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance { get; private set; }

    [SerializeField] private AudioSource soundFXPrefab;
    [SerializeField] private AudioClip[] soundFXClips;

    private Dictionary<string, AudioClip> clipLookup;
    private Dictionary<string, Queue<AudioSource>> audioSourcePool;
    private Dictionary<string, List<AudioSource>> activeSources;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeClipLookup();
            audioSourcePool = new Dictionary<string, Queue<AudioSource>>();
            activeSources = new Dictionary<string, List<AudioSource>>();
        }
        else
        {
            //Destroy(gameObject);
        }
    }

    private void InitializeClipLookup()
    {
        clipLookup = new Dictionary<string, AudioClip>();
        foreach (var clip in soundFXClips)
        {
            if (clip != null && !clipLookup.ContainsKey(clip.name))
            {
                clipLookup.Add(clip.name, clip);
            }
        }
    }

    public void PlaySoundByName(string clipName, Transform spawnTransform, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (!clipLookup.TryGetValue(clipName, out AudioClip clipToPlay))
        {
            Debug.LogWarning($"SoundFXManager: AudioClip '{clipName}' not found.");
            return;
        }

        AudioSource source = GetAudioSourceFromPool(clipName, spawnTransform.position);
        source.clip = clipToPlay;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();

        if (!activeSources.ContainsKey(clipName))
            activeSources[clipName] = new List<AudioSource>();

        activeSources[clipName].Add(source);

        if (!loop)
        {
            StartCoroutine(ReturnToPoolAfterPlay(source, clipName, clipToPlay.length));
        }
    }

    private AudioSource GetAudioSourceFromPool(string clipName, Vector3 position)
    {
        if (!audioSourcePool.TryGetValue(clipName, out Queue<AudioSource> pool))
        {
            pool = new Queue<AudioSource>();
            audioSourcePool[clipName] = pool;
        }

        AudioSource source;
        if (pool.Count > 0)
        {
            source = pool.Dequeue();
            source.gameObject.SetActive(true);
        }
        else
        {
            GameObject newObj = Instantiate(soundFXPrefab.gameObject, position, Quaternion.identity);
            source = newObj.GetComponent<AudioSource>();
        }

        source.transform.position = position;
        return source;
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource source, string clipName, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeSources.ContainsKey(clipName))
            activeSources[clipName].Remove(source);

        source.Stop();
        source.loop = false;
        source.gameObject.SetActive(false);
        audioSourcePool[clipName].Enqueue(source);
    }

    public void StopSoundByName(string clipName)
    {
        if (!activeSources.ContainsKey(clipName)) return;

        foreach (var source in activeSources[clipName])
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
                source.loop = false;
                source.gameObject.SetActive(false);

                if (!audioSourcePool.ContainsKey(clipName))
                    audioSourcePool[clipName] = new Queue<AudioSource>();

                audioSourcePool[clipName].Enqueue(source);
            }
        }

        activeSources[clipName].Clear();
    }
    public bool IsSoundPlaying(string clipName)
    {
        if (activeSources.TryGetValue(clipName, out List<AudioSource> sources))
        {
            foreach (var source in sources)
            {
                if (source != null && source.isPlaying)
                    return true;
            }
        }
        return false;
    }

}
