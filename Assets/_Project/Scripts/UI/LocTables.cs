#nullable enable
using System;
#if UNITY_EDITOR
using System.IO;
#endif
using AF.Localization;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Las tablas de textos del juego (D-018; 08 §4.7; contrato 32), <c>Assets/_Project/Data/Loc/strings.csv</c> (UI) y
    /// <c>facts.csv</c> (textos largos), y cómo se configura <see cref="Loc"/> con ellas: <see cref="GameBootstrap"/> las recibe
    /// como TextAsset de la escena (<c>GameSceneBuilder</c>) y las lee con <see cref="FromAssetsOrEmpty"/>, que nunca lanza; en
    /// el Editor, los tests y las herramientas las leen del disco (<c>FromProjectFiles</c>, que no va en los builds), en UTF-8
    /// estricto.
    /// </summary>
    public static class LocTables
    {
        /// <summary>Ruta de la tabla de UI en el proyecto.</summary>
        public const string StringsAssetPath = "Assets/_Project/Data/Loc/strings.csv";

        /// <summary>Ruta de la tabla de textos largos en el proyecto.</summary>
        public const string FactsAssetPath = "Assets/_Project/Data/Loc/facts.csv";

        /// <summary>Aviso sin la tabla de UI: el juego arranca sin textos en es-419.</summary>
        public const string MissingStringsWarning = "faltan las tablas de textos (TextAsset): la UI irá sin textos en es-419. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.";

        /// <summary>Aviso sin la tabla de textos largos: la UI va con su tabla y sin datos de fauna.</summary>
        public const string MissingFactsWarning = "falta la tabla de textos largos (facts.csv): sin datos de fauna. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.";

        /// <summary>
        /// Lee las tablas de la escena sin lanzar nunca (D-018; tarea L2b, punto 4: «si faltan los TextAsset, aviso y respaldo
        /// es-419»): ver <see cref="FromTextsOrEmpty"/>. Un TextAsset destruido cuenta como que falta (el == de Unity).
        /// </summary>
        public static LocCatalog FromAssetsOrEmpty(TextAsset? strings, TextAsset? facts, out string? warning, out string? error)
        {
            return FromTextsOrEmpty(strings != null ? strings.text : null, facts != null ? facts.text : null, out warning, out error);
        }

        /// <summary>
        /// Lee las dos tablas desde su texto sin lanzar nunca, para que el juego arranque igual con la escena sin tablas o con un
        /// CSV roto: en desarrollo, cada texto que falte se ve como «[clave]» y en release, vacío; nunca en otro idioma ni con
        /// una excepción en <c>Start</c> que deje el juego sin UI. Quien llama registra <paramref name="warning"/> (falta una
        /// tabla) y <paramref name="error"/> (una tabla no es válida); pueden venir los dos.
        /// <list type="bullet">
        /// <item>Sin <paramref name="stringsCsv"/>: <see cref="LocCatalog.Empty"/> (solo es-419, sin textos) y
        /// <see cref="MissingStringsWarning"/>.</item>
        /// <item><paramref name="stringsCsv"/> no válida: <see cref="LocCatalog.Empty"/> y el error, con el archivo y la
        /// línea.</item>
        /// <item>Sin <paramref name="factsCsv"/>: la UI con su tabla, sin textos largos, y <see cref="MissingFactsWarning"/>.</item>
        /// <item><paramref name="factsCsv"/> no válida (o con una clave que ya está en <c>strings.csv</c>): la UI con su tabla,
        /// sin textos largos, y el error.</item>
        /// </list>
        /// </summary>
        public static LocCatalog FromTextsOrEmpty(string? stringsCsv, string? factsCsv, out string? warning, out string? error)
        {
            warning = null;
            error = null;
            if (stringsCsv == null)
            {
                warning = MissingStringsWarning;
                return LocCatalog.Empty;
            }

            if (factsCsv == null)
            {
                warning = MissingFactsWarning;
            }

            LocTable strings;
            try
            {
                strings = LocTable.Parse(stringsCsv, LocCatalog.StringsFile);
            }
            catch (LocCsvException e)
            {
                error = "la tabla de textos no es válida: " + e.Message + ". La UI irá sin textos en es-419.";
                return LocCatalog.Empty;
            }

            if (factsCsv == null)
            {
                return new LocCatalog(strings);
            }

            try
            {
                return new LocCatalog(strings, LocTable.Parse(factsCsv, LocCatalog.FactsFile));
            }
            catch (LocCsvException e)
            {
                error = "la tabla de textos largos no es válida: " + e.Message + ". La UI va con su tabla y sin datos de fauna.";
                return new LocCatalog(strings);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Lee las dos tablas del disco del proyecto (solo en el Editor, para los tests y las herramientas:
        /// <see cref="Application.dataPath"/> es <c>Assets/</c>; en un player no existe), en UTF-8 estricto
        /// (<see cref="LocCsv.Decode"/>: con BOM o con bytes que no son UTF-8, error con la línea).
        /// </summary>
        public static LocCatalog FromProjectFiles()
        {
            return LocCatalog.Parse(ReadProjectFile(StringsAssetPath, LocCatalog.StringsFile), ReadProjectFile(FactsAssetPath, LocCatalog.FactsFile));
        }

        /// <summary>Texto de la tabla <paramref name="assetPath"/> («Assets/…») del proyecto, en UTF-8 estricto (solo en el Editor).</summary>
        public static string ReadProjectFile(string assetPath, string file)
        {
            return LocCsv.Decode(File.ReadAllBytes(ProjectPath(assetPath)), file);
        }

        /// <summary>Ruta en disco de <paramref name="assetPath"/> («Assets/…»; solo en el Editor).</summary>
        public static string ProjectPath(string assetPath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            return Path.Combine(root, assetPath);
        }
#endif

        /// <summary>
        /// Resuelve el idioma de la UI y configura <see cref="Loc"/> sin idioma guardado ni del teléfono: como
        /// <see cref="Configure(LocCatalog, string, string, string, bool)"/> con <c>settings.language</c> = auto y sin etiqueta del
        /// teléfono (cifras de la región del idioma: es-419 → es-CO [P]).
        /// </summary>
        public static LocResolution Configure(LocCatalog catalog, string? devLanguage, bool development)
        {
            return Configure(catalog, devLanguage, null, null, development);
        }

        /// <summary>
        /// Resuelve el idioma de la UI y configura <see cref="Loc"/> (D-018, Q7-A y Q8-A; arquitectura de la tanda 4, §1; tarea
        /// L3): el de desarrollo (<paramref name="devLanguage"/>, <c>af_lang</c>; solo con <paramref name="development"/>) &gt; el
        /// guardado (<paramref name="savedLanguage"/>, <c>settings.language</c>; <c>auto</c> no cuenta) &gt; el del teléfono
        /// (<paramref name="deviceTag"/>, de <c>IDeviceLocale</c>) &gt; en, y solo un idioma con la UI completa (si no, es-419 [P]:
        /// mientras EN y PT-BR no estén completos, el automático se queda en es-419; notas de la tanda 4, punto 10). Las cifras van
        /// con el idioma de la UI y, en español, con el país de la región de <paramref name="deviceTag"/> (aquí no hay país de Play;
        /// observación 22 de la jornada 6; <see cref="Configure(LocCatalog, string, string, string, bool, string)"/>). <paramref name="development"/> es también <c>showUnapproved</c>: enseña lo no aprobado,
        /// marca las claves que faltan y permite en-XA.
        /// </summary>
        public static LocResolution Configure(LocCatalog catalog, string? devLanguage, string? savedLanguage, string? deviceTag, bool development)
        {
            return Configure(catalog, devLanguage, savedLanguage, deviceTag, development, null);
        }

        /// <summary>
        /// Como <see cref="Configure(LocCatalog, string, string, string, bool)"/>, con el país de la cuenta de Play
        /// (<paramref name="playCountry"/>; D-057). Las cifras siguen al idioma de la UI y, en español, al país
        /// (<see cref="LocLocale.NumberCountry"/>: el de Play y, sin él, la región del teléfono; observación 22 de la jornada 6,
        /// decisión del dueño del 2026-10-30): en «1,000», pt-BR «1.000» y es-419 según el país (es-CO [P] si no se sabe). La región
        /// del teléfono ya no fija los separadores por sí sola: con el teléfono en en-US y la UI en pt-BR, «1.000».
        /// </summary>
        public static LocResolution Configure(LocCatalog catalog, string? devLanguage, string? savedLanguage, string? deviceTag, bool development, string? playCountry)
        {
            LocResolution resolution = Resolve(catalog, devLanguage, savedLanguage, deviceTag, development);
            Loc.Configure(catalog, resolution.Locale, development, null, LocLocale.NumberCountry(playCountry, deviceTag));
            return resolution;
        }

        /// <summary>
        /// El idioma que tocaría con estas fuentes, sin tocar <see cref="Loc"/> (el mismo orden que
        /// <see cref="Configure(LocCatalog, string, string, string, bool)"/>): para decidir en Ajustes si elegir un idioma cambia la UI.
        /// </summary>
        public static LocResolution Resolve(LocCatalog catalog, string? devLanguage, string? savedLanguage, string? deviceTag, bool development)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            return LocLocale.Resolve(catalog, development ? devLanguage : null, savedLanguage, deviceTag, development);
        }
    }

    /// <summary>
    /// Texto de las dos tablas que sustituye a los TextAsset de la escena (<see cref="LaunchOptions.LocTablesOverride"/>): solo
    /// tests, para arrancar sin tablas o con una rota (tarea L2b, punto 4). Null en una tabla = falta.
    /// </summary>
    public sealed class LocTableTexts
    {
        /// <summary>Tablas de textos con el texto de <c>strings.csv</c> y de <c>facts.csv</c> (null = falta).</summary>
        public LocTableTexts(string? strings, string? facts)
        {
            Strings = strings;
            Facts = facts;
        }

        /// <summary>Texto de <c>strings.csv</c>, o null si falta.</summary>
        public string? Strings { get; }

        /// <summary>Texto de <c>facts.csv</c>, o null si falta.</summary>
        public string? Facts { get; }
    }
}
