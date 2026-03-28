using UnityEngine;
using UnityEditor;
using System.IO;

namespace MuOnline.Editor
{
    /// <summary>
    /// Configura automáticamente todos los archivos de imagen en
    /// Assets/_MuOnline/Resources/MuAssets como Sprites UI.
    /// Ejecutar una vez: menú MU Pegaso > Import MU Assets.
    /// </summary>
    public static class MuAssetImporter
    {
        private const string MU_ASSETS_PATH = "Assets/_MuOnline/Resources/MuAssets";

        [MenuItem("MU Pegaso/Import MU Assets (Configurar Sprites)")]
        public static void ImportAllMuAssets()
        {
            var files = Directory.GetFiles(
                Path.Combine(Application.dataPath, "../", MU_ASSETS_PATH),
                "*.*", SearchOption.AllDirectories);

            int processed = 0;
            int skipped   = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext != ".tga" && ext != ".jpg" && ext != ".png") { skipped++; continue; }

                    // Convertir path absoluto a path relativo de Unity
                    var relativePath = "Assets" + file
                        .Replace(Application.dataPath, "")
                        .Replace("\\", "/");

                    var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                    if (importer == null) { skipped++; continue; }

                    bool changed = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    // Filtro bilineal para los assets pixelados de MU
                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        changed = true;
                    }

                    // Sin compresión con pérdida para TGA (evita artefactos en bordes)
                    var platformSettings = importer.GetDefaultPlatformTextureSettings();
                    if (ext == ".tga" && platformSettings.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SetPlatformTextureSettings(platformSettings);
                        changed = true;
                    }

                    // Preservar alpha para TGA (muchos tienen transparencia)
                    if (ext == ".tga" && !importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }

                    // Tamaño máximo 1024 para UI (ahorra memoria en móvil)
                    if (importer.maxTextureSize > 1024)
                    {
                        importer.maxTextureSize = 1024;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        processed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("MU PEGASO",
                $"Import completado.\n\n" +
                $"Configurados: {processed} assets\n" +
                $"Sin cambios: {skipped}",
                "OK");

            Debug.Log($"[MuAssetImporter] {processed} assets configurados como Sprites.");
        }

        [MenuItem("MU Pegaso/Log Asset List (Listar Assets)")]
        public static void LogAssetList()
        {
            var files = Directory.GetFiles(
                Path.Combine(Application.dataPath, "../", MU_ASSETS_PATH),
                "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLower();
                if (ext == ".tga" || ext == ".jpg" || ext == ".png")
                    Debug.Log($"[MuAsset] {Path.GetFileName(file)}");
            }
        }
    }
}
