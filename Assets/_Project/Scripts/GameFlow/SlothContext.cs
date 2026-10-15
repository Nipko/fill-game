#nullable enable
using System;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Momento de las frases de TiTino (glosario §10: <c>sloth_line_&lt;momento&gt;_&lt;nn&gt;</c>; D-016, punto 4), en el orden de los
    /// índices de <c>sloth.last_line</c> (<see cref="SlothData.Moments"/>): cambiarlo exige migrar el perfil.
    /// </summary>
    public enum SlothMoment : byte
    {
        /// <summary><c>win</c>: victoria normal (8 frases).</summary>
        Win = 0,

        /// <summary><c>hard</c>: victoria Difícil o Muy difícil (2).</summary>
        Hard = 1,

        /// <summary><c>fail</c>: Nivel fallido (5).</summary>
        Fail = 2,

        /// <summary><c>restart</c>: aviso tras Volver a la orilla (2).</summary>
        Restart = 3,

        /// <summary><c>streak</c>: Crecida de 4 o más fuera de hito y la de consuelo cuando se rompe (4).</summary>
        Streak = 4,

        /// <summary><c>return</c>: saludo de regreso y de la mañana en el inicio (4).</summary>
        Return = 5,

        /// <summary><c>debut</c>: victoria de un estreno (3).</summary>
        Debut = 6,

        /// <summary><c>milestone</c>: fin de mundo y primer Muy difícil (2).</summary>
        Milestone = 7,
    }

    /// <summary>Pantalla en la que se decide la aparición de TiTino.</summary>
    public enum SlothScreen : byte
    {
        /// <summary>Victoria (02 §6, fila TiTino).</summary>
        Victory = 0,

        /// <summary>Nivel fallido (propuesta de TiTino d.5).</summary>
        Fail = 1,

        /// <summary>Aviso tras Volver a la orilla en 1-30 (D-016, T5-B).</summary>
        Restart = 2,

        /// <summary>Inicio (propuesta de TiTino d.7).</summary>
        Home = 3,
    }

    /// <summary>Gesto de TiTino (glosario §3.4: poses <c>sloth_*</c>; propuesta de TiTino c.6).</summary>
    public enum SlothGesture : byte
    {
        /// <summary>No sale, o sale en su reposo sin gesto propio.</summary>
        None = 0,

        /// <summary><c>sloth_cheer</c>: cualquier victoria en la que cabe.</summary>
        Cheer = 1,

        /// <summary><c>sloth_cheer_big</c>: sus tres hitos (02 §6, fila TiTino).</summary>
        CheerBig = 2,

        /// <summary><c>sloth_encourage</c>: Nivel fallido y el aviso de Volver a la orilla.</summary>
        Encourage = 3,

        /// <summary><c>sloth_wave</c>: saludo de regreso o de la mañana en el inicio.</summary>
        Wave = 4,
    }

    /// <summary>Estreno del nivel ganado (propuesta de TiTino e.4, frases 26-28), para el banco <c>debut</c>.</summary>
    public enum SlothDebut : byte
    {
        /// <summary>No es un estreno.</summary>
        None = 0,

        /// <summary>Estreno de una mecánica (02 §9.1): <c>sloth_line_debut_01</c>.</summary>
        Mechanic = 1,

        /// <summary>Estreno de una fruta: <c>sloth_line_debut_02</c>.</summary>
        Fruit = 2,

        /// <summary>Primer nivel de un mundo: <c>sloth_line_debut_03</c>.</summary>
        World = 3,
    }

    /// <summary>
    /// Lo que TiTino sabe de la victoria que se decide (<see cref="SlothLines.ForVictory"/>). Lo calcula quien llama con el
    /// progreso y el nivel; valor puro.
    /// </summary>
    public readonly struct SlothVictoryFacts
    {
        /// <summary>Crea los datos de la victoria.</summary>
        /// <param name="winCount"><c>stats.levels_won</c> contando esta victoria: la cadencia de 1 de cada N.</param>
        /// <param name="hard">El nivel es Difícil o Muy difícil (03 §11).</param>
        /// <param name="debut">El estreno del nivel, si lo es.</param>
        /// <param name="streak">Victorias de la Crecida tras esta (<c>streak.wins</c>).</param>
        /// <param name="streakMilestone">Esta victoria paga un hito de la Crecida (3, 6 o 10): protagonizan las nutrias (d.6, punto 3).</param>
        /// <param name="usedRescue">En este nivel se usó la Cuia de rescate.</param>
        /// <param name="usedHint">En este nivel se usó Morpho.</param>
        /// <param name="speakBox">La caja de TiTino mide ≥ 104 dp: cabe el bocadillo (d.4).</param>
        /// <param name="colorFactApproved">El asesor aprobó <c>fauna.bradypus_variegatus.dato_11</c> (la frase n.º 3 lo necesita).</param>
        public SlothVictoryFacts(int winCount, bool hard, SlothDebut debut, int streak, bool streakMilestone, bool usedRescue, bool usedHint,
            bool speakBox, bool colorFactApproved)
        {
            WinCount = winCount;
            Hard = hard;
            Debut = debut;
            Streak = streak;
            StreakMilestone = streakMilestone;
            UsedRescue = usedRescue;
            UsedHint = usedHint;
            SpeakBox = speakBox;
            ColorFactApproved = colorFactApproved;
        }

        /// <summary><c>stats.levels_won</c> contando esta victoria.</summary>
        public int WinCount { get; }

        /// <summary>Difícil o Muy difícil.</summary>
        public bool Hard { get; }

        /// <summary>Estreno del nivel.</summary>
        public SlothDebut Debut { get; }

        /// <summary>Victorias de la Crecida tras esta.</summary>
        public int Streak { get; }

        /// <summary>Hito de la Crecida en esta victoria.</summary>
        public bool StreakMilestone { get; }

        /// <summary>Cuia de rescate en este nivel.</summary>
        public bool UsedRescue { get; }

        /// <summary>Morpho en este nivel.</summary>
        public bool UsedHint { get; }

        /// <summary>Cabe el bocadillo (caja ≥ 104 dp).</summary>
        public bool SpeakBox { get; }

        /// <summary><c>fauna.bradypus_variegatus.dato_11</c> aprobado por el asesor.</summary>
        public bool ColorFactApproved { get; }
    }

    /// <summary>Lo que TiTino sabe de Nivel fallido (<see cref="SlothLines.ForFail"/>).</summary>
    public readonly struct SlothFailFacts
    {
        /// <summary>Crea los datos del fallo.</summary>
        /// <param name="firstFail">Es el primer fallo de este nivel (<c>level_state.fails_level</c> = 1).</param>
        /// <param name="streakBroken">Este fallo duro rompió la Crecida (04 §5.2): la frase es la n.º 21.</param>
        /// <param name="failsHard"><c>stats.fails_hard</c> contando este fallo (rota el banco).</param>
        /// <param name="speakBox">La frase cabe en la tarjeta (con la escala del 130 % puede no caber: solo el gesto; d.5).</param>
        public SlothFailFacts(bool firstFail, bool streakBroken, int failsHard, bool speakBox)
        {
            FirstFail = firstFail;
            StreakBroken = streakBroken;
            FailsHard = failsHard;
            SpeakBox = speakBox;
        }

        /// <summary>Primer fallo del nivel.</summary>
        public bool FirstFail { get; }

        /// <summary>El fallo rompió la Crecida.</summary>
        public bool StreakBroken { get; }

        /// <summary><c>stats.fails_hard</c>.</summary>
        public int FailsHard { get; }

        /// <summary>La frase cabe en la tarjeta.</summary>
        public bool SpeakBox { get; }
    }

    /// <summary>Lo que TiTino sabe del reinicio con Volver a la orilla (<see cref="SlothLines.ForRestartNotice"/>).</summary>
    public readonly struct SlothRestartFacts
    {
        /// <summary>Crea los datos del reinicio.</summary>
        /// <param name="firstRestart">Es el primer reinicio de este nivel.</param>
        /// <param name="tutorialInstruction">El nivel tiene instrucción del tutorial (TiTino ya está en ella; guion de la intro §14).</param>
        /// <param name="streakBroken">En 25-30, Volver a la orilla desde Aguas estancadas rompió la Crecida (04 §5.2): la n.º 21.</param>
        public SlothRestartFacts(bool firstRestart, bool tutorialInstruction, bool streakBroken)
        {
            FirstRestart = firstRestart;
            TutorialInstruction = tutorialInstruction;
            StreakBroken = streakBroken;
        }

        /// <summary>Primer reinicio del nivel.</summary>
        public bool FirstRestart { get; }

        /// <summary>El nivel tiene instrucción del tutorial.</summary>
        public bool TutorialInstruction { get; }

        /// <summary>El reinicio rompió la Crecida.</summary>
        public bool StreakBroken { get; }
    }

    /// <summary>Lo que TiTino sabe al mostrarse el inicio (<see cref="SlothLines.ForHome"/>).</summary>
    public readonly struct SlothHomeFacts
    {
        /// <summary>Crea los datos del inicio.</summary>
        /// <param name="today">Fecha local de hoy, <c>yyyy-MM-dd</c> (<c>ClockExtensions.LocalDateKey</c>).</param>
        /// <param name="hour">Hora local (0-23).</param>
        /// <param name="lastSeenDate"><c>sloth.last_seen_date</c>: la última apertura antes de esta, o null.</param>
        /// <param name="lastGreetingDate"><c>sloth.last_greeting_date</c>: el último saludo, o null.</param>
        /// <param name="nextLevel">Nivel que se juega con «Jugar» (la frase n.º 23 lo nombra); 0 si no se sabe.</param>
        public SlothHomeFacts(string? today, int hour, string? lastSeenDate, string? lastGreetingDate, int nextLevel)
        {
            Today = today;
            Hour = hour;
            LastSeenDate = lastSeenDate;
            LastGreetingDate = lastGreetingDate;
            NextLevel = nextLevel;
        }

        /// <summary>Fecha local de hoy.</summary>
        public string? Today { get; }

        /// <summary>Hora local.</summary>
        public int Hour { get; }

        /// <summary>Última apertura antes de esta.</summary>
        public string? LastSeenDate { get; }

        /// <summary>Último saludo.</summary>
        public string? LastGreetingDate { get; }

        /// <summary>Nivel de «Jugar».</summary>
        public int NextLevel { get; }
    }

    /// <summary>
    /// Entrada de <see cref="SlothLines"/> (D-016, puntos 4 y 5; propuesta de TiTino d.6 y d.10): los interruptores
    /// (<c>ui_sloth_*</c> de 08 §9 y <c>settings.sloth_messages</c>), lo que va de sesión (frases dichas y si la pantalla anterior
    /// tuvo frase), la última frase vista de cada momento (<c>sloth.last_line</c>) y los datos del momento que se decide
    /// (<see cref="WithVictory"/>, <see cref="WithFail"/>, <see cref="WithRestart"/> y <see cref="WithHome"/>). Valor puro: misma
    /// entrada, mismo resultado; lo crea <see cref="SlothSession.Context"/> o un test.
    /// </summary>
    public readonly struct SlothContext
    {
        private readonly int[]? _lastLine;

        /// <summary>Crea el contexto sin datos de ningún momento.</summary>
        /// <param name="enabled"><c>ui_sloth_enabled</c>.</param>
        /// <param name="messages"><c>settings.sloth_messages</c>: apagado, solo gestos.</param>
        /// <param name="failEnabled"><c>ui_sloth_fail_enabled</c>.</param>
        /// <param name="winLineEveryN"><c>ui_sloth_win_line_every_n</c>.</param>
        /// <param name="linesPerSessionMax"><c>ui_sloth_lines_per_session_max</c>.</param>
        /// <param name="linesThisSession">Frases dichas en esta sesión.</param>
        /// <param name="previousScreenSpoke">La pantalla anterior con TiTino tuvo frase.</param>
        /// <param name="lastLine"><c>sloth.last_line</c> (<see cref="SlothData.LastLine"/>; solo se lee), o null si no hay perfil.</param>
        public SlothContext(bool enabled, bool messages, bool failEnabled, int winLineEveryN, int linesPerSessionMax, int linesThisSession,
            bool previousScreenSpoke, int[]? lastLine)
        {
            Enabled = enabled;
            Messages = messages;
            FailEnabled = failEnabled;
            WinLineEveryN = winLineEveryN;
            LinesPerSessionMax = linesPerSessionMax;
            LinesThisSession = linesThisSession;
            PreviousScreenSpoke = previousScreenSpoke;
            _lastLine = lastLine;
            Victory = default;
            Fail = default;
            Restart = default;
            Home = default;
        }

        private SlothContext(in SlothContext source, in SlothVictoryFacts victory, in SlothFailFacts fail, in SlothRestartFacts restart,
            in SlothHomeFacts home)
        {
            this = source;
            Victory = victory;
            Fail = fail;
            Restart = restart;
            Home = home;
        }

        /// <summary><c>ui_sloth_enabled</c>.</summary>
        public bool Enabled { get; }

        /// <summary><c>settings.sloth_messages</c>.</summary>
        public bool Messages { get; }

        /// <summary><c>ui_sloth_fail_enabled</c>.</summary>
        public bool FailEnabled { get; }

        /// <summary><c>ui_sloth_win_line_every_n</c>.</summary>
        public int WinLineEveryN { get; }

        /// <summary><c>ui_sloth_lines_per_session_max</c>.</summary>
        public int LinesPerSessionMax { get; }

        /// <summary>Frases dichas en esta sesión.</summary>
        public int LinesThisSession { get; }

        /// <summary>La pantalla anterior con TiTino tuvo frase.</summary>
        public bool PreviousScreenSpoke { get; }

        /// <summary>Datos de la victoria (<see cref="WithVictory"/>).</summary>
        public SlothVictoryFacts Victory { get; }

        /// <summary>Datos de Nivel fallido (<see cref="WithFail"/>).</summary>
        public SlothFailFacts Fail { get; }

        /// <summary>Datos del reinicio (<see cref="WithRestart"/>).</summary>
        public SlothRestartFacts Restart { get; }

        /// <summary>Datos del inicio (<see cref="WithHome"/>).</summary>
        public SlothHomeFacts Home { get; }

        /// <summary>Índice de la última frase vista del momento <paramref name="moment"/>, o <see cref="SlothData.NoLine"/>.</summary>
        public int LastLineOf(SlothMoment moment)
        {
            int i = (int)moment;
            return _lastLine != null && i < _lastLine.Length ? _lastLine[i] : SlothData.NoLine;
        }

        /// <summary>Copia con los datos de la victoria.</summary>
        public SlothContext WithVictory(in SlothVictoryFacts facts)
        {
            return new SlothContext(in this, in facts, Fail, Restart, Home);
        }

        /// <summary>Copia con los datos de Nivel fallido.</summary>
        public SlothContext WithFail(in SlothFailFacts facts)
        {
            return new SlothContext(in this, Victory, in facts, Restart, Home);
        }

        /// <summary>Copia con los datos del reinicio.</summary>
        public SlothContext WithRestart(in SlothRestartFacts facts)
        {
            return new SlothContext(in this, Victory, Fail, in facts, Home);
        }

        /// <summary>Copia con los datos del inicio.</summary>
        public SlothContext WithHome(in SlothHomeFacts facts)
        {
            return new SlothContext(in this, Victory, Fail, Restart, in facts);
        }
    }

    /// <summary>
    /// Lo que decide <see cref="SlothLines"/> para una pantalla: el gesto, y la frase con su momento, su índice en el banco y su
    /// clave de <c>strings.csv</c> (o ninguna). Valor puro; las claves son constantes, sin asignaciones.
    /// </summary>
    public readonly struct SlothLine
    {
        /// <summary>Crea el resultado (solo <see cref="SlothLines"/> y los tests).</summary>
        public SlothLine(SlothScreen screen, SlothGesture gesture, SlothMoment moment, int index, string? key, int arg, bool greeting)
        {
            Screen = screen;
            Gesture = gesture;
            Moment = moment;
            Index = index;
            Key = key;
            Arg = arg;
            Greeting = greeting;
        }

        /// <summary>Pantalla decidida.</summary>
        public SlothScreen Screen { get; }

        /// <summary>Gesto (<see cref="SlothGesture.None"/>: TiTino no hace nada propio de este momento).</summary>
        public SlothGesture Gesture { get; }

        /// <summary>Momento de la frase (solo vale con <see cref="Speaks"/>).</summary>
        public SlothMoment Moment { get; }

        /// <summary>Índice de la frase en su banco (0 = <c>_01</c>), o <see cref="SlothData.NoLine"/>.</summary>
        public int Index { get; }

        /// <summary>Clave de la frase (<c>sloth_line_&lt;momento&gt;_&lt;nn&gt;</c>), o null si no habla.</summary>
        public string? Key { get; }

        /// <summary>Valor del marcador <c>{n}</c> de la frase (la Crecida o el nivel), o 0 si no lleva.</summary>
        public int Arg { get; }

        /// <summary>En el inicio, es el saludo del día (de regreso o de la mañana): cuenta para el tope de 1 al día.</summary>
        public bool Greeting { get; }

        /// <summary>TiTino dice una frase.</summary>
        public bool Speaks => Key != null;
    }

    /// <summary>
    /// Estado de TiTino durante una sesión (D-016, punto 5): los interruptores, las frases dichas, si la pantalla anterior tuvo
    /// frase y el <c>sloth</c> del perfil. Da el <see cref="Context"/> de cada decisión y apunta el resultado con
    /// <see cref="Record"/> (también las pantallas sin frase, que cortan la racha de «dos pantallas seguidas»).
    /// </summary>
    public sealed class SlothSession
    {
        private readonly SlothData _data;

        /// <summary>Sesión con los defaults de <see cref="GameConfig"/> (08 §9).</summary>
        public SlothSession(SlothData data, bool messages)
            : this(data, messages, GameConfig.UiSlothEnabled, GameConfig.UiSlothFailEnabled, GameConfig.UiSlothWinLineEveryN,
                GameConfig.UiSlothLinesPerSessionMax)
        {
        }

        /// <summary>Sesión con interruptores explícitos (Remote Config o tests).</summary>
        public SlothSession(SlothData data, bool messages, bool enabled, bool failEnabled, int winLineEveryN, int linesPerSessionMax)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            Messages = messages;
            Enabled = enabled;
            FailEnabled = failEnabled;
            WinLineEveryN = winLineEveryN;
            LinesPerSessionMax = linesPerSessionMax;
        }

        /// <summary><c>settings.sloth_messages</c> (se cambia en Ajustes en mitad de la sesión).</summary>
        public bool Messages { get; set; }

        /// <summary><c>ui_sloth_enabled</c>.</summary>
        public bool Enabled { get; }

        /// <summary><c>ui_sloth_fail_enabled</c>.</summary>
        public bool FailEnabled { get; }

        /// <summary><c>ui_sloth_win_line_every_n</c>.</summary>
        public int WinLineEveryN { get; }

        /// <summary><c>ui_sloth_lines_per_session_max</c>.</summary>
        public int LinesPerSessionMax { get; }

        /// <summary>Frases dichas en esta sesión.</summary>
        public int LinesThisSession { get; private set; }

        /// <summary>La última pantalla apuntada tuvo frase.</summary>
        public bool PreviousScreenSpoke { get; private set; }

        /// <summary>Contexto de ahora, sin datos de ningún momento.</summary>
        public SlothContext Context => new SlothContext(Enabled, Messages, FailEnabled, WinLineEveryN, LinesPerSessionMax, LinesThisSession,
            PreviousScreenSpoke, _data.LastLine);

        /// <summary>Datos del inicio con lo guardado en <c>sloth</c> (<c>last_seen_date</c> y <c>last_greeting_date</c>).</summary>
        public SlothHomeFacts HomeFacts(string today, int hour, int nextLevel)
        {
            return new SlothHomeFacts(today, hour, _data.LastSeenDate, _data.LastGreetingDate, nextLevel);
        }

        /// <summary>
        /// Apunta lo que se enseñó en una pantalla: con frase, suma una a la sesión y la guarda como la última de su momento
        /// (<c>sloth.last_line</c>); sin frase, la pantalla corta la racha. En el inicio, además, la fecha de hoy pasa a ser la
        /// última apertura y, con saludo, el último saludo. Quien llama guarda el perfil.
        /// </summary>
        public void Record(in SlothLine line, in SlothContext context)
        {
            PreviousScreenSpoke = line.Speaks;
            if (line.Speaks)
            {
                LinesThisSession++;
                int moment = (int)line.Moment;
                if (moment < _data.LastLine.Length)
                {
                    _data.LastLine[moment] = line.Index;
                }
            }

            if (line.Screen == SlothScreen.Home && SlothData.IsDateKey(context.Home.Today))
            {
                _data.LastSeenDate = context.Home.Today;
                if (line.Greeting)
                {
                    _data.LastGreetingDate = context.Home.Today;
                }
            }
        }
    }
}
