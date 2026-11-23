using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TripMate.EditorTools
{
    /// <summary>上方選單「TripMate → 建立第一版場景」：一鍵產生可以直接按 Play 的場景。</summary>
    public static class TripMateMenu
    {
        const string ScenePath = "Assets/TripMate/Scenes/TripMate_Town.unity";

        [MenuItem("TripMate/建立第一版場景（小鎮＋道頓堀）", priority = 0)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.227f, 0.247f, 0.478f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 9, -13);
            camGo.transform.rotation = Quaternion.Euler(48, 0, 0);

            var boot = new GameObject("TripMate (按 Play 會自動搭場景)");
            boot.AddComponent<TripMateBootstrap>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = boot;
            Debug.Log("[TripMate] 已建立場景：" + ScenePath + "，按 Play 開始。");
        }

        [MenuItem("TripMate/開啟第一版場景", priority = 1)]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { CreateScene(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
