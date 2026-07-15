using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

public class ReplacePlayerSprites : EditorWindow
{
    [MenuItem("Tools/Replace Player Sprites")]
    public static void ReplaceSpritesMenu()
    {
        ReplaceSprites(true);
    }

    public static void ReplaceSprites(bool showDialog = false)
    {
        Debug.Log("[ReplacePlayerSprites] Bắt đầu quét và thay thế Sprite của Player...");

        // 1. Refresh AssetDatabase để chắc chắn các Sprite mới đã được import đầy đủ
        AssetDatabase.Refresh();

        // 2. Định nghĩa các đường dẫn tài nguyên
        string assetFolderPath = "Assets/Asset_Player";
        string animFolderPath = "Assets/_Project/Animations";

        if (!Directory.Exists(assetFolderPath))
        {
            if (showDialog) EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy thư mục asset: {assetFolderPath}", "OK");
            return;
        }

        // 3. Tự động chỉnh sửa PPU, FilterMode và Pivot (Bottom-Center) trong file .meta trực tiếp để tránh lỗi Unity API khác biệt
        ConfigureTexturesDirectly(assetFolderPath, 128);

        // 4. Thực hiện ánh xạ các hoạt ảnh
        // Cấu hình: Tên animation clip ➔ Tên file png trong Asset_Player ➔ Frame Rate
        MapAnimation(animFolderPath + "/Player_Idle.anim", assetFolderPath + "/idle.png", 8f);
        MapAnimation(animFolderPath + "/Player_Run.anim", assetFolderPath + "/run.png", 12f);
        MapAnimation(animFolderPath + "/Player_Jump.anim", assetFolderPath + "/jump.png", 10f);
        MapAnimation(animFolderPath + "/Player_Hurt.anim", assetFolderPath + "/hurt.png", 8f);
        MapAnimation(animFolderPath + "/Player_Dead.anim", assetFolderPath + "/dead.png", 8f);
        MapAnimation(animFolderPath + "/Player_Protect.anim", assetFolderPath + "/protect.png", 8f);
        
        // Đòn đánh 1 & 2
        MapAnimation(animFolderPath + "/Player_Attack1.anim", assetFolderPath + "/Attack1.png", 12f);
        MapAnimation(animFolderPath + "/Player_Attack2.anim", assetFolderPath + "/attack2.png", 12f);

        // Đòn đánh 3 & RunAttack (Do thiếu Attack3.png, dùng tạm Attack1.png làm placeholder)
        string attack3Path = assetFolderPath + "/Attack3.png";
        if (File.Exists(attack3Path))
        {
            MapAnimation(animFolderPath + "/Player_Attack3.anim", attack3Path, 12f);
        }
        else
        {
            Debug.LogWarning("[ReplacePlayerSprites] Thiếu Attack3.png, dùng tạm Attack1.png làm đòn đánh 3.");
            MapAnimation(animFolderPath + "/Player_Attack3.anim", assetFolderPath + "/Attack1.png", 12f);
        }
        MapAnimation(animFolderPath + "/Player_RunAttack.anim", assetFolderPath + "/Attack1.png", 12f);

        // 5. Cập nhật Sprite mặc định của Player trong Scene để hiển thị đúng nhân vật mới ngay trong Editor
        UpdatePlayerDefaultSprite(assetFolderPath + "/idle.png");

        // 6. Tự động điều chỉnh kích thước Collider của Player tương ứng với chiều cao 2 ô grid (2.0f units)
        AdjustPlayerCollider();

        // 7. Lưu lại thay đổi
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();

        if (showDialog) EditorUtility.DisplayDialog("Thành công", "Đã cập nhật toàn bộ hoạt ảnh của Player và điều chỉnh tỉ lệ thành công (cao 2 ô Grid)!", "OK");
        Debug.Log("[ReplacePlayerSprites] Hoàn thành thay thế Sprite của Player!");
    }

    private static void ConfigureTexturesDirectly(string folderPath, int targetPPU)
    {
        string[] metaFiles = Directory.GetFiles(folderPath, "*.png.meta");
        bool changedAny = false;

        foreach (string metaFile in metaFiles)
        {
            string[] lines = File.ReadAllLines(metaFile);
            bool inSpriteSheet = false;
            bool changed = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Đổi PPU
                if (line.Contains("spritePixelsToUnits:"))
                {
                    string currentPPUString = line.Substring(line.IndexOf("spritePixelsToUnits:") + "spritePixelsToUnits:".Length).Trim();
                    if (currentPPUString != targetPPU.ToString())
                    {
                        lines[i] = line.Substring(0, line.IndexOf("spritePixelsToUnits:")) + "spritePixelsToUnits: " + targetPPU;
                        changed = true;
                    }
                }

                // Đổi Filter Mode thành Point (1)
                if (line.Contains("filterMode:"))
                {
                    string currentFilter = line.Substring(line.IndexOf("filterMode:") + "filterMode:".Length).Trim();
                    if (currentFilter != "1")
                    {
                        lines[i] = line.Substring(0, line.IndexOf("filterMode:")) + "filterMode: 1";
                        changed = true;
                    }
                }

                // Đánh dấu khi vào vùng cấu hình của spriteSheet
                if (line.Contains("spriteSheet:"))
                {
                    inSpriteSheet = true;
                }

                // Cập nhật alignment & pivot của các slice con sang Bottom-Center (7)
                if (inSpriteSheet)
                {
                    if (line.Contains("alignment:"))
                    {
                        if (line.StartsWith("      alignment:") || line.StartsWith("        alignment:"))
                        {
                            string currentAlignment = line.Substring(line.IndexOf("alignment:") + "alignment:".Length).Trim();
                            if (currentAlignment != "7")
                            {
                                lines[i] = line.Substring(0, line.IndexOf("alignment:")) + "alignment: 7";
                                changed = true;
                            }
                        }
                    }
                    else if (line.Contains("pivot:"))
                    {
                        if (line.StartsWith("      pivot:") || line.StartsWith("        pivot:"))
                        {
                            string currentPivot = line.Substring(line.IndexOf("pivot:") + "pivot:".Length).Trim();
                            if (currentPivot != "{x: 0.5, y: 0}")
                            {
                                lines[i] = line.Substring(0, line.IndexOf("pivot:")) + "pivot: {x: 0.5, y: 0}";
                                changed = true;
                            }
                        }
                    }
                }
            }

            if (changed)
            {
                File.WriteAllLines(metaFile, lines);
                changedAny = true;
                Debug.Log($"[ReplacePlayerSprites] Đã cập nhật file meta của {Path.GetFileName(metaFile)} sang PPU {targetPPU} & Bottom-Center pivots.");
            }
        }

        if (changedAny)
        {
            AssetDatabase.Refresh();
        }
    }

    private static void AdjustPlayerCollider()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            // Reset local scale về (1, 1, 1) để tránh việc nhân vật bị méo khi dùng PPU làm chuẩn
            Undo.RecordObject(player.transform, "Reset Scale Player");
            float currentSignX = Mathf.Sign(player.transform.localScale.x);
            player.transform.localScale = new Vector3(currentSignX * 1f, 1f, 1f);

            // Điều chỉnh CapsuleCollider2D
            CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
            if (capsule == null) capsule = player.GetComponentInChildren<CapsuleCollider2D>();
            if (capsule != null)
            {
                Undo.RecordObject(capsule, "Adjust Player CapsuleCollider2D");
                capsule.size = new Vector2(0.6f, 2.0f); // Rộng 0.6, Cao 2.0 (bằng đúng 2 ô grid)
                capsule.offset = new Vector2(0f, 1.0f); // Tâm collider ở giữa y=1.0f
                EditorUtility.SetDirty(capsule);
                Debug.Log("[ReplacePlayerSprites] Đã điều chỉnh CapsuleCollider2D của Player thành: size=(0.6, 2.0), offset=(0, 1.0)");
            }

            // Điều chỉnh BoxCollider2D (nếu có)
            BoxCollider2D box = player.GetComponent<BoxCollider2D>();
            if (box == null) box = player.GetComponentInChildren<BoxCollider2D>();
            if (box != null)
            {
                Undo.RecordObject(box, "Adjust Player BoxCollider2D");
                box.size = new Vector2(0.6f, 2.0f); // Rộng 0.6, Cao 2.0
                box.offset = new Vector2(0f, 1.0f); // Tâm collider ở giữa y=1.0f
                EditorUtility.SetDirty(box);
                Debug.Log("[ReplacePlayerSprites] Đã điều chỉnh BoxCollider2D của Player thành: size=(0.6, 2.0), offset=(0, 1.0)");
            }
        }
    }

    private static void MapAnimation(string clipPath, string spriteSheetPath, float frameRate)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogError($"[ReplacePlayerSprites] Không tìm thấy AnimationClip tại: {clipPath}");
            return;
        }

        // Load toàn bộ Sprite từ sprite sheet png
        Sprite[] newSprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(s => GetSpriteIndexFromName(s.name))
            .ToArray();

        if (newSprites.Length == 0)
        {
            Debug.LogError($"[ReplacePlayerSprites] Không tìm thấy Sprite nào trong sheet: {spriteSheetPath}");
            return;
        }

        Undo.RecordObject(clip, "Thay thế Sprite Animation");

        // Lấy liên kết thuộc tính Sprite từ clip hiện tại
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        EditorCurveBinding spriteBinding = default;
        bool foundBinding = false;

        foreach (var b in bindings)
        {
            if (b.propertyName == "m_Sprite")
            {
                spriteBinding = b;
                foundBinding = true;
                break;
            }
        }

        if (!foundBinding)
        {
            // Thiết lập mặc định nếu chưa có binding
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";
        }

        // Tạo danh sách keyframe mới dựa trên số lượng ảnh mới và frame rate
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[newSprites.Length];
        float frameInterval = 1f / frameRate;

        for (int i = 0; i < newSprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i * frameInterval;
            keyframes[i].value = newSprites[i];
        }

        // Gán đường cong hoạt ảnh mới
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        // Bảo toàn và căn chỉnh thời gian cho Animation Events (như FinishAttack, FinishHurt) về cuối clip mới
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        if (events != null && events.Length > 0)
        {
            float newEndTime = (newSprites.Length - 1) * frameInterval;
            foreach (var ev in events)
            {
                if (ev.functionName == "FinishAttack" || ev.functionName == "FinishHurt")
                {
                    ev.time = newEndTime;
                    Debug.Log($"[ReplacePlayerSprites] Đã di chuyển sự kiện {ev.functionName} của {clip.name} về thời gian {newEndTime}s.");
                }
            }
            AnimationUtility.SetAnimationEvents(clip, events);
        }

        EditorUtility.SetDirty(clip);
        Debug.Log($"[ReplacePlayerSprites] Đã cập nhật thành công clip: {clip.name} ({newSprites.Length} frames, {frameRate} fps)");
    }

    private static int GetSpriteIndexFromName(string name)
    {
        string[] parts = name.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int idx))
        {
            return idx;
        }
        return 0;
    }

    private static void UpdatePlayerDefaultSprite(string idleSheetPath)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr == null) sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Sprite idleZero = AssetDatabase.LoadAllAssetsAtPath(idleSheetPath)
                    .OfType<Sprite>()
                    .OrderBy(s => GetSpriteIndexFromName(s.name))
                    .FirstOrDefault();

                if (idleZero != null)
                {
                    Undo.RecordObject(sr, "Gán Sprite Player mặc định");
                    sr.sprite = idleZero;
                    EditorUtility.SetDirty(sr);
                    Debug.Log($"[ReplacePlayerSprites] Đã gán thành công Sprite mặc định {idleZero.name} cho Player trong Scene!");
                }
            }
        }
    }
}
