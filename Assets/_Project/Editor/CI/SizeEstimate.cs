#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>
    /// Estimación temprana del peso del arte encargado (tarea B0): kits de mundo, héroes, Mapa del río, ambientes, perezoso y
    /// fichas de Frutas del río, sin esperar a una medida del AAB (M1.8: «los kits, en paralelo con los héroes en cuanto B0 dé
    /// la estimación de peso»). La medida real es de M5.2 y B2 con <c>ci/size-gate.ps1</c>.
    ///
    /// Cómo mide: por cada medida distinta de cada familia crea un PNG de relleno en <see cref="TempFolder"/> (ignorada por
    /// git), lo importa como Sprite con el formato de Android de su familia (ASTC 8×8 en los kits, 6×6 en los héroes y 4×4 en la
    /// UI: propuesta del Mapa §10.7 y §12; README de <c>Assets/_Project/Art/Generated/</c>, sin mipmaps), lee su tamaño en
    /// almacenamiento con la API del Editor (lo que enseña el inspector) y al terminar borra la carpeta con sus <c>.meta</c>.
    /// ASTC es de tasa fija, así que una pieza basta por medida: el tamaño solo depende de los píxeles y del bloque. Si la
    /// plataforma activa no es Android, el Editor no importa en ASTC y la cifra sale del recuento de bloques (lo dice la tabla).
    ///
    /// Escribe una tabla por familia en dos escenarios: piezas a su medida en el build (1×, [P] del README) y a su medida de
    /// entrega (2× en kits y héroes; cómo se pasa de 2× a 1× lo decide M5.2), y el total frente al AAB de hoy
    /// (<see cref="AabTodayMB"/>) y a la compuerta de <see cref="GateMB"/> (08 §6.1; CLAUDE.md). Es una cota superior: el AAB
    /// comprime los datos ASTC y la medida de Play (bundletool) suele ser menor.
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/size-estimate.log --no-tail --non-interactive --
    ///             -nographics -buildTarget Android -executeMethod AF.EditorTools.SizeEstimate.Run
    ///             [-afSizeFamilies kits,heroes,mapa] [-afSizeReport Logs/size-estimate.md] [-afSizeAabMB 35]
    /// Familias (<c>-afSizeFamilies</c>, separadas por comas; todas si se omite): kits, heroes, mapa, ambiente, sloth, fichas
    /// (el id del perezoso protagonista no lleva su nombre: glosario §11).
    /// Informe: <see cref="DefaultReportPath"/> (Logs/ está ignorada por git) y el log de Unity.
    ///
    /// Códigos de salida (solo en batchmode):
    ///   0  estimación hecha y dentro de la compuerta
    ///   2  argumentos incorrectos: familia desconocida o repetida, opción sin valor, opción -afSize* desconocida o cifra mala
    ///   3  el AAB de hoy más la estimación (el peor escenario) supera la compuerta de 80 MB
    ///   4  falló la importación o la medida, o hubo una excepción (lee el log)
    /// En el Editor interactivo (menú "Amazon Fill/CI") solo escribe en la consola y en el informe.
    /// </summary>
    public static class SizeEstimate
    {
        /// <summary>Carpeta temporal de los PNG de relleno: ignorada por git y borrada al terminar.</summary>
        public const string TempFolder = "Assets/_SizeEstimateTemp";

        /// <summary>Informe por defecto (Logs/ está ignorada por git).</summary>
        public const string DefaultReportPath = "Logs/size-estimate.md";

        /// <summary>AAB de humo de hoy en MB decimales (CLAUDE.md, compuerta del día 1: «AAB de 35 MB»).</summary>
        public const double AabTodayMB = 35.0;

        /// <summary>Compuerta del AAB en MB decimales (08 §6.1; <c>ci/size-gate.ps1</c>: 1 MB = 1.000.000 bytes).</summary>
        public const double GateMB = 80.0;

        internal const int ExitOk = 0;
        internal const int ExitArguments = 2;
        internal const int ExitOverGate = 3;
        internal const int ExitFailed = 4;

        internal const string FamiliesOption = "-afSizeFamilies";
        internal const string ReportOption = "-afSizeReport";
        internal const string AabOption = "-afSizeAabMB";
        private const string OptionPrefix = "-afSize";

        private const string LogPrefix = "[AF.SizeEstimate] ";
        private const double BytesPerMiB = 1024.0 * 1024.0;
        private const double BytesPerKiB = 1024.0;
        private const double BytesPerMB = 1000000.0;

        // Ajustes de importación pendientes de los PNG de relleno (ruta → formato de Android), que aplica TempImporter.
        private static readonly Dictionary<string, AstcBlock> Pending = new Dictionary<string, AstcBlock>(StringComparer.Ordinal);

        /// <summary>
        /// Las familias del encargo con sus medidas (README de <c>Assets/_Project/Art/Generated/</c>, «Ranuras reservadas» y
        /// «Para estimar el peso»; glosario §11; propuesta del Mapa §10.7).
        /// </summary>
        public static readonly IReadOnlyList<ArtFamily> Families = new[]
        {
            new ArtFamily("kits", "Kits de mundo (bg_kit_w01…w09)", new[]
            {
                // 9 kits en la 1.0: w01-w05 en 1-200 y w06-w09 en 201-700. Entrega 2048² (2×); en el build 1024² [P].
                new ArtPiece("bg_kit_w{nn}", 9, 1024, 1024, 2048, 2048, AstcBlock.Astc8x8),
            }),
            new ArtFamily("heroes", "Héroes de los sitios (bg_hero_<sitio>)", new[]
            {
                // 14 sitios de 1-200 más la reserva bg_hero_casa_de_fierro. Entrega 1440 × 480 (2×); en el build 720 × 240 [P].
                new ArtPiece("bg_hero_<sitio>", 15, 720, 240, 1440, 480, AstcBlock.Astc6x6),
            }),
            new ArtFamily("mapa", "Mapa del río (map_*)", new[]
            {
                new ArtPiece("map_tile_*", 5, 512, 512, 512, 512, AstcBlock.Astc6x6),
                // Las piezas de UI del Mapa cuentan como un atlas de 1024² con las siluetas de las muestras horneadas (§10.7).
                new ArtPiece("atlas de UI del Mapa", 1, 1024, 1024, 1024, 1024, AstcBlock.Astc4x4),
            }),
            new ArtFamily("ambiente", "Ambientes de los sitios (amb_*)", new[]
            {
                new ArtPiece("amb_*", 3, 256, 256, 256, 256, AstcBlock.Astc4x4),
            }),
            new ArtFamily("sloth", "Perezoso protagonista (fauna_bradypus_variegatus_* y arbol_embauba)", new[]
            {
                new ArtPiece("fauna_bradypus_variegatus_partes", 1, 1024, 1024, 1024, 1024, AstcBlock.Astc4x4),
                new ArtPiece("fauna_bradypus_variegatus_retrato", 1, 512, 512, 512, 512, AstcBlock.Astc4x4),
                new ArtPiece("arbol_embauba", 1, 1024, 256, 1024, 256, AstcBlock.Astc4x4),
            }),
            new ArtFamily("fichas", "Fichas de Frutas del río (ficha_<tinte>)", new[]
            {
                new ArtPiece("ficha_<tinte>", 14, 1024, 640, 1024, 640, AstcBlock.Astc6x6),
            }),
        };

        /// <summary>Punto de entrada en batchmode (<c>-executeMethod AF.EditorTools.SizeEstimate.Run</c>).</summary>
        [MenuItem("Amazon Fill/CI/Estimar el peso del arte encargado")]
        public static void Run()
        {
            int code;
            try
            {
                code = Execute(Environment.GetCommandLineArgs(), null);
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = ExitFailed;
            }

            if (code == ExitOk)
            {
                Debug.Log(LogPrefix + "OK.");
            }
            else
            {
                Debug.LogError(LogPrefix + "FALLO (código " + code.ToString(CultureInfo.InvariantCulture) + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        /// <summary>
        /// Lee las opciones de <paramref name="commandLine"/>, estima, escribe el informe y devuelve el código de salida.
        /// <paramref name="measure"/> sustituye a la importación real (costura de los tests); null importa de verdad.
        /// </summary>
        internal static int Execute(string[] commandLine, Func<int, int, AstcBlock, Measurement>? measure)
        {
            if (!TryParseOptions(commandLine, out Options? parsed, out string error) || parsed == null)
            {
                Debug.LogError(LogPrefix + error);
                return ExitArguments;
            }

            Options options = parsed;
            EstimateResult result;
            try
            {
                result = measure != null ? Estimate(options.Families, measure) : EstimateByImport(options.Families);
            }
            catch (Exception e) when (e is InvalidOperationException || e is IOException)
            {
                Debug.LogError(LogPrefix + e.Message);
                return ExitFailed;
            }

            string report = Format(result, options.AabMB);
            Debug.Log(LogPrefix + "\n" + report);
            string? folder = Path.GetDirectoryName(options.ReportPath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(options.ReportPath, report, new UTF8Encoding(false));
            Debug.Log(LogPrefix + "informe en " + options.ReportPath);

            double worstMB = options.AabMB + (Math.Max(result.BuildBytes, result.DeliveryBytes) / BytesPerMB);
            if (worstMB > GateMB)
            {
                Debug.LogError(LogPrefix + "el AAB de hoy más la estimación (" + Number(worstMB, "0.0") + " MB) supera la compuerta de "
                    + Number(GateMB, "0") + " MB (08 §6.1).");
                return ExitOverGate;
            }

            return ExitOk;
        }

        /// <summary>Opciones de la línea de órdenes; false y <paramref name="error"/> si alguna es incorrecta.</summary>
        internal static bool TryParseOptions(string[] commandLine, out Options? options, out string error)
        {
            if (commandLine == null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            options = null;
            string? families = null;
            string? report = null;
            string? aab = null;
            for (int i = 0; i < commandLine.Length; i++)
            {
                string arg = commandLine[i];
                if (!arg.StartsWith(OptionPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (arg != FamiliesOption && arg != ReportOption && arg != AabOption)
                {
                    error = "opción desconocida " + arg + " (válidas: " + FamiliesOption + ", " + ReportOption + ", " + AabOption + ").";
                    return false;
                }

                if (i + 1 >= commandLine.Length || commandLine[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    error = "falta el valor de " + arg + ".";
                    return false;
                }

                string value = commandLine[++i];
                if ((arg == FamiliesOption && families != null) || (arg == ReportOption && report != null) || (arg == AabOption && aab != null))
                {
                    error = "opción repetida " + arg + ".";
                    return false;
                }

                if (arg == FamiliesOption)
                {
                    families = value;
                }
                else if (arg == ReportOption)
                {
                    report = value;
                }
                else
                {
                    aab = value;
                }
            }

            List<ArtFamily> selected;
            if (families == null)
            {
                selected = new List<ArtFamily>(Families);
            }
            else if (!TryParseFamilies(families, out selected, out error))
            {
                return false;
            }

            double aabMB = AabTodayMB;
            if (aab != null && (!double.TryParse(aab, NumberStyles.Float, CultureInfo.InvariantCulture, out aabMB) || aabMB < 0.0
                || double.IsNaN(aabMB) || double.IsInfinity(aabMB)))
            {
                error = AabOption + " necesita una cifra en MB, no negativa y con punto decimal (recibido: '" + aab + "').";
                return false;
            }

            options = new Options(selected, report ?? DefaultReportPath, aabMB);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Las familias de <paramref name="csv"/> (ids separados por comas, en el orden dado); false si alguna es desconocida
        /// o se repite, o si la lista queda vacía.
        /// </summary>
        internal static bool TryParseFamilies(string csv, out List<ArtFamily> families, out string error)
        {
            if (csv == null)
            {
                throw new ArgumentNullException(nameof(csv));
            }

            families = new List<ArtFamily>();
            string[] ids = csv.Split(',');
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i].Trim();
                if (id.Length == 0)
                {
                    continue;
                }

                ArtFamily? family = Find(id);
                if (family == null)
                {
                    error = "familia desconocida '" + id + "' (válidas: " + KnownIds() + ").";
                    return false;
                }

                if (families.Contains(family))
                {
                    error = "familia repetida '" + id + "'.";
                    return false;
                }

                families.Add(family);
            }

            if (families.Count == 0)
            {
                error = "lista de familias vacía (válidas: " + KnownIds() + ").";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Bytes de una textura ASTC de <paramref name="width"/> × <paramref name="height"/> sin mipmaps (16 B por bloque).</summary>
        public static long AstcBytes(int width, int height, AstcBlock block)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height), "La medida tiene que ser positiva.");
            }

            int side = BlockSide(block);
            long blocksX = (width + side - 1) / side;
            long blocksY = (height + side - 1) / side;
            return blocksX * blocksY * 16L;
        }

        /// <summary>
        /// Estima <paramref name="families"/> con <paramref name="measure"/>, que mide una pieza de una medida y un formato (una
        /// sola vez por combinación).
        /// </summary>
        internal static EstimateResult Estimate(IReadOnlyList<ArtFamily> families, Func<int, int, AstcBlock, Measurement> measure)
        {
            var cache = new Dictionary<string, Measurement>(StringComparer.Ordinal);
            var rows = new List<EstimateRow>();
            for (int f = 0; f < families.Count; f++)
            {
                ArtFamily family = families[f];
                for (int p = 0; p < family.Pieces.Count; p++)
                {
                    ArtPiece piece = family.Pieces[p];
                    Measurement build = Cached(cache, piece.BuildWidth, piece.BuildHeight, piece.Block, measure);
                    Measurement delivery = Cached(cache, piece.DeliveryWidth, piece.DeliveryHeight, piece.Block, measure);
                    rows.Add(new EstimateRow(family, piece, build, delivery));
                }
            }

            return new EstimateResult(rows);
        }

        /// <summary>Tabla Markdown por familia, con los totales frente al AAB de hoy (<paramref name="aabMB"/>) y a la compuerta.</summary>
        internal static string Format(EstimateResult result, double aabMB)
        {
            var sb = new StringBuilder();
            sb.Append("# Estimación del peso del arte encargado (B0)\n\n");
            sb.Append("Tamaño en almacenamiento de las texturas en Android, sin mipmaps. «Build» = medida en el build (1×, [P]); ")
                .Append("«entrega» = medida de entrega (2× en kits y héroes). Cota superior: el AAB comprime los datos ASTC.\n\n");
            sb.Append("| Familia | Pieza | N | Formato | Build (px) | KiB/pieza | Total build (MiB) | Entrega (px) | KiB/pieza | Total entrega (MiB) | Medida |\n");
            sb.Append("|---|---|---|---|---|---|---|---|---|---|---|\n");
            for (int i = 0; i < result.Rows.Count; i++)
            {
                EstimateRow row = result.Rows[i];
                ArtPiece piece = row.Piece;
                string method = row.Build.Method == row.Delivery.Method ? row.Build.Method : row.Build.Method + " / " + row.Delivery.Method;
                sb.Append("| ").Append(row.Family.Id)
                    .Append(" | ").Append(piece.Name)
                    .Append(" | ").Append(piece.Count.ToString(CultureInfo.InvariantCulture))
                    .Append(" | ").Append(BlockName(piece.Block))
                    .Append(" | ").Append(Size(piece.BuildWidth, piece.BuildHeight))
                    .Append(" | ").Append(Number(row.Build.Bytes / BytesPerKiB, "0.0"))
                    .Append(" | ").Append(Number(row.BuildBytes / BytesPerMiB, "0.00"))
                    .Append(" | ").Append(Size(piece.DeliveryWidth, piece.DeliveryHeight))
                    .Append(" | ").Append(Number(row.Delivery.Bytes / BytesPerKiB, "0.0"))
                    .Append(" | ").Append(Number(row.DeliveryBytes / BytesPerMiB, "0.00"))
                    .Append(" | ").Append(method)
                    .Append(" |\n");
            }

            sb.Append("\n## Por familia\n\n");
            sb.Append("| Familia | Piezas | Build (MiB) | Entrega (MiB) |\n");
            sb.Append("|---|---|---|---|\n");
            var seen = new List<ArtFamily>();
            for (int i = 0; i < result.Rows.Count; i++)
            {
                ArtFamily family = result.Rows[i].Family;
                if (seen.Contains(family))
                {
                    continue;
                }

                seen.Add(family);
                long build = 0;
                long delivery = 0;
                int count = 0;
                for (int j = 0; j < result.Rows.Count; j++)
                {
                    if (result.Rows[j].Family == family)
                    {
                        build += result.Rows[j].BuildBytes;
                        delivery += result.Rows[j].DeliveryBytes;
                        count += result.Rows[j].Piece.Count;
                    }
                }

                sb.Append("| ").Append(family.Title)
                    .Append(" | ").Append(count.ToString(CultureInfo.InvariantCulture))
                    .Append(" | ").Append(Number(build / BytesPerMiB, "0.00"))
                    .Append(" | ").Append(Number(delivery / BytesPerMiB, "0.00"))
                    .Append(" |\n");
            }

            sb.Append("| **Total** | ").Append(result.PieceCount.ToString(CultureInfo.InvariantCulture))
                .Append(" | **").Append(Number(result.BuildBytes / BytesPerMiB, "0.00"))
                .Append("** | **").Append(Number(result.DeliveryBytes / BytesPerMiB, "0.00")).Append("** |\n");

            double buildMB = result.BuildBytes / BytesPerMB;
            double deliveryMB = result.DeliveryBytes / BytesPerMB;
            sb.Append("\n## Frente al AAB y a la compuerta\n\n");
            sb.Append("| Escenario | Arte (MB) | AAB de hoy (MB) | AAB estimado (MB) | Compuerta (MB) | Margen (MB) |\n");
            sb.Append("|---|---|---|---|---|---|\n");
            AppendGateRow(sb, "Build (1×)", buildMB, aabMB);
            AppendGateRow(sb, "Entrega (2×)", deliveryMB, aabMB);
            sb.Append("\nMB decimales (1 MB = 1.000.000 bytes, como `ci/size-gate.ps1`). La medida real es de M5.2 y B2.\n");
            return sb.ToString();
        }

        private static void AppendGateRow(StringBuilder sb, string scenario, double artMB, double aabMB)
        {
            double total = aabMB + artMB;
            sb.Append("| ").Append(scenario)
                .Append(" | ").Append(Number(artMB, "0.00"))
                .Append(" | ").Append(Number(aabMB, "0.0"))
                .Append(" | ").Append(Number(total, "0.00"))
                .Append(" | ").Append(Number(GateMB, "0"))
                .Append(" | ").Append(Number(GateMB - total, "0.00"))
                .Append(" |\n");
        }

        // Importación real: un PNG de relleno por medida y formato en TempFolder, que se borra siempre al terminar.
        private static EstimateResult EstimateByImport(IReadOnlyList<ArtFamily> families)
        {
            DeleteTempFolder();
            try
            {
                AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
                return Estimate(families, MeasureByImport);
            }
            finally
            {
                Pending.Clear();
                DeleteTempFolder();
            }
        }

        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }

            // Restos de una ejecución cortada a medias que el AssetDatabase no conozca.
            if (Directory.Exists(TempFolder))
            {
                Directory.Delete(TempFolder, true);
            }

            if (File.Exists(TempFolder + ".meta"))
            {
                File.Delete(TempFolder + ".meta");
            }
        }

        private static Measurement MeasureByImport(int width, int height, AstcBlock block)
        {
            string path = TempFolder + "/relleno_" + width.ToString(CultureInfo.InvariantCulture) + "x"
                + height.ToString(CultureInfo.InvariantCulture) + "_" + BlockName(block).Replace(' ', '_').Replace('×', 'x') + ".png";
            var fill = new Texture2D(width, height, TextureFormat.RGBA32, false);
            byte[] png;
            try
            {
                var pixels = new Color32[width * height];
                var color = new Color32(64, 120, 96, 255);
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }

                fill.SetPixels32(pixels);
                fill.Apply(false, false);
                png = fill.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fill);
            }

            File.WriteAllBytes(path, png);
            Pending[path] = block;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("no se pudo importar " + path + " como textura.");
            }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.format != ImporterFormat(block))
            {
                throw new InvalidOperationException(path + ": el ajuste de Android no quedó en " + BlockName(block) + " (" + android.format + ").");
            }

            Texture2D? texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException("no se pudo cargar " + path + " tras importarlo.");
            }

            if (texture.width != width || texture.height != height)
            {
                throw new InvalidOperationException(path + " se importó a " + Size(texture.width, texture.height) + " y no a " + Size(width, height) + ".");
            }

            long blocks = AstcBytes(width, height, block);
            if (texture.format != RuntimeFormat(block))
            {
                // Otra plataforma activa: el Editor no tiene la textura en ASTC; se cuenta por bloques.
                return new Measurement(blocks, "bloques (plataforma " + EditorUserBuildSettings.activeBuildTarget + ")");
            }

            long? storage = EditorStorageBytes(texture);
            if (storage == null)
            {
                return new Measurement(blocks, "bloques (sin TextureUtil)");
            }

            if (storage.Value != blocks)
            {
                Debug.LogWarning(LogPrefix + path + ": el Editor da " + storage.Value.ToString(CultureInfo.InvariantCulture)
                    + " B y el recuento de bloques " + blocks.ToString(CultureInfo.InvariantCulture) + " B; manda el Editor.");
            }

            return new Measurement(storage.Value, "Editor (Android)");
        }

        // Tamaño en almacenamiento que enseña el inspector (UnityEditor.TextureUtil es interna; null si cambia).
        private static long? EditorStorageBytes(Texture texture)
        {
            Type? util = typeof(Editor).Assembly.GetType("UnityEditor.TextureUtil");
            MethodInfo? method = util?.GetMethod("GetStorageMemorySizeLong", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Texture) }, null);
            if (method == null)
            {
                return null;
            }

            object? value = method.Invoke(null, new object[] { texture });
            return value is long bytes ? bytes : (long?)null;
        }

        private static Measurement Cached(Dictionary<string, Measurement> cache, int width, int height, AstcBlock block,
            Func<int, int, AstcBlock, Measurement> measure)
        {
            string key = Size(width, height) + " " + BlockName(block);
            if (!cache.TryGetValue(key, out Measurement found))
            {
                found = measure(width, height, block);
                if (found.Bytes <= 0)
                {
                    throw new InvalidOperationException("medida no válida de " + key + ": " + found.Bytes.ToString(CultureInfo.InvariantCulture) + " B.");
                }

                cache.Add(key, found);
            }

            return found;
        }

        private static ArtFamily? Find(string id)
        {
            for (int i = 0; i < Families.Count; i++)
            {
                if (string.Equals(Families[i].Id, id, StringComparison.Ordinal))
                {
                    return Families[i];
                }
            }

            return null;
        }

        private static string KnownIds()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Families.Count; i++)
            {
                sb.Append(i == 0 ? string.Empty : ", ").Append(Families[i].Id);
            }

            return sb.ToString();
        }

        private static int BlockSide(AstcBlock block)
        {
            switch (block)
            {
                case AstcBlock.Astc4x4:
                    return 4;
                case AstcBlock.Astc6x6:
                    return 6;
                case AstcBlock.Astc8x8:
                    return 8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(block), block, "Bloque ASTC no previsto.");
            }
        }

        private static string BlockName(AstcBlock block)
        {
            int side = BlockSide(block);
            return "ASTC " + side.ToString(CultureInfo.InvariantCulture) + "×" + side.ToString(CultureInfo.InvariantCulture);
        }

        private static TextureImporterFormat ImporterFormat(AstcBlock block)
        {
            switch (block)
            {
                case AstcBlock.Astc4x4:
                    return TextureImporterFormat.ASTC_4x4;
                case AstcBlock.Astc6x6:
                    return TextureImporterFormat.ASTC_6x6;
                case AstcBlock.Astc8x8:
                    return TextureImporterFormat.ASTC_8x8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(block), block, "Bloque ASTC no previsto.");
            }
        }

        private static TextureFormat RuntimeFormat(AstcBlock block)
        {
            switch (block)
            {
                case AstcBlock.Astc4x4:
                    return TextureFormat.ASTC_4x4;
                case AstcBlock.Astc6x6:
                    return TextureFormat.ASTC_6x6;
                case AstcBlock.Astc8x8:
                    return TextureFormat.ASTC_8x8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(block), block, "Bloque ASTC no previsto.");
            }
        }

        private static string Size(int width, int height)
        {
            return width.ToString(CultureInfo.InvariantCulture) + " × " + height.ToString(CultureInfo.InvariantCulture);
        }

        // Cifras con coma decimal, como el resto de la documentación.
        private static string Number(double value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');
        }

        /// <summary>Bloque ASTC de una familia (propuesta del Mapa §10.7).</summary>
        public enum AstcBlock
        {
            /// <summary>4×4 (8 bpp): UI, fauna y piezas del Mapa.</summary>
            Astc4x4,

            /// <summary>6×6 (3,56 bpp): héroes, terreno del Mapa y fichas.</summary>
            Astc6x6,

            /// <summary>8×8 (2 bpp): kits de mundo.</summary>
            Astc8x8,
        }

        /// <summary>Una pieza encargada: cuántas hay, su medida en el build y en la entrega, y su bloque ASTC.</summary>
        public sealed class ArtPiece
        {
            /// <summary>Crea la pieza.</summary>
            public ArtPiece(string name, int count, int buildWidth, int buildHeight, int deliveryWidth, int deliveryHeight, AstcBlock block)
            {
                Name = name;
                Count = count;
                BuildWidth = buildWidth;
                BuildHeight = buildHeight;
                DeliveryWidth = deliveryWidth;
                DeliveryHeight = deliveryHeight;
                Block = block;
            }

            /// <summary>Ranura o patrón de ranuras (glosario §11).</summary>
            public string Name { get; }

            /// <summary>Número de piezas de esta medida.</summary>
            public int Count { get; }

            /// <summary>Ancho en el build (px).</summary>
            public int BuildWidth { get; }

            /// <summary>Alto en el build (px).</summary>
            public int BuildHeight { get; }

            /// <summary>Ancho de entrega (px).</summary>
            public int DeliveryWidth { get; }

            /// <summary>Alto de entrega (px).</summary>
            public int DeliveryHeight { get; }

            /// <summary>Bloque ASTC en Android.</summary>
            public AstcBlock Block { get; }
        }

        /// <summary>Una familia de arte (<c>-afSizeFamilies</c>).</summary>
        public sealed class ArtFamily
        {
            /// <summary>Crea la familia.</summary>
            public ArtFamily(string id, string title, IReadOnlyList<ArtPiece> pieces)
            {
                Id = id;
                Title = title;
                Pieces = pieces;
            }

            /// <summary>Id ASCII de la línea de órdenes.</summary>
            public string Id { get; }

            /// <summary>Nombre en el informe.</summary>
            public string Title { get; }

            /// <summary>Sus piezas.</summary>
            public IReadOnlyList<ArtPiece> Pieces { get; }
        }

        /// <summary>Tamaño de una pieza y cómo se obtuvo.</summary>
        internal readonly struct Measurement
        {
            public Measurement(long bytes, string method)
            {
                Bytes = bytes;
                Method = method;
            }

            public long Bytes { get; }

            public string Method { get; }
        }

        /// <summary>Una fila: una pieza de una familia, medida a las dos escalas.</summary>
        internal sealed class EstimateRow
        {
            public EstimateRow(ArtFamily family, ArtPiece piece, Measurement build, Measurement delivery)
            {
                Family = family;
                Piece = piece;
                Build = build;
                Delivery = delivery;
            }

            public ArtFamily Family { get; }

            public ArtPiece Piece { get; }

            public Measurement Build { get; }

            public Measurement Delivery { get; }

            public long BuildBytes => Build.Bytes * Piece.Count;

            public long DeliveryBytes => Delivery.Bytes * Piece.Count;
        }

        /// <summary>El resultado: filas y totales.</summary>
        internal sealed class EstimateResult
        {
            public EstimateResult(List<EstimateRow> rows)
            {
                Rows = rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    BuildBytes += rows[i].BuildBytes;
                    DeliveryBytes += rows[i].DeliveryBytes;
                    PieceCount += rows[i].Piece.Count;
                }
            }

            public IReadOnlyList<EstimateRow> Rows { get; }

            public long BuildBytes { get; }

            public long DeliveryBytes { get; }

            public int PieceCount { get; }
        }

        /// <summary>Opciones de la línea de órdenes.</summary>
        internal sealed class Options
        {
            public Options(IReadOnlyList<ArtFamily> families, string reportPath, double aabMB)
            {
                Families = families;
                ReportPath = reportPath;
                AabMB = aabMB;
            }

            public IReadOnlyList<ArtFamily> Families { get; }

            public string ReportPath { get; }

            public double AabMB { get; }
        }

        // Aplica a los PNG de relleno los ajustes de su familia antes de la primera importación (como GeneratedArt con las
        // ranuras): Sprite, sin mipmaps, sin reescalar y el ASTC de su familia en Android. La calidad Fast solo acorta la
        // compresión: ASTC es de tasa fija y el tamaño no cambia.
        internal sealed class TempImporter : AssetPostprocessor
        {
            private void OnPreprocessTexture()
            {
                if (!Pending.TryGetValue(assetPath, out AstcBlock block))
                {
                    return;
                }

                var importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    format = ImporterFormat(block),
                    maxTextureSize = 2048,
                    compressionQuality = (int)TextureCompressionQuality.Fast,
                });
            }
        }
    }
}
