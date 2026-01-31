using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using System.Collections;


public enum AudioType
{
    SFX,
    Music,
    UI,
}

public class AudioManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject audioPrefab;
    [SerializeField] private List<AudioSource> audiosList;
    [SerializeField] private int audioAmmount;

    [Header("Mixer Groups")]
    // Arrastra aquí los grupos desde tu AudioMixer asset
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    private static AudioManager instance;
    public static AudioManager Instance{get {return instance;}}


    void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        audiosList = new List<AudioSource>();
        AddAudioToPool(audioAmmount);
    }

    private void AddAudioToPool(int ammount)
    {
        for(int i = 0; i < ammount; i++)
        {
            GameObject audio = Instantiate(audioPrefab);
            audio.SetActive(false);
            audio.transform.parent = transform;

            audiosList.Add(audio.GetComponent<AudioSource>());
        }
    }

    public AudioSource RequestedAudio()
    {
        for(int i = 0; i < audiosList.Count; i++)
        {
            if(!audiosList[i].gameObject.activeSelf)
            {
                return audiosList[i];
            }
        }

        AddAudioToPool(1);
        return audiosList[audiosList.Count - 1]; 
    }

    private AudioMixerGroup GetMixerGroup(AudioType type)
    {
        switch (type)
        {
            case AudioType.SFX: return sfxGroup;
            case AudioType.Music: return musicGroup;
            case AudioType.UI: return uiGroup;
            default: return sfxGroup;
        }
    }

    public void PlaySound2D(AudioClip clip, AudioType type = AudioType.SFX, float volume = 1f, float pitch = 1f)
    {
        AudioSource source = RequestedAudio();

        source.outputAudioMixerGroup = GetMixerGroup(type);
        
        source.spatialBlend = 0f; 
        source.transform.parent = this.transform;

        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;

        source.gameObject.SetActive(true);
        source.Play();

        StartCoroutine(DisableAudioDelayed(source, clip.length + 0.05f));
    }


    public void PlaySound(AudioClip clip, Vector3 position, AudioType type = AudioType.SFX, float volume = 1f, float pitch = 1f, Transform parent = null)
    {
        AudioSource source = RequestedAudio();

        source.outputAudioMixerGroup = GetMixerGroup(type);

        source.spatialBlend = 1f;
        source.transform.position = position;
        
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        if(parent != null) source.gameObject.transform.parent = parent;

        source.gameObject.SetActive(true);
        source.Play();

        StartCoroutine(DisableAudioDelayed(source, clip.length + 0.05f, parent));
    }


    private System.Collections.IEnumerator DisableAudioDelayed(AudioSource source, float duration, Transform parent = null)
    {
        yield return new WaitForSeconds(duration);
        if(parent != null) source.transform.parent = transform;
        source.gameObject.SetActive(false);
    }
}
