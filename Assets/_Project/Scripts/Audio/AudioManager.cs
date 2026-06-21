using UnityEngine;

public enum SoundEffect
{
    Jump,
    Attack,
    Hurt,
    DefeatEnemy,
    CollectPotion,
    ButtonClick,
    GameOver,
    BossDefeated
}

/// <summary>
/// Quản lý âm thanh (BGM và SFX) cho toàn bộ game.
/// Tự động khởi tạo nếu chưa có sẵn trong Scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("--- Audio Sources ---")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("--- Audio Clips ---")]
    public AudioClip bgmMusic;
    public AudioClip jumpClip;
    public AudioClip attackClip;      // Hit.wav
    public AudioClip hurtClip;        // Hurt.wav
    public AudioClip collectClip;     // Coin.wav
    public AudioClip clickClip;       // Coin.wav
    public AudioClip gameOverClip;    // GameOver.wav
    public AudioClip bossDefeatedClip; // GameOver.wav hoặc clip đặc biệt khác

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Tạo AudioSource nếu chưa có
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        LoadDefaultClips();
    }

    private void Start()
    {
        PlayBGM();
    }

    private void LoadDefaultClips()
    {
        // Tải các tài nguyên âm thanh trực tiếp từ thư mục assets
#if UNITY_EDITOR
        if (bgmMusic == null) bgmMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/BGM.mp3");
        if (jumpClip == null) jumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Jump.wav");
        if (attackClip == null) attackClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Hit.wav");
        if (hurtClip == null) hurtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Hurt.wav");
        if (collectClip == null) collectClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Coin.wav");
        if (clickClip == null) clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Coin.wav");
        if (gameOverClip == null) gameOverClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/GameOver.wav");
        if (bossDefeatedClip == null) bossDefeatedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Victory.wav");
#else
        // Nếu chạy build, tải từ Resources
        bgmMusic = Resources.Load<AudioClip>("Audio/BGM");
        jumpClip = Resources.Load<AudioClip>("Audio/Jump");
        attackClip = Resources.Load<AudioClip>("Audio/Hit");
        hurtClip = Resources.Load<AudioClip>("Audio/Hurt");
        collectClip = Resources.Load<AudioClip>("Audio/Coin");
        clickClip = Resources.Load<AudioClip>("Audio/Coin");
        gameOverClip = Resources.Load<AudioClip>("Audio/GameOver");
        bossDefeatedClip = Resources.Load<AudioClip>("Audio/Victory");
#endif
    }

    public void PlayBGM()
    {
        if (bgmSource == null) return;
        if (bgmMusic != null)
        {
            if (bgmSource.clip == bgmMusic && bgmSource.isPlaying) return;
            bgmSource.clip = bgmMusic;
            bgmSource.volume = 0.35f;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("[AudioManager] Không tìm thấy BGM Clip!");
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySFX(SoundEffect sfx)
    {
        if (sfxSource == null) return;

        AudioClip clip = null;
        float volume = 1f;

        switch (sfx)
        {
            case SoundEffect.Jump:
                clip = jumpClip;
                break;
            case SoundEffect.Attack:
                clip = attackClip;
                volume = 0.8f;
                break;
            case SoundEffect.Hurt:
                clip = hurtClip;
                break;
            case SoundEffect.DefeatEnemy:
                clip = hurtClip;
                volume = 0.8f;
                break;
            case SoundEffect.CollectPotion:
                clip = collectClip;
                break;
            case SoundEffect.ButtonClick:
                clip = clickClip;
                volume = 0.6f;
                break;
            case SoundEffect.GameOver:
                clip = gameOverClip;
                break;
            case SoundEffect.BossDefeated:
                clip = bossDefeatedClip;
                break;
        }

        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Chưa gán Audio Clip cho SFX: {sfx}");
        }
    }
}
