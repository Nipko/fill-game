#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 con su clave del glosario (§5.1, §10) y de GDD 04 §6.5. Clase parcial (arquitectura del paso 1, §9): los
    /// textos de la tarjeta previa, de la Luciérnaga y del banner del mundo van en sus propios archivos (UiText.Cards.cs,
    /// UiText.Reveal.cs y UiText.World.cs).
    /// <para>Idiomas, nivel 1 (D-018; propuesta de idiomas §3.1, §3.2 y §6; tarea L1 de la tanda 4): sin trampas para traducir y
    /// sin cambiar ningún texto en es-419 (lo comprueba el golden de <c>UiTextTrapsTests</c>). Las plantillas que aún viven aquí
    /// llevan marcadores con nombre (<c>{n}</c>, <c>{price}</c>), plurales con la sintaxis de la tabla
    /// (<c>{n:plural:a|b}</c>, nunca <c>n == 1</c>), el salto «\n» dentro de la cadena donde es fijo (lo decide el traductor,
    /// regla de G1) y el corte <c>{sep}</c> solo donde la pantalla elige de verdad si parte la línea; se
    /// formatean con <see cref="LocFormat"/> en es-419, su idioma, y las cifras con los separadores de <see cref="Loc"/> (hoy
    /// es-CO, «1.000»: aún no se lee la región del teléfono). Ninguna pantalla pega trozos ni hace <c>Replace</c> sobre texto
    /// visible. L2b las pasa a <c>Data/Loc/strings.csv</c> y <c>facts.csv</c> con estas mismas claves. Quedan posicionales
    /// (<c>{0}</c>) las cuatro que usan con <c>string.Format</c> pantallas de fuera de L1 (<see cref="Missing"/>,
    /// <see cref="LevelFailedReward"/>, <see cref="NoCanoesRefill"/> y <see cref="NoCanoesInfinite"/>).</para>
    /// </summary>
    public static partial class UiText
    {
        /// <summary>
        /// Separador de la tabla («·» con sus espacios): lo que vale <c>{sep}</c> cuando no es un salto de línea
        /// (<see cref="LocFormat"/>).
        /// </summary>
        internal const string Separator = " · ";

        static UiText()
        {
            // Los textos que se arman una vez y se guardan (la canoa, el banner del mundo) se rehacen si cambian el idioma o
            // la región de las cifras (L2b los saca de la tabla con estas mismas claves).
            Loc.Changed += ClearCaches;
        }

        /// <summary><c>level_n</c>: «Nivel {n}».</summary>
        public const string LevelN = "Nivel {n}";

        /// <summary>
        /// <c>play_level</c> [P]: «Jugar · Nivel {n}», el botón del inicio (06 §4.7; D-008 punto 6) en una sola frase, sin pegar
        /// <c>play</c> y <c>level_n</c> (el orden de las palabras cambia por idioma).
        /// </summary>
        public const string PlayLevelN = "Jugar · Nivel {n}";

        /// <summary>
        /// <c>fauna_sighted_card</c> [P] (guion de la intro §4.5, Q10 A): «avistamiento», nunca «nuevo» ni «tu mono». Pendiente
        /// del dueño (Q10) y del asesor cultural (07 §8.2); EN «Monkey spotted!».
        /// </summary>
        public const string FaunaSightedCard = "¡Miquito a la vista!";

        /// <summary>
        /// «Nivel {n} · intento {k}» [P]: rótulo de la intro corta del reintento (02 §2, INTRO: «En reintento muestra Nivel N ·
        /// intento k»), con k = <c>fails_level</c> + 1. Sin clave en el glosario §10: pendiente con los textos de P14.
        /// </summary>
        public const string LevelAttemptN = "Nivel {n} · intento {attempt}";

        /// <summary>
        /// Título del aviso de perfil en solo lectura [P] (08 §4.4; el texto de la pantalla de <c>biz_min_app_version</c> de
        /// 08 §9). Pendiente de que el dueño decida entre este aviso y una pantalla de bloqueo.
        /// </summary>
        public const string ProfileLockedTitle = "Actualiza Tintalú";

        /// <summary>Cuerpo del aviso con un perfil de una versión más nueva (<c>ProfileLoadStatus.TooNew</c>) [P].</summary>
        public const string ProfileLockedTooNew = "Tu progreso es de una versión más nueva de Tintalú y sigue a salvo. Actualiza la app para jugar con él: lo que juegues ahora no se guardará y no hay compras.";

        /// <summary>Cuerpo del aviso cuando falló la migración del perfil (<c>ProfileLoadStatus.MigrationFailed</c>) [P].</summary>
        public const string ProfileLockedMigration = "No pudimos abrir tu progreso, pero sigue a salvo. Actualiza la app para recuperarlo: lo que juegues ahora no se guardará y no hay compras.";

        /// <summary>Botón del aviso de perfil en solo lectura [P].</summary>
        public const string ProfileLockedPlay = "Jugar sin guardar";

        /// <summary><c>undo</c>: etiqueta funcional de Marea en la bandeja (glosario §0.2).</summary>
        public const string Undo = "Deshacer";

        /// <summary><c>restart</c>: etiqueta funcional de Volver a la orilla.</summary>
        public const string Restart = "Reiniciar";

        /// <summary><c>next</c>.</summary>
        public const string Next = "Siguiente";

        /// <summary><c>level_complete</c>.</summary>
        public const string LevelComplete = "¡Nivel completado!";

        /// <summary><c>stuck_title</c> (= <c>no_moves</c>).</summary>
        public const string StuckTitle = "No hay más vertidos";

        /// <summary><c>stuck_subtitle</c>.</summary>
        public const string StuckSubtitle = "Aguas estancadas";

        /// <summary><c>stuck_undo</c>.</summary>
        public const string StuckUndo = "Deshacer · Marea";

        /// <summary><c>stuck_restart</c>.</summary>
        public const string StuckRestart = "Volver a la orilla (gratis)";

        /// <summary><c>play</c>.</summary>
        public const string Play = "Jugar";

        /// <summary><c>currency</c> (nombre del contador de semillas para el lector de pantalla).</summary>
        public const string Seeds = "Semillas";

        /// <summary><c>lives</c>.</summary>
        public const string Canoes = "Canoas";

        /// <summary><c>settings</c>.</summary>
        public const string Settings = "Ajustes";

        /// <summary>
        /// «Inicio» [P]: botón secundario de la victoria y pestaña de la pantalla de inicio (06 §4.7; D-008 puntos 3 y 6). Aún
        /// sin clave en el glosario §10 (pendiente con los textos de P14).
        /// </summary>
        public const string Home = "Inicio";

        /// <summary><c>back</c>.</summary>
        public const string Back = "Volver";

        /// <summary><c>difficulty_hard</c> (chip del botón Jugar [P], 06 §4.7).</summary>
        public const string DifficultyHard = "Difícil";

        /// <summary><c>very_hard</c> (chip del botón Jugar [P], 06 §4.7).</summary>
        public const string DifficultyVeryHard = "Muy difícil";

        /// <summary>Subtítulo temático de Difícil (glosario §6.1; 02 §10): «Aguas bravas».</summary>
        public const string DifficultySubtitleHard = "Aguas bravas";

        /// <summary>
        /// Subtítulo temático de Muy difícil (glosario §6.1; 02 §10): «Noche de jaguar». Nombra fauna: pendiente de revisión del
        /// asesor cultural (07 §8.2).
        /// </summary>
        public const string DifficultySubtitleVeryHard = "Noche de jaguar";

        /// <summary>
        /// Cinta del premio multiplicado (propuesta-mecanicas-dificultad.md §c.2: «×3» y «×4» sobre las 20 de un nivel normal).
        /// Con «×» U+00D7, que está en el atlas de las fuentes.
        /// </summary>
        public const string RibbonFormat = "×{n}";

        /// <summary>Premio de la tarjeta previa (propuesta §c.1: «Premio: 60»; <c>eco_reward_*</c>, 08 §9).</summary>
        public const string RewardLineFormat = "Premio: {n}";

        /// <summary>Título de la pantalla de final de contenido (06 §9; 08 §6.2).</summary>
        public const string ContentEndTitle = "El río sigue creciendo";

        /// <summary>
        /// Texto [P] de «El río sigue creciendo» con <c>live_content_end_message.next_batch_eta_days</c> (06 §9: «fecha estimada
        /// de los próximos 100 niveles»), redactado sobre el push «Nuevos niveles» de 06 §8.3. Pendiente de aprobación (P14).
        /// Con plural (L1): «en unos {n} días» y, con 1, «en 1 día» [P], nunca «en unos 1 días».
        /// </summary>
        public const string ContentEndEta = "Río arriba llegan 100 niveles nuevos en {n:plural:{} día|unos {} días}.";

        /// <summary>Título de Sin canoas (04 §7.3).</summary>
        public const string NoCanoesTitle = "Sin canoas";

        /// <summary>Cronómetro de Sin canoas (04 §7.3; D-008 paso 4).</summary>
        public const string NoCanoesNext = "Tu próxima canoa vuelve en";

        /// <summary>Etiqueta funcional de Cuia extra en la bandeja (04 §2.2; glosario §0.2).</summary>
        public const string ExtraCuia = "+1 cuia";

        /// <summary>Texto de Capi al regalar la Cuia extra en el nivel 8 (04 §2.2).</summary>
        public const string ExtraCuiaGift = "Capi te trae una cuia más para este nivel. La próxima la encuentras en tu Canasta";

        /// <summary><c>stuck_rescue_cta</c>.</summary>
        public const string StuckRescueCta = "Continuar: +1 cuia";

        /// <summary>
        /// <c>stuck_rescue_discount</c> («Primer rescate del día: −50 %»). El atlas de las fuentes no tiene el signo menos
        /// U+2212: se escribe con la raya corta U+2013, que sí tiene (también en las demás cifras negativas).
        /// </summary>
        public const string StuckRescueDiscount = "Primer rescate del día: –50 %";

        /// <summary>
        /// <c>stuck_rescue_missing</c>: «Te faltan {n} semillas», con plural (L1): con 1, «Te falta 1 semilla», nunca «Te faltan
        /// 1 semillas».
        /// </summary>
        public const string StuckRescueMissing = "{n:plural:Te falta {} semilla|Te faltan {} semillas}";

        /// <summary>
        /// «Te faltan {n}» del precio en rojo (04 §1.1; bandeja). Aún posicional: la usa BoosterTray con <c>string.Format</c>,
        /// fuera de L1 (pasa a la tabla con L2b).
        /// </summary>
        public const string Missing = "Te faltan {0}";

        /// <summary>
        /// <c>stuck_undo_refill_ad</c> (la cifra es <c>undo_refill_amount</c>): «+{n} Deshacer», salto de línea y «Ver video»
        /// (<see cref="StuckRefillVideo"/>). Aguas estancadas siempre la parte ahí: el salto es fijo y va en la cadena («\n»),
        /// donde el traductor puede moverlo; sin <c>{sep}</c>.
        /// </summary>
        public const string StuckUndoRefillAd = "+{n} Deshacer\nVer video";

        /// <summary>
        /// <c>stuck_undo_refill_seeds</c> (cifras: <c>undo_refill_amount</c> y <c>undo_refill_seed_price</c>): «+{n} Deshacer ·
        /// {price} semillas», con plural y el corte <c>{sep}</c>: aquí sí elige la pantalla, en una línea o en dos
        /// (<see cref="StuckRefillSeeds"/>).
        /// </summary>
        public const string StuckUndoRefillSeeds = "+{n} Deshacer{sep}{price:plural:{} semilla|{} semillas}";

        /// <summary>Recarga gratis de +1 Marea de la rama F [P] (04 §6.3; sin clave en 04 §6.5).</summary>
        public const string StuckUndoGift = "+1 Deshacer · gratis";

        /// <summary>
        /// <c>stuck_pack_199</c> («Puñado de semillas · 1.000 · {precio_local}») como la pinta su tarjeta: «Puñado de semillas»,
        /// salto de línea y «{seeds} · {price}» (<see cref="StuckPackLine"/>). El salto es fijo y va en la cadena («\n»).
        /// </summary>
        public const string StuckPack = "Puñado de semillas\n{seeds} · {price}";

        /// <summary>
        /// <c>stuck_balsa_499</c>, primera parte («Balsa de rescate · {precio_local}») como la pinta su tarjeta: «Balsa de
        /// rescate», salto de línea y el precio (<see cref="StuckBalsaLine"/>). El salto es fijo y va en la cadena («\n»).
        /// </summary>
        public const string StuckBalsa = "Balsa de rescate\n{price}";

        /// <summary><c>stuck_balsa_499</c>, segunda parte.</summary>
        public const string StuckBalsaContent = "Todo para seguir: semillas, una cuia y canoas infinitas";

        /// <summary><c>stuck_canoe_lost</c> («Travesía perdida: −1 canoa», con U+2013 por el atlas).</summary>
        public const string StuckCanoeLost = "Travesía perdida: –1 canoa";

        /// <summary><c>stuck_canoe_lost_last</c>.</summary>
        public const string StuckCanoeLostLast = "Travesía perdida: tu última canoa";

        /// <summary><c>stuck_unsolvable</c> (rama F).</summary>
        public const string StuckUnsolvable = "Ni una cuia más salva este tramo. Prueba la Marea o vuelve a la orilla.";

        /// <summary><c>stuck_give_up</c> [P] (D-008; P14).</summary>
        public const string StuckGiveUp = "Rendirse";

        /// <summary><c>rescue_applied</c>.</summary>
        public const string RescueApplied = "¡Una cuia más! Sigue desde aquí";

        /// <summary><c>giveup_confirm_title</c> [P].</summary>
        public const string GiveUpConfirmTitle = "¿Seguro?";

        /// <summary>
        /// «Perderás {n} canoa(s)» con plural (L1): la plantilla de <c>giveup_confirm_canoe</c> [P] y de la segunda parte de
        /// <c>quit_confirm</c> [P], que hoy siempre llevan n = 1 (<see cref="LoseCanoes"/>).
        /// </summary>
        public const string LoseCanoesN = "{n:plural:Perderás {} canoa|Perderás {} canoas}";

        private static string? s_loseOneCanoe;

        /// <summary><c>giveup_confirm_canoe</c> [P]: «Perderás 1 canoa» (<see cref="LoseCanoesN"/> con n = 1).</summary>
        public static string GiveUpConfirmCanoe => s_loseOneCanoe ??= LoseCanoes(1);

        /// <summary>«Perderás tu última canoa» [P] (D-008, paso 2; sin clave propia en 04 §6.5).</summary>
        public const string GiveUpConfirmLastCanoe = "Perderás tu última canoa";

        /// <summary><c>level_failed</c> [P].</summary>
        public const string LevelFailed = "Nivel fallido";

        /// <summary>
        /// <c>level_failed_reward</c> [P]. Aún posicional y sin plural: la usa LevelFailedScreen con <c>string.Format</c>, fuera
        /// de L1 (pasa a la tabla con L2b).
        /// </summary>
        public const string LevelFailedReward = "Premio: {0} semillas";

        /// <summary><c>retry</c> [P].</summary>
        public const string Retry = "Reintentar";

        /// <summary><c>quit_confirm</c> [P], primera parte (título del diálogo).</summary>
        public const string QuitConfirmTitle = "¿Salir del nivel?";

        /// <summary><c>quit_confirm</c> [P], segunda parte: «Perderás 1 canoa» (<see cref="LoseCanoesN"/> con n = 1).</summary>
        public static string QuitConfirmCanoe => s_loseOneCanoe ??= LoseCanoes(1);

        /// <summary>Confirmación de Volver a la orilla con ≥ 8 vertidos (04 §2.7), primera parte.</summary>
        public const string RestartConfirmTitle = "¿Volver a la orilla?";

        /// <summary>Confirmación de Volver a la orilla con ≥ 8 vertidos (04 §2.7), segunda parte.</summary>
        public const string RestartConfirmBody = "Perderás los vertidos de este intento";

        /// <summary>Botón «Volver a la orilla» (pausa y confirmación; glosario §5.1).</summary>
        public const string RestartLong = "Volver a la orilla";

        /// <summary>«Salir» del menú de pausa (D-008).</summary>
        public const string Quit = "Salir";

        /// <summary>Título del menú de pausa [P].</summary>
        public const string Pause = "Pausa";

        /// <summary>Volver a la partida desde la pausa o un diálogo [P].</summary>
        public const string KeepPlaying = "Seguir jugando";

        /// <summary>Sin canoas: «Canoa extra · Ver video» (04 §7.3).</summary>
        public const string NoCanoesVideo = "Canoa extra · Ver video";

        /// <summary>
        /// Sin canoas: «Llenar canoas · {n}/{n}» con la etiqueta de precio (<c>canoes_refill_seed_price</c>) al lado (04 §7.3:
        /// «Llenar canoas · 200 (S) 5/5»). Aún posicional: la usa NoCanoesScreen con <c>string.Format</c>, fuera de L1.
        /// </summary>
        public const string NoCanoesRefill = "Llenar canoas · {0}/{0}";

        /// <summary>
        /// Sin canoas: «Canoas infinitas 1 h · {precio}» (04 §7.3; <c>af_infinite_canoes_1h</c>). Aún posicional: la usa
        /// NoCanoesScreen con <c>string.Format</c>, fuera de L1.
        /// </summary>
        public const string NoCanoesInfinite = "Canoas infinitas 1 h · {0}";

        /// <summary>Sin canoas: «Volver al inicio» (tarea B2-3; el wireframe de 04 §7.3 dice Mapa del río, que aún no existe).</summary>
        public const string BackHome = "Volver al inicio";

        /// <summary><c>river_lowered_color</c>.</summary>
        public const string RiverLoweredColor = "El río bajó: esta vez hay un color menos";

        /// <summary><c>river_lowered_cuia</c>.</summary>
        public const string RiverLoweredCuia = "El río bajó: esta vez hay una cuia más";

        /// <summary>Rótulo de la pantalla de anuncio simulado (B1-3; D-008 punto 4).</summary>
        public const string SimulatedAdClose = "Cerrar";

        /// <summary>Botón de compra de la hoja «Compra simulada» (B1-4).</summary>
        public const string Buy = "Comprar";

        /// <summary>Cancelar la compra simulada.</summary>
        public const string Cancel = "Cancelar";

        /// <summary><c>harvest</c>: fila de la pantalla de victoria con las semillas del nivel (02 §6; glosario §5.1).</summary>
        public const string Harvest = "Cosecha";

        /// <summary><c>double_harvest</c>: botón de la Cosecha doble (D-008 punto 3; 02 §6; glosario: Cosecha doble, x2).</summary>
        public const string DoubleHarvest = "Cosecha doble: x2 por video";

        /// <summary>Lo que gana la Cosecha doble (02 §6: «+20 → +40»).</summary>
        public const string DoubleHarvestDelta = "+{n} → +{doubled}";

        /// <summary>Formato del anuncio simulado en su pantalla (D-008 punto 4).</summary>
        public const string SimulatedAdInterstitial = "intersticial";

        /// <summary>Formato del anuncio simulado recompensado.</summary>
        public const string SimulatedAdRewarded = "recompensado";

        /// <summary>
        /// Rótulo del anuncio simulado (D-008 punto 4; B1-3): «{ubicación} · {formato}» en una sola plantilla, sin pegar trozos.
        /// </summary>
        public const string SimulatedAdLabelFormat = "{placement} · {format}";

        /// <summary>«+20 → +40» para una Cosecha de <paramref name="seeds"/>.</summary>
        public static string DoubleHarvestGain(int seeds)
        {
            return Format(DoubleHarvestDelta, LocArgs.Of("n", NonNegative(seeds)).And("doubled", NonNegative(seeds * 2)));
        }

        /// <summary>"Nivel {n}".</summary>
        public static string Level(int level)
        {
            return Format(LevelN, LocArgs.OfDigits("n", level));
        }

        /// <summary>«Nivel {n} · intento {k}» (02 §2, INTRO del reintento) [P].</summary>
        public static string LevelAttempt(int level, int attempt)
        {
            return Format(LevelAttemptN, LocArgs.OfDigits("n", level).AndDigits("attempt", attempt < 1 ? 1 : attempt));
        }

        /// <summary>«Jugar · Nivel {n}» (<c>play_level</c>; 06 §4.7; D-008 punto 6): una plantilla, sin pegar trozos.</summary>
        public static string PlayLevel(int level)
        {
            return Format(PlayLevelN, LocArgs.OfDigits("n", level));
        }

        /// <summary>«Te faltan {n} semillas» (con 1, «Te falta 1 semilla»).</summary>
        public static string MissingSeeds(int n)
        {
            return Format(StuckRescueMissing, LocArgs.Of("n", NonNegative(n)));
        }

        /// <summary>«Perderás {n} canoa(s)» (<see cref="LoseCanoesN"/>).</summary>
        public static string LoseCanoes(int n)
        {
            return Format(LoseCanoesN, LocArgs.OfDigits("n", n));
        }

        /// <summary>
        /// Recarga de Marea por video en Aguas estancadas: «+{n} Deshacer», salto de línea y «Ver video»
        /// (<see cref="StuckUndoRefillAd"/>; el salto va en la plantilla, nunca un <c>Replace</c>).
        /// </summary>
        public static string StuckRefillVideo(int amount)
        {
            return Format(StuckUndoRefillAd, LocArgs.OfDigits("n", amount));
        }

        /// <summary>
        /// Recarga de Marea por semillas: «+{n} Deshacer · {price} semillas», o en dos líneas (<paramref name="twoLines"/>,
        /// la fila de dos botones de Aguas estancadas) con el salto en el <c>{sep}</c> de la plantilla.
        /// </summary>
        public static string StuckRefillSeeds(int amount, int price, bool twoLines)
        {
            return Format(StuckUndoRefillSeeds, LocArgs.OfDigits("n", amount).AndDigits("price", price), twoLines ? 1 : 0);
        }

        /// <summary>
        /// Tarjeta del Puñado en Aguas estancadas: «Puñado de semillas», salto de línea y «{seeds} · {price}»
        /// (<see cref="StuckPack"/>, con el salto en la plantilla; <paramref name="price"/>, el de la tienda tal cual).
        /// </summary>
        public static string StuckPackLine(int seeds, string price)
        {
            return Format(StuckPack, LocArgs.Of("seeds", NonNegative(seeds)).And("price", price));
        }

        /// <summary>
        /// Tarjeta de la Balsa en Aguas estancadas: «Balsa de rescate», salto de línea y el precio de la tienda
        /// (<see cref="StuckBalsa"/>, con el salto en la plantilla).
        /// </summary>
        public static string StuckBalsaLine(string price)
        {
            return Format(StuckBalsa, LocArgs.Of("price", price));
        }

        /// <summary>
        /// Rótulo del anuncio simulado: la ubicación (<paramref name="placementId"/>, su ID de 05 §6.3) y el formato
        /// (<see cref="SimulatedAdLabelFormat"/>).
        /// </summary>
        public static string SimulatedAdLabel(string placementId, bool rewarded)
        {
            return Format(SimulatedAdLabelFormat, LocArgs.Of("placement", placementId).And("format", rewarded ? SimulatedAdRewarded : SimulatedAdInterstitial));
        }

        /// <summary>
        /// Número con el separador de miles de la región (<see cref="Loc.Number(long)"/>: hoy es-CO, «1.000», glosario §10);
        /// los negativos, como 0.
        /// </summary>
        public static string Grouped(int value)
        {
            return Loc.Number(NonNegative(value));
        }

        /// <summary>Etiqueta del chip de dificultad para <c>difficulty.label</c>, o null si es <c>normal</c>.</summary>
        public static string? DifficultyChip(string label)
        {
            switch (label)
            {
                case LevelDefinition.LabelHard: return DifficultyHard;
                case LevelDefinition.LabelVeryHard: return DifficultyVeryHard;
                default: return null;
            }
        }

        /// <summary>
        /// Subtítulo temático de <c>difficulty.label</c> («Aguas bravas» o «Noche de jaguar», glosario §6.1), o null en
        /// <c>normal</c> o con <c>ui_show_difficulty_subtitles</c> apagado (<see cref="GameConfig.UiShowDifficultySubtitles"/>).
        /// Pendientes de revisión del asesor cultural (07 §8.2).
        /// </summary>
        public static string? DifficultySubtitle(string? label)
        {
            return DifficultySubtitle(label, GameConfig.UiShowDifficultySubtitles);
        }

        /// <summary>
        /// Como <see cref="DifficultySubtitle(string)"/> con el interruptor <paramref name="show"/> explícito (el valor de
        /// <c>ui_show_difficulty_subtitles</c> que llegue de Remote Config; 02 §10: «se apagan si confunden»).
        /// </summary>
        public static string? DifficultySubtitle(string? label, bool show)
        {
            if (!show)
            {
                return null;
            }

            switch (label)
            {
                case LevelDefinition.LabelHard: return DifficultySubtitleHard;
                case LevelDefinition.LabelVeryHard: return DifficultySubtitleVeryHard;
                default: return null;
            }
        }

        /// <summary>«×{n}»: cinta del premio multiplicado (propuesta §c.2).</summary>
        public static string Ribbon(int multiplier)
        {
            return Format(RibbonFormat, LocArgs.OfDigits("n", multiplier));
        }

        /// <summary>«Premio: {n}» (propuesta §c.1), con separador de miles.</summary>
        public static string RewardLine(int seeds)
        {
            return Format(RewardLineFormat, LocArgs.Of("n", NonNegative(seeds)));
        }

        /// <summary>Texto de «El río sigue creciendo» con los días estimados (con 1, «en 1 día»).</summary>
        public static string ContentEnd(int etaDays)
        {
            return Format(ContentEndEta, LocArgs.OfDigits("n", etaDays));
        }

        /// <summary>
        /// Escribe <paramref name="value"/> (los negativos, como 0) en <paramref name="buffer"/> desde <paramref name="pos"/>,
        /// con el separador de miles de la región si <paramref name="grouped"/> (<see cref="Loc.WriteNumber"/>: hoy es-CO,
        /// «1.000», glosario §10). Devuelve la posición siguiente. Sin asignaciones: los recuentos por fotograma lo usan con
        /// <c>TMP_Text.SetCharArray</c>.
        /// </summary>
        public static int WriteNumber(char[] buffer, int pos, int value, bool grouped)
        {
            return Loc.WriteNumber(buffer, pos, NonNegative(value), grouped);
        }

        // Línea de la tarjeta «¡Miquito a la vista!» por clave dato_tarjeta del catálogo de fauna (fauna_<id>_dato; propuesta de
        // idiomas §3.1 y §3.4): una tabla clave → texto en lugar del switch, en el orden de las claves, como la tabla de textos
        // largos. L2b la mueve a facts.csv con estas mismas claves.
        private static readonly Dictionary<string, string> FaunaFacts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["fauna_aotus_trivirgatus_dato"] = "El papá carga a la cría",
            ["fauna_ateles_chamek_dato"] = "Siembra la selva: dispersa muchas semillas",
            ["fauna_cacajao_calvus_ucayalii_dato"] = "Su cara roja es señal de buena salud",
            ["fauna_lagothrix_lagothricha_dato"] = "El mono más grande de Amacayacu",
            ["fauna_mico_argentatus_dato"] = "Vive en Alter do Chão, junto al Tapajós",
            ["fauna_pithecia_pithecia_dato"] = "Casi siempre se mueve a saltos",
            ["fauna_plecturocebus_discolor_dato"] = "Las parejas cantan a dúo al amanecer",
            ["fauna_saguinus_bicolor_dato"] = "Manaos tiende puentes de cuerda para él",
            ["fauna_saguinus_imperator_dato"] = "Su bigote blanco pasa de los hombros",
            ["fauna_saimiri_collinsi_dato"] = "Cruza el bosque en fila, por ramas finas",
            ["fauna_saimiri_sciureus_albigena_dato"] = "Vecinos del Orinoco",
        };

        /// <summary>
        /// Línea de la tarjeta «¡Miquito a la vista!» (≤ 50 caracteres, con un dato verificado; guion de la intro §4.5) para la
        /// clave <c>dato_tarjeta</c> del catálogo de fauna (<c>fauna_&lt;id&gt;_dato</c>); cadena vacía si no hay. Todas
        /// pendientes del asesor cultural (07 §8.2); en las especies amenazadas, una buena noticia de conservación, nunca la
        /// etiqueta UICN. Sin asignaciones.
        /// </summary>
        public static string FaunaFact(string key)
        {
            return key != null && FaunaFacts.TryGetValue(key, out string? text) ? text : string.Empty;
        }

        /// <summary>Claves de <see cref="FaunaFact"/> (tests).</summary>
        public static IEnumerable<string> FaunaFactKeys => FaunaFacts.Keys;

        /// <summary>
        /// Idioma del catálogo de fauna (<c>nombres</c> de <c>miquitos_v0.json</c>: <c>es</c>, <c>pt</c> o <c>en</c>) para el
        /// idioma de la UI <paramref name="locale"/> (<see cref="Loc.Locale"/>): es-* → es, pt-* → pt y el resto (en, en-XA),
        /// en. <c>FaunaSpecies.NameIn</c> cae al español si falta.
        /// </summary>
        public static string FaunaLanguage(string locale)
        {
            if (LocLocale.HasLanguage(locale, "es"))
            {
                return "es";
            }

            return LocLocale.HasLanguage(locale, "pt") ? "pt" : "en";
        }

        /// <summary>
        /// Aplica <paramref name="args"/> a una plantilla de UiText (<see cref="LocFormat"/>): plurales de es-419, el idioma en
        /// que están escritas, y cifras con los separadores de <see cref="Loc"/> (hoy es-CO, «1.000»). El <c>{sep}</c> número i
        /// es un salto de línea si el bit i de <paramref name="breaks"/> está puesto y, si no, <see cref="Separator"/>. Asigna
        /// la cadena: solo al construir o colocar la pantalla, nunca por fotograma.
        /// </summary>
        private static string Format(string template, in LocArgs args, int breaks = 0)
        {
            return LocFormat.Format(template, args, LocLocale.SourceLocale, Loc.NumberFormat, Separator, breaks);
        }

        // Las cantidades de la UI nunca son negativas: como antes, un negativo se escribe 0.
        private static long NonNegative(int value)
        {
            return value < 0 ? 0L : value;
        }

        private static void ClearCaches()
        {
            s_loseOneCanoe = null;
            ClearWorldCaches();
        }

        /// <summary>
        /// Escribe un cronómetro (04 §7.3: «mm:ss»; «h:mm:ss» desde una hora, p. ej. Canoas infinitas acumuladas) con
        /// <paramref name="totalSeconds"/> ≥ 0. Devuelve la posición siguiente. Sin asignaciones.
        /// </summary>
        public static int WriteClock(char[] buffer, int pos, long totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            long hours = totalSeconds / 3600;
            int minutes = (int)((totalSeconds / 60) % 60);
            int seconds = (int)(totalSeconds % 60);
            if (hours > 0)
            {
                pos = WriteNumber(buffer, pos, (int)System.Math.Min(hours, 99), false);
                buffer[pos++] = ':';
            }

            buffer[pos++] = (char)('0' + (minutes / 10));
            buffer[pos++] = (char)('0' + (minutes % 10));
            buffer[pos++] = ':';
            buffer[pos++] = (char)('0' + (seconds / 10));
            buffer[pos++] = (char)('0' + (seconds % 10));
            return pos;
        }
    }
}
