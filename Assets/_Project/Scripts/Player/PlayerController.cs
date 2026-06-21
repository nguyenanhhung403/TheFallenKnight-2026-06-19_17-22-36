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

    [Header("--- Chưởng Hỏa Cầu (Fireball) ---")]
    public KeyCode fireballKey = KeyCode.F;       // Phím bấm xuất chiêu
    public float fireballManaCost = 20f;          // Tiêu tốn Mana
    public float fireballCooldown = 0.5f;         // Thời gian hồi chiêu
    public GameObject fireballPrefab;             // Prefab của quả cầu lửa
    public Transform firePoint;                   // Vị trí xuất phát của đạn (tùy chọn)

    // Components
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    // Trạng thái hoạt động
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isAttacking;
    private bool isDefending;
    private bool isRunningAttack; // Đang thực hiện Run Attack
    private bool isDead; // Trạng thái đã chết
    private bool isHurt; // Trạng thái đang bị thương (đơ người)
    private float lastHurtTime; // Thời điểm bị thương gần nhất (dùng làm bộ đếm an toàn)

    // Biến phụ trợ Combo & Hỏa cầu
    private float lastAttackTime;
    private float lastFireballTime;

    // Hiệu ứng nhấp nháy đỏ khi bị thương
    private Color originalColor;
    private Coroutine flashCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        // Tìm SpriteRenderer (hỗ trợ cả trường hợp nằm ở GameObject con)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        // 1. Bộ an toàn: Tự động reset trạng thái bị đơ (isHurt) sau 0.5 giây đề phòng kẹt do thiếu Animation Event
        if (isHurt && Time.time - lastHurtTime > 0.5f)
        {
            isHurt = false;
            Debug.Log("[Safety Failsafe] Tự động giải phóng trạng thái Đơ (isHurt = false)");
        }

        // 2. Xử lý các phím nóng debug/test (H: Sát thương, G: Hồi máu, B: Hồi mana)
        HandleDebugInputs();

        // Nếu đã chết thì ngắt toàn bộ di chuyển/chiến đấu
        if (isDead) return;

        // Nếu đang bị đơ do trúng đòn thì không nhận phím di chuyển/tấn công
        if (isHurt) return;

        // 3. Lấy Input di chuyển
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 4. Kiểm tra chạm đất (an toàn chống lỗi NullReferenceException nếu chưa gán groundCheck)
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;
        }

        // 5. Bộ an toàn: Tự động reset isAttacking nếu animation event bị bỏ lỡ
        if (isAttacking && Time.time - lastAttackTime > comboResetTime)
        {
            isAttacking = false;
            isRunningAttack = false;
        }

        // 6. Xử lý logic phím bấm
        HandleDefend();
        HandleFireball(); // Xuất chiêu Hỏa Cầu trước để tránh xung đột nút chém
        HandleAttack();
        HandleJump();

        // 7. Cập nhật Animator
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

        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = stats != null ? stats.TotalDamage : 15;

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
                StartCoroutine(DealMeleeDamageDelay(0.25f, Mathf.RoundToInt(damage * 1.1f), false, true));
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
            StartCoroutine(DealMeleeDamageDelay(0.2f, damage, false, false));
        }

        // 3. Combo 2: Chuột phải (Fire2) -> Kích hoạt Attack2
        if (Input.GetButtonDown("Fire2") && isGrounded)
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            anim.ResetTrigger("Attack2"); // Reset trước để tránh queue
            anim.SetTrigger("Attack2");
            StartCoroutine(DealMeleeDamageDelay(0.3f, Mathf.RoundToInt(damage * 1.25f), true, false));
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

    /// <summary>
    /// Xử lý xuất chiêu bắn cầu lửa.
    /// </summary>
    private void HandleFireball()
    {
        KeyCode key = (fireballKey == KeyCode.None) ? KeyCode.F : fireballKey;
        if (Input.GetKeyDown(key))
        {
            Debug.Log($"[Fireball Input] Phím {key} được bấm! isAttacking={isAttacking}, isDefending={isDefending}, isGrounded={isGrounded}, cooldown={(Time.time - lastFireballTime >= fireballCooldown)}");
            
            if (isDefending)
            {
                Debug.LogWarning("[Fireball Input] Bị chặn: Nhân vật đang phòng thủ!");
                return;
            }

            if (Time.time - lastFireballTime < fireballCooldown)
            {
                Debug.LogWarning("[Fireball Input] Bị chặn: Kỹ năng đang hồi chiêu!");
                return;
            }

            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats == null)
            {
                Debug.LogError("[Fireball Input] Bị chặn: Không tìm thấy Component PlayerStats trên Player!");
                return;
            }

            if (stats.currentMP < fireballManaCost)
            {
                Debug.LogWarning($"[Fireball Input] Bị chặn: Không đủ Mana! Cần {fireballManaCost}, hiện có {stats.currentMP}");
                return;
            }

            if (stats.ConsumeMana(fireballManaCost))
            {
                lastFireballTime = Time.time;
                lastAttackTime = Time.time;
                isAttacking = true;

                // Kích hoạt animation xuất chiêu (Sử dụng trigger Attack2 để vung kiếm chém lửa ra)
                anim.ResetTrigger("Attack2");
                anim.SetTrigger("Attack2");

                // Tính toán điểm xuất phát của cầu lửa
                Vector3 spawnOffset = new Vector3(isFacingRight ? 0.8f : -0.8f, 0.2f, 0f);
                Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)(transform.position + spawnOffset);

                GameObject fireballObj = null;
                if (fireballPrefab != null)
                {
                    fireballObj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    // Tạo cầu lửa bằng code nếu chưa gán Prefab để người chơi test được ngay
                    fireballObj = CreateProceduralFireball(spawnPos);
                }

                if (fireballObj != null)
                {
                    Fireball fireball = fireballObj.GetComponent<Fireball>();
                    if (fireball == null)
                    {
                        fireball = fireballObj.AddComponent<Fireball>();
                    }
                    Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
                    fireball.Setup(dir);
                }
                Debug.Log("[Fireball Input] Đã xuất chiêu Hỏa Cầu thành công!");
            }
        }
    }

    /// <summary>
    /// Tạo quả cầu lửa giả lập bằng Code (với TrailRenderer & màu sắc rực rỡ) để phục vụ test ngay
    /// </summary>
    private GameObject CreateProceduralFireball(Vector2 spawnPos)
    {
        GameObject obj = new GameObject("ProceduralFireball");
        obj.transform.position = spawnPos;

        // Thêm Collider2D làm trigger
        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;
        col.isTrigger = true;

        // Thêm SpriteRenderer (sử dụng sprite hình tròn tạo tự động)
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(32, Color.white);
        sr.color = new Color(1f, 0.5f, 0f, 1f); // Màu cam lửa sáng
        sr.sortingOrder = 5;

        // Thêm TrailRenderer tạo vệt lửa bay sau đuôi
        TrailRenderer trail = obj.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0.02f;

        // Thiết lập Gradient cho vệt đuôi: Vàng cam nhạt -> Đỏ đậm -> Trong suốt
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0f), 0.0f),  // Vàng
                new GradientColorKey(new Color(1f, 0.2f, 0f), 0.5f),  // Cam đỏ
                new GradientColorKey(new Color(0.5f, 0f, 0f), 1.0f)   // Đỏ tối
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;

        // Gán material mặc định cho Trail để tránh lỗi hiển thị màu hồng (missing shader)
        Shader lineShader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (lineShader != null)
        {
            trail.material = new Material(lineShader);
        }

        // Thêm component Fireball
        obj.AddComponent<Fireball>();

        return obj;
    }

    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        float radius = size / 2f;
        float cx = radius;
        float cy = radius;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                Color c = Color.clear;
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    c = new Color(color.r, color.g, color.b, color.a * alpha);
                }
                pixels[y * size + x] = c;
            }
        }
        
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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

        // Đóng băng Animator ở khung hình cuối sau 0.8 giây để tránh lặp anim chết (nếu clip bị bật Loop Time)
        StartCoroutine(FreezeAnimatorAfterDelay(0.8f));

        // Khởi chạy Coroutine làm nhân vật bay màu/tan biến dần vào hư vô
        StartCoroutine(FadeOutToNothingness());

        // Hiển thị màn hình Game Over sau 1.5 giây
        StartCoroutine(TriggerGameOverDelay(1.5f));
    }

    private System.Collections.IEnumerator TriggerGameOverDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (GameOverController.Instance != null)
        {
            GameOverController.Instance.TriggerGameOver();
        }
    }

    private System.Collections.IEnumerator FreezeAnimatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (anim != null && isDead)
        {
            anim.speed = 0f; // Dừng animation tại khung hình cuối
        }
    }

    // Hàm gọi khi nhân vật nhận sát thương (bị thương)
    public void TakeDamage()
    {
        if (isDead || isHurt) return;
        
        isHurt = true;
        lastHurtTime = Time.time;
        isAttacking = false; // Hủy trạng thái tấn công nếu đang đánh dở
        isRunningAttack = false;
        isDefending = false; // Hủy thế thủ
        
        anim.SetTrigger("Hurt"); // Kích hoạt animation bị thương

        // Nhấp nháy màu đỏ báo hiệu bị thương
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashColorEffect(Color.red, 0.15f));

        // Tạo lực đẩy lùi (Knockback) nhẹ dựa theo hướng đang đứng
        float knockbackDirection = isFacingRight ? -1.8f : 1.8f;
        rb.linearVelocity = new Vector2(knockbackDirection * moveSpeed * 0.6f, rb.linearVelocity.y);
     }

    // Coroutine nháy màu đặc biệt khi nhận sát thương, hồi máu hoặc hồi mana
    private System.Collections.IEnumerator FlashColorEffect(Color flashColor, float duration)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(duration);
            spriteRenderer.color = originalColor;
        }
    }

    // Coroutine làm nhân vật tan biến dần vào hư vô khi chết
    private System.Collections.IEnumerator FadeOutToNothingness()
    {
        // Chờ 1.2 giây để chạy xong phần lớn hiệu ứng/animation gục xuống gốc
        yield return new WaitForSeconds(1.2f);

        float fadeDuration = 2.0f; // Thời gian tan biến dần
        float elapsedTime = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        // Vô hiệu hóa sprite và collider hoàn toàn
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        rb.bodyType = RigidbodyType2D.Static; // Đóng băng vật lý
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

    private System.Collections.IEnumerator DealMeleeDamageDelay(float delay, int damage, bool isCombo2, bool runAttack)
    {
        yield return new WaitForSeconds(delay);

        if (isDead) yield break;

        // Điểm phát lực chém ở trước mặt người chơi
        Vector2 attackPos = (Vector2)transform.position + (isFacingRight ? Vector2.right : Vector2.left) * 0.8f;
        
        // Sinh hiệu ứng slash visual chém kiếm để người chơi dễ căn phạm vi
        SpawnSlashEffect(attackPos, isCombo2, runAttack);

        // Vẽ tia / quét hình tròn kiểm tra va chạm
        Collider2D[] targets = Physics2D.OverlapCircleAll(attackPos, 0.9f);
        foreach (var target in targets)
        {
            if (target.gameObject != gameObject)
            {
                // Nếu trúng đối thủ (quái vật hoặc vật phẩm phá hủy được), gửi thông điệp TakeDamage
                target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void Revive()
    {
        if (!isDead) return;
        isDead = false;
        isHurt = false;
        isAttacking = false;
        isRunningAttack = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        if (anim != null)
        {
            anim.speed = 1f; // Khôi phục tốc độ chạy animation
            anim.Rebind();
            anim.Update(0f);
        }

        Debug.Log("[Revive] Player đã được hồi sinh thành công!");
    }

    private void HandleDebugInputs()
    {
        // 1. Phím H: Gây 20 sát thương
        if (Input.GetKeyDown(KeyCode.H))
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(20);
                Debug.Log("[Debug Input] Bấm H -> Nhận 20 sát thương!");
            }
        }

        // 2. Phím G: Hồi 20 HP + Hiệu ứng hào quang xanh lá
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.Heal(20);
                
                // Nháy sprite màu xanh lá
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashColorEffect(new Color(0.2f, 1f, 0.2f, 1f), 0.25f));
                
                // Hiệu ứng vòng xoáy xanh lá
                SpawnAuraEffect(new Color(0.2f, 1f, 0.2f, 0.8f), 30);
                Debug.Log("[Debug Input] Bấm G -> Hồi 20 HP!");
            }
        }

        // 3. Phím B: Hồi 20 MP + Hiệu ứng hào quang xanh dương/cyan
        if (Input.GetKeyDown(KeyCode.B))
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.RestoreMana(20);
                
                // Nháy sprite màu xanh dương/cyan
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashColorEffect(new Color(0f, 0.7f, 1f, 1f), 0.25f));
                
                // Hiệu ứng vòng xoáy xanh dương/cyan
                SpawnAuraEffect(new Color(0f, 0.8f, 1f, 0.8f), 30);
                Debug.Log("[Debug Input] Bấm B -> Hồi 20 Mana!");
            }
        }
    }

    public void SpawnAuraEffect(Color color, int count)
    {
        // 1. Sinh vòng sáng lan tỏa ở dưới chân
        for (int j = 0; j < 2; j++)
        {
            GameObject ringObj = new GameObject("AuraRing");
            ringObj.transform.position = transform.position + new Vector3(0f, -0.9f, 0f); // Dưới chân

            SpriteRenderer srRing = ringObj.AddComponent<SpriteRenderer>();
            srRing.sprite = CreateCircleSprite(64, Color.white); // Dùng độ phân giải cao cho mượt
            srRing.color = color;
            srRing.sortingOrder = 5;

            AuraRingEffect ringEffect = ringObj.AddComponent<AuraRingEffect>();
            ringEffect.Setup(color);
            if (j > 0)
            {
                ringObj.transform.localScale = new Vector3(0.2f, 0.05f, 1f);
            }
        }

        // 2. Sinh các hạt xoáy tròn xung quanh và đi theo người chơi
        float baseAngleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            GameObject particle = new GameObject("SwirlingAuraParticle");
            
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32, Color.white);
            sr.color = color;
            sr.sortingOrder = 6;

            SwirlingAuraParticle swirl = particle.AddComponent<SwirlingAuraParticle>();
            // Phân bổ góc ban đầu đều để tạo hiệu ứng xoáy đều đẹp mắt
            float startAngle = i * baseAngleStep + Random.Range(-10f, 10f);
            float direction = (i % 2 == 0) ? 1f : -1f; // Một nửa xoáy xuôi, một nửa xoáy ngược chiều kim đồng hồ
            swirl.Setup(transform, color, startAngle, direction);
        }
    }

    public bool IsDefending()
    {
        return isDefending;
    }

    public void PlayBlockEffect()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashColorEffect(new Color(1f, 0.9f, 0.4f, 1f), 0.12f));
    }

    private void SpawnSlashEffect(Vector3 attackPos, bool isCombo2, bool runAttack)
    {
        GameObject slash = new GameObject("SlashEffect");
        slash.transform.position = attackPos;
        
        Vector3 scale = new Vector3(isFacingRight ? 1f : -1f, 1f, 1f);
        float baseScaleX = 1.2f;
        float baseScaleY = 0.8f;
        
        if (isCombo2)
        {
            baseScaleX = 1.5f; 
            baseScaleY = 1.1f;
        }
        else if (runAttack)
        {
            baseScaleX = 1.4f;
            baseScaleY = 0.9f;
        }
        
        slash.transform.localScale = new Vector3(scale.x * baseScaleX, baseScaleY, 1f);

        SpriteRenderer sr = slash.AddComponent<SpriteRenderer>();
        
        // Sao chép material để hỗ trợ ánh sáng URP 2D (không bị mất màu/tối/magenta)
        if (spriteRenderer != null)
        {
            sr.material = spriteRenderer.material;
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingLayerName = spriteRenderer.sortingLayerName;
            sr.sortingOrder = spriteRenderer.sortingOrder + 2; 
        }
        else
        {
            sr.sortingOrder = 7;
        }

        Color slashColor = new Color(0.9f, 0.95f, 1f, 0.95f);
        sr.sprite = CreateSlashSprite(64, 48, slashColor);

        SlashMovement effectScript = slash.AddComponent<SlashMovement>();
        effectScript.Setup(isFacingRight);
    }

    private Sprite CreateSlashSprite(int width, int height, Color color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[width * height];
        
        float cx = width * 0.1f; 
        float cy = height * 0.5f;
        float rOuter = width * 0.85f;
        float rInner = width * 0.65f;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy * 1.5f); 
                
                Color c = Color.clear;
                if (dist >= rInner && dist <= rOuter)
                {
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle >= -60f && angle <= 60f)
                    {
                        float alpha = Mathf.Sin((angle + 60f) / 120f * Mathf.PI); 
                        float edgeFade = Mathf.Sin((dist - rInner) / (rOuter - rInner) * Mathf.PI);
                        c = new Color(color.r, color.g, color.b, color.a * alpha * edgeFade);
                    }
                }
                pixels[y * width + x] = c;
            }
        }
        
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.1f, 0.5f), 16f); // Đặt PPU = 16f
    }
}

/// <summary>
/// Component quản lý vòng sáng lan tỏa dưới chân
/// </summary>
public class AuraRingEffect : MonoBehaviour
{
    private Color startColor;
    private Color endColor;
    private float duration = 0.55f;
    private float elapsed = 0f;
    private float maxScale = 3.5f;

    public void Setup(Color baseColor)
    {
        startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);
        endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        transform.localScale = new Vector3(0.5f, 0.1f, 1f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;

        // Phóng to theo hình elip dẹt ở chân 2D
        float currentScaleX = Mathf.Lerp(0.5f, maxScale, progress);
        float currentScaleY = Mathf.Lerp(0.1f, maxScale * 0.25f, progress);
        transform.localScale = new Vector3(currentScaleX, currentScaleY, 1f);

        // Mờ dần
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.Lerp(startColor, endColor, progress);
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Component điều khiển hạt hào quang xoáy tròn bám theo người chơi
/// </summary>
public class SwirlingAuraParticle : MonoBehaviour
{
    private Transform player;
    private Color startColor;
    private Color endColor;
    private float lifetime;
    private float elapsed;
    
    private float startAngle;
    private float rotationSpeed;
    private float maxRadius;
    private float verticalSpeed;
    private float startHeight;

    public void Setup(Transform playerTransform, Color baseColor, float angleOffset, float direction)
    {
        player = playerTransform;
        startColor = baseColor;
        endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        
        startAngle = angleOffset;
        // Tốc độ xoáy: 360 - 540 độ mỗi giây
        rotationSpeed = direction * Random.Range(360f, 540f);
        // Bán kính xoáy lan tỏa dần ra
        maxRadius = Random.Range(0.8f, 1.4f);
        // Tốc độ bay lên trên
        verticalSpeed = Random.Range(1.2f, 2.2f);
        // Cao độ ban đầu (từ chân nhân vật)
        startHeight = Random.Range(-0.8f, -0.4f);

        lifetime = Random.Range(0.8f, 1.2f);
        elapsed = 0f;

        // Kích thước hạt ngẫu nhiên to rõ nét
        float scale = Random.Range(1.5f, 3.0f);
        transform.localScale = new Vector3(scale, scale, 1f);
        
        // Đặt vị trí ban đầu
        UpdatePosition();
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float progress = elapsed / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();

        // Thu nhỏ dần khi sắp biến mất
        float currentScale = Mathf.Lerp(transform.localScale.x, 0.1f, progress);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        // Mờ dần theo thời gian
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.Lerp(startColor, endColor, progress);
        }
    }

    private void UpdatePosition()
    {
        if (player == null) return;

        float progress = elapsed / lifetime;
        // Tính toán góc xoay hiện tại
        float currentAngle = (startAngle + rotationSpeed * elapsed) * Mathf.Deg2Rad;
        // Bán kính mở rộng từ nhỏ ra to dần
        float currentRadius = Mathf.Lerp(0.1f, maxRadius, progress);
        // Độ cao bay dần lên đầu người chơi
        float currentHeight = startHeight + (verticalSpeed * elapsed);

        Vector3 offset = new Vector3(
            Mathf.Cos(currentAngle) * currentRadius,
            currentHeight,
            0f
        );

        transform.position = player.position + offset;
    }
}

public class SlashMovement : MonoBehaviour
{
    private float lifetime = 0.16f;
    private float elapsed = 0f;
    private float rotationSpeed = 320f;
    private bool faceRight;

    public void Setup(bool facingRight)
    {
        faceRight = facingRight;
        transform.rotation = Quaternion.Euler(0f, 0f, facingRight ? 40f : -40f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float rotDirection = faceRight ? -1f : 1f;
        transform.Rotate(0f, 0f, rotDirection * rotationSpeed * Time.deltaTime);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0.95f, 0f, progress);
            sr.color = c;
        }
    }
}
