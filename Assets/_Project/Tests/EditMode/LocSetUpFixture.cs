#nullable enable
using System.Globalization;
using AF.Localization;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Preparación común de todos los tests de <c>AF.Tests.EditMode</c> (D-018; arquitectura de la tanda 4, §1; tarea L2b):
    /// <see cref="Loc"/> con las tablas del proyecto (<c>Data/Loc/strings.csv</c> y <c>facts.csv</c>) en es-419, como un build
    /// de desarrollo (enseña lo no aprobado), y los ganchos de <see cref="LaunchOptions"/> limpios. Las aserciones con texto
    /// español (como «Jugar · Nivel 1» de <c>LobbyTextTests</c>) no dependen de la cultura del equipo: durante la ejecución, la
    /// cultura es <see cref="ForcedCulture"/> (en-US, distinta de la del equipo del dueño) y al final vuelve la de antes. Un test
    /// que cambie <see cref="Loc"/> lo deja como estaba (<c>LocStateSnapshot</c>) o vuelve a <see cref="ConfigureEs419"/>.
    /// </summary>
    [SetUpFixture]
    public sealed class LocSetUpFixture
    {
        /// <summary>Cultura forzada durante los tests: los textos no pueden depender de ella.</summary>
        public const string ForcedCulture = "en-US";

        private CultureInfo? _culture;
        private CultureInfo? _uiCulture;
        private CultureInfo? _threadCulture;
        private CultureInfo? _threadUiCulture;

        /// <summary>Catálogo leído de las tablas del proyecto (vacío antes del <c>[OneTimeSetUp]</c>).</summary>
        public static LocCatalog Catalog { get; private set; } = LocCatalog.Empty;

        /// <summary>Configura <see cref="Loc"/> en es-419 con las tablas del proyecto, sin región del teléfono (cifras de es-CO).</summary>
        public static void ConfigureEs419()
        {
            if (Catalog.Strings.Count == 0)
            {
                Catalog = LocTables.FromProjectFiles();
            }

            Loc.Configure(Catalog, LocLocale.SourceLocale, true);
        }

        /// <summary>Fuerza la cultura, limpia los ganchos y configura <see cref="Loc"/>.</summary>
        [OneTimeSetUp]
        public void SetUp()
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            _threadCulture = CultureInfo.DefaultThreadCurrentCulture;
            _threadUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
            CultureInfo forced = CultureInfo.GetCultureInfo(ForcedCulture);
            CultureInfo.CurrentCulture = forced;
            CultureInfo.CurrentUICulture = forced;
            CultureInfo.DefaultThreadCurrentCulture = forced;
            CultureInfo.DefaultThreadCurrentUICulture = forced;

            LaunchOptions.Reset();
            Catalog = LocTables.FromProjectFiles();
            ConfigureEs419();
        }

        /// <summary>Deja la cultura, los ganchos y <see cref="Loc"/> como estaban antes de los tests.</summary>
        [OneTimeTearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            Loc.Reset();
            CultureInfo.CurrentCulture = _culture ?? CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = _uiCulture ?? CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = _threadCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _threadUiCulture;
        }
    }
}
