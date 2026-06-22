using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hệ thống Potion (Bình thuốc): Máu, Mana, và Tốc độ (Tốc chạy + Tốc đánh).
/// Sử dụng các bình thuốc bằng phím nóng [1], [2], [3].
/// Tự động sinh giao diện hiển thị số lượng bình thuốc trên Canvas.
/// </summary>
public class PotionSystem : MonoBehaviour
{
    [Header("--- Cấu hình Potion Sprites ---")]
    public Sprite healthPotionSprite;
    public Sprite manaPotionSprite;
    public Sprite speedPotionSprite;

    [Header("--- Số lượng Potion ban đầu ---")]
    public int healthPotionCount = 3;
    public int manaPotionCount = 3;
    public int speedPotionCount = 3;

    [Header("--- Chỉ số phục hồi ---")]
    public int healthHealAmount = 50;
    public float manaRestoreAmount = 50f;

    [Header("--- Hiệu ứng Tốc độ (Speed Buff) ---")]
    public float speedBuffMultiplier = 1.5f;
    public float speedBuffDuration = 5f;

    // Trạng thái Buff Tốc độ
    private bool isSpeedBuffActive = false;
    private float speedBuffTimer = 0f;
    private float originalMoveSpeed;

    // References
    private PlayerStats playerStats;
    private PlayerController playerController;

    // UI References (Text số lượng)
    private Text healthCountText;
    private Text manaCountText;
    private Text speedCountText;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        playerController = GetComponent<PlayerController>();

        // Tự động khởi tạo giao diện Potion HUD ở Runtime
        CreatePotionHUD();
    }

    void Update()
    {
        // Nếu nhân vật đã chết thì không cho uống thuốc
        if (playerController != null && playerController.IsDead())
        {
            // Tránh trường hợp bị kẹt tốc độ buff nếu chết trong khi đang buff
            if (isSpeedBuffActive)
            {
                EndSpeedBuff();
            }
            return;
        }

        // Nhận phím nóng uống bình thuốc (Alpha1, Alpha2, Alpha3 hoặc Keypad1, Keypad2, Keypad3)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            UseHealthPotion();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            UseManaPotion();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            UseSpeedPotion();
        }

        // Quản lý thời gian hiệu lực của buff Tốc độ
        if (isSpeedBuffActive)
        {
            speedBuffTimer -= Time.deltaTime;
            if (speedBuffTimer <= 0f)
            {
                EndSpeedBuff();
            }
        }
    }

    public void UseHealthPotion()
    {
        if (playerStats == null) return;
        
        if (healthPotionCount <= 0)
        {
            return;
        }

        healthPotionCount--;
        playerStats.Heal(healthHealAmount);

        // Kích hoạt hiệu ứng hào quang xoáy màu xanh lá & chớp xanh lá trên sprite nhân vật
        if (playerController != null)
        {
            playerController.SpawnAuraEffect(new Color(0.2f, 1f, 0.2f, 0.8f), 35);
            StartCoroutine(FlashSpriteColor(new Color(0.2f, 1f, 0.2f, 1f), 0.25f));
        }

        UpdateCountUI();
    }

    public void UseManaPotion()
    {
        if (playerStats == null) return;

        if (manaPotionCount <= 0)
        {
            return;
        }

        manaPotionCount--;
        playerStats.RestoreMana(manaRestoreAmount);

        // Kích hoạt hiệu ứng hào quang xoáy màu xanh dương & chớp xanh dương trên sprite nhân vật
        if (playerController != null)
        {
            playerController.SpawnAuraEffect(new Color(0f, 0.8f, 1f, 0.8f), 35);
            StartCoroutine(FlashSpriteColor(new Color(0f, 0.7f, 1f, 1f), 0.25f));
        }

        UpdateCountUI();
    }

    public void UseSpeedPotion()
    {
        if (speedPotionCount <= 0)
        {
            return;
        }

        speedPotionCount--;
        StartSpeedBuff();

        // Kích hoạt hiệu ứng hào quang xoáy màu vàng Gold & chớp vàng Gold trên sprite nhân vật
        if (playerController != null)
        {
            playerController.SpawnAuraEffect(new Color(1f, 0.85f, 0f, 0.8f), 35);
            StartCoroutine(FlashSpriteColor(new Color(1f, 0.85f, 0f, 1f), 0.25f));
        }

        UpdateCountUI();
    }

    public void AddHealthPotion(int amount = 1)
    {
        healthPotionCount += amount;
        UpdateCountUI();
    }

    public void AddManaPotion(int amount = 1)
    {
        manaPotionCount += amount;
        UpdateCountUI();
    }

    public void AddSpeedPotion(int amount = 1)
    {
        speedPotionCount += amount;
        UpdateCountUI();
    }

    private void StartSpeedBuff()
    {
        // Nếu đang có buff rồi thì reset lại thời gian buff
        if (isSpeedBuffActive)
        {
            speedBuffTimer = speedBuffDuration;
            return;
        }

        isSpeedBuffActive = true;
        speedBuffTimer = speedBuffDuration;

        if (playerController != null)
        {
            originalMoveSpeed = playerController.moveSpeed;
            // Tăng tốc chạy lên 1.5 lần
            playerController.moveSpeed = originalMoveSpeed * speedBuffMultiplier;

            // Tăng tốc độ chạy hoạt ảnh Animator lên 1.5 lần (bao gồm cả chém và chạy nhảy)
            Animator anim = playerController.GetComponent<Animator>();
            if (anim != null)
            {
                anim.speed = 1.5f;
            }
        }
    }

    private void EndSpeedBuff()
    {
        isSpeedBuffActive = false;

        if (playerController != null)
        {
            // Trả tốc độ chạy về nguyên bản
            playerController.moveSpeed = originalMoveSpeed > 0 ? originalMoveSpeed : 5.0f;

            // Trả tốc độ Animator về bình thường (1.0f)
            Animator anim = playerController.GetComponent<Animator>();
            if (anim != null && !playerController.IsDead())
            {
                anim.speed = 1.0f;
            }
        }
    }

    private System.Collections.IEnumerator FlashSpriteColor(Color flashColor, float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null && playerController != null)
        {
            Color original = sr.color;
            sr.color = flashColor;
            yield return new WaitForSeconds(duration);
            
            // Trả về màu cũ nếu người chơi không bị chết
            if (!playerController.IsDead())
            {
                sr.color = original;
            }
        }
    }

    private void CreatePotionHUD()
    {
        Canvas canvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // Tìm Canvas có tên phù hợp thuộc cùng Scene với Player
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.scene.name == gameObject.scene.name && 
                (c.name.ToUpper().Contains("HUD") || c.name.ToUpper().Contains("GAME") || c.name.ToUpper().Contains("PLAY")))
            {
                canvas = c;
                break;
            }
        }
        
        // Nếu không thấy, lấy bất kỳ Canvas nào thuộc cùng Scene
        if (canvas == null)
        {
            foreach (Canvas c in canvases)
            {
                if (c.gameObject.scene.name == gameObject.scene.name)
                {
                    canvas = c;
                    break;
                }
            }
        }

        // Dự phòng cuối cùng nếu vẫn không thấy Canvas nào
        if (canvas == null && canvases.Length > 0)
        {
            canvas = canvases[0];
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            Debug.Log("[PotionSystem] Tự động tạo HUD_Canvas vì không tìm thấy Canvas nào trong Scene.");
        }
        else
        {
            canvas.gameObject.SetActive(true);
        }

        // 1. Tạo Container chính cho Potion HUD
        GameObject hudObj = new GameObject("PotionHUD");
        hudObj.transform.SetParent(canvas.transform, false);
        
        // Đưa HUD lên trên cùng (vẽ sau các panel khác) để không bị đè khuất
        hudObj.transform.SetAsLastSibling();

        RectTransform hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        // Đặt ở vị trí X=40, Y=-130 (dưới thanh HP/Mana)
        hudRect.anchoredPosition = new Vector2(40f, -130f); 
        hudRect.sizeDelta = new Vector2(250f, 65f);

        // 2. Tạo 3 Slot Potion (Máu - Mana - Tốc độ) xếp ngang
        CreatePotionSlot(hudObj.transform, 0, "Health", healthPotionSprite, "[1]", out healthCountText);
        CreatePotionSlot(hudObj.transform, 1, "Mana", manaPotionSprite, "[2]", out manaCountText);
        CreatePotionSlot(hudObj.transform, 2, "Speed", speedPotionSprite, "[3]", out speedCountText);

        UpdateCountUI();
    }

    private void CreatePotionSlot(Transform parent, int index, string slotName, Sprite iconSprite, string hotkeyLabel, out Text countTextOut)
    {
        // Tạo Slot container
        GameObject slotObj = new GameObject($"{slotName}Slot");
        slotObj.transform.SetParent(parent, false);

        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, 0.5f);
        slotRect.anchorMax = new Vector2(0f, 0.5f);
        slotRect.pivot = new Vector2(0f, 0.5f);
        slotRect.anchoredPosition = new Vector2(index * 75f, 0f);
        slotRect.sizeDelta = new Vector2(56f, 56f);

        // Tạo Background Bo Góc sắc nét màu tối
        Image bgImage = slotObj.AddComponent<Image>();
        bgImage.sprite = CreateRoundedRectSprite(64, 64, 10, new Color(0.1f, 0.1f, 0.1f, 0.85f), new Color(0.35f, 0.35f, 0.35f, 0.7f), 2);

        // Tạo default sprite nếu chưa được gán để tránh hiển thị ô vuông trắng trống
        if (iconSprite == null)
        {
            if (slotName == "Health") iconSprite = CreateRoundedRectSprite(48, 48, 8, new Color(0.85f, 0.15f, 0.15f, 1f), Color.white, 2);
            else if (slotName == "Mana") iconSprite = CreateRoundedRectSprite(48, 48, 8, new Color(0.15f, 0.45f, 0.85f, 1f), Color.white, 2);
            else if (slotName == "Speed") iconSprite = CreateRoundedRectSprite(48, 48, 8, new Color(0.85f, 0.7f, 0.15f, 1f), Color.white, 2);
        }

        // Tạo Icon Potion bên trong
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.preserveAspect = true;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = new Vector2(-16f, -16f); // Đệm viền xung quanh icon
        iconRect.anchoredPosition = new Vector2(0f, 2f);

        // Tạo nhãn phím nóng (Hotkey: [1], [2], [3]) ở góc trên bên trái
        GameObject hotkeyObj = new GameObject("Hotkey");
        hotkeyObj.transform.SetParent(slotObj.transform, false);

        Text hotkeyText = hotkeyObj.AddComponent<Text>();
        Font customFont = FindCustomFontAtRuntime();
        if (customFont == null) customFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (customFont == null) customFont = Font.CreateDynamicFontFromOSFont("Arial", 11);
        
        hotkeyText.font = customFont;
        hotkeyText.fontSize = 11;
        hotkeyText.fontStyle = FontStyle.Bold;
        hotkeyText.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        hotkeyText.text = hotkeyLabel;
        hotkeyText.alignment = TextAnchor.UpperLeft;
        hotkeyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        hotkeyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform hotkeyRect = hotkeyObj.GetComponent<RectTransform>();
        hotkeyRect.anchorMin = new Vector2(0f, 1f);
        hotkeyRect.anchorMax = new Vector2(0f, 1f);
        hotkeyRect.pivot = new Vector2(0f, 1f);
        hotkeyRect.anchoredPosition = new Vector2(4f, -3f);
        hotkeyRect.sizeDelta = new Vector2(25f, 14f);

        Shadow shadowHotkey = hotkeyObj.AddComponent<Shadow>();
        shadowHotkey.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadowHotkey.effectDistance = new Vector2(1f, -1f);

        // Tạo Text số lượng Potion ở góc dưới bên phải
        GameObject countObj = new GameObject("Count");
        countObj.transform.SetParent(slotObj.transform, false);

        Text countText = countObj.AddComponent<Text>();
        countText.font = customFont;
        countText.fontSize = 14;
        countText.fontStyle = FontStyle.Bold;
        countText.color = Color.white;
        countText.alignment = TextAnchor.LowerRight;
        countText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-4f, 4f);
        countRect.sizeDelta = new Vector2(30f, 18f);

        Shadow countShadow = countObj.AddComponent<Shadow>();
        countShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        countShadow.effectDistance = new Vector2(1f, -1f);

        // Tạo Text nhãn tên Việt hóa (Bánh Mì / Trà Sữa / Cà Phê) bên dưới slot
        GameObject nameLabelObj = new GameObject("NameLabel");
        nameLabelObj.transform.SetParent(slotObj.transform, false);

        Text nameText = nameLabelObj.AddComponent<Text>();
        nameText.font = customFont;
        nameText.fontSize = 10;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = new Color(0.9f, 0.85f, 0.6f, 0.95f); // Vàng nhạt dễ đọc
        if (slotName == "Health") nameText.text = "Bánh Mì";
        else if (slotName == "Mana") nameText.text = "Trà Sữa";
        else if (slotName == "Speed") nameText.text = "Cà Phê";
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform nameLabelRect = nameLabelObj.GetComponent<RectTransform>();
        nameLabelRect.anchorMin = new Vector2(0.5f, 0f);
        nameLabelRect.anchorMax = new Vector2(0.5f, 0f);
        nameLabelRect.pivot = new Vector2(0.5f, 1f);
        nameLabelRect.anchoredPosition = new Vector2(0f, -4f);
        nameLabelRect.sizeDelta = new Vector2(70f, 15f);

        Shadow nameLabelShadow = nameLabelObj.AddComponent<Shadow>();
        nameLabelShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        nameLabelShadow.effectDistance = new Vector2(1f, -1f);

        countTextOut = countText;
    }

    private void UpdateCountUI()
    {
        if (healthCountText != null) healthCountText.text = $"x{healthPotionCount}";
        if (manaCountText != null) manaCountText.text = $"x{manaPotionCount}";
        if (speedCountText != null) speedCountText.text = $"x{speedPotionCount}";
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
}
