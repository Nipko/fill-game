#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.Ads;
using AF.Boosters;
using AF.Core.Board;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de la Luciérnaga · Revelar (arquitectura del paso 1, §7; tarea T14; 04 §2.5; 07 §7.4 y §7.6; 02 §5), sin editar
    /// GameScreen.cs:
    /// <list type="bullet">
    /// <item><description><b>Bandeja</b>: la ranura Revelar (<see cref="BoosterTray"/>) existe solo en los niveles de Aguas
    /// turbias mientras quede alguna dosis oculta y desaparece (la bandeja se recentra) cuando no queda ninguna; su insignia
    /// lleva el stock, el precio o el video (<see cref="RefreshRevealBadge"/>). Al aparecer o desaparecer, AF_LAYOUT se vuelve a
    /// emitir con el botón <c>reveal</c>, y también cuando aparece otra ranura después de emitirlo (la Cuia extra, en el primer
    /// fotograma del nivel): los botones de la bandeja se recolocan.</description></item>
    /// <item><description><b>Selección</b> (<see cref="OnRevealSelection"/>): las cuias turbias laten con un aro de luz
    /// (<see cref="RevealPulse"/>, en la capa de mecánicas delantera). Es la única excepción a D-002, y solo en este modo; el
    /// botón se queda pulsado y otro toque en él cancela. Los toques los enruta LevelFlow (T10).</description></item>
    /// <item><description><b>Revelado</b> (<see cref="OnRevealed"/>, con lo revelado ya aplicado, guardado y cobrado): la
    /// luciérnaga sale del botón y se posa en el borde de la cuia (<see cref="RevealFireflyFx"/>), y entonces su luz baja por
    /// el vidrio: <c>CuiaView.PlayReveal(mask, RevealStyle.Firefly)</c>, el pardo que se aclara de arriba abajo en 0,6 s ×
    /// factor y cada «?» que gira y muestra su fruta (T15). Al terminar, la cola registra la cuia revelada
    /// (<c>BoardAnimator.ShowContent</c>).</description></item>
    /// <item><description><b>Regalo</b> (<see cref="OnRevealGifted"/>, nivel 100): el aviso «+1 Luciérnaga» sale cuando se cierra
    /// la tarjeta previa (LEVEL_CARD), si la hay, o al empezar la intro.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para><b>Convivencia con la cola de animación</b>: el revelado lo anima la cuia (sus marcas), no la cola, así que la
    /// cola está quieta mientras la luciérnaga vuela. Si al elegir la cuia la cola aún animaba un vertido, salta a su final
    /// antes de despegar. Si algo cambia esa cuia mientras se revela (un vertido, Marea, Volver a la orilla, una cuia añadida)
    /// o la pantalla sale de PLAYING, el revelado termina en el acto: las capas quedan con su tinte y la cuia con el estado que
    /// le toca (el de la cola si ya lo pintó; si no, el revelado de antes del cambio, que es de donde parte la animación nueva).
    /// Marea, Volver a la orilla y la Cuia extra lo terminan antes de actuar (<see cref="SettleReveal"/>, desde
    /// <c>GameBootstrap</c>).</para>
    /// <para>Sin asignaciones por fotograma: las imágenes se crean una vez y los sprites de la luciérnaga, al cargar el nivel
    /// turbio (<see cref="RevealFireflyFx.Prewarm"/>); el video se consulta como mucho cada <see cref="RevealVideoPollMs"/> (y
    /// el de Marea, en la bandeja, cada <see cref="BoosterTray.UndoVideoPollMs"/>); la sonda de desarrollo
    /// (<c>DevProbe.Event("reveal", …)</c>) solo escribe en desarrollo y en los momentos del comodín.</para>
    /// </remarks>
    public sealed partial class GameScreen : IRevealListener
    {
        /// <summary>
        /// Cada cuánto se vuelve a preguntar a los anuncios si hay video de <c>rv_reveal</c> mientras la ranura lo ofrecería (sin
        /// stock ni saldo) [P]: preguntar crea la cadena de la fecha del contador diario, así que no se hace en cada fotograma
        /// (08 §5.4); el tope (3 al día) y la espera (60 s) no cambian más deprisa.
        /// </summary>
        internal const float RevealVideoPollMs = 500f;

        private RevealPulse _revealPulse = null!;
        private RevealFireflyFx _revealFx = null!;
        private bool _revealGiftPending;
        private int _revealGiftUnits;
        private int _revealCuia = -1;
        private int _revealMask;
        private Cuia _revealState;
        private bool _revealSealed;
        private int _revealBoardCount;
        private bool _revealMarks;
        private bool _revealPainted;
        private float _revealSpeed = 1f;
        private int _revealLayoutVersion = -1;
        private int _revealLayoutSlots = -1;
        private bool _revealVideoReady;
        private float _revealVideoPollMs;

        /// <summary>Pulso de las cuias turbias del modo de selección (tests y capturas).</summary>
        internal RevealPulse RevealPulse => _revealPulse;

        /// <summary>Luciérnaga del revelado (tests y capturas).</summary>
        internal RevealFireflyFx RevealFirefly => _revealFx;

        /// <summary>Hay un revelado de la Luciérnaga en marcha (la luciérnaga vuela o la cuia se aclara).</summary>
        internal bool RevealAnimating => _revealCuia >= 0;

        /// <summary>Cuia del revelado en marcha (−1 si no hay).</summary>
        internal int RevealAnimatingCuia => _revealCuia;

        /// <summary>La cuia del revelado ya empezó a aclararse (la luciérnaga se posó).</summary>
        internal bool RevealMarksStarted => _revealCuia >= 0 && _revealMarks;

        /// <summary>El aviso «+1 Luciérnaga» espera a que se cierre la tarjeta previa o empiece la intro.</summary>
        internal bool RevealGiftPending => _revealGiftPending;

        /// <summary>Ms que faltan para volver a preguntar por el video de <c>rv_reveal</c> (0 si la ranura no lo ofrecería; tests).</summary>
        internal float RevealVideoPollLeftMs => _revealVideoPollMs;

        /// <inheritdoc />
        public void OnRevealGifted(int units)
        {
            // Llega en LOADING, antes de la tarjeta previa o de la intro: el aviso sale cuando la tarjeta se cierra (T13).
            _revealGiftPending = true;
            _revealGiftUnits = units;
            LevelFlow flow = RequireFlow();
            RevealProbe("gift", flow, -1, 0, null);
        }

        /// <inheritdoc />
        public void OnRevealSelection(bool selecting)
        {
            LevelFlow flow = RequireFlow();
            if (selecting)
            {
                // 04 §2.5: «las cuias turbias pulsan». Solo ellas, y solo en este modo (excepción a D-002).
                _revealPulse.Show(MurkyMask(flow.Session.Current), _views, flow.ReduceMotion);
                _revealLayoutVersion = _animator.LayoutVersion;
                RevealProbe("select", flow, -1, 0, flow.RevealSelectionPayment == RevealPayment.Seeds ? "seeds" : "stock");
            }
            else
            {
                _revealPulse.Hide();
                RevealProbe("select_off", flow, -1, 0, null);
            }

            RefreshRevealBadge(flow, true);
        }

        /// <inheritdoc />
        public void OnRevealed(int cuia, int layerMask)
        {
            LevelFlow flow = RequireFlow();
            FinishReveal(flow, true); // otro revelado aún en marcha termina ya
            BoardState board = flow.Session.Current;
            RevealProbe("revealed", flow, cuia, layerMask, null);
            if ((uint)cuia >= (uint)_views.Length || (uint)cuia >= (uint)board.Count || layerMask == 0)
            {
                return;
            }

            // La cuia elegida deja de moverse antes de que llegue la luciérnaga; la vista ya cuenta con lo revelado (el
            // siguiente vertido parte de ahí y no lo vuelve a revelar).
            if (!_animator.IsIdle)
            {
                _animator.SnapAll();
            }

            _shownBoard = board;
            CuiaView view = _views[cuia];
            _revealCuia = cuia;
            _revealMask = layerMask;
            _revealState = board[cuia];
            _revealSealed = board.IsSealed(cuia);
            _revealBoardCount = board.Count;
            _revealMarks = false;
            _revealPainted = false;
            _revealSpeed = _animator.StepSpeedBase;
            Vector2 from = RevealButtonPoint();
            float glowMs = view.RevealStepMs(RevealStyle.Firefly) * _revealSpeed;
            _revealFx.Play(from, RevealPerch(view, from), view.Unit, _revealSpeed, glowMs, flow.ReduceMotion);
        }

        /// <summary>
        /// Termina en el acto el revelado en marcha, antes de una acción que cambia el tablero (Marea, Volver a la orilla, Cuia
        /// extra u otra Luciérnaga): la cuia queda revelada y la cola lo sabe. Sin revelado, no hace nada.
        /// </summary>
        internal void SettleReveal()
        {
            LevelFlow? flow = _flow;
            if (flow != null && flow.HasLevel)
            {
                FinishReveal(flow, true);
            }
        }

        /// <summary>
        /// Aviso de Revelar sin stock, sin saldo y sin video [P]: «Te faltan {n} semillas» (como el rescate), sin cobrar nada.
        /// </summary>
        internal void ShowRevealMissing(int missing)
        {
            ShowToast(UiText.RevealMissing(missing));
            LevelFlow? flow = _flow;
            if (flow != null && flow.HasLevel)
            {
                RevealProbe("missing", flow, -1, 0, null);
            }
        }

        // Al final del constructor, con la bandeja ya creada: la acción del botón, el pulso y la luciérnaga (en la capa de
        // mecánicas delantera, detrás de la intro, los chorros y los efectos).
        partial void RevealBuild()
        {
            _tray.SetRevealAction(() => _actions.PressReveal());
            _revealPulse = new RevealPulse(_mechanicFrontLayer, _art);
            _revealFx = new RevealFireflyFx(_mechanicFrontLayer, _art, _fx);
        }

        // Al final de LayoutHud (la bandeja ya se colocó): los aros se vuelven a colocar con la siguiente versión del tablero.
        partial void RevealLayout()
        {
            _revealLayoutVersion = -1;
        }

        partial void RevealTick(float dtMs)
        {
            // El plazo del video de Marea avanza con el fotograma, como el de Revelar más abajo: la bandeja lo pregunta en
            // Refresh (RefreshHud) como mucho cada BoosterTray.UndoVideoPollMs.
            _tray.Tick(dtMs);
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel)
            {
                return;
            }

            // En el modo de selección el toque elige la cuia que se revela, no un origen: la elevación de vista previa del
            // touch-down (02 §3.1) no vale aquí y OnCuiaDown ya no la empieza; si la selección se abre con una cuia ya pulsada
            // (otro dedo en el botón), esa cuia vuelve a su repisa.
            if (flow.RevealSelecting && _pressPreview >= 0)
            {
                SetLift(_pressPreview, false);
                _pressPreview = -1;
            }

            ShowRevealGiftIfReady(flow);
            if (_revealPulse.Active)
            {
                if (_revealLayoutVersion != _animator.LayoutVersion)
                {
                    _revealLayoutVersion = _animator.LayoutVersion;
                    _revealPulse.Layout(_views);
                }

                _revealPulse.Tick(dtMs);
            }

            TickReveal(flow, dtMs);
            if (_revealVideoPollMs > 0f)
            {
                _revealVideoPollMs -= dtMs;
            }

            RefreshRevealBadge(flow, false);

            // Si una ranura apareció o se fue después del último AF_LAYOUT (la Cuia extra aparece en el primer fotograma del
            // nivel, después de OnLevelLoaded), los botones de la bandeja cambiaron de sitio: se vuelve a emitir.
            if (_tray.SlotsVersion != _revealLayoutSlots)
            {
                EmitLevelLayout();
            }
        }

        partial void RevealLevelLoaded(LevelFlow flow)
        {
            // Vistas nuevas (BuildBoard): nada de lo del nivel anterior se pinta en ellas.
            _revealFx.Snap();
            _revealCuia = -1;
            _revealMarks = false;
            _revealPulse.Hide();
            _revealGiftPending = false;
            _revealLayoutVersion = -1;
            SyncRevealSlot(flow, true);
            if (_tray.RevealShown)
            {
                // Nivel turbio con ocultas: la luciérnaga genera ya sus sprites (cuerpo y alas), como MurkyMarks.Prewarm los
                // discos y los iconos de cada cuia turbia; ni el toque del primer revelado ni el vuelo crean texturas.
                _revealFx.Prewarm();
            }

            RefreshRevealBadge(flow, true);
        }

        partial void RevealBoardChanged(LevelFlow flow)
        {
            if (_revealCuia >= 0)
            {
                BoardState board = flow.Session.Current;
                int i = _revealCuia;
                bool changed = board.Count != _revealBoardCount || (uint)i >= (uint)board.Count || board[i] != _revealState
                    || board.IsSealed(i) != _revealSealed;
                if (changed || flow.Phase != FlowPhase.Playing)
                {
                    FinishReveal(flow, true);
                }
            }

            ShowRevealGiftIfReady(flow);
            SyncRevealSlot(flow, true);
            RefreshRevealBadge(flow, true);
        }

        partial void RevealLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            SyncRevealSlot(_flow, false);
            _revealLayoutSlots = _tray.SlotsVersion; // estas son las ranuras que salen en este AF_LAYOUT
            if (_tray.RevealShown)
            {
                ids.Add("reveal");
                rects.Add((RectTransform)_tray.RevealButton.transform);
            }
        }

        // Un fotograma del revelado: la luciérnaga vuela y, al posarse, la cuia se aclara; al terminar las marcas, la cola
        // registra la cuia revelada. Fuera de PLAYING (modal, celebración) termina en el acto.
        private void TickReveal(LevelFlow flow, float dtMs)
        {
            if (_revealCuia < 0)
            {
                return;
            }

            if (flow.Phase != FlowPhase.Playing)
            {
                FinishReveal(flow, true);
                return;
            }

            int i = _revealCuia;
            if ((uint)i >= (uint)_views.Length)
            {
                DropReveal(flow);
                return;
            }

            CuiaView view = _views[i];
            _revealFx.SetPerch(RevealPerch(view, _revealFx.From));
            RevealFireflyEvent e = _revealFx.Tick(dtMs);
            if (e == RevealFireflyEvent.Landed && !_revealMarks)
            {
                _revealMarks = true;
                if (view.PlayReveal(_revealMask, RevealStyle.Firefly, _revealSpeed) <= 0f)
                {
                    PaintRevealed(flow); // nada que aclarar (la vista ya no lo tenía oculto)
                }
            }

            if (_revealMarks && !_revealPainted && !view.MarksAnimating)
            {
                PaintRevealed(flow);
            }

            if (!_revealFx.Active)
            {
                if (!_revealPainted)
                {
                    if (_revealMarks && view.MarksAnimating)
                    {
                        view.SnapMarks();
                    }

                    PaintRevealed(flow);
                }

                DropReveal(flow);
            }
        }

        // Fin natural de las marcas: la cuia revelada, en la vista y en la cola (ShownOf), si nada la ha cambiado desde el toque.
        private void PaintRevealed(LevelFlow flow)
        {
            _revealPainted = true;
            BoardState board = flow.Session.Current;
            int i = _revealCuia;
            if ((uint)i < (uint)board.Count && (uint)i < (uint)_views.Length && board.Count == _revealBoardCount && board[i] == _revealState)
            {
                _animator.ShowContent(i, board[i], board.IsSealed(i));
            }
        }

        // Termina el revelado en el acto (ver la clase). Con paint, la cuia queda con el estado que le toca: el del modelo si la
        // cola está quieta; si no, el revelado de antes del cambio, que es de donde parte el trabajo nuevo (si la cola ya pintó
        // el estado de ahora, no se toca). En la intro (Volver a la orilla) la pinta la intro.
        private void FinishReveal(LevelFlow flow, bool paint)
        {
            int i = _revealCuia;
            if (i < 0)
            {
                return;
            }

            _revealFx.Snap();
            if ((uint)i < (uint)_views.Length)
            {
                CuiaView view = _views[i];
                if (view.MarksAnimating)
                {
                    view.SnapMarks();
                }

                bool introPaints = flow.Phase == FlowPhase.Intro || flow.Phase == FlowPhase.Loading || flow.Phase == FlowPhase.LevelCard;
                BoardState board = flow.Session.Current;
                if (paint && !_revealPainted && !introPaints && (uint)i < (uint)board.Count)
                {
                    _revealPainted = true;
                    if (_animator.IsIdle)
                    {
                        _animator.ShowContent(i, board[i], board.IsSealed(i));
                    }
                    else if (_animator.ShownOf(i) != board[i])
                    {
                        ((ICuiaBoard)_animator).ShowContent(i, _revealState, _revealSealed);
                    }
                }
            }

            DropReveal(flow);
        }

        private void DropReveal(LevelFlow flow)
        {
            _revealFx.Snap();
            _revealCuia = -1;
            _revealMarks = false;
            SyncRevealSlot(flow, true);
        }

        // La ranura existe en un nivel turbio con alguna oculta; si la última se reveló con la Luciérnaga, espera a que la
        // luciérnaga termine para irse (sale de ese botón). Al aparecer o desaparecer, AF_LAYOUT se vuelve a emitir (emit).
        private void SyncRevealSlot(LevelFlow? flow, bool emit)
        {
            bool want = flow != null && flow.HasLevel && flow.Level.IsMurky && flow.Session.HiddenCount > 0;
            if (!want && flow != null && _revealCuia >= 0 && flow.Phase == FlowPhase.Playing)
            {
                want = _tray.RevealShown;
            }

            if (_tray.SetRevealVisible(want) && emit && flow != null && flow.HasLevel)
            {
                EmitLevelLayout();
            }
        }

        // Insignia y habilitado de Revelar (BoosterTray.SetRevealBadge, que solo repinta si algo cambió). El video solo se
        // consulta sin stock ni saldo, que es cuando el botón lo pone, y en cada fotograma como mucho cada RevealVideoPollMs
        // (pollNow: en los avisos del flujo, en el acto). Sin ocultas, la ranura solo espera a que la luciérnaga salga de ella y
        // se vaya: gris y sin precio (revisión de la captura 67a, que enseñaba «120» en rojo). Y cuando no se puede usar ni se
        // está eligiendo (fuera de PLAYING: intro, tarjeta previa, al resolver), igual que la Cuia extra: gris, con su stock si
        // lo hay y, si no, sin precio ni «Te faltan {n}» (antes enseñaba el «120» en rojo y saltaba a «+1» con video al llegar a
        // PLAYING; revisión de T14). Los dos casos son bare, que SetRevealBadge dibuja gris y sin insignia, precio ni aviso.
        private void RefreshRevealBadge(LevelFlow flow, bool pollNow)
        {
            if (!_tray.RevealShown)
            {
                return;
            }

            int stock = flow.RevealStock;
            int price = flow.RevealPrice;
            int seeds = flow.Wallet?.Seeds ?? 0;
            bool available = flow.RevealAvailable;
            bool selecting = flow.RevealSelecting;
            bool spent = flow.Session.HiddenCount == 0;
            bool idle = !available && !selecting;
            bool offersVideo = available && !spent && stock == 0 && seeds < price && _services != null;
            if (!offersVideo)
            {
                _revealVideoReady = false;
                _revealVideoPollMs = 0f;
            }
            else if (pollNow || _revealVideoPollMs <= 0f)
            {
                _revealVideoReady = _services!.RewardedReady(AdPlacement.Reveal);
                _revealVideoPollMs = RevealVideoPollMs;
            }

            bool bare = spent || (idle && stock == 0);
            _tray.SetRevealBadge(stock, price, seeds, offersVideo && _revealVideoReady, available || selecting, selecting, bare);
        }

        // «+1 Luciérnaga» cuando ya no hay tarjeta previa abierta (02 §10; T14 §6): al cerrarla o, sin ella, al empezar la intro.
        private void ShowRevealGiftIfReady(LevelFlow flow)
        {
            if (!_revealGiftPending || flow.Phase == FlowPhase.Loading || flow.Phase == FlowPhase.LevelCard)
            {
                return;
            }

            _revealGiftPending = false;
            ShowToast(UiText.RevealGiftToast);
            RevealProbe("gift_toast", flow, -1, _revealGiftUnits, null);
        }

        // Punto de salida de la luciérnaga: el canto de arriba del botón Revelar, en las coordenadas del contenedor del tablero.
        private Vector2 RevealButtonPoint()
        {
            var rt = (RectTransform)_tray.RevealButton.transform;
            Rect r = rt.rect;
            return BoardPoint(rt.TransformPoint(new Vector3(r.center.x, r.yMax - 10f, 0f)));
        }

        // Dónde se posa: sobre el borde de la boca (con la cuia elevada, también), del lado por el que llega.
        private static Vector2 RevealPerch(CuiaView view, Vector2 from)
        {
            float u = view.Unit;
            Vector2 rim = view.FootPosition + view.BodyOffset + new Vector2(0f, view.HeightU * u);
            return RevealFireflyFx.PerchFor(rim, u, from.x <= rim.x ? -1f : 1f);
        }

        // Punto del mundo en las coordenadas del contenedor del tablero (origen arriba a la izquierda, y hacia arriba), las de
        // CuiaView.FootPosition y FxLayer.
        private Vector2 BoardPoint(Vector3 world)
        {
            Vector3 local = _mechanicFrontLayer.InverseTransformPoint(world);
            Rect lr = _mechanicFrontLayer.rect;
            return new Vector2(local.x - lr.xMin, local.y - lr.yMax);
        }

        private static int MurkyMask(BoardState board)
        {
            int mask = 0;
            for (int i = 0; i < board.Count && i < BoardState.MaxCuias; i++)
            {
                if (board[i].HiddenMask != 0)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        // DevProbe.Event("reveal", …): fase, nivel, cuia y capas, stock, semillas y ocultas que quedan (solo en desarrollo).
        private void RevealProbe(string phase, LevelFlow flow, int cuia, int value, string? payment)
        {
            if (!DevProbe.Enabled || !flow.HasLevel)
            {
                return;
            }

            CultureInfo inv = CultureInfo.InvariantCulture;
            string fields = "\"phase\":\"" + phase + "\",\"level\":" + flow.LevelNumber.ToString(inv)
                + ",\"cuia\":" + cuia.ToString(inv) + ",\"mask\":" + value.ToString(inv)
                + (payment != null ? ",\"payment\":\"" + payment + "\"" : string.Empty)
                + ",\"stock\":" + flow.RevealStock.ToString(inv) + ",\"seeds\":" + (flow.Wallet?.Seeds ?? 0).ToString(inv)
                + ",\"hidden\":" + flow.Session.HiddenCount.ToString(inv);
            _probe.Event("reveal", fields);
        }
    }
}
