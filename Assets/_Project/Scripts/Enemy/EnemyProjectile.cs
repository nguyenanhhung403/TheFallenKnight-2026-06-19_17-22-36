using UnityEngine;

/// <summary>
/// Quản lý đường đạn (mũi tên) bắn ra từ quái vật tầm xa.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("--- Cấu hình đạn ---")]
    public float speed = 10f;
    public int damage = 10;
    public float lifetime = 4f;

    private Vector2 direction;
    private bool initialized = false;

    public void Setup(Vector2 dir, int dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        initialized = true;

        // Xoay mũi tên theo hướng bay
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!initialized) return;

        // Di chuyển đạn theo hệ tọa độ World
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Va chạm với Player -> Gây sát thương
        if (collision.CompareTag("Player"))
        {
            PlayerStats stats = collision.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        // Va chạm với mặt đất (Ground) -> Tan biến
        else if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Ground")) != 0)
        {
            Destroy(gameObject);
        }
    }
}
