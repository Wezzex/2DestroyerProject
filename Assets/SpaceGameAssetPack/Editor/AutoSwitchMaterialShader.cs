/*
 * AutoSwitchMaterialShader.cs
 * 
 * Automatically updates SpeedTree8 materials in a specific folder to match
 * the active render pipeline (Built-in, URP, or HDRP).
 * 
 * - Assigns the correct SpeedTree8 shader.
 * - Enables double-sided rendering settings per pipeline.
 * 
 * It Runs automatically on project load.
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
namespace MaterialSwitchUtils
{
    [InitializeOnLoad]
    public static class AutoSwitchMaterialShader
    {
        private static readonly string targetFolder = "Assets/SpaceGameAssetPack/Models_Materials/Materials"; // <-- CHANGE THIS

        static AutoSwitchMaterialShader()
        {
            EditorApplication.delayCall += FixMaterials;
        }

        static void FixMaterials()
        {
            string shaderName = GetShaderForCurrentPipeline();
            if (string.IsNullOrEmpty(shaderName))
            {
                Debug.LogWarning("SpeedTree material fixer: Unknown pipeline.");
                return;
            }

            string[] matPaths = Directory.GetFiles(targetFolder, "*.mat", SearchOption.AllDirectories);
            int changedCount = 0;

            foreach (string path in matPaths)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || !mat.shader.name.Contains("SpeedTree8"))
                    continue;

                bool changed = false;

                // Set correct shader if needed
                if (mat.shader.name != shaderName)
                {
                    Shader newShader = Shader.Find(shaderName);
                    if (newShader != null)
                    {
                        mat.shader = newShader;
                        changed = true;
                    }
                }

                // Set double-sided settings based on pipeline
                switch (GetRenderPipeline())
                {
                    case "HDRP":
                        if (mat.HasProperty("_DoubleSidedEnable"))
                        {
                            mat.SetFloat("_DoubleSidedEnable", 1f);
                            changed = true;
                        }
                        mat.doubleSidedGI = true;
                        break;

                    case "URP":
                        if (mat.HasProperty("_TwoSidedEnum"))
                        {
                            mat.SetInt("_TwoSidedEnum", 1); // Selects "Yes" from dropdown
                            changed = true;
                        }
                        break;

                    case "Built-in":
                        if (mat.HasProperty("_TwoSidedEnum"))
                        {
                            mat.SetInt("_TwoSidedEnum", 1); // Selects "Yes" from dropdown
                            changed = true;
                        }
                        break;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(mat);
                    changedCount++;
                    Debug.Log($"✅ Fixed material: {mat.name}");
                }
            }

            if (changedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"🎉 Updated {changedCount} SpeedTree materials with correct shader + double-sided settings.");
            }
        }

        static string GetRenderPipeline()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null) return "Built-in";
            var type = pipeline.GetType().ToString();
            if (type.Contains("HDRenderPipelineAsset")) return "HDRP";
            if (type.Contains("UniversalRenderPipelineAsset")) return "URP";
            return "Unknown";
        }

        static string GetShaderForCurrentPipeline()
        {
            switch (GetRenderPipeline())
            {
                case "HDRP": return "HDRP/Nature/SpeedTree8";
                case "URP": return "Universal Render Pipeline/Nature/SpeedTree8";
                case "Built-in": return "Nature/SpeedTree8";
            }
            return null;
        }
    }
}