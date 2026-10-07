#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>
    /// Ranuras de arte pintado (guía visual v2, 10.3; <c>docs/art/prompts-chatgpt.md</c> §2 y §5): aplica los ajustes de
    /// importación de los PNG de <c>Assets/_Project/Art/Generated/</c> (Sprite, PPU, bordes 9-slice, sin mipmaps, ASTC en
    /// Android) y mantiene el catálogo <c>Assets/_Project/Resources/AF/ArtSlotCatalog.asset</c> que lee el juego
    /// (<see cref="ArtSlots"/>). Es la forma versionada de hacerlo: nunca se editan a mano los <c>.meta</c> ni el catálogo
    /// (CLAUDE.md, regla dura 3). Sin PNG no hay catálogo y todo es procedural.
    /// </summary>
    public sealed class GeneratedArt : AssetPostprocessor, IPreprocessBuildWithReport
    {
        /// <summary>Carpeta de los PNG pintados.</summary>
        public const string Folder = "Assets/_Project/Art/Generated";

        /// <summary>Ruta del catálogo.</summary>
        public const string CatalogPath = "Assets/_Project/Resources/AF/ArtSlotCatalog.asset";

        private const string LogPrefix = "[AF.GeneratedArt] ";

        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <summary>Ajustes de importación por ranura (tabla 2 de los prompts).</summary>
        public static SlotSettings SettingsFor(string slot)
        {
            switch (slot)
            {
                case ArtSlots.Panorama:
                case ArtSlots.Dock:
                    return new SlotSettings(100f, Vector4.zero, TextureImporterFormat.ASTC_6x6, false);
                case ArtSlots.FoliageTopLeft:
                case ArtSlots.FoliageTopRight:
                case ArtSlots.FoliageBottomLeft:
                case ArtSlots.FoliageBottomRight:
                    return new SlotSettings(100f, Vector4.zero, TextureImporterFormat.ASTC_6x6, false);
                case ArtSlots.Ribbon:
                    return new SlotSettings(400f, new Vector4(320f, 0f, 320f, 0f), TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.ButtonLeaf:
                case ArtSlots.ButtonWater:
                case ArtSlots.ButtonWood:
                    return new SlotSettings(300f, new Vector4(84f, 0f, 84f, 0f), TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.SlotWater:
                    return new SlotSettings(300f, Vector4.zero, TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.DiscWood:
                case ArtSlots.BadgePlate:
                    return new SlotSettings(400f, Vector4.zero, TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.PanelFrame:
                    return new SlotSettings(400f, new Vector4(96f, 96f, 96f, 96f), TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.ModalPanel:
                    return new SlotSettings(400f, new Vector4(112f, 112f, 112f, 112f), TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.Tray:
                    return new SlotSettings(400f, new Vector4(128f, 0f, 128f, 0f), TextureImporterFormat.ASTC_4x4, false);
                case ArtSlots.WoodDarkTile:
                case ArtSlots.WoodLightTile:
                    return new SlotSettings(400f, Vector4.zero, TextureImporterFormat.ASTC_6x6, true);
                default:
                    return new SlotSettings(100f, Vector4.zero, TextureImporterFormat.ASTC_4x4, false);
            }
        }

        /// <summary>Reconstruye el catálogo con los PNG presentes (lo borra si no queda ninguno).</summary>
        [MenuItem("Amazon Fill/Arte/Actualizar catálogo de arte generado")]
        public static void RebuildCatalog()
        {
            var entries = new List<ArtSlotCatalog.Entry>();
            if (AssetDatabase.IsValidFolder(Folder))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
                Array.Sort(guids, StringComparer.Ordinal);
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string slot = Path.GetFileNameWithoutExtension(path);
                    if (Array.IndexOf(ArtSlots.All, slot) < 0)
                    {
                        Debug.LogWarning(LogPrefix + path + " no es una ranura conocida (docs/art/prompts-chatgpt.md §2): se ignora.");
                        continue;
                    }

                    Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                    {
                        Debug.LogWarning(LogPrefix + path + " no se importó como Sprite: se ignora.");
                        continue;
                    }

                    entries.Add(new ArtSlotCatalog.Entry { slot = slot, sprite = sprite });
                }
            }

            ArtSlotCatalog? catalog = AssetDatabase.LoadAssetAtPath<ArtSlotCatalog>(CatalogPath);
            if (entries.Count == 0)
            {
                if (catalog != null)
                {
                    AssetDatabase.DeleteAsset(CatalogPath);
                    Debug.Log(LogPrefix + "sin PNG en " + Folder + ": catálogo borrado (todo procedural).");
                }

                ArtSlots.Reload();
                return;
            }

            if (catalog == null)
            {
                EnsureFolder(Path.GetDirectoryName(CatalogPath)!.Replace('\\', '/'));
                catalog = ScriptableObject.CreateInstance<ArtSlotCatalog>();
                catalog.SetEntries(entries.ToArray());
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                catalog.SetEntries(entries.ToArray());
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            ArtSlots.Reload();
            Debug.Log(LogPrefix + entries.Count + " ranuras con PNG en el catálogo.");
        }

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            RebuildCatalog();
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder + "/", StringComparison.Ordinal) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            SlotSettings settings = SettingsFor(Path.GetFileNameWithoutExtension(assetPath));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = settings.PixelsPerUnit;
            importer.spriteBorder = settings.Border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = settings.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = settings.AndroidFormat,
                maxTextureSize = 2048,
            };
            importer.SetPlatformTextureSettings(android);
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
            {
                // Crear assets durante una importación no es seguro: se hace al terminar.
                EditorApplication.delayCall += RebuildCatalog;
            }
        }

        private static bool Touches(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i].StartsWith(Folder + "/", StringComparison.Ordinal) && paths[i].EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        /// <summary>Ajustes de importación de una ranura.</summary>
        public readonly struct SlotSettings
        {
            /// <summary>Crea los ajustes.</summary>
            public SlotSettings(float pixelsPerUnit, Vector4 border, TextureImporterFormat androidFormat, bool repeat)
            {
                PixelsPerUnit = pixelsPerUnit;
                Border = border;
                AndroidFormat = androidFormat;
                Repeat = repeat;
            }

            /// <summary>PPU (300 = 3 px por dp; 400 = 4 px por dp; prompts §1).</summary>
            public float PixelsPerUnit { get; }

            /// <summary>Bordes 9-slice en px del PNG final (izquierda, abajo, derecha, arriba).</summary>
            public Vector4 Border { get; }

            /// <summary>Formato en Android (ASTC 6×6 en fondos, 4×4 en UI; 08 §5.4).</summary>
            public TextureImporterFormat AndroidFormat { get; }

            /// <summary>Tesela que se repite.</summary>
            public bool Repeat { get; }
        }
    }
}
