#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Qué gesto y qué frase de TiTino tocan en cada momento (D-016, T5-B y T6-A, puntos 4 y 5; propuesta de TiTino d.4-d.7,
    /// d.10 y e.4; 02 §6, fila TiTino; 08 §9, <c>ui_sloth_*</c>; plan (i), paso 3). Sin motor, determinista y sin asignaciones:
    /// devuelve claves de <c>strings.csv</c>, nunca texto, y la misma entrada da siempre el mismo resultado. Quien llama apunta el
    /// resultado con <see cref="SlothSession.Record"/>.
    /// <para><b>Victoria</b> (<see cref="ForVictory"/>): gesto siempre que TiTino está encendido (<c>sloth_cheer_big</c> en sus
    /// hitos); frase segura en sus tres hitos si el árbitro de la línea se la da (<see cref="VictoryLine.SlothSpeaks"/>; para
    /// pedírsela, <see cref="HitoVoice"/>); en el resto, 1 de cada <c>ui_sloth_win_line_every_n</c> y solo con la ranura de la
    /// línea vacía, del banco más específico (hard &gt; debut &gt; streak &gt; win).</para>
    /// <para><b>Topes, en el orden de d.6, punto 5:</b> como mucho <c>ui_sloth_lines_per_session_max</c> frases por sesión &gt;
    /// nunca frase en dos pantallas seguidas &gt; una sola línea de texto por victoria &gt; hito o 1 de cada N. Única excepción:
    /// el fin de mundo (salvo el 10) habla siempre. En todos los momentos, nunca la misma frase que la última vista del mismo
    /// momento (<c>sloth.last_line</c>) cuando el banco da otra. La frase fija de un estreno que repetiría la última del momento
    /// cede al banco siguiente (streak &gt; win). Las frases fijas sin alternativa (la del fin de mundo, la del primer Muy
    /// difícil, el saludo de la mañana y la n.º 21) se repiten [P] (pendiente del dueño).</para>
    /// <para><b>Fallo, reinicio e inicio:</b> <see cref="ForFail"/>, <see cref="ForRestartNotice"/> y <see cref="ForHome"/>.
    /// Con <c>settings.sloth_messages</c> apagado, solo gestos; con <c>ui_sloth_enabled</c> apagado, nada.</para>
    /// </summary>
    public static class SlothLines
    {
        /// <summary>Prefijo de las claves de las frases (glosario §10).</summary>
        public const string KeyPrefix = "sloth_line_";

        /// <summary>
        /// Clave del dato del perezoso que necesita la frase n.º 3 (<c>sloth_line_win_03</c>): solo se dice con él aprobado
        /// (propuesta de TiTino e.4, notas; regla 8 de e.2).
        /// </summary>
        public const string ColorFactKey = "fauna.bradypus_variegatus.dato_11";

        /// <summary>Último nivel con el aviso de Volver a la orilla (04 §2.7; D-016, T5-B).</summary>
        public const int RestartNoticeLastLevel = 30;

        /// <summary>Crecida mínima del banco <c>streak</c> fuera de hito (d.6, punto 2).</summary>
        public const int StreakFrom = 4;

        /// <summary>Días sin abrir para el saludo de regreso (d.7).</summary>
        public const int ReturnAfterDays = 3;

        /// <summary>Hora local desde la que hay saludo de la mañana (06:00; d.7).</summary>
        public const int MorningFromHour = 6;

        /// <summary>Hora local hasta la que hay saludo de la mañana, sin incluirla (12:00; d.7).</summary>
        public const int MorningToHour = 12;

        /// <summary>Primer Muy difícil: su frase es <c>sloth_line_milestone_02</c> (e.4, n.º 30).</summary>
        public const int FirstVeryHardLevel = 60;

        // Índices fijos de los bancos (0 = _01), con su número de e.4.
        private const int WinColorIndex = 2;            // n.º 3, solo con el dato_11 aprobado
        private const int WinPlannedIndex = 3;          // n.º 4, nunca tras Cuia de rescate ni Morpho
        private const int WinFinalPourIndex = 7;        // n.º 8, ídem
        private const int StreakNamedIndex = 0;         // n.º 18, «¡{n} seguidas!»
        private const int StreakHighIndex = 2;          // n.º 20, Crecida de 7 a 9
        private const int StreakBrokenIndex = 3;        // n.º 21, la Crecida se vació
        private const int ReturnLevelIndex = 1;         // n.º 23, «nivel {n}»
        private const int ReturnMorningIndex = 3;       // n.º 25, buenos días
        private const int MilestoneWorldIndex = 0;      // n.º 29, fin de mundo
        private const int MilestoneVeryHardIndex = 1;   // n.º 30, primer Muy difícil
        private const int StreakHighFrom = 7;
        private const int StreakHighTo = 9;

        private static readonly string[] WinKeys =
        {
            "sloth_line_win_01", "sloth_line_win_02", "sloth_line_win_03", "sloth_line_win_04",
            "sloth_line_win_05", "sloth_line_win_06", "sloth_line_win_07", "sloth_line_win_08",
        };

        private static readonly string[] HardKeys = { "sloth_line_hard_01", "sloth_line_hard_02" };

        private static readonly string[] FailKeys =
        {
            "sloth_line_fail_01", "sloth_line_fail_02", "sloth_line_fail_03", "sloth_line_fail_04", "sloth_line_fail_05",
        };

        private static readonly string[] RestartKeys = { "sloth_line_restart_01", "sloth_line_restart_02" };

        private static readonly string[] StreakKeys =
        {
            "sloth_line_streak_01", "sloth_line_streak_02", "sloth_line_streak_03", "sloth_line_streak_04",
        };

        private static readonly string[] ReturnKeys =
        {
            "sloth_line_return_01", "sloth_line_return_02", "sloth_line_return_03", "sloth_line_return_04",
        };

        private static readonly string[] DebutKeys = { "sloth_line_debut_01", "sloth_line_debut_02", "sloth_line_debut_03" };

        private static readonly string[] MilestoneKeys = { "sloth_line_milestone_01", "sloth_line_milestone_02" };

        // Bancos en el orden de SlothMoment (y de sloth.last_line).
        private static readonly string[][] Banks = { WinKeys, HardKeys, FailKeys, RestartKeys, StreakKeys, ReturnKeys, DebutKeys, MilestoneKeys };

        private static readonly string[] FactKeys =
        {
            "fauna.bradypus_variegatus.dato_01", "fauna.bradypus_variegatus.dato_02", "fauna.bradypus_variegatus.dato_03",
            "fauna.bradypus_variegatus.dato_04", "fauna.bradypus_variegatus.dato_05", "fauna.bradypus_variegatus.dato_06",
            "fauna.bradypus_variegatus.dato_07", "fauna.bradypus_variegatus.dato_08", "fauna.bradypus_variegatus.dato_09",
            "fauna.bradypus_variegatus.dato_10", ColorFactKey,
        };

        /// <summary>Número de momentos (= <see cref="SlothData.MomentCount"/>).</summary>
        public static int MomentCount => Banks.Length;

        /// <summary>Número de frases del banco de <paramref name="moment"/> (win 8, hard 2, fail 5, restart 2, streak 4, return 4, debut 3, milestone 2).</summary>
        public static int BankSize(SlothMoment moment)
        {
            return Bank(moment).Length;
        }

        /// <summary>Clave de la frase <paramref name="index"/> (0 = <c>_01</c>) del banco de <paramref name="moment"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice no está en el banco.</exception>
        public static string KeyOf(SlothMoment moment, int index)
        {
            string[] bank = Bank(moment);
            if (index < 0 || index >= bank.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Frase fuera del banco " + moment + ".");
            }

            return bank[index];
        }

        /// <summary>Las 30 claves de las frases, banco a banco (tests y tabla de textos).</summary>
        public static IEnumerable<string> AllKeys
        {
            get
            {
                for (int m = 0; m < Banks.Length; m++)
                {
                    for (int i = 0; i < Banks[m].Length; i++)
                    {
                        yield return Banks[m][i];
                    }
                }
            }
        }

        /// <summary>
        /// Las 11 claves de los datos del perezoso en <c>facts.csv</c> (propuesta de TiTino g.3; pregunta 49, (a), D-022:
        /// <c>fauna.bradypus_variegatus.dato_&lt;nn&gt;</c>), en orden.
        /// </summary>
        public static IReadOnlyList<string> SlothFactKeys => FactKeys;

        /// <summary>
        /// El nivel es un fin de mundo en el que TiTino habla siempre, aunque se haya llegado al tope o la pantalla anterior
        /// tuviera frase (d.6, punto 5): el último de cada mundo salvo el 10.
        /// </summary>
        public static bool IsWorldEnd(int level)
        {
            if (level <= RiverStopCatalog.LevelsPerStop || level > WorldTable.LastLevel)
            {
                return false;
            }

            return WorldTable.LastLevelOf(WorldTable.WorldOf(level)) == level;
        }

        /// <summary>
        /// Si TiTino puede decir su frase de hito en la victoria de <paramref name="level"/>, sin contar la caja: encendido, con
        /// mensajes, el nivel es uno de sus hitos (<see cref="VictoryLineArbiter.IsSlothHito"/>) y, salvo en el fin de mundo, sin
        /// el tope de la sesión ni frase en la pantalla anterior. Quien llama le suma la caja de ≥ 104 dp y lo pasa como
        /// <see cref="VictoryContext.SlothCanSpeak"/>.
        /// </summary>
        public static bool HitoVoice(int level, in SlothContext c)
        {
            if (!c.Enabled || !c.Messages || level < 1 || level > WorldTable.LastLevel || !VictoryLineArbiter.IsSlothHito(level))
            {
                return false;
            }

            return IsWorldEnd(level) || UnderCaps(in c);
        }

        /// <summary>
        /// La victoria de <paramref name="level"/> con la línea <paramref name="line"/> que decidió el árbitro (véase el resumen de
        /// la clase). Lee <see cref="SlothContext.Victory"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static SlothLine ForVictory(int level, in VictoryLine line, in SlothContext c)
        {
            if (level < 1 || level > WorldTable.LastLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "La primera vuelta va del nivel 1 al 1.000 (glosario §8).");
            }

            if (!c.Enabled)
            {
                return Nothing(SlothScreen.Victory);
            }

            SlothGesture gesture = line.SlothBig ? SlothGesture.CheerBig : SlothGesture.Cheer;

            // Hito con la línea de TiTino: frase segura (el árbitro ya aplicó HitoVoice y la caja).
            if (line.SlothSpeaks)
            {
                if (!c.Messages)
                {
                    return GestureOnly(SlothScreen.Victory, gesture);
                }

                int milestone = level == FirstVeryHardLevel ? MilestoneVeryHardIndex : MilestoneWorldIndex;
                return Speak(SlothScreen.Victory, gesture, SlothMoment.Milestone, milestone, 0);
            }

            // Una sola línea de texto por victoria (T6-A): con la ranura ocupada (llegada, dato o fruta), solo el gesto. En el 10,
            // el Mapa (06 §2.3); en un hito sin su línea, victoria normal; en un hito de la Crecida, las nutrias (d.6, punto 3).
            SlothVictoryFacts v = c.Victory;
            if (level == RiverStopCatalog.LevelsPerStop || VictoryLineArbiter.IsSlothHito(level) || line.Arrival
                || line.ArrivalFactKey != null || line.FruitKey != null || v.StreakMilestone)
            {
                return GestureOnly(SlothScreen.Victory, gesture);
            }

            // 1 de cada N victorias.
            int n = c.WinLineEveryN;
            if (n <= 0 || v.WinCount <= 0 || v.WinCount % n != 0)
            {
                return GestureOnly(SlothScreen.Victory, gesture);
            }

            if (!c.Messages || !v.SpeakBox || !UnderCaps(in c))
            {
                return GestureOnly(SlothScreen.Victory, gesture);
            }

            // Banco más específico: hard > debut > streak > win (d.6, punto 2).
            int seed = level + v.WinCount;
            if (v.Hard)
            {
                return Pick(SlothScreen.Victory, gesture, SlothMoment.Hard, AllOf(HardKeys), seed, in c, 0);
            }

            // Cada tipo de estreno tiene su frase fija: si repetiría la última del momento, cede al banco siguiente (d.10).
            if (v.Debut != SlothDebut.None && c.LastLineOf(SlothMoment.Debut) != (int)v.Debut - 1)
            {
                return Speak(SlothScreen.Victory, gesture, SlothMoment.Debut, (int)v.Debut - 1, 0);
            }

            if (v.Streak >= StreakFrom)
            {
                uint streak = Bit(StreakNamedIndex) | Bit(1);
                if (v.Streak >= StreakHighFrom && v.Streak <= StreakHighTo)
                {
                    streak |= Bit(StreakHighIndex);
                }

                return Pick(SlothScreen.Victory, gesture, SlothMoment.Streak, streak, seed, in c, v.Streak);
            }

            uint win = AllOf(WinKeys);
            if (v.UsedRescue || v.UsedHint)
            {
                win &= ~(Bit(WinPlannedIndex) | Bit(WinFinalPourIndex));
            }

            if (!v.ColorFactApproved)
            {
                win &= ~Bit(WinColorIndex);
            }

            return Pick(SlothScreen.Victory, gesture, SlothMoment.Win, win, seed, in c, 0);
        }

        /// <summary>
        /// Nivel fallido de <paramref name="level"/> (d.5): <c>sloth_encourage</c> con <c>ui_sloth_fail_enabled</c>; frase solo en
        /// el primer fallo del nivel (con los topes) y, si el fallo rompió la Crecida, la n.º 21 (<c>sloth_line_streak_04</c>). Lee
        /// <see cref="SlothContext.Fail"/>.
        /// </summary>
        public static SlothLine ForFail(int level, in SlothContext c)
        {
            if (!c.Enabled || !c.FailEnabled)
            {
                return Nothing(SlothScreen.Fail);
            }

            SlothFailFacts f = c.Fail;
            if (!f.FirstFail || !c.Messages || !f.SpeakBox || !UnderCaps(in c))
            {
                return GestureOnly(SlothScreen.Fail, SlothGesture.Encourage);
            }

            if (f.StreakBroken)
            {
                return Speak(SlothScreen.Fail, SlothGesture.Encourage, SlothMoment.Streak, StreakBrokenIndex, 0);
            }

            return Pick(SlothScreen.Fail, SlothGesture.Encourage, SlothMoment.Fail, AllOf(FailKeys), NonNegative(level) + NonNegative(f.FailsHard), in c, 0);
        }

        /// <summary>
        /// Aviso tras Volver a la orilla en <paramref name="level"/> (D-016, T5-B; d.5): solo en 1-30, en el primer reinicio del
        /// nivel y si el nivel no tiene instrucción del tutorial; si no, no hay aviso (<see cref="SlothGesture.None"/>). Con aviso,
        /// <c>sloth_encourage</c> y una frase de <c>restart</c> (con los topes) o, si el reinicio rompió la Crecida (25-30), la
        /// n.º 21. Sin frase (mensajes apagados o topes), solo el gesto: quien enseña el aviso decide si sale sin ella. Lee
        /// <see cref="SlothContext.Restart"/>.
        /// </summary>
        public static SlothLine ForRestartNotice(int level, in SlothContext c)
        {
            SlothRestartFacts r = c.Restart;
            if (!c.Enabled || level < 1 || level > RestartNoticeLastLevel || !r.FirstRestart || r.TutorialInstruction)
            {
                return Nothing(SlothScreen.Restart);
            }

            if (!c.Messages || !UnderCaps(in c))
            {
                return GestureOnly(SlothScreen.Restart, SlothGesture.Encourage);
            }

            if (r.StreakBroken)
            {
                return Speak(SlothScreen.Restart, SlothGesture.Encourage, SlothMoment.Streak, StreakBrokenIndex, 0);
            }

            return Pick(SlothScreen.Restart, SlothGesture.Encourage, SlothMoment.Restart, AllOf(RestartKeys), level, in c, 0);
        }

        /// <summary>
        /// El inicio (d.7 y d.10): como mucho un saludo al día (<c>sloth.last_greeting_date</c>), con <c>sloth_wave</c>. Primero el
        /// de regreso, tras ≥ 3 días sin abrir (<c>sloth.last_seen_date</c>; frases 22-24); si no, en la primera apertura del día
        /// entre las 06:00 y las 12:00, el de la mañana (n.º 25), nunca el mismo día que el de regreso. Con los topes de la
        /// sesión; sin frase, solo el saludo con la mano. Lee <see cref="SlothContext.Home"/>.
        /// <para>«Primera apertura del día entre las 06:00 y las 12:00» (d.7 y d.10) se lee [P] (pendiente del dueño) como «la
        /// primera apertura del día, si cae entre las 06:00 y las 12:00»: se mira <c>sloth.last_seen_date</c>, que se apunta en
        /// cada inicio aunque no haya saludo, así que quien abre a las 05:30 y otra vez a las 07:00 del mismo día no tiene el de
        /// la mañana. La otra lectura («la primera apertura dentro de esa franja») se basaría en <c>sloth.last_greeting_date</c>.</para>
        /// </summary>
        public static SlothLine ForHome(in SlothContext c)
        {
            SlothHomeFacts h = c.Home;
            if (!c.Enabled || !TryDayNumber(h.Today, out int today))
            {
                return Nothing(SlothScreen.Home);
            }

            if (string.Equals(h.LastGreetingDate, h.Today, StringComparison.Ordinal))
            {
                return Nothing(SlothScreen.Home);
            }

            bool seen = TryDayNumber(h.LastSeenDate, out int lastSeen);
            bool returning = seen && today - lastSeen >= ReturnAfterDays;
            bool morning = (!seen || lastSeen != today) && h.Hour >= MorningFromHour && h.Hour < MorningToHour;
            if (!returning && !morning)
            {
                return Nothing(SlothScreen.Home);
            }

            if (!c.Messages || !UnderCaps(in c))
            {
                return new SlothLine(SlothScreen.Home, SlothGesture.Wave, SlothMoment.Return, SlothData.NoLine, null, 0, true);
            }

            if (!returning)
            {
                return new SlothLine(SlothScreen.Home, SlothGesture.Wave, SlothMoment.Return, ReturnMorningIndex, ReturnKeys[ReturnMorningIndex], 0, true);
            }

            uint back = Bit(0) | Bit(2);
            if (h.NextLevel > 0)
            {
                back |= Bit(ReturnLevelIndex);
            }

            SlothLine picked = Pick(SlothScreen.Home, SlothGesture.Wave, SlothMoment.Return, back, today, in c, 0);
            int arg = picked.Index == ReturnLevelIndex ? h.NextLevel : 0;
            return new SlothLine(SlothScreen.Home, SlothGesture.Wave, SlothMoment.Return, picked.Index, picked.Key, arg, true);
        }

        /// <summary>
        /// Número de día de una fecha <c>yyyy-MM-dd</c> (calendario gregoriano proléptico; 1970-01-01 = 0), sin asignaciones, o
        /// false si no es una fecha válida (<see cref="SlothData.IsDateKey"/>).
        /// </summary>
        public static bool TryDayNumber(string? dateKey, out int day)
        {
            day = 0;
            if (dateKey == null || dateKey.Length != 10 || dateKey[4] != '-' || dateKey[7] != '-')
            {
                return false;
            }

            for (int i = 0; i < 10; i++)
            {
                if (i != 4 && i != 7 && (dateKey[i] < '0' || dateKey[i] > '9'))
                {
                    return false;
                }
            }

            int y = Digits(dateKey, 0, 4);
            int m = Digits(dateKey, 5, 2);
            int d = Digits(dateKey, 8, 2);
            if (y < 1 || m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m))
            {
                return false;
            }

            // days_from_civil (H. Hinnant), en enteros.
            y -= m <= 2 ? 1 : 0;
            int era = (y >= 0 ? y : y - 399) / 400;
            int yoe = y - (era * 400);
            int doy = ((153 * (m + (m > 2 ? -3 : 9))) + 2) / 5 + d - 1;
            int doe = (yoe * 365) + (yoe / 4) - (yoe / 100) + doy;
            day = (era * 146097) + doe - 719468;
            return true;
        }

        // Tope de la sesión y nunca dos pantallas seguidas con frase (d.6, punto 5).
        private static bool UnderCaps(in SlothContext c)
        {
            return c.LinesThisSession < c.LinesPerSessionMax && !c.PreviousScreenSpoke;
        }

        // Elige una frase del conjunto mask (bits = índices del banco) que no sea la última vista del momento si hay otra; el
        // elegido es la posición seed (mod el número de candidatas) entre los bits puestos.
        private static SlothLine Pick(SlothScreen screen, SlothGesture gesture, SlothMoment moment, uint mask, int seed, in SlothContext c, int arg)
        {
            int last = c.LastLineOf(moment);
            if (last >= 0 && last < 32 && (mask & Bit(last)) != 0 && Count(mask) > 1)
            {
                mask &= ~Bit(last);
            }

            int count = Count(mask);
            if (count == 0)
            {
                return GestureOnly(screen, gesture);
            }

            int k = NonNegative(seed) % count;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & Bit(i)) == 0)
                {
                    continue;
                }

                if (k == 0)
                {
                    return Speak(screen, gesture, moment, i, i == StreakNamedIndex && moment == SlothMoment.Streak ? arg : 0);
                }

                k--;
            }

            return GestureOnly(screen, gesture);
        }

        private static SlothLine Speak(SlothScreen screen, SlothGesture gesture, SlothMoment moment, int index, int arg)
        {
            return new SlothLine(screen, gesture, moment, index, Bank(moment)[index], arg, false);
        }

        private static SlothLine GestureOnly(SlothScreen screen, SlothGesture gesture)
        {
            return new SlothLine(screen, gesture, SlothMoment.Win, SlothData.NoLine, null, 0, false);
        }

        private static SlothLine Nothing(SlothScreen screen)
        {
            return GestureOnly(screen, SlothGesture.None);
        }

        private static string[] Bank(SlothMoment moment)
        {
            int m = (int)moment;
            if (m < 0 || m >= Banks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(moment), moment, "Momento desconocido.");
            }

            return Banks[m];
        }

        private static uint Bit(int index)
        {
            return 1u << index;
        }

        private static uint AllOf(string[] bank)
        {
            return (1u << bank.Length) - 1u;
        }

        private static int Count(uint mask)
        {
            int n = 0;
            while (mask != 0)
            {
                mask &= mask - 1u;
                n++;
            }

            return n;
        }

        private static int NonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static int Digits(string s, int start, int length)
        {
            int v = 0;
            for (int i = start; i < start + length; i++)
            {
                v = (v * 10) + (s[i] - '0');
            }

            return v;
        }
    }
}
