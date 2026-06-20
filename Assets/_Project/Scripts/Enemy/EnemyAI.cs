using UnityEngine;

/// <summary>
/// Quản lý trí tuệ nhân tạo (AI) của Quái vật: Tuần tra thông minh (tránh rơi vực, đâm tường), 
/// phát hiện người chơi, đuổi theo và tự động chọn 2 kiểu tấn công cận chiến khác nhau.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(EnemySpriteAnimator))]
public class EnemyAI : MonoBehaviour
{
    [Header("--- Cấu hình di chuyển & phát hiện ---")]
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float attackRange = 1.1f;
    public float patrolDistance = 4f;

    [Header("--- Cấu hình Tấn công ---")]
    public float attackCooldown = 1.8f;
    public int attackDamage = 12;

    [Header("--- Tấn công tầm xa (Archer) ---")]
    public bool isRanged = false;
    [Tooltip("Prefab của mũi tên. Nếu để trống, code sẽ tự vẽ mũi tên pixel-art.")]
    public GameObject projectilePrefab;

    private Transform playerTransform;
    private PlayerController playerController;
    private EnemySpriteAnimator animator;
    private Rigidbody2D rb;

    private Vector3 startPos;
    private float patrolLeft;
    private float patrolRight;
    private bool patrolDirectionRight = true;
    private float patrolWaitTimer = 0f;
    private float lastAttackTime = 0f;

    private bool isDead = false;
    private bool isHurt = false;
    private float hurtTimer = 0f;

    void Start()
    {
        startPos = transform.position;
        patrolLeft = startPos.x - patrolDistance;
        patrolRight = startPos.x + patrolDistance;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<EnemySpriteAnimator>();

        // Đảm bảo Rigidbody2D khóa xoay trục Z
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tìm Player trong màn chơi
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerController = pc;
            playerTransform = pc.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        // Bị choáng khi nhận sát thương
        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f)
            {
                isHurt = false;
            }
            return;
        }

        // Kiểm tra xem Player còn sống không
        bool isPlayerAlive = playerController != null && !playerController.IsDead();

        if (isPlayerAlive && Vector3.Distance(transform.position, playerTransform.position) <= chaseRange)
        {
            // Trạng thái đuổi theo và tấn công Player
            ChaseAndAttackPlayer();
        }
        else
        {
            // Trạng thái tuần tra (Patrol)
            Patrol();
        }
    }

    private void ChaseAndAttackPlayer()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        float directionX = playerTransform.position.x - transform.position.x;

        // Quay mặt về hướng Player bằng cách lật Scale
        if (directionX > 0.1f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX < -0.1f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        if (distance <= attackRange)
        {
            // Dừng lại khi đã ở tầm chém
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            
            if (animator != null && animator.GetCurrentState() != EnemyState.AttackA && animator.GetCurrentState() != EnemyState.AttackB)
            {
                if (animator.GetCurrentState() != EnemyState.Hit)
                    animator.PlayAnimation(EnemyState.Idle);
            }

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(PerformAttack());
            }
        }
        else
        {
            // Đuổi theo Player
            float step = (directionX > 0 ? 1 : -1) * moveSpeed;
            rb.linearVelocity = new Vector2(step, rb.linearVelocity.y);

            // Kiểm tra va chạm vực sâu / tường trước khi đuổi theo để tránh kẹt
            CheckBoundaries(directionX > 0 ? 1f : -1f);

            if (animator != null && animator.GetCurrentState() != EnemyState.Hit && 
                animator.GetCurrentState() != EnemyState.AttackA && animator.GetCurrentState() != EnemyState.AttackB)
            {
                animator.PlayAnimation(EnemyState.Walk);
            }
        }
    }

    private System.Collections.IEnumerator PerformAttack()
    {
        if (animator == null) yield break;

        // Chọn ngẫu nhiên giữa 2 chiêu chém khác nhau (AttackA hoặc AttackB)
        EnemyState attackState = (Random.value < 0.5f) ? EnemyState.AttackA : EnemyState.AttackB;
        animator.PlayAnimation(attackState, true);

        // Chờ vung kiếm/bắn tên (0.45 giây) trước khi check va chạm/gây sát thương
        yield return new WaitForSeconds(0.45f);

        if (isDead || isHurt) yield break;

        if (isRanged)
        {
            // Tấn công tầm xa: Bắn tên hướng mặt quái vật đang quay sang
            Vector2 shootDir = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;
            Vector3 spawnPos = transform.position + new Vector3(shootDir.x * 0.7f, 0.2f, 0f);

            GameObject projObj = null;
            if (projectilePrefab != null)
            {
                projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                projObj = CreateProceduralArrow(spawnPos);
            }

            if (projObj != null)
            {
                EnemyProjectile projectile = projObj.GetComponent<EnemyProjectile>();
                if (projectile == null) projectile = projObj.AddComponent<EnemyProjectile>();
                projectile.Setup(shootDir, attackDamage);
            }
        }
        else
        {
            // Tấn công cận chiến (Melee)
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= attackRange + 0.25f)
            {
                PlayerStats pStats = playerTransform.GetComponent<PlayerStats>();
                if (pStats != null)
                {
                    pStats.TakeDamage(attackDamage);
                }
            }
        }
    }

    private void Patrol()
    {
        // Đang tạm dừng suy nghĩ
        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null && animator.GetCurrentState() != EnemyState.Hit)
            {
                animator.PlayAnimation(EnemyState.Idle);
            }
            return;
        }

        float directionX = patrolDirectionRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(directionX * (moveSpeed * 0.7f), rb.linearVelocity.y); // Tuần tra đi chậm hơn đuổi bắt
        
        if (animator != null && animator.GetCurrentState() != EnemyState.Hit)
        {
            animator.PlayAnimation(EnemyState.Walk);
        }

        // Lật mặt theo hướng tuần tra
        if (patrolDirectionRight)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            if (transform.position.x >= patrolRight)
            {
                TurnAround();
            }
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            if (transform.position.x <= patrolLeft)
            {
                TurnAround();
            }
        }

        // Tránh lao xuống vực hoặc đâm tường khi đang tuần tra
        CheckBoundaries(directionX);
    }

    private void CheckBoundaries(float directionX)
    {
        LayerMask groundMask = LayerMask.GetMask("Ground");
        if (playerController != null)
        {
            groundMask = playerController.groundLayer;
        }

        Collider2D col = GetComponent<Collider2D>();
        Vector2 feetPos = (col != null) ? new Vector2(col.bounds.center.x, col.bounds.min.y) : (Vector2)transform.position;

        // 1. Kiểm tra vực thẳm phía trước (Cliff Check)
        // Cast từ chân quái vật lấn ra trước một ít. Ta lấy thêm offset y là 0.1f để tránh tia bắt đầu dưới mặt đất
        Vector2 cliffCheckOrigin = feetPos + new Vector2(directionX * 0.4f, 0.1f);
        RaycastHit2D cliffHit = Physics2D.Raycast(cliffCheckOrigin, Vector2.down, 0.4f, groundMask);
        if (cliffHit.collider == null)
        {
            TurnAround();
            return;
        }

        // 2. Kiểm tra tường cản trước mặt (Wall Check)
        // Cast từ tâm của Collider ra phía trước một khoảng vừa bằng nửa bề rộng cộng thêm 0.2f
        Vector2 wallCheckOrigin = (col != null) ? (Vector2)col.bounds.center : (Vector2)transform.position;
        float checkDistance = (col != null) ? (col.bounds.extents.x + 0.2f) : 0.6f;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckOrigin, new Vector2(directionX, 0f), checkDistance, groundMask);
        if (wallHit.collider != null)
        {
            TurnAround();
        }
    }

    private void TurnAround()
    {
        patrolDirectionRight = !patrolDirectionRight;
        patrolWaitTimer = Random.Range(0.8f, 2.2f);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void GetHit()
    {
        isHurt = true;
        hurtTimer = 0.4f; // Khựng 0.4s
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void DisableAI()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
    }

    private GameObject CreateProceduralArrow(Vector3 spawnPos)
    {
        GameObject arrow = new GameObject("EnemyArrow");
        arrow.transform.position = spawnPos;

        SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
        SpriteRenderer enemySr = GetComponent<SpriteRenderer>();
        if (enemySr != null)
        {
            sr.material = enemySr.material;
            sr.sortingLayerID = enemySr.sortingLayerID;
            sr.sortingLayerName = enemySr.sortingLayerName;
            sr.sortingOrder = enemySr.sortingOrder + 1;
        }

        // Vẽ mũi tên pixel art 16x5
        int w = 16;
        int h = 5;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[w * h];

        Color bodyColor = new Color(0.6f, 0.4f, 0.2f, 1f);
        Color tipColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        Color fletchColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (y == 2 && x < 12)
                {
                    c = bodyColor;
                }
                else if (x == 0 && (y == 1 || y == 3))
                {
                    c = fletchColor;
                }
                else if (x >= 12 && Mathf.Abs(y - 2) <= (w - 1 - x))
                {
                    c = tipColor;
                }
                pixels[y * w + x] = c;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);

        BoxCollider2D box = arrow.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 0.2f);

        return arrow;
    }
}
