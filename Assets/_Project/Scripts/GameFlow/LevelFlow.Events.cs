#nullable enable
using System;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.Events;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// Eventos de dominio del nivel por el bus (08 §2.1, principio 3; D-033, Q-T1 (a); bloque B7, parche B7-levelflow):
    /// <see cref="LevelStarted"/> al terminar Llenar y teñir, <see cref="LevelWon"/> en el instante lógico de la victoria,
    /// <see cref="LevelFailed"/> en cada fallo (duro o blando, 04 §5.2) y <see cref="LevelAbandoned"/> al salir de un nivel sin
    /// resolver (pausa › «Salir» en 1-30). <c>AF.Analytics</c> los traduce a <c>level_start</c>, <c>af_level_win</c>,
    /// <c>af_level_fail</c>, <c>level_end</c> y <c>af_level_abandon</c> (08 §8.2). Solo en la campaña: la partida del Reto (su
    /// propio <see cref="LevelFlow"/>) no publica nada aquí; su analítica sigue en sus sondas [P] (nota B7, Q-B7-9).
    /// </summary>
    public sealed partial class LevelFlow
    {
        private DateTime _eventsStartUtc;
        private int _eventsUndoUsed;
        private bool _eventsStarted;

        /// <summary>Bus de eventos (null = sin bus: tests del flujo y la partida del Reto).</summary>
        public EventBus? Events { get; set; }

        // id del nivel de 08 §4.1 y variante servida (base o el tipo de relief[], 08 §4.1).
        private string EventLevelName => _level?.Id ?? string.Empty;

        private string EventVariant => _level != null && _attempt.ReliefIndex >= 0 && _attempt.ReliefIndex < _level.Relief.Count
            ? _level.Relief[_attempt.ReliefIndex].Type
            : "base";

        private long EventTimeMs => _eventsStarted ? Math.Max(0L, (long)(_clock.UtcNow - _eventsStartUtc).TotalMilliseconds) : 0L;

        /// <summary>Etiqueta del nivel para la analítica: <c>normal</c>, <c>hard</c> o <c>very_hard</c> (08 §8.1, <c>difficulty</c>).</summary>
        public static string DifficultyId(LevelDefinition level)
        {
            RewardTiers.TryParse(level.DifficultyLabel, out RewardTier tier);
            switch (tier)
            {
                case RewardTier.Hard: return "hard";
                case RewardTier.VeryHard: return "very_hard";
                default: return "normal";
            }
        }

        // FinishIntro: empieza el intento (level_start, 08 §8.2: «al terminar Llenar y teñir»).
        private void PublishLevelStarted()
        {
            _eventsStartUtc = _clock.UtcNow;
            _eventsUndoUsed = 0;
            _eventsStarted = true;
            if (Events == null || IsDaily || _level == null)
            {
                return;
            }

            Events.Publish(new LevelStarted(
                LevelNumber, EventLevelName, EventVariant, _level.Band ?? string.Empty, _attempt.FailsLevel + 1, _level.World, DifficultyId(_level), _level.IsMurky));
        }

        // Undo: una Marea más en el intento.
        private void CountUndoForEvents()
        {
            _eventsUndoUsed++;
        }

        // ResolveWin, después de la Crecida (el x2 del río ya está en la Cosecha).
        private void PublishLevelWon()
        {
            if (Events == null || IsDaily || _level == null)
            {
                return;
            }

            int pours = _session?.AppliedPours ?? 0;
            Events.Publish(new LevelWon(
                LevelNumber, EventLevelName, pours, _level.Par, _eventsUndoUsed, BoostersJson(), EventTimeMs, _harvest, _harvestDoubled,
                !_perfect));
        }

        // Fail: el resultado de FailResolve y, en el duro, el de la Crecida (LastStreakResult).
        private void PublishLevelFailed(FailOutcome outcome)
        {
            if (Events == null || IsDaily || _level == null)
            {
                return;
            }

            bool hard = outcome.Kind == FailKind.Hard;
            bool broken = hard && LastStreakResult != null && LastStreakResult.Streak.StreakBroken;
            bool shield = hard && LastStreakResult != null && LastStreakResult.Streak.ShieldUsed;
            int relief = FailResolve.ChooseRelief(_level, _attempt, _boosters);
            bool reliefServed = relief >= 0 && relief != _attempt.ReliefIndex;
            Events.Publish(new LevelFailed(
                LevelNumber, EventLevelName, hard, outcome.CanoeLost, broken, shield, reliefServed, _session?.AppliedPours ?? 0, EventTimeMs));
        }

        /// <summary>
        /// <see cref="LevelAbandoned"/> (<c>af_level_abandon</c>, 08 §8.2) si el nivel sigue sin resolver (intro, tarjeta previa,
        /// tablero o Aguas estancadas); en la victoria, Nivel fallido o Sin canoas no hay abandono. Lo llama <see cref="AppFlow"/>
        /// al salir al inicio con <paramref name="reason"/> <c>map</c> [P]. Devuelve si publicó.
        /// </summary>
        public bool PublishAbandonIfUnresolved(string reason)
        {
            bool unresolved = Phase == FlowPhase.Intro || Phase == FlowPhase.LevelCard || Phase == FlowPhase.Playing || Phase == FlowPhase.StuckModal;
            if (Events == null || IsDaily || _level == null || !unresolved || IsResolvingWin)
            {
                return false;
            }

            Events.Publish(new LevelAbandoned(LevelNumber, EventLevelName, _session?.AppliedPours ?? 0, EventTimeMs, reason));
            return true;
        }

        // boosters_json [P]: los comodines del intento con unidades, en el orden de BoosterKinds (undo, extra_cuia…), solo los > 0.
        private string BoostersJson()
        {
            var sb = new StringBuilder("{");
            Append(sb, "undo", _eventsUndoUsed);
            Append(sb, "extra_cuia", _attempt.ExtraCuias);
            return sb.Append('}').ToString();
        }

        private static void Append(StringBuilder sb, string id, int n)
        {
            if (n <= 0)
            {
                return;
            }

            if (sb.Length > 1)
            {
                sb.Append(',');
            }

            sb.Append('"').Append(id).Append("\":").Append(n.ToString(CultureInfo.InvariantCulture));
        }
    }
}
