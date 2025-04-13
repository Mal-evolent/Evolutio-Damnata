using UnityEngine;
using UnityEditor;
using EnemyInteraction.Services;

public class CreateAIServices : EditorWindow
{
    [MenuItem("Tools/Create AIServices Asset")]
    public static void CreateAsset()
    {
        // Create the asset
        var asset = ScriptableObject.CreateInstance<AIServices>();
        
        // Make sure the Resources folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        // Create the asset in the Resources folder
        AssetDatabase.CreateAsset(asset, "Assets/Resources/AIServices.asset");
        AssetDatabase.SaveAssets();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
} 