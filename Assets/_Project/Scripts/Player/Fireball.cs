using UnityEngine;

/// <summary>
/// Quản lý chuyển động và va chạm của Fireball (Cầu lửa).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Fireball : MonoBehaviour
{
    [Header("--- Chỉ số ---")]
    public float speed = 12f;
    public int damage = 25;
    public float lifetime = 3f;

    [Header("--- Hiệu ứng ---")]
    public GameObject explosionPrefab; // Prefab nổ khi va chạm

    private Vector2 moveDirection;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Đảm bảo Rigidbody2D của đạn không chịu trọng lực và là Trigger hoặc Dynamic
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Đảm bảo Collider là Trigger để dễ xuyên qua và nổ
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Start()
    {
        // Tự hủy sau khoảng thời gian lifetime
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Thiết lập hướng bay của Fireball.
    /// </summary>
    public void Setup(Vector2 direction)
    {
        moveDirection = direction.normalized;
        
        // Quay mặt Sprite theo hướng bay
        if (moveDirection.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (moveDirection.x > 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
    }

    void FixedUpdate()
    {
        // Dự phòng di chuyển nếu Rigidbody2D bị tắt hoặc không hoạt động
        if (rb == null || rb.bodyType == RigidbodyType2D.Static)
        {
            transform.Translate(moveDirection * speed * Time.fixedDeltaTime, Space.World);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Tránh va chạm với Player, Trigger rỗng, hoặc các đạn khác
        if (collision.CompareTag("Player") || collision.isTrigger) return;

        // Gây sát thương nếu đối tượng có phương thức TakeDamage
        collision.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // Tạo hiệu ứng nổ và phá hủy đạn
        Explode();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Tránh va chạm với Player
        if (collision.gameObject.CompareTag("Player")) return;

        // Gây sát thương nếu đối tượng có phương thức TakeDamage
        collision.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // Tạo hiệu ứng nổ và phá hủy đạn
        Explode();
    }

    private void Explode()
    {
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 1.5f); // Tự hủy hiệu ứng nổ sau 1.5 giây
        }
        else
        {
            // Tạo hiệu ứng nổ lập trình bằng code nếu không có Prefab sẵn
            CreateProceduralExplosion();
        }

        Destroy(gameObject);
    }

    private void CreateProceduralExplosion()
    {
        GameObject expObj = new GameObject("ProceduralExplosion");
        expObj.transform.position = transform.position;
        
        SpriteRenderer sr = expObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(32, Color.white);
        sr.color = new Color(1f, 0.4f, 0f, 0.8f); // Màu cam đỏ lửa
        sr.sortingOrder = 6;
        
        ProceduralExplosionEffect effect = expObj.AddComponent<ProceduralExplosionEffect>();
        effect.duration = 0.35f;
        effect.maxScale = 1.6f;
    }

    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        float radius = size / 2f;
        float cx = radius;
        float cy = radius;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                Color c = Color.clear;
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    c = new Color(color.r, color.g, color.b, color.a * alpha);
                }
                pixels[y * size + x] = c;
            }
        }
        
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}

/// <summary>
/// Component điều khiển hiệu ứng nổ lan tỏa bằng code
/// </summary>
public class ProceduralExplosionEffect : MonoBehaviour
{
    public float duration = 0.35f;
    public float maxScale = 1.6f;
    public Color startColor = new Color(1f, 0.8f, 0.1f, 0.9f);
    public Color endColor = new Color(0.8f, 0.1f, 0f, 0f);

    private float elapsedTime = 0f;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero;
        Destroy(gameObject, duration);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / duration;
        
        // Phóng to dần
        float currentScale = Mathf.Lerp(0f, maxScale, progress);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);
        
        // Mờ dần và chuyển từ vàng sáng sang đỏ sẫm
        if (sr != null)
        {
            sr.color = Color.Lerp(startColor, endColor, progress);
        }
    }
}
