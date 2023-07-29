#if !NOT_UNITY3D

using System.IO;
using UnityEditor;
using UnityEngine;
using Zenject.Internal;

namespace Zenject.ReflectionBaking
{
    public static class ReflectionBakingMenuItems
    {
        [MenuItem("Assets/Create/Zenject/Reflection Baking Settings", false, 100)]
        public static void CreateReflectionBakingSettings()
        {
            string folderPath = ZenUnityEditorUtil.GetCurrentDirectoryAssetPathFromSelection();

            ZenjectReflectionBakingSettings config = ScriptableObject.CreateInstance<ZenjectReflectionBakingSettings>();

            ZenUnityEditorUtil.SaveScriptableObjectAsset(
                Path.Combine(folderPath, "ZenjectReflectionBakingSettings.asset"), config);
        }
    }
}
#endif
