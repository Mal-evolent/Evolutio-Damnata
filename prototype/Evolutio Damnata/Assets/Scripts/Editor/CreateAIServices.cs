using UnityEngine;
using UnityEditor;
using EnemyInteraction.Services;

public class CreateAIServices : EditorWindow
{
    [MenuItem("Tools/Create AIServices Prefab")]
    public static void CreatePrefab()
    {
        // Create a new GameObject with the AIServices component
        GameObject aiServicesObj = new GameObject("AIServices");
        AIServices aiServices = aiServicesObj.AddComponent<AIServices>();
        
        // Make sure the Resources folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        // Create the prefab in the Resources folder
        string prefabPath = "Assets/Resources/AIServicesPrefab.prefab";
        bool success = false;
        
        #if UNITY_2018_3_OR_NEWER
        PrefabUtility.SaveAsPrefabAsset(aiServicesObj, prefabPath, out success);
        #else
        UnityEngine.Object prefab = PrefabUtility.CreatePrefab(prefabPath, aiServicesObj);
        success = prefab != null;
        #endif
        
        // Destroy the temporary GameObject
        DestroyImmediate(aiServicesObj);
        
        if (success)
        {
            Debug.Log("AIServices prefab created successfully in Resources folder.");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        else
        {
            Debug.LogError("Failed to create AIServices prefab!");
        }
    }
} 