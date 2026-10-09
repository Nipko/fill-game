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
    /// (CLAUDE.md, regla dura 3). Sin PNG no hay catálogo y todo es procedural. Con «Llenar y teñir» (guion de la intro §12)
    /// llegan las ranuras de fauna (<c>fauna_&lt;id&gt;_partes</c> y <c>fauna_&lt;id&gt;_retrato</c>), las 14 frutas
    /// (<c>fruta_&lt;tinte&gt;</c>), la rama de cada mundo (<c>rama_&lt;mundo&gt;</c>) y la liana (<c>liana_tile</c>); con los
    /// <c>fruta_*.png</c> presentes, el script genera el atlas de vuelo de 64 px
    /// (<c>Assets/_Project/Resources/AF/fruit_flight_atlas.png</c>, 512 × 128) con el respaldo procedural en las que falten.
    /// </summary>
    public sealed class GeneratedArt : AssetPostprocessor, IPreprocessBuildWithReport
    {
        /// <summary>Carpeta de los PNG pintados.</summary>
        public const string Folder = "Assets/_Project/Art/Generated";

        /// <summary>Ruta del catálogo.</summary>
        public const string CatalogPath = "Assets/_Project/Resources/AF/ArtSlotCatalog.asset";

        /// <summary>Atlas de frutas en vuelo de 64 px (lo lee <see cref="ProceduralArt.FruitFlight"/>).</summary>
        public const string FruitAtlasPath = "Assets/_Project/Resources/AF/fruit_flight_atlas.png";

        private const string LogPrefix = "[AF.GeneratedArt] ";

        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <summary>Ajustes de importación por ranura (tabla 2 de los prompts).</summary>
        public static SlotSettings SettingsFor(string slot)
        {
            if (slot.StartsWith(ArtSlots.FaunaPrefix, StringComparison.Ordinal) || slot.StartsWith(ArtSlots.FruitPrefix, StringComparison.Ordinal)
                || slot.StartsWith(ArtSlots.BranchPrefix, StringComparison.Ordinal))
            {
                // Piezas de miquito (1024 × 1024, rejilla de 4 × 4), retratos (512), frutas (256) y ramas (1024 × 256): UI.
                return new SlotSettings(100f, Vector4.zero, TextureImporterFormat.ASTC_4x4, false);
            }

            if (slot == ArtSlots.LianaTile)
            {
                return new SlotSettings(100f, Vector4.zero, TextureImporterFormat.ASTC_4x4, true);
            }

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
                    if (!ArtSlots.IsKnown(slot))
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

            RebuildFruitAtlas();
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

        /// <summary>
        /// Atlas de frutas en vuelo de 512 × 128 (guion de la intro §11): cada fruta con PNG, reducida a 64 px con un filtro de
        /// caja con alfa premultiplicado; el resto, el respaldo procedural. Sin ningún <c>fruta_*.png</c>, el atlas no existe.
        /// </summary>
        public static void RebuildFruitAtlas()
        {
            Color32[] pixels = ProceduralArt.FruitFlightPixels();
            int painted = 0;
            for (int tint = 1; tint <= 14; tint++)
            {
                string path = Folder + "/" + ArtSlots.FruitPrefix + ProceduralArt.TintIds[tint] + ".png";
                if (!File.Exists(path))
                {
                    continue;
                }

                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!source.LoadImage(File.ReadAllBytes(path)))
                    {
                        Debug.LogWarning(LogPrefix + path + " no se pudo leer para el atlas de vuelo.");
                        continue;
                    }

                    Downscale(source, pixels, ProceduralArt.FruitCell(tint));
                    painted++;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }

            if (painted == 0)
            {
                if (File.Exists(FruitAtlasPath))
                {
                    AssetDatabase.DeleteAsset(FruitAtlasPath);
                }

                return;
            }

            var atlas = new Texture2D(ProceduralArt.FruitAtlasWidth, ProceduralArt.FruitAtlasHeight, TextureFormat.RGBA32, false);
            try
            {
                atlas.SetPixels32(pixels);
                atlas.Apply(false, false);
                EnsureFolder(Path.GetDirectoryName(FruitAtlasPath)!.Replace('\\', '/'));
                File.WriteAllBytes(FruitAtlasPath, atlas.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
            }

            AssetDatabase.ImportAsset(FruitAtlasPath, ImportAssetOptions.ForceUpdate);
            Debug.Log(LogPrefix + painted + " frutas con PNG en el atlas de vuelo.");
        }

        // Reduce source a la celda (filtro de caja con alfa premultiplicado: sin halos oscuros en los bordes).
        private static void Downscale(Texture2D source, Color32[] atlas, RectInt cell)
        {
            Color32[] src = source.GetPixels32();
            int sw = source.width;
            int sh = source.height;
            for (int y = 0; y < cell.height; y++)
            {
                int y0 = y * sh / cell.height;
                int y1 = Mathf.Max(y0 + 1, (y + 1) * sh / cell.height);
                for (int x = 0; x < cell.width; x++)
                {
                    int x0 = x * sw / cell.width;
                    int x1 = Mathf.Max(x0 + 1, (x + 1) * sw / cell.width);
                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    float a = 0f;
                    int n = 0;
                    for (int sy = y0; sy < y1; sy++)
                    {
                        for (int sx = x0; sx < x1; sx++)
                        {
                            Color32 c = src[(sy * sw) + sx];
                            float ca = c.a / 255f;
                            r += c.r * ca;
                            g += c.g * ca;
                            b += c.b * ca;
                            a += ca;
                            n++;
                        }
                    }

                    var o = new Color32(0, 0, 0, 0);
                    if (a > 0f)
                    {
                        o = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(r / a), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(g / a), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(b / a), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(a / n * 255f), 0, 255));
                    }

                    atlas[((cell.y + y) * ProceduralArt.FruitAtlasWidth) + cell.x + x] = o;
                }
            }
        }

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            RebuildCatalog();
        }

        private void OnPreprocessTexture()
        {
            if (string.Equals(assetPath, FruitAtlasPath, StringComparison.Ordinal))
            {
                // Atlas de vuelo: textura (no Sprite; los sprites los crea el juego), sin mipmaps, ASTC 4×4 en Android.
                var atlasImporter = (TextureImporter)assetImporter;
                atlasImporter.textureType = TextureImporterType.Default;
                atlasImporter.mipmapEnabled = false;
                atlasImporter.alphaIsTransparency = true;
                atlasImporter.sRGBTexture = true;
                atlasImporter.wrapMode = TextureWrapMode.Clamp;
                atlasImporter.filterMode = FilterMode.Bilinear;
                atlasImporter.npotScale = TextureImporterNPOTScale.None;
                atlasImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    format = TextureImporterFormat.ASTC_4x4,
                    maxTextureSize = 512,
                });
                return;
            }

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
