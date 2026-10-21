#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>Un nivel terminado (<see cref="LevelFlow.LevelPlayed"/>): la Bitácora lo apunta (06 §4.2; D-033, añadido del 2026-10-20, punto 5).</summary>
    public readonly struct LevelPlayedInfo
    {
        /// <summary>Crea el aviso.</summary>
        public LevelPlayedInfo(int level, bool won, bool daily)
        {
            Level = level;
            Won = won;
            Daily = daily;
        }

        /// <summary>Número del nivel (en el Reto, el nivel efectivo).</summary>
        public int Level { get; }

        /// <summary>Ganado (si no, fallo duro).</summary>
        public bool Won { get; }

        /// <summary>Partida del Reto del río.</summary>
        public bool Daily { get; }
    }

    /// <summary>
    /// La Bitácora del río en el flujo del nivel (B4; 06 §4.2; D-033, añadido del 2026-10-20, punto 5: «Cuándo se cobra: al
    /// terminar el primer nivel del día (campaña o Reto, se gane o se pierda)»). <c>AF.GameFlow</c> no ve <c>AF.LiveOps</c>: el
    /// flujo solo avisa con <see cref="LevelPlayed"/> y el arranque (<c>GameBootstrap.Logbook.cs</c>) lo apunta en
    /// <c>LoginCalendarService.MarkPlayed</c>. En los archivos existentes solo hay dos llamadas (parche
    /// <c>docs/dev/traspaso/spark-enganches/B4-2-flow.diff</c>):
    /// <list type="bullet">
    /// <item><description><c>ResolveWin</c>: tras <c>OnWinSkins</c> y dentro del mismo lote del perfil, antes de
    /// <see cref="ILevelFlowListener.OnWin"/> (así la victoria ya sabe que la libreta está lista y el apunte sale en la misma
    /// escritura que la victoria).</description></item>
    /// <item><description><c>Fail</c>: tras <c>OnHardFailStreak</c>, solo con el fallo duro y antes de
    /// <see cref="ILevelFlowListener.OnFailResolved"/>. Es fallo duro Volver a la orilla desde Aguas estancadas en 1-30 y, desde
    /// el 31, la rendición confirmada o «Salir» con el menú (D-008; 04 §6.6): perder el nivel. El fallo blando (04 §5.2) y salir
    /// al inicio en 1-30 sin terminar no cuentan.</description></item>
    /// </list>
    /// Vale para la campaña y para el Reto (sus victorias y su Volver a la orilla desde Aguas estancadas pasan por los mismos
    /// <c>ResolveWin</c> y <c>Fail</c>; <see cref="LevelPlayedInfo.Daily"/> los distingue). Sin suscriptores no hace nada.
    /// </summary>
    public sealed partial class LevelFlow
    {
        /// <summary>
        /// Se terminó un nivel: victoria o fallo duro (campaña o Reto). Se dispara dentro del lote de guardado del fin de nivel y
        /// antes de avisar a la vista. Un suscriptor no debe cargar ni cambiar de nivel.
        /// </summary>
        public event Action<LevelPlayedInfo>? LevelPlayed;

        /// <summary>Niveles terminados en este flujo (diagnóstico y tests).</summary>
        public int LevelsPlayed { get; private set; }

        // B4-2-flow: ResolveWin (won = true) y Fail con FailKind.Hard (won = false).
        private void OnLevelPlayedCalendar(bool won)
        {
            LevelsPlayed++;
            LevelPlayed?.Invoke(new LevelPlayedInfo(LevelNumber, won, IsDaily));
        }

        // B4-2-flow: en Fail, solo el fallo duro cuenta.
        private void OnFailCalendar(FailKind kind)
        {
            if (kind == FailKind.Hard)
            {
                OnLevelPlayedCalendar(false);
            }
        }
    }
}
