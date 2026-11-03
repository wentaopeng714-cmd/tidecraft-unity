#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Tidecraft.Editor
{
    public static class WebBuild
    {
        [MenuItem("Tools/Tidecraft/Configure browser preview")]
        public static void Configure()
        {
            var target = NamedBuildTarget.WebGL;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 720; PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            // Static hosting needs no custom Content-Encoding headers with Unity's fallback.
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.wasm2023 = false;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;
            // Optional editor module: do not make Android-only project imports depend on it.
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.WebGL.UserBuildSettings")).FirstOrDefault(t => t != null);
            var property = type?.GetProperty("codeOptimization", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (property != null) property.SetValue(null, Enum.Parse(property.PropertyType, "BuildTimes"));
            AssetDatabase.SaveAssets();
            Debug.Log("[Tidecraft] Browser preview: Gzip fallback, Medium stripping, 128–512 MB heap, browser frame pacing and local browser save.");
        }
        [MenuItem("Tools/Tidecraft/Build browser preview")]
        public static void Build()
        {
            Configure(); Directory.CreateDirectory("Builds/WebGL");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Tidecraft/Scenes/Beach.unity" }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.DetailedBuildReport });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Web build failed: " + result.summary.result);
        }
    }
}
#endif
