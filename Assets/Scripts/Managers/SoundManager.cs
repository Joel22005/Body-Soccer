using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip ballHitPuckSound;
    [SerializeField] private AudioClip ballHitWallSound;
    [SerializeField] private AudioClip ballHitPostSound;
    [SerializeField] private AudioClip puckHitPuckSound;
    [SerializeField] private AudioClip goalSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource per a la música (loop)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.4f;

        // AudioSource per als efectes (sense loop)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 1f;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayBallHitPuck() => PlaySFX(ballHitPuckSound);
    public void PlayBallHitWall() => PlaySFX(ballHitWallSound);
    public void PlayBallHitPost() => PlaySFX(ballHitPostSound);
    public void PlayPuckHitPuck() => PlaySFX(puckHitPuckSound);
    public void PlayGoal() => PlaySFX(goalSound);

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }
}