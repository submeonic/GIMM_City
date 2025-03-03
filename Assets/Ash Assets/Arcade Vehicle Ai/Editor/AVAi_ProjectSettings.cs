using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace ArcadeVP
{
    public class AVAi_ProjectSettings : EditorWindow
    {
        private const string importKey = "AVAi_ProjectSettingsImported"; // Key to track if settings have been imported

        // This method listens for asset imports
        public class ImportAssetPrompt : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                // Check if the asset folder has been imported and settings haven't been applied yet
                if (!EditorPrefs.GetBool(importKey, false))
                {
                    foreach (string assetPath in importedAssets)
                    {
                        if (assetPath.Contains("Assets/Ash Assets/Arcade Vehicle Ai")) // Adjust to match your asset's folder
                        {
                            ShowWindow();
                            break;
                        }
                    }
                }
            }
        }

        // Shows the custom prompt window
        public static void ShowWindow()
        {
            AVAi_ProjectSettings window = GetWindow<AVAi_ProjectSettings>("Import Project Settings");
            window.minSize = new Vector2(300, 150);
        }

        //[MenuItem("Tools/AVAi_Import_Project_Settings")]
        //static void OpenWindow()
        //{
        //    AVAi_ProjectSettings window = GetWindow<AVAi_ProjectSettings>("Import Project Settings");
        //    window.minSize = new Vector2(300, 150);
        //}

        private void OnGUI()
        {
            GUILayout.Label("Import Project Settings", EditorStyles.boldLabel);
            GUILayout.Label("Would you like to import the project settings required for this asset?", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            if (GUILayout.Button("Yes, Import Settings"))
            {
                ImportProjectSettings();
                EditorPrefs.SetBool(importKey, true); // Mark as imported
                Close();
            }

            if (GUILayout.Button("No, Skip"))
            {
                EditorPrefs.SetBool(importKey, true); // Mark as imported, even if skipped
                Close();
            }
        }

        private static void ImportProjectSettings()
        {
            ApplyPreset("Assets/Ash Assets/Arcade Vehicle Ai/Project Settings presets/Physics Settings Preset 1.preset", "ProjectSettings/DynamicsManager.asset");
            ApplyPreset("Assets/Ash Assets/Arcade Vehicle Ai/Project Settings presets/Tag and Layers Preset 1.preset", "ProjectSettings/TagManager.asset");

            Debug.Log("Project settings have been successfully imported.");
        }

        private static void ApplyPreset(string presetPath, string settingsPath)
        {
            // Load preset asset
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            if (preset == null)
            {
                Debug.LogWarning($"Preset not found at path: {presetPath}");
                return;
            }

            // Load settings asset
            var settingsAsset = AssetDatabase.LoadAssetAtPath<Object>(settingsPath);
            if (settingsAsset == null)
            {
                Debug.LogWarning($"Settings not found at path: {settingsPath}");
                return;
            }

            // Apply preset
            if (preset.ApplyTo(settingsAsset))
            {
                Debug.Log($"Preset applied successfully to {settingsPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to apply preset to {settingsPath}");
            }
        }
    }
}
