using UnityEngine;

/// <summary>
/// Đặt component này lên một GameObject rỗng (Node) trên Map.
/// Khi game Start, nó sẽ tự động Instantiate Prefab quái tại vị trí của mình.
/// </summary>
public class EnemySpawnNode : MonoBehaviour
{
    [Header("--- Cấu hình Spawn ---")]
    public GameObject enemyPrefab; // Kéo Prefab quái vào đây trong Inspector
    public float spawnDelay = 0f;  // Delay trước khi spawn (giây)

    void Start()
    {
        if (enemyPrefab == null) return;

        if (spawnDelay <= 0f)
            Spawn();
        else
            Invoke(nameof(Spawn), spawnDelay);
    }

    private void Spawn()
    {
        Instantiate(enemyPrefab, transform.position, Quaternion.identity);
    }

    // Hiển thị vị trí Node trong Scene Editor để dễ đặt
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawIcon(transform.position, "sv_icon_dot4_pix16_gizmo", true);
    }
}
