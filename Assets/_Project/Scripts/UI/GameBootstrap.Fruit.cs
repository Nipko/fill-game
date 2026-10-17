#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// La ficha de una fruta de Frutas del río (D-017, Q4-A; 02 §6, fila «Dato del río»; propuesta de Frutas §2.3; tarea F4;
    /// <see cref="FruitSheet"/>): se abre al tocar la línea de fruta de la victoria (<see cref="FactLine.Tapped"/>) y desde
    /// Ajustes › «Sobre la Amazonía» (<see cref="SettingsScreen.FruitChosen"/>); se cierra con su X, el velo o el Atrás de
    /// Android. Lo revelado sale del progreso (<c>progress.max_level</c> y <c>progress.skipped_levels</c>; sin estado nuevo).
    /// Mientras se ve encima de la victoria, ni la vista ni el flujo avanzan (Update no los llama): al cerrarla, la victoria
    /// sigue tal como estaba (la línea, «Siguiente», la Cosecha doble y su espera, AD_GATE). Cada apertura escribe la sonda
    /// <c>af_fact_page_open</c> {tint_id, unlocked} (08 §8.2; DevProbe.Fruit.cs).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private FruitSheet? _fruitSheet;

        /// <summary>La ficha de una fruta (null hasta <c>Start</c>).</summary>
        internal FruitSheet? FruitSheetView => _fruitSheet;

        /// <summary>La ficha de una fruta está abierta.</summary>
        internal bool FruitSheetOpen => _fruitSheet != null && _fruitSheet.Visible;

        /// <summary>Veces que se escribió <c>af_fact_page_open</c> (tests).</summary>
        internal int FactPageOpenCount { get; private set; }

        /// <inheritdoc />
        public bool OpenFruitSheet(string subject, FruitSheetSource source)
        {
            FruitSheet? sheet = _fruitSheet;
            FactSchedule? facts = FruitFacts;
            AppFlow? app = _app;
            IProgressStore? progress = _progress;
            if (subject == null || sheet == null || facts == null || app == null || progress == null || sheet.Visible || app.Screen == AppScreen.Boot)
            {
                return false;
            }

            if (CommerceOpen || (_lockedNotice != null && _lockedNotice.Visible) || (_mapScreen != null && _mapScreen.Visible))
            {
                return false;
            }

            FactSeries? series = facts.Catalog.SeriesOf(subject);
            if (series == null)
            {
                return false;
            }

            int level = 0;
            if (source == FruitSheetSource.Victory)
            {
                // Solo la fruta de la línea que se ve, con la victoria a la vista (no en AD_GATE ni fuera del nivel).
                FactLine? line = _screen?.FruitLine;
                LevelFlow? flow = _flow;
                if (app.Screen != AppScreen.Level || flow == null || flow.Phase != FlowPhase.VictoryScreen || line == null || !line.Shown
                    || !string.Equals(line.Position.Series?.Subject, subject, StringComparison.Ordinal))
                {
                    return false;
                }

                level = flow.LevelNumber;
            }
            else if (_settingsScreen == null || !_settingsScreen.Visible || app.Screen != AppScreen.Lobby)
            {
                return false;
            }

            if (!sheet.Show(series, facts.Catalog, progress, source))
            {
                return false;
            }

            FactPageOpenCount++;
            _probe?.FactPageOpen(series.Subject, sheet.Unlocked, source, level);
            _skipNextDelta = true;
            return true;
        }

        /// <inheritdoc />
        public bool CloseFruitSheet()
        {
            FruitSheet? sheet = _fruitSheet;
            if (sheet == null || !sheet.Visible)
            {
                return false;
            }

            sheet.Hide();
            _skipNextDelta = true; // lo de debajo sigue sin el tiempo que estuvo abierta
            return true;
        }

        // Crea la ficha (tras Ajustes: va encima) y la engancha a la línea de la victoria y a «Sobre la Amazonía» de Ajustes.
        private void BuildFruitSheet()
        {
            _fruitSheet = new FruitSheet(transform, _art!, _probe!, () => CloseFruitSheet());
            FactLine? line = _screen?.FruitLine;
            if (line != null)
            {
                line.Tapped = OpenFruitSheetFromVictory;
            }

            SettingsScreen? settings = _settingsScreen;
            if (settings != null)
            {
                settings.FruitCatalog = FruitFacts?.Catalog;
                settings.FruitProgress = _progress;
                settings.FruitChosen = subject => OpenFruitSheet(subject, FruitSheetSource.Settings);
            }
        }

        // Un toque en la línea de fruta de la victoria: la ficha de su fruta.
        private void OpenFruitSheetFromVictory()
        {
            string? subject = _screen?.FruitLine?.Position.Series?.Subject;
            if (subject != null)
            {
                OpenFruitSheet(subject, FruitSheetSource.Victory);
            }
        }

        // Con la ficha abierta, la recoloca si cambió la pantalla y devuelve true si Update debe parar aquí: encima del nivel (la
        // victoria), nada de debajo avanza. Encima de Ajustes, el inicio sigue como siempre.
        private bool TickFruitSheet()
        {
            FruitSheet? sheet = _fruitSheet;
            if (sheet == null || !sheet.Visible)
            {
                return false;
            }

            sheet.Tick();
            return _app != null && _app.Screen == AppScreen.Level;
        }
    }
}
