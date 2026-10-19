#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Avisos legibles del tablero (D-011, punto 2; prueba en el teléfono del 2026-10-11, §P2 y §P3): el aviso (<c>_toast</c>:
    /// regalo de Capi, «¡Una cuia más!» y «+1 Luciérnaga»), la tarjeta «¡Miquito a la vista!»
    /// (<c>_sighting</c>), la tarjeta «Fruta nueva» (<c>_fruitNew</c>, tarea F5) y el rótulo del lugar (<c>_stopLabel</c>, tarea
    /// M2.3) salen de uno en uno, por orden de llegada, y cada uno dura lo que se tarda en leerlo (<see cref="NoticeTiming"/>:
    /// 1.000 ms + 55 ms por carácter, entre 3 y 7 s). La cola va por tipos (<see cref="NoticeKind"/>: Text, Species, StopLabel,
    /// FruitNew, Sloth y Tutorial). <b>La instrucción del tutorial con TiTino</b> (tipo Tutorial, tarea TU1a; GameScreen.Cards.cs)
    /// entra la primera de la cola en los niveles 1, 2, 3 y 6. <b>La tarjeta «Fruta nueva»</b> (<see cref="FruitNewCard"/>; 06
    /// §4.5) entra en la cola al habilitarse el toque en el estreno de un tinte (<see cref="SightingRule.FruitNewAt"/>: 4, 7, 11,
    /// 21, 41, 61, 81, 111, 141 y 201; nunca con instrucción del tutorial), solo en el primer intento; como la del miquito,
    /// espera a que la tropa se vaya, va en su mismo sitio, aparta las etiquetas del HUD mientras se ve y la ocultan lo mismo (un modal del embudo, otro intento, otro nivel, la celebración
    /// o, sobre el muelle, una burbuja que sale debajo). Con 3 filas (el 111 y el 201), donde no cabe ni en la franja ni sobre el
    /// muelle, va como placa de una línea clavada al marco, como el rótulo del lugar, y se va si una cuia de la fila 1 que queda
    /// debajo se eleva hasta ella o si cambia el tablero [P]. Sonda <c>AF_STATE fruit_new</c> (<c>mode</c>: band, dock o plate; none,
    /// con <c>ms</c> 0, si no cabe en ninguno y no sale).
    /// <para><b>El aviso de TiTino tras Volver a la orilla</b> (<see cref="SlothNoticeCard"/>; tarea TI6b; D-016, T5-B; 04 §2.7;
    /// propuesta de TiTino d.5; guion de la intro §14): en los niveles 1-30, al habilitarse el toque tras la intro corta del
    /// reinicio, solo en el primer reinicio del nivel (en esta sesión [P]) y solo si el nivel no tiene instrucción del tutorial
    /// (<see cref="HasTutorialInstruction"/>: TiTino ya está en ella), con su retrato de 40 dp y su frase
    /// (<c>sloth_line_restart_*</c>; la n.º 21 si el reinicio rompió la Crecida, que aún no existe en el cliente [P]) por
    /// <see cref="SlothLines.ForRestartNotice"/>, con los topes de la sesión de TiTino. Sin frase que enseñar (mensajes apagados,
    /// topes, sin texto aprobado en release o sin su arte) no hay aviso [P]: el retrato solo no dice nada. Es una tarjeta de la
    /// familia del miquito: entra en la cola (tipo Sloth), espera a que la tropa se vaya, va sobre la franja del panorama si mide
    /// ≥ 64 dp y si no sobre el muelle (si tampoco cabe, no sale), aparta las etiquetas del HUD mientras se ve, la ocultan lo
    /// mismo que a la del miquito y dura por lectura (D-011). Como mucho una tarjeta por nivel: tras un reinicio no sale ninguna
    /// de las otras (solo van en el primer intento). Sonda <c>AF_STATE sloth_restart</c>.</para>
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
    /// se pisa. El mismo texto que ya se ve vuelve a empezar y el que ya espera no se repite.
    /// Con la cola llena (<see cref="NoticeSlots"/>), el que llega se descarta.</para>
    /// <para><b>La tarjeta del miquito</b> espera a que la tropa se haya ido de la rama (<c>IntroAnimation.TroopVisible</c>: tras
    /// su reposo y su salida ya en juego, o tras el fundido de 100 ms si se saltó la intro) y entra entonces en la cola; mientras
    /// espera, las etiquetas del HUD siguen a la vista, y mientras se ve, se apartan como siempre (GameScreen.Labels.cs). Como
    /// los avisos, solo sale en PLAYING; si no cabe sin tapar cuias, botones ni las burbujas «Te faltan {n}» de la bandeja, no
    /// sale, y si una burbuja sale después bajo la tarjeta del muelle, la tarjeta se oculta (A1, criterio 3).</para>
    /// <para><b>El rótulo del lugar</b> (<see cref="StopLabel"/>; D-013, punto 8; propuesta del Mapa §10.5, punto 5): entra en la
    /// cola al habilitarse el toque (<c>OnIntroFinished</c>) en los niveles de <see cref="StopLabelRule"/> (31, 51, 71, 91, 101,
    /// 121, 131, 151, 161, 171, 181 y 191 en 1-200), solo en el primer intento y nunca al reanudar; como la tarjeta del miquito,
    /// sale cuando la tropa se ha ido de la rama, y mientras se ve aparta las etiquetas del HUD. Con 1-2 filas va sobre el
    /// horizonte; con 3, como placa bajo el HUD. Lo ocultan y lo descartan lo mismo que al aviso: un modal del embudo, otro
    /// intento, otro nivel y la celebración. Sonda <c>AF_STATE stop_label</c>.</para>
    /// <para><b>La placa y la fila 1</b> (observación 3 de la prueba de la ola 2 en el teléfono; tarea FX4) [P]: con la placa a
    /// la vista, nada de la fila 1 queda bajo ella, ni elevado. Al enseñarla se mide lo más alto de cada cuia de la fila 1 en
    /// reposo, con su corcho y lo que su mecánica asoma sobre la boca, y lo que ocupa a lo ancho (los gráficos visibles de su
    /// cuerpo y de sus anclas de overlay, <see cref="StopLabelRowOneTop"/>), y la placa va en el primero de estos sitios donde
    /// cabe en una línea a 12 sp o más:
    /// <list type="number">
    /// <item><description>clavada al marco y centrada, <see cref="StopLabel.PlateCuiaGapDp"/> por encima de toda la fila 1
    /// (sonda <c>mode</c> = <c>plate</c>);</description></item>
    /// <item><description>a la misma altura, de lado: centrada en el tramo libre más ancho entre las cuias de la fila 1 que no
    /// le dejan sitio encima, con <see cref="StopLabel.PlateSideAirDp"/> a cada lado (la Hoja en la fila 1: en el 171 su
    /// medallón llega a 0,70 u sobre la boca y la placa va sobre las cuias 2 y 3; <c>mode</c> = <c>plate_side</c>; revisión
    /// de FX4);</description></item>
    /// <item><description>sobre el muelle, bajo el panel y por encima de la bandeja y de sus burbujas, como la tarjeta del
    /// miquito, centrada en ese hueco (<c>mode</c> = <c>dock</c>).</description></item>
    /// </list>
    /// Si no cabe en ninguno, no sale (sonda <c>stop_label_off</c> con <c>reason</c> = <c>no_room</c>). Clavada al marco no
    /// cabe nunca sobre una cuia elevada (en 360 × 780 dp quedan ~12 dp entre su borde de arriba y la cuia elevada), así que al
    /// elevarse una cuia de la fila 1 que queda bajo la placa (a lo ancho, con <see cref="StopLabel.PlateSideAirDp"/>) hasta
    /// menos de <see cref="StopLabel.PlateLiftAirDp"/> de ella (contando el rebote de la elevación,
    /// <see cref="StopLabel.LiftReachDp"/>), la placa se oculta en el acto y se descarta, como el aviso ante un modal
    /// (<c>reason</c> = <c>lift</c>; también si ya estaba elevada al llegarle el turno). Sobre el muelle, si después sale debajo
    /// una burbuja «Te faltan {n}», se oculta igual (<c>reason</c> = <c>bubble</c>); y con el rótulo a la vista, si cambia el
    /// tablero y ya no cabe, <c>no_room</c>, o si cambia el número de filas, <c>board</c>.</para>
    /// <para>Fuera de la cola, con su propia duración: el cartel (<c>_banner</c>) con «El río bajó» (04 §2.7) y el rótulo del
    /// intento. La cola se vacía al cargar un nivel, al empezar otro intento (<c>OnRestart</c>) y al empezar la celebración
    /// (<c>OnCelebrationStarted</c>); al volver de segundo plano (<c>SnapToModel</c>) no se toca: un aviso no es una animación
    /// del modelo y el regalo que espera a la intro no se pierde. Ningún aviso captura toques. Sin asignaciones: huecos
    /// preasignados y solo referencias a textos que ya existen.</para>
    /// <para><b>Capa</b>: el aviso y la tarjeta van en su propia capa (<c>_noticeLayer</c>), delante del tablero, del HUD y del
    /// polen y justo detrás de la capa de vertido: el origen que vierte se dibuja delante de ellos (D-006: delante de todo),
    /// como los modales y la pausa. El cartel (<c>_banner</c>) no va en ella.</para>
    /// <para><b>Dónde va el aviso</b> (<c>PlaceToast</c>; observación 1 de la prueba del 2026-10-13 y pregunta 41 (4); T52 y su
    /// revisión) [P]. Las alturas van en dp del lienzo, que crecen hacia abajo (la convención de <see cref="BoardLayoutResult"/>):
    /// «pie + n» es n dp bajo la línea de pie de la fila y «pie − n», n dp por encima. Con su alto de siempre
    /// (<see cref="Toast.DefaultHeightDp"/>, o el que pide su texto con la escala de texto o si no cabe ni a 12 sp:
    /// <see cref="Toast.PreferredHeightDp"/>, tarea A1) y su borde de abajo 8 dp sobre la bandeja, como antes. Si se ve alguna burbuja
    /// «Te faltan {n}» de la bandeja (<see cref="BoosterTray.TryGetMissingTop"/>), sube lo justo para quedar
    /// <see cref="NoticeBubbleGapDp"/> por encima de la más alta, con su canto: nunca la tapa. Para no tapar
    /// el tablero, se encoge hasta su texto (<see cref="Toast.MinHeightDp"/>) si entre la repisa de la última fila (con su
    /// sombra, <see cref="ShelfShadowBelowFootDp"/> bajo el pie, donde van la ficha «?×n» de las cuias turbias y la de la pareja
    /// del Remolino) y su borde de abajo no cabe entero. Si ni así cabe, tapa parte de esa repisa, nunca las cuias. Pasa con 3
    /// filas y las dos burbujas en dos líneas (sin semillas ni video): en el 135 a 360 × 780 dp, entre el pie de la ficha «?×1»
    /// y la burbuja más alta quedan 24 dp, y el aviso más bajo ocupa 29 + 3 de canto + 4 de aire. Entonces, para no dejar a
    /// medias una ficha («?×n» cuenta las dosis ocultas, la única información oculta del juego: 02, T1 y T3):</para>
    /// <list type="bullet">
    /// <item><description>se ajusta a su texto también a lo ancho (<see cref="Toast.FitWidthDp"/>) y va en el tramo más ancho de
    /// esa repisa sin fichas, con <see cref="NoticeChipClearDp"/> de aire, lo más centrado posible: tapa la cara de la repisa
    /// en su tramo, no fichas. Es lo que pasa en el 135 tras el rescate (12 cuias; captura 92 de T52): «¡Una cuia más! Sigue
    /// desde aquí», de unos 253 dp, va a la derecha de la ficha «?×1» (de 73 a 326 dp) y tapa la cara de la repisa de abajo en
    /// ese tramo durante los 3 s que dura. Solo cuentan
    /// como fichas la «?×n» y la de la pareja del Remolino (<c>CollectNoticeChips</c>): si una cuia de una pareja del
    /// Remolino está en la fila de abajo, el aviso también puede tapar en parte, en su tramo, lo que de ese Remolino queda
    /// bajo el pie (la bajada del canal por el frente de la repisa; el tramo horizontal pegado bajo ella, de pie + 17 a pie +
    /// 21,5; y, si el aviso sube más, la mitad de abajo del remanso, que llega a pie + 12,6 con u = 24 dp) durante lo que
    /// dura (3-7 s), y el canal se ve cortado. Pasa en el 103 tras el rescate (captura 92g de T52): con la cuia 8 de la pareja
    /// bajo la primera cuia de la fila de abajo, «¡Una cuia más!» va a la derecha de su ficha (de 53,5 a 306,5 dp a lo ancho y
    /// desde pie + 12,5, es decir, 12,5 dp bajo el pie, por debajo de las cuias) y tapa la bajada y el tramo horizontal; el
    /// remanso, no;</description></item>
    /// <item><description>si así no cabe, va de su ancho de siempre y sube hasta tapar enteras las fichas que alcanza: su borde
    /// de arriba, <see cref="NoticeChipCoverDp"/> sobre ellas y nunca por encima del pie de la fila. Así no asoma una tira
    /// suelta de la ficha, que parecía un fallo de dibujo (captura 92 de la primera versión de T52), pero tapa los 23 dp bajo el
    /// pie: la repisa de abajo y sus fichas durante lo que dura el aviso (3-7 s), y con una pareja del Remolino en esa fila,
    /// también la mitad de abajo de su remanso y su canal en ese tramo. Se da en el juego cuando una ficha parte el tramo de la
    /// repisa: en el 115 tras el rescate (12 cuias, la ficha «?×2» bajo la tercera cuia de la fila de abajo; captura 92f de
    /// T52). Y en el 135 tras el rescate con la escala de texto al 130 % (A1): ajustado a su texto ya no cabe junto a la ficha
    /// «?×1», así que la tapa entera con su borde de arriba en el pie de la fila (la placa se queda en el hueco, sin pasar de
    /// ahí, y su texto se encoge hacia 12 sp × 1,3; MinTextSizeTests y a1_07_l135_3_filas_130).</description></item>
    /// </list>
    /// <para>Este compromiso lo ratificó el dueño (pregunta 47, (1); D-022): se deja como está y se prueba (capturas 92, 92f y
    /// 92g de T52; MinTextSnapshotTests, A1). Se descartó dibujar las burbujas delante del aviso: aunque el aviso se encogiera,
    /// taparían su texto, y eso choca con D-011, punto 2 (avisos legibles). Revelar sin stock, saldo ni video ya no saca aviso:
    /// responde su burbuja «Te faltan {n}», que ya lo dice (pregunta 48 (b), D-022; <see cref="ShowRevealMissing"/>). Se
    /// recoloca en cada fotograma si cambian las burbujas, las fichas o el tablero (sin asignaciones; solo mide el texto cuando
    /// cambia).</para>
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

        /// <summary>Aire a lo ancho entre el aviso ajustado a su texto y una ficha de la repisa que no tapa (dp) [P].</summary>
        internal const float NoticeChipClearDp = 4f;

        /// <summary>Lo que el borde de arriba del aviso queda sobre una ficha de la repisa que tapa entera (dp) [P].</summary>
        internal const float NoticeChipCoverDp = 2f;

        /// <summary>
        /// La tarjeta del miquito va sobre la franja del panorama si mide al menos esto (dp; 07 §6.4: «sobre la franja del
        /// panorama si mide ≥ 64 dp»).
        /// </summary>
        internal const float SightingBandDp = 64f;

        /// <summary>
        /// Si la tarjeta del miquito crece (el dato en dos líneas o la escala de texto; A1), la franja tiene que medir además su
        /// alto + esto (dp) [P]: cabe entera y centrada, con el aire de la tarjeta de 60 dp en la franja de 64. Con el dato en
        /// una línea a la escala 1 (61 dp), el umbral sigue siendo <see cref="SightingBandDp"/>.
        /// </summary>
        internal const float SightingBandAirDp = 3f;

        /// <summary>Borde de abajo del aviso de siempre sobre el borde de arriba de la bandeja (dp).</summary>
        private const float NoticeAboveTrayDp = 8f;

        // Fichas de la repisa como mucho (la «?×n» y la del Remolino de cada cuia).
        private const int MaxNoticeChips = 2 * BoardState.MaxCuias;

        private readonly RectTransform _noticeLayer;
        private readonly Notice[] _notices = new Notice[NoticeSlots];
        private readonly float[] _chipX0 = new float[MaxNoticeChips];
        private readonly float[] _chipX1 = new float[MaxNoticeChips];
        private readonly float[] _chipY0 = new float[MaxNoticeChips];
        private readonly float[] _chipY1 = new float[MaxNoticeChips];
        private readonly float[] _chipFoot = new float[MaxNoticeChips];
        private int _noticeHead;
        private int _noticeCount;
        private FaunaSpecies? _sightingPending;
        private float _sightingDockBottom = float.NaN; // borde de abajo de la placa de la tarjeta sobre el muelle; NaN si no
        private readonly FruitNewCard _fruitNew; // «Fruta nueva» (tarea F5); la crea el constructor de GameScreen
        private float _fruitNewDockBottom = float.NaN; // como _sightingDockBottom, para la tarjeta «Fruta nueva»
        private int _fruitNewLevel; // nivel cuya tarjeta «Fruta nueva» ya entró en la cola en esta visita (0: ninguno)
        private readonly StringBuilder _fruitNewProbeSb = new StringBuilder(128);
        private string? _lastFruitNewProbe;
        private BoardLayoutResult? _fruitNewPlateLayout; // tablero con el que se colocó la placa de «Fruta nueva» (3 filas)
        private float _fruitNewPlateShift;
        private float _fruitNewPlateCanvasW;
        private bool _fruitNewCoversPost; // la tarjeta «Fruta nueva» va sobre el muelle y tapa el poste: el poste se funde con ella
        private readonly StopLabel _stopLabel;
        private readonly StringBuilder _stopLabelProbeSb = new StringBuilder(256);
        private string? _lastStopLabelProbe;
        private int _stopLabelLevel; // nivel cuyo rótulo ya entró en la cola en esta visita (0: ninguno)
        private BoardLayoutResult? _stopLabelLayout; // tablero y lienzo con los que se colocó el rótulo a la vista
        private float _stopLabelCanvasW;
        private float _stopLabelHorizon;
        private float _stopLabelShift;
        private StopLabelSpot _stopLabelSpot; // dónde va la placa (3 filas)
        private readonly float[] _stopLabelRestTop = new float[BoardState.MaxCuias]; // lo más alto de cada cuia de la fila 1
        private readonly float[] _stopLabelRestLeft = new float[BoardState.MaxCuias]; // y lo que ocupa a lo ancho (dp)
        private readonly float[] _stopLabelRestRight = new float[BoardState.MaxCuias];
        private readonly float[] _stopLabelBlockLeft = new float[BoardState.MaxCuias]; // tramos que no deja la fila 1 (de lado)
        private readonly float[] _stopLabelBlockRight = new float[BoardState.MaxCuias];
        private float _stopLabelSideLeft = float.NaN; // tramo libre donde se centró la placa de lado (dp; NaN si no va de lado)
        private float _stopLabelSideRight = float.NaN;
        private readonly List<Graphic> _stopLabelGraphics = new List<Graphic>(64);
        private SlothNoticeCard? _slothNotice; // aviso de TiTino tras Volver a la orilla (TI6b): perezoso, lo crea el primero
        private float _slothNoticeDockBottom = float.NaN; // como _sightingDockBottom, para el aviso de TiTino
        private bool _slothCoversPost; // la tarjeta de TiTino va sobre el muelle y tapa el poste: el poste se funde con ella
        private int _slothRestartLevel; // nivel cuyos reinicios se cuentan (0: ninguno)
        private int _slothRestarts; // reinicios de ese nivel en esta sesión
        private readonly Vector3[] _stopLabelCorners = new Vector3[4];

        /// <summary>Avisos que esperan turno (sin contar el que se ve; tests).</summary>
        internal int NoticesQueued => _noticeCount;

        /// <summary>La tarjeta del miquito espera a que la tropa se vaya (aún no está en la cola; tests).</summary>
        internal bool SightingPending => _sightingPending != null;

        /// <summary>
        /// La tarjeta «¡Miquito a la vista!» de su turno en la cola salió a la vista (Q11 del guion de la intro, D-029; tarea
        /// DC15): la especie y el nivel. Lo engancha <see cref="GameBootstrap"/> para apuntar el avistamiento en el perfil
        /// (<c>fauna.sighted</c>) y escribir la sonda <c>af_fauna_sighted</c>. Se llama una vez por tarjeta que sale; si la tarjeta
        /// no sale (se gana antes de que se vaya la tropa, no cabe o la tapa algo de más prioridad), no se llama. El camino de
        /// tests y capturas (<see cref="ShowSighting"/>) no lo llama. Null: nada (tests sin juego).
        /// </summary>
        internal Action<FaunaSpecies, int>? SightingShown { get; set; }

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
                ShowToastNow(text); // el mismo aviso otra vez: vuelve a empezar, como antes de la cola
                return;
            }

            for (int k = 0; k < _noticeCount; k++)
            {
                ref Notice queued = ref _notices[(_noticeHead + k) % NoticeSlots];
                if (queued.Kind == NoticeKind.Text && string.Equals(queued.Text, text, StringComparison.Ordinal))
                {
                    return; // ya espera su turno
                }
            }

            EnqueueNotice(NoticeKind.Text, text, null, null);
            PumpNotices();
        }

        private void ShowToastNow(string text)
        {
            _toast.Show(text, NoticeTiming.ReadMs(text));
            PlaceToast(true);
        }

        /// <summary>
        /// Un aviso del tablero por la cola, como los del juego (tests y capturas: el rescate o «+1 Luciérnaga» sin pasar por su
        /// camino entero).
        /// </summary>
        internal void ShowNotice(string text)
        {
            ShowToast(text);
        }

        // El aviso, sin tapar las burbujas «Te faltan {n}» ni, si cabe, el tablero (ver la clase). Cada fotograma con el aviso a
        // la vista, tras repintar la bandeja (GameScreen.Tick); solo lo mueve si cambia su sitio (force: al mostrarlo). Es también
        // el punto de cada fotograma tras repintar la bandeja para la tarjeta del miquito sobre el muelle
        // (KeepSightingClearOfBubbles).
        private void PlaceToast(bool force)
        {
            KeepSightingClearOfBubbles();
            if (!_toast.Visible)
            {
                return;
            }

            float bottom = TrayTop - NoticeAboveTrayDp;
            if (_tray.TryGetMissingTop(out float bubbleTop))
            {
                bottom = Mathf.Min(bottom, bubbleTop - NoticeBubbleGapDp - Toast.EdgeDp);
            }

            float width = Toast.FullWidthDp(_canvasW);
            float left = (_canvasW - width) * 0.5f;
            float height = _toast.PreferredHeightDp(_canvasW);
            float floor = BoardContentBottom();
            if (!float.IsNegativeInfinity(floor))
            {
                float room = bottom - (floor + NoticeBoardGapDp);
                if (room < height)
                {
                    float min = _toast.MinHeightDp(_canvasW);
                    height = Mathf.Max(min, room);
                    if (room < min)
                    {
                        // Llega a la repisa de abajo: que no deje a medias ninguna de sus fichas.
                        float top0 = bottom - height;
                        PlaceToastBesideChips(bottom, ref left, ref width, ref top0);
                        height = bottom - top0;
                    }

                    // Nunca por encima del pie de la última fila: no tapa las cuias (pregunta 47). Si ni a su alto mínimo cabe
                    // (con la escala de texto y 3 filas: las burbujas «Te faltan {n}» crecen con ella), la placa se queda en el
                    // hueco y su texto se encoge, nunca por debajo de 12 sp (tarea A1).
                    float foot = BoardLastRowFoot();
                    if (bottom - height < foot)
                    {
                        height = Mathf.Max(0f, bottom - foot);
                    }
                }
            }

            float top = bottom - height;
            if (force || float.IsNaN(_toast.Top) || Mathf.Abs(top - _toast.Top) > 0.01f || Mathf.Abs(height - _toast.Height) > 0.01f
                || Mathf.Abs(left - _toast.Left) > 0.01f || Mathf.Abs(width - _toast.Width) > 0.01f)
            {
                _toast.PlaceAt(left, top, width, height);
            }
        }

        // El aviso que llega a la repisa de la última fila (ver la clase): ajustado a su texto en el tramo más ancho sin fichas,
        // lo más centrado posible; si no cabe, de su ancho de siempre y con su borde de arriba sobre las fichas que alcanza, para
        // taparlas enteras, sin pasar del pie de su fila. Sin asignaciones.
        private void PlaceToastBesideChips(float bottom, ref float left, ref float width, ref float top)
        {
            int n = CollectNoticeChips();
            float seenBottom = bottom + Toast.EdgeDp;
            if (!AnyChipIn(n, top, seenBottom))
            {
                return;
            }

            float fit = _toast.FitWidthDp(_canvasW);
            if (fit < width - 0.01f && TryChipFreeSpan(left, left + width, fit, n, top, seenBottom, out float x))
            {
                left = x;
                width = fit;
                return;
            }

            // Tapa enteras las fichas que alcanza; al subir puede alcanzar otra de la misma repisa (la del Remolino baja más que
            // la «?×n»): se repite hasta que no cambia (como mucho una vez por ficha).
            for (int pass = 0; pass <= n; pass++)
            {
                bool moved = false;
                for (int i = 0; i < n; i++)
                {
                    if (_chipY1[i] <= top || _chipY0[i] >= seenBottom)
                    {
                        continue;
                    }

                    float want = Mathf.Max(_chipFoot[i], _chipY0[i] - NoticeChipCoverDp);
                    if (want < top - 0.01f)
                    {
                        top = want;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    break;
                }
            }
        }

        // Las fichas de la repisa a la vista (la «?×n» de las cuias con ocultas y la de la pareja del Remolino), en el lienzo
        // (dp desde arriba), con el pie de su cuia. La «?×n» se cuenta aunque su cuia esté en vuelo (se oculta mientras vierte y
        // vuelve después): así el aviso no salta durante un vertido.
        private int CollectNoticeChips()
        {
            BoardLayoutResult? l = _layout;
            if (l == null)
            {
                return 0;
            }

            int n = 0;
            int count = Mathf.Min(_views.Length, l.CenterX.Length);
            for (int i = 0; i < count && n < MaxNoticeChips - 1; i++)
            {
                CuiaView view = _views[i];
                float x = l.CenterX[i];
                float foot = l.FootY[i] - _boardShift;
                if (view.Murky.BadgeCount > 0)
                {
                    float half = 0.5f * ProceduralArt.MurkyBadgeWidthDp;
                    float y0 = foot + MurkyMarks.BadgeGapDp;
                    AddNoticeChip(ref n, x - half, x + half, y0, y0 + ProceduralArt.MurkyBadgeDp, foot);
                }

                if (view.OverlayOf(MechanicType.Remolino) is RemolinoOverlay whirl && whirl.PairMarkVisible)
                {
                    Rect r = whirl.PairMarkRect; // dp desde el centro del pie, hacia arriba
                    AddNoticeChip(ref n, x + r.xMin, x + r.xMax, foot - r.yMax, foot - r.yMin, foot);
                }
            }

            return n;
        }

        private void AddNoticeChip(ref int n, float x0, float x1, float y0, float y1, float foot)
        {
            _chipX0[n] = x0;
            _chipX1[n] = x1;
            _chipY0[n] = y0;
            _chipY1[n] = y1;
            _chipFoot[n] = foot;
            n++;
        }

        private bool AnyChipIn(int n, float top, float bottom)
        {
            for (int i = 0; i < n; i++)
            {
                if (_chipY1[i] > top && _chipY0[i] < bottom)
                {
                    return true;
                }
            }

            return false;
        }

        // El sitio (borde izquierdo) de un aviso de width dp entre lo y hi sin tocar, con NoticeChipClearDp de aire, ninguna
        // ficha que alcance a lo alto (de top a bottom): en el tramo libre que lo deja más cerca del centro del lienzo. Sin
        // asignaciones (las fichas se recorren por orden con una búsqueda del siguiente hueco, ≤ 32).
        private bool TryChipFreeSpan(float lo, float hi, float width, int n, float top, float bottom, out float left)
        {
            float center = 0.5f * _canvasW;
            float best = float.PositiveInfinity;
            left = lo;
            float cursor = lo;
            while (cursor < hi)
            {
                // La siguiente ficha que alcanza, a la derecha del cursor (o que lo cubre): la de borde izquierdo más a la izquierda.
                float nextX0 = hi;
                float nextX1 = hi;
                for (int i = 0; i < n; i++)
                {
                    if (_chipY1[i] <= top || _chipY0[i] >= bottom)
                    {
                        continue;
                    }

                    float x0 = _chipX0[i] - NoticeChipClearDp;
                    float x1 = _chipX1[i] + NoticeChipClearDp;
                    if (x1 > cursor && x0 < nextX0)
                    {
                        nextX0 = x0;
                        nextX1 = x1;
                    }
                }

                float gapEnd = Mathf.Min(nextX0, hi);
                if (gapEnd - cursor >= width - 0.01f)
                {
                    float x = Mathf.Clamp(center - (0.5f * width), cursor, gapEnd - width);
                    float d = Mathf.Abs(x + (0.5f * width) - center);
                    if (d < best)
                    {
                        best = d;
                        left = x;
                    }
                }

                if (nextX0 >= hi)
                {
                    break;
                }

                cursor = Mathf.Max(cursor, nextX1);
            }

            return !float.IsPositiveInfinity(best);
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

        // El pie de la última fila en el lienzo (dp desde arriba), con el tablero tal como está desplazado: lo más bajo de sus
        // cuias. Solo con tablero (BoardContentBottom finito).
        private float BoardLastRowFoot()
        {
            BoardLayoutResult l = _layout!;
            return l.RowFootY[l.Rows - 1] - _boardShift;
        }

        // Pregunta 42, opción (b) (decidida por el dueño el 2026-10-13; T52): al abrirse un modal del embudo, el aviso y la tarjeta
        // del miquito que se vean se ocultan en el acto, sin salida (como en la celebración), y se descartan. La cola y la
        // tarjeta pendiente siguen esperando a que se vuelva a jugar (NoticesHeld).
        // En el juego llegan aquí con un aviso a la vista OnStuckModalOpened y, desde el juego, OnFunnelPhase al abrir Nivel
        // fallido (Pausa › «Salir» confirmado desde el 31) y Sin canoas (Volver a la orilla con 0 canoas). Son defensivas las
        // demás: OnFunnelPhase con ¿Seguro? (solo desde Aguas estancadas, que ya ocultó el aviso) y con Nivel fallido o Sin
        // canoas dentro del embudo, OnResumed (al reanudar, OnLevelLoaded ya vació la cola) y la red de seguridad de
        // TickNotices. NoticeHooksDefensiveTests prueba los dos caminos del juego y llama a las defensivas fuera de orden, con
        // el aviso y la tarjeta forzados a la vista, y sin flujo (revisión de T52, U1).
        private void HideNoticesForModal()
        {
            _toast.Hide();
            _sighting.Hide();
            _fruitNew.Hide();
            _stopLabel.Hide();
            _slothNotice?.Hide();
        }

        // Aguas estancadas, ¿Seguro?, Nivel fallido y Sin canoas.
        private static bool IsFunnelModal(FlowPhase phase)
        {
            return phase == FlowPhase.StuckModal || phase == FlowPhase.GiveUpConfirm || phase == FlowPhase.LevelFailed
                || phase == FlowPhase.NoCanoes;
        }

        // Un aviso del tipo kind al final de la cola: el texto (Text y StopLabel), la especie (Species), la parada (StopLabel) y el
        // tinte de «Fruta nueva» (FruitNew: índice en SightingRule.FruitDebutTints). Con la cola llena, se descarta.
        private void EnqueueNotice(NoticeKind kind, string? text, FaunaSpecies? species, RiverStop? stop, int fruit = -1)
        {
            EnqueueNotice(kind, text, species, stop, fruit, default);
        }

        // Como el de arriba, con lo que decidió SlothLines para el aviso de TiTino (Sloth: se apunta en la sesión al enseñarlo).
        private void EnqueueNotice(NoticeKind kind, string? text, FaunaSpecies? species, RiverStop? stop, int fruit, in SlothLine sloth)
        {
            if (_noticeCount >= NoticeSlots)
            {
                return;
            }

            int slot = (_noticeHead + _noticeCount) % NoticeSlots;
            _notices[slot].Kind = kind;
            _notices[slot].Text = text;
            _notices[slot].Species = species;
            _notices[slot].Stop = stop;
            _notices[slot].Fruit = fruit;
            _notices[slot].Sloth = sloth;
            _noticeCount++;
        }

        // Un aviso al principio de la cola (la instrucción del tutorial, la primera en prioridad: TU1a). Con la cola llena, se
        // descarta el último que espera, para que la instrucción no se pierda.
        private void EnqueueNoticeFirst(NoticeKind kind, string text)
        {
            if (_noticeCount >= NoticeSlots)
            {
                _notices[(_noticeHead + _noticeCount - 1) % NoticeSlots] = default;
                _noticeCount--;
            }

            _noticeHead = (_noticeHead + NoticeSlots - 1) % NoticeSlots;
            _notices[_noticeHead] = default;
            _notices[_noticeHead].Kind = kind;
            _notices[_noticeHead].Text = text;
            _notices[_noticeHead].Fruit = -1;
            _noticeCount++;
        }

        /// <summary>
        /// Hay un aviso de la cola a la vista: el aviso, la tarjeta del miquito, la de «Fruta nueva», el rótulo del lugar o la
        /// tarjeta de TiTino (el aviso tras Volver a la orilla, TI6b, o la instrucción del tutorial, TU1a).
        /// </summary>
        internal bool NoticeShowing => _toast.Visible || _sighting.Visible || _fruitNew.Visible || _stopLabel.Visible || SlothNoticeVisible;

        /// <summary>
        /// La tarjeta de TiTino está a la vista: el aviso tras Volver a la orilla (TI6b) o la instrucción del tutorial (TU1a, que
        /// usa la misma tarjeta: <see cref="TutorialSlothVisible"/>).
        /// </summary>
        internal bool SlothNoticeVisible => _slothNotice != null && _slothNotice.Visible;

        // Saca de la cola el siguiente aviso si no hay ninguno a la vista y la fase lo permite (cada fotograma, al encolar y al
        // habilitarse el toque). El rótulo del lugar, la tarjeta «Fruta nueva» y el aviso de TiTino (este, también al rótulo del
        // intento) esperan además a que la tropa se vaya de la rama,
        // como la tarjeta del miquito (van en la franja del panorama o bajo el HUD, donde se sienta): mientras esperan, los que
        // van detrás también.
        private void PumpNotices()
        {
            while (_noticeCount > 0 && !NoticeShowing && !NoticesHeld(_flow))
            {
                ref Notice head = ref _notices[_noticeHead];
                bool slothCard = head.Kind == NoticeKind.Sloth || head.Kind == NoticeKind.Tutorial;
                if ((head.Kind == NoticeKind.StopLabel || head.Kind == NoticeKind.FruitNew || slothCard) && _intro.TroopVisible)
                {
                    return;
                }

                if (slothCard && _banner.Visible)
                {
                    // La tarjeta de TiTino (el aviso y la instrucción del tutorial) va tras la intro corta y su rótulo «Nivel N ·
                    // intento k», que ocupa su franja (TI6b y TU1a).
                    return;
                }

                NoticeKind kind = head.Kind;
                string? text = head.Text;
                FaunaSpecies? species = head.Species;
                RiverStop? stop = head.Stop;
                int fruit = head.Fruit;
                SlothLine sloth = head.Sloth;
                head = default;
                _noticeHead = (_noticeHead + 1) % NoticeSlots;
                _noticeCount--;
                switch (kind)
                {
                    case NoticeKind.Text:
                        if (text != null)
                        {
                            ShowToastNow(text);
                        }

                        break;

                    case NoticeKind.Species:
                        if (species != null)
                        {
                            ShowSightingNow(species); // si no cabe, no sale y pasa el siguiente
                            if (_sighting.Visible)
                            {
                                NotifySightingShown(species); // el avistamiento, al quedar a la vista (DC15)
                            }
                        }

                        break;

                    case NoticeKind.StopLabel:
                        if (text != null && stop != null)
                        {
                            ShowStopLabelNow(text, stop); // sin tablero no sale y pasa el siguiente
                        }

                        break;

                    case NoticeKind.FruitNew:
                        if (fruit >= 0)
                        {
                            ShowFruitNewNow(fruit); // sin textos o si no cabe, no sale y pasa el siguiente
                        }

                        break;

                    case NoticeKind.Sloth:
                        if (text != null)
                        {
                            ShowSlothNoticeNow(text, in sloth); // si no cabe, no sale y pasa el siguiente
                        }

                        break;

                    case NoticeKind.Tutorial:
                        if (text != null)
                        {
                            ShowTutorialNow(text); // sin TiTino o sin sitio, como aviso de texto: la instrucción no se pierde
                        }

                        break;
                }
            }
        }

        // Cada fotograma, tras el Tick del aviso y de la tarjeta (el que se acaba de ocultar deja pasar al siguiente en el
        // mismo fotograma): la tarjeta pendiente entra en la cola cuando la tropa se ha ido.
        private void TickNotices()
        {
            // Red de seguridad de HideNoticesForModal: con un modal del embudo abierto por un camino que no avisó, nada a la vista.
            LevelFlow? flow = _flow;
            if (flow != null && flow.HasLevel && IsFunnelModal(flow.Phase) && NoticeShowing)
            {
                HideNoticesForModal();
            }

            if (_sightingPending != null && !_intro.TroopVisible && !NoticesHeld(_flow))
            {
                EnqueueNotice(NoticeKind.Species, null, _sightingPending, null);
                _sightingPending = null;
            }

            PumpNotices();
            KeepStopLabelPlaced();
            KeepFruitNewPlateClear();
            KeepPostUnderFruitNew();
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
            _tutorialAfterPour = null; // el tooltip de Marea que esperaba un vertido (TU1a; GameScreen.Cards.cs)
            _toast.Hide();
            _sighting.Hide();
            _fruitNew.Hide();
            _stopLabel.Hide();
            _slothNotice?.Hide();
        }

        /// <summary>Veces que salió el consejo de la Tinaja del segundo tablero de su estreno desde que se creó la pantalla (tests).</summary>
        internal int TinajaTipShownCount { get; private set; }

        // Consejo del segundo tablero del estreno de la Tinaja (02 §9.1 M13: «Si sacas dos veces, la primera dosis queda debajo.»;
        // TinajaTutorial.TipLevel, el 324 [P]; tarea TJ4): una línea en el aviso de texto, la primera de la cola, al habilitarse
        // el toque tras la intro (también tras Volver a la orilla: el dilema de las dos dosis es de cada intento), solo si el nivel
        // lleva tinaja; al reanudar no pasa por aquí (sin intro). Sin texto en este idioma, nada (nunca otro idioma; D-018).
        private void MaybeShowTinajaTip(LevelFlow flow)
        {
            if (!flow.HasLevel || TinajaTutorial.TipKeyFor(flow.LevelNumber, flow.Level.HasTinaja) == null)
            {
                return;
            }

            string text = UiText.TutorialTinajaTip;
            if (text.Length == 0)
            {
                return;
            }

            EnqueueNoticeFirst(NoticeKind.Text, text);
            TinajaTipShownCount++;
            if (DevProbe.Enabled)
            {
                _probe.Event("tinaja_tip", "\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture)
                    + ",\"key\":\"" + TinajaTutorial.TipKey + "\",\"retry\":" + (_introRetry ? "true" : "false"));
            }
        }

        // «¡Miquito a la vista!» (guion de la intro §4.5 y §14): al habilitarse el input tras la intro, en el primer intento del
        // primer nivel libre desde el nivel_tarjeta de la especie del tramo (nunca la de otro mundo) y solo con su arte
        // (FaunaArtAvailable: fuera de desarrollo, sin el PNG del retrato no sale). Queda pendiente hasta que la tropa se vaya
        // (TickNotices). El avistamiento (fauna.sighted y af_fauna_sighted; Q11, D-029, DC15) se apunta cuando la tarjeta sale
        // a la vista (PumpNotices → SightingShown), no aquí: si no sale, no hay avistamiento.
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

        // Sobre la franja del panorama si mide al menos SightingBandDp (64 dp, 07 §6.4) y, si la tarjeta crece (el dato en dos
        // líneas o la escala de texto; A1), al menos su alto + SightingBandAirDp, para que quepa entera y centrada: con el dato
        // en una línea a la escala 1 (61 dp), el umbral es el de 07 §6.4. Si no, sobre el muelle, bajo la última fila y por
        // encima de la bandeja y de sus burbujas «Te faltan {n}» (SightingDockLimit); si no cabe sin tapar cuias, botones ni
        // burbujas, no sale.
        private void ShowSightingNow(FaunaSpecies species)
        {
            BoardLayoutResult? l = _layout;
            _sightingDockBottom = float.NaN;
            if (l == null)
            {
                return;
            }

            // El nombre en el idioma de la UI, de la tabla de textos largos (D-018; tarea FX5: antes salía del catálogo tal cual
            // y en en-XA no se pseudolocalizaba).
            string name = UiText.FaunaName(species.NameKey);
            string fact = UiText.FaunaFact(species.FactKey);
            float height = _sighting.Measure(_canvasW, name, fact);
            float band = l.PanelY - BoardTop;
            float top;
            if (band >= Mathf.Max(SightingBandDp, height + SightingBandAirDp))
            {
                top = BoardTop + ((band - height) * 0.5f);
            }
            else
            {
                top = l.PanelBottom + 4f;
                if (top + height > SightingDockLimit())
                {
                    return;
                }

                _sightingDockBottom = top + height;
            }

            _sighting.Place(_canvasW, top);
            _sighting.Show(_art.MonkeyArtFor(species).Portrait, name, fact);
        }

        // Avisa del avistamiento de la tarjeta que acaba de salir, con el nivel en curso (DC15). Sin nivel, nada.
        private void NotifySightingShown(FaunaSpecies species)
        {
            Action<FaunaSpecies, int>? shown = SightingShown;
            LevelFlow? flow = _flow;
            if (shown != null && flow != null && flow.HasLevel)
            {
                shown(species, flow.LevelNumber);
            }
        }

        /// <summary>
        /// La tarjeta del miquito de <paramref name="species"/> en el acto, por su camino de colocación (tests y capturas: el
        /// muelle con una burbuja «Te faltan {n}» a la vista, sin esperar a su nivel_tarjeta). Devuelve si sale.
        /// </summary>
        internal bool ShowSighting(FaunaSpecies species)
        {
            _sighting.Hide();
            ShowSightingNow(species);
            return _sighting.Visible;
        }

        /// <summary>La tarjeta del miquito va sobre el muelle (no en la franja del panorama; tests).</summary>
        internal bool SightingOnDock => _sighting.Visible && !float.IsNaN(_sightingDockBottom);

        // Lo más bajo que puede llegar la placa de la tarjeta sobre el muelle (dp desde arriba): 8 dp sobre la bandeja, como el
        // aviso, y, si se ve alguna burbuja «Te faltan {n}» (pregunta 41 (4)), con su canto NoticeBubbleGapDp por encima de la
        // más alta: no la tapa (A1, criterio 3; revisión de A1: antes solo miraba la bandeja).
        private float SightingDockLimit()
        {
            float limit = TrayTop - NoticeAboveTrayDp;
            if (_tray.TryGetMissingTop(out float bubbleTop))
            {
                limit = Mathf.Min(limit, bubbleTop - NoticeBubbleGapDp - SightingCard.EdgeDp);
            }

            return limit;
        }

        // Con la tarjeta sobre el muelle, una burbuja «Te faltan {n}» que sale después debajo de ella (p. ej. tras gastar
        // semillas en un comodín) se taparía: la tarjeta no puede subir sin tapar las cuias, así que se oculta en el acto, como
        // ante un modal. Cada fotograma, con la bandeja ya repintada (desde PlaceToast); sin asignaciones.
        private void KeepSightingClearOfBubbles()
        {
            if (!float.IsNaN(_slothNoticeDockBottom) && SlothNoticeVisible && _slothNoticeDockBottom > SightingDockLimit() + 0.01f)
            {
                _slothNotice!.Hide(); // el aviso de TiTino sobre el muelle, igual (TI6b)
                _slothNoticeDockBottom = float.NaN;
            }

            if (!float.IsNaN(_fruitNewDockBottom) && _fruitNew.Visible && _fruitNewDockBottom > SightingDockLimit() + 0.01f)
            {
                _fruitNew.Hide(); // «Fruta nueva» sobre el muelle, igual (F5)
                _fruitNewDockBottom = float.NaN;
            }

            if (float.IsNaN(_sightingDockBottom) || !_sighting.Visible)
            {
                return;
            }

            if (_sightingDockBottom > SightingDockLimit() + 0.01f)
            {
                _sighting.Hide();
                _sightingDockBottom = float.NaN;
            }
        }

        /// <summary>
        /// Solo tests: enciende o apaga el aviso de TiTino tras Volver a la orilla (null = encendido, como en el juego). Los tests
        /// PlayMode que no tratan de él lo apagan (<c>PlayModeHelpers</c>), para que sus reinicios en 1-30 sigan como antes de
        /// TI6b; los que lo prueban (<c>SlothFailTests</c>) lo encienden y lo vuelven a null en su TearDown.
        /// </summary>
        internal static bool? SlothRestartNoticeOverride { get; set; }

        /// <summary>
        /// Niveles de 1-30 con instrucción del tutorial (02 §13): el dedo guía de 1 y 2, el tutorial de Marea del 3, la frase del
        /// 6 y la presentación de Morpho del 12, con el retrato de TiTino (D-016, T2-A), y el 8, el de Capi con la Cuia extra y
        /// «Reiniciar es gratis…» [P]. En ellos no hay aviso de TiTino tras Volver a la orilla (guion de la intro §14: manda la
        /// instrucción, y TiTino sale como mucho una vez). Los de TiTino que ya se pintan son los de
        /// <see cref="TutorialInstruction.Levels"/> (1, 2, 3 y 6, TU1a, y 12, TU1b; GameScreen.Cards.cs).
        /// </summary>
        internal static readonly int[] SlothTutorialLevels = { 1, 2, 3, 6, 8, 12 };

        /// <summary>El nivel <paramref name="level"/> tiene instrucción del tutorial (<see cref="SlothTutorialLevels"/>).</summary>
        internal static bool HasTutorialInstruction(int level)
        {
            return Array.IndexOf(SlothTutorialLevels, level) >= 0;
        }

        /// <summary>El aviso de TiTino (null hasta el primero; tests).</summary>
        internal SlothNoticeCard? SlothNoticeView => _slothNotice;

        /// <summary>El aviso de TiTino va sobre el muelle (no en la franja del panorama; tests).</summary>
        internal bool SlothNoticeOnDock => SlothNoticeVisible && !float.IsNaN(_slothNoticeDockBottom);

        /// <summary>Lo que decidió TiTino en el último reinicio de 1-30 (default antes del primero; tests).</summary>
        internal SlothLine LastSlothRestartLine { get; private set; }

        // Aviso de TiTino tras Volver a la orilla (ver la clase): al habilitarse el toque (OnIntroFinished) tras la intro corta de
        // un reinicio en 1-30 (Volver a la orilla desde la cabecera, la pausa o el pie del modal; en 1-30 no hay Nivel fallido),
        // cuenta el reinicio y, si es el primero del nivel y no tiene instrucción del tutorial, pide la frase a SlothLines y la
        // pone en la cola. Al reanudar no pasa por aquí (OnResumed: sin intro).
        private void MaybeShowSlothRestart(LevelFlow flow)
        {
            if (!_introRetry || !flow.HasLevel)
            {
                return;
            }

            int level = flow.LevelNumber;
            if (level < 1 || level > SlothLines.RestartNoticeLastLevel)
            {
                return;
            }

            if (_slothRestartLevel != level)
            {
                _slothRestartLevel = level;
                _slothRestarts = 0;
            }

            _slothRestarts++;
            LastSlothRestartLine = default;
            if (!(SlothRestartNoticeOverride ?? true) || !SlothEnabled)
            {
                return;
            }

            SlothSession session = SlothSession;
            session.Messages = _slothVoiceOverride ?? SlothMessagesSetting;
            SlothContext context = session.Context.WithRestart(new SlothRestartFacts(_slothRestarts == 1, HasTutorialInstruction(level), false));
            SlothLine line = SlothLines.ForRestartNotice(level, in context);
            LastSlothRestartLine = line;
            if (!line.Speaks || !UiText.TrySlothLine(in line, out string text))
            {
                return; // sin frase no hay aviso [P]: ni se enseña ni se apunta
            }

            EnqueueNotice(NoticeKind.Sloth, text, null, null, -1, in line);
        }

        // El aviso de TiTino, en el sitio de la tarjeta del miquito (ShowSightingNow): sobre la franja del panorama si mide al
        // menos SightingBandDp (y su alto + SightingBandAirDp); si no, sobre el muelle, por encima de la bandeja y de sus burbujas;
        // si tampoco cabe, o sin el arte de TiTino, no sale. Al enseñarlo, la frase se apunta en la sesión de TiTino.
        private void ShowSlothNoticeNow(string text, in SlothLine line)
        {
            BoardLayoutResult? l = _layout;
            _slothNoticeDockBottom = float.NaN;
            SlothArt? art = _art.SlothRigArt();
            if (l == null || art == null)
            {
                return;
            }

            _slothNotice ??= new SlothNoticeCard(_noticeLayer, _art);
            float height = _slothNotice.Measure(_canvasW, text);
            if (height <= 0f)
            {
                return;
            }

            if (!PlaceSlothCard(l, height))
            {
                EmitSlothRestartProbe(line, 0, "none");
                return;
            }

            LevelFlow? flow = _flow;
            _slothNoticeTutorial = false;
            _slothNotice.Show(art.Head, flow != null && flow.ReduceMotion);
            SlothSession session = SlothSession;
            session.Record(in line, session.Context);
            EmitSlothRestartProbe(line, (int)_slothNotice.DurationMs, float.IsNaN(_slothNoticeDockBottom) ? "band" : "dock");
        }

        // La tarjeta de TiTino ya medida (height), en el sitio de la tarjeta del miquito (ShowSightingNow): sobre la franja del
        // panorama si mide al menos SightingBandDp (y su alto + SightingBandAirDp); si no, sobre el muelle, por encima de la
        // bandeja y de sus burbujas (y entonces apunta su borde de abajo en _slothNoticeDockBottom). False si tampoco cabe ahí.
        // La usan el aviso tras Volver a la orilla (TI6b) y la instrucción del tutorial (TU1a).
        private bool PlaceSlothCard(BoardLayoutResult l, float height)
        {
            float band = l.PanelY - BoardTop;
            float top;
            if (band >= Mathf.Max(SightingBandDp, height + SightingBandAirDp))
            {
                top = BoardTop + ((band - height) * 0.5f);
            }
            else
            {
                top = l.PanelBottom + 4f;
                if (top + height > SightingDockLimit())
                {
                    return false;
                }

                _slothNoticeDockBottom = top + height;
            }

            _slothNotice!.Place(_canvasW, top);
            _slothCoversPost = !float.IsNaN(_slothNoticeDockBottom) && CardOverPost(_slothNotice.Root, SlothNoticeCard.EdgeDp);
            return true;
        }

        /// <summary>
        /// El aviso de TiTino con <paramref name="text"/> en el acto, por su camino de colocación (tests y capturas: el muelle con
        /// una burbuja «Te faltan {n}» a la vista, sin pasar por un reinicio). No se apunta en la sesión. Devuelve si sale.
        /// </summary>
        internal bool ShowSlothNotice(string text)
        {
            _slothNotice?.Hide();
            var line = new SlothLine(SlothScreen.Restart, SlothGesture.Encourage, SlothMoment.Restart, SlothData.NoLine, null, 0, false);
            ShowSlothNoticeNow(text, in line);
            return SlothNoticeVisible;
        }

        // AF_STATE sloth_restart: nivel, clave de la frase, duración y sitio (band, dock o, si no cabe y no sale, none). Solo con
        // la sonda; al enseñarlo, nunca por fotograma.
        private void EmitSlothRestartProbe(in SlothLine line, int ms, string mode)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            var sb = new StringBuilder(128);
            sb.Append("\"level\":").Append((_flow?.LevelNumber ?? 0).ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "line", line.Key);
            sb.Append(",\"ms\":").Append(ms.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "mode", mode);
            _probe.Event("sloth_restart", sb.ToString());
        }

        /// <summary>
        /// Solo tests: enciende o apaga la tarjeta «Fruta nueva» (null = encendida, como en el juego). Los tests PlayMode que no
        /// tratan de ella la apagan (<c>PlayModeHelpers</c>), para que sus avisos y sus capturas del 2, 4, 7, 11, 21, 41… salgan
        /// como antes de F5.
        /// </summary>
        internal static bool? FruitNewOverride { get; set; }

        /// <summary>La tarjeta «Fruta nueva» (tests).</summary>
        internal FruitNewCard FruitNewView => _fruitNew;

        /// <summary>La tarjeta «Fruta nueva» va sobre el muelle (no en la franja del panorama; tests).</summary>
        internal bool FruitNewOnDock => _fruitNew.Visible && !float.IsNaN(_fruitNewDockBottom);

        /// <summary>Campos de la última sonda <c>AF_STATE fruit_new</c> (null si no salió ninguna o sin sonda; tests).</summary>
        internal string? LastFruitNewProbe => _lastFruitNewProbe;

        // «Fruta nueva» (06 §4.5; tarea F5): al habilitarse el toque (OnIntroFinished), antes que la tarjeta del miquito y que el
        // rótulo (prioridad tutorial > Fruta nueva > miquito > rótulo; en estos niveles ninguno de los dos sale: SightingRule.IsBusy
        // y StopLabelRule.IsTaken), entra en la cola si lo dice SightingRule.FruitNewAt: en el estreno de un tinte sin tutorial
        // (desde el 4), en el primer intento (ni la intro corta del reintento ni un nivel con fallos) y una sola vez por visita al nivel. Al
        // reanudar no pasa por aquí (OnResumed: sin intro). Sale cuando la tropa se ha ido (PumpNotices).
        private void MaybeShowFruitNew(LevelFlow flow)
        {
            if (!flow.HasLevel || !(FruitNewOverride ?? true))
            {
                return;
            }

            int level = flow.LevelNumber;
            bool firstAttempt = !_introRetry && flow.Attempt.FailsLevel == 0 && _fruitNewLevel != level;
            int fruit = SightingRule.FruitNewAt(level, firstAttempt);
            if (fruit < 0)
            {
                return;
            }

            _fruitNewLevel = level;
            EnqueueNotice(NoticeKind.FruitNew, null, null, null, fruit);
        }

        // La tarjeta «Fruta nueva» del tinte fruit, en el sitio de la del miquito (ShowSightingNow): sobre la franja del panorama
        // si cabe; si no, sobre el muelle, por encima de la bandeja y de sus burbujas; si tampoco (3 filas), como placa de una
        // línea clavada al marco; y si ni así, no sale. Sin el nombre o la frase en el idioma de la UI, tampoco
        // (FruitNewCard.TryTexts). Suena sfx_fruit_new afinado a la fruta (AU1) con su remate sting_fruit_new (AU3) y deja su
        // sonda AF_STATE fruit_new.
        private void ShowFruitNewNow(int fruit)
        {
            BoardLayoutResult? l = _layout;
            _fruitNewDockBottom = float.NaN;
            _fruitNewCoversPost = false;
            if (l == null || !FruitNewCard.TryTexts(fruit, out string name, out string tag))
            {
                return;
            }

            float height = _fruitNew.Measure(_canvasW, name, tag);
            float band = l.PanelY - BoardTop;
            _fruitNewPlateLayout = null;
            if (band >= Mathf.Max(SightingBandDp, height + SightingBandAirDp))
            {
                _fruitNew.Place(_canvasW, BoardTop + ((band - height) * 0.5f));
            }
            else if (l.PanelBottom + 4f + height <= SightingDockLimit())
            {
                _fruitNew.Place(_canvasW, l.PanelBottom + 4f);
                _fruitNewDockBottom = l.PanelBottom + 4f + height;
                _fruitNewCoversPost = FruitNewOverPost();
            }
            else
            {
                // Ni en la franja ni sobre el muelle (3 filas, como el 111 y el 201, o la escala de texto grande): como placa de
                // una línea clavada al marco, como la del rótulo del lugar (FX4) [P], entre el pie del HUD (FruitNewPlateHudBottom)
                // y lo más alto de la fila 1 en reposo, con FruitNewCard.PlateClearDp de aire a cada lado y centrada en ese hueco
                // (revisión de F5: pegada a la pausa se leía como parte del HUD). Si ni así cabe (o una cuia de la fila 1 ya está
                // elevada bajo ella), no sale, y la sonda lo cuenta (mode "none"): así se ve en el teléfono quién la pierde (p. ej.
                // con la escala de texto grande).
                float plateTop = Mathf.Max(StopLabelPlateTop(l), FruitNewPlateHudBottom() + FruitNewCard.PlateClearDp);
                float rowTop = StopLabelRowOneTop(l);
                if (!_fruitNew.PlaceAsPlate(_canvasW, name, tag, plateTop, rowTop - FruitNewCard.PlateClearDp))
                {
                    EmitFruitNewProbe(fruit, 0, FruitNewNone);
                    return;
                }

                _fruitNewPlateLayout = l;
                _fruitNewPlateShift = _boardShift;
                _fruitNewPlateCanvasW = _canvasW;
                if (FruitNewPlateRowOneLifted())
                {
                    _fruitNewPlateLayout = null;
                    EmitFruitNewProbe(fruit, 0, FruitNewNone);
                    return;
                }
            }

            int tint = FactLine.TintOf(SightingRule.FruitDebutTints[fruit]);
            _fruitNew.Show(tint, tint > 0 ? _art.FruitFlight(tint) : null, tint > 0 ? _art.TintSwatch(tint) : null);
            AF.Audio.AudioCues.PlayFruit(AF.Audio.SfxId.FruitNew, tint); // AU1: tarjeta «Fruta nueva»
            AF.Audio.AudioCues.Stinger(AF.Audio.StingerId.FruitNew); // AU3: su remate, como los otros (tras la toada, con Música, −8 dB)
            EmitFruitNewProbe(fruit, (int)_fruitNew.DurationMs, FruitNewMode);
        }

        /// <summary>
        /// La tarjeta «Fruta nueva» del tinte <paramref name="fruit"/> (índice en <see cref="SightingRule.FruitDebutTints"/>) en
        /// el acto, por su camino de colocación (tests y capturas: la franja o el muelle sin esperar a su nivel). Devuelve si sale.
        /// </summary>
        internal bool ShowFruitNew(int fruit)
        {
            _fruitNew.Hide();
            ShowFruitNewNow(fruit);
            return _fruitNew.Visible;
        }

        // Sitio de la sonda fruit_new cuando la tarjeta no cabe en ninguno y no sale (ms 0).
        private const string FruitNewNone = "none";

        // AF_STATE fruit_new: nivel, tinte, duración y sitio (mode: band, dock, plate o, si no cabe y no sale, none). Solo con la
        // sonda; al enseñarla o al perderla, nunca por fotograma.
        private void EmitFruitNewProbe(int fruit, int ms, string mode)
        {
            if (!DevProbe.Enabled)
            {
                _lastFruitNewProbe = null;
                return;
            }

            StringBuilder sb = _fruitNewProbeSb;
            sb.Clear();
            sb.Append("\"level\":").Append((_flow?.LevelNumber ?? 0).ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "tint", SightingRule.FruitDebutTints[fruit]);
            sb.Append(",\"ms\":").Append(ms.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "mode", mode);
            _lastFruitNewProbe = sb.ToString();
            _probe.Event("fruit_new", _lastFruitNewProbe);
        }

        /// <summary>
        /// Sitio de la tarjeta «Fruta nueva», como lo cuenta la sonda <c>fruit_new</c>: <c>band</c> (sobre la franja del
        /// panorama), <c>dock</c> (sobre el muelle) o <c>plate</c> (placa de una línea clavada al marco), y <c>none</c> si no está
        /// a la vista (tests).
        /// </summary>
        internal string FruitNewMode => !_fruitNew.Visible ? FruitNewNone
            : _fruitNew.OnPlate ? "plate" : float.IsNaN(_fruitNewDockBottom) ? "band" : "dock";

        // Con la tarjeta «Fruta nueva» como placa a la vista: si una cuia de la fila 1 que queda bajo ella se eleva hasta ella, o
        // si cambian el tablero, el lienzo o su desplazamiento, se oculta en el acto y no vuelve (como el rótulo, FX4; sin volver
        // a colocarla). Cada fotograma, desde TickNotices; sin asignaciones.
        private void KeepFruitNewPlateClear()
        {
            if (!_fruitNew.Visible || !_fruitNew.OnPlate)
            {
                return;
            }

            if (!ReferenceEquals(_layout, _fruitNewPlateLayout) || _fruitNewPlateShift != _boardShift || _fruitNewPlateCanvasW != _canvasW
                || FruitNewPlateRowOneLifted())
            {
                _fruitNew.Hide();
                _fruitNewPlateLayout = null;
            }
        }

        // La tarjeta «Fruta nueva» recién colocada sobre el muelle tapa el poste del muelle (su caja con el canto se cruza con la
        // del poste, si se ve): solo al colocarla.
        private bool FruitNewOverPost()
        {
            return CardOverPost(_fruitNew.Root, FruitNewCard.EdgeDp);
        }

        // Una tarjeta recién colocada (card, con su canto de edgeDp por debajo) se cruza con el poste del muelle, si se ve.
        private bool CardOverPost(RectTransform card, float edgeDp)
        {
            RectTransform post = _post.rectTransform;
            if (!post.gameObject.activeInHierarchy)
            {
                return false;
            }

            CanvasRectDp(post, out float pl, out float pt, out float pr, out float pb);
            CanvasRectDp(card, out float cl, out float ct, out float cr, out float cb);
            cb += edgeDp;
            return cl < pr && cr > pl && ct < pb && cb > pt;
        }

        // Revisión de F5: un poste cortado que asoma bajo la tarjeta parecía un error de recorte. Mientras la tarjeta «Fruta
        // nueva» lo tapa sobre el muelle, el poste se funde a la vez que ella (1 − su opacidad: su mismo fundido de 200 ms) y
        // vuelve en cuanto se va, la oculte lo que la oculte. Cada fotograma, desde TickNotices; sin asignaciones y sin tocar el
        // poste si no cambia.
        // Igual con la tarjeta de TiTino sobre el muelle (la instrucción del tutorial, TU1a, y el aviso tras Volver a la orilla).
        private void KeepPostUnderFruitNew()
        {
            float alpha = _fruitNewCoversPost && FruitNewOnDock ? 1f - _fruitNew.Alpha
                : _slothCoversPost && SlothNoticeOnDock ? 1f - _slothNotice!.Alpha : 1f;
            CanvasRenderer post = _post.canvasRenderer;
            if (post.GetAlpha() != alpha)
            {
                post.SetAlpha(alpha);
            }
        }

        // Como StopLabelRowOneLifted, para la placa de «Fruta nueva»: una cuia de la fila 1 que queda bajo ella (a lo ancho, con
        // PlateSideAirDp) está elevada y, con el rebote, llega a menos de PlateLiftAirDp de su borde de abajo. Usa lo que midió
        // StopLabelRowOneTop al colocarla (el rótulo no sale en los niveles de Fruta nueva: StopLabelRule.IsTaken).
        private bool FruitNewPlateRowOneLifted()
        {
            Rect plate = _fruitNew.Rect;
            float limit = plate.yMax + StopLabel.PlateEdgeDp + StopLabel.PlateLiftAirDp;
            int n = Mathf.Min(_views.Length, _stopLabelRestTop.Length);
            for (int i = 0; i < n; i++)
            {
                float rest = _stopLabelRestTop[i];
                if (!float.IsNaN(rest) && _views[i].IsLifted && rest - StopLabel.LiftReachDp < limit
                    && _stopLabelRestRight[i] + StopLabel.PlateSideAirDp > plate.xMin
                    && _stopLabelRestLeft[i] - StopLabel.PlateSideAirDp < plate.xMax)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Solo tests: enciende o apaga el rótulo del lugar (null = <c>ui_stop_label</c>, <see cref="MapConfig.UiStopLabel"/>).
        /// Los tests PlayMode que no tratan de él lo apagan (<c>PlayModeHelpers</c>), para que sus avisos salgan como antes.
        /// </summary>
        internal static bool? StopLabelsOverride { get; set; }

        /// <summary>
        /// Dónde puede ir la placa del rótulo (3 filas), por orden: clavada al marco, de lado y sobre el muelle (tarea FX4).
        /// </summary>
        [Flags]
        internal enum StopLabelSpot
        {
            None = 0,
            Frame = 1,
            Side = 2,
            Dock = 4,
            All = Frame | Side | Dock,
        }

        /// <summary>
        /// Solo tests: los sitios que puede probar la placa (<see cref="StopLabelSpot.All"/> en el juego); así se prueba el
        /// muelle en un tablero en el que cabría de lado (revisión de FX4).
        /// </summary>
        internal static StopLabelSpot StopLabelSpotsOverride { get; set; } = StopLabelSpot.All;

        /// <summary>El rótulo del lugar (tests).</summary>
        internal StopLabel StopLabelView => _stopLabel;

        /// <summary>Campos de la última sonda <c>AF_STATE stop_label</c> (null si no salió ninguno o sin sonda; tests).</summary>
        internal string? LastStopLabelProbe => _lastStopLabelProbe;

        /// <summary>Pie de la fila del HUD (dp desde arriba; tests de colocación del rótulo).</summary>
        internal float HudBottomDp => BoardTop;

        /// <summary>La tropa de la intro sigue en la rama (tests: el rótulo y la tarjeta del miquito la esperan).</summary>
        internal bool IntroTroopVisible => _intro.TroopVisible;

        // Rótulo del lugar (D-013, punto 8; propuesta del Mapa §10.5, punto 5; tarea M2.3): al habilitarse el toque
        // (OnIntroFinished), tras decidir la tarjeta del miquito, entra en la cola si lo dice StopLabelRule: primer intento (ni
        // la intro corta del reintento ni un nivel con fallos: tras salir al inicio y volver, el nivel ya se jugó), una sola vez
        // por visita al nivel y sin tarjeta del miquito pendiente. Al reanudar no pasa por aquí (OnResumed: sin intro). El texto
        // se formatea ya, con los nombres de facts.csv por Loc; si alguno no se puede enseñar (sin aprobar en un build de
        // release, o sin traducir), no sale: nunca «· Brasil» ni un idioma mezclado.
        private void MaybeShowStopLabel(LevelFlow flow)
        {
            if (!flow.HasLevel)
            {
                return;
            }

            int level = flow.LevelNumber;
            bool firstAttempt = !_introRetry && flow.Attempt.FailsLevel == 0 && _stopLabelLevel != level;
            RiverStopCatalog? stops = RiverStops;
            if (!StopLabelRule.ShowsOn(level, stops, firstAttempt, _sightingPending != null, StopLabelsOverride ?? MapConfig.UiStopLabel))
            {
                return;
            }

            RiverStop stop = stops!.StopOf(level)!;
            string place = UiText.StopName(stop);
            string country = UiText.StopCountry(stop);
            if (place.Length == 0 || country.Length == 0)
            {
                return;
            }

            string text = UiText.StopLabel(place, country);
            if (text.Length == 0)
            {
                return;
            }

            _stopLabelLevel = level;
            EnqueueNotice(NoticeKind.StopLabel, text, null, stop);
        }

        // Al cargar otro nivel, el rótulo de esta visita ya no cuenta (volver a entrar en el mismo nivel no lo repite).
        private void StopLabelLevelLoaded(int level)
        {
            if (_stopLabelLevel != level)
            {
                _stopLabelLevel = 0;
            }

            if (_fruitNewLevel != level)
            {
                _fruitNewLevel = 0; // lo mismo para la tarjeta «Fruta nueva» (F5)
            }
        }

        // El rótulo, por ReadMs de su texto (D-011): con 1 o 2 filas, sobre el horizonte; con 3, como placa bajo el HUD.
        private void ShowStopLabelNow(string text, RiverStop stop)
        {
            BoardLayoutResult? l = _layout;
            if (l == null || l.Rows <= 0)
            {
                return;
            }

            int ms = NoticeTiming.ReadMs(text);
            _stopLabelSpot = StopLabelSpot.None;
            if (l.Rows >= 3)
            {
                _stopLabel.BeginPlate(text, ms);
                if (!PlaceStopLabelPlate(l))
                {
                    HideStopLabelFor("no_room"); // ni bajo el HUD (centrada o de lado) ni sobre el muelle: no sale y pasa el siguiente
                    return;
                }
            }
            else
            {
                _stopLabel.ShowOnHorizon(text, ms, _canvasW, _horizonY, BoardTop + StopLabel.HudGapDp, StopLabelHorizonLimit(l));
            }

            RememberStopLabelPlace(l);
            EmitStopLabelProbe(stop, text, ms, l.Rows);
            if (StopLabelRowOneLifted())
            {
                HideStopLabelFor("lift"); // ya había una cuia de la fila 1 elevada al llegarle el turno
            }
        }

        // La placa (3 filas), en el primer sitio donde cabe: clavada al marco y centrada, por encima de lo más alto de la fila 1
        // en reposo; a esa altura, de lado; o sobre el muelle. Devuelve false si no cabe en ninguno.
        private bool PlaceStopLabelPlate(BoardLayoutResult l)
        {
            StopLabelSpot spots = StopLabelSpotsOverride;
            float top = StopLabelPlateTop(l);
            float rowTop = StopLabelRowOneTop(l); // mide la fila 1 (también para la elevación y para ir de lado)
            _stopLabelSideLeft = float.NaN;
            _stopLabelSideRight = float.NaN;
            _stopLabelSpot = StopLabelSpot.Frame;
            if ((spots & StopLabelSpot.Frame) != 0
                && _stopLabel.PlaceAsPlate(StopLabel.SideDp, _canvasW - StopLabel.SideDp, top, rowTop - StopLabel.PlateCuiaGapDp, true))
            {
                return true;
            }

            _stopLabelSpot = StopLabelSpot.Side;
            if ((spots & StopLabelSpot.Side) != 0 && PlaceStopLabelSide(top))
            {
                return true;
            }

            _stopLabelSpot = StopLabelSpot.Dock;
            return (spots & StopLabelSpot.Dock) != 0 && PlaceStopLabelDock(l);
        }

        // De lado (revisión de FX4): a la altura de la placa clavada al marco, centrada en el tramo libre más ancho que dejan las
        // cuias de la fila 1 que no le dejan sitio encima ni a 12 sp, con PlateSideAirDp a cada lado de lo que ocupan, y por
        // encima de las demás con su aire. Solo en una línea. Sin asignaciones (huecos preasignados), aunque no va por fotograma.
        private bool PlaceStopLabelSide(float top)
        {
            float minBottom = top + _stopLabel.MinPlateHeight;
            float maxBottom = float.MaxValue;
            int blocks = 0;
            for (int i = 0; i < _stopLabelRestTop.Length; i++)
            {
                float rest = _stopLabelRestTop[i];
                if (float.IsNaN(rest))
                {
                    continue;
                }

                if (rest - StopLabel.PlateCuiaGapDp < minBottom)
                {
                    // Inserción ordenada por el borde izquierdo (como mucho una fila de cuias).
                    float left = _stopLabelRestLeft[i] - StopLabel.PlateSideAirDp;
                    float right = _stopLabelRestRight[i] + StopLabel.PlateSideAirDp;
                    int k = blocks++;
                    while (k > 0 && _stopLabelBlockLeft[k - 1] > left)
                    {
                        _stopLabelBlockLeft[k] = _stopLabelBlockLeft[k - 1];
                        _stopLabelBlockRight[k] = _stopLabelBlockRight[k - 1];
                        k--;
                    }

                    _stopLabelBlockLeft[k] = left;
                    _stopLabelBlockRight[k] = right;
                }
                else
                {
                    maxBottom = Mathf.Min(maxBottom, rest - StopLabel.PlateCuiaGapDp);
                }
            }

            if (blocks == 0)
            {
                return false; // nada de la fila 1 se lo impide encima: si no cupo centrada, de lado tampoco
            }

            if (maxBottom == float.MaxValue)
            {
                maxBottom = top + (2f * StopLabel.PlateTextSp * StopLabel.PlateHeightEm); // ninguna otra cuia debajo: sin límite útil
            }

            // El tramo libre más ancho entre los márgenes del lienzo y los tramos que no deja la fila 1.
            float bestLeft = 0f;
            float bestRight = 0f;
            float cursor = StopLabel.SideDp;
            float end = _canvasW - StopLabel.SideDp;
            for (int k = 0; k <= blocks; k++)
            {
                float spanRight = k < blocks ? Mathf.Min(_stopLabelBlockLeft[k], end) : end;
                if (spanRight - cursor > bestRight - bestLeft)
                {
                    bestLeft = cursor;
                    bestRight = spanRight;
                }

                if (k < blocks)
                {
                    cursor = Mathf.Max(cursor, _stopLabelBlockRight[k]);
                }
            }

            if (bestRight <= bestLeft || !_stopLabel.PlaceAsPlate(bestLeft, bestRight, top, maxBottom, false))
            {
                return false;
            }

            _stopLabelSideLeft = bestLeft;
            _stopLabelSideRight = bestRight;
            return true;
        }

        // Sobre el muelle: del canto de abajo del panel (con NoticeBubbleGapDp) a StopLabelDockLimit, centrada en ese hueco si le
        // sobra alto (revisión de FX4: pegada al riel se leía como parte de él).
        private bool PlaceStopLabelDock(BoardLayoutResult l)
        {
            float top = StopLabelDockTop(l);
            float limit = StopLabelDockLimit();
            if (!_stopLabel.PlaceAsPlate(StopLabel.SideDp, _canvasW - StopLabel.SideDp, top, limit, true))
            {
                return false;
            }

            float slack = limit - _stopLabel.Rect.yMax;
            if (slack >= 1f)
            {
                _stopLabel.MovePlateTo(top + Mathf.Floor(slack * 0.5f));
            }

            return true;
        }

        // Lo más alto de la placa (3 filas): clavada al marco, PlateOverFrameDp sobre el canto interior de su barra de arriba (tal
        // como se dibuja, con el desplazamiento del tablero), sin subir del pie de la fila del HUD.
        private float StopLabelPlateTop(BoardLayoutResult l)
        {
            return Mathf.Max(BoardTop, l.PanelY - _boardShift + PanelFrameDp - StopLabel.PlateOverFrameDp);
        }

        // La placa sobre el muelle: NoticeBubbleGapDp bajo el panel, como la tarjeta del miquito.
        private float StopLabelDockTop(BoardLayoutResult l)
        {
            return l.PanelBottom - _boardShift + NoticeBubbleGapDp;
        }

        // Lo más bajo de la placa sobre el muelle: 8 dp sobre la bandeja y, con una burbuja «Te faltan {n}» a la vista, su aire
        // por encima de la más alta (como SightingDockLimit).
        private float StopLabelDockLimit()
        {
            float limit = TrayTop - NoticeAboveTrayDp;
            if (_tray.TryGetMissingTop(out float bubbleTop))
            {
                limit = Mathf.Min(limit, bubbleTop - NoticeBubbleGapDp);
            }

            return limit;
        }

        /// <summary>
        /// Lo más alto de la fila 1 en reposo (dp desde arriba, con el desplazamiento del tablero; tarea FX4): de cada cuia de la
        /// fila 1, su boca (lo alto de la cuia, con el corcho) o, si algo visible asoma por encima (la tapa del sellado o una
        /// pieza de su mecánica, como el medallón de la Hoja), lo más alto de ello. Mide los gráficos visibles del cuerpo y de
        /// las anclas de overlay de la ranura, relativos a su boca (así una cuia ya elevada cuenta en reposo; una que vierte, con
        /// el corcho o la tapa de reposo de su cuerpo y, a lo ancho, con toda su ranura), y deja cada cuia
        /// en <c>_stopLabelRestTop</c>, con lo que ocupa a lo ancho lo que se ve de la cuia en <c>_stopLabelRestLeft</c> y
        /// <c>_stopLabelRestRight</c>. Solo al colocar la placa (nunca por fotograma).
        /// </summary>
        internal float StopLabelRowOneTop(BoardLayoutResult l)
        {
            float top = float.MaxValue;
            int n = Mathf.Min(_views.Length, Mathf.Min(l.RowOf.Length, _stopLabelRestTop.Length));
            for (int i = 0; i < _stopLabelRestTop.Length; i++)
            {
                _stopLabelRestTop[i] = float.NaN;
            }

            for (int i = 0; i < n; i++)
            {
                if (l.RowOf[i] != 0)
                {
                    continue;
                }

                CuiaView v = _views[i];
                float mouth = l.FootY[i] - (v.HeightU * v.Unit) - _boardShift;
                float crown = 0f;
                if (v.BodyInFront)
                {
                    // Vertiendo (el cuerpo, girado, delante de todo): su corcho o su tapa como en reposo, medidos en su marco.
                    crown = BodyCrownAtRest(v.Body);
                }
                else if (VisibleBox(v.Body, out _, out float bodyGraphicsTop, out _))
                {
                    CanvasRectDp(v.Body, out _, out float bodyTop, out _, out _);
                    crown = Mathf.Max(crown, bodyTop - bodyGraphicsTop);
                }

                CuiaOverlayHost host = v.OverlayHost;
                CanvasRectDp(v.Root, out float left, out _, out float right, out float rootBottom);
                float slotMouth = rootBottom - (v.HeightU * v.Unit);
                if (VisibleBox(host.SlotBack, out _, out float backTop, out _))
                {
                    crown = Mathf.Max(crown, slotMouth - backTop);
                }

                if (VisibleBox(host.SlotFront, out _, out float frontTop, out _))
                {
                    crown = Mathf.Max(crown, slotMouth - frontTop);
                }

                // A lo ancho, lo que se ve de la cuia (la ranura, Root, es más ancha que ella); si nada, la ranura entera. Con el
                // cuerpo vertiendo fuera de la ranura, la ranura entera y lo que se ve en ella (de más, nunca de menos).
                if (VisibleBox(v.Root, out float seenLeft, out _, out float seenRight))
                {
                    left = v.BodyInFront ? Mathf.Min(left, seenLeft) : seenLeft;
                    right = v.BodyInFront ? Mathf.Max(right, seenRight) : seenRight;
                }

                _stopLabelRestTop[i] = mouth - crown;
                _stopLabelRestLeft[i] = left;
                _stopLabelRestRight[i] = right;
                top = Mathf.Min(top, _stopLabelRestTop[i]);
            }

            return top == float.MaxValue ? l.BlockTop + BoardLayout.Lift - _boardShift : top;
        }

        /// <summary>
        /// Pie del HUD de arriba para la placa de «Fruta nueva» (dp desde arriba): el de su fila (<see cref="HudBottomDp"/>) o, si
        /// bajan más, el canto de madera de los discos de la pausa y de Reiniciar (su cara de
        /// <see cref="ReliefSprites.FaceHeightDp"/> más <see cref="ProceduralArt.ButtonEdgeDp"/>, desde lo alto del disco: la pausa
        /// baja 4 dp y su canto pasa 2 dp del pie de la fila), la etiqueta de Reiniciar (en dos líneas con la escala de texto) y las
        /// píldoras del saldo. Son los bordes que se ven, no las cajas de los sprites, que llevan aire transparente y la sombra
        /// difusa (a alfa 0,3) alrededor. Las etiquetas del HUD no cuentan: se apartan mientras se ve un aviso. Solo al colocar la
        /// placa (nunca por fotograma).
        /// </summary>
        internal float FruitNewPlateHudBottom()
        {
            float bottom = BoardTop;
            float disc = _art.DiscWood.FaceHeightDp + ProceduralArt.ButtonEdgeDp;
            if (_pauseButton.Button.gameObject.activeInHierarchy)
            {
                CanvasRectDp((RectTransform)_pauseButton.Face.parent, out _, out float pauseTop, out _, out _);
                bottom = Mathf.Max(bottom, pauseTop + disc);
            }

            if (_restart.Button.gameObject.activeInHierarchy)
            {
                CanvasRectDp((RectTransform)_restart.Face.parent, out _, out float restartTop, out _, out _);
                bottom = Mathf.Max(bottom, restartTop + disc);
            }

            if (_restartPill.gameObject.activeInHierarchy)
            {
                CanvasRectDp(_restartPill.rectTransform, out _, out _, out _, out float pill);
                bottom = Mathf.Max(bottom, pill);
            }

            if (_walletBar.Visible && _walletBar.Compact && _walletBar.Root.gameObject.activeInHierarchy)
            {
                if (_walletBar.SeedsRect.gameObject.activeInHierarchy)
                {
                    CanvasRectDp(_walletBar.SeedsRect, out _, out _, out _, out float seeds);
                    bottom = Mathf.Max(bottom, seeds);
                }

                if (_walletBar.CanoesRect.gameObject.activeInHierarchy)
                {
                    CanvasRectDp(_walletBar.CanoesRect, out _, out _, out _, out float canoes);
                    bottom = Mathf.Max(bottom, canoes);
                }
            }

            return bottom;
        }

        // La caja de lo visible bajo root (los gráficos activos, con alfa y que se pintan; no el área táctil), en dp del lienzo
        // (x desde la izquierda, y desde arriba). False si no se ve nada.
        private bool VisibleBox(RectTransform root, out float left, out float top, out float right)
        {
            left = float.MaxValue;
            top = float.MaxValue;
            right = float.MinValue;
            List<Graphic> graphics = _stopLabelGraphics;
            root.GetComponentsInChildren(false, graphics);
            for (int k = 0; k < graphics.Count; k++)
            {
                Graphic g = graphics[k];
                if (!g.enabled || g is HitArea || g.color.a <= 0.01f || g.canvasRenderer.GetAlpha() <= 0.01f)
                {
                    continue;
                }

                CanvasRectDp(g.rectTransform, out float gLeft, out float gTop, out float gRight, out _);
                left = Mathf.Min(left, gLeft);
                top = Mathf.Min(top, gTop);
                right = Mathf.Max(right, gRight);
            }

            graphics.Clear();
            return left <= right;
        }

        // Lo que asoma lo visible del cuerpo por encima de su canto de arriba (dp; 0 si nada), medido en el marco del propio
        // cuerpo: sale lo mismo en reposo, elevado o girado vertiendo (revisión de FX4: a mitad de un vertido, la corona de
        // reposo y no 0). Solo al colocar la placa.
        private float BodyCrownAtRest(RectTransform body)
        {
            float top = float.MinValue;
            List<Graphic> graphics = _stopLabelGraphics;
            body.GetComponentsInChildren(false, graphics);
            for (int k = 0; k < graphics.Count; k++)
            {
                Graphic g = graphics[k];
                if (!g.enabled || g is HitArea || g.color.a <= 0.01f || g.canvasRenderer.GetAlpha() <= 0.01f)
                {
                    continue;
                }

                g.rectTransform.GetWorldCorners(_stopLabelCorners);
                for (int c = 0; c < 4; c++)
                {
                    top = Mathf.Max(top, body.InverseTransformPoint(_stopLabelCorners[c]).y);
                }
            }

            graphics.Clear();
            if (top == float.MinValue)
            {
                return 0f;
            }

            // Del marco del cuerpo a dp del lienzo (el cuerpo solo gira: la escala es la misma en reposo y vertiendo).
            float dpPerUnit = ((Vector2)_root.InverseTransformVector(body.TransformVector(Vector3.up))).magnitude;
            return Mathf.Max(0f, (top - body.rect.yMax) * dpPerUnit);
        }

        // La caja de rt en dp del lienzo (x desde la izquierda, y desde arriba; con giros, la que abarca sus cuatro esquinas).
        private void CanvasRectDp(RectTransform rt, out float left, out float top, out float right, out float bottom)
        {
            rt.GetWorldCorners(_stopLabelCorners);
            left = float.MaxValue;
            top = float.MaxValue;
            right = float.MinValue;
            bottom = float.MinValue;
            Rect canvas = _root.rect;
            for (int c = 0; c < 4; c++)
            {
                Vector3 p = _root.InverseTransformPoint(_stopLabelCorners[c]);
                float x = p.x - canvas.xMin;
                float y = canvas.yMax - p.y;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
                top = Mathf.Min(top, y);
                bottom = Mathf.Max(bottom, y);
            }
        }

        /// <summary>
        /// Una cuia de la fila 1 que queda bajo la placa a la vista (a lo ancho, con <see cref="StopLabel.PlateSideAirDp"/>) está
        /// elevada y, con el rebote de la elevación (<see cref="StopLabel.LiftReachDp"/>), llega a menos de
        /// <see cref="StopLabel.PlateLiftAirDp"/> de ella (clavada al marco, centrada o de lado; sobre el muelle, nunca). Cada
        /// fotograma con la placa a la vista: sin asignaciones.
        /// </summary>
        internal bool StopLabelRowOneLifted()
        {
            if (!_stopLabel.Visible || !_stopLabel.OnPlate || _stopLabelSpot == StopLabelSpot.Dock)
            {
                return false;
            }

            Rect plate = _stopLabel.Rect;
            float limit = plate.yMax + StopLabel.PlateLiftAirDp;
            int n = Mathf.Min(_views.Length, _stopLabelRestTop.Length);
            for (int i = 0; i < n; i++)
            {
                float rest = _stopLabelRestTop[i];
                if (!float.IsNaN(rest) && _views[i].IsLifted && rest - StopLabel.LiftReachDp < limit
                    && _stopLabelRestRight[i] + StopLabel.PlateSideAirDp > plate.xMin
                    && _stopLabelRestLeft[i] - StopLabel.PlateSideAirDp < plate.xMax)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Borde de arriba de la bandeja de comodines (dp desde arriba; tests de la placa sobre el muelle).</summary>
        internal float TrayTopDp => TrayTop;

        /// <summary>La placa del rótulo va sobre el muelle (tests).</summary>
        internal bool StopLabelOnDock => _stopLabel.Visible && _stopLabel.OnPlate && _stopLabelSpot == StopLabelSpot.Dock;

        /// <summary>
        /// Sitio del rótulo, como lo cuenta la sonda <c>stop_label</c>: <c>horizon</c>, <c>plate</c> (clavada al marco y
        /// centrada), <c>plate_side</c> (clavada al marco, de lado) o <c>dock</c> (sobre el muelle; tests).
        /// </summary>
        internal string StopLabelMode => !_stopLabel.OnPlate ? "horizon"
            : _stopLabelSpot == StopLabelSpot.Side ? "plate_side" : _stopLabelSpot == StopLabelSpot.Dock ? "dock" : "plate";

        /// <summary>Lo que ocupa a lo ancho cada cuia de la fila 1 al colocar la placa (x: izquierda, y: derecha, dp; tests).</summary>
        internal Vector2 StopLabelRestSpanOf(int index)
        {
            return index >= 0 && index < _stopLabelRestLeft.Length
                ? new Vector2(_stopLabelRestLeft[index], _stopLabelRestRight[index])
                : new Vector2(float.NaN, float.NaN);
        }

        /// <summary>
        /// Tramo libre de la fila 1 en el que se centró la placa de lado (x: izquierda, y: derecha, dp; NaN si no va de lado;
        /// tests y capturas).
        /// </summary>
        internal Vector2 StopLabelSideSpan => new Vector2(_stopLabelSideLeft, _stopLabelSideRight);

        /// <summary>Lo más alto de cada cuia de la fila 1 en reposo al colocar la placa (NaN fuera de la fila 1; tests).</summary>
        internal float StopLabelRestTopOf(int index)
        {
            return index >= 0 && index < _stopLabelRestTop.Length ? _stopLabelRestTop[index] : float.NaN;
        }

        // Oculta el rótulo en el acto y lo descarta (no vuelve), con su sonda stop_label_off.
        private void HideStopLabelFor(string reason)
        {
            _stopLabel.Hide();
            if (!DevProbe.Enabled)
            {
                return;
            }

            StringBuilder sb = _stopLabelProbeSb;
            sb.Clear();
            sb.Append("\"level\":").Append((_flow?.LevelNumber ?? 0).ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "reason", reason);
            _probe.Event("stop_label_off", sb.ToString());
        }

        // Lo más bajo del rótulo sobre el horizonte (1-2 filas): el canto de arriba del panel, con su aire.
        private float StopLabelHorizonLimit(BoardLayoutResult l)
        {
            return l.PanelY - _boardShift - StopLabel.CuiaGapDp;
        }

        private void RememberStopLabelPlace(BoardLayoutResult l)
        {
            _stopLabelLayout = l;
            _stopLabelCanvasW = _canvasW;
            _stopLabelHorizon = _horizonY;
            _stopLabelShift = _boardShift;
        }

        // Con el rótulo a la vista, si cambian el tablero, el lienzo, el horizonte o el desplazamiento del tablero, se vuelve a
        // colocar (mide su texto solo entonces). Cada fotograma, desde TickNotices; sin asignaciones.
        private void KeepStopLabelPlaced()
        {
            BoardLayoutResult? l = _layout;
            if (!_stopLabel.Visible || l == null || l.Rows <= 0)
            {
                return;
            }

            // FX4: una cuia de la fila 1 que se eleva hasta la placa la oculta; sobre el muelle, una burbuja que sale debajo.
            if (StopLabelRowOneLifted())
            {
                HideStopLabelFor("lift");
                return;
            }

            if (_stopLabelSpot == StopLabelSpot.Dock && _stopLabel.OnPlate && _stopLabel.Rect.yMax > StopLabelDockLimit() + 0.01f)
            {
                HideStopLabelFor("bubble");
                return;
            }

            if (ReferenceEquals(l, _stopLabelLayout) && _stopLabelCanvasW == _canvasW && _stopLabelHorizon == _horizonY
                && _stopLabelShift == _boardShift)
            {
                return;
            }

            if (l.Rows >= 3 && _stopLabel.OnPlate)
            {
                if (!PlaceStopLabelPlate(l))
                {
                    HideStopLabelFor("no_room"); // con el tablero nuevo no cabe en ningún sitio
                    return;
                }
            }
            else if (l.Rows < 3 && !_stopLabel.OnPlate)
            {
                _stopLabel.PlaceOnHorizon(_canvasW, _horizonY, BoardTop + StopLabel.HudGapDp, StopLabelHorizonLimit(l));
            }
            else
            {
                HideStopLabelFor("board"); // otro número de filas con el rótulo a la vista (otro tablero): no se cambia de forma a medias
                return;
            }

            RememberStopLabelPlace(l);
        }

        // AF_STATE stop_label (plan del Mapa §7: level, stop, text y ms; además el id del sitio, el id de parada s{nn}, la forma
        // y las filas del tablero). Solo con la sonda.
        private void EmitStopLabelProbe(RiverStop stop, string text, int ms, int rows)
        {
            if (!DevProbe.Enabled)
            {
                _lastStopLabelProbe = null;
                return;
            }

            StringBuilder sb = _stopLabelProbeSb;
            sb.Clear();
            sb.Append("\"level\":").Append((_flow?.LevelNumber ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stop\":").Append(stop.N.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "stop_id", DevProbe.StopId(stop));
            AppendJsonString(sb, "id", stop.Id);
            AppendJsonString(sb, "text", text);
            sb.Append(",\"ms\":").Append(ms.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "mode", StopLabelMode);
            sb.Append(",\"rows\":").Append(rows.ToString(CultureInfo.InvariantCulture));
            _lastStopLabelProbe = sb.ToString();
            _probe.Event("stop_label", _lastStopLabelProbe);
        }

        /// <summary>
        /// Los avisos de la cola de los avisos de juego, por tipos (arquitectura de la tanda 4, §3). F5 añade la Fruta nueva;
        /// TI6b (TiTino) y TU1a (tutorial) añaden los suyos; la prioridad entre tarjetas la deciden las reglas al encolar (tutorial
        /// &gt; Fruta nueva &gt; miquito &gt; rótulo, como mucho una por nivel), no la cola, que va por orden de llegada; salvo la
        /// instrucción del tutorial, que entra al principio (TU1a).
        /// </summary>
        internal enum NoticeKind
        {
            /// <summary>Un aviso de texto en la placa de abajo (<c>_toast</c>): regalo de Capi, «¡Una cuia más!», «+1 Luciérnaga».</summary>
            Text = 0,

            /// <summary>La tarjeta «¡Miquito a la vista!» de una especie (<c>_sighting</c>).</summary>
            Species = 1,

            /// <summary>El rótulo del lugar, «{lugar} · {país}» (<see cref="StopLabel"/>; tarea M2.3).</summary>
            StopLabel = 2,

            /// <summary>La tarjeta «Fruta nueva» del estreno de un tinte (<see cref="FruitNewCard"/>; tarea F5).</summary>
            FruitNew = 3,

            /// <summary>El aviso de TiTino tras Volver a la orilla en 1-30 (<see cref="SlothNoticeCard"/>; tarea TI6b).</summary>
            Sloth = 4,

            /// <summary>
            /// La instrucción del tutorial con el retrato de TiTino en 1, 2, 3 y 6 (la misma <see cref="SlothNoticeCard"/>, con
            /// <c>sloth_present</c>; tarea TU1a): entra la primera de la cola (tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo).
            /// </summary>
            Tutorial = 5,
        }

        private struct Notice
        {
            public NoticeKind Kind;

            // Texto del aviso (Text), de la instrucción del tutorial (Tutorial) o del rótulo ya formateado (StopLabel; se formatea
            // al encolar, nunca por fotograma).
            public string? Text;

            // Especie de la tarjeta del miquito (Species).
            public FaunaSpecies? Species;

            // Parada del rótulo (StopLabel).
            public RiverStop? Stop;

            // Tinte de la tarjeta «Fruta nueva» (FruitNew): índice en SightingRule.FruitDebutTints; −1 en los demás.
            public int Fruit;

            // Lo que decidió SlothLines para el aviso de TiTino (Sloth; Text lleva su frase ya formateada).
            public SlothLine Sloth;
        }
    }
}
