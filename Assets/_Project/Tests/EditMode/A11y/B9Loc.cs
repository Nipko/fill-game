#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Localization;
using AF.UI;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Tabla de textos de los tests de B9: la del proyecto (<c>strings.csv</c> y <c>facts.csv</c>) más las filas de
    /// <c>docs/dev/traspaso/spark-enganches/B9-strings.csv</c> que aún no estén en ella (antes de aplicar <c>B9-loc.diff</c>,
    /// todas; después, ninguna). Así los tests de la rama leen los textos de verdad sin tocar <c>strings.csv</c> (encargo §3.2).
    /// </summary>
    internal static class B9Loc
    {
        /// <summary>Ruta del CSV de B9, desde la raíz del repositorio.</summary>
        public const string CsvPath = "docs/dev/traspaso/spark-enganches/B9-strings.csv";

        /// <summary>El catálogo del proyecto con las filas de B9 que falten.</summary>
        public static LocCatalog Catalog()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            IReadOnlyList<LocCsvRow> project = LocCsv.Parse(LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile), LocCatalog.StringsFile);
            var rows = new List<LocCsvRow>(project);
            string extra = Path.Combine(root, CsvPath);
            if (File.Exists(extra))
            {
                var keys = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 1; i < project.Count; i++)
                {
                    keys.Add(project[i].Fields[0]);
                }

                IReadOnlyList<LocCsvRow> mine = LocCsv.Parse(LocCsv.Decode(File.ReadAllBytes(extra), "B9-strings.csv"), "B9-strings.csv");
                for (int i = 1; i < mine.Count; i++)
                {
                    if (!keys.Contains(mine[i].Fields[0]))
                    {
                        rows.Add(mine[i]);
                    }
                }
            }

            LocCatalog projectCatalog = LocTables.FromProjectFiles();
            return new LocCatalog(LocTable.FromRows(rows, LocCatalog.StringsFile), projectCatalog.Facts);
        }

        /// <summary>Configura <see cref="Loc"/> en es-419 (desarrollo: enseña lo no aprobado) con <see cref="Catalog"/>.</summary>
        public static void ConfigureEs419()
        {
            Loc.Configure(Catalog(), LocLocale.SourceLocale, true);
        }

        /// <summary>Todas las claves de <see cref="A11yText"/> existen en el catálogo de B9.</summary>
        public static bool HasAllKeys()
        {
            LocCatalog catalog = Catalog();
            for (int i = 0; i < A11yText.AllKeys.Length; i++)
            {
                if (!catalog.Strings.Contains(A11yText.AllKeys[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
