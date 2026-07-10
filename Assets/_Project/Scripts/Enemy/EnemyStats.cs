using UnityEngine;

/// <summary>
/// Quản lý chỉ số, sát thương và cái chết của Quái vật.
/// Tích hợp tính năng rớt bình thuốc theo tỉ lệ 15% tổng thể, chia đều 33.3% cho 3 loại.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("--- Chỉ số Quái vật ---")]
    public int maxHealth = 60;
    public int currentHealth;
    public int baseDamage = 15;

    [Header("--- Tỉ lệ rớt Potion ---")]
    [Range(0f, 1f)]
    [Tooltip("Tỉ lệ rớt bình thuốc tổng thể khi quái chết (15% là 0.15).")]
    public float dropProbability = 0.15f; 

    [Tooltip("Prefab của Bình Máu.")]
    public GameObject healthPotionPrefab;
    [Tooltip("Prefab của Bình Mana.")]
    public GameObject manaPotionPrefab;
    [Tooltip("Prefab của Bình Tốc Độ.")]
    public GameObject speedPotionPrefab;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;
    private EnemyHealthBar healthBar;

    void Awake()
    {
        gameObject.tag = "Enemy";
    }

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Tự động nạp bình thuốc khi chạy game trong Editor đề phòng người chơi chưa gán trong Inspector
#if UNITY_EDITOR
        AutoAssignPotionPrefabs();
#endif

        // Tự động tạo thanh máu trên đầu
        GameObject barObj = new GameObject(gameObject.name + "_HPBar");
        healthBar = barObj.AddComponent<EnemyHealthBar>();
        
        float offsetHeight = 1.2f;
        if (spriteRenderer != null)
        {
            offsetHeight = spriteRenderer.bounds.extents.y + 0.3f;
        }
        healthBar.Setup(transform, this, offsetHeight);
    }

    /// <summary>
    /// Hàm nhận sát thương (gọi bởi chém kiếm hoặc hỏa cầu của Player).
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"[EnemyStats] {gameObject.name} nhận {amount} sát thương. Máu còn lại: {currentHealth}/{maxHealth}");

        // Hiển thị thanh máu trên đầu
        if (healthBar != null)
        {
            healthBar.Show();
        }

        // Hiệu ứng chớp đỏ báo hiệu bị thương
        StartCoroutine(FlashRed());

        EnemySpriteAnimator anim = GetComponent<EnemySpriteAnimator>();
        EnemyAI ai = GetComponent<EnemyAI>();

        if (currentHealth <= 0)
        {
            Die(anim, ai);
        }
        else
        {
            if (anim != null)
            {
                anim.PlayAnimation(EnemyState.Hit, true);
            }
            if (ai != null)
            {
                ai.GetHit();
            }
        }
    }

    private void Die(EnemySpriteAnimator anim, EnemyAI ai)
    {
        isDead = true;

        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        if (anim != null)
        {
            anim.PlayAnimation(EnemyState.Dead);
        }

        if (ai != null)
        {
            ai.DisableAI();
        }

        // Gỡ va chạm để người chơi đi qua
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Static;

        // Xử lý tỉ lệ rơi Potion
        HandleLootDrop();

        string lowerName = gameObject.name.ToLower();
        if (gameObject.name.Contains("Boss_UndeadExecutioner") || lowerName.Contains("boss") || lowerName.Contains("executioner"))
        {
            StartCoroutine(DelayBossDefeatedChoice());
        }
        else
        {
            // Cho quái tan biến dần sau khi hoạt ảnh ngã kết thúc
            StartCoroutine(FadeAndDestroy());
        }
    }

    private void HandleLootDrop()
    {
        if (Random.value <= dropProbability)
        {
            float rand = Random.value;
            GameObject prefabToSpawn = null;

            // Chia đều 33.3% cơ hội cho 3 loại bình thuốc
            if (rand < 0.3333f)
            {
                prefabToSpawn = healthPotionPrefab;
            }
            else if (rand < 0.6666f)
            {
                prefabToSpawn = manaPotionPrefab;
            }
            else
            {
                prefabToSpawn = speedPotionPrefab;
            }

            if (prefabToSpawn != null)
            {
                // Instantiate bình thuốc tại vị trí quái chết
                GameObject spawnedPotion = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
                
                // Đảm bảo bình thuốc khi rớt từ quái sẽ có hiệu ứng văng nảy lên vật lý
                CollectibleItem item = spawnedPotion.GetComponent<CollectibleItem>();
                if (item != null)
                {
                    item.dropOnSpawn = true;
                }
                
                Debug.Log($"[LootDrop] {gameObject.name} bị tiêu diệt -> Rớt bình thuốc: {prefabToSpawn.name}");
            }
        }
    }

    private System.Collections.IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            if (!isDead)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private System.Collections.IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(1.2f);

        float duration = 1.0f;
        float elapsed = 0f;
        Color startCol = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(startCol.r, startCol.g, startCol.b, alpha);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tự động gán các prefab bình thuốc khi có thay đổi hoặc tải cấu hình trong Editor
        AutoAssignPotionPrefabs();
    }

    private void AutoAssignPotionPrefabs()
    {
        bool changed = false;
        if (healthPotionPrefab == null)
        {
            healthPotionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Potions/Collectible_HealthPotion.prefab");
            if (healthPotionPrefab != null) changed = true;
        }
        if (manaPotionPrefab == null)
        {
            manaPotionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Potions/Collectible_ManaPotion.prefab");
            if (manaPotionPrefab != null) changed = true;
        }
        if (speedPotionPrefab == null)
        {
            speedPotionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Potions/Collectible_SpeedPotion.prefab");
            if (speedPotionPrefab != null) changed = true;
        }

        if (changed && !Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private System.Collections.IEnumerator DelayBossDefeatedChoice()
    {
        yield return new WaitForSeconds(1.2f);

        // Dừng nhạc nền để chuyển sang không khí u sầu đối thoại
        AudioManager.Instance.StopBGM();

        // Kích hoạt cuộc đối thoại trăn trối khi Boss bại trận
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null && DialogueManager.Instance != null)
        {
            List<DialogueLine> deathLines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    speakerName = "Tráng Sĩ Sơn Nam",
                    speakerTransform = transform,
                    text = "Hự... U minh hắc khí... cuối cùng cũng đã tan biến... Ta... ta được giải thoát rồi sao?"
                },
                new DialogueLine
                {
                    speakerName = "Tráng Sĩ",
                    speakerTransform = player.transform,
                    text = "Tiền bối! Tà niệm đã tiêu tan, thần hồn của ông đã trở lại thanh tịnh!"
                },
                new DialogueLine
                {
                    speakerName = "Tráng Sĩ Sơn Nam",
                    speakerTransform = transform,
                    text = "Cảm ơn tráng sĩ... Hào khí Đông A vạn thuở lưu truyền... Giờ đây cõi giang sơn này... ta xin phó thác lại cho ngươi..."
                }
            };

            bool dialogueDone = false;
            DialogueManager.Instance.StartDialogue(deathLines, () => {
                dialogueDone = true;
            });

            // Chờ đối thoại kết thúc
            while (!dialogueDone)
            {
                yield return null;
            }
        }

        // Phát âm thanh tiêu diệt Boss (Chiến thắng)
        AudioManager.Instance.PlaySFX(SoundEffect.BossDefeated);

        GameObject bdPanel = null;

        // 1. Tìm thông qua tất cả Canvas
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            Transform tr = c.transform.Find("BossDefeatedPanel");
            if (tr != null)
            {
                bdPanel = tr.gameObject;
                break;
            }
        }

        // 2. Nếu vẫn không thấy, quét toàn bộ GameObject trong Scene (bao gồm cả ẩn)
        if (bdPanel == null)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allObjects)
            {
                if (go.name == "BossDefeatedPanel" && go.scene.isLoaded)
                {
                    bdPanel = go;
                    break;
                }
            }
        }

        if (bdPanel != null)
        {
            bdPanel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield break;
        }

        Debug.LogWarning("[EnemyStats] Không tìm thấy BossDefeatedPanel trong Scene, quay lại Main Menu...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
    }
}

/// <summary>
/// Thanh máu nhỏ gọn hiển thị phía trên đầu quái vật khi nhận sát thương
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private Transform enemy;
    private EnemyStats stats;
    private float heightOffset = 1.2f;
    private float hideTimer = 0f;
    private float showDuration = 2.5f;

    private SpriteRenderer bgRenderer;
    private SpriteRenderer fillRenderer;
    
    private float targetFillScaleX = 1f;
    private float currentFillScaleX = 1f;

    public void Setup(Transform enemyTransform, EnemyStats enemyStats, float offset)
    {
        enemy = enemyTransform;
        stats = enemyStats;
        heightOffset = offset;
        
        // Ẩn ban đầu
        gameObject.SetActive(false);
    }

    void Start()
    {
        Sprite barSprite = CreateBarSprite();

        // 1. Tạo Background (Đen)
        GameObject bgObj = new GameObject("HPBar_BG");
        bgObj.transform.SetParent(transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(1.2f, 0.16f, 1f);

        bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = barSprite;
        bgRenderer.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        bgRenderer.sortingOrder = 30;

        // 2. Tạo Fill (Đỏ/Xanh)
        GameObject fillObj = new GameObject("HPBar_Fill");
        fillObj.transform.SetParent(transform);
        fillObj.transform.localPosition = new Vector3(-0.6f, 0f, 0f);
        fillObj.transform.localScale = new Vector3(1.2f, 0.16f, 1f);

        fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.color = new Color(0.9f, 0.15f, 0.15f, 0.95f);
        fillRenderer.sortingOrder = 31;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        hideTimer = showDuration;
        UpdateBarVisuals();
    }

    void LateUpdate()
    {
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        // Bám theo vị trí của quái vật trong World Space
        transform.position = enemy.position + Vector3.up * heightOffset;

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
        {
            gameObject.SetActive(false);
        }

        if (stats != null)
        {
            float pct = (float)stats.currentHealth / stats.maxHealth;
            targetFillScaleX = pct * 1.2f;
        }
        currentFillScaleX = Mathf.MoveTowards(currentFillScaleX, targetFillScaleX, 3f * Time.deltaTime);
        
        if (fillRenderer != null)
        {
            fillRenderer.transform.localScale = new Vector3(currentFillScaleX, 0.16f, 1f);
            fillRenderer.transform.localPosition = new Vector3(-0.6f + (currentFillScaleX / 2f), 0f, 0f);
            
            float pct = stats != null ? (float)stats.currentHealth / stats.maxHealth : 1f;
            fillRenderer.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f, 0.95f), new Color(0.2f, 0.85f, 0.2f, 0.95f), pct);
        }
    }

    private void UpdateBarVisuals()
    {
        if (stats != null)
        {
            float pct = (float)stats.currentHealth / stats.maxHealth;
            targetFillScaleX = pct * 1.2f;
            currentFillScaleX = targetFillScaleX;
        }
    }

    private Sprite CreateBarSprite()
    {
        int w = 16;
        int h = 4;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
