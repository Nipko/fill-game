#nullable enable
using AF.GameFlow;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// El tooltip de Marea, una sola vez en la vida (D-030, pregunta 15 (b); tarea DC23; 02 §13 y 06 §2.2, fila del 3): la
    /// pantalla de juego lee <c>progress.undo_tip_seen</c> del perfil (<see cref="GameScreen.UndoTipSeen"/>) para decidir si sale
    /// en el 3 o, como respaldo, al inicio del 5, y al enseñarlo (<see cref="GameScreen.UndoTipShown"/>) se apunta y el perfil se
    /// guarda en un lote propio (<see cref="Saves"/>), cuyo cierre lo entrega al escritor de segundo plano con el perfil en disco
    /// (D-012: nada nuevo en el vertido ni en el hilo principal, salvo serializar). Con el perfil en solo lectura (08 §4.4)
    /// queda en memoria para esta sesión.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Veces que se apuntó el tooltip de Marea en el perfil (tests).</summary>
        internal int UndoTipRecordedCount { get; private set; }

        // Gancho de GameScreen.UndoTipSeen.
        private bool ReadUndoTipSeen()
        {
            Profile? profile = _profile;
            return profile != null && profile.Progress.UndoTipSeen;
        }

        // Gancho de GameScreen.UndoTipShown: apunta el tooltip y guarda la primera vez.
        private void RecordUndoTip()
        {
            Profile? profile = _profile;
            if (profile == null || profile.Progress.UndoTipSeen)
            {
                return;
            }

            profile.Progress.UndoTipSeen = true;
            UndoTipRecordedCount++;
            ProfileSaveBatch? batch = _saveBatch;
            if (batch != null && !ProfileLocked)
            {
                // Dentro de un lote: fuera de él, ProfileSaveBatch.Save escribiría en síncrono (fsync y Replace) en el hilo
                // principal, en plena partida (en el 3 sale tras un vertido). El cierre lo entrega al escritor (HandOff).
                using (Saves())
                {
                    batch.TrySave(profile);
                }
            }
        }
    }
}
