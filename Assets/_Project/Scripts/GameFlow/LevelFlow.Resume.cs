#nullable enable
using System;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Levels;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>Qué se puede hacer con un <c>level_state</c> guardado (<see cref="LevelFlow.CheckResume"/>).</summary>
    public enum ResumeCheck
    {
        /// <summary>No sirve (otro formato, nivel que ya no está en el catálogo u otro tablero con ese número): se descarta.</summary>
        Invalid = 0,

        /// <summary>Nivel en curso: se reabre tal cual (<see cref="LevelFlow.Resume"/>).</summary>
        Resumable = 1,

        /// <summary>Se salió al inicio sin ganarlo: la app abre el inicio y el nivel recuerda <c>fails_level</c> y la variante.</summary>
        Exited = 2,
    }

    /// <summary>
    /// Nivel en curso guardado tras cada vertido y reabierto al volver a la app (tarea B2-5; 02 R8 y §14; 04 §5.2 y §10.1;
    /// D-008 «Persistencia»). Se guarda en cada cambio del nivel (carga, vertido, Marea, reinicio, cuia añadida, apertura del
    /// modal, ¿Seguro?, Nivel fallido, Sin canoas) con <see cref="LevelStateSnapshot"/>; la victoria lo borra. Al abrir la app,
    /// LOADING del mismo nivel sin intro y con la pantalla que estaba abierta: cerrar la app nunca cobra canoa ni cuenta como
    /// fallo (no se resuelve nada al reabrir), y tampoco es una salida gratis del embudo. Las capas de Aguas turbias ya
    /// reveladas se guardan en <c>history</c> h2 y se restauran al reabrir (02 R8 y §11 T6: el conocimiento no se revierte).
    /// </summary>
    public sealed partial class LevelFlow
    {
        private readonly LevelStateData _stateBuffer = new LevelStateData();
        private readonly StringBuilder _historyBuilder = new StringBuilder(160);
        private string _seed = string.Empty;

        /// <summary>Dónde se guarda el nivel en curso; null = no se guarda (tests del flujo).</summary>
        public ILevelStateStore? StateStore { get; set; }

        /// <summary>
        /// Lote de guardado del perfil (revisión de B2-5): cada acción del jugador (vertido, victoria, rescate, rendición,
        /// salida…) escribe el perfil una sola vez, con la cartera, el progreso y <c>level_state</c> juntos; el vertido, en
        /// segundo plano, y el fin de nivel, en síncrono (T-D). Null = cada almacén guarda por su cuenta (tests del flujo sin
        /// perfil).
        /// </summary>
        public ProfileSaveBatch? SaveBatch { get; set; }

        /// <summary>
        /// ¿Sirve <paramref name="saved"/>? Comprueba el formato de <c>history</c>, que el nivel siga en el catálogo con la misma
        /// semilla y que la variante exista. No cambia nada.
        /// </summary>
        public ResumeCheck CheckResume(LevelStateData? saved)
        {
            return Validate(saved, out _, out _, out _);
        }

        /// <summary>
        /// Reabre el nivel guardado (02 §14): LOADING del mismo nivel con el mismo tablero (variante, cuias añadidas, vertidos
        /// rehechos con las reglas del núcleo y capas de Aguas turbias ya reveladas, 02 R8 y §11 T6), el cupo de Marea que
        /// quedaba, <c>fails_level</c> y la pantalla que estaba abierta (tablero, Aguas estancadas, ¿Seguro? o Nivel fallido),
        /// sin intro. No cobra nada. False, sin excepciones, si el estado no sirve, los vertidos no se pueden rehacer o las capas
        /// reveladas no son de este tablero: quien llama carga el nivel de nuevo.
        /// </summary>
        public bool Resume(LevelStateData saved)
        {
            using (Saves())
            {
                return ResumeSaved(saved);
            }
        }

        /// <summary>
        /// <c>level_state</c> de un nivel del que se salió al inicio (<see cref="ResumeCheck.Exited"/>): el nivel recuerda
        /// <c>fails_level</c>, la variante aliviada y si Capi ya regaló su Cuia extra y si ya se regaló la Luciérnaga, para
        /// cuando se vuelva a jugar (04 §2.7, §2.2 y §2.5). False si no sirve.
        /// </summary>
        public bool RestoreExited(LevelStateData saved)
        {
            if (Validate(saved, out SavedHistory history, out _, out int relief) != ResumeCheck.Exited)
            {
                return false;
            }

            _attempt.Restore(saved.Level, saved.FailsLevel, relief, null, false, history.ExtraCuiaGifted, history.RevealGifted);
            return true;
        }

        /// <summary>
        /// Se sale del nivel al inicio sin ganarlo (X de Nivel fallido, «Volver al inicio», «Salir» en 1-30): se guarda que el
        /// nivel ya no está en curso, con <c>fails_level</c>, la variante y los regalos de Capi y de la Luciérnaga (las cuias
        /// añadidas desaparecen: 04 §2.2). Si la Luciérnaga estaba eligiendo cuia, se cancela sin coste. Es fin de nivel
        /// (T-D): la escritura espera al escritor de segundo plano y va en síncrono.
        /// </summary>
        public void LeaveLevel()
        {
            EndRevealSelection();
            if (StateStore == null || _session == null || _level == null || IsWonPhase)
            {
                return;
            }

            using (Saves())
            {
                SaveBatch?.RequireDurable();
                WriteState(SavedPhase.Exited, false);
            }
        }

        /// <summary>Descarta el <c>level_state</c> guardado (no sirve).</summary>
        public void DiscardSaved()
        {
            StateStore?.Clear();
        }

        // Abre un lote de guardado (una escritura del perfil al cerrarlo); sin lote, no hace nada.
        private ProfileSaveScope Saves()
        {
            return SaveBatch == null ? default : SaveBatch.Begin();
        }

        private bool ResumeSaved(LevelStateData saved)
        {
            if (Validate(saved, out SavedHistory history, out LevelDefinition? def, out int relief) != ResumeCheck.Resumable || def == null)
            {
                return false;
            }

            int number = saved.Level;
            _catalog.Serve(number); // carga del nivel: el aviso del kill switch si se sirve el publicado (KS1)
            _attempt.Restore(number, saved.FailsLevel, relief, history.Added, history.UndoGiftUsed, history.ExtraCuiaGifted, history.RevealGifted);
            var session = new LevelSession(BoardFor(def, relief), GameConfig.FutileRuleEnabled, _pourMode);
            for (int i = 0; i < history.Sources.Count; i++)
            {
                int source = history.Sources[i];
                int target = history.Targets[i];
                if (source >= session.Current.Count || target >= session.Current.Count || !session.TryPour(source, target, out _))
                {
                    return false;
                }
            }

            // 02 R8 y §11 T6: las capas reveladas (R de h2) se restauran tras rehacer los vertidos; las que el historial ya
            // reveló no suman. Una entrada que no es de este tablero (cuia que no existe o capas que no eran ocultas) rompe el
            // estado como un vertido que no se puede rehacer: quien llama carga el nivel de nuevo.
            for (int i = 0; i < history.Revealed.Count; i++)
            {
                SavedReveal reveal = history.Revealed[i];
                if (!TryRestoreRevealed(session, reveal.Cuia, reveal.Layers))
                {
                    return false;
                }
            }

            // 02 §14: al reanudar no hay tarjeta previa (LEVEL_CARD) ni intro; la Luciérnaga no sigue eligiendo cuia.
            EndRevealSelection();
            ResetLevelCard();
            Phase = FlowPhase.Loading;
            _level = def;
            _levelNumber = number;
            _seed = saved.Seed;
            ResetHarvest();
            _undo.ResetForLevel(number);
            _undo.RestoreFreeLeft(saved.UndoFree);
            ResetStuck();
            _session = session;
            _taps.Clear();
            ResetPending();
            _phaseMs = 0;
            _introMs = 0;
            _listener.OnLevelLoaded(number);

            bool stuck = session.Status == LevelStatus.Stuck;
            SavedPhase phase = history.Phase;
            if ((phase == SavedPhase.StuckModal || phase == SavedPhase.GiveUpConfirm) && !stuck)
            {
                phase = SavedPhase.Playing;
            }

            if ((phase == SavedPhase.GiveUpConfirm || phase == SavedPhase.LevelFailed) && !CanoesLevel)
            {
                phase = stuck ? SavedPhase.StuckModal : SavedPhase.Playing;
            }

            // La Balsa que ya se enseñó en este bloqueo se vuelve a ofrecer sin contar otra impresión (marca b; D-008).
            _balsaShown = history.BalsaShown && (phase == SavedPhase.StuckModal || phase == SavedPhase.GiveUpConfirm);
            switch (phase)
            {
                case SavedPhase.StuckModal:
                    CheckRescue();
                    OpenStuckModal();
                    _listener.OnResumed(FlowPhase.StuckModal);
                    break;

                case SavedPhase.GiveUpConfirm:
                    CheckRescue();
                    _quote = _wallet?.QuoteRescue(LevelNumber);
                    Phase = FlowPhase.GiveUpConfirm;
                    _listener.OnResumed(FlowPhase.GiveUpConfirm);
                    break;

                case SavedPhase.LevelFailed:
                    Phase = FlowPhase.LevelFailed;
                    _listener.OnResumed(FlowPhase.LevelFailed);
                    break;

                default:
                    Phase = FlowPhase.Playing;
                    _listener.OnResumed(FlowPhase.Playing);
                    EvaluateStatus(); // cerrado en los 400 ms antes del modal: el bloqueo se vuelve a detectar
                    break;
            }

            return true;
        }

        // Restaura las capas reveladas <paramref name="layers"/> de la cuia <paramref name="cuia"/> en una sesión rehecha
        // (reanudar o añadir una cuia; 02 R8, §11 T6). Sin excepciones: false si la cuia no existe o si alguna capa no es una
        // dosis oculta del tablero inicial, que es lo que LevelSession.RestoreRevealed rechazaría.
        //
        // Se restaura al final del rehacer, no en el punto del historial en que se reveló cada capa: h2 guarda por cuia solo la
        // máscara. Es exacto en 1-200 porque ver antes una oculta solo cambia lo que pasa después si alarga la racha (R6) o
        // permite un sellado (R5), y con T4 (bajo una oculta nunca va el color de la de encima) eso solo ocurre si un lavado de
        // Barro deja sobre ella su mismo color; ningún nivel de la campaña lleva Barro en una cuia turbia
        // (MurkyPersistenceTests lo comprueba). Si un pack futuro lo combinara, al añadir una cuia el rehacer da otra cantidad,
        // la cuia no se añade y no se cobra (LastFunnelError); al reanudar habría que guardar el punto del revelado.
        private static bool TryRestoreRevealed(LevelSession session, int cuia, int layers)
        {
            if (cuia < 0 || cuia >= session.Initial.Count || layers <= 0 || (layers & ~session.Initial[cuia].HiddenMask) != 0)
            {
                return false;
            }

            session.RestoreRevealed(cuia, layers);
            return true;
        }

        private bool IsWonPhase => Phase == FlowPhase.Celebration || Phase == FlowPhase.VictoryScreen || Phase == FlowPhase.AdGate
            || _pending == Outcome.Win || (_session != null && _session.Status == LevelStatus.Won);

        // Guarda el nivel en curso con la pantalla que se reabriría ahora. Tras la victoria no hay nada que guardar.
        private void PersistState()
        {
            if (StateStore == null || _session == null || _level == null || IsWonPhase)
            {
                return;
            }

            SavedPhase phase;
            switch (Phase)
            {
                case FlowPhase.StuckModal:
                    phase = SavedPhase.StuckModal;
                    break;
                case FlowPhase.GiveUpConfirm:
                    phase = SavedPhase.GiveUpConfirm;
                    break;
                case FlowPhase.LevelFailed:
                    phase = SavedPhase.LevelFailed;
                    break;
                case FlowPhase.NoCanoes:
                    phase = _noCanoesReturn == FlowPhase.LevelFailed ? SavedPhase.LevelFailed : SavedPhase.Playing;
                    break;
                default:
                    phase = SavedPhase.Playing;
                    break;
            }

            WriteState(phase, true);
        }

        private void WriteState(SavedPhase phase, bool inProgress)
        {
            LevelSession session = _session!;
            LevelDefinition def = _level!;
            LevelStateData s = _stateBuffer;
            s.Level = _levelNumber;
            s.Seed = _seed;
            s.Variant = LevelStateSnapshot.VariantOf(def, _attempt.ReliefIndex);
            s.FailsLevel = _attempt.FailsLevel;
            s.Pours = inProgress ? session.AppliedPours : 0;
            s.ExtraCuias = inProgress ? _attempt.Added.Count : 0;
            s.RescuesLevel = inProgress ? _attempt.Rescues : 0;
            s.UndoFree = _undo.FreeLeft;
            s.Stuck = phase == SavedPhase.StuckModal || phase == SavedPhase.GiveUpConfirm;
            if (inProgress)
            {
                LevelStateSnapshot.WriteRevealed(session.Initial, session.Current, s.RevealedCuias);
                LevelStateSnapshot.WriteHistory(
                    _historyBuilder, phase, _attempt.Added, _attempt.UndoGiftUsed, _attempt.ExtraCuiaGifted, session, _balsaShown, _attempt.RevealGifted);
            }
            else
            {
                s.RevealedCuias.Clear();
                LevelStateSnapshot.WriteHistory(
                    _historyBuilder, phase, Array.Empty<AddedCuiaKind>(), false, _attempt.ExtraCuiaGifted, null, revealGifted: _attempt.RevealGifted);
            }

            s.History = _historyBuilder.ToString();
            StateStore!.Save(s);
        }

        private ResumeCheck Validate(LevelStateData? saved, out SavedHistory history, out LevelDefinition? def, out int relief)
        {
            def = null;
            relief = -1;
            if (saved == null || !LevelStateSnapshot.TryParseHistory(saved.History, out history))
            {
                history = new SavedHistory();
                return ResumeCheck.Invalid;
            }

            def = _catalog.Find(saved.Level);
            if (def == null || !string.Equals(LevelStateSnapshot.SeedOf(def), saved.Seed, StringComparison.Ordinal))
            {
                return ResumeCheck.Invalid;
            }

            relief = LevelStateSnapshot.ReliefIndexOf(def, saved.Variant);
            if (relief < -1 || history.Added.Count + def.CuiaCount > BoardState.MaxCuias)
            {
                return ResumeCheck.Invalid;
            }

            if (relief >= 0 && history.Added.Count + def.Relief[relief].CuiaCount > BoardState.MaxCuias)
            {
                return ResumeCheck.Invalid;
            }

            return history.Phase == SavedPhase.Exited ? ResumeCheck.Exited : ResumeCheck.Resumable;
        }
    }
}
