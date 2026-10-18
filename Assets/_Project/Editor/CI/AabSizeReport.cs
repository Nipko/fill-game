#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Audio;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>
    /// Peso del AAB del juego por grupo (tarea B2; 08 §6.1): niveles, textos y datos, fuentes, arte, audio, shaders y materiales,
    /// escenas y scripts, paquetes y recursos integrados de Unity, a partir del contenido empaquetado del Build Report que deja
    /// <see cref="AudioAssetSetup.BuildGameAab"/>. Lo escribe en <see cref="ReportPath"/> junto al informe del audio.
    ///
    /// Las cifras son bytes empaquetados (datos serializados de Unity antes de la compresión del AAB), así que suman menos que el
    /// AAB: el resto es el código nativo (<c>libil2cpp.so</c>, <c>libunity.so</c>), el dex y los recursos de Android, que
    /// <c>ci/size-gate.ps1 -Breakdown</c> separa leyendo el propio AAB.
    /// </summary>
    public static class AabSizeReport
    {
        /// <summary>Informe por grupo (Logs/ está ignorada por git).</summary>
        public const string ReportPath = "Logs/aab-size.md";

        /// <summary>Grupos en el orden del informe; una entrada cae en el primero que la reconoce (<see cref="GroupOf"/>).</summary>
        public static readonly string[] Groups =
        {
            "Niveles (packs)",
            "Textos, Mapa y datos",
            "Fuentes",
            "Arte",
            "Audio",
            "Shaders y materiales",
            "Escenas y scripts",
            "Paquetes",
            "Recursos integrados de Unity",
            "Otros",
        };

        private const string LevelsFolder = "Assets/_Project/Data/Levels/";
        private const string DataFolder = "Assets/_Project/Data/";
        private const string ArtFolder = "Assets/_Project/Art/";
        private const string ShadersFolder = "Assets/_Project/Shaders/";
        private const string ScenesFolder = "Assets/_Project/Scenes/";
        private const string ScriptsFolder = "Assets/_Project/Scripts/";

        /// <summary>Índice en <see cref="Groups"/> del grupo de una entrada del Build Report, por su ruta y su tipo.</summary>
        public static int GroupOf(string sourcePath, Type? type)
        {
            string path = sourcePath ?? string.Empty;
            if (path.StartsWith(LevelsFolder, StringComparison.Ordinal))
            {
                return 0;
            }

            if (path.StartsWith(DataFolder, StringComparison.Ordinal))
            {
                return 1;
            }

            // Antes que el arte: el atlas de una fuente TextMeshPro es una textura dentro del asset de la fuente.
            if (path.IndexOf("/Fonts/", StringComparison.Ordinal) >= 0
                || path.IndexOf("Fonts & Materials/", StringComparison.Ordinal) >= 0
                || type == typeof(Font))
            {
                return 2;
            }

            if (path.StartsWith(AudioClipIds.Folder, StringComparison.Ordinal) || type == typeof(AudioClip))
            {
                return 4;
            }

            bool inAssets = path.StartsWith("Assets/", StringComparison.Ordinal);
            if (path.StartsWith(ArtFolder, StringComparison.Ordinal)
                || (inAssets && (type == typeof(Texture2D) || type == typeof(Sprite))))
            {
                return 3;
            }

            if (path.StartsWith(ShadersFolder, StringComparison.Ordinal)
                || (inAssets && (type == typeof(Shader) || type == typeof(Material))))
            {
                return 5;
            }

            if (path.StartsWith(ScenesFolder, StringComparison.Ordinal) || path.StartsWith(ScriptsFolder, StringComparison.Ordinal))
            {
                return 6;
            }

            if (path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return 7;
            }

            if (!inAssets)
            {
                return 8; // "Resources/unity_builtin_extra", "Library/unity default resources", entradas sin ruta…
            }

            return 9;
        }

        /// <summary>
        /// Tabla del peso por grupo: assets distintos (por ruta), MB empaquetados (1 MB = 1.000.000 bytes, como
        /// <c>ci/size-gate.ps1</c>) y porcentaje del total empaquetado, y el AAB al pie. Pública para los tests.
        /// </summary>
        public static string Report(IReadOnlyList<PackedEntry> contents, long aabBytes)
        {
            var bytes = new long[Groups.Length];
            var assets = new int[Groups.Length];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            long all = 0;
            for (int i = 0; i < contents.Count; i++)
            {
                PackedEntry entry = contents[i];
                int group = GroupOf(entry.SourcePath, entry.Type);
                bytes[group] += entry.PackedSize;
                all += entry.PackedSize;
                if (seen.Add(entry.SourcePath ?? string.Empty))
                {
                    assets[group]++;
                }
            }

            var sb = new StringBuilder();
            sb.Append("# Peso del AAB del juego por grupo (AF.EditorTools.AudioAssetSetup.BuildGameAab)\n\n");
            sb.Append("| Grupo | Assets | MB | % |\n| --- | ---: | ---: | ---: |\n");
            for (int g = 0; g < Groups.Length; g++)
            {
                double share = all > 0 ? 100.0 * bytes[g] / all : 0.0;
                sb.Append("| ").Append(Groups[g]).Append(" | ").Append(assets[g].ToString(CultureInfo.InvariantCulture))
                    .Append(" | ").Append(Mb(bytes[g])).Append(" | ")
                    .Append(share.ToString("0.0", CultureInfo.InvariantCulture)).Append(" |\n");
            }

            sb.Append("| **Datos empaquetados** | ").Append(seen.Count.ToString(CultureInfo.InvariantCulture)).Append(" | **")
                .Append(Mb(all)).Append("** | 100.0 |\n\n");
            sb.Append("AAB: ").Append(Mb(aabBytes)).Append(" MB (1 MB = 1.000.000 bytes). Los datos empaquetados van sin comprimir;")
                .Append(" el código nativo, el dex y los recursos de Android no salen aquí (ci/size-gate.ps1 -Breakdown).\n");
            return sb.ToString();
        }

        private static string Mb(long value)
        {
            return (value / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
