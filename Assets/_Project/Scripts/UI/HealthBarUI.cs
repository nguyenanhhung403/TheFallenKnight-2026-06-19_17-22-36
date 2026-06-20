using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên Canvas/Panel chứa thanh máu.
/// Tự động cập nhật thanh máu theo PlayerStats với hiệu ứng "Ghost/Damage Bar" cao cấp
/// và tạo giao diện Bo góc (Rounded Corners) cực kỳ hiện đại.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("--- Kết nối UI ---")]
    public Slider healthSlider;       // Thanh trượt máu chính
    public Image fillImage;           // Hình ảnh fill của Slider chính

    [Header("--- Hiệu ứng & Phong cách ---")]
    public Color fullHealthColor = new Color(0.12f, 0.73f, 0.3f); // Green hiện đại (vibrant green)
    public Color lowHealthColor = new Color(0.89f, 0.15f, 0.21f);  // Red sang trọng
    public Color ghostColor = new Color(0.95f, 0.61f, 0.07f);      // Yellow/Orange cảnh báo cho ghost bar
    
    // Ghost Slider để làm hiệu ứng thanh máu tụt dần sau khi nhận sát thương
    private Image ghostFillImage;
    private float targetValue;
    private float ghostValue;
    private float ghostDelayTimer;
    private const float GHOST_DELAY = 0.5f; // Thời gian trễ trước khi ghost bar bắt đầu tụt
    private const float GHOST_SPEED = 2f;    // Tốc độ tụt của ghost bar

    // Text chỉ số
    private Text hpText;
    private PlayerStats playerStats;

    void Start()
    {
        // 1. Tìm kiếm PlayerStats
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();

        // Tự động gán slider nếu chưa được kéo thủ công
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (healthSlider != null)
        {
            if (fillImage == null && healthSlider.fillRect != null)
                fillImage = healthSlider.fillRect.GetComponent<Image>();

            // 2. Tạo hiệu ứng Ghost Bar (Thanh tụt máu gián tiếp)
            SetupGhostBar();

            // 3. Tạo Text chỉ số HP hiển thị
            SetupHPText();

            // 4. Áp dụng phong cách đồ họa Bo Góc (Procedural Rounded Box)
            ApplyProceduralStyling();

            if (playerStats != null)
            {
                healthSlider.maxValue = playerStats.maxHP;
                healthSlider.value = playerStats.currentHP;
                targetValue = playerStats.currentHP;
                ghostValue = playerStats.currentHP;
            }
        }
    }

    private void SetupGhostBar()
    {
        if (healthSlider == null || fillImage == null) return;

        // Nhân bản Fill để làm Ghost Fill nằm phía sau
        GameObject ghostFillObj = Instantiate(fillImage.gameObject, fillImage.transform.parent);
        ghostFillObj.name = "GhostFill";
        
        // Đặt thứ tự hiển thị: Ghost Fill nằm dưới Fill chính trong Hierarchy để hiện phía sau
        ghostFillObj.transform.SetAsFirstSibling();

        ghostFillImage = ghostFillObj.GetComponent<Image>();
        if (ghostFillImage != null)
        {
            ghostFillImage.color = ghostColor;
        }
    }

    private void SetupHPText()
    {
        // Tạo GameObject Text
        GameObject textObj = new GameObject("HPText");
        textObj.transform.SetParent(healthSlider.transform, false);

        hpText = textObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Font chuẩn có sẵn
        hpText.fontSize = 16;
        hpText.fontStyle = FontStyle.Bold;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;

        // Căn giữa văn bản trong Slider bằng RectTransform
        RectTransform rect = hpText.rectTransform;
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
        if (healthSlider == null) return;

        // Chiều rộng và chiều cao ước tính để render bo góc
        int w = 512;
        int h = 36;
        int radius = 12;
        int border = 3;

        // 1. Tạo Sprite nền bo góc (Đen mờ trong suốt, viền bạc mảnh)
        Color bgCol = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        Color borderCol = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        Sprite bgSprite = CreateRoundedRectSprite(w, h, radius, bgCol, borderCol, border);

        Image bgImage = healthSlider.GetComponentInChildren<Image>();
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
        if (ghostFillImage != null)
        {
            ghostFillImage.sprite = fillSprite;
            ghostFillImage.type = Image.Type.Sliced;
        }

        // 3. Tinh chỉnh vùng đệm (Padding) để fill nằm trọn trong khung viền nền
        RectTransform fillRect = healthSlider.fillRect;
        if (fillRect != null)
        {
            fillRect.offsetMin = new Vector2(border, border);
            fillRect.offsetMax = new Vector2(-border, -border);
        }
    }

    void Update()
    {
        if (playerStats == null || healthSlider == null) return;

        // Cập nhật giá trị máu mục tiêu
        targetValue = playerStats.currentHP;

        // Lerp giá trị Slider chính nhanh chóng
        healthSlider.value = Mathf.Lerp(healthSlider.value, targetValue, Time.deltaTime * 12f);

        // Xử lý hiệu ứng Ghost Bar
        if (targetValue < ghostValue)
        {
            // Nếu nhận sát thương: thiết lập thời gian trễ
            if (Mathf.Abs(healthSlider.value - targetValue) > 0.1f)
            {
                ghostDelayTimer = GHOST_DELAY;
            }
            ghostValue = healthSlider.value; // Giữ nguyên vị trí ban đầu
        }

        if (ghostDelayTimer > 0)
        {
            ghostDelayTimer -= Time.deltaTime;
        }
        else
        {
            // Tụt dần đều bám theo slider chính
            ghostValue = Mathf.MoveTowards(ghostValue, targetValue, Time.deltaTime * playerStats.maxHP * GHOST_SPEED * 0.15f);
        }

        // Cập nhật kích thước Ghost Fill thông qua giá trị trượt gián tiếp
        if (ghostFillImage != null)
        {
            float hpPercent = ghostValue / playerStats.maxHP;
            ghostFillImage.rectTransform.anchorMax = new Vector2(hpPercent, 1f);
        }

        // Đổi màu thanh máu chính dựa trên lượng HP còn lại
        if (fillImage != null)
        {
            float hpPercent = (float)playerStats.currentHP / playerStats.maxHP;
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, hpPercent);
        }

        // Cập nhật text chỉ số HP
        if (hpText != null)
        {
            hpText.text = $"HP: {playerStats.currentHP} / {playerStats.maxHP}";
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
