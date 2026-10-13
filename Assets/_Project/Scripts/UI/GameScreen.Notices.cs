#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Avisos legibles del tablero (D-011, punto 2; prueba en el teléfono del 2026-10-11, §P2 y §P3): el aviso (<c>_toast</c>:
    /// regalo de Capi, «¡Una cuia más!», «+1 Luciérnaga», «Te faltan {n} semillas») y la tarjeta «¡Miquito a la vista!»
    /// (<c>_sighting</c>) salen de uno en uno, por orden de llegada, y cada uno dura lo que se tarda en leerlo
    /// (<see cref="NoticeTiming"/>: 1.000 ms + 55 ms por carácter, entre 3 y 7 s).
    /// </summary>
    /// <remarks>
    /// <para><b>Cuándo</b>: solo en PLAYING y sin victoria ni bloqueo pendientes (<c>NoticesHeld</c>). Ninguno durante LOADING,
    /// la tarjeta previa (LEVEL_CARD) ni la INTRO, que incluye el cartel «El río bajó» de la variante aliviada: lo que llega en esas
    /// fases (el regalo de Capi del 8 y la Luciérnaga del 100 llegan al cargar) espera en la cola y sale al habilitarse el toque
    /// (<c>OnIntroFinished</c>), como hacía ya el regalo de la Luciérnaga con la tarjeta previa. Desde el bloqueo lógico (antes
    /// de que se abra Aguas estancadas) y con un modal del embudo abierto, la cola espera a que se vuelva a jugar. Al abrirse
    /// un modal del embudo (Aguas estancadas, ¿Seguro?, Nivel fallido o Sin canoas), el aviso y la tarjeta que se vean se
    /// ocultan en el acto, sin salida, como en la celebración, y se descartan: no vuelven al cerrarlo (pregunta 42, opción (b),
    /// decidida por el dueño el 2026-10-13; <c>HideNoticesForModal</c>). Y desde la
    /// victoria lógica no sale ninguno: al empezar la celebración
    /// (<c>OnCelebrationStarted</c>) se ocultan el aviso y la tarjeta que se vean y se vacían la cola y la tarjeta pendiente, así
    /// que nada se dibuja encima de la celebración ni de la pantalla de victoria (un aviso dura hasta 7 s).</para>
    /// <para><b>Uno cada vez</b>: si hay un aviso o la tarjeta a la vista, el siguiente espera a que se oculte; nada se corta ni
    /// se pisa. El mismo texto que ya se ve vuelve a empezar (Revelar tocado otra vez sin saldo) y el que ya espera no se repite.
    /// Con la cola llena (<see cref="NoticeSlots"/>), el que llega se descarta.</para>
    /// <para><b>La tarjeta del miquito</b> espera a que la tropa se haya ido de la rama (<c>IntroAnimation.TroopVisible</c>: tras
    /// su reposo y su salida ya en juego, o tras el fundido de 100 ms si se saltó la intro) y entra entonces en la cola; mientras
    /// espera, las etiquetas del HUD siguen a la vista, y mientras se ve, se apartan como siempre (GameScreen.Labels.cs). Como
    /// los avisos, solo sale en PLAYING; si no cabe sin tapar cuias ni botones, no sale.</para>
    /// <para>Fuera de la cola, con su propia duración: el cartel (<c>_banner</c>) con «El río bajó» (04 §2.7) y el rótulo del
    /// intento. La cola se vacía al cargar un nivel, al empezar otro intento (<c>OnRestart</c>) y al empezar la celebración
    /// (<c>OnCelebrationStarted</c>); al volver de segundo plano (<c>SnapToModel</c>) no se toca: un aviso no es una animación
    /// del modelo y el regalo que espera a la intro no se pierde. Ningún aviso captura toques. Sin asignaciones: huecos
    /// preasignados y solo referencias a textos que ya existen.</para>
    /// <para><b>Capa</b>: el aviso y la tarjeta van en su propia capa (<c>_noticeLayer</c>), delante del tablero, del HUD y del
    /// polen y justo detrás de la capa de vertido: el origen que vierte se dibuja delante de ellos (D-006: delante de todo),
    /// como los modales y la pausa. El cartel (<c>_banner</c>) no va en ella.</para>
    /// <para><b>Dónde va el aviso</b> (<c>PlaceToast</c>; observación 1 de la prueba del 2026-10-13 y pregunta 41 (4); T52) [P]:
    /// con su alto de siempre (<see cref="Toast.DefaultHeightDp"/>) y su borde de abajo 8 dp sobre la bandeja, como antes. Si se
    /// ve alguna burbuja «Te faltan {n}» de la bandeja (<see cref="BoosterTray.TryGetMissingTop"/>), sube lo justo para quedar
    /// <see cref="NoticeBubbleGapDp"/> por encima de la más alta, con su canto: nunca la tapa. Y para no tapar el tablero, se
    /// encoge hasta su texto (<see cref="Toast.MinHeightDp"/>) si entre la repisa de la última fila (con su sombra,
    /// <see cref="ShelfShadowBelowFootDp"/> bajo el pie, donde va la ficha «?×n» de las cuias turbias) y su borde de abajo no
    /// cabe entero. Si ni así cabe (3 filas con burbujas de dos líneas: el 135), sube lo justo sobre las burbujas con el alto
    /// de su texto y tapa lo menos posible del canto inferior de esa repisa, nunca las cuias: es el compromiso que documentan
    /// las capturas 92 de T52. Se recoloca en cada fotograma si cambian las burbujas o el tablero (sin asignaciones; solo
    /// mide el texto cuando cambia).</para>
    /// </remarks>
    public sealed partial class GameScreen
    {
        /// <summary>Huecos de la cola de avisos (preasignados).</summary>
        internal const int NoticeSlots = 4;

        /// <summary>Aire entre el canto de abajo del aviso y la burbuja «Te faltan {n}» más alta de la bandeja (dp) [P].</summary>
        internal const float NoticeBubbleGapDp = 4f;

        /// <summary>Aire entre el aviso y lo más bajo del tablero (la sombra de la repisa de la última fila; dp) [P].</summary>
        internal const float NoticeBoardGapDp = 2f;

        /// <summary>
        /// Lo más bajo del tablero bajo el pie de la última fila (dp): la repisa (de pie − 3 a pie + 17, con la ficha «?×n» de
        /// la cuia turbia de pie + 1 a pie + 15) y su sombra de 6 dp; el canal y la ficha del Remolino tampoco pasan de ahí.
        /// </summary>
        internal const float ShelfShadowBelowFootDp = 23f;

        /// <summary>Borde de abajo del aviso de siempre sobre el borde de arriba de la bandeja (dp).</summary>
        private const float NoticeAboveTrayDp = 8f;

        private readonly RectTransform _noticeLayer;
        private readonly Notice[] _notices = new Notice[NoticeSlots];
        private int _noticeHead;
        private int _noticeCount;
        private FaunaSpecies? _sightingPending;

        /// <summary>Avisos que esperan turno (sin contar el que se ve; tests).</summary>
        internal int NoticesQueued => _noticeCount;

        /// <summary>La tarjeta del miquito espera a que la tropa se vaya (aún no está en la cola; tests).</summary>
        internal bool SightingPending => _sightingPending != null;

        /// <summary>El aviso del tablero (tests: duración y toques).</summary>
        internal Toast NoticeToast => _toast;

        /// <summary>Capa del aviso y de la tarjeta: delante del HUD, detrás de la capa de vertido (tests de orden de capas).</summary>
        internal RectTransform NoticeLayer => _noticeLayer;

        // Los avisos solo salen en PLAYING sin victoria ni bloqueo pendientes: en LOADING, LEVEL_CARD e INTRO esperan a que se
        // habilite el toque; desde el bloqueo lógico (la vista termina el último vertido y espera StuckModalDelayMs, aún en
        // PLAYING) y con un modal (STUCK_MODAL, GIVE_UP_CONFIRM, LEVEL_FAILED, NO_CANOES), a que se vuelva a jugar, para que
        // ninguno empiece y se lea detrás de Aguas estancadas (el rescate quita el pendiente antes de añadir la cuia, así que
        // «¡Una cuia más!» sale en el acto si no espera otro); y desde la victoria lógica (el último vertido aún se anima),
        // CELEBRATION, VICTORY_SCREEN y AD_GATE no sale ninguno: la celebración vacía la cola (OnCelebrationStarted) y el nivel
        // siguiente empieza sin nada (OnLevelLoaded).
        private static bool NoticesHeld(LevelFlow? flow)
        {
            if (flow == null || !flow.HasLevel)
            {
                return true;
            }

            return flow.Phase != FlowPhase.Playing || flow.IsResolving;
        }

        // Aviso del tablero: a la cola; sale ya si no hay nada a la vista ni esperando y la fase lo permite.
        private void ShowToast(string text)
        {
            if (_toast.Visible && string.Equals(_toast.Text, text, StringComparison.Ordinal))
            {
                ShowToastNow(text); // el mismo aviso otra vez (Revelar sin saldo): vuelve a empezar, como antes de la cola
                return;
            }

            for (int k = 0; k < _noticeCount; k++)
            {
                if (string.Equals(_notices[(_noticeHead + k) % NoticeSlots].Text, text, StringComparison.Ordinal))
                {
                    return; // ya espera su turno
                }
            }

            EnqueueNotice(text, null);
            PumpNotices();
        }

        private void ShowToastNow(string text)
        {
            _toast.Show(text, NoticeTiming.ReadMs(text));
            PlaceToast(true);
        }

        // El aviso, sin tapar las burbujas «Te faltan {n}» ni, si cabe, el tablero (ver la clase). Cada fotograma con el aviso a
        // la vista, tras repintar la bandeja (GameScreen.Tick); solo lo mueve si cambia su sitio (force: al mostrarlo).
        private void PlaceToast(bool force)
        {
            if (!_toast.Visible)
            {
                return;
            }

            float bottom = TrayTop - NoticeAboveTrayDp;
            if (_tray.TryGetMissingTop(out float bubbleTop))
            {
                bottom = Mathf.Min(bottom, bubbleTop - NoticeBubbleGapDp - Toast.EdgeDp);
            }

            float height = Toast.DefaultHeightDp;
            float floor = BoardContentBottom();
            if (!float.IsNegativeInfinity(floor))
            {
                float room = bottom - (floor + NoticeBoardGapDp);
                if (room < height)
                {
                    height = Mathf.Max(_toast.MinHeightDp(_canvasW), room);
                }
            }

            float top = bottom - height;
            if (force || Mathf.Abs(top - _toast.Top) > 0.01f || Mathf.Abs(height - _toast.Height) > 0.01f || float.IsNaN(_toast.Top))
            {
                _toast.Place(_canvasW, top, height);
            }
        }

        // Lo más bajo del tablero en el lienzo (dp desde arriba): la sombra de la repisa de la última fila, con el tablero tal
        // como está desplazado; −∞ sin tablero.
        private float BoardContentBottom()
        {
            BoardLayoutResult? l = _layout;
            if (l == null || _views.Length == 0 || l.Rows <= 0)
            {
                return float.NegativeInfinity;
            }

            return l.RowFootY[l.Rows - 1] + ShelfShadowBelowFootDp - _boardShift;
        }

        // Pregunta 42, opción (b) (decidida por el dueño el 2026-10-13; T52): al abrirse un modal del embudo, el aviso y la tarjeta
        // del miquito que se vean se ocultan en el acto, sin salida (como en la celebración), y se descartan. La cola y la
        // tarjeta pendiente siguen esperando a que se vuelva a jugar (NoticesHeld).
        private void HideNoticesForModal()
        {
            _toast.Hide();
            _sighting.Hide();
        }

        // Aguas estancadas, ¿Seguro?, Nivel fallido y Sin canoas.
        private static bool IsFunnelModal(FlowPhase phase)
        {
            return phase == FlowPhase.StuckModal || phase == FlowPhase.GiveUpConfirm || phase == FlowPhase.LevelFailed
                || phase == FlowPhase.NoCanoes;
        }

        private void EnqueueNotice(string? text, FaunaSpecies? species)
        {
            if (_noticeCount >= NoticeSlots)
            {
                return;
            }

            int slot = (_noticeHead + _noticeCount) % NoticeSlots;
            _notices[slot].Text = text;
            _notices[slot].Species = species;
            _noticeCount++;
        }

        // Saca de la cola el siguiente aviso si no hay ninguno a la vista y la fase lo permite (cada fotograma, al encolar y al
        // habilitarse el toque).
        private void PumpNotices()
        {
            while (_noticeCount > 0 && !_toast.Visible && !_sighting.Visible && !NoticesHeld(_flow))
            {
                string? text = _notices[_noticeHead].Text;
                FaunaSpecies? species = _notices[_noticeHead].Species;
                _notices[_noticeHead] = default;
                _noticeHead = (_noticeHead + 1) % NoticeSlots;
                _noticeCount--;
                if (text != null)
                {
                    ShowToastNow(text);
                }
                else if (species != null)
                {
                    ShowSightingNow(species); // si no cabe, no sale y pasa el siguiente
                }
            }
        }

        // Cada fotograma, tras el Tick del aviso y de la tarjeta (el que se acaba de ocultar deja pasar al siguiente en el
        // mismo fotograma): la tarjeta pendiente entra en la cola cuando la tropa se ha ido.
        private void TickNotices()
        {
            // Red de seguridad de HideNoticesForModal: con un modal del embudo abierto por un camino que no avisó, nada a la vista.
            LevelFlow? flow = _flow;
            if (flow != null && flow.HasLevel && IsFunnelModal(flow.Phase) && (_toast.Visible || _sighting.Visible))
            {
                HideNoticesForModal();
            }

            if (_sightingPending != null && !_intro.TroopVisible && !NoticesHeld(_flow))
            {
                EnqueueNotice(null, _sightingPending);
                _sightingPending = null;
            }

            PumpNotices();
        }

        // Fuera todo: el aviso y la tarjeta a la vista, la cola y la tarjeta pendiente (otro nivel, otro intento o la celebración).
        private void ClearNotices()
        {
            for (int i = 0; i < NoticeSlots; i++)
            {
                _notices[i] = default;
            }

            _noticeHead = 0;
            _noticeCount = 0;
            _sightingPending = null;
            _toast.Hide();
            _sighting.Hide();
        }

        // «¡Miquito a la vista!» (guion de la intro §4.5 y §14): al habilitarse el input tras la intro, en el primer intento del
        // primer nivel libre desde el nivel_tarjeta de la especie del tramo (nunca la de otro mundo) y solo con su arte
        // (FaunaArtAvailable: fuera de desarrollo, sin el PNG del retrato no sale). Queda pendiente hasta que la tropa se vaya
        // (TickNotices). Sin persistir ni af_fauna_sighted: eso exige migrar el perfil (Q11, PR aparte).
        private void MaybeShowSighting(LevelFlow flow)
        {
            FaunaCatalog? fauna = Fauna;
            if (_introRetry || fauna == null || _layout == null || !flow.HasLevel)
            {
                return;
            }

            int level = flow.LevelNumber;
            FaunaSpecies? candidate = fauna.ForLevel(level);
            bool art = candidate != null && FaunaArtAvailable(candidate, true);
            FaunaSpecies? species = IntroCast.Resolve(fauna, flow.Level.World, level, 1, art);
            if (species == null || !SightingRule.ShowsCard(level, species.CardLevel, true))
            {
                return;
            }

            _sightingPending = species;
        }

        // Sobre la franja del panorama si mide ≥ 64 dp; si no, sobre el muelle, bajo la última fila y por encima de la bandeja;
        // si no cabe sin tapar cuias ni botones, no sale.
        private void ShowSightingNow(FaunaSpecies species)
        {
            BoardLayoutResult? l = _layout;
            if (l == null)
            {
                return;
            }

            float band = l.PanelY - BoardTop;
            float top;
            if (band >= 64f)
            {
                top = BoardTop + ((band - SightingCard.HeightDp) * 0.5f);
            }
            else
            {
                top = l.PanelBottom + 4f;
                if (top + SightingCard.HeightDp > TrayTop - 8f)
                {
                    return;
                }
            }

            _sighting.Place(_canvasW, top);
            _sighting.Show(_art.MonkeyArtFor(species).Portrait, species.NameIn("es"), UiText.FaunaFact(species.FactKey));
        }

        private struct Notice
        {
            // Texto del aviso; null = la tarjeta del miquito de Species.
            public string? Text;
            public FaunaSpecies? Species;
        }
    }
}
