#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using AF.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace AF.EditorTools
{
    /// <summary>
    /// Tipografía de la dirección visual v2 (docs/art/direccion-visual-v2.md 5.6; D-003): importa los recursos esenciales
    /// de TextMeshPro (vienen con com.unity.ugui en Unity 6, sin paquetes nuevos) y crea, a partir de las fuentes OFL de
    /// <c>Assets/_Project/Art/Fonts/</c>, un font asset SDF con <b>atlas estático</b> por fuente y <b>un material por estilo</b>
    /// de contorno y sombra. Todo queda en <c>Assets/_Project/Resources/AF/Fonts/</c>, así entra en el build y la variante
    /// <c>UNDERLAY_ON</c> (shader_feature) no se elimina. Es la forma versionada de crear estos assets (CLAUDE.md, regla dura
    /// 3): nunca se editan a mano. Idempotente: recrea los assets cada vez, y los que ya existen conservan su GUID.
    /// <para>Glifos (D-018; propuesta de idiomas §4.3; tarea B1): los de <see cref="Characters()"/>, calculados desde las tablas
    /// de textos, menos, en los atlas de Nunito, los que solo se pintan en Baloo 2 (<see cref="Characters(GlyphFamily)"/>). Antes
    /// de crear nada, <see cref="FontCoverage"/> comprueba que cada carácter de las tablas esté en la TTF de su familia; si no,
    /// para con el código 5 y los lista.</para>
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/font-setup.log --non-interactive
    ///             -- -nographics -executeMethod AF.EditorTools.FontSetup.ApplyAll
    /// Solo los materiales de estilo (conserva los font assets y los GUID de los materiales):
    ///   ... -executeMethod AF.EditorTools.FontSetup.ApplyStyles
    /// Códigos de salida (solo en batchmode): 0 OK · 2 faltan los recursos esenciales de TMP tras importarlos (vuelve a
    /// ejecutarlo) · 3 falta una fuente (o un font asset, con ApplyStyles) · 4 excepción · 5 un carácter de las tablas no está
    /// en la TTF de su familia o está prohibido (<see cref="FontCoverage"/>), o los glifos no caben en el atlas más grande ·
    /// 6 las tablas de textos no se pueden leer.
    /// </summary>
    /// <remarks>
    /// Contorno y sombra nítidos (revisión visual del 2026-10-07): el contorno de cada estilo mide lo que dice la guía 5.6
    /// y queda entero por fuera de la letra (antes salía al doble, porque <c>_OutlineWidth</c> es un semiancho en TMP), la
    /// cara conserva su grosor y la sombra es la silueta nítida (suavidad 0,05, antes 0,25) desplazada 1,5-2 dp hacia abajo.
    /// </remarks>
    public static class FontSetup
    {
        /// <summary>Carpeta de las TTF aprobadas (OFL).</summary>
        public const string SourceDir = "Assets/_Project/Art/Fonts";

        /// <summary>Carpeta de salida (dentro de un <c>Resources</c>: la UI los carga con <c>Resources.Load</c>).</summary>
        public const string OutputDir = "Assets/_Project/Resources/AF/Fonts";

        /// <summary>Ajustes de TMP que crea la importación de los recursos esenciales.</summary>
        public const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private const string LogPrefix = "[AF.FontSetup] ";

        // Margen del SDF (guía 5.6): ≥ 0,20 em en los estilos con contorno y sombra. 13 px a 56 pt = 0,23 em.
        private const int SamplingPointSize = 56;
        private const int AtlasPadding = 13;

        // Carpeta temporal donde se crea cada font asset antes de copiarlo sobre el que ya existe (así conserva su GUID).
        private const string TempDir = "Assets/_FontSetupTemp";

        /// <summary>Puntuación tipográfica fija de la guía 5.6 (la de siempre, además de lo que piden las tablas).</summary>
        public const string Typography = "–—‘’“”…•";

        private static readonly string[] Fonts =
        {
            "Baloo2-ExtraBold",
            "Baloo2-Bold",
            "Nunito-ExtraBold",
            "Nunito-Bold",
            "Nunito-Regular",
        };

        // Estilos de la tabla de la guía 5.6: fuente, nombre, color del contorno, contorno (dp sobre el tamaño de
        // referencia), tamaño de referencia (dp), sombra (desplazamiento en dp y alfa).
        private static readonly Style[] Styles =
        {
            new Style("Baloo2-ExtraBold", "Ribbon", 0x0F3A15, 3.0f, 26f, 2.0f, 0.35f),     // cinta de victoria (hoja_contorno)
            new Style("Baloo2-ExtraBold", "Badge", 0x1E140E, 2.5f, 26f, 2.0f, 0.35f),      // insignia de nivel (madera_contorno)
            new Style("Baloo2-ExtraBold", "ModalTitle", 0x1E140E, 2.0f, 22f, 1.5f, 0.30f), // título sobre placa
            new Style("Baloo2-Bold", "CtaHoja", 0x0F3A15, 2.0f, 22f, 1.5f, 0.30f),         // Siguiente
            new Style("Baloo2-Bold", "CtaAgua", 0x073640, 2.0f, 22f, 1.5f, 0.30f),         // Deshacer · Marea (modal)
            new Style("Nunito-ExtraBold", "SlotAgua", 0x073640, 1.5f, 12f, 0f, 0f),        // etiqueta de la ranura
            new Style("Nunito-ExtraBold", "PillMadera", 0x1E140E, 1.5f, 12f, 0f, 0f),      // píldora «Reiniciar»
            new Style("Nunito-Bold", "Plain", 0, 0f, 14f, 0f, 0f),                          // subtítulo y enlace
            new Style("Nunito-Regular", "Plain", 0, 0f, 14f, 0f, 0f),                       // texto corrido
        };

        /// <summary>Importa los recursos esenciales de TMP (si faltan) y crea los font assets y materiales.</summary>
        [MenuItem("Amazon Fill/Arte/Importar TMP y crear fuentes")]
        public static void ApplyAll()
        {
            int code;
            try
            {
                code = Run();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = 4;
            }

            if (code == 0)
            {
                Debug.Log(LogPrefix + "OK: fuentes en " + OutputDir + ".");
            }
            else
            {
                Debug.LogError(LogPrefix + "FALLO (código " + code + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        /// <summary>
        /// Actualiza solo los materiales de estilo (contorno y sombra) sobre los font assets que ya existen, sin
        /// recrearlos: los materiales conservan su GUID.
        /// </summary>
        [MenuItem("Amazon Fill/Arte/Actualizar estilos de texto")]
        public static void ApplyStyles()
        {
            int code;
            try
            {
                code = RunStyles();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = 4;
            }

            if (code == 0)
            {
                Debug.Log(LogPrefix + "OK: estilos de texto actualizados en " + OutputDir + ".");
            }
            else
            {
                Debug.LogError(LogPrefix + "FALLO (código " + code + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        /// <summary>
        /// Glifos de los atlas (D-018; propuesta de idiomas §4.3; tarea B1): ASCII, Latin-1, la puntuación tipográfica de siempre
        /// (<see cref="Typography"/>) y todos los caracteres de <c>strings.csv</c>, <c>facts.csv</c> y <c>glossary.csv</c> (si
        /// existe) en es-419, en y pt-BR, más los de <see cref="LocNumber.Characters"/> y <see cref="LocPseudo.Characters"/>. Lee
        /// las tablas del proyecto (UTF-8 estricto). Sin repetir y en un orden fijo.
        /// </summary>
        public static string Characters()
        {
            return Characters(GlyphFamily.Title, FontCoverage.Analyze(FontCoverage.ReadProjectTables(), AnyGlyph));
        }

        /// <summary>
        /// Como <see cref="Characters()"/>, solo con lo que se pinta en <paramref name="family"/>: en Nunito
        /// (<see cref="GlyphFamily.Body"/>) faltan los caracteres que solo usan las claves de <see cref="FontCoverage.TitleOnlyKeys"/>.
        /// </summary>
        public static string Characters(GlyphFamily family)
        {
            return Characters(family, FontCoverage.Analyze(FontCoverage.ReadProjectTables(), AnyGlyph));
        }

        /// <summary>Los glifos de <paramref name="family"/> con los caracteres de las tablas ya analizados en <paramref name="coverage"/>.</summary>
        public static string Characters(GlyphFamily family, FontCoverageReport coverage)
        {
            if (coverage == null)
            {
                throw new ArgumentNullException(nameof(coverage));
            }

            var sb = new StringBuilder(512);
            var seen = new HashSet<char>();
            for (int c = 0x20; c <= 0x7E; c++)
            {
                Append(sb, seen, (char)c);
            }

            for (int c = 0xA0; c <= 0xFF; c++)
            {
                Append(sb, seen, (char)c);
            }

            for (int i = 0; i < Typography.Length; i++)
            {
                Append(sb, seen, Typography[i]);
            }

            string tables = coverage.CharactersFor(family);
            for (int i = 0; i < tables.Length; i++)
            {
                Append(sb, seen, tables[i]);
            }

            return sb.ToString();
        }

        private static bool AnyGlyph(string font, char c)
        {
            return true;
        }

        private static void Append(StringBuilder sb, HashSet<char> seen, char c)
        {
            if (seen.Add(c))
            {
                sb.Append(c);
            }
        }

        private static int Run()
        {
            if (!ImportTmpEssentials())
            {
                return 2;
            }

            // Antes de tocar nada: cada carácter de las tablas en la TTF de su familia (B1).
            int coverageCode = FontCoverage.Run(false, out FontCoverageReport? coverage);
            if (coverageCode != 0 || coverage == null)
            {
                Debug.LogError(LogPrefix + "la cobertura de las fuentes falla (FontCoverage, código " + coverageCode + "): no se regeneran los atlas.");
                return coverageCode != 0 ? coverageCode : 4;
            }

            // La carpeta temporal de Save() no puede quedarse en Assets/ (acabaría commiteada con su .meta): se borra la que
            // dejara un intento anterior interrumpido y, pase lo que pase (también con una excepción, código 4), al terminar.
            DeleteTempDir();
            try
            {
                return Generate(coverage);
            }
            finally
            {
                DeleteTempDir();
            }
        }

        private static int Generate(FontCoverageReport coverage)
        {
            EnsureFolder(OutputDir);
            var assets = new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
            bool missingAny = false;
            for (int i = 0; i < Fonts.Length; i++)
            {
                string name = Fonts[i];
                string ttf = SourceDir + "/" + name + ".ttf";
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                if (font == null)
                {
                    Debug.LogError(LogPrefix + "no existe " + ttf + ".");
                    return 3;
                }

                GlyphFamily family = FontCoverage.FamilyOfFont(name);
                string chars = Characters(family, coverage);

                // Primero un atlas de 1024 × 1024; si no caben todos los glifos que tiene la fuente, se repite con 2048 × 1024,
                // el máximo de la guía (8). Lo que la TTF no tiene (algún carácter de Latin-1 que no sale en las tablas) solo se
                // avisa: lo de las tablas ya lo comprobó FontCoverage.
                TMP_FontAsset? asset = CreateStatic(font, name, chars, 1024, 1024, out string missing);
                string notInFont = NotInFont(name, missing, out string notFitting);
                if (asset != null && notFitting.Length > 0)
                {
                    UnityEngine.Object.DestroyImmediate(asset.material);
                    UnityEngine.Object.DestroyImmediate(asset.atlasTexture);
                    UnityEngine.Object.DestroyImmediate(asset);
                    asset = CreateStatic(font, name, chars, 2048, 1024, out missing);
                    notInFont = NotInFont(name, missing, out notFitting);
                }

                if (asset == null)
                {
                    Debug.LogError(LogPrefix + "no se pudo crear el font asset de " + name + ".");
                    return 4;
                }

                if (notFitting.Length > 0)
                {
                    Debug.LogError(LogPrefix + name + ": no caben en el atlas: " + Describe(notFitting));
                    missingAny = true;
                }

                if (notInFont.Length > 0)
                {
                    Debug.LogWarning(LogPrefix + name + ": la fuente no tiene " + Describe(notInFont) + "(ninguno sale en las tablas).");
                }

                Debug.Log(LogPrefix + name + " (" + family + "): " + chars.Length + " caracteres pedidos.");
                assets[name] = Save(asset, name);
            }

            DeleteTempDir();

            List<string> gaps = AtlasGaps(coverage);
            for (int i = 0; i < gaps.Count; i++)
            {
                Debug.LogError(LogPrefix + gaps[i]);
                missingAny = true;
            }

            for (int i = 0; i < Styles.Length; i++)
            {
                Style s = Styles[i];
                CreateStyleMaterial(assets[s.Font], s);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return missingAny ? 5 : 0;
        }

        /// <summary>Borra <see cref="TempDir"/> (y su .meta) si existe.</summary>
        private static void DeleteTempDir()
        {
            if (AssetDatabase.IsValidFolder(TempDir))
            {
                AssetDatabase.DeleteAsset(TempDir);
            }
        }

        private static int RunStyles()
        {
            for (int i = 0; i < Styles.Length; i++)
            {
                Style s = Styles[i];
                string path = OutputDir + "/" + s.Font + " SDF.asset";
                TMP_FontAsset? asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset == null)
                {
                    Debug.LogError(LogPrefix + "no existe " + path + ": ejecuta antes ApplyAll.");
                    return 3;
                }

                CreateStyleMaterial(asset, s);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return 0;
        }

        /// <summary>Nombre del material de un estilo (lo usa la UI: <c>Resources.Load&lt;Material&gt;("AF/Fonts/...")</c>).</summary>
        public static string MaterialName(string font, string style)
        {
            return font + " SDF - " + style;
        }

        private static bool ImportTmpEssentials()
        {
            if (File.Exists(TmpSettingsPath))
            {
                return true;
            }

            UnityEditor.PackageManager.PackageInfo? info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly);
            if (info == null)
            {
                Debug.LogError(LogPrefix + "no se encuentra el paquete com.unity.ugui.");
                return false;
            }

            string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(package))
            {
                Debug.LogError(LogPrefix + "no existe " + package + ".");
                return false;
            }

            Debug.Log(LogPrefix + "importando los recursos esenciales de TMP desde " + package + " ...");

            // AssetDatabase.ImportPackage es asíncrono: en batchmode con -quit (unity run) el Editor se cierra antes de
            // procesarlo. AssetDatabase.ImportPackageImmediately hace lo mismo de forma síncrona, pero es interna: se
            // llama por reflexión y, si no existe en esta versión, se cae a la importación asíncrona (en el Editor
            // gráfico basta con volver a ejecutar el menú cuando termine).
            MethodInfo? immediate = typeof(AssetDatabase).GetMethod(
                "ImportPackageImmediately",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);
            if (immediate != null)
            {
                immediate.Invoke(null, new object[] { package });
            }
            else
            {
                Debug.LogWarning(LogPrefix + "sin AssetDatabase.ImportPackageImmediately: importación asíncrona.");
                AssetDatabase.ImportPackage(package, false);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(TmpSettingsPath))
            {
                Debug.LogError(LogPrefix + "la importación de los recursos esenciales de TMP no ha terminado: vuelve a ejecutar el método.");
                return false;
            }

            return true;
        }

        private static TMP_FontAsset? CreateStatic(Font font, string name, string chars, int width, int height, out string missing)
        {
            missing = string.Empty;
            TMP_FontAsset? asset = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, width, height, AtlasPopulationMode.Dynamic, false);
            if (asset == null)
            {
                return null;
            }

            asset.name = name + " SDF";
            asset.TryAddCharacters(chars, out string notAdded);
            missing = notAdded ?? string.Empty;
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            return asset;
        }

        /// <summary>
        /// Lo que falta en los atlas generados (<see cref="OutputDir"/>) de lo que pide su familia en <paramref name="coverage"/>:
        /// un mensaje por font asset, vacío si todos lo tienen. Un carácter que falta en el atlas estático sale en pantalla como □
        /// (o tira de la fuente de reserva de TMP). Solo mira el atlas, sin reservas ni añadir glifos.
        /// </summary>
        public static List<string> AtlasGaps(FontCoverageReport coverage)
        {
            if (coverage == null)
            {
                throw new ArgumentNullException(nameof(coverage));
            }

            var gaps = new List<string>();
            for (int i = 0; i < Fonts.Length; i++)
            {
                string path = OutputDir + "/" + Fonts[i] + " SDF.asset";
                TMP_FontAsset? asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset == null)
                {
                    gaps.Add(path + " no existe: ejecuta AF.EditorTools.FontSetup.ApplyAll.");
                    continue;
                }

                string chars = coverage.CharactersFor(FontCoverage.FamilyOfFont(Fonts[i]));
                var missing = new StringBuilder();
                for (int c = 0; c < chars.Length; c++)
                {
                    if (!asset.HasCharacter(chars[c], false, false))
                    {
                        missing.Append(chars[c]);
                    }
                }

                if (missing.Length > 0)
                {
                    gaps.Add(path + " no tiene " + Describe(missing.ToString()) + "(atlas desactualizado): ejecuta AF.EditorTools.FontSetup.ApplyAll.");
                }
            }

            return gaps;
        }

        /// <summary>
        /// De los caracteres que TMP no añadió (<paramref name="missing"/>), los que la TTF no tiene (se devuelven) y los que sí
        /// tiene pero no cupieron en el atlas (<paramref name="notFitting"/>).
        /// </summary>
        private static string NotInFont(string font, string missing, out string notFitting)
        {
            var absent = new StringBuilder();
            var full = new StringBuilder();
            var glyphs = new FontCoverage.TtfGlyphs();
            for (int i = 0; i < missing.Length; i++)
            {
                (glyphs.Has(font, missing[i]) ? full : absent).Append(missing[i]);
            }

            notFitting = full.ToString();
            return absent.ToString();
        }

        private static TMP_FontAsset Save(TMP_FontAsset asset, string name)
        {
            // Si ya existe, se crea en una carpeta temporal y se copia el archivo encima: el font asset conserva su GUID (y su
            // .meta), y los materiales de estilo lo siguen encontrando. Si no, se crea en su sitio.
            string path = OutputDir + "/" + name + " SDF.asset";
            bool exists = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null;
            string target = path;
            if (exists)
            {
                EnsureFolder(TempDir);
                target = TempDir + "/" + name + " SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target) != null)
                {
                    AssetDatabase.DeleteAsset(target);
                }
            }

            Texture2D atlas = asset.atlasTexture;
            atlas.name = name + " SDF Atlas";
            Material material = asset.material;
            material.name = name + " SDF Material";
            ShaderUtilities.UpdateShaderRatios(material); // como lo deja TMP al pintar: el asset no cambia solo después
            AssetDatabase.CreateAsset(asset, target);
            AssetDatabase.AddObjectToAsset(atlas, asset);
            AssetDatabase.AddObjectToAsset(material, asset);

            // Atlas estático: la TTF no hace falta en el build (solo se queda la referencia de creación por GUID).
            var so = new SerializedObject(asset);
            SerializedProperty source = so.FindProperty("m_SourceFontFile");
            if (source != null)
            {
                source.objectReferenceValue = null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            if (exists)
            {
                File.Copy(DiskPathOf(target), DiskPathOf(path), true);
                AssetDatabase.DeleteAsset(target);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TMP_FontAsset saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Debug.Log(LogPrefix + path + ": " + saved.characterTable.Count + " caracteres, atlas " + saved.atlasWidth + " × " + saved.atlasHeight + ".");
            return saved;
        }

        private static void CreateStyleMaterial(TMP_FontAsset asset, Style style)
        {
            // Si el material ya existe se actualiza en su sitio (conserva el GUID); si no, se crea.
            string path = OutputDir + "/" + MaterialName(style.Font, style.Name) + ".mat";
            Material? existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            Material mat;
            if (existing != null)
            {
                mat = existing;
                mat.shader = asset.material.shader;
                mat.CopyPropertiesFromMaterial(asset.material);
            }
            else
            {
                mat = new Material(asset.material) { name = MaterialName(style.Font, style.Name) };
            }

            // Unidades del SDF de TMP: el valor del atlas va de 0 a 1 a lo largo de 2 · (padding + 1) px, con el borde en
            // 0,5. En el shader, _OutlineWidth = w pone el contorno a ± w · (padding + 1) px del borde de la cara (es el
            // semiancho, no el ancho), _FaceDilate = f mueve ese borde f · (padding + 1) px hacia fuera, y lo mismo
            // _UnderlayDilate y _UnderlayOffset con la sombra. En em: spread = (padding + 1) / tamaño de muestreo.
            // Revisión visual del 2026-10-07: antes se tomaba w como el ancho total y los contornos salían al doble (5 dp
            // en lugar de 2,5 en la insignia): letras gordas y un halo oscuro ancho.
            float spreadEm = (AtlasPadding + 1f) / SamplingPointSize;
            float half = 0f; // semiancho del contorno (unidades del shader)
            if (style.OutlineDp > 0f)
            {
                // Contorno de OutlineDp entero por fuera de la letra: semiancho de OutlineDp / 2 y la cara dilatada lo mismo,
                // así la letra conserva su grosor y el contorno no se la come. Sin suavizado.
                float outlineEm = style.OutlineDp / style.SizeDp;
                half = Mathf.Clamp01(outlineEm / (2f * spreadEm));
                mat.SetColor("_OutlineColor", Hex(style.OutlineRgb));
                mat.SetFloat("_OutlineWidth", half);
                mat.SetFloat("_OutlineSoftness", 0f);
                mat.SetFloat("_FaceDilate", half);
                mat.SetFloat("_Sharpness", 0f);
                mat.EnableKeyword("OUTLINE_ON");
            }
            else
            {
                mat.SetFloat("_OutlineWidth", 0f);
                mat.SetFloat("_OutlineSoftness", 0f);
                mat.SetFloat("_FaceDilate", 0f);
                mat.DisableKeyword("OUTLINE_ON");
            }

            if (style.ShadowDp > 0f)
            {
                // Sombra nítida y desplazada (no un brillo): la silueta de la letra con su contorno, 1,5-2 dp hacia abajo,
                // con suavidad 0,05 (antes 0,25: halo difuso).
                float offsetEm = style.ShadowDp / style.SizeDp;
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, style.ShadowAlpha));
                mat.SetFloat("_UnderlayOffsetX", 0f);
                mat.SetFloat("_UnderlayOffsetY", -Mathf.Clamp(offsetEm / spreadEm, 0f, 1f));
                mat.SetFloat("_UnderlayDilate", half); // el peso de la cara (_FaceDilate) ya entra en la sombra: + half = la silueta con contorno
                mat.SetFloat("_UnderlaySoftness", 0.05f);
            }
            else
            {
                mat.DisableKeyword("UNDERLAY_ON");
                mat.SetFloat("_UnderlaySoftness", 0f);
            }

            // Las proporciones del shader (_ScaleRatioA/B/C) las calcula TMP al pintar con el material; se fijan aquí para que el
            // asset quede como lo deja TMP y no cambie solo al abrir una escena o correr los tests.
            ShaderUtilities.UpdateShaderRatios(mat);
            if (existing != null)
            {
                EditorUtility.SetDirty(mat);
            }
            else
            {
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static string DiskPathOf(string assetPath)
        {
            return FontCoverage.DiskPath(assetPath);
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

        private static string Describe(string chars)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                sb.Append("U+").Append(((int)chars[i]).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
            }

            return sb.ToString();
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        private readonly struct Style
        {
            public Style(string font, string name, int outlineRgb, float outlineDp, float sizeDp, float shadowDp, float shadowAlpha)
            {
                Font = font;
                Name = name;
                OutlineRgb = outlineRgb;
                OutlineDp = outlineDp;
                SizeDp = sizeDp;
                ShadowDp = shadowDp;
                ShadowAlpha = shadowAlpha;
            }

            public string Font { get; }

            public string Name { get; }

            public int OutlineRgb { get; }

            public float OutlineDp { get; }

            public float SizeDp { get; }

            public float ShadowDp { get; }

            public float ShadowAlpha { get; }
        }
    }
}
