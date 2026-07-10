using UnityEngine;

/// <summary>
/// Chứa các chỉ số của Player.
/// Gắn vào cùng GameObject với PlayerController.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("--- Chỉ số gốc ---")]
    public int maxHP = 100;
    public int currentHP;
    public int baseDamage = 10;
    public float baseSpeed = 5f;

    [Header("--- Chỉ số Mana ---")]
    public int maxMP = 100;
    public float currentMP;
    public float manaRegenRate = 5f; // Hồi mana mỗi giây

    [Header("--- Chỉ số tăng thêm (từ Tế Đàn) ---")]
    public int bonusDamage = 0;
    public float bonusSpeed = 0f;

    // Chỉ số thực tế
    public int TotalDamage => isRageMode ? (baseDamage + bonusDamage) * 2 : (baseDamage + bonusDamage);
    public float TotalSpeed => isRageMode ? (baseSpeed + bonusSpeed) * 1.4f : (baseSpeed + bonusSpeed);

    [Header("--- Chỉ số Nộ (Rage / Hào Khí Đông A) ---")]
    public float currentRage = 0f;
    public float maxRage = 100f;
    public float rageDecayRate = 12.5f; // Hết nộ sau 8 giây (100/12.5 = 8s)
    public bool isRageMode = false;

    private float originalMoveSpeedRage;
    private float originalAnimSpeedRage;

    void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
    }

    void Update()
    {
        // Tự động hồi Mana theo thời gian
        if (currentMP < maxMP)
        {
            currentMP = Mathf.Min(currentMP + manaRegenRate * Time.deltaTime, maxMP);
        }

        // Tự động tiêu hao nộ khi kích hoạt Hào Khí Đông A
        if (isRageMode)
        {
            currentRage = Mathf.Max(currentRage - rageDecayRate * Time.deltaTime, 0f);
            if (currentRage <= 0f)
            {
                EndRageMode();
            }
        }
    }

    public void AddRage(float amount)
    {
        if (isRageMode) return;
        currentRage = Mathf.Min(currentRage + amount, maxRage);
    }

    public void StartRageMode()
    {
        if (isRageMode || currentRage < maxRage) return;

        isRageMode = true;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            originalMoveSpeedRage = pc.moveSpeed;
            pc.moveSpeed = originalMoveSpeedRage * 1.4f; // Tăng 40% tốc chạy

            Animator anim = pc.GetComponent<Animator>();
            if (anim != null)
            {
                originalAnimSpeedRage = anim.speed;
                anim.speed = originalAnimSpeedRage * 1.3f; // Tăng 30% tốc đánh/hoạt ảnh
            }

            // Kích hoạt hiệu ứng hào quang màu đỏ của Hào Khí Đông A
            pc.SpawnAuraEffect(new Color(1f, 0.1f, 0.1f, 0.9f), 60);

            // Đổi màu sprite nhân vật sang đỏ rực
            SpriteRenderer sr = pc.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pc.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.5f, 0.5f, 1f);
            }
        }

        AudioManager.Instance.PlaySFX(SoundEffect.BossDefeated);
        Debug.Log("[RageMode] Đã kích hoạt HÀO KHÍ ĐÔNG A! Tăng sát thương và tốc độ chạy.");
    }

    public void EndRageMode()
    {
        if (!isRageMode) return;

        isRageMode = false;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.moveSpeed = originalMoveSpeedRage > 0 ? originalMoveSpeedRage : 5f;

            Animator anim = pc.GetComponent<Animator>();
            if (anim != null)
            {
                anim.speed = originalAnimSpeedRage > 0 ? originalAnimSpeedRage : 1f;
            }

            SpriteRenderer sr = pc.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pc.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white;
            }

            pc.SpawnAuraEffect(new Color(0.5f, 0.5f, 0.5f, 0.5f), 20);
        }

        Debug.Log("[RageMode] Kết thúc HÀO KHÍ ĐÔNG A.");
    }

    public void TakeDamage(int amount)
    {
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.IsDefending())
        {
            // Đỡ đòn thành công: Không mất HP, không bị đơ/hurt
            Debug.Log("[PlayerStats] Đỡ đòn thành công! Không nhận sát thương.");
            pc.PlayBlockEffect();
            return;
        }

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);

        // Cộng nộ khi nhận sát thương
        AddRage(12f);

        if (currentHP > 0)
        {
            AudioManager.Instance.PlaySFX(SoundEffect.Hurt);
        }

        // Rung màn hình khi bị quái đánh
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.TriggerShake(0.2f, 0.25f);
        }

        // Kích hoạt animation bị thương
        pc?.TakeDamage();

        if (currentHP <= 0)
        {
            // Nếu chết thì tắt chế độ nộ
            if (isRageMode) EndRageMode();
            pc?.Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (currentHP > 0)
        {
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null && pc.IsDead())
            {
                pc.Revive();
            }
        }
    }

    public bool ConsumeMana(float amount)
    {
        if (currentMP >= amount)
        {
            currentMP -= amount;
            return true;
        }
        return false;
    }

    public void RestoreMana(float amount)
    {
        currentMP = Mathf.Min(currentMP + amount, maxMP);
    }
}
