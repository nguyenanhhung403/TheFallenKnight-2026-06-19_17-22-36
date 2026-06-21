using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.IO;

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

    [MenuItem("Tools/Setup Menus (Dark Fantasy)")]
    public static void SetupMenus()
    {
        // ------------------ 1. CONFIG SPRITES ------------------
        string menuBgPath = "Assets/_Project/Sprites/UI/dark_fantasy_menu_bg.png";
        string gameoverBgPath = "Assets/_Project/Sprites/UI/dark_fantasy_gameover_bg.png";
        ConfigureSpriteImporter(menuBgPath);
        ConfigureSpriteImporter(gameoverBgPath);

        // Lưu scene hiện tại
        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(originalScenePath))
        {
            originalScenePath = "Assets/Scenes/SampleScene.unity";
        }
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // ------------------ 2. SETUP GAMEPLAY SCENE UI (PAUSE & GAME OVER) ------------------
        // Đảm bảo đang mở Gameplay Scene
        if (EditorSceneManager.GetActiveScene().path != originalScenePath)
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Canvas mới");
        }

        Transform canvasTransform = canvas.transform;

        // --- SETUP PAUSE MENU ---
        Transform oldPause = canvasTransform.Find("PauseMenuPanel");
        if (oldPause != null) Undo.DestroyObjectImmediate(oldPause.gameObject);

        GameObject pausePanelObj = new GameObject("PauseMenuPanel");
        pausePanelObj.transform.SetParent(canvasTransform, false);
        RectTransform pauseRect = pausePanelObj.AddComponent<RectTransform>();
        pauseRect.sizeDelta = new Vector2(600, 500);
        pauseRect.anchoredPosition = Vector2.zero;

        Image pauseImg = pausePanelObj.AddComponent<Image>();
        pauseImg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f); // Màu tối âm u
        
        Outline pauseOutline = pausePanelObj.AddComponent<Outline>();
        pauseOutline.effectColor = new Color(0.5f, 0.1f, 0.1f, 0.7f); // Viền đỏ máu
        pauseOutline.effectDistance = new Vector2(3, -3);

        PauseMenuController pauseCtrl = canvas.gameObject.GetComponent<PauseMenuController>();
        if (pauseCtrl == null) pauseCtrl = canvas.gameObject.AddComponent<PauseMenuController>();
        pauseCtrl.pausePanel = pausePanelObj;

        // Title Pause
        GameObject pauseTitleObj = new GameObject("Title");
        pauseTitleObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform pauseTitleRect = pauseTitleObj.AddComponent<RectTransform>();
        pauseTitleRect.sizeDelta = new Vector2(500, 80);
        pauseTitleRect.anchoredPosition = new Vector2(0, 160);
        Text pauseTitleText = pauseTitleObj.AddComponent<Text>();
        pauseTitleText.text = "GAME PAUSED";
        pauseTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pauseTitleText.fontSize = 48;
        pauseTitleText.fontStyle = FontStyle.Bold;
        pauseTitleText.alignment = TextAnchor.MiddleCenter;
        pauseTitleText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
        Shadow pauseTitleShadow = pauseTitleObj.AddComponent<Shadow>();
        pauseTitleShadow.effectColor = Color.black;
        pauseTitleShadow.effectDistance = new Vector2(3, -3);

        // Pause Buttons
        Color btnNormalColor = new Color(0.18f, 0.05f, 0.05f, 1f);
        Color btnHoverColor = new Color(0.35f, 0.08f, 0.08f, 1f);

        GameObject resumeBtnObj = CreateMenuButton(pausePanelObj.transform, "ResumeButton", "CONTINUE", new Vector2(0, 40), new Vector2(320, 60), btnNormalColor, btnHoverColor, 20);
        GameObject mainMenuBtnObj = CreateMenuButton(pausePanelObj.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -40), new Vector2(320, 60), btnNormalColor, btnHoverColor, 20);
        GameObject quitBtnObj = CreateMenuButton(pausePanelObj.transform, "QuitButton", "QUIT GAME", new Vector2(0, -120), new Vector2(320, 60), btnNormalColor, btnHoverColor, 20);

        UnityEventTools.AddVoidPersistentListener(resumeBtnObj.GetComponent<Button>().onClick, pauseCtrl.ResumeGame);
        UnityEventTools.AddVoidPersistentListener(mainMenuBtnObj.GetComponent<Button>().onClick, pauseCtrl.LoadMainMenu);
        UnityEventTools.AddVoidPersistentListener(quitBtnObj.GetComponent<Button>().onClick, pauseCtrl.QuitGame);

        // --- SETUP GAME OVER MENU ---
        Transform oldGameOver = canvasTransform.Find("GameOverPanel");
        if (oldGameOver != null) Undo.DestroyObjectImmediate(oldGameOver.gameObject);

        GameObject goPanelObj = new GameObject("GameOverPanel");
        goPanelObj.transform.SetParent(canvasTransform, false);
        RectTransform goRect = goPanelObj.AddComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.sizeDelta = Vector2.zero;
        goRect.anchoredPosition = Vector2.zero;

        Image goImg = goPanelObj.AddComponent<Image>();
        Sprite gameoverBg = AssetDatabase.LoadAssetAtPath<Sprite>(gameoverBgPath);
        if (gameoverBg != null)
        {
            goImg.sprite = gameoverBg;
            goImg.color = Color.white;
        }
        else
        {
            goImg.color = new Color(0.1f, 0.02f, 0.02f, 0.95f);
        }

        GameOverController goCtrl = canvas.gameObject.GetComponent<GameOverController>();
        if (goCtrl == null) goCtrl = canvas.gameObject.AddComponent<GameOverController>();
        goCtrl.gameOverPanel = goPanelObj;

        // Title Game Over
        GameObject goTitleObj = new GameObject("Title");
        goTitleObj.transform.SetParent(goPanelObj.transform, false);
        RectTransform goTitleRect = goTitleObj.AddComponent<RectTransform>();
        goTitleRect.sizeDelta = new Vector2(800, 150);
        goTitleRect.anchoredPosition = new Vector2(0, 180);
        Text goTitleText = goTitleObj.AddComponent<Text>();
        goTitleText.text = "YOU DIED";
        goTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        goTitleText.fontSize = 90;
        goTitleText.fontStyle = FontStyle.Bold;
        goTitleText.alignment = TextAnchor.MiddleCenter;
        goTitleText.color = new Color(0.75f, 0.05f, 0.05f, 1f);
        Shadow goTitleShadow = goTitleObj.AddComponent<Shadow>();
        goTitleShadow.effectColor = Color.black;
        goTitleShadow.effectDistance = new Vector2(4, -4);

        // Game Over Buttons
        Color goBtnNormal = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        Color goBtnHover = new Color(0.45f, 0.05f, 0.05f, 1f);

        GameObject restartBtnObj = CreateMenuButton(goPanelObj.transform, "RestartButton", "TRY AGAIN", new Vector2(0, -60), new Vector2(350, 70), goBtnNormal, goBtnHover, 24);
        GameObject goMainMenuBtnObj = CreateMenuButton(goPanelObj.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -150), new Vector2(350, 70), goBtnNormal, goBtnHover, 24);

        UnityEventTools.AddVoidPersistentListener(restartBtnObj.GetComponent<Button>().onClick, goCtrl.RestartGame);
        UnityEventTools.AddVoidPersistentListener(goMainMenuBtnObj.GetComponent<Button>().onClick, goCtrl.LoadMainMenu);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

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
            GameObject mcObj = new GameObject("Canvas");
            menuCanvas = mcObj.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler ms = mcObj.AddComponent<CanvasScaler>();
            ms.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            ms.referenceResolution = new Vector2(1920, 1080);
            ms.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            ms.matchWidthOrHeight = 0.5f;
            mcObj.AddComponent<GraphicRaycaster>();
        }

        // Background Main Menu
        GameObject menuBgObj = new GameObject("Background");
        menuBgObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform menuBgRect = menuBgObj.AddComponent<RectTransform>();
        menuBgRect.anchorMin = Vector2.zero;
        menuBgRect.anchorMax = Vector2.one;
        menuBgRect.sizeDelta = Vector2.zero;
        menuBgRect.anchoredPosition = Vector2.zero;
        Image menuBgImg = menuBgObj.AddComponent<Image>();
        Sprite menuBg = AssetDatabase.LoadAssetAtPath<Sprite>(menuBgPath);
        if (menuBg != null)
        {
            menuBgImg.sprite = menuBg;
            menuBgImg.color = Color.white;
        }
        else
        {
            menuBgImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        }

        // Main Menu Controller Manager
        GameObject menuManager = new GameObject("MainMenuManager");
        MainMenuController menuCtrl = menuManager.AddComponent<MainMenuController>();

        // Title
        GameObject menuTitleObj = new GameObject("Title");
        menuTitleObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform menuTitleRect = menuTitleObj.AddComponent<RectTransform>();
        menuTitleRect.sizeDelta = new Vector2(1200, 120);
        menuTitleRect.anchoredPosition = new Vector2(0, 240);
        Text menuTitleText = menuTitleObj.AddComponent<Text>();
        menuTitleText.text = "THE FALLEN KNIGHT";
        menuTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        menuTitleText.fontSize = 80;
        menuTitleText.fontStyle = FontStyle.Bold;
        menuTitleText.alignment = TextAnchor.MiddleCenter;
        menuTitleText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        Shadow menuTitleShadow = menuTitleObj.AddComponent<Shadow>();
        menuTitleShadow.effectColor = Color.black;
        menuTitleShadow.effectDistance = new Vector2(4, -4);

        // Subtitle
        GameObject menuSubObj = new GameObject("Subtitle");
        menuSubObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform menuSubRect = menuSubObj.AddComponent<RectTransform>();
        menuSubRect.sizeDelta = new Vector2(800, 50);
        menuSubRect.anchoredPosition = new Vector2(0, 160);
        Text menuSubText = menuSubObj.AddComponent<Text>();
        menuSubText.text = "Dark Fantasy Chronicles";
        menuSubText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        menuSubText.fontSize = 26;
        menuSubText.fontStyle = FontStyle.Italic;
        menuSubText.alignment = TextAnchor.MiddleCenter;
        menuSubText.color = new Color(0.6f, 0.05f, 0.05f, 1f);
        Shadow menuSubShadow = menuSubObj.AddComponent<Shadow>();
        menuSubShadow.effectColor = Color.black;
        menuSubShadow.effectDistance = new Vector2(2, -2);

        // Menu Buttons
        GameObject playBtnObj = CreateMenuButton(menuCanvas.transform, "PlayButton", "PLAY GAME", new Vector2(0, -40), new Vector2(360, 75), goBtnNormal, goBtnHover, 24);
        GameObject quitGameBtnObj = CreateMenuButton(menuCanvas.transform, "QuitButton", "QUIT GAME", new Vector2(0, -140), new Vector2(360, 75), goBtnNormal, goBtnHover, 24);

        UnityEventTools.AddVoidPersistentListener(playBtnObj.GetComponent<Button>().onClick, menuCtrl.PlayGame);
        UnityEventTools.AddVoidPersistentListener(quitGameBtnObj.GetComponent<Button>().onClick, menuCtrl.QuitGame);

        // --- CREDITS PANEL (Góc phải màn hình Main Menu) ---
        GameObject creditsPanelObj = new GameObject("CreditsPanel");
        creditsPanelObj.transform.SetParent(menuCanvas.transform, false);
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
        membersText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        membersText.fontSize = 15;
        membersText.fontStyle = FontStyle.Bold;
        membersText.alignment = TextAnchor.UpperLeft;
        membersText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        membersText.lineSpacing = 1.3f;

        Shadow membersShadow = membersObj.AddComponent<Shadow>();
        membersShadow.effectColor = Color.black;
        membersShadow.effectDistance = new Vector2(2, -2);

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
        buildScenes.Add(new EditorBuildSettingsScene(targetMenuScenePath, true));
        buildScenes.Add(new EditorBuildSettingsScene(originalScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        // ------------------ 5. RETURN TO ORIGINAL GAMEPLAY SCENE ------------------
        EditorSceneManager.OpenScene(originalScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công", 
            "Đã khởi tạo hệ thống Giao Diện Dark Fantasy thành công:\n\n" +
            "1. Cấu hình 2 ảnh nền Gothic tuyệt đẹp (Menu Background & Game Over Background).\n" +
            "2. Tạo màn hình Tạm dừng (PauseMenuPanel) & Màn hình báo tử (GameOverPanel) trong Gameplay Scene.\n" +
            "3. Tạo cảnh chứa Menu chính (MainMenuScene.unity) đầy đủ hiệu ứng âm u huyền bí.\n" +
            "4. Đã tự động cấu hình Build Settings để chạy liên kết giữa các cảnh chơi!\n\n" +
            "Nhấn Play trong Unity để cảm nhận thành quả!", "Tuyệt vời");
    }

    private static GameObject CreateMenuButton(Transform parent, string name, string textStr, Vector2 pos, Vector2 size, Color normalColor, Color highlightedColor, int fontSize)
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
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        
        System.Collections.Generic.Dictionary<string, Sprite> spriteDict = new System.Collections.Generic.Dictionary<string, Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                spriteDict[sprite.name] = sprite;
            }
        }

        if (spriteDict.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy sprite nào được cắt trong main_lev_build.png!", "OK");
            return;
        }

        // 3. Cấu hình các sprite mặc định
        if (spriteDict.TryGetValue("main_lev_build_2", out Sprite defaultSprite))
        {
            ruleTile.m_DefaultSprite = defaultSprite;
        }
        else if (spriteDict.TryGetValue("main_lev_build_0", out defaultSprite))
        {
            ruleTile.m_DefaultSprite = defaultSprite;
        }

        // 4. Định nghĩa danh sách các quy tắc (Tiling Rules)
        ruleTile.m_TilingRules = new System.Collections.Generic.List<RuleTile.TilingRule>();

        // Thiết lập các sprite tương ứng cho quy tắc 9-slice
        // Vị trí m_Neighbors trong RuleTile (8 neighbors):
        // 0: NW, 1: N, 2: NE, 3: W, 4: E, 5: SW, 6: S, 7: SE
        // Giá trị: DontCare = 0, This = 1, NotThis = 2

        void AddRule(string spriteName, int[] neighbors)
        {
            if (spriteDict.TryGetValue(spriteName, out Sprite sprite))
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

        // Dưới đây là bộ quy tắc chuẩn cho 9-slice tilemap (This = 1, NotThis = 2, DontCare = 0)
        // 1. Top-Left Corner (viền góc trên-trái)
        AddRule("main_lev_build_0", new int[] {
            2, 2, 0,
            2,    1,
            0, 1, 1
        });

        // 2. Top-Center (mặt đất ở giữa)
        AddRule("main_lev_build_2", new int[] {
            2, 2, 2,
            1,    1,
            1, 1, 1
        });

        // 3. Top-Right Corner (viền góc trên-phải)
        AddRule("main_lev_build_4", new int[] {
            0, 2, 2,
            1,    2,
            1, 1, 0
        });

        // 4. Left Wall (viền vách đá bên trái)
        AddRule("main_lev_build_12", new int[] {
            2, 1, 1,
            2,    1,
            2, 1, 1
        });

        // 5. Center Dirt (lòng đất bên dưới)
        AddRule("main_lev_build_15", new int[] {
            1, 1, 1,
            1,    1,
            1, 1, 1
        });

        // 6. Right Wall (viền vách đá bên phải)
        AddRule("main_lev_build_14", new int[] {
            1, 1, 2,
            1,    2,
            1, 1, 2
        });

        // 7. Bottom-Left Corner (góc dưới-trái)
        AddRule("main_lev_build_24", new int[] {
            0, 1, 1,
            2,    1,
            2, 2, 0
        });

        // 8. Bottom-Center (lớp đất đáy)
        AddRule("main_lev_build_25", new int[] {
            1, 1, 1,
            1,    1,
            2, 2, 2
        });

        // 9. Bottom-Right Corner (góc dưới-phải)
        AddRule("main_lev_build_26", new int[] {
            1, 1, 0,
            1,    2,
            0, 2, 2
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
            $"Đã tạo thành công Ground_RuleTile tại:\n{savePath}\n\n" +
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
}
