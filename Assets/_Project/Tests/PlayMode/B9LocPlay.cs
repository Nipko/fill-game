#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Localization;
using AF.UI;
using UnityEngine;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Textos de B9 en los tests de PlayMode (como <c>B9Loc</c> en EditMode, que este ensamblado no ve): la tabla del proyecto más
    /// las filas de <c>docs/dev/traspaso/spark-enganches/B9-strings.csv</c> que falten, sobre la tabla de hechos de la escena. Con
    /// <c>B9-loc.diff</c> aplicado ya no falta ninguna y el integrador puede quitar este ayudante y el CSV.
    /// </summary>
    internal static class B9LocPlay
    {
        /// <summary>Configura <see cref="Loc"/> en es-419 (desarrollo) con la tabla de UI del proyecto más las filas de B9.</summary>
        public static void Use()
        {
#if UNITY_EDITOR
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            IReadOnlyList<LocCsvRow> project = LocCsv.Parse(LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile), LocCatalog.StringsFile);
            var rows = new List<LocCsvRow>(project);
            string extra = Path.Combine(root, "docs/dev/traspaso/spark-enganches/B9-strings.csv");
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

            Loc.Configure(new LocCatalog(LocTable.FromRows(rows, LocCatalog.StringsFile), Loc.Catalog.Facts), LocLocale.SourceLocale, true);
#endif
        }
    }
}
