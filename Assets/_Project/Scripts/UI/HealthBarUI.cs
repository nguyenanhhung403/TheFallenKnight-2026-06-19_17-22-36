using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên Canvas/Panel chứa thanh máu.
/// Tự động cập nhật thanh máu theo PlayerStats.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("--- Kết nối UI ---")]
    public Slider healthSlider;       // Thanh trượt máu
    public Image fillImage;           // Hình ảnh fill của Slider (để đổi màu)

    [Header("--- Màu sắc ---")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;

    // Tham chiếu
    private PlayerStats playerStats;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            healthSlider.maxValue = playerStats.maxHP;
            healthSlider.value = playerStats.currentHP;
        }
    }

    void Update()
    {
        if (playerStats == null || healthSlider == null) return;

        // Cập nhật giá trị thanh máu mỗi frame
        healthSlider.value = Mathf.Lerp(healthSlider.value, playerStats.currentHP, Time.deltaTime * 10f);

        // Đổi màu dần từ xanh -> đỏ khi máu giảm
        if (fillImage != null)
        {
            float hpPercent = (float)playerStats.currentHP / playerStats.maxHP;
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, hpPercent);
        }
    }
}
