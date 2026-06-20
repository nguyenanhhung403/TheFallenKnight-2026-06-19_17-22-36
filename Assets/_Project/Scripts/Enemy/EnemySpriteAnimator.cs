using UnityEngine;

/// <summary>
/// Trạng thái hoạt ảnh của Quái vật.
/// </summary>
public enum EnemyState
{
    Idle,
    Walk,
    AttackA,
    AttackB,
    Hit,
    Dead,
    Jump
}

/// <summary>
/// Bộ điều khiển hoạt ảnh dạng Sprite-swapping tự động cho quái vật pixel art.
/// Giải phóng việc phải tạo hàng tá Animator Controllers và Clips trong Unity Editor.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    [Header("--- Tập hợp Sprite của từng hoạt ảnh ---")]
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] attackASprites;
    public Sprite[] attackBSprites;
    public Sprite[] hitSprites;
    public Sprite[] deadSprites;
    public Sprite[] jumpSprites;

    [Header("--- Tốc độ khung hình (FPS) ---")]
    public float fps = 10f;

    private SpriteRenderer spriteRenderer;
    private EnemyState currentState = EnemyState.Idle;
    private Sprite[] currentSprites;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private bool isLooping = true;
    private System.Action onAnimationComplete;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        PlayAnimation(EnemyState.Idle);
    }

    void Update()
    {
        if (currentSprites == null || currentSprites.Length == 0) return;

        frameTimer += Time.deltaTime;
        float timePerFrame = 1f / fps;

        if (frameTimer >= timePerFrame)
        {
            frameTimer -= timePerFrame;
            currentFrameIndex++;

            if (currentFrameIndex >= currentSprites.Length)
            {
                if (isLooping)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = currentSprites.Length - 1; // Giữ nguyên khung hình cuối
                    onAnimationComplete?.Invoke();
                    return;
                }
            }

            spriteRenderer.sprite = currentSprites[currentFrameIndex];
        }
    }

    /// <summary>
    /// Phát hoạt ảnh tương ứng với trạng thái EnemyState.
    /// </summary>
    /// <param name="state">Trạng thái mới.</param>
    /// <param name="overrideCurrent">Có ghi đè hoạt ảnh đang phát cùng loại không.</param>
    public void PlayAnimation(EnemyState state, bool overrideCurrent = false)
    {
        if (currentState == state && !overrideCurrent) return;

        // Đã chết thì không cho phát hoạt ảnh nào khác đè lên nữa
        if (currentState == EnemyState.Dead) return;

        currentState = state;
        currentFrameIndex = 0;
        frameTimer = 0f;
        onAnimationComplete = null;

        switch (state)
        {
            case EnemyState.Idle:
                currentSprites = idleSprites;
                isLooping = true;
                break;
            case EnemyState.Walk:
                currentSprites = walkSprites;
                isLooping = true;
                break;
            case EnemyState.AttackA:
                currentSprites = attackASprites;
                isLooping = false;
                onAnimationComplete = () => PlayAnimation(EnemyState.Idle);
                break;
            case EnemyState.AttackB:
                currentSprites = attackBSprites;
                isLooping = false;
                onAnimationComplete = () => PlayAnimation(EnemyState.Idle);
                break;
            case EnemyState.Hit:
                currentSprites = hitSprites;
                isLooping = false;
                // Nhận sát thương xong quay lại đứng thở Idle
                onAnimationComplete = () => PlayAnimation(EnemyState.Idle);
                break;
            case EnemyState.Dead:
                currentSprites = deadSprites;
                isLooping = false;
                break;
            case EnemyState.Jump:
                currentSprites = jumpSprites;
                isLooping = true;
                break;
        }

        if (currentSprites != null && currentSprites.Length > 0)
        {
            spriteRenderer.sprite = currentSprites[0];
        }
    }

    public EnemyState GetCurrentState()
    {
        return currentState;
    }
}
