using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên Canvas/Panel chứa thanh Rage (Nộ).
/// Tự động cập nhật thanh nộ theo PlayerStats với phong cách Bo góc (Rounded Corners) hiện đại,
/// nhấp nháy chữ "HÀO KHÍ ĐÔNG A" khi kích hoạt trạng thái Nộ.
/// </summary>
public class RageBarUI : MonoBehaviour
{
    [Header("--- Kết nối UI ---")]
    public Slider rageSlider;         // Thanh trượt nộ chính
    public Image fillImage;           // Hình ảnh fill của Slider chính

    [Header("--- Màu sắc ---")]
    public Color normalRageColor = new Color(0.7f, 0.1f, 0.1f);  // Đỏ đậm bình thường
    public Color activeRageColor = new Color(1.0f, 0.5f, 0.0f);  // Cam lửa rực rỡ khi nộ
    public Color maxRageColor = new Color(1.0f, 0.85f, 0.0f);    // Vàng rực rỡ khi đầy nộ

    // Text chỉ số
    private Text rageText;
    private PlayerStats playerStats;
    private float textFlashTimer = 0f;

    void Start()
    {
        // 1. Tìm kiếm PlayerStats
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();

        // Tự động gán slider nếu chưa được kéo thủ công
        if (rageSlider == null)
            rageSlider = GetComponent<Slider>();

        if (rageSlider != null)
        {
            if (fillImage == null && rageSlider.fillRect != null)
                fillImage = rageSlider.fillRect.GetComponent<Image>();

            // 2. Tạo Text chỉ số hiển thị
            SetupRageText();

            // 3. Áp dụng phong cách đồ họa Bo Góc (Procedural Rounded Box)
            ApplyProceduralStyling();

            if (playerStats != null)
            {
                rageSlider.maxValue = playerStats.maxRage;
                rageSlider.value = playerStats.currentRage;
            }
        }
    }

    private Font FindCustomFontAtRuntime()
    {
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        foreach (Font f in fonts)
        {
            if (f != null && f.name != "Arial" && f.name != "LegacyRuntime" && !f.name.StartsWith("Liberation"))
            {
                return f;
            }
        }
        return null;
    }

    private void SetupRageText()
    {
        // Tạo GameObject Text
        GameObject textObj = new GameObject("RageText");
        textObj.transform.SetParent(rageSlider.transform, false);

        rageText = textObj.AddComponent<Text>();
        Font customFont = FindCustomFontAtRuntime();
        rageText.font = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rageText.fontSize = 11;
        rageText.fontStyle = FontStyle.Bold;
        rageText.color = Color.white;
        rageText.alignment = TextAnchor.MiddleCenter;

        // Căn giữa văn bản trong Slider bằng RectTransform
        RectTransform rect = rageText.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        // Thêm Shadow nhẹ cho text dễ nhìn
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.75f);
        shadow.effectDistance = new Vector2(1, -1);
    }

    private void ApplyProceduralStyling()
    {
        if (rageSlider == null) return;

        int w = 512;
        int h = 22; // Thấp hơn một chút so với mana
        int radius = 8;
        int border = 2;

        // 1. Tạo Sprite nền bo góc
        Color bgCol = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        Color borderCol = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        Sprite bgSprite = CreateRoundedRectSprite(w, h, radius, bgCol, borderCol, border);

        Image bgImage = rageSlider.GetComponentInChildren<Image>();
        if (bgImage != null && bgImage.gameObject != (fillImage ? fillImage.gameObject : null))
        {
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Sliced;
        }

        // 2. Tạo Sprite Fill bo góc
        Sprite fillSprite = CreateRoundedRectSprite(w, h, radius, Color.white, Color.clear, 0);
        if (fillImage != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Sliced;
        }

        // 3. Tinh chỉnh vùng đệm (Padding)
        RectTransform fillRect = rageSlider.fillRect;
        if (fillRect != null)
        {
            fillRect.offsetMin = new Vector2(border, border);
            fillRect.offsetMax = new Vector2(-border, -border);
        }
    }

    void Update()
    {
        if (playerStats == null || rageSlider == null) return;

        // Lerp giá trị Slider nộ mượt mà
        rageSlider.value = Mathf.Lerp(rageSlider.value, playerStats.currentRage, Time.deltaTime * 10f);

        // Đổi màu fill dựa trên trạng thái nộ
        if (fillImage != null)
        {
            if (playerStats.isRageMode)
            {
                // Nhấp nháy màu vàng/cam lửa rực rỡ khi đang kích hoạt
                float pulse = Mathf.PingPong(Time.time * 5f, 1f);
                fillImage.color = Color.Lerp(activeRageColor, maxRageColor, pulse);
            }
            else
            {
                // Lerp từ đỏ thường sang cam rực rỡ khi đầy nộ
                float ragePercent = playerStats.currentRage / playerStats.maxRage;
                if (ragePercent >= 1.0f)
                {
                    float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                    fillImage.color = Color.Lerp(activeRageColor, maxRageColor, pulse);
                }
                else
                {
                    fillImage.color = Color.Lerp(normalRageColor, activeRageColor, ragePercent);
                }
            }
        }

        // Cập nhật text hiển thị chỉ số Nộ
        if (rageText != null)
        {
            if (playerStats.isRageMode)
            {
                textFlashTimer += Time.deltaTime * 8f;
                // Nhấp nháy chữ "HÀO KHÍ ĐÔNG A" màu vàng
                if (Mathf.Sin(textFlashTimer) > 0f)
                {
                    rageText.text = "🔥 HÀO KHÍ ĐÔNG A 🔥";
                    rageText.color = maxRageColor;
                }
                else
                {
                    rageText.text = "⚔️ HÀO KHÍ ĐÔNG A ⚔️";
                    rageText.color = Color.white;
                }
            }
            else
            {
                rageText.color = Color.white;
                if (playerStats.currentRage >= playerStats.maxRage)
                {
                    rageText.text = "🔥 ĐẦY NỘ - NHẤN [L]! 🔥";
                }
                else
                {
                    rageText.text = $"NỘ: {(int)playerStats.currentRage} / {playerStats.maxRage}";
                }
            }
        }
    }

    private Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fillColor, Color borderColor, int borderThickness)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = x < radius ? radius : (x >= width - radius ? width - radius - 1 : x);
                float cy = y < radius ? radius : (y >= height - radius ? height - radius - 1 : y);
                
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color color = Color.clear;
                
                if ((x < radius && y < radius) || (x >= width - radius && y < radius) || 
                    (x < radius && y >= height - radius) || (x >= width - radius && y >= height - radius))
                {
                    if (dist <= radius)
                    {
                        if (dist > radius - borderThickness)
                            color = borderColor;
                        else
                            color = fillColor;
                    }
                }
                else
                {
                    if (x < borderThickness || x >= width - borderThickness || y < borderThickness || y >= height - borderThickness)
                        color = borderColor;
                    else
                        color = fillColor;
                }
                
                if (dist > radius - 1 && dist <= radius + 1 && ((x < radius || x >= width - radius) && (y < radius || y >= height - radius)))
                {
                    float alpha = Mathf.Clamp01(radius + 1 - dist);
                    color.a *= alpha;
                }
                
                pixels[y * width + x] = color;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
}
