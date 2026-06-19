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

    [Header("--- Chỉ số tăng thêm (từ Tế Đàn) ---")]
    public int bonusDamage = 0;
    public float bonusSpeed = 0f;

    // Chỉ số thực tế
    public int TotalDamage => baseDamage + bonusDamage;
    public float TotalSpeed => baseSpeed + bonusSpeed;

    void Awake()
    {
        currentHP = maxHP;
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
    }
}
