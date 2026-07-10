using UnityEngine;

/// <summary>
/// Quản lý hiệu ứng di chuyển đa chiều (Parallax Scrolling) cho các lớp nền 2D.
/// Tự động đo kích thước ảnh và lặp lại nền vô hạn (Tiling/Looping) khi Camera di chuyển sang trái/phải.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("--- Hiệu ứng Parallax ---")]
    [Range(0f, 1f)]
    [Tooltip("Tốc độ trượt tương đối theo camera. (1: Trôi cùng camera như bầu trời, 0.5: Trôi vừa như núi, 0: Đứng yên như mặt đất)")]
    public float parallaxEffectX = 0.5f;
    [Range(0f, 1f)]
    public float parallaxEffectY = 0.05f;

    [Header("--- Lặp lại nền vô hạn ---")]
    public bool infiniteHorizontal = true;
    public bool infiniteVertical = false;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;
    private float textureUnitSizeY;

    void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            lastCameraPosition = cameraTransform.position;
        }

        // Tự động đo đạc kích thước thực tế của SpriteRenderer trong game
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Sprite sprite = spriteRenderer.sprite;
            if (sprite != null)
            {
                textureUnitSizeX = (sprite.rect.width / sprite.pixelsPerUnit) * Mathf.Abs(transform.localScale.x);
                textureUnitSizeY = (sprite.rect.height / sprite.pixelsPerUnit) * Mathf.Abs(transform.localScale.y);
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Tính khoảng cách camera đã di chuyển trong khung hình này
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Di chuyển lớp nền theo tỉ lệ trượt
        transform.position += new Vector3(deltaMovement.x * parallaxEffectX, deltaMovement.y * parallaxEffectY, 0f);

        lastCameraPosition = cameraTransform.position;

        // Xử lý cuốn nền vô cực theo chiều ngang
        if (infiniteHorizontal && textureUnitSizeX > 0f)
        {
            if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
            {
                float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
                transform.position = new Vector3(cameraTransform.position.x - offsetPositionX, transform.position.y, transform.position.z);
            }
        }

        // Xử lý cuốn nền vô cực theo chiều dọc
        if (infiniteVertical && textureUnitSizeY > 0f)
        {
            if (Mathf.Abs(cameraTransform.position.y - transform.position.y) >= textureUnitSizeY)
            {
                float offsetPositionY = (cameraTransform.position.y - transform.position.y) % textureUnitSizeY;
                transform.position = new Vector3(transform.position.x, cameraTransform.position.y - offsetPositionY, transform.position.z);
            }
        }
    }
}
