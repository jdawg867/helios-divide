using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HeliosDivide.Editor
{
    [InitializeOnLoad]
    static class HeliosDividePlayModeBootstrap
    {
        const string FoundationScenePath = "Assets/HeliosDivide/World/FoundationTest.unity";

        static HeliosDividePlayModeBootstrap()
        {
            EditorApplication.delayCall += ApplyPlayModeStartScene;
        }

        static void ApplyPlayModeStartScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(FoundationScenePath);
            if (scene == null)
            {
                return;
            }

            if (EditorSceneManager.playModeStartScene != scene)
            {
                EditorSceneManager.playModeStartScene = scene;
            }
        }
    }
}
