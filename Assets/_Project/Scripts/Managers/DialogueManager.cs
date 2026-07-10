using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct DialogueLine
{
    public string speakerName;
    public Transform speakerTransform; // Vị trí nhân vật nói để hiện bong bóng chữ
    public string text;
}

/// <summary>
/// Quản lý hội thoại cắt cảnh điện ảnh (typewriter effect, click chuột tua nhanh, dải đen cinema).
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("--- Font Chữ ---")]
    public Font dialogueFont;

    // Trạng thái đối thoại
    private bool isDialogueActive = false;
    private List<DialogueLine> dialogueLines;
    private int currentLineIndex = 0;
    private System.Action onDialogueComplete;

    // UI điện ảnh
    private GameObject cinemaCanvasObj;
    private Image topCinemaBar;
    private Image bottomCinemaBar;

    // Bong bóng hội thoại World Space
    private GameObject bubbleCanvasObj;
    private Image bubbleBG;
    private Text bubbleText;
    private Text speakerNameText;
    
    // Typewriter
    private Coroutine typingCoroutine;
    private bool isLineFullyDisplayed = false;
    private string targetFullText = "";

    // Camera
    private Transform originalCamTarget;
    private float originalCamSize = 5f;
    private GameObject cameraMidpointTarget;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Nạp font tiếng Việt mặc định của dự án
#if UNITY_EDITOR
        dialogueFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/PressStart2P-Regular.ttf");
#endif
        if (dialogueFont == null)
        {
            dialogueFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // Khởi tạo ngay lập tức trong Awake đề phòng gọi StartDialogue trong cùng frame khi AddComponent
        InitializeCinemaUI();
        InitializeBubbleUI();
    }

    private void InitializeCinemaUI()
    {
        cinemaCanvasObj = new GameObject("CinemaCanvas");
        cinemaCanvasObj.transform.SetParent(transform);
        
        Canvas canvas = cinemaCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950; // Thấp hơn các bảng UI Menu chính nhưng đè lên gameplay

        cinemaCanvasObj.AddComponent<CanvasScaler>();

        // Tạo dải đen trên
        GameObject topBarObj = new GameObject("TopCinemaBar");
        topBarObj.transform.SetParent(cinemaCanvasObj.transform, false);
        topCinemaBar = topBarObj.AddComponent<Image>();
        topCinemaBar.color = new Color(0f, 0f, 0f, 0f); // Bắt đầu bằng trong suốt

        RectTransform rectTop = topCinemaBar.rectTransform;
        rectTop.anchorMin = new Vector2(0f, 1f);
        rectTop.anchorMax = new Vector2(1f, 1f);
        rectTop.pivot = new Vector2(0.5f, 1f);
        rectTop.anchoredPosition = new Vector2(0f, 150f); // Ẩn phía trên màn hình
        rectTop.sizeDelta = new Vector2(0f, 120f);

        // Tạo dải đen dưới
        GameObject bottomBarObj = new GameObject("BottomCinemaBar");
        bottomBarObj.transform.SetParent(cinemaCanvasObj.transform, false);
        bottomCinemaBar = bottomBarObj.AddComponent<Image>();
        bottomCinemaBar.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rectBottom = bottomCinemaBar.rectTransform;
        rectBottom.anchorMin = new Vector2(0f, 0f);
        rectBottom.anchorMax = new Vector2(1f, 0f);
        rectBottom.pivot = new Vector2(0.5f, 0f);
        rectBottom.anchoredPosition = new Vector2(0f, -150f); // Ẩn phía dưới màn hình
        rectBottom.sizeDelta = new Vector2(0f, 120f);

        cinemaCanvasObj.SetActive(false);
    }

    private void InitializeBubbleUI()
    {
        bubbleCanvasObj = new GameObject("DialogueBubbleCanvas");
        bubbleCanvasObj.transform.SetParent(transform);

        Canvas canvas = bubbleCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 25; // Render phía trên Sprite nhân vật

        // Đặt kích thước và tỉ lệ của Canvas thế giới
        RectTransform canvasRect = bubbleCanvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300f, 115f); // Kích thước của bong bóng hội thoại
        canvasRect.localScale = new Vector3(0.015f, 0.015f, 1f); // Tỉ lệ quy đổi ra World Space

        CanvasScaler scaler = bubbleCanvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20;

        // Panel nền bong bóng hội thoại (Stretches to fill canvas)
        GameObject bgObj = new GameObject("BubbleBG");
        bgObj.transform.SetParent(bubbleCanvasObj.transform, false);
        bubbleBG = bgObj.AddComponent<Image>();
        bubbleBG.color = new Color(0.05f, 0.05f, 0.05f, 0.85f); // Đen mờ tinh tế
        bubbleBG.sprite = CreateRoundedRectSprite();
        bubbleBG.type = Image.Type.Sliced; // Sử dụng chế độ Sliced để giữ nguyên bo tròn 4 góc không bị kéo giãn méo mó

        RectTransform rectBG = bubbleBG.rectTransform;
        rectBG.anchorMin = Vector2.zero;
        rectBG.anchorMax = Vector2.one;
        rectBG.pivot = new Vector2(0.5f, 0.5f);
        rectBG.offsetMin = Vector2.zero;
        rectBG.offsetMax = Vector2.zero;
        rectBG.localScale = Vector3.one;

        // Tên người nói
        GameObject nameObj = new GameObject("SpeakerNameText");
        nameObj.transform.SetParent(bgObj.transform, false);
        speakerNameText = nameObj.AddComponent<Text>();
        speakerNameText.font = dialogueFont;
        speakerNameText.fontSize = 20;
        speakerNameText.alignment = TextAnchor.UpperLeft;
        speakerNameText.color = new Color(1f, 0.84f, 0f, 1f); // Màu vàng hoàng gia
        speakerNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        speakerNameText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectName = speakerNameText.rectTransform;
        rectName.anchorMin = Vector2.zero;
        rectName.anchorMax = Vector2.one;
        rectName.pivot = new Vector2(0.5f, 0.5f);
        rectName.offsetMin = new Vector2(15f, 82f);
        rectName.offsetMax = new Vector2(-15f, -8f);
        rectName.localScale = Vector3.one;

        // Nội dung thoại
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(bgObj.transform, false);
        bubbleText = textObj.AddComponent<Text>();
        bubbleText.font = dialogueFont;
        bubbleText.fontSize = 18;
        bubbleText.alignment = TextAnchor.UpperLeft;
        bubbleText.color = Color.white;
        bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bubbleText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectText = bubbleText.rectTransform;
        rectText.anchorMin = Vector2.zero;
        rectText.anchorMax = Vector2.one;
        rectText.pivot = new Vector2(0.5f, 0.5f);
        rectText.offsetMin = new Vector2(15f, 10f);
        rectText.offsetMax = new Vector2(-15f, -38f);
        rectText.localScale = Vector3.one;

        bubbleCanvasObj.SetActive(false);
    }

    public void StartDialogue(List<DialogueLine> lines, System.Action onComplete)
    {
        if (isDialogueActive) return;

        // Đảm bảo UI đã được khởi tạo hoàn toàn
        if (cinemaCanvasObj == null) InitializeCinemaUI();
        if (bubbleCanvasObj == null) InitializeBubbleUI();

        isDialogueActive = true;
        dialogueLines = lines;
        currentLineIndex = 0;
        onDialogueComplete = onComplete;

        // 1. Khóa di chuyển người chơi
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.inputLocked = true;
        }

        // 2. Khóa AI các quái vật trong màn chơi gần đó
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.FreezeAI(true);
        }

        // 3. Setup Camera cắt cảnh
        SetupCinemaCamera(player);

        // 4. Kích hoạt hiệu ứng rạp phim UI
        cinemaCanvasObj.SetActive(true);
        StartCoroutine(AnimateCinemaUI(true));

        // 5. Chạy dòng thoại đầu tiên
        DisplayNextLine();
    }

    private void SetupCinemaCamera(PlayerController player)
    {
        if (CameraFollow.Instance != null)
        {
            originalCamTarget = CameraFollow.Instance.target;
            originalCamSize = Camera.main.orthographicSize;
            
            // Zoom cận cảnh điện ảnh
            StartCoroutine(AnimateCameraZoom(Camera.main, 3.6f, 0.8f));
        }
    }

    private void DisplayNextLine()
    {
        if (currentLineIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];
        speakerNameText.text = line.speakerName;
        targetFullText = line.text;

        // Trỏ camera vào người đang nói để camera tự động lia (pan) mượt mà đến đó
        if (CameraFollow.Instance != null && line.speakerTransform != null)
        {
            CameraFollow.Instance.target = line.speakerTransform;
        }

        // Hiện bong bóng hội thoại và đặt vị trí trên đầu nhân vật nói
        bubbleCanvasObj.SetActive(true);
        Vector3 headPos = line.speakerTransform.position + Vector3.up * 1.3f;
        bubbleCanvasObj.transform.position = headPos;

        // Reset typewriter
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterRoutine(line.text));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isLineFullyDisplayed = false;
        bubbleText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            bubbleText.text += letter;
            yield return new WaitForSecondsRealtime(0.04f); // Chữ chạy từ từ
        }

        isLineFullyDisplayed = true;
    }

    void Update()
    {
        if (!isDialogueActive) return;

        // Cập nhật vị trí bóng bóng liên tục theo nhân vật nói (đề phòng nhân vật lay động)
        if (currentLineIndex < dialogueLines.Count)
        {
            Transform speaker = dialogueLines[currentLineIndex].speakerTransform;
            if (speaker != null)
            {
                bubbleCanvasObj.transform.position = speaker.position + Vector3.up * 1.3f;
            }
        }

        // Bấm chuột trái (Fire1) hoặc Phím Space để tua nhanh hoặc chuyển câu
        if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
        {
            SkipOrNext();
        }
    }

    public void SkipOrNext()
    {
        if (!isDialogueActive) return;

        if (!isLineFullyDisplayed)
        {
            // Tua nhanh: hiện toàn bộ chữ ngay lập tức
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            bubbleText.text = targetFullText;
            isLineFullyDisplayed = true;
        }
        else
        {
            // Chuyển sang câu thoại tiếp theo
            currentLineIndex++;
            DisplayNextLine();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        bubbleCanvasObj.SetActive(false);

        // 1. Tắt dải đen rạp phim
        StartCoroutine(AnimateCinemaUI(false));

        // 2. Trả lại Camera về Player
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.target = originalCamTarget;
            StartCoroutine(AnimateCameraZoom(Camera.main, originalCamSize, 0.8f));
        }

        // Tự hủy object trung điểm camera
        if (cameraMidpointTarget != null)
        {
            Destroy(cameraMidpointTarget, 1f);
        }

        // 3. Mở khóa điều khiển người chơi
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.inputLocked = false;
        }

        // 4. Mở khóa AI các quái vật
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.FreezeAI(false);
        }

        // 5. Gọi sự kiện callback hoàn thành
        onDialogueComplete?.Invoke();
    }

    private IEnumerator AnimateCinemaUI(bool fadeIn)
    {
        float elapsed = 0f;
        float duration = 0.5f;

        Vector2 topStart = fadeIn ? new Vector2(0f, 150f) : new Vector2(0f, 0f);
        Vector2 topEnd = fadeIn ? new Vector2(0f, 0f) : new Vector2(0f, 150f);

        Vector2 bottomStart = fadeIn ? new Vector2(0f, -150f) : new Vector2(0f, 0f);
        Vector2 bottomEnd = fadeIn ? new Vector2(0f, 0f) : new Vector2(0f, -150f);

        Color colorStart = fadeIn ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.9f);
        Color colorEnd = fadeIn ? new Color(0f, 0f, 0f, 0.9f) : new Color(0f, 0f, 0f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            topCinemaBar.rectTransform.anchoredPosition = Vector2.Lerp(topStart, topEnd, t);
            bottomCinemaBar.rectTransform.anchoredPosition = Vector2.Lerp(bottomStart, bottomEnd, t);

            topCinemaBar.color = Color.Lerp(colorStart, colorEnd, t);
            bottomCinemaBar.color = Color.Lerp(colorStart, colorEnd, t);

            yield return null;
        }

        topCinemaBar.rectTransform.anchoredPosition = topEnd;
        bottomCinemaBar.rectTransform.anchoredPosition = bottomEnd;
        topCinemaBar.color = colorEnd;
        bottomCinemaBar.color = colorEnd;

        if (!fadeIn)
        {
            cinemaCanvasObj.SetActive(false);
        }
    }

    private IEnumerator AnimateCameraZoom(Camera c, float targetSize, float duration)
    {
        float elapsed = 0f;
        float startSize = c.orthographicSize;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            yield return null;
        }

        c.orthographicSize = targetSize;
    }

    private Sprite CreateRoundedRectSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Bo tròn 4 góc đơn giản bằng khoảng cách
                float dx = Mathf.Min(x, size - 1 - x);
                float dy = Mathf.Min(y, size - 1 - y);
                
                Color32 c = Color.white;
                // Nếu ở góc bo tròn ngoài
                if (dx < 3 && dy < 3)
                {
                    float dist = Mathf.Sqrt((3 - dx) * (3 - dx) + (3 - dy) * (3 - dy));
                    if (dist > 3f)
                    {
                        c = Color.clear;
                    }
                }
                pixels[y * size + x] = c;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        
        // Cấu hình border 4 pixel xung quanh để sử dụng chế độ Sliced trong UI (9-slicing)
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect, new Vector4(4f, 4f, 4f, 4f));
    }
}
