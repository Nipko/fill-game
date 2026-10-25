#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial del banner de entrada a un mundo (07 §2.2, regla 1; 02 §6; glosario §10, <c>world_banner</c>; decisión del dueño
    /// del 2026-10-11) sin editar GameScreen.cs: implementa <see cref="IWorldBannerListener"/> y pinta el banner con
    /// <see cref="WorldBannerView"/>. Lo llaman los ganchos <c>Cards*</c> de GameScreen.Cards.cs (los métodos parciales solo
    /// admiten una implementación).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo</b>: el banner es LEVEL_CARD con <see cref="LevelFlow.WorldBanner"/> (LevelFlow.Cards.cs): llega tras
    /// <c>OnLevelLoaded</c> y los regalos, la intro se retiene en t = 0 detrás de él como detrás de la tarjeta previa, y 1 toque
    /// llama a <see cref="LevelFlow.DismissWorldBanner"/>, que empieza su salida: el flujo sigue en LEVEL_CARD
    /// (<see cref="LevelFlow.WorldBannerClosing"/>) los <see cref="WorldBannerView.ExitDurationMs"/> que esta parcial le fija al
    /// abrirlo (<see cref="LevelFlow.WorldBannerExitMs"/>), y solo después abre la tarjeta previa si el nivel la tiene o arranca
    /// la intro: «Llenar y teñir» nunca empieza debajo del banner (07 §5.1). Un segundo toque durante la salida la termina en el
    /// acto. Al ser LEVEL_CARD, los avisos esperan a que termine (la Luciérnaga regalada, <c>ShowRevealGiftIfReady</c>), la
    /// pausa queda inerte bajo el velo y nada de debajo recibe el toque.</para>
    /// <para><b>Arte</b>: se pinta fuera del hilo principal (<see cref="WorldBannerView.AsyncArt"/>; 08 §5.4). Mientras llega,
    /// el velo sale en el acto y los toques se ignoran (<see cref="LevelFlow.WorldBannerWaiting"/>, también los del flujo:
    /// <c>TapAnywhere</c>, <c>TapCuia</c> y el fondo); la tarjeta entra cuando llega o, si el trabajo falla, no se puede encolar
    /// o pasan <see cref="WorldBannerView.ArtTimeoutMs"/> de reloj real, sin él (el respaldo de D-012: la tarjeta, la placa y el
    /// texto, con la viñeta del color plano del agua y sin Rosa ni Capi). Así el banner nunca se pierde sin verse. Para que casi
    /// siempre esté ya, se pide antes (<see cref="PrefetchWorldBanner"/>): en la victoria del nivel anterior (el 10, el 40, el 80
    /// y el 140) y al ofrecer «Jugar» con un nivel que abre mundo (GameBootstrap). El mismo trabajo pinta la superficie de madera
    /// de la tarjeta (T52, observación 4 de la prueba del 2026-10-13: se pintaba en el hilo principal al abrir el banner, de 66
    /// a 111 ms en el teléfono, y era eso lo que medía <c>wait_ms</c> al entrar con «Siguiente» aunque el arte ya estuviera).
    /// Los tests que miden instantes exactos lo piden en el acto con <see cref="LaunchOptions.BannerArtSyncOverride"/>: el mismo
    /// trabajo, corrido en el hilo principal, y así lo registra el origen de la superficie
    /// (<c>ProceduralArt.ModalSurfaceOriginOf</c>: <c>MainThread</c>; revisión de T52, U1). Los del respaldo lo fuerzan con
    /// <see cref="LaunchOptions.BannerArtFaultOverride"/> y <see cref="LaunchOptions.BannerArtTimeoutMsOverride"/>.</para>
    /// <para>En desarrollo (los documenta docs/dev/prototipo-v0.md): al abrirse, <c>AF_STATE {"event":"world_banner",…}</c>
    /// con <c>level</c>, <c>world</c>, <c>text</c> (la clave entera), <c>then</c> (<c>"level_card"</c> si al cerrarlo sale la
    /// tarjeta previa, o <c>"intro"</c>) y <c>reduce_motion</c> (la versión quieta); cuando la tarjeta empieza a entrar, con su
    /// arte o con el respaldo, <c>AF_STATE {"event":"world_banner_art",…}</c> con <c>level</c>, <c>world</c>, <c>async</c>,
    /// <c>wait_ms</c> (lo que esperó, en reloj real desde la petición), <c>mascots_ms</c> (lo que costó pintarlo),
    /// <c>main_ms</c> (lo que ocupó el hilo principal) y <c>fallback</c> (<c>"none"</c> con su arte; si no,
    /// <c>"error"</c>, <c>"queue"</c> o <c>"timeout"</c>); al tocarlo, <c>AF_STATE {"event":"world_banner_closed",…}</c> con
    /// <c>level</c>, <c>world</c> y <c>next</c>, con los mismos valores que <c>then</c> y antes de que se abra lo que sigue (la
    /// línea <c>level_card</c>, si la hay, sale después); y, al terminar la entrada, <c>AF_LAYOUT {"screen":"world_banner",…}</c>
    /// con el id <c>world_banner</c> y el rectángulo de la tarjeta (cualquier toque la cierra). En el <c>AF_LAYOUT</c> del nivel,
    /// <c>world_banner</c> solo sale con la tarjeta a la vista (no mientras espera el arte).</para>
    /// <para><b>Viaje de mundo</b> (D-013, punto 7; Q4-D; 02 §6, fila Banner de mundo; tarea M3.6): solo si
    /// <see cref="MapMoments.TravelInBanner"/> (el 41, el 81 y el 141 en 1-200, y con Q14-A el 201, el 301, el 421 y el 561;
    /// nunca el 11, que lo cuenta el Mapa del 10, ni con <c>ui_map_autopen</c> = <c>l10</c>) y si hay datos del Mapa con el
    /// tramo de la gran parada (<see cref="BannerTravel.For"/>; sin ellos, el banner de siempre). Bajo la cabecera, la pregunta
    /// del mundo y la franja del Mapa (<see cref="BannerTravelStrip"/>): la canoa recorre el tramo en ≤ 4 s, los nombres de
    /// paso salen sin pararse, la bandera se iza en 800 ms si cambia de país (la oficial si existe; si no, la simplificada por
    /// código, con <c>MapArtRule</c>, también en release: D-052) y el guía se releva en Teotônio si cambia (P-M8). Bajo el lugar, «Ver mapa»
    /// (<c>map_open</c>): <see cref="IGameActions.OpenRiverMap"/> con <see cref="MapOpenSource.WorldChange"/>, encima del banner,
    /// que sigue ahí al cerrarlo. El primer toque completa el viaje y el siguiente cierra el banner, como siempre; con Reducir
    /// movimiento, el estado final con un fundido de 150 ms. La geometría del Mapa se lee la primera vez que hace falta
    /// (<see cref="RiverGeometry"/>, de GameBootstrap), al pedir el arte antes si se puede. La sonda <c>world_banner</c> suma
    /// <c>travel</c>, <c>travel_ms</c> (lo que dura el viaje desde que entra la tarjeta) y <c>flag</c> (la bandera que se iza, o
    /// null), y el <c>AF_LAYOUT</c> del banner, el id <c>world_banner_map</c> con «Ver mapa».</para>
    /// </remarks>
    public sealed partial class GameScreen : IWorldBannerListener
    {
        /// <summary>Lo que sigue al banner (<c>then</c> y <c>next</c> de la sonda): la tarjeta previa.</summary>
        internal const string BannerNextLevelCard = "level_card";

        /// <summary>Lo que sigue al banner (<c>then</c> y <c>next</c> de la sonda): la intro.</summary>
        internal const string BannerNextIntro = "intro";

        private WorldBannerView _worldBanner = null!;
        private string? _lastBannerProbe;
        private string? _lastBannerClosedProbe;
        private string? _lastBannerArtProbe;
        private int _worldBannerShown;
        private int _bannerPrefetchLevel;
        private int _bannerMapOpened;
        private BannerTravel? _travelCache;
        private int _travelCacheLevel;
        private BannerTravelData? _travelCacheData;
        private RiverStopCatalog? _travelCacheStops;
        private RiverGeometrySource? _travelCacheGeometry;
        private LocCatalog? _travelCacheCatalog;
        private string? _travelCacheLocale;
        private bool _travelCacheUnapproved;
        private int _travelBuilds;

        /// <summary>Veces que se resolvió un viaje de mundo contra los datos del Mapa (tests: el arte pedido antes no lo repite).</summary>
        internal int BannerTravelBuilds => _travelBuilds;

        /// <summary>
        /// Lee (la primera vez) la geometría del Mapa del río y las rutas de las paradas; false si no hay o no valida. La pone
        /// GameBootstrap (sus datos, leídos al abrir el Mapa o al pedir el primer viaje de mundo). Firma de <see cref="RiverGeometry"/>.
        /// </summary>
        internal delegate bool RiverGeometrySource(out RiverMap? map, out RiverRoutes? routes);

        /// <summary>
        /// Viaje de mundo dentro del banner (M3.6): false = nunca, el banner de siempre (tests que no tratan del viaje,
        /// <c>PlayModeHelpers.NoBannerTravelUnlessSet</c>); null o true = la regla del juego (<see cref="MapMoments.TravelInBanner"/>
        /// con los datos del Mapa). Los tests que lo prueban lo fijan y lo vuelven a null en su TearDown.
        /// </summary>
        internal static bool? BannerTravelOverride { get; set; }

        /// <summary><c>ui_map_autopen</c> para el viaje del banner (tests); null = <see cref="MapConfig.UiMapAutopen"/>.</summary>
        internal static string? BannerAutopenOverride { get; set; }

        /// <summary>
        /// Datos del Mapa para el viaje del banner (tests: un catálogo de prueba, como el del relevo en Teotônio); null = los del
        /// juego (<see cref="RiverStops"/> y <see cref="RiverGeometry"/>).
        /// </summary>
        internal static BannerTravelData? BannerTravelDataOverride { get; set; }

        /// <summary>Geometría del Mapa para el viaje de mundo del banner (la pone GameBootstrap; null = sin viaje).</summary>
        internal RiverGeometrySource? RiverGeometry { get; set; }

        /// <summary>Veces que «Ver mapa» del banner abrió el Mapa (tests).</summary>
        internal int BannerMapOpened => _bannerMapOpened;

        /// <summary>Banner de entrada a un mundo (tests y capturas).</summary>
        internal WorldBannerView WorldBannerView => _worldBanner;

        /// <summary>Campos de la última línea <c>AF_STATE world_banner</c> (tests; null si aún no hubo banner o sin sonda).</summary>
        internal string? LastWorldBannerProbe => _lastBannerProbe;

        /// <summary>
        /// Campos de la última línea <c>AF_STATE world_banner_closed</c> (tests; null si aún no se cerró ninguno o sin sonda).
        /// </summary>
        internal string? LastWorldBannerClosedProbe => _lastBannerClosedProbe;

        /// <summary>
        /// Campos de la última línea <c>AF_STATE world_banner_art</c> (tests; null si aún no entró la tarjeta de ninguno o sin
        /// sonda).
        /// </summary>
        internal string? LastWorldBannerArtProbe => _lastBannerArtProbe;

        /// <summary>Banners abiertos desde que existe la pantalla (tests: no sale al reintentar ni al reanudar).</summary>
        internal int WorldBannersShown => _worldBannerShown;

        /// <summary>
        /// Escala de la fuente del sistema para el banner, de 1 a 1,3 (02 §12; pregunta 43; T52), con el mismo mecanismo que la
        /// de la tarjeta previa (<see cref="CardFontScale"/>; tests y capturas: la escala de Android aún no llega al juego [P]).
        /// Con el banner abierto, lo vuelve a colocar en el acto.
        /// </summary>
        internal float BannerFontScale
        {
            get => _worldBanner.FontScale;
            set
            {
                _worldBanner.FontScale = value;
                if (_worldBanner.Visible)
                {
                    _worldBanner.Relayout();
                }
            }
        }

        /// <inheritdoc />
        public void OnWorldBanner(int world, int level)
        {
            LevelFlow flow = RequireFlow();
            HoldIntroForCard(flow); // el tablero espera detrás en el fotograma 0 de la intro, como con la tarjeta previa
            BannerTravel? travel = TravelFor(level); // el viaje de mundo (M3.6), o null: el banner de siempre

            // La tarjeta previa o la intro, solo cuando el banner haya salido del todo (07 §5.1: antes de la intro, no encima).
            flow.WorldBannerExitMs = Mathf.CeilToInt(WorldBannerView.ExitDurationMs(flow.ReduceMotion));
            Color water = Palette.WaterBase(flow.Level.WaterBase);
            ApplyBannerArtHooks();
            _worldBanner.AsyncArt = LaunchOptions.BannerArtSyncOverride != true; // el arte, fuera del fotograma de la carga
            _worldBanner.Show(world, water, flow.ReduceMotion, _canvasW, _canvasH, _safeTop, _safeBottom, travel);
            _worldBanner.Root.SetAsLastSibling(); // por encima de todo: el toque en cualquier punto es suyo
            flow.WorldBannerWaiting = !_worldBanner.CardShown; // sin tarjeta a la vista, ningún toque lo cierra
            _worldBannerShown++;
            _lastBannerProbe = DevProbe.Enabled ? BannerProbeJson(world, level) : null;
            if (_lastBannerProbe != null)
            {
                _probe.Event("world_banner", _lastBannerProbe);
            }

            if (_worldBanner.ConsumeCardShown())
            {
                EmitBannerArt(flow); // en el acto: el arte ya estaba (pedido antes o sin hilo) o el respaldo
            }
        }

        /// <summary>
        /// Pide ya el arte del banner que abrirá el nivel <paramref name="level"/> si, cargado ahora, lo abriría
        /// (<see cref="LevelFlow.WorldBannerAhead"/>): en un hilo del grupo y con las medidas de esta pantalla, así que al abrirse
        /// suele estar ya (<see cref="WorldBannerView.Prefetch"/>). Lo llaman la victoria del nivel anterior (BannerTick) y
        /// GameBootstrap al ofrecer «Jugar». Sin efecto con el banner abierto, si el nivel no lo abriría o si los tests lo piden
        /// en el acto (<see cref="LaunchOptions.BannerArtSyncOverride"/>). True si lo pidió.
        /// </summary>
        internal bool PrefetchWorldBanner(int level)
        {
            LevelFlow? flow = _flow;
            if (flow == null || _worldBanner == null || _worldBanner.Visible || LaunchOptions.BannerArtSyncOverride == true)
            {
                return false;
            }

            int world = flow.WorldBannerAhead(level);
            LevelDefinition? def = world == 0 ? null : flow.Catalog.Find(flow.ResolveLevel(level));
            if (def == null)
            {
                return false;
            }

            ApplyBannerArtHooks();
            _worldBanner.Prefetch(world, Palette.WaterBase(def.WaterBase), _canvasW, TravelFor(level));
            return true;
        }

        private void BannerBuild()
        {
            _worldBanner = new WorldBannerView(_root, _art, OnWorldBannerTapped, OnWorldBannerMapPressed);
        }

        private void BannerLayout()
        {
            if (_worldBanner.Visible)
            {
                _worldBanner.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
            }
        }

        private void BannerTick(float dtMs)
        {
            LevelFlow? flow = _flow;
            if (!_worldBanner.Visible)
            {
                if (flow != null && flow.HasLevel)
                {
                    BannerPrefetchOnWin(flow);
                }

                return;
            }

            bool inBanner = flow != null && flow.HasLevel && flow.Phase == FlowPhase.LevelCard && flow.WorldBanner != 0;
            if (!inBanner)
            {
                // El flujo ya siguió (terminó la salida, un segundo toque la cortó): la tarjeta previa o la intro ya están en
                // marcha, así que el banner se quita en el acto y nunca queda encima de ellas.
                if (!_worldBanner.Dismissing && flow != null && flow.HasLevel)
                {
                    EmitBannerClosed(flow);
                }

                _worldBanner.HideNow();
                return;
            }

            if (flow!.WorldBannerClosing && !_worldBanner.Dismissing)
            {
                StartBannerExit(flow); // el flujo lo cerró por otro camino (TapAnywhere, TapCuia, el fondo…): su salida
                if (!_worldBanner.Visible)
                {
                    return;
                }
            }

            _worldBanner.Tick(dtMs); // esperando el arte: entra con él, o sin él al fallar o al cumplirse su tiempo límite
            if (_worldBanner.ConsumeCardShown())
            {
                flow.WorldBannerWaiting = false; // ya se ve: un toque lo cierra
                EmitBannerArt(flow);
            }

            if (_worldBanner.ConsumeEntered())
            {
                EmitBannerLayout();
            }
        }

        // En la victoria (la celebración o la pantalla de victoria) del nivel anterior a uno que abre mundo (el 10, el 40, el 80 y
        // el 140): su arte se pide ya, una vez por nivel, para que el banner entre en el acto tras «Siguiente».
        private void BannerPrefetchOnWin(LevelFlow flow)
        {
            FlowPhase phase = flow.Phase;
            if (phase != FlowPhase.Celebration && phase != FlowPhase.VictoryScreen)
            {
                return;
            }

            int next = flow.LevelNumber + 1;
            if (_bannerPrefetchLevel == next)
            {
                return;
            }

            _bannerPrefetchLevel = next;
            PrefetchWorldBanner(next);
        }

        private void BannerLevelLoaded()
        {
            // Otro nivel (o el mismo reanudado): fuera el banner anterior. Con el banner cerrado no hay nada que quitar, y el arte
            // pedido antes (PrefetchWorldBanner) se queda para el banner que abre este nivel.
            if (_worldBanner.Visible)
            {
                _worldBanner.HideNow();
            }

            _bannerPrefetchLevel = 0;
        }

        private void BannerLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            // Solo con la tarjeta a la vista (con su arte o con el respaldo): mientras espera el arte solo se ve el velo.
            if (_worldBanner.Visible && _worldBanner.CardShown && !_worldBanner.Dismissing)
            {
                ids.Add("world_banner");
                rects.Add(_worldBanner.Panel);
                if (_worldBanner.Travel != null)
                {
                    ids.Add("world_banner_map"); // «Ver mapa» del viaje de mundo (M3.6)
                    rects.Add((RectTransform)_worldBanner.MapButton.transform);
                }
            }
        }

        // 1 toque en cualquier punto: el flujo empieza la salida (la tarjeta previa o la intro esperan a que termine) y el banner
        // sale. Un segundo toque durante la salida la termina en el acto. Mientras espera el arte (solo el velo), nada. Con el viaje
        // de mundo en marcha (M3.6), el toque lo completa y el siguiente cierra.
        private void OnWorldBannerTapped()
        {
            if (!_worldBanner.Visible || !_worldBanner.CardShown)
            {
                return;
            }

            if (_worldBanner.TravelRunning)
            {
                _worldBanner.CompleteTravel();
                return;
            }

            LevelFlow flow = RequireFlow();
            if (_worldBanner.Dismissing)
            {
                flow.DismissWorldBanner(); // con el banner saliendo, sigue ya con la tarjeta previa o la intro
                _worldBanner.HideNow();
                return;
            }

            StartBannerExit(flow);
        }

        // «Ver mapa» del viaje (M3.6; P-M2): completa el viaje y abre el Mapa del río encima del banner (MapOpenSource.WorldChange);
        // al cerrarlo, el banner sigue ahí, quieto, y un toque lo cierra.
        private void OnWorldBannerMapPressed()
        {
            if (!_worldBanner.Visible || !_worldBanner.CardShown || _worldBanner.Dismissing || _worldBanner.Travel == null)
            {
                return;
            }

            _worldBanner.CompleteTravel();
            if (_actions.OpenRiverMap(MapOpenSource.WorldChange))
            {
                _bannerMapOpened++;
            }
        }

        // El viaje de mundo del banner del nivel level (M3.6), o null: el banner de siempre (apagado por el test, no toca por
        // TravelInBanner, o sin datos del Mapa o sin el tramo de la gran parada). Lee la geometría la primera vez que hace falta.
        // El último viaje resuelto se guarda (CachedTravel): el que construye el arte pedido antes en la celebración del 40, el 80 o
        // el 140 (PrefetchWorldBanner) es el mismo que abre el banner del nivel siguiente, sin volver a pasar por los datos del Mapa
        // (revisión de M3.6: se construía dos veces). Vale mientras sean los mismos datos y los mismos textos (catálogo e idioma).
        private BannerTravel? TravelFor(int level)
        {
            if (BannerTravelOverride == false || !MapMoments.TravelInBanner(level, BannerAutopenOverride ?? MapConfig.UiMapAutopen))
            {
                return null;
            }

            BannerTravelData? data = BannerTravelDataOverride;
            RiverStopCatalog? stops = RiverStops;
            RiverGeometrySource? geometry = RiverGeometry;
            BannerTravel? cached = CachedTravel(level, data, stops, geometry);
            if (cached != null)
            {
                return cached;
            }

            BannerTravel? travel;
            if (data != null)
            {
                travel = BannerTravel.For(level, data.Map, data.Routes, data.Stops);
            }
            else if (stops == null || geometry == null || !geometry(out RiverMap? map, out RiverRoutes? routes))
            {
                return null;
            }
            else
            {
                travel = BannerTravel.For(level, map, routes, stops);
            }

            _travelCache = travel;
            _travelCacheLevel = level;
            _travelCacheData = data;
            _travelCacheStops = stops;
            _travelCacheGeometry = geometry;
            _travelCacheCatalog = Loc.Catalog;
            _travelCacheLocale = Loc.Locale;
            _travelCacheUnapproved = Loc.ShowUnapproved;
            _travelBuilds++;
            return travel;
        }

        // El viaje guardado del nivel level, si se resolvió con estos mismos datos y con los textos de ahora; si no, null.
        private BannerTravel? CachedTravel(int level, BannerTravelData? data, RiverStopCatalog? stops, RiverGeometrySource? geometry)
        {
            if (_travelCache == null || _travelCacheLevel != level || !ReferenceEquals(_travelCacheData, data))
            {
                return null;
            }

            if (data == null && (!ReferenceEquals(_travelCacheStops, stops) || !Equals(_travelCacheGeometry, geometry)))
            {
                return null;
            }

            bool sameTexts = ReferenceEquals(_travelCacheCatalog, Loc.Catalog) && string.Equals(_travelCacheLocale, Loc.Locale, StringComparison.Ordinal)
                && _travelCacheUnapproved == Loc.ShowUnapproved;
            return sameTexts ? _travelCache : null;
        }

        // La salida del banner: la línea world_banner_closed antes de que el flujo siga (con lo que vendrá detrás), el flujo
        // empieza la salida (si no la empezó ya por otro camino) y el banner sale. Los toques no lo cierran mientras espera el arte
        // (LevelFlow.WorldBannerWaiting), así que aquí la tarjeta ya se ve; si aun así no se viera (nada que ver salir), sin
        // salida: WorldBannerExitMs = 0, el flujo sigue en el acto (o en su propio Tick de este fotograma) y el banner se quita.
        private void StartBannerExit(LevelFlow flow)
        {
            EmitBannerClosed(flow);
            bool shown = _worldBanner.CardShown;
            if (!shown)
            {
                flow.WorldBannerExitMs = 0;
                flow.WorldBannerWaiting = false;
            }

            if (!flow.WorldBannerClosing)
            {
                flow.DismissWorldBanner();
            }

            if (!shown)
            {
                _worldBanner.HideNow();
                return;
            }

            _worldBanner.Dismiss();
        }

        // Los ganchos de test del arte del banner (solo tests; en el juego, sin fallo y con el tiempo límite de 800 ms [P]): el
        // arte es compartido entre escenas, así que se fijan antes de cada petición.
        private void ApplyBannerArtHooks()
        {
            _art.BannerArtFault = LaunchOptions.BannerArtFaultOverride ?? BannerArtFault.None;
            _worldBanner.ArtTimeoutLimitMs = LaunchOptions.BannerArtTimeoutMsOverride ?? WorldBannerView.ArtTimeoutMs;
        }

        // AF_STATE world_banner_art: cuándo entró la tarjeta del banner, lo que costó su arte (08 §5.4;
        // ProceduralArt.RequestBannerArt) y si entró sin él (fallback).
        private void EmitBannerArt(LevelFlow flow)
        {
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.WorldBanner); // AU1: la piha, solo aquí, al entrar la tarjeta del banner (07 §9.2)
            AF.Audio.AudioCues.Stinger(AF.Audio.StingerId.WorldBanner); // AU3: su remate, bajo la piha
            if (!DevProbe.Enabled || !flow.HasLevel)
            {
                return;
            }

            ProceduralArt art = _art;
            var sb = new StringBuilder(192);
            sb.Append("{\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(_worldBanner.World.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"async\":").Append(_worldBanner.AsyncArt ? "true" : "false");
            sb.Append(",\"wait_ms\":").Append(_worldBanner.ArtWaitMs.ToString("0", CultureInfo.InvariantCulture));
            sb.Append(",\"mascots_ms\":").Append(art.MascotsMs.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"main_ms\":").Append(art.MascotsMainMs.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"fallback\":\"").Append(_worldBanner.Fallback).Append("\"}");
            _lastBannerArtProbe = sb.ToString();
            _probe.Event("world_banner_art", _lastBannerArtProbe);
        }

        // AF_STATE world_banner_closed: nivel, mundo y lo que sigue, con los valores de "then" (BannerNext).
        private void EmitBannerClosed(LevelFlow flow)
        {
            if (!DevProbe.Enabled || !flow.HasLevel)
            {
                return;
            }

            _lastBannerClosedProbe = "{\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture)
                + ",\"world\":" + _worldBanner.World.ToString(CultureInfo.InvariantCulture)
                + ",\"next\":\"" + BannerNext(flow) + "\"}";
            _probe.Event("world_banner_closed", _lastBannerClosedProbe);
        }

        // Lo que sigue al banner, con un solo vocabulario para "then" y "next": la tarjeta previa ("level_card") o la intro
        // ("intro"). Con el banner abierto o saliendo, lo que saldrá al terminar (LevelFlow.WorldBannerThenCard); ya cerrado, lo
        // que está en marcha.
        private static string BannerNext(LevelFlow flow)
        {
            bool card = flow.WorldBanner != 0 ? flow.WorldBannerThenCard : flow.LevelCard != null;
            return card ? BannerNextLevelCard : BannerNextIntro;
        }

        private void EmitBannerLayout()
        {
            LevelFlow? flow = _flow;
            if (!DevProbe.Enabled || flow == null || !flow.HasLevel)
            {
                return;
            }

            _layoutIds.Clear();
            _layoutRects.Clear();
            _layoutIds.Add("world_banner");
            _layoutRects.Add(_worldBanner.Panel);
            if (_worldBanner.Travel != null)
            {
                _layoutIds.Add("world_banner_map"); // «Ver mapa» del viaje de mundo (M3.6)
                _layoutRects.Add((RectTransform)_worldBanner.MapButton.transform);
            }

            _probe.Layout("world_banner", flow.LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // AF_STATE world_banner: nivel, mundo, texto (world_banner del glosario §10), lo que sale al cerrarlo (la tarjeta previa o
        // la intro: BannerNext), si es la versión quieta y el viaje de mundo (M3.6): travel, travel_ms (lo que dura desde que entra
        // la tarjeta; 0 sin viaje) y flag (la bandera que se iza, o null). Lo que costó su arte va aparte, cuando entra la tarjeta
        // (world_banner_art).
        private string BannerProbeJson(int world, int level)
        {
            LevelFlow flow = RequireFlow();
            var sb = new StringBuilder(224);
            sb.Append("{\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(world.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"text\":\"").Append(UiText.WorldBanner(world)).Append('"');
            sb.Append(",\"then\":\"").Append(BannerNext(flow)).Append('"');
            sb.Append(",\"reduce_motion\":").Append(flow.ReduceMotion ? "true" : "false");
            sb.Append(",\"travel\":").Append(_worldBanner.Travel != null ? "true" : "false");
            sb.Append(",\"travel_ms\":").Append(Mathf.RoundToInt(_worldBanner.TravelMs).ToString(CultureInfo.InvariantCulture));
            string? flag = _worldBanner.TravelFlag;
            sb.Append(",\"flag\":");
            if (flag != null)
            {
                sb.Append('"').Append(flag).Append('"');
            }
            else
            {
                sb.Append("null");
            }

            sb.Append('}');
            return sb.ToString();
        }
    }

    /// <summary>Datos del Mapa para el viaje de mundo del banner (tests: <see cref="GameScreen.BannerTravelDataOverride"/>).</summary>
    internal sealed class BannerTravelData
    {
        /// <summary>Crea los datos.</summary>
        public BannerTravelData(RiverMap map, RiverRoutes routes, RiverStopCatalog stops)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Routes = routes ?? throw new ArgumentNullException(nameof(routes));
            Stops = stops ?? throw new ArgumentNullException(nameof(stops));
        }

        /// <summary>Geometría.</summary>
        public RiverMap Map { get; }

        /// <summary>Rutas de las paradas.</summary>
        public RiverRoutes Routes { get; }

        /// <summary>Paradas.</summary>
        public RiverStopCatalog Stops { get; }
    }
}
