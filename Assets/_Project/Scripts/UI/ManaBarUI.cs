using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên Canvas/Panel chứa thanh mana.
/// Tự động cập nhật thanh mana theo PlayerStats với phong cách Bo góc (Rounded Corners) cực kỳ hiện đại
/// và hiển thị chỉ số số thực tế của Mana.
/// </summary>
public class ManaBarUI : MonoBehaviour
{
    [Header("--- Kết nối UI ---")]
    public Slider manaSlider;         // Thanh trượt mana chính
    public Image fillImage;           // Hình ảnh fill của Slider chính

    [Header("--- Màu sắc ---")]
    public Color fullManaColor = new Color(0.0f, 0.72f, 1.0f);   // Cyan/Light Blue rực rỡ
    public Color lowManaColor = new Color(0.0f, 0.28f, 0.73f);   // Dark Blue sang trọng

    // Text chỉ số
    private Text mpText;
    private PlayerStats playerStats;

    void Start()
    {
        // 1. Tìm kiếm PlayerStats
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();

        // Tự động gán slider nếu chưa được kéo thủ công
        if (manaSlider == null)
            manaSlider = GetComponent<Slider>();

        if (manaSlider != null)
        {
            if (fillImage == null && manaSlider.fillRect != null)
                fillImage = manaSlider.fillRect.GetComponent<Image>();

            // 2. Tạo Text chỉ số MP hiển thị
            SetupMPText();

            // 3. Áp dụng phong cách đồ họa Bo Góc (Procedural Rounded Box)
            ApplyProceduralStyling();

            if (playerStats != null)
            {
                manaSlider.maxValue = playerStats.maxMP;
                manaSlider.value = playerStats.currentMP;
            }
        }
    }

    private void SetupMPText()
    {
        // Tạo GameObject Text
        GameObject textObj = new GameObject("MPText");
        textObj.transform.SetParent(manaSlider.transform, false);

        mpText = textObj.AddComponent<Text>();
        mpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Font chuẩn có sẵn
        mpText.fontSize = 14;
        mpText.fontStyle = FontStyle.Bold;
        mpText.color = Color.white;
        mpText.alignment = TextAnchor.MiddleCenter;

        // Căn giữa văn bản trong Slider bằng RectTransform
        RectTransform rect = mpText.rectTransform;
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
        if (manaSlider == null) return;

        // Chiều rộng và chiều cao ước tính để render bo góc
        int w = 512;
        int h = 28; // Hơi thấp hơn thanh máu một chút
        int radius = 10;
        int border = 3;

        // 1. Tạo Sprite nền bo góc (Đen mờ trong suốt, viền bạc mảnh)
        Color bgCol = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        Color borderCol = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        Sprite bgSprite = CreateRoundedRectSprite(w, h, radius, bgCol, borderCol, border);

        Image bgImage = manaSlider.GetComponentInChildren<Image>();
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

        // 3. Tinh chỉnh vùng đệm (Padding) để fill nằm trọn trong khung viền nền
        RectTransform fillRect = manaSlider.fillRect;
        if (fillRect != null)
        {
            fillRect.offsetMin = new Vector2(border, border);
            fillRect.offsetMax = new Vector2(-border, -border);
        }
    }

    void Update()
    {
        if (playerStats == null || manaSlider == null) return;

        // Lerp giá trị Slider mana mượt mà
        manaSlider.value = Mathf.Lerp(manaSlider.value, playerStats.currentMP, Time.deltaTime * 10f);

        // Đổi màu dần dựa trên lượng Mana hiện tại
        if (fillImage != null)
        {
            float mpPercent = playerStats.currentMP / playerStats.maxMP;
            fillImage.color = Color.Lerp(lowManaColor, fullManaColor, mpPercent);
        }

        // Cập nhật text hiển thị chỉ số MP
        if (mpText != null)
        {
            mpText.text = $"MP: {(int)playerStats.currentMP} / {playerStats.maxMP}";
        }
    }

    // Hàm tạo Sprite bo góc đẹp mắt ngay trong RAM
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
