using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SetupUIEditor
{
    [MenuItem("Tools/Setup Combat System")]
    public static void SetupCombatSystem()
    {
        // ------------------ 1. THIẾT LẬP PLAYER COMPONENTS ------------------
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'Player' nào trong Scene để cấu hình!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // Đảm bảo có PlayerStats
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = player.AddComponent<PlayerStats>();
            Undo.RegisterCreatedObjectUndo(stats, "Thêm PlayerStats");
            Debug.Log("[Setup] Đã tự động thêm Component PlayerStats vào Player.");
        }

        // Đảm bảo có PlayerController
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = player.AddComponent<PlayerController>();
            Undo.RegisterCreatedObjectUndo(controller, "Thêm PlayerController");
            Debug.Log("[Setup] Đã tự động thêm Component PlayerController vào Player.");
        }

        // ------------------ 2. THIẾT LẬP CANVAS & UI ------------------
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas nào trong Scene!", "OK");
            return;
        }

        // Cấu hình CanvasScaler cho chuẩn 1920x1080
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Cấu hình CanvasScaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Debug.Log("[Setup] Đã cấu hình CanvasScaler sang chế độ ScaleWithScreenSize (1920x1080).");
        }

        Transform canvasTransform = canvas.transform;

        // Tìm HealthBar gốc
        Transform healthBarTransform = canvasTransform.Find("HealthBar");
        if (healthBarTransform == null)
        {
            foreach (Transform child in canvasTransform)
            {
                if (child.name.ToLower().Contains("health"))
                {
                    healthBarTransform = child;
                    break;
                }
            }
        }

        if (healthBarTransform == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy thanh HealthBar nào dưới Canvas để làm mẫu!", "OK");
            return;
        }

        // Cấu hình HealthBar thành chuẩn Cao Cấp
        ConfigureBarUI(healthBarTransform.gameObject, true);

        // Tạo/Cấu hình ManaBar
        Transform existingManaBar = canvasTransform.Find("ManaBar");
        if (existingManaBar != null)
        {
            Undo.DestroyObjectImmediate(existingManaBar.gameObject);
        }

        // Nhân bản HealthBar thành ManaBar
        GameObject manaBarObj = Object.Instantiate(healthBarTransform.gameObject, canvasTransform);
        manaBarObj.name = "ManaBar";
        Undo.RegisterCreatedObjectUndo(manaBarObj, "Tạo ManaBar");

        // Đặt vị trí ngay dưới HealthBar trong Hierarchy
        int healthIndex = healthBarTransform.GetSiblingIndex();
        manaBarObj.transform.SetSiblingIndex(healthIndex + 1);

        // Cấu hình ManaBar thành chuẩn Cao Cấp
        ConfigureBarUI(manaBarObj, false);

        // Đánh dấu Scene thay đổi để lưu
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Thành công", 
            "Đã đồng bộ hóa thành công hệ thống chiến đấu:\n\n" +
            "1. Thêm thành phần PlayerStats & PlayerController vào Player.\n" +
            "2. Loại bỏ hoàn toàn các nút tròn màu trắng thô cứng (tránh lỗi Knob.psd).\n" +
            "3. Thiết lập kích thước thanh Máu & Mana to lớn và sắc nét chuẩn Full HD (1920x1080)!\n\n" +
            "Vui lòng nhấn Ctrl + S để lưu lại Scene.", "Tuyệt vời");
    }

    private static void ConfigureBarUI(GameObject barObj, bool isHealth)
    {
        // 1. Gỡ bỏ hoàn toàn nút trượt tròn trắng (Handle Slide Area) để tránh lỗi nạp Knob.psd
        Transform handleArea = barObj.transform.Find("Handle Slide Area");
        if (handleArea != null)
        {
            Undo.DestroyObjectImmediate(handleArea.gameObject);
        }

        Slider slider = barObj.GetComponent<Slider>();
        if (slider != null)
        {
            slider.handleRect = null;
            slider.transition = Selectable.Transition.None; // Tắt hiệu ứng đổi màu handle
        }

        // Loại bỏ các sprite mặc định bị thiếu tài nguyên để tránh báo lỗi "Failed to find UI/Skin/Knob.psd" hoặc tương tự
        Image[] images = barObj.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.sprite != null && (img.sprite.name == "Background" || img.sprite.name == "UISprite" || img.sprite.name == "Knob" || img.sprite.name.Contains("Knob") || img.sprite.name.Contains("Sprite")))
            {
                img.sprite = null;
            }
        }

        // 2. Căn chỉnh kích thước & Vị trí chuẩn cao cấp cho 1920x1080 (Đẹp mắt ở góc trái)
        RectTransform rect = barObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            Undo.RecordObject(rect, "Cập nhật RectTransform");
            
            // Neo ở góc trên bên trái
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            if (isHealth)
            {
                rect.anchoredPosition = new Vector2(40f, -40f);
                rect.sizeDelta = new Vector2(420f, 36f); // Thanh máu lớn, nổi bật
            }
            else
            {
                rect.anchoredPosition = new Vector2(40f, -88f);
                rect.sizeDelta = new Vector2(360f, 28f); // Thanh mana nằm dưới, kích thước cân đối
            }
        }

        // 3. Đảm bảo đúng script UI được gắn
        if (isHealth)
        {
            ManaBarUI oldMana = barObj.GetComponent<ManaBarUI>();
            if (oldMana != null) Object.DestroyImmediate(oldMana);

            HealthBarUI hpUI = barObj.GetComponent<HealthBarUI>();
            if (hpUI == null)
            {
                hpUI = barObj.AddComponent<HealthBarUI>();
                Undo.RegisterCreatedObjectUndo(hpUI, "Gán HealthBarUI");
            }
            
            hpUI.healthSlider = slider;
            
            Transform fill = barObj.transform.Find("Fill Area/Fill");
            if (fill != null) hpUI.fillImage = fill.GetComponent<Image>();
        }
        else
        {
            HealthBarUI oldHealth = barObj.GetComponent<HealthBarUI>();
            if (oldHealth != null) Object.DestroyImmediate(oldHealth);

            ManaBarUI mpUI = barObj.GetComponent<ManaBarUI>();
            if (mpUI == null)
            {
                mpUI = barObj.AddComponent<ManaBarUI>();
                Undo.RegisterCreatedObjectUndo(mpUI, "Gán ManaBarUI");
            }

            mpUI.manaSlider = slider;

            Transform fill = barObj.transform.Find("Fill Area/Fill");
            if (fill != null) mpUI.fillImage = fill.GetComponent<Image>();
        }
    }
}
