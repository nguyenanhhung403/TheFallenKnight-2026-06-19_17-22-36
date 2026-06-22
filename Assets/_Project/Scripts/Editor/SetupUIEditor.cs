using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

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

        // Đảm bảo có PotionSystem và gán Sprites bình thuốc từ 2D Pixel Item Pack
        PotionSystem potionSys = player.GetComponent<PotionSystem>();
        if (potionSys == null)
        {
            potionSys = player.AddComponent<PotionSystem>();
            Undo.RegisterCreatedObjectUndo(potionSys, "Thêm PotionSystem");
            Debug.Log("[Setup] Đã tự động thêm Component PotionSystem vào Player.");
        }

        Undo.RecordObject(potionSys, "Gán Sprites Potion");
        potionSys.healthPotionSprite = LoadSpriteSafe("Assets/_Project/Sprites/VietNam/banh_mi.png");
        potionSys.manaPotionSprite = LoadSpriteSafe("Assets/_Project/Sprites/VietNam/tra_sua.png");
        potionSys.speedPotionSprite = LoadSpriteSafe("Assets/_Project/Sprites/VietNam/ca_phe.png");

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

    [MenuItem("Tools/Spawn Test Collectibles")]
    public static void SpawnTestCollectibles()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Player trong Scene để sinh vật phẩm thử nghiệm!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Vector3 playerPos = player.transform.position;

        // Sinh 3 vật phẩm Việt Nam ở vị trí x = +3f, +5f, +7f so với player
        CreateCollectible(playerPos + Vector3.right * 3f + Vector3.up * 0.5f, CollectibleType.HealthPotion, 
            "Assets/_Project/Sprites/VietNam/banh_mi.png", "Collectible_BanhMi");

        CreateCollectible(playerPos + Vector3.right * 5f + Vector3.up * 0.5f, CollectibleType.ManaPotion, 
            "Assets/_Project/Sprites/VietNam/tra_sua.png", "Collectible_TraSua");

        CreateCollectible(playerPos + Vector3.right * 7f + Vector3.up * 0.5f, CollectibleType.SpeedPotion, 
            "Assets/_Project/Sprites/VietNam/ca_phe.png", "Collectible_CaPhe");

        // Đánh dấu Scene thay đổi để lưu
        if (player.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Thành công", 
            "Đã tạo thành công 3 vật phẩm thử nghiệm Việt Nam trên mặt đất (phía bên phải nhân vật):\n\n" +
            "1. Bánh Mì kẹp thịt (Hồi Máu)\n" +
            "2. Trà Sữa trân châu (Hồi Năng Lượng)\n" +
            "3. Cà Phê Sữa Đá Phin (Tăng Tốc Độ)\n\n" +
            "Hãy nhấn Play và điều khiển nhân vật chạy qua để thưởng thức!", "Tuyệt vời");
    }

    private static void CreateCollectible(Vector3 position, CollectibleType type, string spritePath, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        // Thiết lập PPU của Sprite sang 16 để hiển thị to rõ nét trong màn chơi 2D Pixel Art
        if (sprite != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(sprite.texture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && (importer.spritePixelsPerUnit != 16f || importer.filterMode != FilterMode.Point))
            {
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        sr.sprite = sprite;
        sr.sortingOrder = 5; // Hiển thị trước map nền

        // Tạo Trigger Collider
        CircleCollider2D circleCol = obj.AddComponent<CircleCollider2D>();
        circleCol.isTrigger = true;
        circleCol.radius = 0.4f;

        // Gán script CollectibleItem
        CollectibleItem item = obj.AddComponent<CollectibleItem>();
        item.itemType = type;
        item.amount = 1;

        Undo.RegisterCreatedObjectUndo(obj, "Tạo " + name);
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

    [MenuItem("Tools/Setup Enemies and Potions")]
    public static void SetupEnemiesAndPotions()
    {
        // Tự động tạo Tag "Enemy" nếu chưa tồn tại
        CreateTag("Enemy");

        // 1. Tạo thư mục chứa Prefabs nếu chưa có
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "_Project/Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Potions"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Potions");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Enemies");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // 2. Tạo/Lấy Prefab các vật phẩm Việt Nam
        GameObject healthPotion = GetOrCreatePotionPrefab(CollectibleType.HealthPotion, 
            "Assets/_Project/Sprites/VietNam/banh_mi.png", "Collectible_BanhMi");
        GameObject manaPotion = GetOrCreatePotionPrefab(CollectibleType.ManaPotion, 
            "Assets/_Project/Sprites/VietNam/tra_sua.png", "Collectible_TraSua");
        GameObject speedPotion = GetOrCreatePotionPrefab(CollectibleType.SpeedPotion, 
            "Assets/_Project/Sprites/VietNam/ca_phe.png", "Collectible_CaPhe");

        if (healthPotion == null || manaPotion == null || speedPotion == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy hoặc không thể tạo các Prefab bình thuốc!", "OK");
            return;
        }

        // 3. Quét và tạo 5 loại quái Enemy1 -> Enemy5
        for (int i = 1; i <= 5; i++)
        {
            string folderPath = $"Assets/Hero and Opponents/Sprites/Enemy{i}";
            if (!System.IO.Directory.Exists(folderPath))
            {
                Debug.LogWarning($"[Setup] Thư mục quái {folderPath} không tồn tại. Bỏ qua.");
                continue;
            }

            // Quét tất cả file ảnh PNG trong thư mục quái
            string[] files = System.IO.Directory.GetFiles(folderPath, "*.png");
            if (files.Length == 0) continue;

            // Đảm bảo Sprite PPU = 100, Point filter
            foreach (string file in files)
            {
                TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer != null)
                {
                    bool needsReimport = false;
                    if (importer.spritePixelsPerUnit != 16f)
                    {
                        importer.spritePixelsPerUnit = 16f;
                        needsReimport = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        needsReimport = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        needsReimport = true;
                    }
                    if (needsReimport)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }

            // Phân loại Sprites vào các hoạt ảnh
            var idleList = new System.Collections.Generic.List<Sprite>();
            var walkList = new System.Collections.Generic.List<Sprite>();
            var attackAList = new System.Collections.Generic.List<Sprite>();
            var attackBList = new System.Collections.Generic.List<Sprite>();
            var hitList = new System.Collections.Generic.List<Sprite>();
            var deadList = new System.Collections.Generic.List<Sprite>();
            var jumpList = new System.Collections.Generic.List<Sprite>();

            foreach (string file in files)
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(file).ToLower();
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                if (s == null) continue;

                if (filename.StartsWith("idle"))
                    idleList.Add(s);
                else if (filename.StartsWith("walk"))
                    walkList.Add(s);
                else if (filename.StartsWith("attack-a") || filename.StartsWith("attack_a"))
                    attackAList.Add(s);
                else if (filename.StartsWith("attack-b") || filename.StartsWith("attack_b"))
                    attackBList.Add(s);
                else if (filename.StartsWith("hit"))
                    hitList.Add(s);
                else if (filename.StartsWith("dead"))
                    deadList.Add(s);
                else if (filename.StartsWith("jump"))
                    jumpList.Add(s);
            }

            // Tạo GameObject tạm để dựng quái
            string enemyName = $"Enemy_{i}";
            GameObject tempEnemy = new GameObject(enemyName);
            tempEnemy.tag = "Enemy";

            SpriteRenderer sr = tempEnemy.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            if (idleList.Count > 0) sr.sprite = idleList[0];

            Rigidbody2D rb = tempEnemy.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D boxCol = tempEnemy.AddComponent<BoxCollider2D>();
            // Tự động tính toán kích thước collider theo tỷ lệ thực tế của Sprite
            float spriteWidth = 1f;
            float spriteHeight = 1.8f;
            if (sr.sprite != null)
            {
                spriteWidth = (sr.sprite.rect.width / sr.sprite.pixelsPerUnit);
                spriteHeight = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit);
            }
            
            // Lấy 70% bề ngang và 90% chiều cao để tạo collider vừa vặn
            boxCol.size = new Vector2(spriteWidth * 0.7f, spriteHeight * 0.9f);
            boxCol.offset = new Vector2(0f, spriteHeight * 0.45f);

            // Thêm các Component mới tạo
            EnemySpriteAnimator animator = tempEnemy.AddComponent<EnemySpriteAnimator>();
            animator.idleSprites = idleList.ToArray();
            animator.walkSprites = walkList.ToArray();
            animator.attackASprites = attackAList.ToArray();
            animator.attackBSprites = attackBList.ToArray();
            animator.hitSprites = hitList.ToArray();
            animator.deadSprites = deadList.ToArray();
            animator.jumpSprites = jumpList.ToArray();
            animator.fps = 10f;

            EnemyStats stats = tempEnemy.AddComponent<EnemyStats>();
            stats.healthPotionPrefab = healthPotion;
            stats.manaPotionPrefab = manaPotion;
            stats.speedPotionPrefab = speedPotion;
            stats.dropProbability = 0.15f; // 15% rơi
            stats.maxHealth = 50 + (i * 10); // Enemy5 trâu hơn Enemy1
            stats.baseDamage = 10 + (i * 2);

            EnemyAI ai = tempEnemy.AddComponent<EnemyAI>();
            ai.moveSpeed = 1.8f + (i * 0.1f);
            ai.patrolDistance = spriteWidth * 2.5f; // Tầm tuần tra tỷ lệ thuận với kích thước quái
            ai.attackCooldown = 1.6f + (i * 0.1f);
            ai.attackDamage = stats.baseDamage;
            
            // Thiết lập tầm đánh và đuổi bắt thích ứng với chiều rộng/cao của quái vật lớn
            ai.attackRange = spriteWidth * 0.6f + 0.5f; 
            ai.chaseRange = spriteHeight * 3.5f;

            // Lưu thành Prefab
            string enemyPrefabPath = $"Assets/_Project/Prefabs/Enemies/{enemyName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempEnemy, enemyPrefabPath);
            Object.DestroyImmediate(tempEnemy);

            // Gán lại tag trên prefab đã lưu để chắc chắn 100%
            GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
            if (prefabObj != null)
            {
                prefabObj.tag = "Enemy";
                EditorUtility.SetDirty(prefabObj);
            }

            Debug.Log($"[Setup] Đã tạo thành công Prefab quái: {enemyPrefabPath}");
        }

        // 4. Tìm các EnemySpawnNode trong Scene và gán ngẫu nhiên
        EnemySpawnNode[] spawnNodes = Object.FindObjectsByType<EnemySpawnNode>(FindObjectsSortMode.None);
        int assignedCount = 0;
        if (spawnNodes.Length > 0)
        {
            for (int nodeIdx = 0; nodeIdx < spawnNodes.Length; nodeIdx++)
            {
                var node = spawnNodes[nodeIdx];
                string randomEnemyPath = $"Assets/_Project/Prefabs/Enemies/Enemy_{Random.Range(1, 6)}.prefab";
                GameObject enemyPref = AssetDatabase.LoadAssetAtPath<GameObject>(randomEnemyPath);
                if (enemyPref != null)
                {
                    Undo.RecordObject(node, "Gán Enemy Prefab ngẫu nhiên");
                    node.enemyPrefab = enemyPref;
                    assignedCount++;
                }
            }
            if (spawnNodes.Length > 0 && spawnNodes[0].gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawnNodes[0].gameObject.scene);
            }
        }

        // 5. Tìm tất cả các GameObject quái vật hiện có trong Scene và gán tag "Enemy"
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int taggedCount = 0;
        foreach (var go in allGo)
        {
            if (go.name.StartsWith("Enemy_") || go.GetComponent<EnemyStats>() != null || go.GetComponent<EnemyAI>() != null)
            {
                Undo.RecordObject(go, "Gán Tag Enemy cho quái vật trong Scene");
                go.tag = "Enemy";
                taggedCount++;
            }
        }
        if (taggedCount > 0)
        {
            Debug.Log($"[Setup] Đã gán tag 'Enemy' thành công cho {taggedCount} quái vật đang có trong Scene!");
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công", 
            $"Đã thiết lập hoàn chỉnh hệ thống Quái vật và Bình thuốc:\n\n" +
            $"1. Tạo 3 Prefabs bình thuốc với PPU=16, Point Filter.\n" +
            $"2. Tạo 5 Prefabs quái vật pixel với PPU=16 và hoạt ảnh Sprite-swapping tự động.\n" +
            $"3. Cấu hình tỉ lệ rớt 15% tổng thể (chia đều 33.3% cho 3 loại bình thuốc).\n" +
            $"4. Tự động gán Prefabs quái ngẫu nhiên cho {assignedCount} Spawn Nodes trong Scene!\n\n" +
            $"Hãy nhấn Ctrl + S để lưu lại Scene.", "OK");
    }

    private static GameObject GetOrCreatePotionPrefab(CollectibleType type, string spritePath, string prefabName)
    {
        string localPath = $"Assets/_Project/Prefabs/Potions/{prefabName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(localPath) != null)
        {
            AssetDatabase.DeleteAsset(localPath);
        }

        GameObject temp = new GameObject(prefabName);
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        
        ConfigurePixelSpriteImporter(spritePath, 512f);
        
        // Tìm Sprite từ file ảnh (hỗ trợ cả Single lẫn Multiple Slice của người dùng)
        Sprite s = null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                s = sprite;
                break;
            }
        }

        sr.sprite = s;
        sr.sortingOrder = 5;
        CircleCollider2D circleCol = temp.AddComponent<CircleCollider2D>();
        circleCol.isTrigger = true;
        circleCol.radius = 0.4f;
        CollectibleItem item = temp.AddComponent<CollectibleItem>();
        item.itemType = type;
        item.amount = 1;
        item.dropOnSpawn = true;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, localPath);
        Object.DestroyImmediate(temp);
        Debug.Log($"[Setup] Đã tạo Prefab Vật Phẩm Việt Nam: {localPath}");
        return saved;
    }

    private static void CreateTag(string tag)
    {
        try
        {
            string[] existingTags = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (string t in existingTags)
            {
                if (t == tag) return;
            }
            UnityEditorInternal.InternalEditorUtility.AddTag(tag);
            Debug.Log($"[Setup] Đã tự động tạo Tag: {tag}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Setup] Không thể tạo Tag '{tag}' tự động: {ex.Message}. Hãy tạo thủ công trong Project Settings nếu cần.");
        }
    }

    private static Font FindCustomFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:Font");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/"))
            {
                Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font != null) return font;
            }
        }
        return null;
    }

    [MenuItem("Tools/Setup Menus (Dark Fantasy)")]
    public static void SetupMenus()
    {
        // ------------------ 1. CONFIG SPRITES ------------------
        string menuBgPath = "Assets/_Project/Sprites/VietNam/vietnam_dark_menu_bg.png";
        string gameoverBgPath = "Assets/_Project/Sprites/VietNam/vietnam_dark_gameover_bg.png";
        string bossDefeatedBgPath = "Assets/_Project/Sprites/VietNam/vietnam_dark_choice_bg.png";

        ConfigureSpriteImporter(menuBgPath);
        ConfigureSpriteImporter(gameoverBgPath);
        ConfigureSpriteImporter(bossDefeatedBgPath);

        // Cấu hình các sprite bản đồ Việt Nam
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnam_flag.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnamese_house.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnamese_house_1.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnamese_house_2.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnamese_bamboo.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnamese_gate.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/vietnamese_banyan.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/banh_mi.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/tra_sua.png", 512f);
        ConfigurePixelSpriteImporter("Assets/_Project/Sprites/VietNam/ca_phe.png", 512f);

        Sprite menuBg = AssetDatabase.LoadAssetAtPath<Sprite>(menuBgPath);
        Sprite gameoverBg = AssetDatabase.LoadAssetAtPath<Sprite>(gameoverBgPath);
        Sprite bossDefeatedBg = AssetDatabase.LoadAssetAtPath<Sprite>(bossDefeatedBgPath);

        Font pixelFont = FindCustomFont();
        if (pixelFont == null) pixelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ------------------ 2. SETUP GAMEPLAY SCENE (Pause & Game Over) ------------------
        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        string gameplayScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        EditorSceneManager.OpenScene(gameplayScenePath, OpenSceneMode.Single);

        // Tạo Canvas trong Gameplay Scene nếu chưa có
        Canvas gameplayCanvas = Object.FindAnyObjectByType<Canvas>();
        if (gameplayCanvas == null)
        {
            GameObject canvasObj = new GameObject("GameplayCanvas");
            gameplayCanvas = canvasObj.AddComponent<Canvas>();
            gameplayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Gameplay Canvas");
        }

        // Tạo hoặc cấu hình PauseMenuPanel
        Transform existingPause = gameplayCanvas.transform.Find("PauseMenuPanel");
        if (existingPause != null)
        {
            Undo.DestroyObjectImmediate(existingPause.gameObject);
        }

        GameObject pausePanelObj = new GameObject("PauseMenuPanel");
        pausePanelObj.transform.SetParent(gameplayCanvas.transform, false);
        RectTransform pauseRect = pausePanelObj.AddComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.zero;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.sizeDelta = Vector2.zero;
        pauseRect.anchoredPosition = Vector2.zero;

        Image pauseBg = pausePanelObj.AddComponent<Image>();
        pauseBg.color = new Color(0f, 0f, 0f, 0.85f); // Đen mờ u ám

        Outline pauseOutline = pausePanelObj.AddComponent<Outline>();
        pauseOutline.effectColor = new Color(0.4f, 0.05f, 0.05f, 0.5f); // Viền đỏ máu nhạt
        pauseOutline.effectDistance = new Vector2(3, -3);

        // Nút bấm UI thiết kế Dark Gothic
        Color btnNormalColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        Color btnHoverColor = new Color(0.35f, 0.05f, 0.05f, 1f); // Đỏ thẫm khi hover

        // Pause Title
        GameObject pauseTitleObj = new GameObject("PauseTitle");
        pauseTitleObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform pauseTitleRect = pauseTitleObj.AddComponent<RectTransform>();
        pauseTitleRect.sizeDelta = new Vector2(600, 100);
        pauseTitleRect.anchoredPosition = new Vector2(0, 160);
        Text pauseTitleText = pauseTitleObj.AddComponent<Text>();
        pauseTitleText.text = "TẠM DỪNG";
        pauseTitleText.font = pixelFont;
        pauseTitleText.fontSize = 50;
        pauseTitleText.fontStyle = FontStyle.Bold;
        pauseTitleText.alignment = TextAnchor.MiddleCenter;
        pauseTitleText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        Shadow pauseTitleShadow = pauseTitleObj.AddComponent<Shadow>();
        pauseTitleShadow.effectColor = Color.black;
        pauseTitleShadow.effectDistance = new Vector2(3, -3);

        GameObject resumeBtnObj = CreateMenuButton(pausePanelObj.transform, "ResumeButton", "TIẾP TỤC", new Vector2(0, 40), new Vector2(320, 60), btnNormalColor, btnHoverColor, 20, pixelFont);
        GameObject mainMenuBtnObj = CreateMenuButton(pausePanelObj.transform, "MainMenuButton", "MENU CHÍNH", new Vector2(0, -40), new Vector2(320, 60), btnNormalColor, btnHoverColor, 20, pixelFont);
        GameObject quitBtnObj = CreateMenuButton(pausePanelObj.transform, "QuitButton", "THOÁT GAME", new Vector2(0, -120), new Vector2(320, 60), btnNormalColor, btnHoverColor, 20, pixelFont);

        // Gán PauseMenuController
        PauseMenuController pmCtrl = gameplayCanvas.gameObject.GetComponent<PauseMenuController>();
        if (pmCtrl == null) pmCtrl = gameplayCanvas.gameObject.AddComponent<PauseMenuController>();
        pmCtrl.pausePanel = pausePanelObj;
        
        UnityEventTools.AddVoidPersistentListener(resumeBtnObj.GetComponent<Button>().onClick, pmCtrl.ResumeGame);
        UnityEventTools.AddVoidPersistentListener(mainMenuBtnObj.GetComponent<Button>().onClick, pmCtrl.LoadMainMenu);
        UnityEventTools.AddVoidPersistentListener(quitBtnObj.GetComponent<Button>().onClick, pmCtrl.QuitGame);

        pausePanelObj.SetActive(false);

        // Tạo hoặc cấu hình GameOverPanel
        Transform existingGO = gameplayCanvas.transform.Find("GameOverPanel");
        if (existingGO != null)
        {
            Undo.DestroyObjectImmediate(existingGO.gameObject);
        }

        GameObject goPanelObj = new GameObject("GameOverPanel");
        goPanelObj.transform.SetParent(gameplayCanvas.transform, false);
        RectTransform goRect = goPanelObj.AddComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.sizeDelta = Vector2.zero;
        goRect.anchoredPosition = Vector2.zero;

        Image goBg = goPanelObj.AddComponent<Image>();
        if (gameoverBg != null)
        {
            goBg.sprite = gameoverBg;
            goBg.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Thẫm màu Gothic
        }
        else
        {
            goBg.color = new Color(0.15f, 0.02f, 0.02f, 0.95f);
        }

        // Game Over Title
        GameObject goTitleObj = new GameObject("GameOverTitle");
        goTitleObj.transform.SetParent(goPanelObj.transform, false);
        RectTransform goTitleRect = goTitleObj.AddComponent<RectTransform>();
        goTitleRect.sizeDelta = new Vector2(800, 120);
        goTitleRect.anchoredPosition = new Vector2(0, 150);
        Text goTitleText = goTitleObj.AddComponent<Text>();
        goTitleText.text = "BẠN ĐÃ GỤC NGÃ";
        goTitleText.font = pixelFont;
        goTitleText.fontSize = 80;
        goTitleText.fontStyle = FontStyle.Bold;
        goTitleText.alignment = TextAnchor.MiddleCenter;
        goTitleText.color = new Color(0.7f, 0.05f, 0.05f, 1f); // Màu đỏ máu đậm nghệ thuật
        Shadow goTitleShadow = goTitleObj.AddComponent<Shadow>();
        goTitleShadow.effectColor = Color.black;
        goTitleShadow.effectDistance = new Vector2(4, -4);

        Color goBtnNormal = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        Color goBtnHover = new Color(0.45f, 0.05f, 0.05f, 1f);

        GameObject restartBtnObj = CreateMenuButton(goPanelObj.transform, "RestartButton", "THỬ LẠI", new Vector2(0, -60), new Vector2(350, 70), goBtnNormal, goBtnHover, 24, pixelFont);
        GameObject goMainMenuBtnObj = CreateMenuButton(goPanelObj.transform, "MainMenuButton", "MENU CHÍNH", new Vector2(0, -150), new Vector2(350, 70), goBtnNormal, goBtnHover, 24, pixelFont);

        // Gán GameOverController
        GameOverController goCtrl = gameplayCanvas.gameObject.GetComponent<GameOverController>();
        if (goCtrl == null) goCtrl = gameplayCanvas.gameObject.AddComponent<GameOverController>();
        goCtrl.gameOverPanel = goPanelObj;

        UnityEventTools.AddVoidPersistentListener(restartBtnObj.GetComponent<Button>().onClick, goCtrl.RestartGame);
        UnityEventTools.AddVoidPersistentListener(goMainMenuBtnObj.GetComponent<Button>().onClick, goCtrl.LoadMainMenu);

        goPanelObj.SetActive(false);

        // Tạo hoặc cấu hình BossDefeatedPanel
        Transform existingBD = gameplayCanvas.transform.Find("BossDefeatedPanel");
        if (existingBD != null)
        {
            Undo.DestroyObjectImmediate(existingBD.gameObject);
        }

        GameObject bdPanelObj = new GameObject("BossDefeatedPanel");
        bdPanelObj.transform.SetParent(gameplayCanvas.transform, false);
        RectTransform bdRect = bdPanelObj.AddComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.sizeDelta = Vector2.zero;
        bdRect.anchoredPosition = Vector2.zero;

        Image bdBg = bdPanelObj.AddComponent<Image>();
        if (bossDefeatedBg != null)
        {
            bdBg.sprite = bossDefeatedBg;
            bdBg.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Giữ nguyên độ sắc nét của tranh vẽ
        }
        else
        {
            bdBg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f); // Đen mờ u ám dự phòng
        }

        Outline bdOutline = bdPanelObj.AddComponent<Outline>();
        bdOutline.effectColor = new Color(0.85f, 0.65f, 0.15f, 0.5f); // Viền vàng hoàng kim
        bdOutline.effectDistance = new Vector2(4, -4);

        // Title
        GameObject bdTitleObj = new GameObject("BossDefeatedTitle");
        bdTitleObj.transform.SetParent(bdPanelObj.transform, false);
        RectTransform bdTitleRect = bdTitleObj.AddComponent<RectTransform>();
        bdTitleRect.sizeDelta = new Vector2(1000, 100);
        bdTitleRect.anchoredPosition = new Vector2(0, 280);
        Text bdTitleText = bdTitleObj.AddComponent<Text>();
        bdTitleText.text = "THE FATE OF AURELIUS";
        bdTitleText.font = pixelFont;
        bdTitleText.fontSize = 55;
        bdTitleText.fontStyle = FontStyle.Bold;
        bdTitleText.alignment = TextAnchor.MiddleCenter;
        bdTitleText.color = new Color(0.95f, 0.75f, 0.15f, 1f); // Màu vàng hoàng kim rực rỡ
        
        Shadow bdTitleShadow = bdTitleObj.AddComponent<Shadow>();
        bdTitleShadow.effectColor = Color.black;
        bdTitleShadow.effectDistance = new Vector2(4, -4);

        // Subtitle
        GameObject bdSubObj = new GameObject("BossDefeatedSubtitle");
        bdSubObj.transform.SetParent(bdPanelObj.transform, false);
        RectTransform bdSubRect = bdSubObj.AddComponent<RectTransform>();
        bdSubRect.sizeDelta = new Vector2(1000, 100);
        bdSubRect.anchoredPosition = new Vector2(0, 190);
        Text bdSubText = bdSubObj.AddComponent<Text>();
        bdSubText.text = "Hiệp sĩ Thánh đã ngã xuống. Lựa chọn định mệnh cho linh hồn ông:";
        bdSubText.font = pixelFont;
        bdSubText.fontSize = 22;
        bdSubText.fontStyle = FontStyle.Normal;
        bdSubText.alignment = TextAnchor.MiddleCenter;
        bdSubText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        Shadow bdSubShadow = bdSubObj.AddComponent<Shadow>();
        bdSubShadow.effectColor = Color.black;
        bdSubShadow.effectDistance = new Vector2(2, -2);

        // Buttons (Sắp xếp Symmetrical: Trái vs Phải để khớp với giao diện Ánh sáng vs Bóng tối)
        Color btnRedemptionColor = new Color(0.08f, 0.28f, 0.14f, 0.9f); // Xanh lá cây hoàng kim nhạt (Bên Trái - Cứu Rỗi)
        Color btnRedemptionHover = new Color(0.12f, 0.48f, 0.24f, 1f);
        
        Color btnLegacyColor = new Color(0.35f, 0.05f, 0.05f, 0.9f); // Đỏ thẫm hắc ám (Bên Phải - Kế Thừa)
        Color btnLegacyHover = new Color(0.58f, 0.08f, 0.08f, 1f);

        // Nút bên trái (X = -320) và Nút bên phải (X = 320)
        GameObject redemptionBtnObj = CreateMenuButton(bdPanelObj.transform, "RedemptionButton", "CỨU RỖI (HÀO KHÍ ĐÔNG A)", new Vector2(-320, -180), new Vector2(440, 80), btnRedemptionColor, btnRedemptionHover, 18, pixelFont);
        GameObject legacyBtnObj = CreateMenuButton(bdPanelObj.transform, "LegacyButton", "KẾ THỪA (VÒNG LẶP TỐI TĂM)", new Vector2(320, -180), new Vector2(440, 80), btnLegacyColor, btnLegacyHover, 18, pixelFont);

        // Gán sự kiện cho các nút lựa chọn Ending
        UnityEventTools.AddVoidPersistentListener(redemptionBtnObj.GetComponent<Button>().onClick, pmCtrl.ChooseRedemptionEnding);
        UnityEventTools.AddVoidPersistentListener(legacyBtnObj.GetComponent<Button>().onClick, pmCtrl.ChooseLegacyEnding);

        bdPanelObj.SetActive(false);

        // Tự động gán Sprite Bánh Mì, Trà Sữa, Cà Phê cho PotionSystem trên Player để đảm bảo HUD hiển thị đúng
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            PotionSystem potionSys = playerObj.GetComponent<PotionSystem>();
            if (potionSys != null)
            {
                Undo.RecordObject(potionSys, "Gán Sprites Potion Việt Nam");
                potionSys.healthPotionSprite = LoadSpriteSafe("Assets/_Project/Sprites/VietNam/banh_mi.png");
                potionSys.manaPotionSprite = LoadSpriteSafe("Assets/_Project/Sprites/VietNam/tra_sua.png");
                potionSys.speedPotionSprite = LoadSpriteSafe("Assets/_Project/Sprites/VietNam/ca_phe.png");
                EditorUtility.SetDirty(potionSys);
                Debug.Log("[Setup] Đã gán tự động Sprite Bánh Mì, Trà Sữa, Cà Phê vào PotionSystem trên Player!");
            }
        }

        EditorSceneManager.MarkSceneDirty(gameplayCanvas.gameObject.scene);
        EditorSceneManager.SaveScene(gameplayCanvas.gameObject.scene);

        // ------------------ 3. SETUP MAIN MENU SCENE ------------------
        string targetMenuScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
        string targetFolder = "Assets/_Project/Scenes";
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // Tạo scene mới và xây dựng UI
        var mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Tìm Canvas mặc định hoặc tạo mới
        Canvas menuCanvas = Object.FindAnyObjectByType<Canvas>();
        if (menuCanvas == null)
        {
            GameObject canvasObj = new GameObject("MenuCanvas");
            menuCanvas = canvasObj.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Cập nhật background cho Menu Canvas
        GameObject bgObj = new GameObject("MenuBackground");
        bgObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image menuBgImg = bgObj.AddComponent<Image>();
        if (menuBg != null)
        {
            menuBgImg.sprite = menuBg;
            menuBgImg.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        }
        else
        {
            menuBgImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        }

        // Main Menu Controller Manager
        GameObject menuManager = new GameObject("MainMenuManager");
        MainMenuController menuCtrl = menuManager.AddComponent<MainMenuController>();
        StoryCutsceneController storyCtrl = menuManager.AddComponent<StoryCutsceneController>();

        // Container chứa toàn bộ UI menu chính gốc để ẩn/hiện dễ dàng
        GameObject mainMenuContainer = new GameObject("MainMenuContainer");
        mainMenuContainer.transform.SetParent(menuCanvas.transform, false);
        RectTransform mainContainerRect = mainMenuContainer.AddComponent<RectTransform>();
        mainContainerRect.anchorMin = Vector2.zero;
        mainContainerRect.anchorMax = Vector2.one;
        mainContainerRect.sizeDelta = Vector2.zero;

        // Cấu hình tham chiếu trên menuCtrl
        menuCtrl.mainMenuContainer = mainMenuContainer;

        // Title
        GameObject menuTitleObj = new GameObject("Title");
        menuTitleObj.transform.SetParent(mainMenuContainer.transform, false);
        RectTransform menuTitleRect = menuTitleObj.AddComponent<RectTransform>();
        menuTitleRect.sizeDelta = new Vector2(1200, 120);
        menuTitleRect.anchoredPosition = new Vector2(0, 240);
        Text menuTitleText = menuTitleObj.AddComponent<Text>();
        menuTitleText.text = "TRÁNG SĨ SƠN NAM";
        menuTitleText.font = pixelFont;
        menuTitleText.fontSize = 80;
        menuTitleText.fontStyle = FontStyle.Bold;
        menuTitleText.alignment = TextAnchor.MiddleCenter;
        menuTitleText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        Shadow menuTitleShadow = menuTitleObj.AddComponent<Shadow>();
        menuTitleShadow.effectColor = Color.black;
        menuTitleShadow.effectDistance = new Vector2(4, -4);

        // Subtitle
        GameObject menuSubObj = new GameObject("Subtitle");
        menuSubObj.transform.SetParent(mainMenuContainer.transform, false);
        RectTransform menuSubRect = menuSubObj.AddComponent<RectTransform>();
        menuSubRect.sizeDelta = new Vector2(800, 50);
        menuSubRect.anchoredPosition = new Vector2(0, 160);
        Text menuSubText = menuSubObj.AddComponent<Text>();
        menuSubText.text = "Hào Khí Đông A";
        menuSubText.font = pixelFont;
        menuSubText.fontSize = 26;
        menuSubText.fontStyle = FontStyle.Italic;
        menuSubText.alignment = TextAnchor.MiddleCenter;
        menuSubText.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        Shadow menuSubShadow = menuSubObj.AddComponent<Shadow>();
        menuSubShadow.effectColor = Color.black;
        menuSubShadow.effectDistance = new Vector2(2, -2);

        // Menu Buttons
        GameObject playBtnObj = CreateMenuButton(mainMenuContainer.transform, "PlayButton", "KHỞI HÀNH", new Vector2(0, 40), new Vector2(360, 75), goBtnNormal, goBtnHover, 24, pixelFont);
        GameObject storyBtnObj = CreateMenuButton(mainMenuContainer.transform, "StoryButton", "BIÊN NIÊN SỬ", new Vector2(0, -50), new Vector2(360, 75), goBtnNormal, goBtnHover, 24, pixelFont);
        GameObject quitGameBtnObj = CreateMenuButton(mainMenuContainer.transform, "QuitButton", "THOÁT GAME", new Vector2(0, -140), new Vector2(360, 75), goBtnNormal, goBtnHover, 24, pixelFont);

        // --- CREDITS PANEL (Góc phải màn hình Main Menu) ---
        GameObject creditsPanelObj = new GameObject("CreditsPanel");
        creditsPanelObj.transform.SetParent(mainMenuContainer.transform, false);
        RectTransform creditsRect = creditsPanelObj.AddComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(1, 0); // Bottom-Right
        creditsRect.anchorMax = new Vector2(1, 0);
        creditsRect.sizeDelta = new Vector2(400, 280);
        creditsRect.anchoredPosition = new Vector2(-220, 160);

        Image creditsImg = creditsPanelObj.AddComponent<Image>();
        creditsImg.color = new Color(0f, 0f, 0f, 0.65f); // Đen mờ u ám

        Outline creditsOutline = creditsPanelObj.AddComponent<Outline>();
        creditsOutline.effectColor = new Color(0.4f, 0.05f, 0.05f, 0.5f); // Viền đỏ máu nhạt
        creditsOutline.effectDistance = new Vector2(2, -2);

        // FPTUCT Icon
        string fptuIconPath = "Assets/IconFPTU/FPTUCT.png";
        ConfigureSpriteImporter(fptuIconPath);
        Sprite fptuSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fptuIconPath);

        GameObject fptuIconObj = new GameObject("FPTUIcon");
        fptuIconObj.transform.SetParent(creditsPanelObj.transform, false);
        RectTransform fptuRect = fptuIconObj.AddComponent<RectTransform>();
        fptuRect.anchorMin = new Vector2(0.5f, 1); // Top Center
        fptuRect.anchorMax = new Vector2(0.5f, 1);
        fptuRect.sizeDelta = new Vector2(160, 60);
        fptuRect.anchoredPosition = new Vector2(0, -50);

        Image fptuImg = fptuIconObj.AddComponent<Image>();
        if (fptuSprite != null)
        {
            fptuImg.sprite = fptuSprite;
            fptuImg.color = Color.white;
        }
        else
        {
            fptuImg.color = new Color(1, 1, 1, 0.1f);
        }

        // Members Text
        GameObject membersObj = new GameObject("MembersText");
        membersObj.transform.SetParent(creditsPanelObj.transform, false);
        RectTransform membersRect = membersObj.AddComponent<RectTransform>();
        membersRect.anchorMin = Vector2.zero;
        membersRect.anchorMax = Vector2.one;
        membersRect.sizeDelta = new Vector2(-30, -110); // Lùi lề
        membersRect.anchoredPosition = new Vector2(0, -45); // Nằm dưới Logo

        Text membersText = membersObj.AddComponent<Text>();
        membersText.text = "PROJECT MEMBERS:\n" +
                           "• SE171440 - Hứa Hoàng Minh\n" +
                           "• SE150834 - Nguyễn Quốc Khánh\n" +
                           "• Se172559 - Huỳnh Hoàng Tiến\n" +
                           "• SE160204 - Trương Anh Kiệt";
        membersText.font = pixelFont;
        membersText.fontSize = 15;
        membersText.fontStyle = FontStyle.Bold;
        membersText.alignment = TextAnchor.UpperLeft;
        membersText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        membersText.lineSpacing = 1.3f;

        Shadow membersShadow = membersObj.AddComponent<Shadow>();
        membersShadow.effectColor = Color.black;
        membersShadow.effectDistance = new Vector2(2, -2);

        // --- STORY SUBMENU PANEL ---
        GameObject storySubPanelObj = new GameObject("StorySubmenuPanel");
        storySubPanelObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform storySubRect = storySubPanelObj.AddComponent<RectTransform>();
        storySubRect.sizeDelta = new Vector2(500, 480);
        storySubRect.anchoredPosition = Vector2.zero;
        Image storySubImg = storySubPanelObj.AddComponent<Image>();
        storySubImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        Outline storySubOutline = storySubPanelObj.AddComponent<Outline>();
        storySubOutline.effectColor = new Color(0.5f, 0.1f, 0.1f, 0.7f);
        storySubOutline.effectDistance = new Vector2(3, -3);
        storySubPanelObj.SetActive(false);

        // Cấu hình tham chiếu trên menuCtrl
        menuCtrl.storySubmenuPanel = storySubPanelObj;

        // Submenu Title
        GameObject storySubTitleObj = new GameObject("Title");
        storySubTitleObj.transform.SetParent(storySubPanelObj.transform, false);
        RectTransform storySubTitleRect = storySubTitleObj.AddComponent<RectTransform>();
        storySubTitleRect.sizeDelta = new Vector2(400, 60);
        storySubTitleRect.anchoredPosition = new Vector2(0, 180);
        Text storySubTitleText = storySubTitleObj.AddComponent<Text>();
        storySubTitleText.text = "BIÊN NIÊN SỬ";
        storySubTitleText.font = pixelFont;
        storySubTitleText.fontSize = 32;
        storySubTitleText.fontStyle = FontStyle.Bold;
        storySubTitleText.alignment = TextAnchor.MiddleCenter;
        storySubTitleText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
        Shadow storySubTitleShadow = storySubTitleObj.AddComponent<Shadow>();
        storySubTitleShadow.effectColor = Color.black;
        storySubTitleShadow.effectDistance = new Vector2(2, -2);

        // Submenu Buttons
        GameObject introBtn = CreateMenuButton(storySubPanelObj.transform, "IntroBtn", "1. KHỞI NGUỒN ĐẠI VIỆT", new Vector2(0, 80), new Vector2(380, 60), btnNormalColor, btnHoverColor, 18, pixelFont);
        GameObject redemptionBtn = CreateMenuButton(storySubPanelObj.transform, "RedemptionBtn", "2. KẾT CỤC CỨU RỖI", new Vector2(0, 0), new Vector2(380, 60), btnNormalColor, btnHoverColor, 18, pixelFont);
        GameObject legacyBtn = CreateMenuButton(storySubPanelObj.transform, "LegacyBtn", "3. KẾT CỤC KẾ THỪA", new Vector2(0, -80), new Vector2(380, 60), btnNormalColor, btnHoverColor, 18, pixelFont);
        GameObject backSubBtn = CreateMenuButton(storySubPanelObj.transform, "BackSubBtn", "QUAY LẠI MENU", new Vector2(0, -160), new Vector2(380, 60), new Color(0.1f, 0.1f, 0.1f, 1f), new Color(0.2f, 0.2f, 0.2f, 1f), 18, pixelFont);

        // --- STORY PLAY PANEL (FULL SCREEN) ---
        GameObject storyPanelObj = new GameObject("StoryPanel");
        storyPanelObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform storyPanelRect = storyPanelObj.AddComponent<RectTransform>();
        storyPanelRect.anchorMin = Vector2.zero;
        storyPanelRect.anchorMax = Vector2.one;
        storyPanelRect.sizeDelta = Vector2.zero;
        storyPanelRect.anchoredPosition = Vector2.zero;
        Image storyPanelImg = storyPanelObj.AddComponent<Image>();
        storyPanelImg.color = Color.black;
        storyPanelObj.SetActive(false);

        // Text Slideshow
        GameObject ssTextObj = new GameObject("SlideshowText");
        ssTextObj.transform.SetParent(storyPanelObj.transform, false);
        RectTransform ssTextRect = ssTextObj.AddComponent<RectTransform>();
        ssTextRect.sizeDelta = new Vector2(1400, 400);
        ssTextRect.anchoredPosition = Vector2.zero;
        Text ssText = ssTextObj.AddComponent<Text>();
        ssText.font = pixelFont;
        ssText.fontSize = 30;
        ssText.fontStyle = FontStyle.Bold;
        ssText.alignment = TextAnchor.MiddleCenter;
        ssText.color = Color.white;
        ssText.lineSpacing = 1.4f;
        Shadow ssTextShadow = ssTextObj.AddComponent<Shadow>();
        ssTextShadow.effectColor = Color.black;
        ssTextShadow.effectDistance = new Vector2(2, -2);

        // Video Render Display (RawImage)
        GameObject rawImgObj = new GameObject("VideoDisplay");
        rawImgObj.transform.SetParent(storyPanelObj.transform, false);
        RectTransform rawImgRect = rawImgObj.AddComponent<RectTransform>();
        rawImgRect.anchorMin = Vector2.zero;
        rawImgRect.anchorMax = Vector2.one;
        rawImgRect.sizeDelta = Vector2.zero;
        rawImgRect.anchoredPosition = Vector2.zero;
        RawImage rawImg = rawImgObj.AddComponent<RawImage>();
        rawImg.color = Color.black;

        // Video Player component on manager
        UnityEngine.Video.VideoPlayer vp = menuManager.AddComponent<UnityEngine.Video.VideoPlayer>();
        vp.playOnAwake = false;
        vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;

        // Credits Container (cuộn chữ)
        GameObject credContainerObj = new GameObject("StoryCreditsContainer");
        credContainerObj.transform.SetParent(storyPanelObj.transform, false);
        RectTransform credContainerRect = credContainerObj.AddComponent<RectTransform>();
        credContainerRect.sizeDelta = new Vector2(1000, 1000);
        credContainerRect.anchoredPosition = new Vector2(0, -600); // Khởi đầu bên dưới
        
        Text storyCredText = credContainerObj.AddComponent<Text>();
        storyCredText.text = "THE FALLEN KNIGHT\n\n\n" +
                             "PHÁT TRIỂN BỞI NHÓM FPTU:\n\n" +
                             "• SE171440 - Hứa Hoàng Minh\n\n" +
                             "• SE150834 - Nguyễn Quốc Khánh\n\n" +
                             "• Se172559 - Huỳnh Hoàng Tiến\n\n" +
                             "• SE160204 - Trương Anh Kiệt\n\n\n" +
                             "CẢM ƠN BẠN ĐÃ TRẢI NGHIỆM!";
        storyCredText.font = pixelFont;
        storyCredText.fontSize = 28;
        storyCredText.fontStyle = FontStyle.Bold;
        storyCredText.alignment = TextAnchor.UpperCenter;
        storyCredText.color = new Color(0.9f, 0.1f, 0.1f, 1f);
        Shadow storyCredShadow = credContainerObj.AddComponent<Shadow>();
        storyCredShadow.effectColor = Color.black;
        storyCredShadow.effectDistance = new Vector2(3, -3);

        // Skip Button (Góc dưới bên phải)
        GameObject skipBtnObj = CreateMenuButton(storyPanelObj.transform, "SkipBtn", "BỎ QUA (ESC)", new Vector2(750, -450), new Vector2(240, 60), new Color(0.2f, 0.05f, 0.05f, 0.8f), new Color(0.4f, 0.05f, 0.05f, 1f), 18, pixelFont);

        // Cài đặt Controller
        UnityEngine.Video.VideoClip introClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/video/intro_boss_transformation.mp4");
        UnityEngine.Video.VideoClip redemptionClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/video/ending_redemption.mp4");
        UnityEngine.Video.VideoClip legacyClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/video/ending_legacy.mp4");

        storyCtrl.storyPanel = storyPanelObj;
        storyCtrl.slideshowText = ssText;
        storyCtrl.videoDisplay = rawImg;
        storyCtrl.videoPlayer = vp;
        storyCtrl.creditsContainer = credContainerRect;
        storyCtrl.creditsText = storyCredText;
        storyCtrl.mainMenuCanvasGroup = storySubPanelObj; // Khi kết thúc sẽ mở lại Submenu

        storyCtrl.introClip = introClip;
        storyCtrl.redemptionClip = redemptionClip;
        storyCtrl.legacyClip = legacyClip;

        // Nội dung Slide
        storyCtrl.introSlides = new string[] {
            "Hàng nghìn năm trước, Tráng Sĩ Sơn Nam là vị anh hùng oai hùng đã đánh tan giặc ngoại xâm phương Bắc và bảo vệ bờ cõi Đại Việt.",
            "Sau thắng lợi vẻ vang, do mang nợ máu chiến trận, vong hồn ông không thể siêu thoát, lưu lạc nhân gian suốt nhiều triều đại.",
            "Theo thời gian, tâm trí ông dần bị tà khí xâm chiếm, ảo tưởng rằng chiến tranh vẫn chưa kết thúc.",
            "Người anh hùng năm xưa giờ trở thành mối hiểm họa đe dọa bờ cõi bình yên của nước nhà.",
            "Người chơi vào vai cậu học trò nghèo mang trong mình dòng máu anh hùng 'Hào Khí Đông A'...",
            "...với lòng yêu nước nồng nàn và ý chí bất khuất, quyết tâm thức tỉnh Tráng Sĩ Sơn Nam khỏi cơn u mê ngàn năm!"
        };
        
        storyCtrl.redemptionSlides = new string[] {
            "Lời ru đất mẹ dịu êm vang vọng...",
            "Hào Khí Đông A đã thanh tẩy tà khí, giúp tráng sĩ Sơn Nam trút bỏ gươm đao, mỉm cười siêu thoát.",
            "Đất nước thanh bình vang tiếng ca thái bình thịnh trị!"
        };
        
        storyCtrl.legacySlides = new string[] {
            "Tráng sĩ ngã xuống, nhưng tà niệm chiến tranh vẫn chưa chịu tan biến.",
            "Cầm lấy thanh thần kiếm vương đầy hắc khí, cậu thiếu niên bị bóng tối nuốt chửng, trở thành hộ vệ mới của cõi u minh.",
            "Vòng lặp giặc giã và thù hận lại bắt đầu..."
        };

        // Gán sự kiện click bằng UnityEventTools
        UnityEventTools.AddVoidPersistentListener(playBtnObj.GetComponent<Button>().onClick, menuCtrl.PlayGame);
        UnityEventTools.AddVoidPersistentListener(quitGameBtnObj.GetComponent<Button>().onClick, menuCtrl.QuitGame);

        // Hiển thị submenu Story khi click Story Button
        Button storyBtn = storyBtnObj.GetComponent<Button>();
        UnityEventTools.AddVoidPersistentListener(storyBtn.onClick, menuCtrl.ShowStorySubmenu);

        // Trở về Main Menu từ Story Submenu
        Button backSub = backSubBtn.GetComponent<Button>();
        UnityEventTools.AddVoidPersistentListener(backSub.onClick, menuCtrl.HideStorySubmenu);

        // Chạy các đoạn cutscene tương ứng
        UnityEventTools.AddVoidPersistentListener(introBtn.GetComponent<Button>().onClick, storyCtrl.PlayIntroSequence);
        UnityEventTools.AddVoidPersistentListener(redemptionBtn.GetComponent<Button>().onClick, storyCtrl.PlayRedemptionSequence);
        UnityEventTools.AddVoidPersistentListener(legacyBtn.GetComponent<Button>().onClick, storyCtrl.PlayLegacySequence);

        // Nút Skip
        UnityEventTools.AddVoidPersistentListener(skipBtnObj.GetComponent<Button>().onClick, storyCtrl.ExitStory);

        // Đảm bảo có EventSystem trong MainMenuScene để tương tác được các nút
        UnityEngine.EventSystems.EventSystem eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Lưu scene Main Menu
        EditorSceneManager.SaveScene(mainMenuScene, targetMenuScenePath);

        // ------------------ 4. UPDATE BUILD SETTINGS ------------------
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        string mainMenuScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
        gameplayScenePath = "Assets/_Project/Scenes/SampleScene.unity";

        buildScenes.Add(new EditorBuildSettingsScene(mainMenuScenePath, true));
        buildScenes.Add(new EditorBuildSettingsScene(gameplayScenePath, true));

        // Thêm scene gốc nếu khác hai scene trên
        if (!string.IsNullOrEmpty(originalScenePath) && 
            originalScenePath != mainMenuScenePath && 
            originalScenePath != gameplayScenePath)
        {
            buildScenes.Add(new EditorBuildSettingsScene(originalScenePath, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();

        // ------------------ 5. RETURN TO ORIGINAL GAMEPLAY SCENE ------------------
        EditorSceneManager.OpenScene(originalScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công", 
            "Đã khởi tạo hệ thống Giao Diện Dark Fantasy & Câu chuyện thành công:\n\n" +
            "1. Tích hợp Font Pixel Game tự động cho tất cả menu (Pause, Game Over, Main Menu, Story).\n" +
            "2. Tạo Submenu Biên Niên Sử với 3 phần: Lore Intro, Redemption Ending, Legacy Ending.\n" +
            "3. Hỗ trợ hiển thị chữ trượt (Slideshow), chạy Video Player tương ứng toàn màn hình và tự động cuộn Credits tên thành viên nhóm!\n\n" +
            "Nhấn Play trong Unity để cảm nhận thành quả!", "Tuyệt vời");
    }

    private static GameObject CreateMenuButton(Transform parent, string name, string textStr, Vector2 pos, Vector2 size, Color normalColor, Color highlightedColor, int fontSize, Font customFont = null)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        
        // Tạo nút bấm có hiệu ứng màu chuyển đổi
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = new Color(normalColor.r * 0.5f, normalColor.g * 0.5f, normalColor.b * 0.5f, 1f);
        colors.selectedColor = normalColor;
        btn.colors = colors;

        // Thêm Text làm con của Button
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.text = textStr;
        txt.font = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        // Thêm Shadow nhẹ cho chữ trên nút bấm cho đẹp mắt
        Shadow shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.75f);
        shadow.effectDistance = new Vector2(2, -2);

        return btnObj;
    }


    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigurePixelSpriteImporter(string path, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool needsSave = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsSave = true;
            }
            // Nếu spriteImportMode chưa được cấu hình, mới đặt thành Single
            if (importer.spriteImportMode == SpriteImportMode.None)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsSave = true;
            }
            // Chỉ ghi đè PPU nếu người dùng chưa đổi sang 16 (hoặc giá trị khác 100 mặc định)
            if (importer.spritePixelsPerUnit == 100f && ppu != 100f)
            {
                importer.spritePixelsPerUnit = ppu;
                needsSave = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                needsSave = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsSave = true;
            }
            if (needsSave)
            {
                importer.SaveAndReimport();
            }
        }
    }

    [MenuItem("Tools/Setup Clean Tilemap Grid")]
    public static void SetupCleanTilemapGrid()
    {
        // 1. Tìm hoặc tạo Grid
        Grid grid = Object.FindAnyObjectByType<Grid>();
        GameObject gridObj;
        if (grid == null)
        {
            gridObj = new GameObject("Grid");
            gridObj.transform.position = Vector3.zero;
            gridObj.AddComponent<Grid>();
            Undo.RegisterCreatedObjectUndo(gridObj, "Tạo Grid mới");
        }
        else
        {
            gridObj = grid.gameObject;
        }

        // 2. Tạo hoặc cấu hình 3 Layer Tilemap: Ground, Background, Decorations
        Tilemap groundMap = SetupTilemapLayer(gridObj.transform, "Tilemap_Ground", 0, true);
        Tilemap bgMap = SetupTilemapLayer(gridObj.transform, "Tilemap_Background", -10, false);
        Tilemap decorMap = SetupTilemapLayer(gridObj.transform, "Tilemap_Decorations", 5, false);

        if (groundMap == null || bgMap == null || decorMap == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không thể khởi tạo các lớp Tilemap!", "OK");
            return;
        }

        // 3. Xóa sạch gạch cũ để tránh bị lỗi lộn xộn
        groundMap.ClearAllTiles();
        bgMap.ClearAllTiles();
        decorMap.ClearAllTiles();

        // 4. Tạo hoặc gán Background u ám phía sau
        string bgImgPath = "Assets/_Project/Sprites/Environment/PixelPlatformerSet1v.1.1/Background/03 background A.png";
        if (!File.Exists(bgImgPath)) bgImgPath = "Assets/_Project/Sprites/Environment/PixelPlatformerSet1v.1.1/Background/01 background.png";
        
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgImgPath);
        if (bgSprite != null)
        {
            Transform oldBg = gridObj.transform.Find("ParallaxBackground");
            if (oldBg != null) Undo.DestroyObjectImmediate(oldBg.gameObject);

            GameObject bgSpriteObj = new GameObject("ParallaxBackground");
            bgSpriteObj.transform.SetParent(gridObj.transform, false);
            bgSpriteObj.transform.position = new Vector3(8f, 2f, 10f); // Đưa ra sau
            
            SpriteRenderer bgSR = bgSpriteObj.AddComponent<SpriteRenderer>();
            bgSR.sprite = bgSprite;
            bgSR.drawMode = SpriteDrawMode.Tiled;
            bgSR.size = new Vector2(100f, 30f);
            bgSR.color = new Color(0.4f, 0.4f, 0.4f, 1f); // Giảm sáng để tăng không khí dark fantasy
            bgSR.sortingOrder = -20;
            
            Undo.RegisterCreatedObjectUndo(bgSpriteObj, "Tạo Parallax Background");
        }

        // 5. Đặt lại vị trí xuất phát của Player về (0, 0, 0) để người dùng tự vẽ xung quanh dễ dàng
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Định vị Player");
            player.transform.position = new Vector3(0f, 1f, 0f);
            Debug.Log("[GridSetup] Đã đặt lại vị trí xuất phát của Player về (0, 1, 0).");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Thành công", 
            "Đã khởi tạo Grid & các lớp Tilemap trống hoàn tất!\n\n" +
            "1. Grid & các lớp (Ground, Background, Decorations) được chia theo Layer chuẩn.\n" +
            "2. Layer 'Tilemap_Ground' đã được gắn CompositeCollider2D hỗ trợ va chạm mượt mà.\n" +
            "3. Đã làm sạch toàn bộ gạch lộn xộn để bạn sẵn sàng tự vẽ thủ công bằng Rule Tile!", "OK");
    }

    [MenuItem("Tools/Create Ground Rule Tile")]
    public static void CreateGroundRuleTile()
    {
        // 1. Khởi tạo đối tượng RuleTile
        RuleTile ruleTile = ScriptableObject.CreateInstance<RuleTile>();
        
        // 2. Tìm các Sprites từ main_lev_build.png
        string texturePath = "Assets/_Project/Sprites/Environment/PixelPlatformerSet1v.1.1/main_lev_build.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        
        System.Collections.Generic.List<Sprite> spriteList = new System.Collections.Generic.List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                spriteList.Add(sprite);
            }
        }

        if (spriteList.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy sprite nào được cắt trong main_lev_build.png! Hãy đảm bảo đã chỉnh Texture Import Settings thành Multiple và Sprite Editor đã slice 16x16.", "OK");
            return;
        }

        // Hàm helper để tìm Sprite theo tọa độ x, y chính xác
        Sprite FindSpriteAt(float x, float y)
        {
            foreach (var s in spriteList)
            {
                // Kiểm tra sai số nhỏ do định dạng float
                if (Mathf.Abs(s.rect.x - x) < 1f && Mathf.Abs(s.rect.y - y) < 1f)
                {
                    return s;
                }
            }
            return null;
        }

        // Lấy các Sprite theo tọa độ 16x16 trên tấm ảnh gốc
        Sprite spTopLeft = FindSpriteAt(32f, 1216f);
        Sprite spTopCenter = FindSpriteAt(48f, 1216f);
        Sprite spTopRight = FindSpriteAt(64f, 1216f);
        
        Sprite spMiddleLeft = FindSpriteAt(32f, 1200f);
        Sprite spMiddleCenter = FindSpriteAt(48f, 1200f);
        Sprite spMiddleRight = FindSpriteAt(64f, 1200f);
        
        Sprite spBottomLeft = FindSpriteAt(32f, 1168f);
        Sprite spBottomCenter = FindSpriteAt(48f, 1168f);
        Sprite spBottomRight = FindSpriteAt(64f, 1168f);

        // Fallback dùng tên/chỉ số nếu không tìm thấy bằng tọa độ (trong trường hợp cách slice khác)
        if (spTopCenter == null)
        {
            System.Collections.Generic.Dictionary<string, Sprite> spriteDict = new System.Collections.Generic.Dictionary<string, Sprite>();
            foreach (var s in spriteList) spriteDict[s.name] = s;

            spriteDict.TryGetValue("main_lev_build_2", out spTopCenter);
            spriteDict.TryGetValue("main_lev_build_1", out spTopLeft);
            spriteDict.TryGetValue("main_lev_build_3", out spTopRight);
            spriteDict.TryGetValue("main_lev_build_60", out spMiddleLeft);
            spriteDict.TryGetValue("main_lev_build_61", out spMiddleCenter);
            spriteDict.TryGetValue("main_lev_build_62", out spMiddleRight);
            spriteDict.TryGetValue("main_lev_build_147", out spBottomLeft);
            spriteDict.TryGetValue("main_lev_build_148", out spBottomCenter);
            spriteDict.TryGetValue("main_lev_build_149", out spBottomRight);
        }

        // Đặt hình hiển thị mặc định
        ruleTile.m_DefaultSprite = spTopCenter != null ? spTopCenter : (spriteList.Count > 0 ? spriteList[0] : null);

        // 4. Định nghĩa danh sách các quy tắc (Tiling Rules)
        ruleTile.m_TilingRules = new System.Collections.Generic.List<RuleTile.TilingRule>();

        // Quy tắc neighbors của RuleTile (8 neighbors):
        // 0: NW, 1: N, 2: NE, 3: W, 4: E, 5: SW, 6: S, 7: SE
        // Giá trị: DontCare = 0, This = 1, NotThis = 2

        void AddRule(Sprite sprite, int[] neighbors)
        {
            if (sprite != null)
            {
                RuleTile.TilingRule rule = new RuleTile.TilingRule();
                rule.m_Sprites = new Sprite[] { sprite };
                rule.m_Output = RuleTile.TilingRule.OutputSprite.Single;
                rule.m_Neighbors = new System.Collections.Generic.List<int>(new int[8]);
                
                for (int i = 0; i < 8; i++)
                {
                    rule.m_Neighbors[i] = neighbors[i];
                }
                ruleTile.m_TilingRules.Add(rule);
            }
        }

        // Thêm các quy tắc bo viền 9-slice linh hoạt (16 quy tắc để vẽ được cả cột dọc, cầu ngang mỏng và căn phòng rỗng)
        // 1. Single Floating Tile (Bốn phía đều trống)
        AddRule(spTopCenter, new int[] {
            2, 2, 2,
            2,    2,
            2, 2, 2
        });

        // 2. Thin Horizontal Platform Left End (Trái trống, Trên trống, Dưới trống, Phải có)
        AddRule(spTopLeft, new int[] {
            2, 2, 2,
            2,    1,
            2, 2, 2
        });

        // 3. Thin Horizontal Platform Right End (Phải trống, Trên trống, Dưới trống, Trái có)
        AddRule(spTopRight, new int[] {
            2, 2, 2,
            1,    2,
            2, 2, 2
        });

        // 4. Thin Horizontal Platform Middle (Trên trống, Dưới trống, Trái/Phải có)
        AddRule(spTopCenter, new int[] {
            2, 2, 2,
            1,    1,
            2, 2, 2
        });

        // 5. Thin Vertical Wall Top End (Trên trống, Trái/Phải trống, Dưới có)
        AddRule(spTopCenter, new int[] {
            2, 2, 2,
            2,    2,
            0, 1, 0
        });

        // 6. Thin Vertical Wall Bottom End (Dưới trống, Trái/Phải trống, Trên có)
        AddRule(spBottomCenter, new int[] {
            0, 1, 0,
            2,    2,
            2, 2, 2
        });

        // 7. Thin Vertical Wall Middle (Trái/Phải trống, Trên/Dưới có)
        AddRule(spMiddleLeft, new int[] {
            2, 1, 2,
            2,    2,
            2, 1, 2
        });

        // 8. Top-Left Corner
        AddRule(spTopLeft, new int[] {
            2, 2, 0,
            2,    1,
            0, 1, 1
        });

        // 9. Top-Right Corner
        AddRule(spTopRight, new int[] {
            0, 2, 2,
            1,    2,
            1, 1, 0
        });

        // 10. Top-Center
        AddRule(spTopCenter, new int[] {
            2, 2, 2,
            1,    1,
            1, 1, 1
        });

        // 11. Bottom-Left Corner
        AddRule(spBottomLeft, new int[] {
            0, 1, 1,
            2,    1,
            2, 2, 0
        });

        // 12. Bottom-Right Corner
        AddRule(spBottomRight, new int[] {
            1, 1, 0,
            1,    2,
            0, 2, 2
        });

        // 13. Bottom-Center (Ceiling)
        AddRule(spBottomCenter, new int[] {
            1, 1, 1,
            1,    1,
            2, 2, 2
        });

        // 14. Left Wall
        AddRule(spMiddleLeft, new int[] {
            2, 1, 1,
            2,    1,
            2, 1, 1
        });

        // 15. Right Wall
        AddRule(spMiddleRight, new int[] {
            1, 1, 2,
            1,    2,
            1, 1, 2
        });

        // 16. Center Dirt (Lòng đất)
        AddRule(spMiddleCenter, new int[] {
            1, 1, 1,
            1,    1,
            1, 1, 1
        });

        // 5. Tạo file Asset lưu trữ
        string savePath = "Assets/_Project/Tilemaps/Ground_RuleTile.asset";
        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        AssetDatabase.CreateAsset(ruleTile, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công", 
            $"Đã tạo thành công Ground_RuleTile (16x16) tại:\n{savePath}\n\n" +
            "Cách sử dụng:\n" +
            "1. Kéo tệp Ground_RuleTile này vào cửa sổ Tile Palette của bạn.\n" +
            "2. Sử dụng cọ vẽ (Brush) chọn Rule Tile này để vẽ địa hình trong Scene view. Unity sẽ tự động bo viền cỏ, vách đá, lòng đất cực kỳ thông minh!", "Tuyệt vời");
    }

    private static Tilemap SetupTilemapLayer(Transform parent, string name, int sortingOrder, bool hasCollider)
    {
        Transform child = parent.Find(name);
        GameObject layerObj;
        if (child != null)
        {
            layerObj = child.gameObject;
        }
        else
        {
            layerObj = new GameObject(name);
            layerObj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(layerObj, "Tạo Layer " + name);
        }

        Tilemap tilemap = layerObj.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = layerObj.AddComponent<Tilemap>();

        TilemapRenderer renderer = layerObj.GetComponent<TilemapRenderer>();
        if (renderer == null) renderer = layerObj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (hasCollider)
        {
            TilemapCollider2D collider = layerObj.GetComponent<TilemapCollider2D>();
            if (collider == null) collider = layerObj.AddComponent<TilemapCollider2D>();
            
            CompositeCollider2D composite = layerObj.GetComponent<CompositeCollider2D>();
            if (composite == null) composite = layerObj.AddComponent<CompositeCollider2D>();
            
            Rigidbody2D rb = layerObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = layerObj.AddComponent<Rigidbody2D>();
            
            rb.bodyType = RigidbodyType2D.Static;
            collider.usedByComposite = true;
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
        }

        return tilemap;
    }

    [MenuItem("Tools/Create Torch C-04 Object")]
    public static void CreateTorchC04Object()
    {
        string baseDir = "Assets/_Project/Sprites/Environment/PixelPlatformerSet1v.1.1/Anim/";
        
        // Cấu hình đồng bộ PPU = 16 và Point Filter cho toàn bộ 4 frame đuốc C để tránh lỗi to nhỏ giật giật
        ConfigureTorchSpritePPU(baseDir + "torch-C-01.png");
        ConfigureTorchSpritePPU(baseDir + "torch-C-02.png");
        ConfigureTorchSpritePPU(baseDir + "torch-C-03.png");
        ConfigureTorchSpritePPU(baseDir + "torch-C-04.png");

        // Gọi tạo/tải Sprite hào quang trước tiên để tránh việc import asset làm hỏng tham chiếu đối tượng trong Hierarchy
        Sprite glowSprite = GetOrCreateGlowSprite();

        // 1. Tạo GameObject Đuốc
        GameObject torchObj = new GameObject("Torch-C-04");
        torchObj.transform.position = Vector3.zero;

        // 2. Thêm SpriteRenderer và gán Sprite torch-C-04
        SpriteRenderer sr = torchObj.AddComponent<SpriteRenderer>();
        string spritePath = baseDir + "torch-C-04.png";
        Sprite torchSprite = LoadSpriteSafe(spritePath);
        if (torchSprite != null)
        {
            sr.sprite = torchSprite;
            sr.sortingOrder = 5; 
        }
        else
        {
            Debug.LogWarning($"[TorchSetup] Không tìm thấy sprite đuốc tại: {spritePath}");
        }

        // 3. Tạo hào quang giả lập ánh sáng (Glow_Halo)
        GameObject haloObj = new GameObject("Glow_Halo");
        haloObj.transform.SetParent(torchObj.transform, false);
        haloObj.transform.localPosition = new Vector3(0f, 0.2f, 0f); 
        haloObj.transform.localScale = new Vector3(3.5f, 3.5f, 1f); 

        SpriteRenderer haloSr = haloObj.AddComponent<SpriteRenderer>();
        if (glowSprite != null)
        {
            haloSr.sprite = glowSprite;
            haloSr.color = new Color(1f, 0.45f, 0.1f, 0.25f);
            haloSr.sortingOrder = 4; 
        }

        // 4. Thêm TorchFlicker script để chạy anim và flicker hào quang
        TorchFlicker flicker = torchObj.AddComponent<TorchFlicker>();
        flicker.glowHaloRenderer = haloSr;
        
        // Tải 4 frame hình hoạt họa đuốc C để chạy hoạt hình ngọn lửa
        Sprite[] frames = new Sprite[4];
        frames[0] = LoadSpriteSafe(baseDir + "torch-C-01.png");
        frames[1] = LoadSpriteSafe(baseDir + "torch-C-02.png");
        frames[2] = LoadSpriteSafe(baseDir + "torch-C-03.png");
        frames[3] = LoadSpriteSafe(baseDir + "torch-C-04.png");
        
        flicker.animationFrames = frames;

        // Đăng ký Undo và chọn đối tượng mới tạo trong Hierarchy
        Undo.RegisterCreatedObjectUndo(torchObj, "Tạo Torch-C-04");
        Selection.activeGameObject = torchObj;

        EditorUtility.DisplayDialog("Thành công", 
            "Đã tạo thành công đối tượng đuốc Torch-C-04 với Hào Quang Giả Lập trong Hierarchy!\n\n" +
            "1. Đã tự động gắn 4 frame Sprite đuốc (hoạt ảnh cháy nhấp nháy).\n" +
            "2. Đã tự động cấu hình toàn bộ frame đuốc về PPU = 16 và Point Filter để hiển thị Pixel Art sắc nét, không bị giật kích thước.\n" +
            "3. Đã tự động tạo Child 'Glow_Halo' bằng Sprite Hào quang mềm mại.\n" +
            "4. Hào quang tự bập bùng thay đổi độ mờ ngẫu nhiên cực kỳ tự nhiên.\n\n" +
            "Gợi ý: Bây giờ bạn có thể Duplicate (Ctrl+D) đuốc để đặt khắp nơi!", "Tuyệt vời");
    }

    private static void ConfigureTorchSpritePPU(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.spritePixelsToUnits != 16)
            {
                importer.spritePixelsToUnits = 16;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static Sprite LoadSpriteSafe(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        // Fallback cho chế độ Multiple
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object sub in subAssets)
        {
            if (sub is Sprite s)
            {
                return s;
            }
        }
        return null;
    }

    private static Sprite GetOrCreateGlowSprite()
    {
        string spritePath = "Assets/_Project/Sprites/Environment/torch_glow.png";
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (glowSprite != null) return glowSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(dist / maxDist);
                
                float alpha = Mathf.Pow(1f - t, 2.2f); // Falloff mượt mà
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string dir = Path.GetDirectoryName(spritePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(spritePath, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(spritePath);
        
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    [MenuItem("Tools/Setup URP 2D Dark Ambient")]
    public static void SetupURPDarkAmbient()
    {
        // 1. Tìm xem có Global Light 2D nào chưa
        System.Type light2DType = GetLight2DType();
        if (light2DType == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Dự án hiện tại chưa sử dụng Universal Render Pipeline (URP) hoặc thiếu package Light2D. Hãy cấu hình URP trước khi sử dụng tính năng này!", "OK");
            return;
        }

        Component[] allLights = (Component[])Object.FindObjectsOfType(light2DType);
        Component globalLight = null;

        foreach (var light in allLights)
        {
            var lightTypeProp = light2DType.GetProperty("lightType");
            if (lightTypeProp != null)
            {
                int typeVal = (int)lightTypeProp.GetValue(light);
                if (typeVal == 1) // 1 = Global
                {
                    globalLight = light;
                    break;
                }
            }
        }

        if (globalLight == null)
        {
            GameObject glObj = new GameObject("Global Light 2D");
            globalLight = glObj.AddComponent(light2DType);
            
            var lightTypeProp = light2DType.GetProperty("lightType");
            if (lightTypeProp != null) lightTypeProp.SetValue(globalLight, 1); // Set to Global
            
            Undo.RegisterCreatedObjectUndo(glObj, "Tạo Global Light 2D");
        }

        // 2. Thiết lập ánh sáng môi trường tối (Màu xanh đen huyền ảo, cường độ 0.15f)
        var colorProp = light2DType.GetProperty("color");
        var intensityProp = light2DType.GetProperty("intensity");
        
        if (colorProp != null) colorProp.SetValue(globalLight, new Color(0.08f, 0.1f, 0.18f)); 
        if (intensityProp != null) intensityProp.SetValue(globalLight, 0.15f);

        // 3. Đổi Material của các Tilemap Renderer sang Sprite-Lit-Default để có thể nhận ánh sáng
        TilemapRenderer[] renderers = Object.FindObjectsOfType<TilemapRenderer>();
        Shader spriteLitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        Material litMaterial = null;
        
        if (spriteLitShader != null)
        {
            litMaterial = new Material(spriteLitShader);
            string matPath = "Assets/_Project/Tilemaps/Sprite-Lit-Default-Shared.mat";
            string dir = Path.GetDirectoryName(matPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(litMaterial, matPath);
        }

        int updatedCount = 0;
        if (litMaterial != null)
        {
            foreach (var r in renderers)
            {
                r.sharedMaterial = litMaterial;
                updatedCount++;
            }
        }

        // Tương tự cho các SpriteRenderer thông thường (như Player, Enemy, Torches)
        SpriteRenderer[] spriteRenderers = Object.FindObjectsOfType<SpriteRenderer>();
        if (litMaterial != null)
        {
            foreach (var sr in spriteRenderers)
            {
                if (sr.sharedMaterial == null || sr.sharedMaterial.name == "Sprites-Default" || sr.sharedMaterial.shader.name == "Sprites/Default")
                {
                    sr.sharedMaterial = litMaterial;
                    updatedCount++;
                }
            }
        }

        EditorUtility.DisplayDialog("Thành công", 
            $"Đã thiết lập không gian ngục tối (Dark Ambient) thành công!\n\n" +
            $"* Đã tạo/cập nhật Global Light 2D về độ sáng 0.15.\n" +
            $"* Đã tự động đổi chất liệu của {updatedCount} đối tượng đồ họa sang Lit Material để có thể nhận và tương tác với ánh sáng từ Đuốc!\n\n" +
            "Hãy chạy game để trải nghiệm hiệu ứng ánh sáng bập bùng tuyệt đẹp!", "Tuyệt vời");
    }

    private static System.Type GetLight2DType()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type type = assembly.GetType("UnityEngine.Rendering.Universal.Light2D");
            if (type != null)
            {
                return type;
            }
        }
        return null;
    }

    [MenuItem("Tools/Create Spike Trap Object")]
    public static void CreateSpikeTrapObject()
    {
        // 1. Tìm sprite env_sprites_86
        string texturePath = "Assets/Pixel Lost Game Scene/Sprites/Sliced/env_sprites.png";
        string spriteName = "env_sprites_86";
        Sprite spikeSprite = LoadSubSprite(texturePath, spriteName);

        if (spikeSprite == null)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy Sprite '{spriteName}' trong sheet '{texturePath}'!\nHãy kiểm tra xem file ảnh có đúng đường dẫn đó không.", "OK");
            return;
        }

        // 2. Tạo GameObject Chông Gai
        GameObject spikeObj = new GameObject("Spike_Trap");
        spikeObj.transform.position = Vector3.zero;

        // 3. Thêm SpriteRenderer và gán Sprite
        SpriteRenderer sr = spikeObj.AddComponent<SpriteRenderer>();
        sr.sprite = spikeSprite;
        sr.sortingOrder = 4; // Vẽ trước Background, ngang hoặc sau Player/Enemy chút

        // 4. Thêm BoxCollider2D và thiết lập là Trigger
        BoxCollider2D boxCollider = spikeObj.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        // Tinh chỉnh kích thước Collider nhỏ hơn một chút cho công bằng về hitbox (không quá rộng)
        boxCollider.size = new Vector2(0.9f, 0.7f);
        boxCollider.offset = new Vector2(0f, -0.1f);

        // 5. Thêm script SpikeTrap
        spikeObj.AddComponent<SpikeTrap>();

        // Đăng ký Undo và chọn đối tượng mới tạo trong Hierarchy
        Undo.RegisterCreatedObjectUndo(spikeObj, "Tạo Spike Trap");
        Selection.activeGameObject = spikeObj;

        EditorUtility.DisplayDialog("Thành công", 
            "Đã tạo thành công đối tượng chông gai Spike_Trap trong Hierarchy!\n\n" +
            "1. Tự động tìm và gán sprite 'env_sprites_86' từ sheet.\n" +
            "2. Đã thêm BoxCollider2D (Trigger) thu gọn hitbox công bằng.\n" +
            "3. Đã gắn script SpikeTrap gây 9999 sát thương (chạm là hẹo).\n\n" +
            "Gợi ý: Bạn có thể di chuyển chông gai đến các hố vực và Duplicate (Ctrl+D) để rải xung quanh map!", "Tuyệt vời");
    }

    private static Sprite LoadSubSprite(string texturePath, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite s && s.name == spriteName)
            {
                return s;
            }
        }
        return null;
    }

    [MenuItem("Tools/Create Undead Executioner Boss")]
    public static void CreateUndeadExecutionerBoss()
    {
        string bossFolder = "Assets/Undead executioner/Undead executioner puppet/png/";
        string[] animFiles = new string[]
        {
            "idle.png",
            "idle2.png",
            "attacking.png",
            "skill1.png",
            "death.png",
            "summon.png",
            "summonAppear.png",
            "summonDeath.png",
            "summonIdle.png"
        };

        // 1. Đồng bộ PPU = 16 và Point Filter cho toàn bộ file ảnh của Boss
        foreach (string file in animFiles)
        {
            string fullPath = bossFolder + file;
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.spritePixelsToUnits != 16)
                {
                    importer.spritePixelsToUnits = 16;
                    changed = true;
                }
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        // 2. Load các Sprite tương ứng
        Sprite[] idle = LoadAllSpritesFromSheet(bossFolder + "idle.png");
        Sprite[] walk = LoadAllSpritesFromSheet(bossFolder + "idle2.png"); // Dùng idle2 làm hoạt ảnh đi tuần tra
        Sprite[] attackA = LoadAllSpritesFromSheet(bossFolder + "attacking.png");
        Sprite[] attackB = LoadAllSpritesFromSheet(bossFolder + "skill1.png"); // Chiêu chém skill
        Sprite[] dead = LoadAllSpritesFromSheet(bossFolder + "death.png");

        if (idle.Length == 0 || dead.Length == 0 || attackA.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy các sprite của Undead Executioner. Vui lòng kiểm tra lại đường dẫn Assets/Undead executioner/Undead executioner puppet/png/", "OK");
            return;
        }

        // 3. Tạo GameObject Boss
        GameObject bossObj = new GameObject("Boss_UndeadExecutioner");
        bossObj.tag = "Enemy";
        bossObj.transform.position = Vector3.zero;

        // 4. Cấu hình SpriteRenderer
        SpriteRenderer sr = bossObj.AddComponent<SpriteRenderer>();
        sr.sprite = idle[0];
        sr.sortingOrder = 4; // Vẽ ngang hàng với quái vật khác

        // Gán Lit Material nếu có
        Material litMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Tilemaps/Sprite-Lit-Default-Shared.mat");
        if (litMaterial != null)
        {
            sr.sharedMaterial = litMaterial;
        }

        // 5. Cấu hình Rigidbody2D
        Rigidbody2D rb = bossObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 6. Cấu hình CapsuleCollider2D (phù hợp với hình dáng boss to cao)
        CapsuleCollider2D col = bossObj.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Vertical;
        // Boss Undead Executioner cao 64 pixel, rộng khoảng 47 pixel ở PPU 16.
        // Quy đổi ra unit: cao 4.0, rộng 2.93. Ta chỉnh size collider nhỏ hơn chút để di chuyển mượt mà.
        col.size = new Vector2(1.5f, 3.2f);
        col.offset = new Vector2(0f, 0f);

        // 7. Cấu hình EnemySpriteAnimator
        EnemySpriteAnimator animator = bossObj.AddComponent<EnemySpriteAnimator>();
        animator.idleSprites = idle;
        animator.walkSprites = walk;
        animator.attackASprites = attackA;
        animator.attackBSprites = attackB;
        animator.deadSprites = dead;
        // Hit và Jump dùng tạm Idle
        animator.hitSprites = new Sprite[] { idle[0], idle[1] };
        animator.jumpSprites = idle;
        animator.fps = 10f;

        // 8. Cấu hình EnemyStats
        EnemyStats stats = bossObj.AddComponent<EnemyStats>();
        // Sát thương & máu của quái thường là 60 HP. Boss máu trâu gấp 5 lần = 300 HP.
        stats.maxHealth = 300;
        stats.baseDamage = 25; // Boss chém đau hơn (25 thay vì 12)
        stats.dropProbability = 0.5f; // Tỉ lệ rớt bình potion cao hơn (50%) khi diệt Boss!

        // 9. Cấu hình EnemyAI
        EnemyAI ai = bossObj.AddComponent<EnemyAI>();
        ai.moveSpeed = 2.2f;      // Tốc độ di chuyển vừa phải
        ai.chaseRange = 12f;      // Phát hiện từ xa
        ai.attackRange = 2.2f;    // Tầm đánh rộng (do có thanh đao Executioner cực lớn)
        ai.attackCooldown = 2.2f; // Cooldown lâu hơn chút nhưng sát thương to
        ai.attackDamage = 25;
        ai.isRanged = false;      // Boss cận chiến chém đao cực uy lực

        // Đăng ký Undo và chọn đối tượng mới tạo trong Hierarchy
        Undo.RegisterCreatedObjectUndo(bossObj, "Tạo Boss Undead Executioner");
        Selection.activeGameObject = bossObj;

        EditorUtility.DisplayDialog("Thành công", 
            "Đã tạo thành công Boss Undead Executioner trong Hierarchy!\n\n" +
            "1. Tự động đồng bộ toàn bộ file sprite sheets về PPU = 16 và Point Filter.\n" +
            "2. Đã tự động cắt và gán tất cả Sprite từ file gốc (Idle, Idle2 làm Walk, Attacking, Skill1, Death).\n" +
            "3. Máu boss được tăng gấp 5 lần (300 HP) và sát thương là 25.\n" +
            "4. Đã gắn Collider dạng Capsule tối ưu hóa cho boss to cao.\n\n" +
            "Gợi ý: Hãy đặt Boss ở khu vực cuối của màn chơi để làm Boss cuối của game!", "Tuyệt vời");
    }

    private static Sprite[] LoadAllSpritesFromSheet(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                sprites.Add(s);
            }
        }
        
        // Sắp xếp các sprite theo thứ tự abc trong tên (ví dụ: idle_0, idle_1...)
        sprites.Sort((a, b) => {
            return ExtractNumber(a.name).CompareTo(ExtractNumber(b.name));
        });
        
        return sprites.ToArray();
    }

    private static int ExtractNumber(string name)
    {
        string numberStr = "";
        for (int i = name.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(name[i]))
            {
                numberStr = name[i] + numberStr;
            }
            else
            {
                if (numberStr.Length > 0) break;
            }
        }
        int result;
        if (int.TryParse(numberStr, out result))
        {
            return result;
        }
        return 0;
    }
}
