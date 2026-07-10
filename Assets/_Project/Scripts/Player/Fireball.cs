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
    public float trailInterval = 0.04f;

    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private float trailTimer = 0f;

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

    void Update()
    {
        // Sinh đuôi lửa khi bay
        trailTimer -= Time.deltaTime;
        if (trailTimer <= 0f)
        {
            SpawnFireTrail();
            trailTimer = trailInterval;
        }
    }

    private void SpawnFireTrail()
    {
        GameObject trail = new GameObject("FireballTrail");
        Vector3 offset = -moveDirection * 0.3f + (Vector2)Random.insideUnitCircle * 0.12f;
        trail.transform.position = transform.position + offset;

        SpriteRenderer sr = trail.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(16, Color.white);
        sr.color = new Color(1f, Random.Range(0.4f, 0.8f), 0.05f, 0.75f);
        sr.sortingOrder = 5;

        FireTrailParticle script = trail.AddComponent<FireTrailParticle>();
        script.Setup(Random.Range(0.2f, 0.32f));
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
        // 1. Quả cầu lửa nổ lan tỏa trung tâm
        GameObject expObj = new GameObject("ProceduralExplosion");
        expObj.transform.position = transform.position;
        
        SpriteRenderer sr = expObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(32, Color.white);
        sr.color = new Color(1f, 0.5f, 0f, 0.85f);
        sr.sortingOrder = 6;
        
        ProceduralExplosionEffect effect = expObj.AddComponent<ProceduralExplosionEffect>();
        effect.duration = 0.4f;
        effect.maxScale = 1.8f;

        // Rung camera khi nổ gần player
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.TriggerShake(0.18f, 0.22f);
        }

        // 2. Bắn thêm 10-14 hạt tia lửa bay tóe ra xung quanh
        int sparkCount = Random.Range(10, 15);
        for (int i = 0; i < sparkCount; i++)
        {
            GameObject spark = new GameObject("FireballExplosionSpark");
            spark.transform.position = transform.position;

            SpriteRenderer sparkSr = spark.AddComponent<SpriteRenderer>();
            sparkSr.sprite = CreateCircleSprite(16, Color.white);
            sparkSr.color = new Color(1f, Random.Range(0.6f, 0.95f), 0.1f, 0.95f);
            sparkSr.sortingOrder = 6;

            FireballExplosionSpark script = spark.AddComponent<FireballExplosionSpark>();
            Vector2 dir = Random.insideUnitCircle.normalized;
            script.Setup(dir, Random.Range(4.5f, 9.5f), Random.Range(0.3f, 0.45f));
        }
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

/// <summary>
/// Hiệu ứng khí nóng/đốm lửa bay bốc lên nhẹ nhàng phía sau quả cầu lửa
/// </summary>
public class FireTrailParticle : MonoBehaviour
{
    private float lifeTime;
    private float elapsed = 0f;
    private Color startColor;
    private Color endColor;
    private Vector3 startScale;

    public void Setup(float duration)
    {
        lifeTime = duration;
        startScale = new Vector3(Random.Range(0.4f, 0.8f), Random.Range(0.4f, 0.8f), 1f);
        transform.localScale = startScale;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            startColor = sr.color;
            endColor = new Color(0.8f, 0.1f, 0f, 0f); // Mờ dần thành đỏ rồi trong suốt
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / lifeTime;

        // Hơi bay lên trên một chút (như khí nóng bốc lên)
        transform.position += Vector3.up * 1.5f * Time.deltaTime;

        // Thu nhỏ và mờ dần
        transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.Lerp(startColor, endColor, progress);
        }

        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Hạt tia lửa nhỏ bắn tung tóe hướng 360 độ chịu ảnh hưởng của trọng lực khi nổ
/// </summary>
public class FireballExplosionSpark : MonoBehaviour
{
    private Vector2 velocity;
    private float gravity = 15f;
    private float lifeTime;
    private float elapsed = 0f;
    private Color startColor;
    private Color endColor;
    private Vector3 startScale;

    public void Setup(Vector2 dir, float speed, float duration)
    {
        velocity = dir * speed;
        lifeTime = duration;
        startScale = new Vector3(Random.Range(0.15f, 0.3f), Random.Range(0.15f, 0.3f), 1f);
        transform.localScale = startScale;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            startColor = sr.color;
            endColor = new Color(0.8f, 0f, 0f, 0f);
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / lifeTime;

        velocity.y -= gravity * Time.deltaTime; // Trọng lực kéo hạt xuống
        transform.position += (Vector3)velocity * Time.deltaTime;

        transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.Lerp(startColor, endColor, progress);
        }

        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
