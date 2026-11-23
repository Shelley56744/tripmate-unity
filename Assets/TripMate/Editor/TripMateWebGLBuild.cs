using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TripMate.EditorTools
{
    /// <summary>
    /// 上方選單「TripMate → 輸出 WebGL 網頁版」：輸出可以放到 GitHub Pages 的 WebGL 版本。
    /// 指令列：unity build . --target WebGL --execute-method TripMate.EditorTools.TripMateWebGLBuild.BuildCI --output-path Builds/WebGL
    /// </summary>
    public static class TripMateWebGLBuild
    {
        const string ScenePath = "Assets/TripMate/Scenes/TripMate_Town.unity";
        const string ShaderRefDir = "Assets/TripMate/Resources/ShaderRefs";
        const string DefaultOutput = "Builds/WebGL";
        const string WebTitle = "TripMate 3D｜去趣大阪旅程";

        [MenuItem("TripMate/輸出 WebGL 網頁版", priority = 20)]
        public static void BuildFromMenu()
        {
            var report = Build(DefaultOutput);
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(DefaultOutput);
        }

        /// <summary>給 unity build --execute-method 用。輸出位置讀 -buildOutput 參數。</summary>
        public static void BuildCI()
        {
            string output = DefaultOutput;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildOutput") output = args[i + 1];

            int code = 1;
            try
            {
                var report = Build(output);
                code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static BuildReport Build(string output)
        {
            EnsureShaderRefs();
            KeepLinearFog();

            // 網頁版設定：gzip 壓縮＋瀏覽器端解壓（GitHub Pages 不會幫 .gz 加 Content-Encoding，要靠這個才能載入）
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            // 網頁標題只在輸出時換掉，輸出後還原，不影響編輯器裡的存檔（PlayerPrefs 依產品名稱存放）
            string oldName = PlayerSettings.productName;
            PlayerSettings.productName = WebTitle;
            try
            {
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(opts);
                Debug.Log($"[TripMate] WebGL 輸出結果：{report.summary.result}，大小 {report.summary.totalSize / 1048576f:0.0} MB，位置 {output}");
                return report;
            }
            finally
            {
                PlayerSettings.productName = oldName;
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// 遊戲的材質都是執行時用 Standard shader 產生的，輸出時 Unity 看不到就會把 shader 拿掉（畫面變粉紅或全黑）。
        /// 在 Resources 放兩個參考材質（一般、自發光），確保需要的 shader 變體會被打包。
        /// </summary>
        static void EnsureShaderRefs()
        {
            if (!AssetDatabase.IsValidFolder(ShaderRefDir))
                AssetDatabase.CreateFolder("Assets/TripMate/Resources", "ShaderRefs");
            MakeRef("TM_StandardRef", false);
            MakeRef("TM_StandardEmissiveRef", true);
            AssetDatabase.SaveAssets();
        }

        static void MakeRef(string name, bool emissive)
        {
            string path = $"{ShaderRefDir}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var m = new Material(Shader.Find("Standard")) { name = name };
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", Color.white);
            }
            AssetDatabase.CreateAsset(m, path);
        }

        /// <summary>場景檔沒開霧，但遊戲執行時會開線性霧；告訴 Unity 輸出時保留線性霧的 shader 變體。</summary>
        static void KeepLinearFog()
        {
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs == null || gs.Length == 0) return;
            var so = new SerializedObject(gs[0]);
            var strip = so.FindProperty("m_FogStripping");
            var keepLinear = so.FindProperty("m_FogKeepLinear");
            if (strip == null || keepLinear == null) return;
            strip.intValue = 1; // Custom
            keepLinear.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
