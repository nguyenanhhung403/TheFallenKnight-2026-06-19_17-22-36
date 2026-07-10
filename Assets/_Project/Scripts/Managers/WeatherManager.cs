using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý thời tiết mưa gió và hiệu ứng sấm sét động lập trình hoàn toàn bằng code.
/// Tự động khởi tạo hạt mưa và canvas chớp sét, không cần tài nguyên ngoài.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("--- Cấu hình Mưa ---")]
    public bool isRaining = true;
    public int rainDropCount = 80;        // Số lượng hạt mưa trên màn hình
    public float fallSpeed = 15f;         // Tốc độ rơi của hạt mưa
    public float windAngle = 15f;         // Góc nghiêng của gió/mưa (độ)

    [Header("--- Cấu hình Sấm Sét ---")]
    public float minLightningInterval = 12f;
    public float maxLightningInterval = 25f;

    // References
    private Camera cam;
    private GameObject[] rainPool;
    private Image flashImage;
    private float lightningTimer;
    private Sprite rainSprite;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        cam = Camera.main;

        // 1. Tạo Rain Sprite trong RAM
        rainSprite = CreateRainSprite();

        // 2. Khởi tạo Pool hạt mưa
        InitializeRainPool();

        // 3. Khởi tạo Canvas chớp sét ở runtime
        InitializeFlashUI();

        // Đặt bộ đếm thời gian sấm sét đầu tiên
        lightningTimer = Random.Range(5f, 10f);
    }

    private void InitializeRainPool()
    {
        rainPool = new GameObject[rainDropCount];
        GameObject rainContainer = new GameObject("RainContainer");
        rainContainer.transform.SetParent(transform);

        for (int i = 0; i < rainDropCount; i++)
        {
            GameObject drop = new GameObject("RainDrop_" + i);
            drop.transform.SetParent(rainContainer.transform);

            SpriteRenderer sr = drop.AddComponent<SpriteRenderer>();
            sr.sprite = rainSprite;
            sr.sortingOrder = 20; // Luôn hiển thị trước map và nhân vật

            // Xoay nghiêng hạt mưa theo chiều gió
            drop.transform.rotation = Quaternion.Euler(0f, 0f, windAngle);
            drop.transform.localScale = new Vector3(0.5f, Random.Range(0.8f, 1.3f), 1f);

            // Đặt vị trí ban đầu ngẫu nhiên xung quanh camera
            RandomizeDropPosition(drop, true);

            rainPool[i] = drop;
        }
    }

    private void InitializeFlashUI()
    {
        GameObject canvasObj = new GameObject("LightningCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Hiển thị đè lên tất cả UI khác

        canvasObj.AddComponent<CanvasScaler>();

        GameObject panelObj = new GameObject("FlashPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        flashImage = panelObj.AddComponent<Image>();
        flashImage.color = new Color(1f, 1f, 1f, 0f);

        RectTransform rect = flashImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    private void RandomizeDropPosition(GameObject drop, bool fullScreenInit)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float topY = camPos.y + (camHeight / 2f) + 2f;
        float bottomY = camPos.y - (camHeight / 2f) - 2f;
        float leftX = camPos.x - (camWidth / 2f) - 3f;
        float rightX = camPos.x + (camWidth / 2f) + 3f;

        float spawnX = Random.Range(leftX, rightX + 4f); // Bù đắp gió thổi chéo sang trái
        float spawnY = fullScreenScreenInit() ? Random.Range(bottomY, topY) : topY + Random.Range(0f, 3f);

        drop.transform.position = new Vector3(spawnX, spawnY, 0f);

        bool fullScreenScreenInit()
        {
            return fullScreenInit;
        }
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // 1. Cập nhật di chuyển các hạt mưa bám theo Camera
        if (isRaining && rainPool != null)
        {
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float bottomLimit = camPos.y - (camHeight / 2f) - 2f;
            float leftLimit = camPos.x - (camWidth / 2f) - 4f;

            foreach (var drop in rainPool)
            {
                if (drop == null) continue;

                // Di chuyển hạt mưa dọc theo hướng nghiêng của nó (Space.Self)
                drop.transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.Self);

                // Nếu rơi ra khỏi biên dưới hoặc biên trái camera -> reset lên đỉnh
                if (drop.transform.position.y < bottomLimit || drop.transform.position.x < leftLimit)
                {
                    RandomizeDropPosition(drop, false);
                }
            }
        }

        // 2. Bộ đếm sấm sét
        lightningTimer -= Time.deltaTime;
        if (lightningTimer <= 0f)
        {
            StartCoroutine(TriggerLightningFlash());
            lightningTimer = Random.Range(minLightningInterval, maxLightningInterval);
        }
    }

    private System.Collections.IEnumerator TriggerLightningFlash()
    {
        if (flashImage == null) yield break;

        // Tiếng sấm rền trước chớp sét một chút hoặc ngay cùng thời điểm
        PlayThunderSound();

        // Phát chớp 1 (Chớp mạnh)
        yield return StartCoroutine(FadeFlash(0f, 0.85f, 0.05f));
        yield return StartCoroutine(FadeFlash(0.85f, 0.15f, 0.08f));

        // Phát chớp 2 (Nháy phụ tạo cảm giác giật điện)
        yield return StartCoroutine(FadeFlash(0.15f, 0.65f, 0.04f));
        yield return StartCoroutine(FadeFlash(0.65f, 0f, 0.55f)); // Mờ dần vào bóng tối
    }

    private System.Collections.IEnumerator FadeFlash(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = flashImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            flashImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        flashImage.color = new Color(c.r, c.g, c.b, endAlpha);
    }

    private void PlayThunderSound()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverClip != null)
        {
            // Tạo nguồn phát âm thanh sấm rền tạm thời
            GameObject thunderObj = new GameObject("ThunderSFXTemp");
            AudioSource source = thunderObj.AddComponent<AudioSource>();
            source.clip = AudioManager.Instance.gameOverClip;
            
            // Hạ thấp pitch xuống để biến tiếng nhạc game over thành tiếng sấm nổ rền vang
            source.pitch = Random.Range(0.22f, 0.32f); 
            source.volume = 0.85f;
            source.Play();

            // Rung nhẹ camera khi sấm sét đánh
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.TriggerShake(0.6f, 0.18f);
            }

            Destroy(thunderObj, 6f);
        }
    }

    private Sprite CreateRainSprite()
    {
        int w = 2;
        int h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];

        for (int i = 0; i < pixels.Length; i++)
        {
            // Màu nước mưa nhạt xanh xám mờ trong suốt
            pixels[i] = new Color(0.7f, 0.8f, 0.9f, 0.38f);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
