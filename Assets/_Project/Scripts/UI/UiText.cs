#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Acceso tipado a los textos de la UI (08 §2.2: «<c>UiText</c> queda como acceso tipado»; D-018; contrato 32): cada texto es
    /// una propiedad o un método que pide su clave a <see cref="Loc"/>, y los textos viven en las tablas
    /// <c>Assets/_Project/Data/Loc/strings.csv</c> (UI) y <c>facts.csv</c> (textos largos), con las claves de <see cref="Keys"/>
    /// (glosario §10 y GDD 04 §6.5, o derivadas [P]). Clase parcial (arquitectura del paso 1, §9): los textos de la tarjeta
    /// previa, de la Luciérnaga y del banner del mundo van en UiText.Cards.cs, UiText.Reveal.cs y UiText.World.cs.
    /// <list type="bullet">
    /// <item>Idiomas, nivel 2 (propuesta de idiomas §6; tarea L2b de la tanda 4): ningún texto en el código. En es-419 los textos
    /// son los mismos que antes, carácter a carácter (lo comprueba el golden de <c>UiTextTrapsTests</c>).</item>
    /// <item>Los textos sin argumentos no asignan memoria (<see cref="Loc.T(string)"/>). Los que llevan argumentos
    /// (<see cref="Loc.T(string, in LocArgs, string, int)"/>: marcadores con nombre, plurales del idioma de <see cref="Loc"/> y
    /// cifras con los separadores de su región) asignan la cadena: solo al construir o colocar la pantalla, nunca por fotograma.
    /// Los que se arman una vez y se guardan (la canoa, el banner del mundo) se rehacen con <see cref="Loc.Changed"/>.</item>
    /// <item>Sin configurar <see cref="Loc"/> (o con una clave que falta) se ve «[clave]» en desarrollo y nada en release:
    /// <c>GameBootstrap</c> lo configura antes de construir la UI y los tests, en su <c>[SetUpFixture]</c>.</item>
    /// </list>
    /// </summary>
    public static partial class UiText
    {
        /// <summary>
        /// Separador de la tabla («·» con sus espacios): lo que vale <c>{sep}</c> cuando no es un salto de línea
        /// (<see cref="LocFormat"/>).
        /// </summary>
        internal const string Separator = " · ";

        private static string? s_loseOneCanoe;
        private static string? s_quitOneCanoe;

        static UiText()
        {
            // Los textos que se arman una vez y se guardan (la canoa, el banner del mundo) se rehacen si cambian el idioma o
            // la región de las cifras.
            Loc.Changed += ClearCaches;
        }

        /// <summary><c>fauna_sighted_card</c> [P] (guion de la intro §4.5, Q10 A): «¡Miquito a la vista!». Pendiente del asesor.</summary>
        public static string FaunaSightedCard => Loc.T(Keys.FaunaSightedCard);

        /// <summary>Título del aviso de perfil en solo lectura [P] (08 §4.4): «Actualiza Tintalú».</summary>
        public static string ProfileLockedTitle => Loc.T(Keys.ProfileLockedTitle);

        /// <summary>Cuerpo del aviso con un perfil de una versión más nueva (<c>ProfileLoadStatus.TooNew</c>) [P].</summary>
        public static string ProfileLockedTooNew => Loc.T(Keys.ProfileLockedTooNew);

        /// <summary>Cuerpo del aviso cuando falló la migración del perfil (<c>ProfileLoadStatus.MigrationFailed</c>) [P].</summary>
        public static string ProfileLockedMigration => Loc.T(Keys.ProfileLockedMigration);

        /// <summary>Botón del aviso de perfil en solo lectura [P]: «Jugar sin guardar».</summary>
        public static string ProfileLockedPlay => Loc.T(Keys.ProfileLockedPlay);

        /// <summary><c>undo</c>: etiqueta funcional de Marea en la bandeja (glosario §0.2).</summary>
        public static string Undo => Loc.T(Keys.Undo);

        /// <summary><c>restart</c>: etiqueta funcional de Volver a la orilla.</summary>
        public static string Restart => Loc.T(Keys.Restart);

        /// <summary><c>next</c>.</summary>
        public static string Next => Loc.T(Keys.Next);

        /// <summary><c>level_complete</c>.</summary>
        public static string LevelComplete => Loc.T(Keys.LevelComplete);

        /// <summary><c>stuck_title</c> (= <c>no_moves</c>).</summary>
        public static string StuckTitle => Loc.T(Keys.StuckTitle);

        /// <summary><c>stuck_subtitle</c>.</summary>
        public static string StuckSubtitle => Loc.T(Keys.StuckSubtitle);

        /// <summary><c>stuck_undo</c>.</summary>
        public static string StuckUndo => Loc.T(Keys.StuckUndo);

        /// <summary><c>stuck_restart</c>.</summary>
        public static string StuckRestart => Loc.T(Keys.StuckRestart);

        /// <summary><c>play</c>.</summary>
        public static string Play => Loc.T(Keys.Play);

        /// <summary><c>currency</c> (nombre del contador de semillas para el lector de pantalla).</summary>
        public static string Seeds => Loc.T(Keys.Currency);

        /// <summary><c>lives</c>.</summary>
        public static string Canoes => Loc.T(Keys.Lives);

        /// <summary><c>settings</c>.</summary>
        public static string Settings => Loc.T(Keys.Settings);

        /// <summary><c>home</c> [P]: botón secundario de la victoria y pestaña de la pantalla de inicio (06 §4.7; D-008).</summary>
        public static string Home => Loc.T(Keys.Home);

        /// <summary><c>back</c>.</summary>
        public static string Back => Loc.T(Keys.Back);

        /// <summary><c>difficulty_hard</c> (chip del botón Jugar [P], 06 §4.7).</summary>
        public static string DifficultyHard => Loc.T(Keys.DifficultyHard);

        /// <summary><c>very_hard</c> (chip del botón Jugar [P], 06 §4.7).</summary>
        public static string DifficultyVeryHard => Loc.T(Keys.VeryHard);

        /// <summary>Subtítulo temático de Difícil (glosario §6.1; 02 §10): «Aguas bravas».</summary>
        public static string DifficultySubtitleHard => Loc.T(Keys.DifficultySubtitleHard);

        /// <summary>
        /// Subtítulo temático de Muy difícil (glosario §6.1; 02 §10): «Noche de jaguar». Nombra fauna: pendiente de revisión del
        /// asesor cultural (07 §8.2).
        /// </summary>
        public static string DifficultySubtitleVeryHard => Loc.T(Keys.DifficultySubtitleVeryHard);

        /// <summary>Título de la pantalla de final de contenido (06 §9; 08 §6.2).</summary>
        public static string ContentEndTitle => Loc.T(Keys.ContentEndTitle);

        /// <summary>Título de Sin canoas (04 §7.3).</summary>
        public static string NoCanoesTitle => Loc.T(Keys.NoCanoesTitle);

        /// <summary>Cronómetro de Sin canoas (04 §7.3; D-008 paso 4).</summary>
        public static string NoCanoesNext => Loc.T(Keys.NoCanoesNext);

        /// <summary>Etiqueta funcional de Cuia extra en la bandeja (04 §2.2; glosario §0.2).</summary>
        public static string ExtraCuia => Loc.T(Keys.ExtraCuia);

        /// <summary>Texto de Capi al regalar la Cuia extra en el nivel 8 (04 §2.2).</summary>
        public static string ExtraCuiaGift => Loc.T(Keys.ExtraCuiaGift);

        /// <summary><c>stuck_rescue_cta</c>.</summary>
        public static string StuckRescueCta => Loc.T(Keys.StuckRescueCta);

        /// <summary>
        /// <c>stuck_rescue_discount</c> («Primer rescate del día: −50 %»). El atlas de las fuentes no tiene el signo menos
        /// U+2212: la tabla lo escribe con la raya corta U+2013, que sí tiene (también en las demás cifras negativas).
        /// </summary>
        public static string StuckRescueDiscount => Loc.T(Keys.StuckRescueDiscount);

        /// <summary>Recarga gratis de +1 Marea de la rama F [P] (04 §6.3; sin clave en 04 §6.5).</summary>
        public static string StuckUndoGift => Loc.T(Keys.StuckUndoGift);

        /// <summary><c>stuck_balsa_499</c>, segunda parte: el contenido de la Balsa.</summary>
        public static string StuckBalsaContent => Loc.T(Keys.StuckBalsaContent);

        /// <summary><c>stuck_canoe_lost</c> («Travesía perdida: −1 canoa», con U+2013 por el atlas).</summary>
        public static string StuckCanoeLost => Loc.T(Keys.StuckCanoeLost);

        /// <summary><c>stuck_canoe_lost_last</c>.</summary>
        public static string StuckCanoeLostLast => Loc.T(Keys.StuckCanoeLostLast);

        /// <summary><c>stuck_unsolvable</c> (rama F).</summary>
        public static string StuckUnsolvable => Loc.T(Keys.StuckUnsolvable);

        /// <summary><c>stuck_give_up</c> [P] (D-008; P14).</summary>
        public static string StuckGiveUp => Loc.T(Keys.StuckGiveUp);

        /// <summary><c>rescue_applied</c>.</summary>
        public static string RescueApplied => Loc.T(Keys.RescueApplied);

        /// <summary><c>giveup_confirm_title</c> [P].</summary>
        public static string GiveUpConfirmTitle => Loc.T(Keys.GiveUpConfirmTitle);

        /// <summary>
        /// <c>giveup_confirm_canoe</c> [P]: «Perderás 1 canoa», la plantilla con plural «Perderás {n} canoa(s)» con n = 1 (hoy
        /// siempre se pierde una; <see cref="LoseCanoes"/>). Se arma una vez por idioma.
        /// </summary>
        public static string GiveUpConfirmCanoe => s_loseOneCanoe ??= LoseCanoes(1);

        /// <summary>«Perderás tu última canoa» [P] (D-008, paso 2; sin clave propia en 04 §6.5).</summary>
        public static string GiveUpConfirmLastCanoe => Loc.T(Keys.GiveUpConfirmLastCanoe);

        /// <summary><c>level_failed</c> [P].</summary>
        public static string LevelFailed => Loc.T(Keys.LevelFailed);

        /// <summary><c>retry</c> [P].</summary>
        public static string Retry => Loc.T(Keys.Retry);

        /// <summary><c>quit_confirm</c> [P], primera parte (título del diálogo).</summary>
        public static string QuitConfirmTitle => Loc.T(Keys.QuitConfirmTitle);

        /// <summary>
        /// <c>quit_confirm</c> [P], segunda parte: «Perderás 1 canoa», su plantilla con plural con n = 1. Se arma una vez por
        /// idioma.
        /// </summary>
        public static string QuitConfirmCanoe => s_quitOneCanoe ??= Format(Keys.QuitConfirmCanoe, LocArgs.OfDigits("n", 1));

        /// <summary>Confirmación de Volver a la orilla con ≥ 8 vertidos (04 §2.7), primera parte.</summary>
        public static string RestartConfirmTitle => Loc.T(Keys.RestartConfirmTitle);

        /// <summary>Confirmación de Volver a la orilla con ≥ 8 vertidos (04 §2.7), segunda parte.</summary>
        public static string RestartConfirmBody => Loc.T(Keys.RestartConfirmBody);

        /// <summary>Botón «Volver a la orilla» (pausa y confirmación; glosario §5.1).</summary>
        public static string RestartLong => Loc.T(Keys.RestartLong);

        /// <summary>«Salir» del menú de pausa (D-008).</summary>
        public static string Quit => Loc.T(Keys.Quit);

        /// <summary>Título del menú de pausa.</summary>
        public static string Pause => Loc.T(Keys.Pause);

        /// <summary>Volver a la partida desde la pausa o un diálogo [P].</summary>
        public static string KeepPlaying => Loc.T(Keys.KeepPlaying);

        /// <summary>Sin canoas: «Canoa extra · Ver video» (04 §7.3).</summary>
        public static string NoCanoesVideo => Loc.T(Keys.NoCanoesVideo);

        /// <summary>Sin canoas: «Volver al inicio» (tarea B2-3; el wireframe de 04 §7.3 dice Mapa del río, que aún no existe).</summary>
        public static string BackHome => Loc.T(Keys.BackHome);

        /// <summary><c>river_lowered_color</c>.</summary>
        public static string RiverLoweredColor => Loc.T(Keys.RiverLoweredColor);

        /// <summary><c>river_lowered_cuia</c>.</summary>
        public static string RiverLoweredCuia => Loc.T(Keys.RiverLoweredCuia);

        /// <summary>Rótulo de la pantalla de anuncio simulado (B1-3; D-008 punto 4).</summary>
        public static string SimulatedAdClose => Loc.T(Keys.SimulatedAdClose);

        /// <summary>Botón de compra de la hoja «Compra simulada» (B1-4).</summary>
        public static string Buy => Loc.T(Keys.Buy);

        /// <summary>Cancelar la compra simulada.</summary>
        public static string Cancel => Loc.T(Keys.Cancel);

        /// <summary><c>harvest</c>: fila de la pantalla de victoria con las semillas del nivel (02 §6; glosario §5.1).</summary>
        public static string Harvest => Loc.T(Keys.Harvest);

        /// <summary><c>double_harvest</c>: botón de la Cosecha doble (D-008 punto 3; 02 §6; glosario: Cosecha doble, x2).</summary>
        public static string DoubleHarvest => Loc.T(Keys.DoubleHarvest);

        /// <summary>Formato del anuncio simulado en su pantalla (D-008 punto 4).</summary>
        public static string SimulatedAdInterstitial => Loc.T(Keys.SimulatedAdInterstitial);

        /// <summary>Formato del anuncio simulado recompensado.</summary>
        public static string SimulatedAdRewarded => Loc.T(Keys.SimulatedAdRewarded);

        /// <summary>«+20 → +40» para una Cosecha de <paramref name="seeds"/> (<c>double_harvest_delta</c>).</summary>
        public static string DoubleHarvestGain(int seeds)
        {
            return Format(Keys.DoubleHarvestDelta, LocArgs.Of("n", NonNegative(seeds)).And("doubled", NonNegative(seeds * 2)));
        }

        /// <summary>«Nivel {n}» (<c>level_n</c>).</summary>
        public static string Level(int level)
        {
            return Format(Keys.LevelN, LocArgs.OfDigits("n", level));
        }

        /// <summary>«Nivel {n} · intento {k}» (02 §2, INTRO del reintento; <c>level_attempt</c>) [P].</summary>
        public static string LevelAttempt(int level, int attempt)
        {
            return Format(Keys.LevelAttempt, LocArgs.OfDigits("n", level).AndDigits("attempt", attempt < 1 ? 1 : attempt));
        }

        /// <summary>«Jugar · Nivel {n}» (<c>play_level</c>; 06 §4.7; D-008 punto 6): una plantilla, sin pegar trozos.</summary>
        public static string PlayLevel(int level)
        {
            return Format(Keys.PlayLevel, LocArgs.OfDigits("n", level));
        }

        /// <summary>«Te faltan {n} semillas» (<c>stuck_rescue_missing</c>; con 1, «Te falta 1 semilla»).</summary>
        public static string MissingSeeds(int n)
        {
            return Format(Keys.StuckRescueMissing, LocArgs.Of("n", NonNegative(n)));
        }

        /// <summary>
        /// «Te faltan {n}» del precio en rojo de la bandeja (02 §5; 04 §1.1; <c>tray_missing</c> [P]), con separador de miles y
        /// plural (con 1, «Te falta 1»).
        /// </summary>
        public static string TrayMissing(int n)
        {
            return Format(Keys.TrayMissing, LocArgs.Of("n", NonNegative(n)));
        }

        /// <summary>«Perderás {n} canoa(s)» (<c>giveup_confirm_canoe</c>, con plural).</summary>
        public static string LoseCanoes(int n)
        {
            return Format(Keys.GiveUpConfirmCanoe, LocArgs.OfDigits("n", n));
        }

        /// <summary>
        /// «Premio: {n} semillas» de Nivel fallido (<c>level_failed_reward</c> [P]; 04 §6.6), con separador de miles y plural.
        /// </summary>
        public static string LevelFailedReward(int seeds)
        {
            return Format(Keys.LevelFailedReward, LocArgs.Of("n", NonNegative(seeds)));
        }

        /// <summary>
        /// Sin canoas: «Llenar canoas · {n}/{n}» con <paramref name="max"/> = <c>canoes_max</c>; la etiqueta de precio va al lado
        /// (04 §7.3: «Llenar canoas · 200 (S) 5/5»).
        /// </summary>
        public static string NoCanoesRefill(int max)
        {
            return Format(Keys.NoCanoesRefill, LocArgs.OfDigits("n", max));
        }

        /// <summary>
        /// Sin canoas: «Canoas infinitas 1 h · {precio}» (04 §7.3; <c>af_infinite_canoes_1h</c>), con el precio de la tienda tal
        /// cual.
        /// </summary>
        public static string NoCanoesInfinite(string price)
        {
            return Format(Keys.NoCanoesInfinite, LocArgs.Of("price", price ?? string.Empty));
        }

        /// <summary>
        /// Recarga de Marea por video en Aguas estancadas: «+{n} Deshacer», salto de línea y «Ver video»
        /// (<c>stuck_undo_refill_ad</c>; el salto va en la cadena, nunca un <c>Replace</c>).
        /// </summary>
        public static string StuckRefillVideo(int amount)
        {
            return Format(Keys.StuckUndoRefillAd, LocArgs.OfDigits("n", amount));
        }

        /// <summary>
        /// Recarga de Marea por semillas: «+{n} Deshacer · {price} semillas», o en dos líneas (<paramref name="twoLines"/>,
        /// la fila de dos botones de Aguas estancadas) con el salto en el <c>{sep}</c> de la plantilla
        /// (<c>stuck_undo_refill_seeds</c>).
        /// </summary>
        public static string StuckRefillSeeds(int amount, int price, bool twoLines)
        {
            return Format(Keys.StuckUndoRefillSeeds, LocArgs.OfDigits("n", amount).AndDigits("price", price), twoLines ? 1 : 0);
        }

        /// <summary>
        /// Tarjeta del Puñado en Aguas estancadas: «Puñado de semillas», salto de línea y «{seeds} · {price}»
        /// (<c>stuck_pack_199</c>, con el salto en la cadena; <paramref name="price"/>, el de la tienda tal cual).
        /// </summary>
        public static string StuckPackLine(int seeds, string price)
        {
            return Format(Keys.StuckPack, LocArgs.Of("seeds", NonNegative(seeds)).And("price", price));
        }

        /// <summary>
        /// Tarjeta de la Balsa en Aguas estancadas: «Balsa de rescate», salto de línea y el precio de la tienda
        /// (<c>stuck_balsa_499</c>, con el salto en la cadena).
        /// </summary>
        public static string StuckBalsaLine(string price)
        {
            return Format(Keys.StuckBalsa, LocArgs.Of("price", price));
        }

        /// <summary>
        /// Rótulo del anuncio simulado: la ubicación (<paramref name="placementId"/>, su ID de 05 §6.3) y el formato
        /// (<c>simulated_ad_label</c>), en una sola plantilla, sin pegar trozos.
        /// </summary>
        public static string SimulatedAdLabel(string placementId, bool rewarded)
        {
            return Format(Keys.SimulatedAdLabel, LocArgs.Of("placement", placementId).And("format", rewarded ? SimulatedAdRewarded : SimulatedAdInterstitial));
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

        /// <summary>«×{n}»: cinta del premio multiplicado (propuesta §c.2; <c>ribbon</c>).</summary>
        public static string Ribbon(int multiplier)
        {
            return Format(Keys.Ribbon, LocArgs.OfDigits("n", multiplier));
        }

        /// <summary>«Premio: {n}» (propuesta §c.1; <c>reward_line</c>), con separador de miles.</summary>
        public static string RewardLine(int seeds)
        {
            return Format(Keys.RewardLine, LocArgs.Of("n", NonNegative(seeds)));
        }

        /// <summary>Texto de «El río sigue creciendo» con los días estimados (<c>content_end_eta</c>; con 1, «en 1 día»).</summary>
        public static string ContentEnd(int etaDays)
        {
            return Format(Keys.ContentEndEta, LocArgs.OfDigits("n", etaDays));
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

        /// <summary>
        /// Línea de la tarjeta «¡Miquito a la vista!» (≤ 50 caracteres, con un dato verificado; guion de la intro §4.5) para la
        /// clave <c>dato_tarjeta</c> del catálogo de fauna (<c>fauna_&lt;id&gt;_dato</c>), de la tabla de textos largos
        /// (<c>facts.csv</c>; <see cref="Loc.Fact(string)"/>): cadena vacía si no hay, si falta en el idioma o si no está
        /// aprobada en un build de release. Todas pendientes del asesor cultural (07 §8.2); en las especies amenazadas, una
        /// buena noticia de conservación, nunca la etiqueta UICN. Sin asignaciones.
        /// </summary>
        public static string FaunaFact(string key)
        {
            return key == null ? string.Empty : Loc.Fact(key);
        }

        /// <summary>Claves <c>fauna_&lt;id&gt;_dato</c> de la tabla de textos largos, en su orden (tests).</summary>
        public static IEnumerable<string> FaunaFactKeys
        {
            get
            {
                LocTable facts = Loc.Catalog.Facts;
                var keys = new List<string>();
                for (int r = 0; r < facts.Count; r++)
                {
                    string key = facts.KeyAt(r);
                    if (key.StartsWith("fauna_", StringComparison.Ordinal) && key.EndsWith("_dato", StringComparison.Ordinal))
                    {
                        keys.Add(key);
                    }
                }

                return keys;
            }
        }

        /// <summary>
        /// Nombre local de la especie de la tarjeta «¡Miquito a la vista!» para su clave <c>fauna.&lt;id&gt;.name</c>
        /// (<c>FaunaSpecies.NameKey</c>), de la tabla de textos largos (<c>facts.csv</c>; <see cref="Loc.Fact(string)"/>; tarea
        /// FX5): en el idioma de la UI y, en en-XA, pseudolocalizado como todo texto de las tablas. Cadena vacía si no hay, si
        /// falta en el idioma o si no está aprobado en un build de release, como <see cref="FaunaFact"/>. Pendientes del asesor
        /// cultural (07 §8.2). Sin asignaciones.
        /// </summary>
        public static string FaunaName(string key)
        {
            return key == null ? string.Empty : Loc.Fact(key);
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

        /// <summary>
        /// Texto de UI de <paramref name="key"/> con <paramref name="args"/> (<see cref="Loc.T(string, in LocArgs, string, int)"/>):
        /// plurales del idioma de <see cref="Loc"/> y cifras con los separadores de su región. El <c>{sep}</c> número i es un
        /// salto de línea si el bit i de <paramref name="breaks"/> está puesto y, si no, <see cref="Separator"/>. Asigna la cadena:
        /// solo al construir o colocar la pantalla, nunca por fotograma.
        /// </summary>
        private static string Format(string key, in LocArgs args, int breaks = 0)
        {
            return Loc.T(key, args, Separator, breaks);
        }

        // Las cantidades de la UI nunca son negativas: como antes, un negativo se escribe 0.
        private static long NonNegative(int value)
        {
            return value < 0 ? 0L : value;
        }

        private static void ClearCaches()
        {
            s_loseOneCanoe = null;
            s_quitOneCanoe = null;
            ClearWorldCaches();
        }

        /// <summary>
        /// Claves de <c>strings.csv</c> que usa <see cref="UiText"/> (snake_case, iguales con las dos opciones de la pregunta 49;
        /// glosario §10 y GDD 04 §6.5 o derivadas [P]). Una clave nunca se reutiliza para otro sentido ni se renombra. Las de los
        /// mundos son <see cref="WorldNameKey"/>, <see cref="WorldCountryKey"/> y <see cref="WorldRiverKey"/>; las de la fauna,
        /// las <c>dato_tarjeta</c> del catálogo (en <c>facts.csv</c>).
        /// </summary>
        public static partial class Keys
        {
            /// <summary><c>back</c>.</summary>
            public const string Back = "back";

            /// <summary><c>back_home</c> [P].</summary>
            public const string BackHome = "back_home";

            /// <summary><c>buy</c>.</summary>
            public const string Buy = "buy";

            /// <summary><c>cancel</c> [P].</summary>
            public const string Cancel = "cancel";

            /// <summary><c>content_end_eta</c> [P].</summary>
            public const string ContentEndEta = "content_end_eta";

            /// <summary><c>content_end_title</c>.</summary>
            public const string ContentEndTitle = "content_end_title";

            /// <summary><c>currency</c>.</summary>
            public const string Currency = "currency";

            /// <summary><c>difficulty_hard</c>.</summary>
            public const string DifficultyHard = "difficulty_hard";

            /// <summary><c>difficulty_subtitle_hard</c> [P].</summary>
            public const string DifficultySubtitleHard = "difficulty_subtitle_hard";

            /// <summary><c>difficulty_subtitle_very_hard</c> [P].</summary>
            public const string DifficultySubtitleVeryHard = "difficulty_subtitle_very_hard";

            /// <summary><c>double_harvest</c>.</summary>
            public const string DoubleHarvest = "double_harvest";

            /// <summary><c>double_harvest_delta</c> [P].</summary>
            public const string DoubleHarvestDelta = "double_harvest_delta";

            /// <summary><c>extra_cuia</c>.</summary>
            public const string ExtraCuia = "extra_cuia";

            /// <summary><c>extra_cuia_gift</c> [P].</summary>
            public const string ExtraCuiaGift = "extra_cuia_gift";

            /// <summary><c>fauna_sighted_card</c> [P].</summary>
            public const string FaunaSightedCard = "fauna_sighted_card";

            /// <summary><c>firefly_gift</c> [P].</summary>
            public const string FireflyGift = "firefly_gift";

            /// <summary><c>firefly_line</c> [P].</summary>
            public const string FireflyLine = "firefly_line";

            /// <summary><c>giveup_confirm_canoe</c> [P].</summary>
            public const string GiveUpConfirmCanoe = "giveup_confirm_canoe";

            /// <summary><c>giveup_confirm_last_canoe</c> [P].</summary>
            public const string GiveUpConfirmLastCanoe = "giveup_confirm_last_canoe";

            /// <summary><c>giveup_confirm_title</c> [P].</summary>
            public const string GiveUpConfirmTitle = "giveup_confirm_title";

            /// <summary><c>harvest</c>.</summary>
            public const string Harvest = "harvest";

            /// <summary><c>home</c> [P].</summary>
            public const string Home = "home";

            /// <summary><c>keep_playing</c> [P].</summary>
            public const string KeepPlaying = "keep_playing";

            /// <summary><c>level_attempt</c> [P].</summary>
            public const string LevelAttempt = "level_attempt";

            /// <summary><c>level_complete</c>.</summary>
            public const string LevelComplete = "level_complete";

            /// <summary><c>level_failed</c> [P].</summary>
            public const string LevelFailed = "level_failed";

            /// <summary><c>level_failed_reward</c> [P].</summary>
            public const string LevelFailedReward = "level_failed_reward";

            /// <summary><c>level_n</c>.</summary>
            public const string LevelN = "level_n";

            /// <summary><c>lives</c>.</summary>
            public const string Lives = "lives";

            /// <summary><c>next</c>.</summary>
            public const string Next = "next";

            /// <summary><c>no_canoes_infinite</c> [P].</summary>
            public const string NoCanoesInfinite = "no_canoes_infinite";

            /// <summary><c>no_canoes_next</c> [P].</summary>
            public const string NoCanoesNext = "no_canoes_next";

            /// <summary><c>no_canoes_refill</c> [P].</summary>
            public const string NoCanoesRefill = "no_canoes_refill";

            /// <summary><c>no_canoes_title</c> [P].</summary>
            public const string NoCanoesTitle = "no_canoes_title";

            /// <summary><c>no_canoes_video</c> [P].</summary>
            public const string NoCanoesVideo = "no_canoes_video";

            /// <summary><c>pause</c>.</summary>
            public const string Pause = "pause";

            /// <summary><c>play</c>.</summary>
            public const string Play = "play";

            /// <summary><c>play_level</c> [P].</summary>
            public const string PlayLevel = "play_level";

            /// <summary><c>profile_locked_migration</c> [P].</summary>
            public const string ProfileLockedMigration = "profile_locked_migration";

            /// <summary><c>profile_locked_play</c> [P].</summary>
            public const string ProfileLockedPlay = "profile_locked_play";

            /// <summary><c>profile_locked_title</c> [P].</summary>
            public const string ProfileLockedTitle = "profile_locked_title";

            /// <summary><c>profile_locked_too_new</c> [P].</summary>
            public const string ProfileLockedTooNew = "profile_locked_too_new";

            /// <summary><c>quit</c> [P].</summary>
            public const string Quit = "quit";

            /// <summary><c>quit_confirm_canoe</c> [P]: segunda parte de <c>quit_confirm</c>.</summary>
            public const string QuitConfirmCanoe = "quit_confirm_canoe";

            /// <summary><c>quit_confirm_title</c> [P]: primera parte de <c>quit_confirm</c>.</summary>
            public const string QuitConfirmTitle = "quit_confirm_title";

            /// <summary><c>rescue_applied</c>.</summary>
            public const string RescueApplied = "rescue_applied";

            /// <summary><c>restart</c>.</summary>
            public const string Restart = "restart";

            /// <summary><c>restart_confirm_body</c> [P].</summary>
            public const string RestartConfirmBody = "restart_confirm_body";

            /// <summary><c>restart_confirm_title</c> [P].</summary>
            public const string RestartConfirmTitle = "restart_confirm_title";

            /// <summary><c>restart_long</c> [P].</summary>
            public const string RestartLong = "restart_long";

            /// <summary><c>retry</c> [P].</summary>
            public const string Retry = "retry";

            /// <summary><c>reveal</c>.</summary>
            public const string Reveal = "reveal";

            /// <summary><c>reveal_gift_toast</c> [P].</summary>
            public const string RevealGiftToast = "reveal_gift_toast";

            /// <summary><c>reward_line</c> [P].</summary>
            public const string RewardLine = "reward_line";

            /// <summary><c>ribbon</c> [P].</summary>
            public const string Ribbon = "ribbon";

            /// <summary><c>river_lowered_color</c>.</summary>
            public const string RiverLoweredColor = "river_lowered_color";

            /// <summary><c>river_lowered_cuia</c>.</summary>
            public const string RiverLoweredCuia = "river_lowered_cuia";

            /// <summary><c>settings</c>.</summary>
            public const string Settings = "settings";

            /// <summary><c>simulated_ad_close</c> [P].</summary>
            public const string SimulatedAdClose = "simulated_ad_close";

            /// <summary><c>simulated_ad_interstitial</c> [P].</summary>
            public const string SimulatedAdInterstitial = "simulated_ad_interstitial";

            /// <summary><c>simulated_ad_label</c> [P].</summary>
            public const string SimulatedAdLabel = "simulated_ad_label";

            /// <summary><c>simulated_ad_rewarded</c> [P].</summary>
            public const string SimulatedAdRewarded = "simulated_ad_rewarded";

            /// <summary><c>stuck_balsa_499</c>, primera parte.</summary>
            public const string StuckBalsa = "stuck_balsa_499";

            /// <summary><c>stuck_balsa_499_content</c> [P]: segunda parte de <c>stuck_balsa_499</c>.</summary>
            public const string StuckBalsaContent = "stuck_balsa_499_content";

            /// <summary><c>stuck_canoe_lost</c>.</summary>
            public const string StuckCanoeLost = "stuck_canoe_lost";

            /// <summary><c>stuck_canoe_lost_last</c>.</summary>
            public const string StuckCanoeLostLast = "stuck_canoe_lost_last";

            /// <summary><c>stuck_give_up</c> [P].</summary>
            public const string StuckGiveUp = "stuck_give_up";

            /// <summary><c>stuck_pack_199</c>.</summary>
            public const string StuckPack = "stuck_pack_199";

            /// <summary><c>stuck_rescue_cta</c>.</summary>
            public const string StuckRescueCta = "stuck_rescue_cta";

            /// <summary><c>stuck_rescue_discount</c>.</summary>
            public const string StuckRescueDiscount = "stuck_rescue_discount";

            /// <summary><c>stuck_rescue_missing</c>.</summary>
            public const string StuckRescueMissing = "stuck_rescue_missing";

            /// <summary><c>stuck_restart</c>.</summary>
            public const string StuckRestart = "stuck_restart";

            /// <summary><c>stuck_subtitle</c>.</summary>
            public const string StuckSubtitle = "stuck_subtitle";

            /// <summary><c>stuck_title</c>.</summary>
            public const string StuckTitle = "stuck_title";

            /// <summary><c>stuck_undo</c>.</summary>
            public const string StuckUndo = "stuck_undo";

            /// <summary><c>stuck_undo_gift</c> [P].</summary>
            public const string StuckUndoGift = "stuck_undo_gift";

            /// <summary><c>stuck_undo_refill_ad</c>.</summary>
            public const string StuckUndoRefillAd = "stuck_undo_refill_ad";

            /// <summary><c>stuck_undo_refill_seeds</c>.</summary>
            public const string StuckUndoRefillSeeds = "stuck_undo_refill_seeds";

            /// <summary><c>stuck_unsolvable</c>.</summary>
            public const string StuckUnsolvable = "stuck_unsolvable";

            /// <summary><c>tray_missing</c> [P].</summary>
            public const string TrayMissing = "tray_missing";

            /// <summary><c>tutorial_alta</c> [P].</summary>
            public const string TutorialAlta = "tutorial_alta";

            /// <summary><c>tutorial_barro</c> [P].</summary>
            public const string TutorialBarro = "tutorial_barro";

            /// <summary><c>tutorial_brote</c> [P].</summary>
            public const string TutorialBrote = "tutorial_brote";

            /// <summary><c>tutorial_chica</c> [P].</summary>
            public const string TutorialChica = "tutorial_chica";

            /// <summary><c>tutorial_hoja</c> [P].</summary>
            public const string TutorialHoja = "tutorial_hoja";

            /// <summary><c>tutorial_liana</c> [P].</summary>
            public const string TutorialLiana = "tutorial_liana";

            /// <summary><c>tutorial_lluvia</c> [P].</summary>
            public const string TutorialLluvia = "tutorial_lluvia";

            /// <summary><c>tutorial_matapi</c> [P].</summary>
            public const string TutorialMatapi = "tutorial_matapi";

            /// <summary><c>tutorial_murky</c> [P].</summary>
            public const string TutorialMurky = "tutorial_murky";

            /// <summary><c>tutorial_murky_rule</c> [P].</summary>
            public const string TutorialMurkyRule = "tutorial_murky_rule";

            /// <summary><c>tutorial_pirana</c> [P].</summary>
            public const string TutorialPirana = "tutorial_pirana";

            /// <summary><c>tutorial_remolino</c> [P].</summary>
            public const string TutorialRemolino = "tutorial_remolino";

            /// <summary><c>undo</c>.</summary>
            public const string Undo = "undo";

            /// <summary><c>very_hard</c>.</summary>
            public const string VeryHard = "very_hard";

            /// <summary><c>world_banner</c> (forma partida de L1 [P]: cabecera y lugar).</summary>
            public const string WorldBanner = "world_banner";

            /// <summary><c>world_heading</c> [P].</summary>
            public const string WorldHeading = "world_heading";

            /// <summary><c>world_place</c> [P].</summary>
            public const string WorldPlace = "world_place";

            /// <summary><c>world_place_named</c> [P].</summary>
            public const string WorldPlaceNamed = "world_place_named";

            /// <summary><c>world_place_named_rich</c> [P].</summary>
            public const string WorldPlaceNamedRich = "world_place_named_rich";

            /// <summary><c>world_title</c> [P].</summary>
            public const string WorldTitle = "world_title";
        }
    }
}
