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
    public bool enableLightning = true;
    public float minLightningInterval = 12f;
    public float maxLightningInterval = 25f;

    [Header("--- Cấu hình Lá Rụng ---")]
    public bool enableLeaves = true;
    public int leafCount = 18;
    public float leafSpeedX = -4.5f;
    public float leafSpeedY = -2f;

    // References
    private Camera cam;
    private GameObject[] rainPool;
    private Image flashImage;
    private float lightningTimer;
    private Sprite rainSprite;
    private GameObject[] leafPool;
    private Sprite leafSprite;

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

        // 4. Khởi tạo Pool lá rụng
        if (enableLeaves)
        {
            leafSprite = CreateLeafSprite();
            InitializeLeafPool();
        }

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
        if (enableLightning)
        {
            lightningTimer -= Time.deltaTime;
            if (lightningTimer <= 0f)
            {
                StartCoroutine(TriggerLightningFlash());
                lightningTimer = Random.Range(minLightningInterval, maxLightningInterval);
            }
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

    public void StopWeather()
    {
        isRaining = false;
        enableLightning = false;
        enableLeaves = false;

        // Ẩn tất cả hạt mưa
        if (rainPool != null)
        {
            foreach (var drop in rainPool)
            {
                if (drop != null) drop.SetActive(false);
            }
        }
        // Ẩn tất cả lá rụng
        if (leafPool != null)
        {
            foreach (var leaf in leafPool)
            {
                if (leaf != null) leaf.SetActive(false);
            }
        }
    }

    private void InitializeLeafPool()
    {
        leafPool = new GameObject[leafCount];
        GameObject leafContainer = new GameObject("LeafContainer");
        leafContainer.transform.SetParent(transform);

        for (int i = 0; i < leafCount; i++)
        {
            GameObject leaf = new GameObject("Leaf_" + i);
            leaf.transform.SetParent(leafContainer.transform);

            SpriteRenderer sr = leaf.AddComponent<SpriteRenderer>();
            sr.sprite = leafSprite;
            sr.sortingOrder = 18; // Render sau mưa nhưng trước cảnh vật/nhân vật

            leaf.transform.localScale = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.6f, 1.2f), 1f);
            
            WindyLeaf script = leaf.AddComponent<WindyLeaf>();
            script.Setup(this, leafSpeedX, leafSpeedY);

            leafPool[i] = leaf;
        }
    }

    private Sprite CreateLeafSprite()
    {
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 3.5f;
                float dy = y - 3.5f;
                if (Mathf.Abs(dx + dy) <= 3.5f && Mathf.Abs(dx - dy) <= 2.2f)
                {
                    pixels[y * size + x] = new Color(0.85f, Random.Range(0.35f, 0.65f), 0.15f, 0.7f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}

/// <summary>
/// Thành phần điều khiển chuyển động của lá rụng đung đưa lượn sóng theo gió
/// </summary>
public class WindyLeaf : MonoBehaviour
{
    private WeatherManager manager;
    private float speedX;
    private float speedY;
    private float sinSpeed;
    private float sinAmplitude;
    private float timeOffset;

    public void Setup(WeatherManager wm, float sx, float sy)
    {
        manager = wm;
        speedX = sx * Random.Range(0.8f, 1.2f);
        speedY = sy * Random.Range(0.8f, 1.2f);
        sinSpeed = Random.Range(2f, 5f);
        sinAmplitude = Random.Range(0.8f, 2.2f);
        timeOffset = Random.Range(0f, 10f);

        ResetPosition(true);
    }

    void Update()
    {
        if (Camera.main == null) return;
        Camera cam = Camera.main;

        float horizontalMove = speedX * Time.deltaTime;
        float sinOffset = Mathf.Sin(Time.time * sinSpeed + timeOffset) * sinAmplitude * Time.deltaTime;
        float verticalMove = (speedY + sinOffset) * Time.deltaTime;

        transform.position += new Vector3(horizontalMove, verticalMove, 0f);

        // Xoay nhẹ lá cây khi rơi
        transform.Rotate(0f, 0f, 45f * Time.deltaTime);

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float bottomLimit = camPos.y - (camHeight / 2f) - 2f;
        float leftLimit = camPos.x - (camWidth / 2f) - 3f;

        if (transform.position.y < bottomLimit || transform.position.x < leftLimit)
        {
            ResetPosition(false);
        }
    }

    public void ResetPosition(bool fullScreenInit)
    {
        if (Camera.main == null) return;
        Camera cam = Camera.main;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float topY = camPos.y + (camHeight / 2f) + 2f;
        float bottomY = camPos.y - (camHeight / 2f) - 2f;
        float leftX = camPos.x - (camWidth / 2f) - 3f;
        float rightX = camPos.x + (camWidth / 2f) + 3f;

        float spawnX = fullScreenInit ? Random.Range(leftX, rightX) : rightX + Random.Range(0f, 3f);
        float spawnY = Random.Range(bottomY + 1f, topY + 2f);

        transform.position = new Vector3(spawnX, spawnY, 0f);
    }
}
