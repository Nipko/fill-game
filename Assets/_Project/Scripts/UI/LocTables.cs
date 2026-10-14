#nullable enable
using System;
using System.IO;
using AF.Localization;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Las tablas de textos del juego (D-018; 08 §4.7; contrato 32), <c>Assets/_Project/Data/Loc/strings.csv</c> (UI) y
    /// <c>facts.csv</c> (textos largos), y cómo se configura <see cref="Loc"/> con ellas: <see cref="GameBootstrap"/> las recibe
    /// como TextAsset de la escena (<c>GameSceneBuilder</c>) y los tests y las herramientas del Editor las leen del disco
    /// (<see cref="FromProjectFiles"/>), en UTF-8 estricto.
    /// </summary>
    public static class LocTables
    {
        /// <summary>Ruta de la tabla de UI en el proyecto.</summary>
        public const string StringsAssetPath = "Assets/_Project/Data/Loc/strings.csv";

        /// <summary>Ruta de la tabla de textos largos en el proyecto.</summary>
        public const string FactsAssetPath = "Assets/_Project/Data/Loc/facts.csv";

        /// <summary>Lee las dos tablas desde el texto de sus TextAsset (<paramref name="facts"/> puede faltar).</summary>
        public static LocCatalog FromAssets(TextAsset strings, TextAsset? facts)
        {
            if (strings == null)
            {
                throw new ArgumentNullException(nameof(strings));
            }

            return LocCatalog.Parse(strings.text, facts != null ? facts.text : null);
        }

        /// <summary>
        /// Lee las dos tablas del disco del proyecto (solo en el Editor y en los tests: <see cref="Application.dataPath"/> es
        /// <c>Assets/</c>), en UTF-8 estricto (<see cref="LocCsv.Decode"/>: con BOM o con bytes que no son UTF-8, error con la
        /// línea).
        /// </summary>
        public static LocCatalog FromProjectFiles()
        {
            return LocCatalog.Parse(ReadProjectFile(StringsAssetPath, LocCatalog.StringsFile), ReadProjectFile(FactsAssetPath, LocCatalog.FactsFile));
        }

        /// <summary>Texto de la tabla <paramref name="assetPath"/> («Assets/…») del proyecto, en UTF-8 estricto.</summary>
        public static string ReadProjectFile(string assetPath, string file)
        {
            return LocCsv.Decode(File.ReadAllBytes(ProjectPath(assetPath)), file);
        }

        /// <summary>Ruta en disco de <paramref name="assetPath"/> («Assets/…»).</summary>
        public static string ProjectPath(string assetPath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            return Path.Combine(root, assetPath);
        }

        /// <summary>
        /// Resuelve el idioma de la UI y configura <see cref="Loc"/> (D-018, Q7-A; arquitectura de la tanda 4, §1): el de desarrollo
        /// (<paramref name="devLanguage"/>, <c>af_lang</c>; solo con <paramref name="development"/>) y, si no vale, el automático.
        /// Hoy el idioma guardado y el del teléfono aún no llegan (L3), y es-419 es el único idioma con la UI completa: la
        /// resolución automática da es-419 (notas de la tanda 4, punto 10). <paramref name="development"/> es también
        /// <c>showUnapproved</c>: enseña lo no aprobado, marca las claves que faltan y permite en-XA.
        /// </summary>
        public static LocResolution Configure(LocCatalog catalog, string? devLanguage, bool development)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            LocResolution resolution = LocLocale.Resolve(catalog, development ? devLanguage : null, null, null, development);
            Loc.Configure(catalog, resolution.Locale, development);
            return resolution;
        }
    }
}
