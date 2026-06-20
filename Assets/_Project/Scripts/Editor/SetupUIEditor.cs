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

        // Đảm bảo có PotionSystem và gán Sprites bình thuốc từ 2D Pixel Item Pack
        PotionSystem potionSys = player.GetComponent<PotionSystem>();
        if (potionSys == null)
        {
            potionSys = player.AddComponent<PotionSystem>();
            Undo.RegisterCreatedObjectUndo(potionSys, "Thêm PotionSystem");
            Debug.Log("[Setup] Đã tự động thêm Component PotionSystem vào Player.");
        }

        Undo.RecordObject(potionSys, "Gán Sprites Potion");
        potionSys.healthPotionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionRed_00.png");
        potionSys.manaPotionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionBlue_00.png");
        potionSys.speedPotionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionGold_00.png");

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

        // Sinh 3 bình thuốc ở vị trí x = +3f, +5f, +7f so với player
        CreateCollectible(playerPos + Vector3.right * 3f + Vector3.up * 0.5f, CollectibleType.HealthPotion, 
            "Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionRed_00.png", "Collectible_HealthPotion");

        CreateCollectible(playerPos + Vector3.right * 5f + Vector3.up * 0.5f, CollectibleType.ManaPotion, 
            "Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionBlue_00.png", "Collectible_ManaPotion");

        CreateCollectible(playerPos + Vector3.right * 7f + Vector3.up * 0.5f, CollectibleType.SpeedPotion, 
            "Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionGold_00.png", "Collectible_SpeedPotion");

        // Đánh dấu Scene thay đổi để lưu
        if (player.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Thành công", 
            "Đã tạo thành công 3 bình thuốc thử nghiệm trên mặt đất (phía bên phải nhân vật):\n\n" +
            "1. Bình Hồi Máu (Red Potion)\n" +
            "2. Bình Hồi Mana (Blue Potion)\n" +
            "3. Bình Tốc Độ (Gold Potion)\n\n" +
            "Hãy nhấn Play và điều khiển nhân vật chạy qua để nhặt thử!", "Tuyệt vời");
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

        // 2. Tạo/Lấy Prefab các bình thuốc
        GameObject healthPotion = GetOrCreatePotionPrefab(CollectibleType.HealthPotion, 
            "Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionRed_00.png", "Collectible_HealthPotion");
        GameObject manaPotion = GetOrCreatePotionPrefab(CollectibleType.ManaPotion, 
            "Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionBlue_00.png", "Collectible_ManaPotion");
        GameObject speedPotion = GetOrCreatePotionPrefab(CollectibleType.SpeedPotion, 
            "Assets/_Project/Sprites/UI/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionGold_00.png", "Collectible_SpeedPotion");

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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(localPath);
        if (prefab != null) return prefab;

        GameObject temp = new GameObject(prefabName);
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (s != null)
        {
            string path = AssetDatabase.GetAssetPath(s.texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.spritePixelsPerUnit != 16f || importer.filterMode != FilterMode.Point))
            {
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
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
        Debug.Log($"[Setup] Đã tạo Prefab Bình Thuốc: {localPath}");
        return saved;
    }

    private static void CreateTag(string tag)
    {
        try
        {
            // Kiểm tra xem tag đã tồn tại trong Project Settings chưa
            string[] existingTags = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (string t in existingTags)
            {
                if (t == tag) return;
            }

            // Dùng API của Unity để thêm Tag an toàn
            UnityEditorInternal.InternalEditorUtility.AddTag(tag);
            Debug.Log($"[Setup] Đã tự động tạo Tag: {tag}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Setup] Không thể tạo Tag '{tag}' tự động: {ex.Message}. Hãy tạo thủ công trong Project Settings nếu cần.");
        }
    }
}
