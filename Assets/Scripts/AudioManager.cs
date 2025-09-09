// Name: Snow Cai
// Email: snowc@unr.edu

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip bgMusic;
    public AudioClip clickSound;
    public AudioClip buttonSound;
    public AudioClip winSound;
    public AudioClip errorSound;

    void Awake()
    {
        // Singleton setup so AudioManager survives scene reloads
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        PlayMusic(bgMusic);
    }

    // music
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // sfx
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayClick() => PlaySFX(clickSound);
    public void PlayButton() => PlaySFX(buttonSound);
    public void PlayWin() => PlaySFX(winSound);
    public void PlayError() => PlaySFX(errorSound);
}
