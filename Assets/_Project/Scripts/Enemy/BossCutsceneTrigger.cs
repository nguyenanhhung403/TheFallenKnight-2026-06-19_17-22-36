using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vùng kích hoạt đối thoại cắt cảnh khi người chơi tiếp cận Boss.
/// Tự động thêm BoxCollider2D và thiết lập là Trigger.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BossCutsceneTrigger : MonoBehaviour
{
    [Header("--- Cấu hình Vùng Kích Hoạt ---")]
    [Tooltip("Khoảng cách kích hoạt (chiều rộng Collider)")]
    public float triggerWidth = 3f;
    public float triggerHeight = 5f;

    private bool hasTriggered = false;
    private BoxCollider2D col;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector2(triggerWidth, triggerHeight);
        }
    }

    void Start()
    {
        // Tự động gán Tag "Enemy" hoặc tag thích hợp cho Boss nếu trigger được đính kèm trực tiếp vào Boss
        // Nhưng nếu trigger được đính kèm vào một đối tượng rỗng đặt trước mặt Boss thì ta cần tìm Boss trong Scene
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        // Nhận diện Player
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null || player.IsDead()) return;

            // Tìm quái vật Boss trong Scene
            GameObject boss = GameObject.Find("Boss_UndeadExecutioner");
            if (boss == null)
            {
                // Fallback nếu người chơi đổi tên Boss
                EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
                foreach (var enemy in allEnemies)
                {
                    string nameLower = enemy.gameObject.name.ToLower();
                    if (nameLower.Contains("boss") || nameLower.Contains("executioner"))
                    {
                        boss = enemy.gameObject;
                        break;
                    }
                }
            }

            if (boss != null)
            {
                hasTriggered = true;
                StartCutsceneDialogue(player.transform, boss.transform);
            }
        }
    }

    private void StartCutsceneDialogue(Transform playerTransform, Transform bossTransform)
    {
        if (DialogueManager.Instance == null)
        {
            // Dự phòng tạo DialogueManager nếu chưa có trong Scene
            GameObject dmObj = new GameObject("DialogueManager");
            dmObj.AddComponent<DialogueManager>();
        }

        // Tạo kịch bản hội thoại đậm chất lịch sử văn hóa Việt Nam u linh
        List<DialogueLine> lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = "Tráng Sĩ Sơn Nam",
                speakerTransform = bossTransform,
                text = "Kẻ phàm trần kia... Hào khí Đông A của ngươi không thể thanh tẩy được linh hồn tà u này!"
            },
            new DialogueLine
            {
                speakerName = "Tráng Sĩ",
                speakerTransform = playerTransform,
                text = "Tiền bối! Tà khí u minh đã che mờ u linh của ông. Thần kiếm Đông A của con sẽ giúp ông thức tỉnh!"
            },
            new DialogueLine
            {
                speakerName = "Tráng Sĩ Sơn Nam",
                speakerTransform = bossTransform,
                text = "Hãy cẩn thận! Hắc khí trong ta đang cuộn trào... Hãy đỡ lấy lưỡi đao U Minh này!"
            }
        };

        // Bắt đầu chạy hội thoại rạp phim
        DialogueManager.Instance.StartDialogue(lines, OnDialogueComplete);
    }

    private void OnDialogueComplete()
    {
        Debug.Log("[BossCutscene] Hội thoại đã kết thúc! Trận chiến Boss bắt đầu!");
        
        // Tự hủy Trigger để không bao giờ kích hoạt lại nữa
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Vẽ vùng kích hoạt màu xanh lá cây trong Editor để dễ quan sát
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawCube(transform.position, new Vector3(triggerWidth, triggerHeight, 1f));
    }
}
