using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("--- Di chuyển ---")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("--- Kiểm tra chạm đất ---")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("--- Chiến đấu ---")]
    public float comboResetTime = 1f; // Thời gian tối đa để bấm chuỗi combo tiếp theo
    public KeyCode defendKey = KeyCode.LeftShift; // Đổi phím phòng thủ sang Left Shift

    // Components
    private Rigidbody2D rb;
    private Animator anim;

    // Trạng thái hoạt động
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isAttacking;
    private bool isDefending;
    private bool isRunningAttack; // Đang thực hiện Run Attack
    private bool isDead; // Trạng thái đã chết
    private bool isHurt; // Trạng thái đang bị thương (đơ người)

    // Biến phụ trợ Combo
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead || isHurt) return;

        // 1. Lấy Input di chuyển
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 2. Kiểm tra chạm đất
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 3. Bộ an toàn: Tự động reset isAttacking nếu animation event bị bỏ lỡ
        // (Xảy ra khi spam quá nhanh, animator chuyển state trước khi đến frame cuối)
        if (isAttacking && Time.time - lastAttackTime > comboResetTime)
        {
            isAttacking = false;
            isRunningAttack = false;
        }

        // 4. Xử lý logic phím bấm
        HandleDefend();
        HandleAttack();
        HandleJump();

        // 5. Cập nhật Animator
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDead || isHurt) return; // Nếu đã chết hoặc bị thương thì không di chuyển vật lý

        // Di chuyển bằng vật lý
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Khóa di chuyển nếu đang thủ hoặc đang chém thường trên mặt đất (KHÔNG khóa khi đang Run Attack)
        if (isDefending || (isAttacking && isGrounded && !isRunningAttack))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Nếu không bấm phím -> dừng ngay lập tức (không trượt băng)
        if (horizontalInput == 0)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Nếu đang Run Attack thì cho đi chậm lại một chút (80% tốc độ), bình thường thì chạy 100%
        float currentSpeed = isRunningAttack ? moveSpeed * 0.8f : moveSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);

        // Lật mặt nhân vật dựa theo hướng đi
        if (horizontalInput > 0 && !isFacingRight)
            Flip();
        else if (horizontalInput < 0 && isFacingRight)
            Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1; // Đổi dấu trục X
        transform.localScale = localScale;
    }

    private void HandleJump()
    {
        // Bấm Space để nhảy
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking && !isDefending)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("Jump"); // Kích hoạt animation Nhảy
        }
    }

    private void HandleAttack()
    {
        // Nếu đang phòng thủ hoặc đang trong animation tấn công, không cho tấn công mới
        if (isDefending || isAttacking) return;

        // 1. Kiểm tra Run Attack (Tấn công khi đang chạy trên mặt đất)
        if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Fire2"))
            {
                isAttacking = true;
                isRunningAttack = true;
                lastAttackTime = Time.time;
                anim.ResetTrigger("RunAttack"); // Reset trước để tránh queue
                anim.SetTrigger("RunAttack");
                return;
            }
        }

        // 2. Combo 1: Chuột trái (Fire1) -> Kích hoạt Attack1
        if (Input.GetButtonDown("Fire1") && isGrounded)
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            anim.ResetTrigger("Attack1"); // Reset trước để tránh queue
            anim.SetTrigger("Attack1");
        }

        // 3. Combo 2: Chuột phải (Fire2) -> Kích hoạt Attack2
        if (Input.GetButtonDown("Fire2") && isGrounded)
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            anim.ResetTrigger("Attack2"); // Reset trước để tránh queue
            anim.SetTrigger("Attack2");
        }
    }

    private void HandleDefend()
    {
        // Nhấn giữ nút phòng thủ (Left Shift)
        if (Input.GetKey(defendKey) && isGrounded && !isAttacking)
        {
            isDefending = true;
        }
        else
        {
            isDefending = false;
        }
    }

    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("IsDefending", isDefending);
    }

    // Hàm gọi khi nhân vật bị tiêu diệt
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        rb.linearVelocity = Vector2.zero; // Dừng mọi chuyển động
        anim.SetTrigger("Dead"); // Kích hoạt animation chết
    }

    // Hàm gọi khi nhân vật nhận sát thương (bị thương)
    public void TakeDamage()
    {
        if (isDead || isHurt) return;
        
        isHurt = true;
        isAttacking = false; // Hủy trạng thái tấn công nếu đang đánh dở
        isRunningAttack = false;
        isDefending = false; // Hủy thế thủ
        
        anim.SetTrigger("Hurt"); // Kích hoạt animation bị thương

        // Tạo lực đẩy lùi (Knockback) nhẹ dựa theo hướng đang đứng
        float knockbackDirection = isFacingRight ? -1.5f : 1.5f;
        rb.linearVelocity = new Vector2(knockbackDirection * moveSpeed * 0.5f, rb.linearVelocity.y);
    }

    // HÀM NÀY SẼ ĐƯỢC GỌI TỪ ANIMATION EVENT cuối clip bị thương (Hurt) để reset trạng thái đơ
    public void FinishHurt()
    {
        isHurt = false;
    }

    // HÀM NÀY SẼ ĐƯỢC GỌI TỪ ANIMATION EVENT cuối mỗi clip chém để reset trạng thái
    public void FinishAttack()
    {
        isAttacking = false;
        isRunningAttack = false; // Reset trạng thái Run Attack
    }
}
