#nullable enable
using AF.GameFlow;
using AF.Levels;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Avistamiento de fauna (Q11 del guion de la intro: recomendación aceptada el 2026-10-19, D-029; tarea DC15): cuando la
    /// tarjeta «¡Miquito a la vista!» queda a la vista (<see cref="GameScreen.SightingShown"/>), la especie se apunta en
    /// <c>fauna.sighted</c> del perfil (esquema 2), con su primer nivel, y se escribe la sonda <c>af_fauna_sighted</c>
    /// {species_id, level, first} (DevProbe.Fauna.cs). La segunda vez de la misma especie, <c>first</c> = false, la lista no
    /// crece y no se guarda nada. La primera vez, el perfil se guarda por el lote del juego (en segundo plano con el perfil en
    /// disco, D-012; nada nuevo en el vertido); con el perfil en solo lectura (08 §4.4) queda en memoria para esta sesión. Lo
    /// necesitan el Cuaderno de campo (1.1) y el puente en el Mapa del río.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Veces que se escribió <c>af_fauna_sighted</c> (una por tarjeta que sale; tests).</summary>
        internal int FaunaSightedCount { get; private set; }

        /// <summary>El <c>first</c> del último <c>af_fauna_sighted</c> (tests).</summary>
        internal bool LastFaunaSightedFirst { get; private set; }

        // Gancho de GameScreen.SightingShown: apunta el avistamiento, guarda la primera vez y escribe la sonda.
        private void RecordSighting(FaunaSpecies species, int level)
        {
            Profile? profile = _profile;
            if (profile == null || level < 1 || !FaunaData.IsSpeciesId(species.Id))
            {
                return;
            }

            bool first = profile.Fauna.RecordSighting(species.Id, level);
            ProfileSaveBatch? batch = _saveBatch;
            if (first && batch != null && !ProfileLocked)
            {
                batch.TrySave(profile);
            }

            FaunaSightedCount++;
            LastFaunaSightedFirst = first;
            _probe?.FaunaSighted(species.Id, level, first);
        }
    }
}
