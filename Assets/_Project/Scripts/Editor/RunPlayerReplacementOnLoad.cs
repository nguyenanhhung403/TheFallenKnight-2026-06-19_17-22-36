using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class RunPlayerReplacementOnLoad
{
    static RunPlayerReplacementOnLoad()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        ReplacePlayerSprites.ReplaceSprites(false);
        
        string selfPath = "Assets/_Project/Scripts/Editor/RunPlayerReplacementOnLoad.cs";
        if (AssetDatabase.LoadAssetAtPath<MonoScript>(selfPath) != null)
        {
            AssetDatabase.DeleteAsset(selfPath);
            Debug.Log("[ReplacePlayerSprites] Đã tự động dọn dẹp file khởi chạy một lần!");
        }
    }
}
