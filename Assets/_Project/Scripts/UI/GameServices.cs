#nullable enable
using System;
using AF.Ads;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;

namespace AF.UI
{
    /// <summary>
    /// Lo que las pantallas necesitan para pintar el embudo y la bandeja (D-008; tareas B2-1 a B2-3), por las interfaces de
    /// <c>AF.Economy</c>, <c>AF.Ads</c> y <c>AF.IAP</c> (implementaciones simuladas de la fase 2, sin SDK): la cartera, los
    /// anuncios (¿hay video para esta ubicación?), la tienda (precio local de un SKU, ¿se ofrece la Balsa?) y la geo.
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
        public GameServices(Wallet wallet, AdController ads, IStoreService store, StoreLedger storeLedger, GeoTier geo, bool purchasesBlocked = false)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Ads = ads ?? throw new ArgumentNullException(nameof(ads));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            StoreLedger = storeLedger ?? throw new ArgumentNullException(nameof(storeLedger));
            Geo = geo;
            PurchasesBlocked = purchasesBlocked;
        }

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

        /// <summary>Precio local de <paramref name="sku"/>, p. ej. <c>COP 3.900</c> (05 §4.2).</summary>
        public string PriceText(string sku)
        {
            return Store.PriceOf(sku).ToString();
        }

        /// <summary>Semillas que da el Puñado (<c>af_coins_2</c>; <c>eco_seed_pack_amounts</c>).</summary>
        public int HandfulSeeds => StoreLedger.Config.SeedPackAmount(StoreCatalog.Handful);
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
    }
}
