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

        // 3. Tự động chỉnh sửa PPU và Pivot (Bottom-Center) trong file .meta trực tiếp với các PPU riêng biệt để đồng bộ tỉ lệ
        ConfigureTexturesDirectly(assetFolderPath);

        // 4. Đảm bảo cấu trúc Trigger Attack3 và State Player_Attack3 tồn tại trong Animator Controller
        EnsureAttack3InAnimator();

        // 5. Thực hiện ánh xạ các hoạt ảnh
        // Cấu hình: Tên animation clip ➔ Tên file png trong Asset_Player ➔ Frame Rate
        MapAnimation(animFolderPath + "/Player_Idle.anim", assetFolderPath + "/idle.png", 8f);
        MapAnimation(animFolderPath + "/Player_Run.anim", assetFolderPath + "/run.png", 12f);
        MapAnimation(animFolderPath + "/Player_Jump.anim", assetFolderPath + "/jump.png", 10f);
        MapAnimation(animFolderPath + "/Player_Hurt.anim", assetFolderPath + "/hurt.png", 8f);
        MapAnimation(animFolderPath + "/Player_Dead.anim", assetFolderPath + "/dead.png", 8f);
        
        // Đổi từ protect.png sang defend.png (kích thước chuẩn) để đỡ đòn không bị phình to
        MapAnimation(animFolderPath + "/Player_Protect.anim", assetFolderPath + "/defend.png", 8f);
        
        // Các đòn đánh
        MapAnimation(animFolderPath + "/Player_Attack1.anim", assetFolderPath + "/Attack1.png", 12f);
        MapAnimation(animFolderPath + "/Player_Attack2.anim", assetFolderPath + "/attack2.png", 12f);
        MapAnimation(animFolderPath + "/Player_Attack3.anim", assetFolderPath + "/Attack3.png", 12f);
        MapAnimation(animFolderPath + "/Player_RunAttack.anim", assetFolderPath + "/Attack1.png", 12f);

        // 6. Cập nhật Sprite mặc định của Player trong Scene để hiển thị đúng nhân vật mới ngay trong Editor
        UpdatePlayerDefaultSprite(assetFolderPath + "/idle.png");

        // 7. Tự động điều chỉnh kích thước Collider của Player tương ứng với chiều cao 2 ô grid (2.0f units)
        AdjustPlayerCollider();

        // 8. Lưu lại thay đổi
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();

        if (showDialog) EditorUtility.DisplayDialog("Thành công", "Đã cập nhật toàn bộ hoạt ảnh của Player, đồng bộ kích thước và sửa lỗi đỡ đòn thành công!", "OK");
        Debug.Log("[ReplacePlayerSprites] Hoàn thành thay thế Sprite của Player!");
    }

    private static void ConfigureTexturesDirectly(string folderPath)
    {
        string[] metaFiles = Directory.GetFiles(folderPath, "*.png.meta");
        bool changedAny = false;

        foreach (string metaFile in metaFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(metaFile)).ToLower();
            
            // Đồng bộ PPU tùy chỉnh để tất cả hành động cao tương đương nhau (khoảng 2.0 Unity units)
            int targetPPU = 128; // Mặc định cho idle, walk, defend, hurt, dead
            if (fileName == "run" || fileName == "jump")
            {
                targetPPU = 90; // Phình to chạy/nhảy vì sprite vẽ ngắn hơn
            }
            else if (fileName == "attack1")
            {
                targetPPU = 115;
            }
            else if (fileName == "attack3")
            {
                targetPPU = 65; // Phình to Attack3 vì sprite vẽ siêu ngắn (130px)
            }

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

    private static void EnsureAttack3InAnimator()
    {
        string controllerPath = "Assets/_Project/Animations/PlayerAnimController.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[ReplacePlayerSprites] Không tìm thấy AnimatorController tại: {controllerPath}");
            return;
        }

        // 1. Thêm Parameter Trigger "Attack3" nếu chưa có
        bool hasParam = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "Attack3")
            {
                hasParam = true;
                break;
            }
        }
        if (!hasParam)
        {
            controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);
            Debug.Log("[ReplacePlayerSprites] Đã thêm Parameter Trigger 'Attack3' vào Animator Controller.");
        }

        // 2. Tìm hoặc tạo State "Player_Attack3" trong layer đầu tiên
        var stateMachine = controller.layers[0].stateMachine;
        var states = stateMachine.states;
        UnityEditor.Animations.AnimatorState attack3State = null;
        
        foreach (var s in states)
        {
            if (s.state.name == "Player_Attack3")
            {
                attack3State = s.state;
                break;
            }
        }

        AnimationClip attack3Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Player_Attack3.anim");

        if (attack3State == null)
        {
            attack3State = stateMachine.AddState("Player_Attack3");
            attack3State.motion = attack3Clip;
            attack3State.speed = 1f;
            Debug.Log("[ReplacePlayerSprites] Đã tạo State 'Player_Attack3' thành công.");

            // Thêm transition từ Any State -> Player_Attack3
            var anyTransition = stateMachine.AddAnyStateTransition(attack3State);
            anyTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Attack3");
            anyTransition.duration = 0f;
            anyTransition.hasExitTime = false;
            Debug.Log("[ReplacePlayerSprites] Đã thêm transition từ Any State tới Player_Attack3.");

            // Thêm transition từ Player_Attack3 -> Default State (Idle)
            var exitTransition = attack3State.AddTransition(stateMachine.defaultState);
            exitTransition.hasExitTime = true;
            exitTransition.exitTime = 1f;
            exitTransition.duration = 0.1f;
            Debug.Log("[ReplacePlayerSprites] Đã thêm transition trả về Default State từ Player_Attack3.");
        }
        else
        {
            // Cập nhật motion clip nếu cần
            attack3State.motion = attack3Clip;
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
        float newEndTime = (newSprites.Length - 1) * frameInterval;
        bool hasResetEvent = false;
        string targetEventName = clip.name.Contains("Hurt") ? "FinishHurt" : "FinishAttack";
        bool shouldHaveEvent = clip.name.Contains("Attack") || clip.name.Contains("Hurt");

        if (shouldHaveEvent)
        {
            if (events != null && events.Length > 0)
            {
                foreach (var ev in events)
                {
                    if (ev.functionName == targetEventName)
                    {
                        ev.time = newEndTime;
                        hasResetEvent = true;
                    }
                }
            }

            if (!hasResetEvent)
            {
                AnimationEvent newEv = new AnimationEvent();
                newEv.time = newEndTime;
                newEv.functionName = targetEventName;

                System.Collections.Generic.List<AnimationEvent> eventList = (events != null) ? events.ToList() : new System.Collections.Generic.List<AnimationEvent>();
                eventList.Add(newEv);
                events = eventList.ToArray();
                Debug.Log($"[ReplacePlayerSprites] Đã thêm mới sự kiện {targetEventName} cho {clip.name} tại {newEndTime}s.");
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
