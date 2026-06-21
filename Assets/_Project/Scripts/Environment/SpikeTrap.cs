using UnityEngine;

/// <summary>
/// Bẫy chông gai gây sát thương cực lớn khiến người chơi chết ngay khi chạm vào.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [Tooltip("Lượng sát thương gây ra. Mặc định cực lớn để người chơi chạm vào là hẹo.")]
    public int damage = 9999;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Thử tìm PlayerStats trên đối tượng va chạm hoặc cha của nó
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = other.GetComponentInParent<PlayerStats>();
        }

        if (stats != null)
        {
            Debug.Log("[SpikeTrap] Người chơi rơi vào bẫy chông gai! Gây sát thương chí mạng.");
            stats.TakeDamage(damage);
        }
    }
}
