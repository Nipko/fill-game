#nullable enable
using System;
using System.Text;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>
    /// Pororoca · Barajar en el flujo (bloque B2; 04 §2.4; 02 §14; 03 §16; 08 §9 <c>shuffle_*</c>): la oferta del botón
    /// (<see cref="Shuffle"/>), la ola con <see cref="ShuffleWave"/> y su aplicación con <see cref="LevelSession.ApplyShuffle"/>,
    /// en el orden aplicar → guardar → cobrar (nunca se cobra sin entregar, y si la ola falla no se cobra: 04 §2.4), los topes de
    /// 1 por nivel y 3 al día (también con el stock) y la tarjeta del nivel 30. La ranura, la ola de 1,2 s y la hoja de
    /// confirmación son de la vista (<see cref="IShuffleListener"/>, <c>GameScreen.Shuffle.cs</c>).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Tope por nivel</b> [P] (<c>docs/dev/pororoca.md</c>, P-4): cuenta las Pororocas del nivel en todos sus
    /// intentos y también si se sale al inicio y se vuelve (marca <c>o</c> de <c>level_state</c>, una por Pororoca); vuelve a 0
    /// al cargar otro nivel. El tope del día es <c>daily.shuffles_today</c> del perfil, que anota la cartera al cobrar (también
    /// en el Reto del río, con la misma cartera sobre el mismo perfil).</description></item>
    /// <item><description><b>Volver a la orilla</b> [P] (P-5): vuelve al tablero del nivel (misma semilla, 04 §2.7), con lo
    /// revelado visible; la Pororoca no persiste, como cualquier vertido, y su tope sigue gastado.</description></item>
    /// <item><description><b>Guardado</b> (02 R8): la ola es determinista, así que <c>history</c> anota solo dónde fue (un
    /// <c>*</c> entre los vertidos de V) y al reanudar se vuelve a dar sobre el tablero rehecho (<see cref="ReplayShuffle"/>).
    /// Tras una Pororoca la sesión ya no tiene el historial anterior (02 §14), así que el flujo guarda los vertidos de antes de
    /// cada ola del intento en <see cref="_shufflePrefix"/>.</description></item>
    /// <item><description><b>Tarjeta del 30</b> (04 §4: «Tarjeta con animación»; 04 §2.4: «no se regala uso»): la primera vez
    /// que se juega el nivel de desbloqueo en la campaña (<c>max_level</c> &lt; 30), una vez por nivel (marca <c>p</c>).</description></item>
    /// </list>
    /// </remarks>
    public sealed partial class LevelFlow
    {
        private const string ShuffleHex = "0123456789abcdef";

        private readonly StringBuilder _shufflePrefix = new StringBuilder(64);
        private ShuffleWave? _shuffleWave;
        private ShuffleConfig _shuffleConfig = ShuffleConfig.Defaults;
        private int _shuffleLevel;
        private int _shufflesUsed;
        private bool _shuffleTemporal;
        private bool _shuffleCardShown;
        private bool _shuffleCardDue;
        private ShuffleOutcome _lastShuffle;
        private bool _hasLastShuffle;

        /// <summary>Cifras de la Pororoca (por defecto, las de 08 §9). Se validan al ponerlas.</summary>
        public ShuffleConfig ShuffleConfig
        {
            get => _shuffleConfig;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                value.Validate();
                _shuffleConfig = value;
            }
        }

        /// <summary>Pororocas usadas en el nivel cargado (todas sus vías e intentos).</summary>
        public int ShufflesUsed => _shufflesUsed;

        /// <summary>Pororocas usadas hoy (<c>daily.shuffles_today</c>; 0 sin cartera).</summary>
        public int ShufflesToday => _wallet?.ShufflesToday ?? 0;

        /// <summary>Estado del botón «Barajar» (04 §2.4): stock, precio, topes y si se puede usar.</summary>
        public ShuffleOffer Shuffle => ShuffleBooster.Evaluate(
            LevelNumber, _shufflesUsed, ShufflesToday, ShufflePlaying, ShuffleBoard, _wallet, _boosters, _shuffleConfig);

        /// <summary>Última ola (con éxito o no) desde que se cargó el nivel, o null.</summary>
        public ShuffleOutcome? LastShuffle => _hasLastShuffle ? _lastShuffle : (ShuffleOutcome?)null;

        /// <summary>
        /// Toca la tarjeta de la Pororoca (04 §4, nivel 30): el nivel de desbloqueo de la campaña, aún no superado, y sin haberla
        /// enseñado en este nivel. La vista la enseña al habilitarse el toque y la cierra con <see cref="MarkShuffleCardShown"/>.
        /// </summary>
        public bool ShuffleCardDue => _shuffleCardDue && !_shuffleCardShown;

        // Solo con el tablero en juego y algún vertido legal: ni en el modal de bloqueo ni con la victoria o el bloqueo pendientes
        // (04 §2.4), ni con la Luciérnaga eligiendo cuia.
        private bool ShufflePlaying => AcceptsBoardInput && !_revealSelecting && _session!.Status == LevelStatus.Playing;

        private ShuffleBoardBlock ShuffleBoard => _session == null
            ? ShuffleBoardBlock.None
            : ShuffleWave.BlockOf(_session.Current, _shuffleTemporal, _shuffleConfig);

        private ShuffleWave Wave => _shuffleWave ??= new ShuffleWave();

        /// <summary>
        /// Botón «Barajar» ya confirmado (la hoja de 04 §2.4 la pide la vista antes, con ≥ 200 semillas): con
        /// <see cref="Shuffle"/> listo y pagable, quita la selección (C8), da la ola sobre el tablero de ahora y, si sale, la
        /// aplica (revela las ocultas y vacía el historial de Marea), guarda, cobra (el stock primero) y avisa a la vista; después,
        /// como tras un vertido, la victoria o el bloqueo. Si la ola falla, avisa y no cobra. Null si no se pudo intentar (sin
        /// nivel, sin cartera o con la oferta no lista: la vista ya lo enseña).
        /// </summary>
        public ShuffleOutcome? UseShuffle()
        {
            LevelSession? session = _session;
            Wallet? wallet = _wallet;
            ShuffleOffer offer = Shuffle;
            if (session == null || wallet == null || !offer.CanUse)
            {
                return null;
            }

            ShufflePayment payment = offer.Payment;
            using (Saves())
            {
                ClearSelection();
                ShuffleOutcome outcome = Wave.Run(session.Current, _seed, _shufflesUsed, _shuffleConfig, session.PourMode, session.FutileRuleEnabled);
                _lastShuffle = outcome;
                _hasLastShuffle = true;
                if (!outcome.Succeeded)
                {
                    (_listener as IShuffleListener)?.OnShuffleFailed(outcome);
                    return outcome;
                }

                AppendShufflePrefix(session);
                session.ApplyShuffle(outcome.Board!); // 1. aplicar
                _shufflesUsed++;
                _taps.Clear();
                PersistState(); // 2. guardar: si la app se cierra ahora, la ola ya está y no se cobró
                if (!ShuffleBooster.Pay(wallet, payment).IsApplied) // 3. cobrar (no puede faltar: se comprobó en la oferta)
                {
                    System.Diagnostics.Debug.Fail("Pororoca: el cobro se rechazó tras aplicar la ola.");
                }
                (_listener as IShuffleListener)?.OnShuffled(outcome, payment);
                EvaluateStatus(); // como tras un vertido (R10 g-h)
                return outcome;
            }
        }

        /// <summary>La vista enseñó la tarjeta de la Pororoca del 30: no vuelve en este nivel (marca <c>p</c>).</summary>
        public void MarkShuffleCardShown()
        {
            if (_shuffleCardShown)
            {
                return;
            }

            _shuffleCardShown = true;
            using (Saves())
            {
                PersistState();
            }
        }

        // LevelFlow.Load, con la sesión nueva: otro nivel pone a 0 el tope del nivel y la tarjeta; el intento empieza sin olas.
        private void ShuffleLevelLoaded(int number)
        {
            if (number != _shuffleLevel)
            {
                _shuffleLevel = number;
                _shufflesUsed = 0;
                _shuffleCardShown = false;
            }

            _shufflePrefix.Clear();
            _hasLastShuffle = false;
            _shuffleTemporal = ShuffleWave.HasTemporalMechanic(_session!.Initial);
            _shuffleCardDue = !IsDaily && number == _boosters.ShuffleUnlockLevel && _progress.LoadMaxLevel() < number;
        }

        // LevelFlow.BeginAttempt (Volver a la orilla, Reintentar, rama C): el tablero vuelve al del nivel y el intento nuevo
        // empieza sin olas (P-5); el tope del nivel no se devuelve.
        private void ShuffleAttemptStarted()
        {
            _shufflePrefix.Clear();
        }

        // Los vertidos del intento hasta ahora y la marca de la ola, al final del prefijo (02 R8).
        private void AppendShufflePrefix(LevelSession session)
        {
            for (int i = 0; i < session.AppliedPours; i++)
            {
                Pour pour = session.GetAppliedPour(i);
                _shufflePrefix.Append(ShuffleHex[pour.Source & 0xF]).Append(ShuffleHex[pour.Target & 0xF]);
            }

            _shufflePrefix.Append(LevelStateSnapshot.ShuffleMark);
        }

        // V de history: los vertidos de antes de cada ola del intento (con sus marcas) y, detrás, los de la sesión.
        private string? ShuffleHistoryPrefix => _shufflePrefix.Length == 0 ? null : _shufflePrefix.ToString();

        // Al reanudar (LevelFlow.Resume.cs): la ola ordinal sobre el tablero rehecho, igual que la primera vez (determinista).
        // False si ya no sale (otro presupuesto en Remote Config, otro pack…): quien reanuda carga el nivel de nuevo.
        private bool ReplayShuffle(LevelSession session, string seed, int ordinal)
        {
            if (session.Status != LevelStatus.Playing)
            {
                return false;
            }

            ShuffleOutcome outcome = Wave.Run(session.Current, seed, ordinal, _shuffleConfig, session.PourMode, session.FutileRuleEnabled);
            return outcome.Succeeded && session.ApplyShuffle(outcome.Board!);
        }

        // Al reanudar o al restaurar un nivel del que se salió: el tope del nivel, la tarjeta y el prefijo de V.
        private void RestoreShuffle(int number, SavedHistory history, LevelSession? session)
        {
            _shuffleLevel = number;
            _shufflesUsed = history.ShufflesUsed;
            _shuffleCardShown = history.ShuffleCardShown;
            _shufflePrefix.Clear();
            _hasLastShuffle = false;
            if (session == null)
            {
                return;
            }

            _shuffleTemporal = ShuffleWave.HasTemporalMechanic(session.Initial);
            _shuffleCardDue = !IsDaily && number == _boosters.ShuffleUnlockLevel && _progress.LoadMaxLevel() < number;
            int marks = history.ShuffleAt.Count;
            if (marks == 0)
            {
                return;
            }

            int upTo = history.ShuffleAt[marks - 1];
            int mark = 0;
            for (int i = 0; i <= upTo; i++)
            {
                while (mark < marks && history.ShuffleAt[mark] == i)
                {
                    _shufflePrefix.Append(LevelStateSnapshot.ShuffleMark);
                    mark++;
                }

                if (i < upTo)
                {
                    _shufflePrefix.Append(ShuffleHex[history.Sources[i] & 0xF]).Append(ShuffleHex[history.Targets[i] & 0xF]);
                }
            }
        }

        // Rehace los vertidos guardados con las olas en su sitio (02 R8): la marca k va antes del vertido ShuffleAt[k]. False si
        // un vertido o una ola no se pueden rehacer.
        private bool ReplaySaved(LevelSession session, SavedHistory history, string seed)
        {
            int marks = history.ShuffleAt.Count;
            if (marks > history.ShufflesUsed)
            {
                return false;
            }

            int mark = 0;
            for (int i = 0; i <= history.Sources.Count; i++)
            {
                while (mark < marks && history.ShuffleAt[mark] == i)
                {
                    int ordinal = history.ShufflesUsed - marks + mark;
                    if (!ReplayShuffle(session, seed, ordinal))
                    {
                        return false;
                    }

                    mark++;
                }

                if (i == history.Sources.Count)
                {
                    break;
                }

                int source = history.Sources[i];
                int target = history.Targets[i];
                if (source >= session.Current.Count || target >= session.Current.Count || !session.TryPour(source, target, out _))
                {
                    return false;
                }
            }

            return mark == marks;
        }
    }
}
