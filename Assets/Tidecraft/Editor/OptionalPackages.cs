#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
namespace Tidecraft.Editor
{
    // Explicit menu action, so opening the project never waits for an optional registry download.
    public static class OptionalPackages
    {
        static AddRequest request;
        [MenuItem("Tools/Tidecraft/Install Cinemachine camera rig")]
        public static void Install()
        {
            if (request != null) return;
            request = Client.Add("com.unity.cinemachine@3.1.7"); EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (request == null || !request.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success) Debug.Log("[Tidecraft] Cinemachine installed; next Play uses the Cinemachine inspection rig.");
            else Debug.LogError("[Tidecraft] Cinemachine install failed: " + request.Error?.message);
            request = null;
        }
    }
}
#endif
