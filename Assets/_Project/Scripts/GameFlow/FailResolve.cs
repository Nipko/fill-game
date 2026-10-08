#nullable enable
using System;
using System.Collections.Generic;
using AF.Boosters;
using AF.Economy;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>Tipo de fallo (definición canónica de 04 §5.2).</summary>
    public enum FailKind
    {
        /// <summary>
        /// Fallo duro: en 1-30, Volver a la orilla desde Aguas estancadas; desde el 31, la rendición confirmada o «Salir» con
        /// el menú (D-008). <c>fails_level</c> + 1 y, desde el 31, −1 canoa salvo Canoas infinitas.
        /// </summary>
        Hard = 0,

        /// <summary>Fallo blando [P]: Volver a la orilla voluntario con ≥ <c>restart_confirm_min_pours</c> vertidos. Sin canoa.</summary>
        Soft = 1,
    }

    /// <summary>Por qué se añadió una cuia al tablero (04 §2.2 y §6.2).</summary>
    public enum AddedCuiaKind
    {
        /// <summary>Cuia extra (comodín planificado de la bandeja).</summary>
        Extra = 0,

        /// <summary>Cuia de rescate («Continuar: +1 cuia» o Balsa de rescate).</summary>
        Rescue = 1,
    }

    /// <summary>Lo que dejó un fallo resuelto (<c>af_level_fail</c>, 08 §8.2: <c>kind</c>, <c>canoe_lost</c>).</summary>
    public readonly struct FailOutcome
    {
        /// <summary>Crea el resultado.</summary>
        public FailOutcome(FailKind kind, int failsLevel, bool canoeLost)
        {
            Kind = kind;
            FailsLevel = failsLevel;
            CanoeLost = canoeLost;
        }

        /// <summary>Tipo de fallo.</summary>
        public FailKind Kind { get; }

        /// <summary><c>fails_level</c> después del fallo.</summary>
        public int FailsLevel { get; }

        /// <summary>Costó una canoa.</summary>
        public bool CanoeLost { get; }
    }

    /// <summary>
    /// Lo que la partida recuerda de un nivel mientras no se gana (04 §2.2, §2.7, §5.2 y §6.2):
    /// <list type="bullet">
    /// <item><c>fails_level</c> y la variante aliviada servida, que duran hasta ganar el nivel (la variante «se mantiene hasta
    /// ganar»);</item>
    /// <item>las cuias añadidas (Cuia extra y Cuia de rescate), el tope de rescates del nivel y la recarga gratis de la rama F,
    /// que duran mientras se juega el nivel (persisten al reiniciar) y desaparecen al salir de él.</item>
    /// </list>
    /// En memoria: guardarlo en <c>level_state</c> del perfil es la tarea B2-5.
    /// </summary>
    public sealed class LevelAttempt
    {
        private readonly List<AddedCuiaKind> _added = new List<AddedCuiaKind>(2);

        /// <summary>Nivel (0 si ninguno).</summary>
        public int Level { get; private set; }

        /// <summary><c>fails_level</c> (04 §5.2): fallos duros y blandos en este nivel desde la última victoria.</summary>
        public int FailsLevel { get; internal set; }

        /// <summary>Índice de <c>relief[]</c> servido (−1 = el tablero base).</summary>
        public int ReliefIndex { get; internal set; } = -1;

        /// <summary>Cuias añadidas en este nivel, en orden (persisten al reiniciar).</summary>
        public IReadOnlyList<AddedCuiaKind> Added => _added;

        /// <summary>Cuias extra aplicadas en este nivel.</summary>
        public int ExtraCuias => Count(AddedCuiaKind.Extra);

        /// <summary>Cuias de rescate compradas en este nivel (<c>rescues_level</c>).</summary>
        public int Rescues => Count(AddedCuiaKind.Rescue);

        /// <summary>La recarga gratis de Marea de la rama F [P] ya se dio.</summary>
        public bool UndoGiftUsed { get; internal set; }

        /// <summary>
        /// Entrar en el nivel <paramref name="level"/> (Jugar, Siguiente): si es otro nivel, todo vuelve a cero; si es el mismo
        /// (se salió al inicio y se vuelve), se conservan <c>fails_level</c> y la variante, pero no las cuias añadidas.
        /// </summary>
        public void Enter(int level)
        {
            if (level != Level)
            {
                Level = level;
                FailsLevel = 0;
                ReliefIndex = -1;
            }

            _added.Clear();
            UndoGiftUsed = false;
        }

        /// <summary>Victoria: el contador de fallos y la variante vuelven a cero (04 §2.7).</summary>
        public void Won()
        {
            FailsLevel = 0;
            ReliefIndex = -1;
        }

        internal void AddCuia(AddedCuiaKind kind)
        {
            _added.Add(kind);
        }

        private int Count(AddedCuiaKind kind)
        {
            int n = 0;
            for (int i = 0; i < _added.Count; i++)
            {
                if (_added[i] == kind)
                {
                    n++;
                }
            }

            return n;
        }
    }

    /// <summary>
    /// FAIL_RESOLVE (02 §2; <c>failHard</c> de 04 §6.2 con D-008; tabla de 04 §5.2): suma el fallo y, si es duro, cobra la
    /// canoa con <see cref="Transaction.HardFail"/> (desde el 31, salvo Canoas infinitas). La Crecida y la Hoja escudo aún no
    /// existen en el cliente (06 §4.3): cuando lleguen, se rompen aquí. También decide qué variante de <c>relief[]</c> sirve el
    /// siguiente intento (04 §2.7 y §2.2 caso b).
    /// </summary>
    public static class FailResolve
    {
        /// <summary>Resuelve un fallo <paramref name="kind"/> en <paramref name="level"/>.</summary>
        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet)
        {
            if (attempt == null)
            {
                throw new ArgumentNullException(nameof(attempt));
            }

            attempt.FailsLevel++;
            bool canoeLost = false;
            if (kind == FailKind.Hard && wallet != null)
            {
                canoeLost = wallet.Apply(Transaction.HardFail(level)).CanoeLost;
            }

            return new FailOutcome(kind, attempt.FailsLevel, canoeLost);
        }

        /// <summary>
        /// Variante del siguiente intento (04 §2.7): la ya servida, que se mantiene hasta ganar; o, al llegar a
        /// <c>relief_after_fails</c> fallos en un nivel con <c>relief[]</c>, <c>relief[0]</c>, salvo que haya una Cuia extra
        /// aplicada: entonces la primera <c>minus_color</c> (nunca la de +1 vacío; 04 §2.2 caso b). −1 = tablero base.
        /// </summary>
        public static int ChooseRelief(LevelDefinition level, LevelAttempt attempt, BoosterConfig config)
        {
            if (attempt.ReliefIndex >= 0 && attempt.ReliefIndex < level.Relief.Count)
            {
                return attempt.ReliefIndex;
            }

            if (attempt.FailsLevel < config.ReliefAfterFails || level.Relief.Count == 0)
            {
                return -1;
            }

            if (attempt.ExtraCuias == 0)
            {
                return 0;
            }

            for (int i = 0; i < level.Relief.Count; i++)
            {
                if (level.Relief[i].Type == ReliefBoard.TypeMinusColor)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
