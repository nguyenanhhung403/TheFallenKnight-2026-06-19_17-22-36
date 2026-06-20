using UnityEngine;

/// <summary>
/// Định nghĩa các loại vật phẩm có thể nhặt được.
/// </summary>
public enum CollectibleType
{
    HealthPotion,
    ManaPotion,
    SpeedPotion
}

/// <summary>
/// Gắn script này lên các vật phẩm nằm trong màn chơi để người chơi đi qua nhặt.
/// Tích hợp hiệu ứng bay bổng (floating) và co giãn nhẹ (pulsing scale) cực kỳ sinh động.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CollectibleItem : MonoBehaviour
{
    [Header("--- Cấu hình Vật phẩm ---")]
    public CollectibleType itemType;
    public int amount = 1;

    [Header("--- Hiệu ứng trôi nổi (Floating) ---")]
    public float floatSpeed = 3f;
    public float floatAmplitude = 0.15f;

    [Header("--- Hiệu ứng Xoay 3D (Spinning Y) ---")]
    public float rotationSpeed = 150f;

    [Header("--- Hiệu ứng Co giãn nhẹ (Pulsing Scale) ---")]
    public float scalePulseSpeed = 2.5f;
    public float scalePulseAmplitude = 0.08f;

    private Vector3 startPos;
    private Vector3 baseScale;
    private bool isCollected = false;

    void Start()
    {
        startPos = transform.position;
        baseScale = transform.localScale;

        // Đảm bảo SpriteRenderer có thiết lập pixel art sắc nét
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            sr.sprite.texture.filterMode = FilterMode.Point;
        }

        // Tự động thiết lập Trigger Collider nếu chưa cấu hình
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circleCol = gameObject.AddComponent<CircleCollider2D>();
            circleCol.isTrigger = true;
            circleCol.radius = 0.4f;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (isCollected) return;

        // 1. Hiệu ứng trôi nổi lên xuống mềm mại (Floating)
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 2. Hiệu ứng nhịp tim co giãn nhẹ (Pulsing Scale)
        float scaleOffset = Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmplitude;
        transform.localScale = baseScale * (1f + scaleOffset);

        // 3. Hiệu ứng xoay tròn 3D quanh trục Y (quay lật 360 độ)
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        // Kiểm tra xem đối tượng va chạm có phải là Player không
        if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
        {
            PotionSystem potionSystem = other.GetComponent<PotionSystem>();
            if (potionSystem != null)
            {
                isCollected = true;
                
                // Gán màu hiệu ứng tương ứng với loại bình thuốc nhặt được
                Color effectColor = Color.white;
                switch (itemType)
                {
                    case CollectibleType.HealthPotion:
                        potionSystem.AddHealthPotion(amount);
                        effectColor = new Color(0.2f, 1f, 0.2f, 0.9f); // Xanh lá
                        break;
                    case CollectibleType.ManaPotion:
                        potionSystem.AddManaPotion(amount);
                        effectColor = new Color(0f, 0.7f, 1f, 0.9f);  // Xanh dương/cyan
                        break;
                    case CollectibleType.SpeedPotion:
                        potionSystem.AddSpeedPotion(amount);
                        effectColor = new Color(1f, 0.85f, 0f, 0.9f);  // Vàng Gold
                        break;
                }

                // Phát hiệu ứng hào quang xoáy màu tương ứng quanh người chơi khi nhặt được
                PlayerController pc = other.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SpawnAuraEffect(effectColor, 20);
                }

                Debug.Log($"[CollectibleItem] Người chơi đã nhặt {itemType} (+{amount})");
                
                // Tự hủy vật phẩm sau khi nhặt
                Destroy(gameObject);
            }
        }
    }
}
