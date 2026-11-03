#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameEngine;
using UnityEditor;

namespace GameEngineEditor
{
    /// <summary>通过 Unity 菜单切换编辑器资源加载模式。</summary>
    public static class AssetProviderModeMenu
    {
        private const string AssetBundleDefine = "GAME_ASSET_AB";
        private const string AssetDatabaseMenu = "GameEngine/Asset/编辑器使用 AssetDatabase";
        private const string AssetBundleMenu = "GameEngine/Asset/编辑器使用 AssetBundle";

        [MenuItem(AssetDatabaseMenu)]
        private static void UseAssetDatabase()
        {
            SetMode(false);
        }

        [MenuItem(AssetBundleMenu)]
        private static void UseAssetBundle()
        {
            SetMode(true);
        }

        [MenuItem(AssetDatabaseMenu, true)]
        private static bool ValidateAssetDatabase()
        {
            Menu.SetChecked(AssetDatabaseMenu, !HasDefine());
            return true;
        }

        [MenuItem(AssetBundleMenu, true)]
        private static bool ValidateAssetBundle()
        {
            Menu.SetChecked(AssetBundleMenu, HasDefine());
            return true;
        }

        private static void SetMode(bool useAssetDatabase)
        {
            List<string> defines = GetDefines();
            defines.RemoveAll(define => string.Equals(define, AssetBundleDefine, StringComparison.Ordinal));
            if (useAssetDatabase)
            {
                defines.Add(AssetBundleDefine);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defines.ToArray()));
            AssetDatabase.Refresh();
            Log.Debug(useAssetDatabase
                ? "[AssetProviderMode] 编辑器资源模式：AssetDatabase。"
                : "[AssetProviderMode] 编辑器资源模式：AssetBundle。");
        }

        private static bool HasDefine()
        {
            return GetDefines().Contains(AssetBundleDefine);
        }

        private static List<string> GetDefines()
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            return new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
#endif
