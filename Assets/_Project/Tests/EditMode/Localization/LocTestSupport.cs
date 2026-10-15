#nullable enable
using AF.Localization;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Estado de <see cref="Loc"/> antes de un test, para dejarlo como estaba: la fachada es estática y el
    /// <c>[SetUpFixture]</c> de L2b la configura para todos los tests de la carpeta.
    /// </summary>
    internal readonly struct LocStateSnapshot
    {
        private readonly bool _configured;
        private readonly LocCatalog _catalog;
        private readonly string _locale;
        private readonly bool _showUnapproved;
        private readonly string? _numberTag;

        private LocStateSnapshot(bool configured, LocCatalog catalog, string locale, bool showUnapproved, string? numberTag)
        {
            _configured = configured;
            _catalog = catalog;
            _locale = locale;
            _showUnapproved = showUnapproved;
            _numberTag = numberTag;
        }

        public static LocStateSnapshot Take()
        {
            return new LocStateSnapshot(Loc.IsConfigured, Loc.Catalog, Loc.Locale, Loc.ShowUnapproved, Loc.RequestedNumberTag);
        }

        public void Restore()
        {
            if (_configured)
            {
                Loc.Configure(_catalog, _locale, _showUnapproved, _numberTag);
            }
            else
            {
                Loc.Reset();
            }
        }
    }

    /// <summary>Tablas de ejemplo de los tests de AF.Localization (la cabecera de la pregunta 49 a, más <c>args</c>).</summary>
    internal static class LocSamples
    {
        /// <summary>Cabecera con estado por idioma (49 a) y la columna <c>args</c> [P].</summary>
        public const string PerLocaleHeader = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args";

        /// <summary>UI con es-419 y en completos, pt-BR a medias.</summary>
        public const string Strings =
            PerLocaleHeader + "\n" +
            "level_complete,¡Nivel completado!,Level complete!,Nível concluído!,Titular de la victoria (02 §6),24,aprobado_es,traducido,traducido,,\n" +
            "level_n,Nivel {level},Level {level},,Rótulo del nivel,12,aprobado_es,revisado_nativo,,,\n" +
            "next,Siguiente,Next,Próximo,Botón de la victoria,12,aprobado_es,revisado_nativo,revisado_nativo,,\n" +
            "stuck_rescue_missing,\"{n:plural:Te falta {} semilla|Te faltan {} semillas}\",\"{n:plural:You need {} more seed|You need {} more seeds}\",,\"Modal Aguas estancadas; n = semillas que faltan\",32,borrador,traducido,,,\n" +
            "world_banner,Mundo {n}: Açaí{sep}Brasil{sep}Amazonas,World {n}: Açaí{sep}Brazil{sep}Amazon,,Banner del mundo,60,pendiente_asesor,traducido,,,\n";

        /// <summary>Textos largos con estados distintos y una cifra en <c>args</c> (Q8-A).</summary>
        public const string Facts =
            PerLocaleHeader + "\n" +
            "fact.acai.01,El açaí da {n} frutos por racimo.,Açaí gives {n} fruits per bunch.,,Dato de fruta,90,aprobado_es,revisado_nativo,,,n=5200\n" +
            "fact.acai.02,Dato en borrador.,Draft fact.,,Dato de fruta,90,pendiente_asesor,revisado_nativo,,,\n" +
            "fact.acai.03,Solo en español y aprobado.,,,Dato de fruta,90,aprobado_es,,,,\n" +
            "fact.acai.04,Mide {m} metros.,It is {m} metres tall.,,Dato de fruta,90,aprobado_es,traducido,,,m=12.5\n" +
            "fauna_saimiri_collinsi_dato,\"Cruza el bosque en fila, por ramas finas\",,,Tarjeta del miquito,50,aprobado_es,,,,\n";

        public static LocCatalog Catalog()
        {
            return LocCatalog.Parse(Strings, Facts);
        }
    }
}
