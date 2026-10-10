#nullable enable
using System.Globalization;
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 del prototipo v0 con su clave del glosario (§5.1, §10) y de GDD 04 §6.5. La localización
    /// (Unity Localization, ES-419 / PT-BR / EN) queda fuera de v0: cuando llegue, estas constantes pasan a ser las
    /// claves de las String Tables. Clase parcial (arquitectura del paso 1, §9): los textos de la tarjeta previa y de la
    /// Luciérnaga van en sus propios archivos (UiText.Cards.cs y UiText.Reveal.cs).
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>level_n</c>.</summary>
        public const string LevelN = "Nivel {0}";

        /// <summary>
        /// <c>fauna_sighted_card</c> [P] (guion de la intro §4.5, Q10 A): «avistamiento», nunca «nuevo» ni «tu mono». Pendiente
        /// del dueño (Q10) y del asesor cultural (07 §8.2); EN «Monkey spotted!».
        /// </summary>
        public const string FaunaSightedCard = "¡Miquito a la vista!";

        /// <summary>
        /// «Nivel {n} · intento {k}» [P]: rótulo de la intro corta del reintento (02 §2, INTRO: «En reintento muestra Nivel N ·
        /// intento k»), con k = <c>fails_level</c> + 1. Sin clave en el glosario §10: pendiente con los textos de P14.
        /// </summary>
        public const string LevelAttemptN = "Nivel {0} · intento {1}";

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
        public const string RibbonFormat = "×{0}";

        /// <summary>Premio de la tarjeta previa (propuesta §c.1: «Premio: 60»; <c>eco_reward_*</c>, 08 §9).</summary>
        public const string RewardLineFormat = "Premio: {0}";

        /// <summary>Título de la pantalla de final de contenido (06 §9; 08 §6.2).</summary>
        public const string ContentEndTitle = "El río sigue creciendo";

        /// <summary>
        /// Texto [P] de «El río sigue creciendo» con <c>live_content_end_message.next_batch_eta_days</c> (06 §9: «fecha estimada
        /// de los próximos 100 niveles»), redactado sobre el push «Nuevos niveles» de 06 §8.3. Pendiente de aprobación (P14).
        /// </summary>
        public const string ContentEndEta = "Río arriba llegan 100 niveles nuevos en unos {0} días.";

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

        /// <summary><c>stuck_rescue_missing</c>.</summary>
        public const string StuckRescueMissing = "Te faltan {0} semillas";

        /// <summary>«Te faltan {n}» del precio en rojo (04 §1.1; bandeja).</summary>
        public const string Missing = "Te faltan {0}";

        /// <summary><c>stuck_undo_refill_ad</c> (la cifra es <c>undo_refill_amount</c>).</summary>
        public const string StuckUndoRefillAd = "+{0} Deshacer · Ver video";

        /// <summary><c>stuck_undo_refill_seeds</c> (cifras: <c>undo_refill_amount</c> y <c>undo_refill_seed_price</c>).</summary>
        public const string StuckUndoRefillSeeds = "+{0} Deshacer · {1} semillas";

        /// <summary>Recarga gratis de +1 Marea de la rama F [P] (04 §6.3; sin clave en 04 §6.5).</summary>
        public const string StuckUndoGift = "+1 Deshacer · gratis";

        /// <summary><c>stuck_pack_199</c>: «Puñado de semillas · 1.000 · {precio_local}».</summary>
        public const string StuckPack = "Puñado de semillas · {0} · {1}";

        /// <summary><c>stuck_balsa_499</c>, primera parte: «Balsa de rescate · {precio_local}».</summary>
        public const string StuckBalsa = "Balsa de rescate · {0}";

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

        /// <summary><c>giveup_confirm_canoe</c> [P].</summary>
        public const string GiveUpConfirmCanoe = "Perderás 1 canoa";

        /// <summary>«Perderás tu última canoa» [P] (D-008, paso 2; sin clave propia en 04 §6.5).</summary>
        public const string GiveUpConfirmLastCanoe = "Perderás tu última canoa";

        /// <summary><c>level_failed</c> [P].</summary>
        public const string LevelFailed = "Nivel fallido";

        /// <summary><c>level_failed_reward</c> [P].</summary>
        public const string LevelFailedReward = "Premio: {0} semillas";

        /// <summary><c>retry</c> [P].</summary>
        public const string Retry = "Reintentar";

        /// <summary><c>quit_confirm</c> [P], primera parte (título del diálogo).</summary>
        public const string QuitConfirmTitle = "¿Salir del nivel?";

        /// <summary><c>quit_confirm</c> [P], segunda parte.</summary>
        public const string QuitConfirmCanoe = "Perderás 1 canoa";

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
        /// «Llenar canoas · 200 (S) 5/5»).
        /// </summary>
        public const string NoCanoesRefill = "Llenar canoas · {0}/{0}";

        /// <summary>Sin canoas: «Canoas infinitas 1 h · {precio}» (04 §7.3; <c>af_infinite_canoes_1h</c>).</summary>
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
        public const string DoubleHarvestDelta = "+{0} → +{1}";

        /// <summary>Formato del anuncio simulado en su pantalla (D-008 punto 4).</summary>
        public const string SimulatedAdInterstitial = "intersticial";

        /// <summary>Formato del anuncio simulado recompensado.</summary>
        public const string SimulatedAdRewarded = "recompensado";

        /// <summary>«+20 → +40» para una Cosecha de <paramref name="seeds"/>.</summary>
        public static string DoubleHarvestGain(int seeds)
        {
            return string.Format(CultureInfo.InvariantCulture, DoubleHarvestDelta, Grouped(seeds), Grouped(seeds * 2));
        }

        /// <summary>"Nivel {n}".</summary>
        public static string Level(int level)
        {
            return string.Format(CultureInfo.InvariantCulture, LevelN, level);
        }

        /// <summary>«Nivel {n} · intento {k}» (02 §2, INTRO del reintento) [P].</summary>
        public static string LevelAttempt(int level, int attempt)
        {
            return string.Format(CultureInfo.InvariantCulture, LevelAttemptN, level, attempt < 1 ? 1 : attempt);
        }

        /// <summary>«Jugar · Nivel {n}» (<c>play</c> + <c>level_n</c>; 06 §4.7; D-008 punto 6).</summary>
        public static string PlayLevel(int level)
        {
            return Play + " · " + Level(level);
        }

        /// <summary>«Te faltan {n} semillas».</summary>
        public static string MissingSeeds(int n)
        {
            return string.Format(CultureInfo.InvariantCulture, StuckRescueMissing, Grouped(n));
        }

        /// <summary>Número con separador de miles «.» (glosario §10: «1.000»).</summary>
        public static string Grouped(int value)
        {
            var buffer = new char[16];
            int n = WriteNumber(buffer, 0, value, true);
            return new string(buffer, 0, n);
        }

        /// <summary>Etiqueta del chip de dificultad para <c>difficulty.label</c>, o null si es <c>normal</c>.</summary>
        public static string? DifficultyChip(string label)
        {
            switch (label)
            {
                case "hard": return DifficultyHard;
                case "very_hard": return DifficultyVeryHard;
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
                case "hard": return DifficultySubtitleHard;
                case "very_hard": return DifficultySubtitleVeryHard;
                default: return null;
            }
        }

        /// <summary>«×{n}»: cinta del premio multiplicado (propuesta §c.2).</summary>
        public static string Ribbon(int multiplier)
        {
            return string.Format(CultureInfo.InvariantCulture, RibbonFormat, multiplier);
        }

        /// <summary>«Premio: {n}» (propuesta §c.1), con separador de miles.</summary>
        public static string RewardLine(int seeds)
        {
            return string.Format(CultureInfo.InvariantCulture, RewardLineFormat, Grouped(seeds));
        }

        /// <summary>Texto de «El río sigue creciendo» con los días estimados.</summary>
        public static string ContentEnd(int etaDays)
        {
            return string.Format(CultureInfo.InvariantCulture, ContentEndEta, etaDays);
        }

        /// <summary>
        /// Escribe <paramref name="value"/> (≥ 0) en <paramref name="buffer"/> desde <paramref name="pos"/>, con separador de
        /// miles «.» si <paramref name="grouped"/> (glosario §10: «1.000»). Devuelve la posición siguiente. Sin asignaciones:
        /// los contadores del inicio lo usan con <c>TMP_Text.SetCharArray</c>.
        /// </summary>
        public static int WriteNumber(char[] buffer, int pos, int value, bool grouped)
        {
            if (value < 0)
            {
                value = 0;
            }

            int digits = 1;
            for (int v = value; v >= 10; v /= 10)
            {
                digits++;
            }

            int length = grouped ? digits + ((digits - 1) / 3) : digits;
            int end = pos + length;
            int at = end - 1;
            int written = 0;
            do
            {
                if (grouped && written > 0 && written % 3 == 0)
                {
                    buffer[at--] = '.';
                }

                buffer[at--] = (char)('0' + (value % 10));
                value /= 10;
                written++;
            }
            while (value > 0);

            return end;
        }

        /// <summary>
        /// Línea de la tarjeta «¡Miquito a la vista!» (≤ 50 caracteres, con un dato verificado; guion de la intro §4.5) para la
        /// clave <c>dato_tarjeta</c> del catálogo de fauna; cadena vacía si no hay. Todas pendientes del asesor cultural
        /// (07 §8.2); en las especies amenazadas, una buena noticia de conservación, nunca la etiqueta UICN.
        /// </summary>
        public static string FaunaFact(string key)
        {
            switch (key)
            {
                case "fauna_saimiri_collinsi_dato":
                    return "Cruza el bosque en fila, por ramas finas";
                case "fauna_mico_argentatus_dato":
                    return "Vive en Alter do Chão, junto al Tapajós";
                case "fauna_saguinus_bicolor_dato":
                    return "Manaos tiende puentes de cuerda para él";
                case "fauna_lagothrix_lagothricha_dato":
                    return "El mono más grande de Amacayacu";
                case "fauna_cacajao_calvus_ucayalii_dato":
                    return "Su cara roja es señal de buena salud";
                case "fauna_plecturocebus_discolor_dato":
                    return "Las parejas cantan a dúo al amanecer";
                case "fauna_saguinus_imperator_dato":
                    return "Su bigote blanco pasa de los hombros";
                case "fauna_ateles_chamek_dato":
                    return "Siembra la selva: dispersa muchas semillas";
                case "fauna_aotus_trivirgatus_dato":
                    return "El papá carga a la cría";
                case "fauna_pithecia_pithecia_dato":
                    return "Casi siempre se mueve a saltos";
                case "fauna_saimiri_sciureus_albigena_dato":
                    return "Vecinos del Orinoco";
                default:
                    return string.Empty;
            }
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
