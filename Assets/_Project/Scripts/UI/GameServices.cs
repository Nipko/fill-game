#nullable enable
using System;
using AF.Ads;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
using AF.Presentation;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Lo que las pantallas necesitan para pintar el embudo y la bandeja (D-008; tareas B2-1 a B2-3), por las interfaces de
    /// <c>AF.Economy</c>, <c>AF.Ads</c> y <c>AF.IAP</c> (implementaciones simuladas de la fase 2, sin SDK): la cartera, los
    /// anuncios (¿hay video para esta ubicación?), la tienda (precio local de un SKU, ¿se ofrece la Balsa?) y la geo; y, desde
    /// la tarea L3, el idioma del teléfono (<see cref="IDeviceLocale"/>: <see cref="AndroidDeviceLocale"/> en Android,
    /// <see cref="SystemDeviceLocale"/> en el Editor) y los ajustes del jugador (<c>settings</c> del perfil, 08 §4.3), con los
    /// Patrones de fruta ya resueltos (<see cref="PatternsOnFor"/>).
    /// </summary>
    public sealed class GameServices
    {
        /// <summary>Crea los servicios.</summary>
        /// <param name="wallet">Cartera.</param>
        /// <param name="ads">Anuncios.</param>
        /// <param name="store">Tienda simulada.</param>
        /// <param name="storeLedger">Registro de la tienda en el perfil.</param>
        /// <param name="geo">Segmento geográfico del modal.</param>
        /// <param name="purchasesBlocked">No se ofrecen compras (perfil en solo lectura; ver <see cref="PurchasesBlocked"/>).</param>
        /// <param name="deviceLocale">Idioma del teléfono (null = <see cref="SystemDeviceLocale"/>).</param>
        /// <param name="settings">Ajustes del perfil (null = los de por defecto, sin perfil: tests).</param>
        /// <param name="mapProgress">Lo que lee el Mapa del río del progreso (null = sin Mapa: tests; tarea M3.3).</param>
        public GameServices(Wallet wallet, AdController ads, IStoreService store, StoreLedger storeLedger, GeoTier geo, bool purchasesBlocked = false,
            IDeviceLocale? deviceLocale = null, SettingsData? settings = null, IMapProgress? mapProgress = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Ads = ads ?? throw new ArgumentNullException(nameof(ads));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            StoreLedger = storeLedger ?? throw new ArgumentNullException(nameof(storeLedger));
            Geo = geo;
            PurchasesBlocked = purchasesBlocked;
            DeviceLocale = deviceLocale ?? SystemDeviceLocale.Instance;
            Settings = settings ?? new SettingsData();
            MapProgress = mapProgress;
        }

        /// <summary>
        /// Lo que lee el Mapa del río del progreso (D-013; 08 §2.2 y §4.3; tarea M3.3): <c>max_level</c>, <c>next_level</c>,
        /// <c>map_seen_level</c> y los niveles saltados, sin asignaciones (<see cref="IMapProgress"/>, el mismo
        /// <see cref="IProgressStore"/> de la partida). Lo consultan la pestaña del inicio (visible desde <c>max_level</c> ≥ 10,
        /// con su punto si hay una llegada sin ver) y la pantalla del Mapa. Null sin progreso (tests sin partida).
        /// </summary>
        public IMapProgress? MapProgress { get; }

        /// <summary>
        /// Idioma del teléfono (D-018, Q7-A y Q8-A; tarea L3): el tercer paso de la resolución del idioma y la región de las
        /// cifras.
        /// </summary>
        public IDeviceLocale DeviceLocale { get; }

        /// <summary><c>settings</c> del perfil (08 §4.3): idioma y Patrones de fruta que elige Ajustes (tarea L3).</summary>
        public SettingsData Settings { get; }

        /// <summary>Cartera.</summary>
        public Wallet Wallet { get; }

        /// <summary>Anuncios (reglas, proveedor simulado y registro).</summary>
        public AdController Ads { get; }

        /// <summary>Tienda simulada.</summary>
        public IStoreService Store { get; }

        /// <summary>Lo que la tienda anota en el perfil (Balsa de rescate).</summary>
        public StoreLedger StoreLedger { get; }

        /// <summary>
        /// Segmento geográfico del modal [P]: T3 (LatAm) mientras no haya detección de país (05 §4.2; el lanzamiento es en
        /// América Latina).
        /// </summary>
        public GeoTier Geo { get; }

        /// <summary>
        /// No se ofrecen compras: el perfil del dispositivo es de una versión más nueva o su migración falló
        /// (<c>ProfileLoadStatus.TooNew</c> o <c>MigrationFailed</c>; 08 §4.4) y nada de esta sesión se puede guardar, así que una
        /// compra se perdería al cerrar la app. Sin Puñado, Balsa ni Canoas infinitas en el embudo. Medida mínima [P]: la
        /// pantalla de bloqueo «Actualiza Tintalú» (como <c>biz_min_app_version</c> de 08 §9) la decide el dueño.
        /// </summary>
        public bool PurchasesBlocked { get; }

        /// <summary>El botón de video de <paramref name="placement"/> se enseña (reglas, tope, cooldown y fill; 05 §6.2).</summary>
        public bool RewardedReady(AdPlacement placement)
        {
            return Ads.CheckRewarded(placement).IsAllowed;
        }

        /// <summary>
        /// Precio local de <paramref name="sku"/> (05 §4.2) como lo da la tienda, escrito con la función única de precios
        /// (<c>Loc.Price</c>; D-025): p. ej. <c>COP 3.900</c> en es-CO y <c>COP 3,900</c> en en-US. Todos los textos con
        /// precio (Aguas estancadas, Sin canoas) lo toman de aquí.
        /// </summary>
        public string PriceText(string sku)
        {
            return Store.PriceOf(sku).ToString();
        }

        /// <summary>Semillas que da el Puñado (<c>af_coins_2</c>; <c>eco_seed_pack_amounts</c>).</summary>
        public int HandfulSeeds => StoreLedger.Config.SeedPackAmount(StoreCatalog.Handful);

        /// <summary>
        /// Patrones de fruta en el nivel <paramref name="level"/> con el ajuste del jugador (<see cref="PatternsResolver.IsOn"/>:
        /// auto = desde el 41, on = siempre, off = nunca; 02 §12). Sin asignaciones.
        /// </summary>
        public bool PatternsOnFor(int level)
        {
            return PatternsResolver.IsOn(Settings.Patterns, level);
        }
    }

    /// <summary>
    /// Patrones de fruta resueltos con el ajuste (02 §12; GDD.md §2 #5: el interruptor existe siempre en Ajustes; tarea L3):
    /// <c>AF.Save</c> guarda <see cref="PatternsSetting"/> y <c>AF.Presentation</c> solo recibe el bool
    /// (<c>BoardAnimator.ApplyPatterns</c>), porque no lee el perfil (08 §2.2).
    /// </summary>
    public static class PatternsResolver
    {
        /// <summary>
        /// Si van los Patrones de fruta en el nivel <paramref name="level"/>: <see cref="PatternsSetting.On"/> siempre,
        /// <see cref="PatternsSetting.Off"/> nunca y <see cref="PatternsSetting.Auto"/> según el nivel
        /// (<see cref="Palette.PatternsOnFor"/>: desde el 41, <c>ui_patterns_default_from_level</c>). Sin asignaciones.
        /// </summary>
        public static bool IsOn(PatternsSetting setting, int level)
        {
            switch (setting)
            {
                case PatternsSetting.On: return true;
                case PatternsSetting.Off: return false;
                default: return Palette.PatternsOnFor(level);
            }
        }
    }

    /// <summary>
    /// Desde dónde se abre la ficha de una fruta de Frutas del río (<see cref="FruitSheet"/>; D-017 Q4-A: «desde la línea y desde
    /// Ajustes › Sobre la Amazonía»; tarea F4). Va en la sonda <c>af_fact_page_open</c> como <c>from</c> (solo en desarrollo: el
    /// evento de 08 §8.2 lleva <c>tint_id</c> y <c>unlocked</c>).
    /// </summary>
    public enum FruitSheetSource
    {
        /// <summary>La línea de fruta de la victoria.</summary>
        Victory = 0,

        /// <summary>Ajustes › «Sobre la Amazonía» › «Frutas del río».</summary>
        Settings = 1,
    }

    /// <summary>
    /// Desde dónde se abre el Mapa del río (<c>af_map_open.source</c>, 08 §8.2; D-013, punto 7; propuesta del Mapa §8.1 y
    /// §11.5): la pestaña del inicio (M3.3), el banner de mundo (M3.6), el Mapa del 10 (M4.2) y, desde la tarea M4.3, la pausa
    /// (encima del nivel, con «Volver al nivel» y sin salir de él: 04 §5.2), «Ver mapa» en la victoria del <c>10k</c> (encima de
    /// la victoria, que sigue igual debajo) y el fin de contenido (antes de «El río sigue creciendo», 06 §9).
    /// </summary>
    public enum MapOpenSource
    {
        /// <summary>Pestaña del Mapa en el inicio (<c>lobby</c>).</summary>
        Lobby = 0,

        /// <summary>Pausa del nivel (<c>pause</c>): encima del nivel, sin salir de él (D-013, punto 7).</summary>
        Pause = 1,

        /// <summary>«Ver mapa» de la victoria del <c>10k</c> (<c>victory</c>): encima de la victoria, que vuelve tal cual al cerrarlo.</summary>
        Victory = 2,

        /// <summary>«Ver mapa» del banner de mundo (<c>world_change</c>).</summary>
        WorldChange = 3,

        /// <summary>El Mapa del 10, que sale solo (<c>l10</c>; 06 §2.3).</summary>
        L10 = 4,

        /// <summary>Al acabarse el contenido (<c>content_end</c>; 06 §9): antes de «El río sigue creciendo», que queda al cerrarlo.</summary>
        ContentEnd = 5,
    }

    /// <summary>
    /// Puntos de entrada del input de la pantalla de nivel y del embudo (los mismos que llaman los toques y los tests PlayMode;
    /// los implementa <see cref="GameBootstrap"/>).
    /// </summary>
    public interface IGameActions
    {
        /// <summary>Toque en una cuia.</summary>
        TapResult TapCuia(int index);

        /// <summary>Toque en el fondo.</summary>
        void TapBackground();

        /// <summary>Toque durante la intro o la celebración.</summary>
        bool TapAnywhere();

        /// <summary>Marea (bandeja o modal).</summary>
        bool PressUndo();

        /// <summary>Volver a la orilla (cabecera, pausa o pie del modal en 1-30), ya confirmado si hacía falta.</summary>
        bool PressRestart();

        /// <summary>Siguiente (victoria).</summary>
        bool PressNext();

        /// <summary>Inicio (victoria).</summary>
        bool PressHome();

        /// <summary>«Cosecha doble: x2 por video» (victoria, desde el 11).</summary>
        bool PressDoubleHarvest();

        /// <summary>«+1 cuia» de la bandeja.</summary>
        bool PressExtraCuia();

        /// <summary>
        /// Revelar · Luciérnaga de la bandeja (04 §2.5): abre el modo de selección pagando con el stock, si no con
        /// <c>reveal_seed_price</c> semillas y, si no, con el video <c>rv_reveal</c>; sin nada, avisa «Te faltan {n} semillas» y
        /// no cobra. Con la selección ya abierta, la cancela sin coste.
        /// </summary>
        bool PressReveal();

        /// <summary>
        /// Video <c>rv_reveal</c> de Revelar (04 §2.5: «1 revelado»): al terminar el anuncio, +1 Luciérnaga al stock y el modo de
        /// selección pagando con ella. False si no hay video (sin fill no aparece) o no se puede revelar ahora.
        /// </summary>
        bool PressRevealVideo();

        /// <summary>«+3 Deshacer · Ver video».</summary>
        bool PressUndoRefillVideo();

        /// <summary>«+3 Deshacer · 30 semillas».</summary>
        bool PressUndoRefillSeeds();

        /// <summary>Recarga gratis de la rama F [P].</summary>
        bool PressUndoGift();

        /// <summary>«Continuar: +1 cuia» (modal o ¿Seguro?).</summary>
        bool PressRescue();

        /// <summary>Puñado de semillas (compra simulada).</summary>
        bool PressHandful();

        /// <summary>Balsa de rescate (compra simulada).</summary>
        bool PressBalsa();

        /// <summary>«Rendirse» en el modal.</summary>
        bool PressGiveUp();

        /// <summary>X o «Rendirse» en ¿Seguro?</summary>
        bool ConfirmGiveUp();

        /// <summary>«Reintentar» en Nivel fallido.</summary>
        bool PressRetry();

        /// <summary>X de Nivel fallido: al inicio.</summary>
        bool PressLevelFailedClose();

        /// <summary>«Canoa extra · Ver video».</summary>
        bool PressExtraCanoeVideo();

        /// <summary>«Llenar canoas · 200 semillas».</summary>
        bool PressRefillCanoes();

        /// <summary>«Canoas infinitas 1 h» (compra simulada).</summary>
        bool PressInfiniteCanoes();

        /// <summary>«Volver al inicio» de Sin canoas.</summary>
        bool PressNoCanoesHome();

        /// <summary>X de Sin canoas: vuelve a donde se abrió.</summary>
        bool PressNoCanoesClose();

        /// <summary>Pausa › «Salir», ya confirmado desde el 31.</summary>
        bool PressQuit();

        /// <summary>
        /// Abre el Mapa del río (D-013; 06 §4.7; tarea M3.3) desde <paramref name="source"/>, en la vista Mundo. False si no hay
        /// Mapa (faltan sus datos), si ya está abierto, si delante hay una pantalla de pago o el aviso del perfil bloqueado, o si
        /// esa entrada no está disponible ahora: la pestaña del inicio y la pausa, desde <c>max_level</c> ≥ 10 (la pausa, con el
        /// nivel en juego); «Ver mapa» de la victoria, con la victoria a la vista; el fin de contenido, con «El río sigue creciendo»
        /// (tarea M4.3).
        /// </summary>
        bool OpenRiverMap(MapOpenSource source);

        /// <summary>
        /// Cierra el Mapa del río (← de la cabecera o el Atrás de Android) y vuelve a donde se abrió; anota lo visto
        /// (<see cref="MarkMapSeen"/>) hasta <c>max_level</c>. False si no estaba abierto.
        /// </summary>
        bool CloseRiverMap();

        /// <summary>
        /// El jugador vio el estado del Mapa hasta el nivel <paramref name="level"/> (<c>progress.map_seen_level</c>; 08 §4.3):
        /// monotónico, recortado a <c>max_level</c> (<see cref="IProgressStore.SaveMapSeenLevel"/>). Quita el punto de la pestaña
        /// del inicio cuando ya no hay llegadas sin ver. False si no cambió nada o no hay progreso.
        /// </summary>
        bool MarkMapSeen(int level);

        /// <summary>
        /// Abre la ficha de la serie de Frutas del río <paramref name="subject"/> (<c>acai</c>, <c>rio_napo</c>…; D-017 Q4-A; tarea
        /// F4; <see cref="FruitSheet"/>) desde <paramref name="source"/>: desde la victoria, solo la de la línea que se ve y solo con
        /// la victoria a la vista; desde Ajustes, con Ajustes abierta. Escribe la sonda <c>af_fact_page_open</c> {tint_id, unlocked}
        /// (08 §8.2). Mientras se ve, la victoria no avanza. False si no se abre.
        /// </summary>
        bool OpenFruitSheet(string subject, FruitSheetSource source);

        /// <summary>Cierra la ficha de una fruta (su X, el velo o el Atrás de Android): se vuelve a lo de debajo tal como estaba.</summary>
        bool CloseFruitSheet();
    }
}
