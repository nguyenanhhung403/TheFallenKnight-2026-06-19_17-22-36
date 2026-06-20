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
    public int TotalDamage => baseDamage + bonusDamage;
    public float TotalSpeed => baseSpeed + bonusSpeed;

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
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);

        // Kích hoạt animation bị thương
        GetComponent<PlayerController>()?.TakeDamage();

        if (currentHP <= 0)
        {
            GetComponent<PlayerController>()?.Die();
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
