using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("--- Mục tiêu ---")]
    public Transform target; // Kéo Player vào đây

    [Header("--- Tùy chỉnh ---")]
    public float smoothSpeed = 5f;    // Độ trơn (càng cao càng bám chặt)
    public Vector3 offset = new Vector3(0f, 1f, -10f); // Khoảng cách so với Player (Z phải âm!)

    // Biến phục vụ hiệu ứng rung màn hình (Screen Shake)
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Kích hoạt hiệu ứng rung màn hình.
    /// </summary>
    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Vị trí mong muốn của camera = vị trí Player + khoảng lệch
        Vector3 desiredPosition = target.position + offset;

        // Xử lý rung màn hình (Screen Shake)
        if (shakeDuration > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0; // Không rung theo trục Z
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // Dùng Lerp để camera di chuyển mượt mà tới vị trí mong muốn
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition + shakeOffset;
    }
}
