using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    [Header("Sprite Animation Loop")]
    public Sprite[] animationFrames;
    public float frameRate = 0.12f;
    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex;
    private float lastFrameTime;

    [Header("Fake Glow Flicker")]
    public SpriteRenderer glowHaloRenderer;
    [Range(0f, 1f)] public float minAlpha = 0.15f;
    [Range(0f, 1f)] public float maxAlpha = 0.35f;
    [Range(0.01f, 0.5f)] public float flickerSpeed = 0.08f;

    private float targetAlpha;
    private float lastFlickerTime;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Tự động tìm Glow_Halo ở con nếu chưa được kéo thả
        if (glowHaloRenderer == null)
        {
            Transform child = transform.Find("Glow_Halo");
            if (child != null)
            {
                glowHaloRenderer = child.GetComponent<SpriteRenderer>();
            }
        }

        targetAlpha = minAlpha;
        lastFrameTime = Time.time;
    }

    void Update()
    {
        // 1. Chạy hoạt ảnh đổi sprite ngọn lửa
        if (animationFrames != null && animationFrames.Length > 0 && Time.time - lastFrameTime > frameRate)
        {
            currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
            lastFrameTime = Time.time;
        }

        // 2. Chạy hiệu ứng hào quang nhấp nháy giả lập ánh sáng (flicker alpha)
        if (glowHaloRenderer != null)
        {
            if (Time.time - lastFlickerTime > flickerSpeed)
            {
                targetAlpha = Random.Range(minAlpha, maxAlpha);
                lastFlickerTime = Time.time;
            }

            Color color = glowHaloRenderer.color;
            color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * (1f / flickerSpeed));
            glowHaloRenderer.color = color;
        }
    }
}
