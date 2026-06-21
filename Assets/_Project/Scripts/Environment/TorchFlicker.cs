using UnityEngine;

#if UNITY_2021_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

public class TorchFlicker : MonoBehaviour
{
#if UNITY_2021_2_OR_NEWER
    private Light2D light2D;
#endif
    private Light pointLight; // Fallback cho 3D Point Light nếu dự án không cài đặt URP 2D

    [Header("Sprite Animation Loop")]
    public Sprite[] animationFrames;
    public float frameRate = 0.12f;
    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex;
    private float lastFrameTime;

    [Header("Intensity Settings")]
    [Range(0f, 5f)] public float minIntensity = 0.8f;
    [Range(0f, 5f)] public float maxIntensity = 1.3f;
    [Range(0.01f, 0.5f)] public float flickerSpeed = 0.08f;

    [Header("Radius Settings (Optional)")]
    public bool flickerRadius = true;
    [Range(0f, 10f)] public float minRadius = 2.8f;
    [Range(0f, 10f)] public float maxRadius = 3.2f;

    private float targetIntensity;
    private float targetRadius;
    private float lastFlickerTime;

    void Start()
    {
#if UNITY_2021_2_OR_NEWER
        light2D = GetComponent<Light2D>();
#endif
        pointLight = GetComponent<Light>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Thiết lập giá trị ban đầu
        targetIntensity = minIntensity;
        targetRadius = minRadius;
        lastFrameTime = Time.time;
    }

    void Update()
    {
        // 1. Chạy hiệu ứng hoạt hình ngọn lửa cháy (Sprite Animation)
        if (animationFrames != null && animationFrames.Length > 0 && Time.time - lastFrameTime > frameRate)
        {
            currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
            lastFrameTime = Time.time;
        }

        // 2. Đến thời điểm cập nhật ngẫu nhiên cường độ sáng mới (Flicker)
        if (Time.time - lastFlickerTime > flickerSpeed)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            targetRadius = Random.Range(minRadius, maxRadius);
            lastFlickerTime = Time.time;
        }

        // Nội suy mượt mà (Lerp) sang cường độ mới
#if UNITY_2021_2_OR_NEWER
        if (light2D != null)
        {
            light2D.intensity = Mathf.Lerp(light2D.intensity, targetIntensity, Time.deltaTime * (1f / flickerSpeed));
            if (flickerRadius)
            {
                light2D.pointLightOuterRadius = Mathf.Lerp(light2D.pointLightOuterRadius, targetRadius, Time.deltaTime * (1f / flickerSpeed));
            }
            return;
        }
#endif

        if (pointLight != null)
        {
            pointLight.intensity = Mathf.Lerp(pointLight.intensity, targetIntensity, Time.deltaTime * (1f / flickerSpeed));
            if (flickerRadius)
            {
                pointLight.range = Mathf.Lerp(pointLight.range, targetRadius, Time.deltaTime * (1f / flickerSpeed));
            }
        }
    }
}
