#nullable enable
using System.Collections.Generic;

namespace AF.Boosters
{
    /// <summary>Segmento geográfico del modal (04 §6.1: T1 = US/CA/Europa occ./AU/JP/KR; T3 = LatAm salvo CL/UY).</summary>
    public enum GeoTier
    {
        /// <summary>T3 (LatAm): Puñado antes que Balsa y la recarga de Marea por video primero.</summary>
        T3 = 0,

        /// <summary>T1: Balsa antes que Puñado.</summary>
        T1 = 1,
    }

    /// <summary>Opción de Marea del modal (regla 0 de 04 §6.2: siempre la primera).</summary>
    public enum StuckUndoKind
    {
        /// <summary>«Deshacer · Marea» con cupo o stock (o ilimitada en 1-10).</summary>
        Undo = 0,

        /// <summary>Sin cupo ni stock: «+3 Deshacer · Ver video» (si hay anuncio y tope) y «+3 Deshacer · 30 semillas».</summary>
        Refill = 1,

        /// <summary>Rama F [P]: sin cupo y sin rescate que vender, 1 recarga gratis de +1 uso (04 §6.3).</summary>
        Gift = 2,
    }

    /// <summary>Opción de Cuia de rescate del modal (04 §6.2).</summary>
    public enum StuckRescueKind
    {
        /// <summary>No se ofrece (antes del 31; rama A', 1 por nivel; rama F, no cumple).</summary>
        None = 0,

        /// <summary>«Continuar: +1 cuia» con saldo (rama A).</summary>
        Available = 1,

        /// <summary>En gris con «Te faltan {n} semillas» (rama B).</summary>
        Disabled = 2,
    }

    /// <summary>Ofertas de pago del modal sin saldo (rama B; 04 §6.2 y 05 §4.1).</summary>
    public enum StuckOffer
    {
        /// <summary>Puñado de semillas (<c>af_coins_2</c>, 1,99: 1.000 semillas).</summary>
        Handful = 0,

        /// <summary>Balsa de rescate (<c>af_rescue</c>, 4,99): 1 al día, oculta 48 h tras 2 rechazos.</summary>
        Balsa = 1,
    }

    /// <summary>Línea de canoa bajo el titular (04 §6.5 <c>stuck_canoe_lost</c> / <c>stuck_canoe_lost_last</c>).</summary>
    public enum StuckCanoeLine
    {
        /// <summary>Sin línea (antes del 31, con Canoas infinitas o sin canoa en juego).</summary>
        None = 0,

        /// <summary>«Travesía perdida: −1 canoa».</summary>
        Lost = 1,

        /// <summary>«Travesía perdida: tu última canoa» (rama G).</summary>
        LostLast = 2,
    }

    /// <summary>Línea de la Hoja escudo sobre el pie (04 §6.5 y §6.6, wireframes A y D; 04 §8, <c>stuck_show_shield_status</c>).</summary>
    public enum StuckShieldLine
    {
        /// <summary>Sin línea: sin Crecida en este nivel, con la barra a 0 (no hay racha que romper) o con la línea apagada por RC.</summary>
        None = 0,

        /// <summary>«Hoja escudo lista: este fallo no rompe tu Crecida» (<c>stuck_shield_ready</c>).</summary>
        Ready = 1,

        /// <summary>«Sin Hoja escudo hoy: este fallo rompe tu Crecida de {n}» (<c>stuck_shield_used</c>).</summary>
        Used = 2,
    }

    /// <summary>Pie del modal (04 §6.2; D-008).</summary>
    public enum StuckFooter
    {
        /// <summary>1-30: «Volver a la orilla (gratis)», botón (rama C).</summary>
        RestartFree = 0,

        /// <summary>Desde el 31: «Rendirse» [P], enlace que lleva a ¿Seguro? (o a Nivel fallido con Canoas infinitas).</summary>
        GiveUp = 1,
    }

    /// <summary>Datos con los que se arma el modal (04 §6.1, el subconjunto que existe en el cliente).</summary>
    public struct StuckContext
    {
        /// <summary>Nivel de campaña.</summary>
        public int Level;

        /// <summary><c>rescue_min_level</c> (31).</summary>
        public int RescueMinLevel;

        /// <summary>Marea ilimitada (1-10).</summary>
        public bool UndoUnlimited;

        /// <summary>Cupo gratis de Marea que queda.</summary>
        public int UndoFree;

        /// <summary>Mareas en el stock.</summary>
        public int UndoStock;

        /// <summary>Hay historial que deshacer.</summary>
        public bool HasHistory;

        /// <summary>Hay anuncio de <c>rv_undo_refill</c> cargado y con tope (sin fill, solo el precio; rama E).</summary>
        public bool UndoAdReady;

        /// <summary>Saldo de semillas.</summary>
        public int Seeds;

        /// <summary>Precio de la recarga de Marea (<c>undo_refill_seed_price</c>).</summary>
        public int UndoRefillPrice;

        /// <summary>Mareas que da la recarga (<c>undo_refill_amount</c>).</summary>
        public int UndoRefillAmount;

        /// <summary>Canoas.</summary>
        public int Canoes;

        /// <summary>Canoas infinitas activas.</summary>
        public bool InfiniteCanoes;

        /// <summary>Cuias de rescate compradas en este nivel.</summary>
        public int RescuesLevel;

        /// <summary><c>rescue_max_per_level</c>.</summary>
        public int RescueMaxPerLevel;

        /// <summary>Resultado de «la compra cumple».</summary>
        public RescueCheck RescueCheck;

        /// <summary>Precio congelado al abrir el modal (300 o 600; 0 antes del 31).</summary>
        public int RescuePrice;

        /// <summary>Precio del tramo sin descuento (600).</summary>
        public int RescueBasePrice;

        /// <summary>La Balsa se puede ofrecer hoy (<c>balsa_ok</c> de 04 §6.2).</summary>
        public bool BalsaOk;

        /// <summary>Segmento geográfico.</summary>
        public GeoTier Geo;

        /// <summary>La recarga gratis de la rama F [P] ya se dio en este nivel.</summary>
        public bool UndoGiftUsed;

        /// <summary>
        /// No se ofrecen compras (ni Puñado ni Balsa): el perfil está en solo lectura porque es de una versión más nueva o su
        /// migración falló (08 §4.4) y lo comprado se perdería. Medida mínima [P] a la espera de la decisión del dueño.
        /// </summary>
        public bool PurchasesBlocked;

        /// <summary>
        /// La Crecida cuenta en este nivel (campaña, desde <c>streak_unlock_level</c> y con <c>biz_feature_win_streak</c>; la da
        /// <c>StreakSnapshot.Active</c>; tarea B1b).
        /// </summary>
        public bool StreakActive;

        /// <summary>Victorias de la barra de la Crecida ahora (0-9).</summary>
        public int StreakWins;

        /// <summary>La Hoja escudo de hoy sigue disponible (<c>daily.shield_available</c>).</summary>
        public bool ShieldAvailable;
        /// <summary>
        /// Salto del Boto (rama D, 04 §6.1 <c>skip_eligible</c>; <see cref="SkipBooster"/>): oculto, listo o en gris con lo que falta.
        /// </summary>
        public SkipOffer Skip;
    }

    /// <summary>El modal Aguas estancadas ya decidido (04 §6.2 <c>buildStuckModal</c>; wireframes A-C de §6.6).</summary>
    public sealed class StuckModalView
    {
        internal StuckModalView(StuckCanoeLine canoeLine, StuckUndoKind undo, int undoBadge, bool undoUnlimited, bool undoVideo,
            bool undoVideoFirst, int undoRefillPrice, int undoRefillMissing, StuckRescueKind rescue, int price, int basePrice, int missing,
            IReadOnlyList<StuckOffer> offers, StuckFooter footer, bool unsolvable, StuckShieldLine shieldLine = StuckShieldLine.None, int shieldWins = 0, SkipOffer skip = default)
        {
            ShieldLine = shieldLine;
            ShieldWins = shieldLine == StuckShieldLine.None ? 0 : shieldWins;
            Unsolvable = unsolvable;
            Skip = skip;
            CanoeLine = canoeLine;
            Undo = undo;
            UndoBadge = undoBadge;
            UndoUnlimited = undoUnlimited;
            UndoVideo = undoVideo;
            UndoVideoFirst = undoVideoFirst;
            UndoRefillPrice = undoRefillPrice;
            UndoRefillMissing = undoRefillMissing;
            Rescue = rescue;
            RescuePrice = price;
            RescueBasePrice = basePrice;
            RescueMissing = missing;
            Offers = offers;
            Footer = footer;
        }

        /// <summary>Rama F: «la compra cumple» demostró que ni con una cuia más hay salida (<c>stuck_unsolvable</c>).</summary>
        public bool Unsolvable { get; }

        /// <summary>Línea de canoa bajo el titular.</summary>
        public StuckCanoeLine CanoeLine { get; }

        /// <summary>Opción de Marea (siempre presente: regla 0).</summary>
        public StuckUndoKind Undo { get; }

        /// <summary>Badge de Marea: cupo + stock (0 si ilimitada).</summary>
        public int UndoBadge { get; }

        /// <summary>Marea ilimitada (sin badge).</summary>
        public bool UndoUnlimited { get; }

        /// <summary>La recarga ofrece el video (<c>rv_undo_refill</c>).</summary>
        public bool UndoVideo { get; }

        /// <summary>El video va antes que las semillas (T3, o T1 sin saldo para 30; 04 §6.6).</summary>
        public bool UndoVideoFirst { get; }

        /// <summary>Precio de la recarga por semillas.</summary>
        public int UndoRefillPrice { get; }

        /// <summary>Semillas que faltan para la recarga (0 si alcanza).</summary>
        public int UndoRefillMissing { get; }

        /// <summary>Opción de rescate.</summary>
        public StuckRescueKind Rescue { get; }

        /// <summary>Precio del rescate (con descuento si es el primero del día).</summary>
        public int RescuePrice { get; }

        /// <summary>Precio sin descuento (600 tachado si hay descuento).</summary>
        public int RescueBasePrice { get; }

        /// <summary>Lleva el −50 % del primer rescate del día.</summary>
        public bool RescueDiscounted => RescuePrice < RescueBasePrice;

        /// <summary>Semillas que faltan (rama B).</summary>
        public int RescueMissing { get; }

        /// <summary>Ofertas de pago, en orden (rama B; vacía en el resto).</summary>
        public IReadOnlyList<StuckOffer> Offers { get; }

        /// <summary>Pie.</summary>
        public StuckFooter Footer { get; }

        /// <summary>Línea de la Hoja escudo sobre el pie (tarea B1b; 04 §6.5).</summary>
        public StuckShieldLine ShieldLine { get; }

        /// <summary>La barra que se perdería (el {n} de <c>stuck_shield_used</c>); 0 sin línea.</summary>
        public int ShieldWins { get; }

        /// <summary>
        /// Salto del Boto (rama D; 04 §6.2 <c>opts.add(SKIP(price=300))</c>, tras el rescate y las ofertas, antes del pie): oculto
        /// antes del 31 y si no es elegible; en gris con «Te faltan {n} semillas» sin saldo (04 §1).
        /// </summary>
        public SkipOffer Skip { get; }
    }

    /// <summary>¿Seguro? [P] (paso 2 del embudo de D-008; 04 §6.2 <c>buildGiveUpConfirm</c>).</summary>
    public sealed class GiveUpConfirmView
    {
        internal GiveUpConfirmView(StuckCanoeLine canoeLine, StuckRescueKind rescue, int price, int basePrice, int missing)
        {
            CanoeLine = canoeLine;
            Rescue = rescue;
            RescuePrice = price;
            RescueBasePrice = basePrice;
            RescueMissing = missing;
        }

        /// <summary>
        /// La canoa que se pierde: «Perderás 1 canoa», «Perderás tu última canoa» o ninguna línea si no hay canoa en juego
        /// (Canoas infinitas o 0 canoas; el fallo duro no cobra nada).
        /// </summary>
        public StuckCanoeLine CanoeLine { get; }

        /// <summary>«Perderás tu última canoa» en lugar de «Perderás 1 canoa».</summary>
        public bool LastCanoe => CanoeLine == StuckCanoeLine.LostLast;

        /// <summary>Anuncia la pérdida de una canoa (hay canoa en juego).</summary>
        public bool ShowsCanoe => CanoeLine != StuckCanoeLine.None;

        /// <summary>El mismo rescate del paso 1, con el mismo precio y condiciones; none si no está disponible.</summary>
        public StuckRescueKind Rescue { get; }

        /// <summary>Precio del rescate.</summary>
        public int RescuePrice { get; }

        /// <summary>Precio sin descuento.</summary>
        public int RescueBasePrice { get; }

        /// <summary>Lleva el −50 %.</summary>
        public bool RescueDiscounted => RescuePrice < RescueBasePrice;

        /// <summary>Semillas que faltan.</summary>
        public int RescueMissing { get; }
    }

    /// <summary>
    /// Árbol de decisión de Aguas estancadas (GDD 04 §6.2 y §6.3, con el embudo de D-008): ramas C (1-30), A (con saldo),
    /// A' (2.º bloqueo: sin rescate), B (sin saldo, Puñado y Balsa en el orden de la geo), D (Salto del Boto, con el contexto
    /// de <see cref="SkipBooster"/>; también en A' y F «si elegible»), E (sin fill: solo semillas), F (no cumple: sin rescate,
    /// con la recarga gratis de +1 [P]), G (última canoa) y H (Canoas infinitas: sin línea de canoa). La línea de la Hoja escudo
    /// (04 §6.5 y §6.6; tarea B1b) sale sobre el pie en 1-30 y desde el 31 (<see cref="ShieldLineFor"/>). Sin motor: solo decide qué se ve.
    /// </summary>
    public static class StuckModalModel
    {
        /// <summary><c>buildStuckModal(ctx)</c>.</summary>
        public static StuckModalView Build(StuckContext ctx, BoosterConfig config)
        {
            bool t1 = ctx.Geo == GeoTier.T1;
            bool atOrAfter31 = ctx.Level >= ctx.RescueMinLevel;
            StuckRescueKind rescue = RescueKind(ctx, out int missing);

            // Regla 0: Marea primero, o su recarga.
            StuckUndoKind undo;
            if (ctx.UndoUnlimited || ctx.UndoFree + ctx.UndoStock > 0)
            {
                undo = StuckUndoKind.Undo;
            }
            else if (atOrAfter31 && rescue == StuckRescueKind.None && ctx.RescueCheck == RescueCheck.Unsolvable && !ctx.UndoGiftUsed)
            {
                undo = StuckUndoKind.Gift; // rama F [P]
            }
            else
            {
                undo = StuckUndoKind.Refill;
            }

            int refillMissing = ctx.Seeds >= ctx.UndoRefillPrice ? 0 : ctx.UndoRefillPrice - ctx.Seeds;
            bool videoFirst = ctx.UndoAdReady && (config.UndoVideoFirst(t1) || refillMissing > 0);
            int badge = ctx.UndoUnlimited ? 0 : ctx.UndoFree + ctx.UndoStock;
            if (!atOrAfter31)
            {
                return new StuckModalView(StuckCanoeLine.None, undo, badge, ctx.UndoUnlimited, ctx.UndoAdReady, videoFirst,
                    ctx.UndoRefillPrice, refillMissing, StuckRescueKind.None, 0, 0, 0, new StuckOffer[0], StuckFooter.RestartFree, false,
                    ShieldLineFor(ctx, config), ctx.StreakWins);
            }

            var offers = new List<StuckOffer>(2);
            if (rescue == StuckRescueKind.Disabled && !ctx.PurchasesBlocked)
            {
                bool balsaFirst = config.BalsaBeforeHandful(t1);
                if (balsaFirst && ctx.BalsaOk)
                {
                    offers.Add(StuckOffer.Balsa);
                }

                offers.Add(StuckOffer.Handful);
                if (!balsaFirst && ctx.BalsaOk)
                {
                    offers.Add(StuckOffer.Balsa);
                }
            }

            return new StuckModalView(CanoeLine(ctx), undo, badge, ctx.UndoUnlimited, ctx.UndoAdReady, videoFirst, ctx.UndoRefillPrice,
                refillMissing, rescue, ctx.RescuePrice, ctx.RescueBasePrice, missing, offers, StuckFooter.GiveUp,
                ctx.RescueCheck == RescueCheck.Unsolvable && ctx.RescuesLevel < ctx.RescueMaxPerLevel, ShieldLineFor(ctx, config), ctx.StreakWins,
                ctx.Skip); // rama D
        }

        /// <summary>
        /// <c>buildGiveUpConfirm(ctx)</c> (paso 2 [P]): el mismo rescate y la canoa que se pierde (ninguna línea sin canoa en
        /// juego, por si ¿Seguro? se reabre tras caducar las Canoas infinitas o con 0 canoas).
        /// </summary>
        public static GiveUpConfirmView BuildGiveUpConfirm(StuckContext ctx)
        {
            StuckRescueKind rescue = RescueKind(ctx, out int missing);
            return new GiveUpConfirmView(CanoeLine(ctx), rescue, ctx.RescuePrice, ctx.RescueBasePrice, missing);
        }

        /// <summary>
        /// ¿Hay ¿Seguro? tras «Rendirse»? Solo con una canoa en juego: desde el 31, sin Canoas infinitas y con alguna canoa
        /// (04 §6.2; D-008, paso 2). Si no, «Rendirse» pasa directo a Nivel fallido.
        /// </summary>
        public static bool GiveUpNeedsConfirm(StuckContext ctx)
        {
            return ctx.Level >= ctx.RescueMinLevel && !ctx.InfiniteCanoes && ctx.Canoes > 0;
        }

        /// <summary>
        /// Línea de la Hoja escudo (04 §6.5 y §8; tarea B1b): con <c>stuck_show_shield_status</c> y la Crecida activa en el nivel,
        /// «lista» si queda la hoja de hoy y «sin Hoja escudo hoy… de {n}» si no; sin línea con la barra a 0, porque entonces el
        /// fallo duro no gasta la hoja ni rompe ninguna racha (06 §4.3: «Hoja escudo solo con Crecida ≥ 1») [P]. El río crecido con
        /// la barra a 0 sí se apaga, pero 04 §6.5 no tiene texto para eso: pregunta abierta de B1b.
        /// </summary>
        public static StuckShieldLine ShieldLineFor(StuckContext ctx, BoosterConfig config)
        {
            if (!config.StuckShowShieldStatus || !ctx.StreakActive || ctx.StreakWins < 1)
            {
                return StuckShieldLine.None;
            }

            return ctx.ShieldAvailable ? StuckShieldLine.Ready : StuckShieldLine.Used;
        }

        private static StuckRescueKind RescueKind(StuckContext ctx, out int missing)
        {
            missing = 0;
            if (ctx.Level < ctx.RescueMinLevel || ctx.RescuePrice <= 0 || ctx.RescuesLevel >= ctx.RescueMaxPerLevel
                || !RescueBooster.IsOffered(ctx.RescueCheck))
            {
                return StuckRescueKind.None;
            }

            if (ctx.Seeds >= ctx.RescuePrice)
            {
                return StuckRescueKind.Available;
            }

            missing = ctx.RescuePrice - ctx.Seeds;
            return StuckRescueKind.Disabled;
        }

        private static StuckCanoeLine CanoeLine(StuckContext ctx)
        {
            if (ctx.Level < ctx.RescueMinLevel || ctx.InfiniteCanoes || ctx.Canoes <= 0)
            {
                return StuckCanoeLine.None; // sin canoa en juego no se anuncia ninguna pérdida
            }

            return ctx.Canoes == 1 ? StuckCanoeLine.LostLast : StuckCanoeLine.Lost;
        }
    }
}
