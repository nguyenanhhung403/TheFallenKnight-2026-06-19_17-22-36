using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tế Đàn - Khi Player đến gần và nhấn E:
/// - Đóng băng game (timeScale = 0)
/// - Hiển thị UI chọn 1 trong 3 bình thuốc tăng chỉ số
/// - Sau khi chọn, ẩn UI và vô hiệu hóa Tế Đàn
/// </summary>
public class Shrine : MonoBehaviour
{
    [Header("--- Cấu hình ---")]
    public float interactRange = 2f;   // Bán kính tương tác
    public KeyCode interactKey = KeyCode.E;

    [Header("--- UI ---")]
    public GameObject upgradePanel;   // Kéo Upgrade Panel (Canvas/Panel) vào đây

    [Header("--- Hiển thị gợi ý ---")]
    public GameObject interactHint;   // Object hiện chữ "[E] Tương tác" khi đứng gần

    // Tham chiếu tới Player để tăng chỉ số
    private Transform player;
    private PlayerStats playerStats;
    private bool isActivated = false;
    private bool playerInRange = false;

    void Start()
    {
        // Tìm Player trong Scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }

        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (interactHint != null) interactHint.SetActive(false);
    }

    void Update()
    {
        if (isActivated || player == null) return;

        // Kiểm tra khoảng cách Player với Tế Đàn
        float distance = Vector2.Distance(transform.position, player.position);
        playerInRange = distance <= interactRange;

        // Hiện/Ẩn gợi ý tương tác
        if (interactHint != null)
            interactHint.SetActive(playerInRange);

        // Nhấn E khi đứng trong vùng
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            OpenUpgradePanel();
        }
    }

    private void OpenUpgradePanel()
    {
        Time.timeScale = 0f; // Đóng băng game
        if (upgradePanel != null) upgradePanel.SetActive(true);
        if (interactHint != null) interactHint.SetActive(false);
    }

    // Gọi từ các nút bấm trên UI (Button onClick)
    public void ChooseUpgrade_Damage()
    {
        if (playerStats != null) playerStats.bonusDamage += 5;
        CloseUpgradePanel();
    }

    public void ChooseUpgrade_HP()
    {
        if (playerStats != null) playerStats.maxHP += 20;
        CloseUpgradePanel();
    }

    public void ChooseUpgrade_Speed()
    {
        if (playerStats != null) playerStats.bonusSpeed += 1f;
        CloseUpgradePanel();
    }

    private void CloseUpgradePanel()
    {
        Time.timeScale = 1f; // Trả lại tốc độ game
        if (upgradePanel != null) upgradePanel.SetActive(false);
        isActivated = true;  // Vô hiệu hóa Tế Đàn sau khi dùng

        // Tắt visual của Tế Đàn (sprite mờ đi hoặc ẩn)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    // Hiển thị vùng tương tác trong Scene Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
