using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StoryCutsceneController : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject storyPanel;
    public Text slideshowText;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;
    public RectTransform creditsContainer;
    public Text creditsText;
    public GameObject mainMenuCanvasGroup; // Để ẩn/hiện menu chính

    [Header("--- Video Clips ---")]
    public VideoClip introClip;
    public VideoClip redemptionClip;
    public VideoClip legacyClip;

    [Header("--- Story Content ---")]
    [TextArea(3, 5)]
    public string[] introSlides;
    [TextArea(3, 5)]
    public string[] redemptionSlides;
    [TextArea(3, 5)]
    public string[] legacySlides;

    private Coroutine cutsceneCoroutine;
    private RenderTexture videoTexture;
    private bool loadGameAtEnd = false;
    private bool showCredits = false;

    void Awake()
    {
        // Ẩn panel story khi bắt đầu
        if (storyPanel != null) storyPanel.SetActive(false);

        // Khởi tạo render texture cho video
        if (videoPlayer != null)
        {
            videoTexture = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            videoPlayer.targetTexture = videoTexture;
            if (videoDisplay != null)
            {
                videoDisplay.texture = videoTexture;
            }
        }
    }

    void Start()
    {
        // Tự động phát cutscene nếu có hàng đợi từ gameplay (khi đánh bại boss)
        if (PlayerPrefs.HasKey("PendingCutscene"))
        {
            string pending = PlayerPrefs.GetString("PendingCutscene");
            PlayerPrefs.DeleteKey("PendingCutscene");
            PlayerPrefs.Save();

            if (pending == "Redemption")
            {
                PlayRedemptionSequence();
            }
            else if (pending == "Legacy")
            {
                PlayLegacySequence();
            }
        }
    }

    void OnDestroy()
    {
        if (videoTexture != null)
        {
            videoTexture.Release();
        }
    }

    // Các hàm kích hoạt khi click button
    public void PlayGameWithIntro()
    {
        loadGameAtEnd = true;
        PlayIntroSequence();
    }

    public void PlayIntroSequence()
    {
        // Giữ nguyên loadGameAtEnd nếu được gọi từ PlayGameWithIntro, còn không thì là false
        StartSequence(introSlides, introClip, false);
    }

    public void PlayRedemptionSequence()
    {
        loadGameAtEnd = false;
        StartSequence(redemptionSlides, redemptionClip, true);
    }

    public void PlayLegacySequence()
    {
        loadGameAtEnd = false;
        StartSequence(legacySlides, legacyClip, true);
    }

    private void StartSequence(string[] slides, VideoClip clip, bool credits)
    {
        showCredits = credits;
        if (cutsceneCoroutine != null)
        {
            StopCoroutine(cutsceneCoroutine);
        }
        cutsceneCoroutine = StartCoroutine(CutsceneRoutine(slides, clip));
    }

    private IEnumerator CutsceneRoutine(string[] slides, VideoClip clip)
    {
        // Dừng BGM để tránh tiếng ồn chồng chéo với video
        AudioManager.Instance.StopBGM();

        // Ẩn Main Menu
        if (mainMenuCanvasGroup != null) mainMenuCanvasGroup.SetActive(false);
        if (storyPanel != null) storyPanel.SetActive(true);

        // Reset UI
        if (slideshowText != null)
        {
            slideshowText.gameObject.SetActive(false); // Ẩn lúc đầu để chạy video trước
            slideshowText.color = new Color(slideshowText.color.r, slideshowText.color.g, slideshowText.color.b, 0f);
        }
        if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
        if (creditsContainer != null)
        {
            creditsContainer.gameObject.SetActive(false);
            creditsContainer.anchoredPosition = new Vector2(creditsContainer.anchoredPosition.x, -600f);
        }

        // 1. PHÁT VIDEO PLAYER TRƯỚC
        if (clip != null && videoPlayer != null && videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(true);
            
            // Đặt màu RawImage đen lúc chuẩn bị phát
            videoDisplay.color = Color.black;
            
            videoPlayer.clip = clip;
            videoPlayer.Prepare();

            // Đợi prepare xong
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            videoDisplay.color = Color.white;
            videoPlayer.Play();

            // Đợi chạy hết video
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }

            videoDisplay.gameObject.SetActive(false);
        }

        // 2. CHẠY CHỮ (SLIDESHOW) FADE IN/OUT SAU ĐÓ
        if (slides != null && slides.Length > 0 && slideshowText != null)
        {
            slideshowText.gameObject.SetActive(true);
            foreach (string slideText in slides)
            {
                slideshowText.text = slideText;
                
                // Fade In
                yield return StartCoroutine(FadeText(0f, 1f, 1.5f));
                yield return new WaitForSeconds(3.5f);
                
                // Fade Out
                yield return StartCoroutine(FadeText(1f, 0f, 1f));
                yield return new WaitForSeconds(0.5f);
            }
            slideshowText.gameObject.SetActive(false);
        }

        // 3. HIỆN MÀN HÌNH CREDITS CHẠY CHỮ CUỐI CÙNG
        if (showCredits && creditsContainer != null)
        {
            creditsContainer.gameObject.SetActive(true);
            float duration = 15f; // Thời gian chạy cuộn chữ
            float timer = 0f;
            float startY = -600f;
            float endY = 800f; // Điểm cuộn lên hết màn hình

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                float currentY = Mathf.Lerp(startY, endY, progress);
                creditsContainer.anchoredPosition = new Vector2(creditsContainer.anchoredPosition.x, currentY);
                yield return null;
            }
        }

        // Kết thúc, quay lại Menu chính
        ExitStory();
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color c = slideshowText.color;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            slideshowText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        slideshowText.color = new Color(c.r, c.g, c.b, endAlpha);
    }

    public void ExitStory()
    {
        if (cutsceneCoroutine != null)
        {
            StopCoroutine(cutsceneCoroutine);
            cutsceneCoroutine = null;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (storyPanel != null) storyPanel.SetActive(false);

        if (loadGameAtEnd)
        {
            loadGameAtEnd = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }
        else
        {
            if (mainMenuCanvasGroup != null) mainMenuCanvasGroup.SetActive(true);
            AudioManager.Instance.PlayBGM();
        }
    }
}
