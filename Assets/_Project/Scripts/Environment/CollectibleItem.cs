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
/// Hỗ trợ cả trạng thái đứng yên lơ lửng và hiệu ứng rơi nảy vật lý (bouncing loot drop) từ quái vật.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CollectibleItem : MonoBehaviour
{
    [Header("--- Cấu hình Vật phẩm ---")]
    public CollectibleType itemType;
    public int amount = 1;

    [Header("--- Vật phẩm rơi tự do (Loot Drop Physics) ---")]
    [Tooltip("Nếu tích chọn, bình thuốc sẽ văng lên và rơi nảy trên mặt đất khi sinh ra (dùng khi quái chết).")]
    public bool dropOnSpawn = true;
    public float gravity = 18f;

    [Header("--- Hiệu ứng trôi nổi sau khi tiếp đất (Floating) ---")]
    public float floatSpeed = 3f;
    public float floatAmplitude = 0.12f;

    [Header("--- Hiệu ứng Xoay 3D (Spinning Y) ---")]
    public float rotationSpeed = 150f;

    [Header("--- Hiệu ứng Co giãn nhẹ (Pulsing Scale) ---")]
    public float scalePulseSpeed = 2.5f;
    public float scalePulseAmplitude = 0.08f;

    private Vector3 startPos;
    private Vector3 baseScale;
    private bool isCollected = false;

    // Các biến phụ trợ vật lý rơi tự do
    private bool isDropping = false;
    private Vector3 dropVelocity;
    private float groundY;
    private int bounceCount = 0;
    private const int MAX_BOUNCES = 2;

    void Start()
    {
        baseScale = transform.localScale;

        // Đảm bảo SpriteRenderer hiển thị pixel art sắc nét
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

        // Cấu hình vật lý rơi tự tạo khi quái rớt đồ
        if (dropOnSpawn)
        {
            isDropping = true;
            bounceCount = 0;

            // Tạo lực văng ngẫu nhiên sang trái/phải và nhảy vọt lên cao
            dropVelocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(5.5f, 7.5f), 0f);

            // Tìm mặt đất bên dưới vật phẩm sử dụng LayerMask từ PlayerController
            LayerMask groundMask = LayerMask.GetMask("Ground");
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                groundMask = pc.groundLayer;
            }

            // Raycast từ vị trí hiện tại thẳng xuống dưới để xác định cao độ mặt đất
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 15f, groundMask);
            if (hit.collider != null)
            {
                // Đặt điểm tiếp đất cao hơn mặt sàn một chút để bình thuốc chạm vừa vặn
                groundY = hit.point.y + 0.35f; 
            }
            else
            {
                // Failsafe nếu không tìm thấy đất: tự động rơi xuống 1.5 đơn vị
                groundY = transform.position.y - 1.5f;
            }
        }
        else
        {
            startPos = transform.position;
        }
    }

    void Update()
    {
        if (isCollected) return;

        if (isDropping)
        {
            // 1. Cập nhật vị trí rơi tự do
            transform.position += dropVelocity * Time.deltaTime;
            dropVelocity.y -= gravity * Time.deltaTime;

            // Xoay tròn bình thuốc trên không trung theo hướng văng (xoay trục Z khi rơi)
            transform.Rotate(Vector3.forward * -dropVelocity.x * 180f * Time.deltaTime);

            // 2. Xử lý chạm đất và nảy (Bounce)
            if (transform.position.y <= groundY)
            {
                transform.position = new Vector3(transform.position.x, groundY, transform.position.z);

                if (bounceCount < MAX_BOUNCES)
                {
                    // Đảo ngược lực rơi để nảy lên và giảm dần xung lực (dampen)
                    dropVelocity.y = -dropVelocity.y * 0.35f;
                    dropVelocity.x *= 0.5f; // Giảm tốc độ trượt ngang
                    bounceCount++;
                }
                else
                {
                    // Dừng rơi, chuyển sang chế độ lơ lửng và xoay 3D
                    isDropping = false;
                    startPos = transform.position;
                    
                    // Reset góc quay về mặc định trước khi thực hiện xoay 3D Y-axis
                    transform.rotation = Quaternion.identity;
                }
            }
        }
        else
        {
            // Trạng thái bình thường sau khi tiếp đất hoặc đặt sẵn trong Scene
            // 1. Hiệu ứng trôi nổi lên xuống mềm mại (Floating)
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // 2. Hiệu ứng nhịp tim co giãn nhẹ (Pulsing Scale)
            float scaleOffset = Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmplitude;
            transform.localScale = baseScale * (1f + scaleOffset);

            // 3. Hiệu ứng xoay tròn 3D quanh trục Y
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        // Kiểm tra va chạm với Player
        if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
        {
            PotionSystem potionSystem = other.GetComponent<PotionSystem>();
            if (potionSystem != null)
            {
                isCollected = true;
                
                Color effectColor = Color.white;
                switch (itemType)
                {
                    case CollectibleType.HealthPotion:
                        potionSystem.AddHealthPotion(amount);
                        effectColor = new Color(0.2f, 1f, 0.2f, 0.9f); // Xanh lá
                        break;
                    case CollectibleType.ManaPotion:
                        potionSystem.AddManaPotion(amount);
                        effectColor = new Color(0f, 0.7f, 1f, 0.9f);  // Xanh dương
                        break;
                    case CollectibleType.SpeedPotion:
                        potionSystem.AddSpeedPotion(amount);
                        effectColor = new Color(1f, 0.85f, 0f, 0.9f);  // Vàng Gold
                        break;
                }

                // Phát hiệu ứng vòng xoáy hào quang màu sắc xung quanh người chơi
                PlayerController pc = other.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SpawnAuraEffect(effectColor, 20);
                }

                Debug.Log($"[CollectibleItem] Đã nhặt {itemType} (+{amount})");
                
                Destroy(gameObject);
            }
        }
    }
}
