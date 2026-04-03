using UnityEngine;

[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip backgroundLoop;
    [SerializeField] private AudioClip shot;
    [SerializeField] private AudioClip asteroidDestroy;
    [SerializeField] private AudioClip shipDestroy;
    [SerializeField] private AudioClip playerHit;
    [SerializeField] private AudioClip playerDeath;
    [SerializeField] private AudioClip victory;

    [Header("Volumes 0-1")]
    [SerializeField] private float musicVolume = 0.4f;
    [SerializeField] private float shotVolume = 0.7f;
    [SerializeField] private float asteroidDestroyVolume = 1f;
    [SerializeField] private float shipDestroyVolume = 1f;
    [SerializeField] private float playerHitVolume = 0.8f;
    [SerializeField] private float playerDeathVolume = 1f;
    [SerializeField] private float victoryVolume = 1f;
    [SerializeField] private float playerHitMinInterval = 0.12f;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private float _lastPlayerHitAt = -10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume = musicVolume;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;

        EnsureAudioListener();
        TryStartMusic();
    }

    private void EnsureAudioListener()
    {
        if (FindFirstObjectByType<AudioListener>() != null)
            return;

        if (Camera.main != null)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
            return;
        }

        var anyCamera = FindFirstObjectByType<Camera>();
        if (anyCamera != null)
            anyCamera.gameObject.AddComponent<AudioListener>();
    }

    public void TryStartMusic()
    {
        if (_musicSource == null || backgroundLoop == null)
            return;

        if (_musicSource.isPlaying)
            return;

        _musicSource.clip = backgroundLoop;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    public void PlayShot() => PlaySfx(shot, shotVolume);
    public void PlayAsteroidDestroy() => PlaySfx(asteroidDestroy, asteroidDestroyVolume);
    public void PlayShipDestroy() => PlaySfx(shipDestroy, shipDestroyVolume);
    public void PlayPlayerHit()
    {
        if (Time.time - _lastPlayerHitAt < playerHitMinInterval)
            return;

        _lastPlayerHitAt = Time.time;
        PlaySfx(playerHit, playerHitVolume);
    }
    public void PlayPlayerDeath() => PlaySfx(playerDeath, playerDeathVolume);
    public void PlayVictory() => PlaySfx(victory, victoryVolume);

    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfxSource == null)
            return;

        _sfxSource.PlayOneShot(clip, volume);
    }
}
