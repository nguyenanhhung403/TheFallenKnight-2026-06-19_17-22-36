using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("--- Mục tiêu ---")]
    public Transform target; // Kéo Player vào đây

    [Header("--- Tùy chỉnh ---")]
    public float smoothSpeed = 5f;    // Độ trơn (càng cao càng bám chặt)
    public Vector3 offset = new Vector3(0f, 1f, -10f); // Khoảng cách so với Player (Z phải âm!)

    void LateUpdate()
    {
        if (target == null) return;

        // Vị trí mong muốn của camera = vị trí Player + khoảng lệch
        Vector3 desiredPosition = target.position + offset;

        // Dùng Lerp để camera di chuyển mượt mà tới vị trí mong muốn
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}
