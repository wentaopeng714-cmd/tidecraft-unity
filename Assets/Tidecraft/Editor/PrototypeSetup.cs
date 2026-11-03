#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Tidecraft.Editor
{
    [InitializeOnLoad]
    public static class PrototypeSetup
    {
        const string ScenePath = "Assets/Tidecraft/Scenes/Beach.unity";
        const string FontPath = "Assets/Tidecraft/Resources/TidecraftFont.asset";
        static PrototypeSetup() { EditorApplication.delayCall += FirstImport; }
        static void FirstImport()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.isBatchMode || File.Exists(ScenePath)) return;
            if (EditorSceneManager.GetActiveScene().isDirty) return;
            try { Prepare(); } catch (Exception e) { Debug.LogException(e); }
        }
        [MenuItem("Tools/Tidecraft/Prepare playable scene")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before preparing.");
            if (EditorSceneManager.GetActiveScene().isDirty && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/Tidecraft/Scenes");
            AssetDatabase.Refresh();
            // Canonical non-interactive TMP essentials import, before the first text component.
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                var importer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TMPro.TMP_PackageResourceImporter")).FirstOrDefault(t => t != null);
                var method = importer?.GetMethod("ImportResources", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method == null) throw new InvalidOperationException("TMP resource importer not available in this package version.");
                AssetDatabase.importPackageCompleted -= ResourcesImported;
                AssetDatabase.importPackageCompleted += ResourcesImported;
                method.Invoke(null, new object[] { true, false, false });
                Debug.Log("[Tidecraft] Importing TMP resources; scene setup resumes after import completes.");
                return;
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Tidecraft/Resources/NotoSansSC-Subset.ttf");
            if (!source) throw new InvalidOperationException("Noto font missing.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (!font)
            {
                font = TMP_FontAsset.CreateFontAsset(source, 48, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = "Tidecraft Chinese"; AssetDatabase.CreateAsset(font, FontPath);
                foreach (var texture in font.atlasTextures) if (texture && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
                if (font.material && !AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
            }
            PlayerSettings.companyName = "Tidecraft Studio"; PlayerSettings.productName = "沙岸工坊"; PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.tidecraft.sandbox");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.tidecraft.sandbox");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 720; PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.SplashScreen.show = false; PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.colorSpace = ColorSpace.Linear; PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Use the mobile URP asset supplied by this project template on every quality tier.
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (!pipeline) throw new InvalidOperationException("Mobile URP asset missing.");
            pipeline.supportsHDR = true; pipeline.msaaSampleCount = 2; pipeline.shadowDistance = 50;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pipeline; QualitySettings.vSyncCount = 0; }
            QualitySettings.SetQualityLevel(0);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Tidecraft").AddComponent<SandApp>();
            app.sandShader = Shader.Find("Tidecraft/Sand"); app.waterShader = Shader.Find("Tidecraft/Water"); app.interfaceFont = font;
            if (!app.sandShader || !app.waterShader) throw new InvalidOperationException("Custom shaders have not imported.");
            // Runtime-created URP material shaders must also survive player stripping.
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach (var shader in new[] { app.sandShader, app.waterShader, Shader.Find("Universal Render Pipeline/Unlit") })
            {
                bool exists = false; for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
                if (!exists && shader) { int i = shaders.arraySize; shaders.InsertArrayElementAtIndex(i); shaders.GetArrayElementAtIndex(i).objectReferenceValue = shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); Debug.Log("[Tidecraft] Playable scene prepared. Press Play. Android build: Tools/Tidecraft/Build Android APK.");
        }
        [MenuItem("Tools/Tidecraft/Build Android APK")]
        public static void BuildAndroid()
        {
            PrepareIfMissing(); Directory.CreateDirectory("Builds/Android");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/Android/Tidecraft-0.1.apk", target = BuildTarget.Android, options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed: " + result.summary.result);
            Debug.Log("[Tidecraft] Android APK: " + Path.GetFullPath("Builds/Android/Tidecraft-0.1.apk"));
        }
        [MenuItem("Tools/Tidecraft/Export iOS Xcode project")]
        public static void BuildIOS()
        {
            PrepareIfMissing(); Directory.CreateDirectory("Builds/iOS");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/iOS", target = BuildTarget.iOS, options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("iOS export failed: " + result.summary.result);
        }
        static void ResourcesImported(string name)
        {
            if (name != "TMP Essential Resources") return;
            AssetDatabase.importPackageCompleted -= ResourcesImported;
            EditorApplication.delayCall += () => { try { Prepare(); } catch (Exception e) { Debug.LogException(e); } };
        }
        static void PrepareIfMissing()
        {
            if (!File.Exists(ScenePath)) Prepare();
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Scene setup is importing resources. Wait for completion, then build again.");
        }
        [MenuItem("Tools/Tidecraft/Run model checks")]
        public static void RunModelChecks() { Debug.Log(Tidecraft.Verification.ModelChecks.Run()); }
    }
}
#endif
