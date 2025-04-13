#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EnemyInteraction.Services;

public class CreateAIServices : EditorWindow
{
    [MenuItem("Tools/Create AIServices GameObject")]
    public static void CreateGameObject()
    {
        // Create a new GameObject with AIServices in the scene
        GameObject aiServicesObj = new GameObject("AIServices");
        AIServices aiServices = aiServicesObj.AddComponent<AIServices>();
        
        // Select the new GameObject in the hierarchy
        Selection.activeGameObject = aiServicesObj;
        EditorGUIUtility.PingObject(aiServicesObj);
        
        Debug.Log("Created AIServices GameObject in the current scene. It will be made persistent at runtime.");
    }
}
#endif 