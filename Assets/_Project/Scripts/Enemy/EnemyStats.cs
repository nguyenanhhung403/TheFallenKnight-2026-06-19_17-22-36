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
        yield return new WaitForSeconds(1.5f);

        // Phát âm thanh tiêu diệt Boss và dừng BGM
        AudioManager.Instance.StopBGM();
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
}
