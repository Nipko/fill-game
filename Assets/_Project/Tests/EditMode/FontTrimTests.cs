#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Fuentes recortadas sin perder ningún glifo (tarea FNT1; medida de B2: 6,88 MB de fuentes en el build):
    /// <list type="bullet">
    /// <item>cada carácter de <c>strings.csv</c>, <c>facts.csv</c> y <c>glossary.csv</c> (es-419, en y pt-BR), de las cifras, de en-XA
    /// y de los símbolos fijos de <see cref="FontSetup"/> (<see cref="FontSetup.CodeSymbols"/>, y <see cref="FontSetup.TitleCodeSymbols"/>
    /// solo en Baloo 2) está en el atlas estático de cada fuente del juego, sin reservas ni glifos añadidos al pintar: un carácter
    /// que faltara se pintaría con otra tipografía (la fuente por defecto de TMP), y el aspecto cambiaría;</item>
    /// <item>los atlas conservan el muestreo y el margen (el texto se ve igual) y ninguno pasa de 1024 × 1024;</item>
    /// <item>LiberationSans no entra en el build: ningún font asset ni TTF de TMP en una carpeta <c>Resources</c>, y la fuente por
    /// defecto de TMP es Nunito Regular.</item>
    /// </list>
    /// </summary>
    public sealed class FontTrimTests
    {
        private const int SamplingPointSize = 56;
        private const int AtlasPadding = 13;

        [Test]
        public void GameFonts_HaveEveryTableNumberPseudoAndCodeCharacter_InTheirOwnAtlas()
        {
            FontCoverageReport report = FontCoverage.Analyze(FontCoverage.ReadProjectTables(), new FontCoverage.TtfGlyphs().Has);
            Assert.That(report.Texts, Is.GreaterThan(150), "celdas con texto en es-419, en y pt-BR");

            var missing = new List<string>();
            int checkedChars = 0;
            foreach (string font in FontSetup.Fonts)
            {
                TMP_FontAsset asset = LoadGameFont(font);
                GlyphFamily family = FontCoverage.FamilyOfFont(font);
                string chars = report.CharactersFor(family) + FontSetup.CodeSymbols
                    + (family == GlyphFamily.Title ? FontSetup.TitleCodeSymbols : string.Empty);
                for (int i = 0; i < chars.Length; i++)
                {
                    checkedChars++;
                    // Sin buscar en reservas: con searchFallbacks, TMP acaba en TMP_Settings.defaultFontAsset (Nunito Regular).
                    if (!asset.HasCharacter(chars[i], false, false))
                    {
                        missing.Add(font + ": «" + chars[i] + "» (U+" + ((int)chars[i]).ToString("X4", CultureInfo.InvariantCulture) + ")");
                    }
                }
            }

            Assert.That(checkedChars, Is.GreaterThan(5 * 100), "unos 120 caracteres por fuente");
            Assert.That(missing, Is.Empty, string.Join("\n", missing));
        }

        [Test]
        public void CodeSymbols_AreInEveryAtlas_CheckMarkInNone()
        {
            // Los símbolos del código van en todos los atlas aunque ninguna tabla los use hoy; las flechas, solo en Baloo 2, y ✓
            // nunca (es un icono procedural, M3.3).
            Assert.That(FontSetup.CodeSymbols, Does.Contain("×").And.Contain("·").And.Contain(" "));
            Assert.That(FontSetup.CodeSymbols + FontSetup.TitleCodeSymbols, Does.Not.Contain(FontCoverage.CheckMark.ToString()));
            foreach (string font in FontSetup.Fonts)
            {
                TMP_FontAsset asset = LoadGameFont(font);
                Assert.That(asset.HasCharacter(FontCoverage.CheckMark, true, false), Is.False, font + ": ✓ no va en ningún atlas");
                foreach (char c in FontSetup.CodeSymbols)
                {
                    Assert.That(asset.HasCharacter(c, false, false), Is.True, font + ": U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture) + " en su propio atlas");
                }
            }
        }

        [Test]
        public void GameFonts_AreStatic_WithTheSameSamplingAndPadding_AndAtMost1024Square()
        {
            long pixels = 0;
            foreach (string font in FontSetup.Fonts)
            {
                TMP_FontAsset asset = LoadGameFont(font);
                Assert.That(asset.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static), font);
                Assert.That(asset.sourceFontFile, Is.Null, font + ": sin la TTF en el build");
                Assert.That(asset.faceInfo.pointSize, Is.EqualTo(SamplingPointSize), font + ": mismo muestreo (mismo aspecto)");
                Assert.That(asset.atlasPadding, Is.EqualTo(AtlasPadding), font + ": mismo margen (mismo aspecto)");
                Assert.That(asset.atlasTextures, Has.Length.EqualTo(1), font + ": un solo atlas");
                Texture2D atlas = asset.atlasTexture;
                Assert.That(atlas.format, Is.EqualTo(TextureFormat.Alpha8), font);
                Assert.That(atlas.mipmapCount, Is.EqualTo(1), font);
                Assert.That(atlas.width, Is.EqualTo(asset.atlasWidth), font);
                Assert.That(atlas.height, Is.EqualTo(asset.atlasHeight), font);
                Assert.That((long)atlas.width * atlas.height, Is.LessThanOrEqualTo(1024L * 1024L), font + ": " + atlas.width + " × " + atlas.height);
                Assert.That(asset.fallbackFontAssetTable == null || asset.fallbackFontAssetTable.Count == 0, Is.True, font + ": sin reservas propias");
                pixels += (long)atlas.width * atlas.height;
            }

            // Antes de FNT1, cinco atlas de 1024 × 1024 (5.242.880 píxeles, 1 byte cada uno).
            Assert.That(pixels, Is.LessThan(5L * 1024L * 1024L), "los atlas pesan menos que antes de FNT1");
        }

        [Test]
        public void AtlasSizes_GrowFromSmallestToTheGuideMaximum()
        {
            Vector2Int[] sizes = FontSetup.AtlasSizes;
            Assert.That(sizes[0], Is.EqualTo(new Vector2Int(1024, 512)));
            Assert.That(sizes[sizes.Length - 1], Is.EqualTo(new Vector2Int(2048, 1024)), "el máximo de la guía (8)");
            for (int i = 1; i < sizes.Length; i++)
            {
                Assert.That(sizes[i].x * sizes[i].y, Is.GreaterThan(sizes[i - 1].x * sizes[i - 1].y), "de menor a mayor: " + sizes[i]);
            }
        }

        [Test]
        public void LiberationSans_IsNotInTheBuild_AndTmpDefaultsToNunitoRegular()
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(FontSetup.TmpSettingsPath);
            Assert.That(settings, Is.Not.Null, FontSetup.TmpSettingsPath);
            var so = new SerializedObject(settings);
            Object? defaultFont = so.FindProperty("m_defaultFontAsset").objectReferenceValue;
            Assert.That(AssetDatabase.GetAssetPath(defaultFont), Is.EqualTo(FontSetup.OutputDir + "/" + FontSetup.DefaultFont + " SDF.asset"));
            Assert.That(so.FindProperty("m_fallbackFontAssets").arraySize, Is.EqualTo(0), "sin reservas globales");

            // Todo lo que está en una carpeta Resources entra en el build: ahí solo pueden estar las fuentes del juego.
            var inResources = new List<string>();
            foreach (string filter in new[] { "t:TMP_FontAsset", "t:Font" })
            {
                foreach (string guid in AssetDatabase.FindAssets(filter, new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("/Resources/") && !path.StartsWith(FontSetup.OutputDir + "/", System.StringComparison.Ordinal))
                    {
                        inResources.Add(path);
                    }
                }
            }

            Assert.That(inResources, Is.Empty, string.Join("\n", inResources));
            foreach (string name in FontSetup.LiberationSansAssets)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Object>(FontSetup.TmpResourcesFontsDir + "/" + name), Is.Null, name + " fuera de Resources");
                Assert.That(AssetDatabase.LoadAssetAtPath<Object>(FontSetup.TmpEditorFontsDir + "/" + name), Is.Not.Null, name + " sigue en el Editor");
            }
        }

        private static TMP_FontAsset LoadGameFont(string font)
        {
            // La misma ruta que carga UiFonts con Resources.Load("AF/Fonts/<font> SDF").
            string path = FontSetup.OutputDir + "/" + font + " SDF.asset";
            TMP_FontAsset? asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Assert.That(asset, Is.Not.Null, path + ": ejecuta AF.EditorTools.FontSetup.ApplyAll");
            return asset!;
        }
    }
}
