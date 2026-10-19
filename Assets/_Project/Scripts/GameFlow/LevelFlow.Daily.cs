#nullable enable
using System;
using System.Globalization;
using System.Text;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.Levels;
using AF.Platform;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>Qué pasó en una partida del Reto del río (<see cref="LevelFlow.DailyResult"/>).</summary>
    public enum DailyPlayOutcome
    {
        /// <summary>Tablero ganado (instante lógico de la victoria, R12). Quien llama cobra el premio (<c>DailyChallengeRules.Claim</c>).</summary>
        Win = 1,

        /// <summary>Salida al inicio sin ganar (<see cref="LevelFlow.ExitDaily"/>): <c>af_daily_challenge.result = fail</c> (R-Q9).</summary>
        Exit = 2,

        /// <summary>Volver a la orilla: nuevo intento con el mismo tablero (reintentos ilimitados; 06 §4.1). La partida sigue.</summary>
        Restart = 3,
    }

    /// <summary>Resultado que el modo Reto entrega a quien lo abrió (AF.UI, que llama a AF.LiveOps; GameFlow no lo referencia).</summary>
    public readonly struct DailyPlayResult
    {
        /// <summary>Crea el resultado.</summary>
        public DailyPlayResult(DailyPlayOutcome outcome, string boardId, int attempt, int pours, bool fromStuckModal)
        {
            Outcome = outcome;
            BoardId = boardId;
            Attempt = attempt;
            Pours = pours;
            FromStuckModal = fromStuckModal;
        }

        /// <summary>Victoria, salida o reinicio.</summary>
        public DailyPlayOutcome Outcome { get; }

        /// <summary>Id del tablero del pool (<c>d{b}_n{nnnn}</c>; 03 §16): el <c>level_name</c> de <c>level_start</c> (08 §8.2).</summary>
        public string BoardId { get; }

        /// <summary>Intento en el que ocurrió (1 = el primero; cada reinicio suma uno).</summary>
        public int Attempt { get; }

        /// <summary>Vertidos aplicados en ese intento en el momento del resultado.</summary>
        public int Pours { get; }

        /// <summary>Solo en <see cref="DailyPlayOutcome.Restart"/>: se reinició desde el pie de Aguas estancadas.</summary>
        public bool FromStuckModal { get; }
    }

    /// <summary>
    /// Modo Reto del río en el flujo del nivel (tarea R8; nota <c>docs/dev/reto-del-rio.md</c> §9; 06 §4.1; 04 §7.1; 04 §2.6 e;
    /// 06 §4.3; R-Q4, R-Q8, R-Q9 y R-Q13). Se monta sin tocar el resto de <see cref="LevelFlow"/>, con un flujo propio que
    /// <see cref="CreateDaily"/> construye con piezas que neutralizan la campaña:
    /// <list type="bullet">
    /// <item><description><b>Catálogo de un tablero</b> con el número de nivel efectivo <c>max_level + 1</c> [P] (R-Q8): los
    /// desbloqueos de la bandeja (Cuia extra 8, Morpho 12, Pororoca 30…) se miden con él. <see cref="Next"/> nunca avanza.</description></item>
    /// <item><description><b>Progreso de solo lectura</b>: la victoria no cambia <c>max_level</c>, <c>next_level</c>,
    /// <c>levels_won</c> ni el Mapa (06 §4.3 «Solo niveles de campaña»), y no hay regalos de tutorial (el nivel efectivo cuenta
    /// como ya alcanzado).</description></item>
    /// <item><description><b>Cartera del Reto</b> (<see cref="CreateDailyWallet"/>) sobre el mismo perfil: el stock y las
    /// semillas son los de siempre y se gastan igual (R-Q8), pero la victoria no paga la Cosecha de campaña (el premio de 60 lo
    /// cobra quien llama) y las canoas y la Cuia de rescate no existen (<c>rescue_min_level</c> inalcanzable): ni se piden ni
    /// se gastan (04 §7.1), Aguas estancadas solo ofrece Marea y Volver a la orilla [P] y no hay embudo de D-008 ni Salto del
    /// Boto (04 §2.6 e). Volver a la orilla es siempre gratis: reintentos ilimitados con el mismo tablero (06 §4.1).</description></item>
    /// <item><description><b>Comodines del Reto</b> (<see cref="DailyBoosters"/>): los de la campaña, con Marea nunca ilimitada
    /// (las gratis por tablero de <c>undo_free_per_level</c> y luego el stock [P]) y sin Cuia de rescate.</description></item>
    /// <item><description>Sin intersticial (<see cref="AdGate"/> null [P]; R-Q13), sin tarjeta previa ni banner de mundo, sin
    /// alivio (<c>relief</c> = []) y con su propio <see cref="StateStore"/> (R-Q4): nunca toca el <c>level_state</c> ni el
    /// <c>fails_level</c> de la campaña.</description></item>
    /// </list>
    /// El resultado sale por <see cref="DailyResult"/>. La Crecida, la Canasta y el Salto del Boto aún no existen en el cliente
    /// (FailResolve, StuckModalModel): cuando lleguen, el flujo del Reto no los alcanza porque no gana niveles de campaña.
    /// </summary>
    public sealed partial class LevelFlow
    {
        /// <summary><c>pack_id</c> interno del catálogo de un tablero del Reto.</summary>
        public const string DailyPackId = "daily_play";

        // Nivel máximo del número efectivo (de sobra para la vuelta 1, 06 §10; el catálogo admite más).
        private const int DailyMaxEffectiveLevel = 1000000;

        private LevelDefinition? _dailyBoard;
        private int _dailyAttempt;
        private bool _dailyExited;

        /// <summary>
        /// Resultado de la partida del Reto (solo en un flujo de <see cref="CreateDaily"/>): la victoria (una vez, en su instante
        /// lógico), cada reinicio y la salida sin ganar.
        /// </summary>
        public event Action<DailyPlayResult>? DailyResult;

        /// <summary>Este flujo es una partida del Reto del río (<see cref="CreateDaily"/>).</summary>
        public bool IsDaily => _dailyBoard != null;

        /// <summary>Tablero del pool que se juega (el de <see cref="DailyPool"/>; <see cref="Level"/> es su copia numerada). Null fuera del Reto.</summary>
        public LevelDefinition? DailyBoard => _dailyBoard;

        /// <summary>Id del tablero (<c>d{b}_n{nnnn}</c>), o null fuera del Reto.</summary>
        public string? DailyBoardId => _dailyBoard?.Id;

        /// <summary>Intento en curso (1 el primero; 0 antes de <see cref="StartDaily"/> o <see cref="ResumeDaily"/>).</summary>
        public int DailyAttempt => _dailyAttempt;

        /// <summary>
        /// Nivel efectivo del Reto [P] (R-Q8; nota §9): <c>max_level + 1</c>, con el que se desbloquean los comodines. Al menos 1.
        /// </summary>
        public static int DailyEffectiveLevel(int maxLevel)
        {
            return maxLevel < 0 ? 1 : maxLevel >= DailyMaxEffectiveLevel ? DailyMaxEffectiveLevel : maxLevel + 1;
        }

        /// <summary>
        /// Nivel efectivo para reabrir <paramref name="saved"/> (R-Q4): el de la partida guardada si es de este tablero (el nivel
        /// efectivo se congela al empezar, como la banda; R-Q3 [P]); si no, <see cref="DailyEffectiveLevel"/>.
        /// </summary>
        public static int DailyEffectiveLevel(int maxLevel, LevelStateData? saved, LevelDefinition board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (saved != null && saved.Level >= 1 && saved.Level <= DailyMaxEffectiveLevel
                && string.Equals(saved.Seed, LevelStateSnapshot.SeedOf(board), StringComparison.Ordinal))
            {
                return saved.Level;
            }

            return DailyEffectiveLevel(maxLevel);
        }

        /// <summary>
        /// Cifras de la economía para el Reto, copiadas de <paramref name="campaign"/>: iguales (precios, canoas, Marea…) salvo
        /// la Cosecha de campaña (<c>eco_reward_*</c> = 0: el Reto paga su premio aparte, 06 §4.1) y <c>rescue_min_level</c>
        /// inalcanzable (sin canoas, 04 §7.1; sin Cuia de rescate, 06 §4.1).
        /// </summary>
        public static EconomyConfig DailyEconomy(EconomyConfig? campaign = null)
        {
            EconomyConfig c = campaign ?? EconomyConfig.Defaults;
            var daily = new EconomyConfig
            {
                EcoRewardNormal = 0,
                EcoRewardHard = 0,
                EcoRewardVeryHard = 0,
                AdsRvDoubleHarvestFromLevel = c.AdsRvDoubleHarvestFromLevel,
                UndoRefillAmount = c.UndoRefillAmount,
                UndoRefillSeedPrice = c.UndoRefillSeedPrice,
                ExtraCuiaSeedPrice = c.ExtraCuiaSeedPrice,
                HintSeedPrice = c.HintSeedPrice,
                ShuffleSeedPrice = c.ShuffleSeedPrice,
                RevealSeedPrice = c.RevealSeedPrice,
                SkipSeedPrice = c.SkipSeedPrice,
                RescueMinLevel = int.MaxValue,
                RescuePriceBands = c.RescuePriceBands,
                RescueFirstDayMult = c.RescueFirstDayMult,
                CanoesMax = c.CanoesMax,
                CanoesRegenMin = c.CanoesRegenMin,
                CanoesRefillSeedsEnabled = c.CanoesRefillSeedsEnabled,
                CanoesRefillSeedPrice = c.CanoesRefillSeedPrice,
            };
            daily.Validate();
            return daily;
        }

        /// <summary>
        /// Cifras de los comodines para el Reto, copiadas de <paramref name="campaign"/>: iguales salvo Marea nunca ilimitada
        /// (<c>undo_unlimited_until_level</c> = 0: las gratis por tablero y luego el stock [P]) y sin Cuia de rescate
        /// (<c>rescue_max_per_level</c> = 0, también para la Balsa de rescate).
        /// </summary>
        public static BoosterConfig DailyBoosters(BoosterConfig? campaign = null)
        {
            BoosterConfig c = campaign ?? BoosterConfig.Defaults;
            var daily = new BoosterConfig
            {
                UndoFreePerLevel = c.UndoFreePerLevel,
                UndoUnlimitedUntilLevel = 0,
                ExtraCuiaUnlockLevel = c.ExtraCuiaUnlockLevel,
                ExtraCuiaMaxPerLevel = c.ExtraCuiaMaxPerLevel,
                ShuffleUnlockLevel = c.ShuffleUnlockLevel,
                RescueMaxPerLevel = 0,
                RescueCheckSolvable = c.RescueCheckSolvable,
                HintBudgetNodes = c.HintBudgetNodes,
                HintWeight = c.HintWeight,
                HintTimeoutMs = c.HintTimeoutMs,
                HintUnlockLevel = c.HintUnlockLevel,
                HintTutorialUnits = c.HintTutorialUnits,
                HintIdleFromLevel = c.HintIdleFromLevel,
                HintIdleDelayS = c.HintIdleDelayS,
                RestartConfirmMinPours = c.RestartConfirmMinPours,
                ReliefAfterFails = c.ReliefAfterFails,
                AnimRiverLoweredBannerMs = c.AnimRiverLoweredBannerMs,
                StuckOrderT1 = c.StuckOrderT1,
                StuckOrderT3 = c.StuckOrderT3,
                StuckShowShieldStatus = c.StuckShowShieldStatus,
            };
            daily.Validate();
            return daily;
        }

        /// <summary>
        /// Cartera del Reto sobre el mismo perfil y el mismo almacén que la de la campaña (con <see cref="DailyEconomy"/>): el
        /// stock y las semillas son los mismos y se guardan igual. Sus cambios avisan por su propio <see cref="Wallet.Changed"/>,
        /// no por el de la cartera de campaña: la vista del Reto se suscribe a esta.
        /// </summary>
        public static Wallet CreateDailyWallet(Profile profile, IProfileStore store, IClock clock, EconomyConfig? campaign = null)
        {
            return new Wallet(profile, store, clock, DailyEconomy(campaign));
        }

        /// <summary>La configuración es la del Reto (<see cref="DailyEconomy"/>): sin Cosecha de campaña ni canoas.</summary>
        public static bool IsDailyEconomy(EconomyConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.RescueMinLevel == int.MaxValue && config.EcoRewardNormal == 0 && config.EcoRewardHard == 0 && config.EcoRewardVeryHard == 0;
        }

        /// <summary>
        /// Flujo de una partida del Reto del río con <paramref name="board"/> (un tablero del pool, sin mecánicas ni ocultas; 03
        /// §16) como nivel <paramref name="effectiveLevel"/> (<see cref="DailyEffectiveLevel(int)"/>). No carga nada hasta
        /// <see cref="StartDaily"/> o <see cref="ResumeDaily"/>. Quien llama pone, como en la campaña, <see cref="StateStore"/>
        /// (el del Reto, R-Q4), <see cref="SaveBatch"/>, <see cref="Clock"/> y las sondas; <see cref="AdGate"/> se queda en null
        /// (R-Q13 [P]).
        /// </summary>
        /// <param name="board">Tablero del pool (<see cref="DailyPool.Find"/>).</param>
        /// <param name="effectiveLevel">Nivel efectivo (≥ 1): desbloqueos de los comodines.</param>
        /// <param name="campaignProgress">Progreso de la campaña: solo se lee.</param>
        /// <param name="listener">Vista del Reto (también los avisos opcionales de Morpho, la Luciérnaga y la tarjeta previa).</param>
        /// <param name="dailyWallet">Cartera de <see cref="CreateDailyWallet"/>, o null (tests sin economía: sin stock).</param>
        /// <param name="campaignBoosters">Cifras de los comodines de la campaña (por defecto las de 08 §9); se pasan por <see cref="DailyBoosters"/>.</param>
        /// <param name="tapSwitchSelection">Valor de <c>tap_switch_selection</c>.</param>
        /// <param name="pourMode">Modo de vertido (<c>rules_pour_mode</c>, D-005).</param>
        /// <exception cref="ArgumentException">El tablero lleva mecánicas, ocultas o alivio, o la cartera no es la del Reto.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="effectiveLevel"/> fuera de 1..1.000.000.</exception>
        public static LevelFlow CreateDaily(
            LevelDefinition board,
            int effectiveLevel,
            IProgressStore campaignProgress,
            ILevelFlowListener listener,
            Wallet? dailyWallet = null,
            BoosterConfig? campaignBoosters = null,
            bool tapSwitchSelection = GameConfig.TapSwitchSelection,
            PourMode pourMode = PourModes.Default)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (campaignProgress == null)
            {
                throw new ArgumentNullException(nameof(campaignProgress));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (effectiveLevel < 1 || effectiveLevel > DailyMaxEffectiveLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveLevel), effectiveLevel, "El nivel efectivo del Reto va de 1 a 1.000.000.");
            }

            if (dailyWallet != null && !IsDailyEconomy(dailyWallet.Config))
            {
                throw new ArgumentException("La cartera del Reto se crea con LevelFlow.CreateDailyWallet (sin Cosecha de campaña ni canoas).", nameof(dailyWallet));
            }

            LevelCatalog catalog = LevelCatalog.FromPack(DailyPackFor(board, effectiveLevel));
            var dailyListener = new DailyListener(listener);
            var flow = new LevelFlow(
                catalog,
                new DailyProgress(campaignProgress, effectiveLevel),
                dailyListener,
                tapSwitchSelection,
                pourMode,
                dailyWallet,
                DailyBoosters(campaignBoosters));
            dailyListener.Flow = flow;
            flow._dailyBoard = board;
            flow.LevelCardsEnabled = false;
            flow.LevelCardOnRetry = false;
            flow.WorldBannersEnabled = false;
            return flow;
        }

        /// <summary>
        /// Empieza la partida del Reto: LOADING del tablero e intro (02 §2), sin canoa (04 §7.1), con las Mareas gratis del
        /// tablero enteras. Un flujo que no es del Reto lanza.
        /// </summary>
        /// <exception cref="InvalidOperationException">El flujo no es del Reto (<see cref="CreateDaily"/>).</exception>
        public void StartDaily()
        {
            RequireDaily();
            _dailyAttempt = 1;
            _dailyExited = false;
            Load(_catalog.FirstLevel);
        }

        /// <summary>
        /// Reabre la partida guardada en el <see cref="StateStore"/> del Reto (R-Q4: al volver a abrir la app el mismo día; quien
        /// llama descarta antes la de otro día). Como <see cref="Resume"/>: false, sin cambiar nada, si no es de este tablero y
        /// nivel efectivo (<see cref="DailyEffectiveLevel(int, LevelStateData, LevelDefinition)"/>) o no se puede rehacer; entonces
        /// quien llama la descarta y usa <see cref="StartDaily"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">El flujo no es del Reto.</exception>
        public bool ResumeDaily(LevelStateData saved)
        {
            RequireDaily();
            if (saved == null || CheckResume(saved) != ResumeCheck.Resumable || !Resume(saved))
            {
                return false;
            }

            _dailyAttempt = 1;
            _dailyExited = false;
            return true;
        }

        /// <summary>
        /// Salir del Reto sin ganar (pausa › Salir o la X de la cabecera): sin canoa ni fallo (04 §7.1), con el resultado
        /// <see cref="DailyPlayOutcome.Exit"/> (<c>result = fail</c>, R-Q9). La partida guardada del Reto se borra [P]: volver a
        /// entrar empieza el mismo tablero desde el principio (cerrar la app, en cambio, la deja para reanudar; R-Q4). False si
        /// no hay partida, ya se ganó (tras la victoria, salir no es un resultado nuevo) o ya se salió.
        /// </summary>
        /// <exception cref="InvalidOperationException">El flujo no es del Reto.</exception>
        public bool ExitDaily()
        {
            RequireDaily();
            if (_session == null || _level == null || IsWonPhase || _dailyExited)
            {
                return false;
            }

            _dailyExited = true;
            int pours = _session.AppliedPours;
            using (Saves())
            {
                ClearSelection();
                ResetHint();
                SaveBatch?.RequireDurable();
                StateStore?.Clear();
            }

            RaiseDaily(DailyPlayOutcome.Exit, pours, false);
            return true;
        }

        private void RequireDaily()
        {
            if (_dailyBoard == null)
            {
                throw new InvalidOperationException("Este flujo no es del Reto del río: créalo con LevelFlow.CreateDaily.");
            }
        }

        private void RaiseDaily(DailyPlayOutcome outcome, int pours, bool fromStuckModal)
        {
            if (_dailyBoard == null)
            {
                return;
            }

            DailyResult?.Invoke(new DailyPlayResult(outcome, _dailyBoard.Id, _dailyAttempt < 1 ? 1 : _dailyAttempt, pours, fromStuckModal));
        }

        // Victoria (ResolveWin ya la resolvió sin progreso ni Cosecha: DailyProgress y DailyEconomy).
        private void OnDailyWin()
        {
            RaiseDaily(DailyPlayOutcome.Win, _session?.AppliedPours ?? 0, false);
        }

        // Restart y el pie del modal avisan antes del nuevo intento: los vertidos son los del intento que termina.
        private void OnDailyRestart(bool fromStuckModal)
        {
            RaiseDaily(DailyPlayOutcome.Restart, _session?.AppliedPours ?? 0, fromStuckModal);
            _dailyAttempt = _dailyAttempt < 1 ? 2 : _dailyAttempt + 1;
        }

        // Paquete af-level/1 de un solo nivel con el tablero del pool numerado como effectiveLevel. AF.Levels no expone cómo
        // renumerar un LevelDefinition (constructores internos), así que se escribe el JSON del tablero y se lee con
        // LevelPackReader, que lo valida igual que la campaña; después se comprueba que el tablero leído es el mismo.
        private static LevelPack DailyPackFor(LevelDefinition board, int effectiveLevel)
        {
            if (board.HiddenCount > 0 || board.Relief.Count > 0 || board.IntroMechanic != null)
            {
                throw new ArgumentException(board.Id + ": el Reto no lleva dosis ocultas, alivio ni estrenos (03 §16).", nameof(board));
            }

            for (int i = 0; i < board.Attrs.Count; i++)
            {
                if (!board.Attrs[i].IsNone)
                {
                    throw new ArgumentException(board.Id + ": el Reto no lleva mecánicas (03 §16).", nameof(board));
                }
            }

            var sb = new StringBuilder(256 + board.CuiaCount * 24);
            sb.Append("{\"pack_schema\":").Append(Num(LevelPackReader.SupportedPackSchema));
            sb.Append(",\"pack_id\":");
            AppendString(sb, DailyPackId);
            sb.Append(",\"kind\":");
            AppendString(sb, DailyPool.KindDaily);
            sb.Append(",\"world\":").Append(Num(board.World));
            sb.Append(",\"world_id\":");
            AppendString(sb, DailyPackId);
            sb.Append(",\"water_base\":");
            AppendString(sb, board.WaterBase);
            sb.Append(",\"levels\":[{\"id\":");
            AppendString(sb, board.Id);
            sb.Append(",\"level\":").Append(Num(effectiveLevel));
            sb.Append(",\"world\":").Append(Num(board.World));
            sb.Append(",\"water_base\":");
            AppendString(sb, board.WaterBase);
            sb.Append(",\"palette\":[");
            for (int i = 0; i < board.Palette.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                AppendString(sb, board.Palette[i]);
            }

            sb.Append("],\"units_per_color\":");
            AppendUnits(sb, board.Units);
            sb.Append(",\"caps\":[");
            for (int i = 0; i < board.CuiaCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Num(board.CapacityOf(i)));
            }

            sb.Append("],\"cuias\":[");
            for (int i = 0; i < board.CuiaCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('[');
                int[] doses = board.CopyDoses(i);
                for (int k = 0; k < doses.Length; k++)
                {
                    if (k > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Num(doses[k]));
                }

                sb.Append(']');
            }

            // El pool se publica sin métricas (par 0; nota §13): LevelPackReader pide par ≥ 1 y el flujo no lo usa.
            sb.Append("],\"murky\":false,\"hidden\":[],\"difficulty\":{\"par\":").Append(Num(board.Par < 1 ? 1 : board.Par));
            sb.Append(",\"label\":");
            AppendString(sb, board.DifficultyLabel);
            sb.Append("},\"relief\":[],\"skippable\":").Append(board.Skippable ? "true" : "false");
            sb.Append(",\"hint_supported\":").Append(board.HintSupported ? "true" : "false");
            sb.Append("}]}");

            LevelPack pack = LevelPackReader.Read(sb.ToString());
            LevelDefinition copy = pack.First;
            bool same = copy.CuiaCount == board.CuiaCount && string.Equals(copy.Id, board.Id, StringComparison.Ordinal);
            for (int i = 0; same && i < board.CuiaCount; i++)
            {
                same = copy.CapacityOf(i) == board.CapacityOf(i) && SameDoses(copy.CopyDoses(i), board.CopyDoses(i));
            }

            for (int c = Cuia.MinColor; same && c <= Cuia.MaxColor; c++)
            {
                same = copy.Units.Of(c) == board.Units.Of(c);
            }

            if (!same)
            {
                throw new InvalidOperationException(board.Id + ": el tablero del Reto no se pudo copiar igual.");
            }

            return pack;
        }

        private static bool SameDoses(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        // units_per_color: el entero si todos los colores valen lo mismo; si no, el objeto con los que no valen 4 (08 §4.1).
        private static void AppendUnits(StringBuilder sb, UnitsPerColor units)
        {
            int first = units.Of(Cuia.MinColor);
            bool uniform = true;
            for (int c = Cuia.MinColor + 1; c <= Cuia.MaxColor; c++)
            {
                if (units.Of(c) != first)
                {
                    uniform = false;
                    break;
                }
            }

            if (uniform)
            {
                sb.Append(Num(first));
                return;
            }

            sb.Append('{');
            bool any = false;
            for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
            {
                int u = units.Of(c);
                if (u == UnitsPerColor.DefaultUnits)
                {
                    continue;
                }

                if (any)
                {
                    sb.Append(',');
                }

                sb.Append('"').Append(Num(c)).Append("\":").Append(Num(u));
                any = true;
            }

            sb.Append('}');
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == '"' || ch == '\\')
                {
                    sb.Append('\\').Append(ch);
                }
                else if (ch < ' ')
                {
                    sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(ch);
                }
            }

            sb.Append('"');
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Progreso de la campaña visto desde el Reto: se lee, nunca se escribe (06 §4.3: la victoria del Reto no es de
        /// campaña). <see cref="LoadMaxLevel"/> da al menos el nivel efectivo, así que los regalos de tutorial (Cuia extra del 8,
        /// Morphos del 12, Luciérnaga del 100), que solo se dan en un nivel aún no superado, no salen en el Reto [P].
        /// </summary>
        private sealed class DailyProgress : IProgressStore
        {
            private readonly IProgressStore _campaign;
            private readonly int _effectiveLevel;

            public DailyProgress(IProgressStore campaign, int effectiveLevel)
            {
                _campaign = campaign;
                _effectiveLevel = effectiveLevel;
            }

            public int MaxLevel => _campaign.MaxLevel;

            public int NextLevel => _campaign.NextLevel;

            public int MapSeenLevel => _campaign.MapSeenLevel;

            public int LevelsWon => _campaign.LevelsWon;

            public bool IsSkipped(int level)
            {
                return _campaign.IsSkipped(level);
            }

            public int LoadNextLevel()
            {
                return _effectiveLevel;
            }

            public int LoadMaxLevel()
            {
                return Math.Max(_campaign.LoadMaxLevel(), _effectiveLevel);
            }

            public void SaveNextLevel(int level)
            {
            }

            public void SaveWin(int level, int nextLevel)
            {
            }

            public int LoadMapSeenLevel()
            {
                return _campaign.LoadMapSeenLevel();
            }

            public void SaveMapSeenLevel(int level)
            {
            }
        }

        /// <summary>
        /// Oyente del Reto: reenvía todo a la vista (también los avisos opcionales que el flujo pide con <c>as</c>) y convierte la
        /// victoria y los reinicios en <see cref="DailyResult"/>.
        /// </summary>
        private sealed class DailyListener : ILevelFlowListener, IHintListener, IRevealListener, ILevelCardListener, IWorldBannerListener
        {
            private readonly ILevelFlowListener _inner;

            public DailyListener(ILevelFlowListener inner)
            {
                _inner = inner;
            }

            public LevelFlow? Flow { get; set; }

            public void OnLevelLoaded(int level)
            {
                _inner.OnLevelLoaded(level);
            }

            public void OnIntroStarted(int durationMs, bool isShort)
            {
                _inner.OnIntroStarted(durationMs, isShort);
            }

            public void OnIntroFinished(bool skipped)
            {
                _inner.OnIntroFinished(skipped);
            }

            public void OnTap(TapResult result)
            {
                _inner.OnTap(result);
            }

            public void OnUndo(Pour undone, bool fromStuckModal)
            {
                _inner.OnUndo(undone, fromStuckModal);
            }

            public void OnRestart(bool fromStuckModal)
            {
                _inner.OnRestart(fromStuckModal);
                Flow?.OnDailyRestart(fromStuckModal);
            }

            public void OnWin()
            {
                _inner.OnWin();
                Flow?.OnDailyWin();
            }

            public void OnStuck()
            {
                _inner.OnStuck();
            }

            public void OnCelebrationStarted()
            {
                _inner.OnCelebrationStarted();
            }

            public void OnVictoryScreen()
            {
                _inner.OnVictoryScreen();
            }

            public void OnStuckModalOpened()
            {
                _inner.OnStuckModalOpened();
            }

            public void OnCuiaAdded(int index, AddedCuiaKind kind)
            {
                _inner.OnCuiaAdded(index, kind);
            }

            public void OnExtraCuiaGifted(int units)
            {
                _inner.OnExtraCuiaGifted(units);
            }

            public void OnFunnelPhase(FlowPhase phase)
            {
                _inner.OnFunnelPhase(phase);
            }

            public void OnFailResolved(FailOutcome outcome)
            {
                _inner.OnFailResolved(outcome);
            }

            public void OnRiverLowered(string reliefType, int bannerMs)
            {
                _inner.OnRiverLowered(reliefType, bannerMs);
            }

            public void OnHarvestDoubled(int totalSeeds)
            {
                _inner.OnHarvestDoubled(totalSeeds);
            }

            public void OnAdGate()
            {
                _inner.OnAdGate();
            }

            public void OnResumed(FlowPhase phase)
            {
                _inner.OnResumed(phase);
            }

            public void OnHintThinking(bool thinking)
            {
                (_inner as IHintListener)?.OnHintThinking(thinking);
            }

            public void OnHint(in HintResult result)
            {
                (_inner as IHintListener)?.OnHint(in result);
            }

            public void OnHintCleared()
            {
                (_inner as IHintListener)?.OnHintCleared();
            }

            public void OnHintGifted(int units)
            {
                (_inner as IHintListener)?.OnHintGifted(units);
            }

            public void OnRevealGifted(int units)
            {
                (_inner as IRevealListener)?.OnRevealGifted(units);
            }

            public void OnRevealSelection(bool selecting)
            {
                (_inner as IRevealListener)?.OnRevealSelection(selecting);
            }

            public void OnRevealed(int cuia, int layerMask)
            {
                (_inner as IRevealListener)?.OnRevealed(cuia, layerMask);
            }

            public void OnLevelCard(LevelCardInfo card)
            {
                (_inner as ILevelCardListener)?.OnLevelCard(card);
            }

            public void OnWorldBanner(int world, int level)
            {
                (_inner as IWorldBannerListener)?.OnWorldBanner(world, level);
            }
        }
    }
}
