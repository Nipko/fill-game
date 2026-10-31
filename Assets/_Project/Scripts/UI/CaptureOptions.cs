#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Levels;
using UnityEngine;

namespace AF.UI
{
    /// <summary>Pantalla que el modo de captura abre directo al llegar al inicio (<c>af_capture_screen</c>; D-066, spark-marketing §6 E1).</summary>
    public enum CaptureScreen
    {
        /// <summary>Ninguna: el arranque de siempre.</summary>
        None = 0,

        /// <summary>El Mapa del río (<c>map</c>), como su pestaña del inicio (desde <c>max_level</c> ≥ 10).</summary>
        Map = 1,

        /// <summary>La Liga del río (<c>league</c>), como su botón del inicio (con la Liga activa y desbloqueada).</summary>
        League = 2,

        /// <summary>El Reto del río (<c>daily</c>), como «Jugar · Reto del río» (desbloqueado).</summary>
        Daily = 3,

        /// <summary>La Bitácora del río (<c>logbook</c>), como su tarjeta del inicio (desbloqueada).</summary>
        Logbook = 4,

        /// <summary>El Mercado (<c>market</c>), como su icono del inicio (desde <c>max_level</c> ≥ 10 y con la tienda).</summary>
        Market = 5,
    }

    /// <summary>
    /// Modo de captura para el marketing (D-066; <c>docs/dev/traspaso/spark-marketing.md</c> §6, enganche E1): los extras de intent
    /// de los builds de desarrollo con los que el equipo Spark graba tomas reales, repetibles y limpias en el teléfono
    /// (<c>com.amazonfill.dev</c>, <c>adb shell am start … --ez af_capture true</c> y <c>screenrecord</c>). Se leen con el mismo
    /// mecanismo que los demás extras (<see cref="LaunchOptions.ReadIntentBool"/>, <see cref="LaunchOptions.ReadIntentInt(string, int)"/> y
    /// <see cref="LaunchOptions.ReadIntentString"/>: solo en Android y solo con <see cref="Debug.isDebugBuild"/>) y, además,
    /// <see cref="Resolve"/> devuelve <see cref="Inactive"/> fuera de desarrollo: en release nada de esto existe aunque llegue el extra.
    /// Cualquier extra <c>af_capture*</c> enciende el modo, y el modo trabaja siempre con el perfil en memoria (no se guarda nada en
    /// disco ni en la nube) y en inmersivo total. La tabla de los extras está en <c>docs/dev/prototipo-v0.md</c> c), «Modo de
    /// captura». Nada de esto cambia una regla del juego: la solución que se reproduce es la del solver con las reglas de la
    /// partida (<c>rules_pour_mode</c>, D-005).
    /// </summary>
    public sealed class CaptureOptions
    {
        /// <summary>Booleano: enciende el modo de captura (perfil en memoria e inmersivo) sin nada más.</summary>
        public const string ExtraCapture = "af_capture";

        /// <summary>Booleano: oculta el HUD del nivel (insignia, Reiniciar, bandeja, engranaje, saldos, avisos y rótulos).</summary>
        public const string ExtraNoHud = "af_capture_no_hud";

        /// <summary>Texto: un paquete <c>af-level/1</c> en base64 (estándar o URL) que sustituye a los paquetes de la escena.</summary>
        public const string ExtraBoard = "af_capture_board";

        /// <summary>Texto: la ruta de un paquete <c>af-level/1</c> dentro de <c>Application.persistentDataPath</c>.</summary>
        public const string ExtraBoardPath = "af_capture_board_path";

        /// <summary>Booleano: reproduce la solución del solver con ritmo «humano».</summary>
        public const string ExtraAutoplay = "af_capture_autoplay";

        /// <summary>Entero (ms): pausa entre el fin de un vertido y el toque siguiente.</summary>
        public const string ExtraPauseMs = "af_capture_pause_ms";

        /// <summary>Entero (ms): variación máxima (±) de cada pausa, determinista con el PRNG del núcleo.</summary>
        public const string ExtraJitterMs = "af_capture_jitter_ms";

        /// <summary>Entero (ms): lo que la cuia se ve elevada entre el toque de origen y el de destino.</summary>
        public const string ExtraSelectMs = "af_capture_select_ms";

        /// <summary>Entero (ms): espera desde que el tablero acepta toques hasta el primer vertido.</summary>
        public const string ExtraStartMs = "af_capture_start_ms";

        /// <summary>Entero: se para cuando faltan estos vertidos para ganar («falta uno» = 1).</summary>
        public const string ExtraStopBefore = "af_capture_stop_before";

        /// <summary>Entero: semilla del PRNG del núcleo para la variación de las pausas.</summary>
        public const string ExtraSeed = "af_capture_seed";

        /// <summary>Entero (%): velocidad de la presentación (100 = normal; 25 = cuatro veces más lento).</summary>
        public const string ExtraSlowMo = "af_capture_slowmo";

        /// <summary>Entero (1-13, <see cref="WorldTable.WorldCount"/>): el fondo de la gran parada de ese mundo, en lugar del de la parada del nivel.</summary>
        public const string ExtraWorld = "af_capture_world";

        /// <summary>Entero (1-1.000): el fondo (parada, temporada) de ese nivel de campaña.</summary>
        public const string ExtraBgLevel = "af_capture_bg_level";

        /// <summary>Texto: el id de una skin de cuia (<c>SkinRewards</c>) solo para la toma, sin tocar el perfil.</summary>
        public const string ExtraSkin = "af_capture_skin";

        /// <summary>Texto: <c>map</c>, <c>league</c>, <c>daily</c>, <c>logbook</c> o <c>market</c>: se abre al llegar al inicio.</summary>
        public const string ExtraScreen = "af_capture_screen";

        /// <summary>Pausa entre vertidos por defecto [P] (ms).</summary>
        public const int DefaultPauseMs = 700;

        /// <summary>Variación de la pausa por defecto [P] (± ms).</summary>
        public const int DefaultJitterMs = 150;

        /// <summary>Tiempo con la cuia elevada por defecto [P] (ms).</summary>
        public const int DefaultSelectMs = 260;

        /// <summary>Espera antes del primer vertido por defecto [P] (ms).</summary>
        public const int DefaultStartMs = 900;

        /// <summary>Tope de cualquier tiempo del ritmo (ms).</summary>
        public const int MaxRhythmMs = 10000;

        /// <summary>Velocidad mínima de la cámara lenta (%).</summary>
        public const int MinSlowMoPercent = 5;

        /// <summary>Tope del paquete en el extra o en el archivo (bytes del JSON).</summary>
        public const int MaxBoardBytes = 512 * 1024;

        /// <summary>Un modo apagado nuevo (cada vez uno: nadie comparte un objeto que se pueda cambiar); el de release.</summary>
        public static CaptureOptions Inactive => new CaptureOptions();

        // Valor de un extra entero que no llegó (ningún valor real lo usa).
        private const int Missing = int.MinValue;

        private readonly List<string> _problems = new List<string>();

        /// <summary>Hay algún extra de captura: perfil en memoria, inmersivo y líneas <c>AF_CAPTURE</c>.</summary>
        public bool Active { get; set; }

        /// <summary>Ocultar el HUD del nivel (<see cref="ExtraNoHud"/>).</summary>
        public bool HideHud { get; set; }

        /// <summary>Paquete en base64 (<see cref="ExtraBoard"/>), sin decodificar; null si no hay.</summary>
        public string? BoardBase64 { get; set; }

        /// <summary>Ruta del paquete (<see cref="ExtraBoardPath"/>); null si no hay.</summary>
        public string? BoardPath { get; set; }

        /// <summary>Reproducir la solución (<see cref="ExtraAutoplay"/>).</summary>
        public bool Autoplay { get; set; }

        /// <summary>Pausa entre vertidos (ms).</summary>
        public int PauseMs { get; set; } = DefaultPauseMs;

        /// <summary>Variación de la pausa (± ms).</summary>
        public int JitterMs { get; set; } = DefaultJitterMs;

        /// <summary>Tiempo con la cuia elevada (ms).</summary>
        public int SelectMs { get; set; } = DefaultSelectMs;

        /// <summary>Espera antes del primer vertido (ms).</summary>
        public int StartMs { get; set; } = DefaultStartMs;

        /// <summary>Vertidos que se dejan sin hacer al final (0 = hasta la victoria).</summary>
        public int StopBefore { get; set; }

        /// <summary>Semilla del PRNG de las pausas.</summary>
        public int Seed { get; set; } = 1;

        /// <summary>Factor del reloj de la presentación (1 = normal; 0,25 = cámara lenta ×4).</summary>
        public float TimeScale { get; set; } = 1f;

        /// <summary>Mundo cuyo fondo se fuerza (0 = ninguno).</summary>
        public int World { get; set; }

        /// <summary>Nivel cuyo fondo se fuerza (0 = ninguno); manda sobre <see cref="World"/>.</summary>
        public int BgLevel { get; set; }

        /// <summary>Skin de cuia forzada (id de <c>SkinRewards</c>), o null.</summary>
        public string? Skin { get; set; }

        /// <summary>Pantalla que se abre al llegar al inicio.</summary>
        public CaptureScreen Screen { get; set; }

        /// <summary>Valores de extras que no se entendieron (se ignoran; cada uno sale como <c>AF_CAPTURE error</c>).</summary>
        public IReadOnlyList<string> Problems => _problems;

        /// <summary>Hay un paquete pedido (en el extra o por ruta).</summary>
        public bool HasBoard => BoardBase64 != null || BoardPath != null;

        /// <summary>
        /// Nivel de campaña cuyo fondo lleva el tablero: <see cref="BgLevel"/> o, si no, el primer nivel de <see cref="World"/>
        /// (<see cref="WorldTable.FirstLevel"/>, su gran parada); 0 = el del nivel cargado.
        /// </summary>
        public int BackdropLevel => BgLevel > 0 ? BgLevel : World > 0 ? WorldTable.FirstLevel(World) : 0;

        /// <summary>
        /// Las opciones de la partida: en un build de desarrollo (<paramref name="development"/>, el <c>Debug.isDebugBuild</c> del
        /// arranque), las del test (<see cref="LaunchOptions.CaptureOverride"/>) o las de los extras de intent; fuera de desarrollo,
        /// siempre <see cref="Inactive"/>.
        /// </summary>
        public static CaptureOptions Resolve(bool development)
        {
            if (!development)
            {
                return Inactive;
            }

            return LaunchOptions.CaptureOverride ?? Read(LaunchOptions.ReadIntentBool, LaunchOptions.ReadIntentInt, LaunchOptions.ReadIntentString);
        }

        /// <summary>
        /// Lee los extras con los lectores dados (<paramref name="readBool"/>, <paramref name="readInt"/> con su valor por defecto y
        /// <paramref name="readString"/>, null si no hay). Un valor fuera de rango se lleva al borde o se ignora, con su problema
        /// en <see cref="Problems"/>.
        /// </summary>
        public static CaptureOptions Read(Func<string, bool> readBool, Func<string, int, int> readInt, Func<string, string?> readString)
        {
            if (readBool == null || readInt == null || readString == null)
            {
                throw new ArgumentNullException(readBool == null ? nameof(readBool) : readInt == null ? nameof(readInt) : nameof(readString));
            }

            var o = new CaptureOptions();
            // Cualquier extra af_capture* que llegue enciende el modo (también uno del ritmo solo, o uno que no se entienda).
            bool any = readBool(ExtraCapture);
            int Int(string name)
            {
                int value = readInt(name, Missing);
                any |= value != Missing;
                return value;
            }

            string? Str(string name)
            {
                string? value = readString(name);
                any |= value != null;
                return Blank(value);
            }

            o.HideHud = readBool(ExtraNoHud);
            o.Autoplay = readBool(ExtraAutoplay);
            o.BoardBase64 = Str(ExtraBoard);
            o.BoardPath = Str(ExtraBoardPath);
            o.PauseMs = o.Rhythm(Int(ExtraPauseMs), DefaultPauseMs, ExtraPauseMs);
            o.JitterMs = o.Rhythm(Int(ExtraJitterMs), DefaultJitterMs, ExtraJitterMs);
            o.SelectMs = o.Rhythm(Int(ExtraSelectMs), DefaultSelectMs, ExtraSelectMs);
            o.StartMs = o.Rhythm(Int(ExtraStartMs), DefaultStartMs, ExtraStartMs);
            int stopBefore = Int(ExtraStopBefore);
            stopBefore = stopBefore == Missing ? 0 : stopBefore;
            if (stopBefore < 0)
            {
                o._problems.Add(ExtraStopBefore + " < 0: se usa 0");
                stopBefore = 0;
            }

            o.StopBefore = stopBefore;
            int seed = Int(ExtraSeed);
            o.Seed = seed == Missing ? 1 : seed;
            int slowMo = Int(ExtraSlowMo);
            slowMo = slowMo == Missing ? 0 : slowMo;
            if (slowMo != 0)
            {
                int clamped = Math.Max(MinSlowMoPercent, Math.Min(100, slowMo));
                if (clamped != slowMo)
                {
                    o._problems.Add(ExtraSlowMo + " fuera de " + MinSlowMoPercent.ToString(CultureInfo.InvariantCulture) + "-100: se usa "
                        + clamped.ToString(CultureInfo.InvariantCulture));
                }

                o.TimeScale = clamped / 100f;
            }

            int world = Int(ExtraWorld);
            world = world == Missing ? 0 : world;
            if (world != 0 && (world < 1 || world > WorldTable.WorldCount))
            {
                o._problems.Add(ExtraWorld + " fuera de 1-" + WorldTable.WorldCount.ToString(CultureInfo.InvariantCulture) + ": se ignora");
                world = 0;
            }

            o.World = world;
            int bgLevel = Int(ExtraBgLevel);
            bgLevel = bgLevel == Missing ? 0 : bgLevel;
            if (bgLevel != 0 && (bgLevel < 1 || bgLevel > WorldTable.LastLevel))
            {
                o._problems.Add(ExtraBgLevel + " fuera de 1-" + WorldTable.LastLevel.ToString(CultureInfo.InvariantCulture) + ": se ignora");
                bgLevel = 0;
            }

            o.BgLevel = bgLevel;
            o.Skin = Str(ExtraSkin);
            string? screen = Str(ExtraScreen);
            if (screen != null)
            {
                if (TryParseScreen(screen, out CaptureScreen parsed))
                {
                    o.Screen = parsed;
                }
                else
                {
                    o._problems.Add(ExtraScreen + " desconocida (\"" + screen + "\"): se ignora");
                }
            }

            o.Active = any || o.HideHud || o.Autoplay || o._problems.Count > 0;
            return o.Active ? o : Inactive;
        }

        /// <summary>Convierte <c>map</c>, <c>league</c>, <c>daily</c>, <c>logbook</c> o <c>market</c>.</summary>
        public static bool TryParseScreen(string? value, out CaptureScreen screen)
        {
            switch (value)
            {
                case "map":
                    screen = CaptureScreen.Map;
                    return true;
                case "league":
                    screen = CaptureScreen.League;
                    return true;
                case "daily":
                    screen = CaptureScreen.Daily;
                    return true;
                case "logbook":
                    screen = CaptureScreen.Logbook;
                    return true;
                case "market":
                    screen = CaptureScreen.Market;
                    return true;
                default:
                    screen = CaptureScreen.None;
                    return false;
            }
        }

        /// <summary>El id de <paramref name="screen"/> en los extras y en <c>AF_CAPTURE</c>.</summary>
        public static string ScreenId(CaptureScreen screen)
        {
            switch (screen)
            {
                case CaptureScreen.Map: return "map";
                case CaptureScreen.League: return "league";
                case CaptureScreen.Daily: return "daily";
                case CaptureScreen.Logbook: return "logbook";
                case CaptureScreen.Market: return "market";
                default: return "none";
            }
        }

        /// <summary>
        /// El JSON del paquete pedido: el del extra en base64 (estándar o URL, con o sin relleno) o el del archivo
        /// <see cref="BoardPath"/> relativo a <paramref name="persistentDataPath"/> (sin salir de esa carpeta). Null y
        /// <paramref name="error"/> si no se pudo leer; null sin error si no se pidió ninguno. No valida el contenido: eso lo hace
        /// <see cref="LevelPackReader.Read"/>, con <c>BoardBuilder</c> y <c>LevelValidation</c> del núcleo.
        /// </summary>
        public string? ReadBoardJson(string? persistentDataPath, out string? error)
        {
            error = null;
            if (BoardBase64 != null)
            {
                return DecodeBase64(BoardBase64, out error);
            }

            if (BoardPath == null)
            {
                return null;
            }

            return ReadBoardFile(persistentDataPath, BoardPath, out error);
        }

        /// <summary>Decodifica un paquete en base64 (estándar o URL) a texto UTF-8.</summary>
        public static string? DecodeBase64(string text, out string? error)
        {
            error = null;
            if (text.Length > ((MaxBoardBytes + 2) / 3 * 4) + 8)
            {
                error = ExtraBoard + ": el paquete pasa de " + MaxBoardBytes.ToString(CultureInfo.InvariantCulture) + " bytes";
                return null; // sin decodificar nada
            }

            string s = text.Trim().Replace('-', '+').Replace('_', '/');
            int pad = s.Length % 4;
            if (pad == 2)
            {
                s += "==";
            }
            else if (pad == 3)
            {
                s += "=";
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(s);
                if (bytes.Length > MaxBoardBytes)
                {
                    error = ExtraBoard + ": el paquete pasa de " + MaxBoardBytes.ToString(CultureInfo.InvariantCulture) + " bytes";
                    return null;
                }

                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (FormatException e)
            {
                error = ExtraBoard + ": no es base64 ni UTF-8 (" + e.Message + ")";
                return null;
            }
            catch (ArgumentException e)
            {
                error = ExtraBoard + ": no es UTF-8 (" + e.Message + ")";
                return null;
            }
        }

        /// <summary>
        /// Lee <paramref name="relative"/> dentro de <paramref name="root"/>: una ruta absoluta o que sale de la carpeta (<c>..</c>) no
        /// se lee.
        /// </summary>
        public static string? ReadBoardFile(string? root, string relative, out string? error)
        {
            error = null;
            if (string.IsNullOrEmpty(root))
            {
                error = ExtraBoardPath + ": sin carpeta de datos de la app";
                return null;
            }

            try
            {
                string full = Path.GetFullPath(Path.Combine(root!, relative));
                string baseDir = Path.GetFullPath(root!).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (Path.IsPathRooted(relative) || !full.StartsWith(baseDir, StringComparison.Ordinal))
                {
                    error = ExtraBoardPath + ": la ruta tiene que ir dentro de la carpeta de la app (" + relative + ")";
                    return null;
                }

                if (!File.Exists(full))
                {
                    error = ExtraBoardPath + ": no existe " + relative;
                    return null;
                }

                var info = new FileInfo(full);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    error = ExtraBoardPath + ": " + relative + " es un enlace: no se sigue";
                    return null;
                }

                if (info.Length > MaxBoardBytes)
                {
                    error = ExtraBoardPath + ": el paquete pasa de " + MaxBoardBytes.ToString(CultureInfo.InvariantCulture) + " bytes";
                    return null;
                }

                return File.ReadAllText(full, new UTF8Encoding(false, true)); // UTF-8 estricto: un byte inválido es un error, no un «?»
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            {
                error = ExtraBoardPath + ": no se pudo leer " + relative + " (" + e.Message + ")";
                return null;
            }
        }

        /// <summary>Resumen JSON (miembros sin llaves) de lo pedido, para <c>AF_CAPTURE start</c>.</summary>
        public string Describe()
        {
            var sb = new StringBuilder(256);
            sb.Append("\"no_hud\":").Append(HideHud ? "true" : "false");
            sb.Append(",\"board\":").Append(BoardBase64 != null ? "\"base64\"" : BoardPath != null ? "\"path\"" : "null");
            sb.Append(",\"autoplay\":").Append(Autoplay ? "true" : "false");
            sb.Append(",\"pause_ms\":").Append(PauseMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"jitter_ms\":").Append(JitterMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"select_ms\":").Append(SelectMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"start_ms\":").Append(StartMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stop_before\":").Append(StopBefore.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"seed\":").Append(Seed.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"slowmo\":").Append(((int)Math.Round(TimeScale * 100f)).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(World.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"bg_level\":").Append(BackdropLevel.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"skin\":").Append(Skin == null ? "null" : CaptureLog.Quote(Skin));
            sb.Append(",\"screen\":\"").Append(ScreenId(Screen)).Append('"');
            return sb.ToString();
        }

        private int Rhythm(int value, int fallback, string extra)
        {
            if (value < 0)
            {
                return fallback;
            }

            if (value > MaxRhythmMs)
            {
                _problems.Add(extra + " > " + MaxRhythmMs.ToString(CultureInfo.InvariantCulture) + ": se usa el tope");
                return MaxRhythmMs;
            }

            return value;
        }

        private static string? Blank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
        }
    }

    /// <summary>
    /// Las líneas <c>AF_CAPTURE {json}</c> del modo de captura (D-066, E1): una al empezar y otra al terminar cada acción, o al fallar,
    /// para que Spark sincronice el <c>screenrecord</c> con logcat. Cada una lleva <c>event</c> y <c>t</c> (ms de reloj real desde el
    /// arranque del proceso de Unity) y cabe holgada en el tope de 960 bytes de la sonda. Solo en desarrollo.
    /// </summary>
    public static class CaptureLog
    {
        /// <summary>Etiqueta de las líneas.</summary>
        public const string Tag = "AF_CAPTURE";

        /// <summary>Largo máximo de un texto dentro de una línea (los mensajes de error se recortan).</summary>
        public const int MaxTextChars = 300;

        /// <summary>Última línea escrita (tests).</summary>
        public static string? LastLine { get; private set; }

        /// <summary>Escribe <c>AF_CAPTURE {"event":…,"t":…[,members]}</c>.</summary>
        public static string Write(string evt, string? members = null)
        {
            string t = ((long)(Time.realtimeSinceStartupAsDouble * 1000.0)).ToString(CultureInfo.InvariantCulture);
            string line = Tag + " {\"event\":\"" + evt + "\",\"t\":" + t + (string.IsNullOrEmpty(members) ? string.Empty : "," + members) + "}";
            LastLine = line;
            if (Debug.isDebugBuild)
            {
                Debug.Log(line);
            }

            return line;
        }

        /// <summary>Escribe <c>AF_CAPTURE {"event":"error",…,"what":…,"message":…}</c> con un aviso de Unity.</summary>
        public static string Error(string what, string message)
        {
            string line = Write("error", "\"what\":" + Quote(what) + ",\"message\":" + Quote(message));
            Debug.LogWarning("[AF.Capture] " + what + ": " + message);
            return line;
        }

        /// <summary>Texto JSON entre comillas, escapado y recortado a <see cref="MaxTextChars"/>.</summary>
        public static string Quote(string text)
        {
            string s = text.Length > MaxTextChars ? text.Substring(0, MaxTextChars) + "…" : text;
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>Olvida la última línea (tests).</summary>
        public static void Reset()
        {
            LastLine = null;
        }
    }
}
