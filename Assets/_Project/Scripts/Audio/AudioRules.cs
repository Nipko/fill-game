#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Hápticos del juego (02 §4; 07 §9.2; la lista, §7), con los efectos predefinidos de Android. Ninguno es de rechazo: «nunca
    /// háptico en rechazo» (02 §4) no se puede ni pedir.
    /// </summary>
    public enum HapticKind
    {
        /// <summary><c>haptic_seal</c>: cuia sellada, ligero (20 ms o <c>EFFECT_TICK</c>; <c>sfx_haptic_seal</c> de 07 §9.2).</summary>
        Seal = 0,

        /// <summary><c>haptic_win</c>: celebración de la victoria, medio.</summary>
        Win = 1,

        /// <summary><c>haptic_capi</c>: llega Capi con la cuia, medio.</summary>
        Capi = 2,

        /// <summary><c>haptic_boto</c>: Salto del Boto, medio.</summary>
        Boto = 3,

        /// <summary><c>haptic_back_blocked</c>: Atrás no puede cerrar un aviso obligatorio, corto.</summary>
        BackBlocked = 4,

        /// <summary><c>haptic_tinaja_release</c>: la suelta de la Tinaja (02 §4.1), ligero [P] (TJ3).</summary>
        TinajaRelease = 5,
    }

    /// <summary>Intensidad de un háptico: el efecto predefinido de Android y su respaldo por duración (API 26-28).</summary>
    public enum HapticStrength
    {
        /// <summary>Ligero: <c>EFFECT_TICK</c> (API 29+) o 20 ms.</summary>
        Light = 0,

        /// <summary>Medio: <c>EFFECT_CLICK</c> (API 29+) o 40 ms.</summary>
        Medium = 1,

        /// <summary>Corto: <c>EFFECT_TICK</c> (API 29+) o 15 ms.</summary>
        Short = 2,
    }

    /// <summary>Nombres y fuerzas de <see cref="HapticKind"/>.</summary>
    public static class Haptics
    {
        /// <summary><c>VibrationEffect.EFFECT_CLICK</c>.</summary>
        public const int EffectClick = 0;

        /// <summary><c>VibrationEffect.EFFECT_TICK</c>.</summary>
        public const int EffectTick = 2;

        /// <summary>ID de la lista (<c>haptic_seal</c>…), el <c>kind</c> de la sonda.</summary>
        public static string Name(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Seal: return "haptic_seal";
                case HapticKind.Win: return "haptic_win";
                case HapticKind.Capi: return "haptic_capi";
                case HapticKind.Boto: return "haptic_boto";
                case HapticKind.BackBlocked: return "haptic_back_blocked";
                case HapticKind.TinajaRelease: return "haptic_tinaja_release";
                default: throw new ArgumentOutOfRangeException(nameof(kind), "Háptico desconocido.");
            }
        }

        /// <summary>Intensidad de la lista (§7): sellado y suelta de la Tinaja ligeros, victoria, Capi y Boto medios, Atrás bloqueado corto.</summary>
        public static HapticStrength StrengthOf(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Seal:
                case HapticKind.TinajaRelease:
                    return HapticStrength.Light;
                case HapticKind.BackBlocked: return HapticStrength.Short;
                default: return HapticStrength.Medium;
            }
        }

        /// <summary>Efecto predefinido de Android (API 29+).</summary>
        public static int PredefinedEffect(HapticStrength strength)
        {
            return strength == HapticStrength.Medium ? EffectClick : EffectTick;
        }

        /// <summary>Duración del respaldo de API 26-28 (ms).</summary>
        public static long FallbackMs(HapticStrength strength)
        {
            switch (strength)
            {
                case HapticStrength.Light: return 20;
                case HapticStrength.Short: return 15;
                default: return 40;
            }
        }
    }

    /// <summary>Lo que el jugador eligió en Ajustes (<c>settings.sound</c>, <c>music</c> y <c>haptics</c>; 08 §4.3).</summary>
    public readonly struct SoundSettings
    {
        /// <summary>Crea los ajustes.</summary>
        public SoundSettings(bool sound, bool music, bool haptics)
        {
            Sound = sound;
            Music = music;
            Haptics = haptics;
        }

        /// <summary>Sonido (efectos).</summary>
        public bool Sound { get; }

        /// <summary>Música.</summary>
        public bool Music { get; }

        /// <summary>Vibración.</summary>
        public bool Haptics { get; }

        /// <summary>
        /// Los de un perfil nuevo: los defaults de 08 §9 (<c>audio_sfx_default_on</c>, <c>audio_music_default_on</c> y
        /// <c>ui_haptics_default_on</c>, todos true; 07 §9.4: «Sonido, Música y Vibración activados por defecto»).
        /// </summary>
        public static SoundSettings Defaults => new SoundSettings(true, true, true);
    }

    /// <summary>Modo del timbre del teléfono (<c>AudioManager.getRingerMode</c>).</summary>
    public enum RingerMode
    {
        /// <summary><c>RINGER_MODE_SILENT</c> (0): ni sonido ni vibración.</summary>
        Silent = 0,

        /// <summary><c>RINGER_MODE_VIBRATE</c> (1): sin sonido; vibra.</summary>
        Vibrate = 1,

        /// <summary><c>RINGER_MODE_NORMAL</c> (2), y el Editor.</summary>
        Normal = 2,
    }

    /// <summary>Estado de la música para la sonda <c>AF_STATE music</c> (tarea AU2).</summary>
    public readonly struct MusicReport
    {
        /// <summary>Crea el estado.</summary>
        public MusicReport(int world, int layers, bool night, bool ducked, bool menu)
        {
            World = world;
            Layers = layers;
            Night = night;
            Ducked = ducked;
            Menu = menu;
        }

        /// <summary>Mundo del lecho (0 = ninguno: el tema de menú o silencio).</summary>
        public int World { get; }

        /// <summary>Capas de percusión pedidas.</summary>
        public int Layers { get; }

        /// <summary>Variante nocturna (Muy difícil).</summary>
        public bool Night { get; }

        /// <summary>El lecho y el ambiente bajan 6 dB (toada o modal).</summary>
        public bool Ducked { get; }

        /// <summary>Suena el tema de menú.</summary>
        public bool Menu { get; }

        /// <summary>Mismo estado.</summary>
        public bool SameAs(in MusicReport other)
        {
            return World == other.World && Layers == other.Layers && Night == other.Night && Ducked == other.Ducked && Menu == other.Menu;
        }
    }

    /// <summary>
    /// Reglas de mezcla de 07 §9.4 y 02 §4, sin motor: qué suena y a qué ganancia en cada momento. Sonido y Música se apagan por
    /// separado y al momento; el modo silencio del teléfono (y el de vibración) calla los efectos y la música [P: también calla
    /// la música]; en silencio total tampoco vibra; durante un anuncio se calla todo y al terminar vuelve con un fundido de
    /// <see cref="AdFadeMs"/>; el lecho baja <see cref="DuckDb"/> durante la toada y los modales, y <see cref="ActionDuckDb"/>, con
    /// ataque y suelta cortos, durante el vertido y el sellado (observación 23 de la jornada 6). Desde AU2 también guarda qué
    /// música y qué ambiente se piden (<see cref="SetMusic"/>, <see cref="SetMenuMusic"/> y <see cref="SetAmbient"/>), lleva el
    /// reloj de la fauna y dice cuándo cambia la sonda de la música (<see cref="TakeMusicReport"/>). El ambiente va con el
    /// ajuste Música y baja con la misma atenuación [P]. Sin asignaciones.
    /// </summary>
    public sealed class AudioMix
    {
        /// <summary>Fundido de vuelta tras un anuncio (ms; 07 §9.4).</summary>
        public const float AdFadeMs = 500f;

        /// <summary>Atenuación del lecho durante la toada y los modales (dB; 07 §9.4).</summary>
        public const float DuckDb = -6f;

        /// <summary>
        /// Lo que dura la toada cuando no se sabe su largo (ms; 2,25 s): el servicio da el del clip que suena (el WAV o el del
        /// respaldo por código) con <see cref="ToadaStarted(float)"/>; este valor queda para el servicio falso y para un clip que aún
        /// no existe (tarea FX7).
        /// </summary>
        public const float ToadaDuckMs = 2250f;

        /// <summary>
        /// Ganancia de un remate sobre su pico de −16 dBFS (dB; tarea AU3) [P]: queda hacia −24 dBFS, por debajo de los efectos que
        /// acompaña (la piha a −14, la llegada a −21, la tarjeta previa a −20 y la canoa perdida a −21; encargo de sonidos §8:
        /// «siempre bajo los efectos»).
        /// </summary>
        public const float StingerGainDb = -8f;

        /// <summary>
        /// Ganancia de la música y del ambiente mientras otro suena por encima y deja oír lo de debajo (foco de audio
        /// <see cref="AudioFocusState.Duck"/>; observación 10 de la jornada 6): el ~20 % [P].
        /// </summary>
        public const float FocusDuckGain = 0.2f;

        /// <summary>
        /// Atenuación suave de la música y del ambiente mientras cae un chorro y mientras suena un sellado (dB; observación 23 de
        /// la jornada 6, decisión del dueño del 2026-10-30) [P]: los efectos de juego se oyen claros sobre el lecho. Menos honda
        /// que la de la toada y los modales (<see cref="DuckDb"/>); si coinciden, manda la más honda.
        /// </summary>
        public const float ActionDuckDb = -5f;

        /// <summary>Lo que tarda la música en bajar del todo al empezar un chorro o un sellado (ms) [P].</summary>
        public const float ActionDuckAttackMs = 80f;

        /// <summary>Lo que tarda la música en volver del todo tras la espera (ms) [P].</summary>
        public const float ActionDuckReleaseMs = 300f;

        /// <summary>
        /// Lo que la música sigue abajo tras cortarse el último chorro antes de empezar a volver (ms) [P]: con vertidos seguidos
        /// no sube y baja en cada uno (sin bombeo).
        /// </summary>
        public const float ActionDuckHoldMs = 600f;

        /// <summary>Lo que la música sigue abajo desde que empieza un sellado (ms) [P]: lo que dura <c>sfx_seal</c> (0,46 s).</summary>
        public const float SealDuckMs = 460f;

        private float _adFade = 1f;
        private float _toadaLeftMs;
        private bool _pouring;
        private float _actionHoldMs;
        private float _actionDuck; // 0 = sin atenuar; 1 = a ActionDuckDb
        private FaunaClock _fauna;
        private MusicReport _reported;
        private bool _hasReported;

        /// <summary>Ajustes de Ajustes.</summary>
        public SoundSettings Settings { get; set; } = SoundSettings.Defaults;

        /// <summary>Modo del timbre del teléfono.</summary>
        public RingerMode Ringer { get; set; } = RingerMode.Normal;

        /// <summary>Hay un anuncio delante (simulado hoy, D-008).</summary>
        public bool AdActive { get; private set; }

        /// <summary>Hay un modal que atenúa el lecho (Aguas estancadas y el embudo).</summary>
        public bool ModalDuck { get; set; }

        /// <summary>El foco de audio del teléfono (<see cref="SetFocus"/>; observación 10 de la jornada 6).</summary>
        public AudioFocusState Focus { get; private set; }

        /// <summary>Otro tomó el foco de audio (una alarma, una llamada, otra app de música): la música y los efectos se pausan.</summary>
        public bool FocusPaused => AudioFocusStates.Pauses(Focus);

        /// <summary>Otro suena por encima un momento: la música y el ambiente bajan a <see cref="FocusDuckGain"/>.</summary>
        public bool FocusDucked => Focus == AudioFocusState.Duck;

        /// <summary>
        /// Ya sonaba música de otra app al pasar a primer plano (el dueño, 2026-10-29): la música y el ambiente del juego no suenan;
        /// los efectos sí.
        /// </summary>
        public bool FocusYielded => Focus == AudioFocusState.Yielded;

        /// <summary>Los efectos se pueden oír ahora (sin contar el fundido).</summary>
        public bool SfxAudible => Settings.Sound && Ringer == RingerMode.Normal && !AdActive && !FocusPaused;

        /// <summary>La música se puede oír ahora.</summary>
        public bool MusicAudible => Settings.Music && Ringer == RingerMode.Normal && !AdActive && !FocusPaused && !FocusYielded;

        /// <summary>Se puede vibrar ahora (Vibración encendida, sin anuncio y el teléfono no en silencio total).</summary>
        public bool HapticsAllowed => Settings.Haptics && Ringer != RingerMode.Silent && !AdActive;

        /// <summary>Ganancia de los efectos (0-1): 0 sin sonido; tras un anuncio, el fundido.</summary>
        public float SfxGain => SfxAudible ? _adFade : 0f;

        /// <summary>
        /// Ganancia de la música (0-1), con la atenuación de la toada y de los modales o la del vertido y el sellado (la más
        /// honda de las dos) y la del foco de audio.
        /// </summary>
        public float MusicGain => MusicAudible ? _adFade * Math.Min(Ducked ? DuckGain : 1f, ActionDuckGain) * (FocusDucked ? FocusDuckGain : 1f) : 0f;

        /// <summary>
        /// Ganancia de un remate (0-1; tarea AU3): va con el ajuste Música [P], con el fundido tras un anuncio y sin la atenuación
        /// del lecho, a <see cref="StingerGainDb"/>.
        /// </summary>
        public float StingerGain => MusicAudible ? _adFade * (float)AudioAnalysis.FromDb(StingerGainDb) : 0f;

        /// <summary>Lo que le queda a la toada de la victoria (ms; 0 si no suena): los remates esperan a que termine (tarea AU3).</summary>
        public float ToadaLeftMs => _toadaLeftMs;

        /// <summary>El lecho está atenuado <see cref="DuckDb"/> (la toada o un modal).</summary>
        public bool Ducked => ModalDuck || _toadaLeftMs > 0f;

        /// <summary>Cae algún chorro (<see cref="SetPouring"/>).</summary>
        public bool Pouring => _pouring;

        /// <summary>
        /// El vertido o el sellado piden bajar la música: cae un chorro o aún dura la espera del último chorro o del sellado, y
        /// los efectos se oyen (sin Sonido, nada que destacar).
        /// </summary>
        public bool ActionDuckWanted => SfxAudible && (_pouring || _actionHoldMs > 0f);

        /// <summary>La música está (o empieza a estar) atenuada por el vertido o el sellado: lo que cuenta la sonda con «ducked».</summary>
        public bool ActionDucked => ActionDuckWanted || _actionDuck > 0f;

        /// <summary>Cuánto pesa ahora la atenuación del vertido (0-1; tests).</summary>
        public float ActionDuckAmount => _actionDuck;

        /// <summary>Ganancia lineal de la atenuación del vertido y del sellado de ahora (1 sin atenuar; <see cref="ActionDuckDb"/> del todo).</summary>
        public float ActionDuckGain => _actionDuck <= 0f ? 1f : (float)AudioAnalysis.FromDb(ActionDuckDb * _actionDuck);

        /// <summary>Ganancia lineal de <see cref="DuckDb"/>.</summary>
        public static float DuckGain => (float)AudioAnalysis.FromDb(DuckDb);

        /// <summary>Mundo del lecho pedido (0 = ninguno).</summary>
        public int MusicWorld { get; private set; }

        /// <summary>Capas de percusión pedidas (0-<see cref="AudioConfig.MusicPercussionLayersMax"/>).</summary>
        public int MusicLayers { get; private set; }

        /// <summary>Se pidió la variante nocturna del lecho (Muy difícil).</summary>
        public bool MusicNight { get; private set; }

        /// <summary>Se pidió el tema de menú (inicio).</summary>
        public bool MusicMenu { get; private set; }

        /// <summary>Mundo del ambiente pedido (0 = ninguno).</summary>
        public int AmbientWorld { get; private set; }

        /// <summary>El nivel tiene lluvia.</summary>
        public bool AmbientRain { get; private set; }

        /// <summary>El ambiente se puede oír ahora [P: va con el ajuste Música, como la música].</summary>
        public bool AmbientAudible => MusicAudible;

        /// <summary>Ganancia del ambiente (0-1): la de la música, con su fundido y su atenuación.</summary>
        public float AmbientGain => MusicGain;

        /// <summary>Ganancia de un animal sobre el pico de su efecto (<see cref="AmbientPlan.FaunaGainDb"/>).</summary>
        public float FaunaGain => AmbientGain * (float)AudioAnalysis.FromDb(AmbientPlan.FaunaGainDb);

        /// <summary>Lo que falta para el próximo animal (ms; tests).</summary>
        public float FaunaLeftMs => _fauna.LeftMs;

        /// <summary>Estado de la música de ahora para la sonda.</summary>
        public MusicReport CurrentMusic => new MusicReport(MusicWorld, MusicLayers, MusicNight, Ducked || ActionDucked, MusicMenu);

        /// <summary>
        /// Pide el lecho del mundo <paramref name="world"/> (0 = silencio) con <paramref name="layers"/> capas de percusión
        /// (recortadas a 0-3) y, con <paramref name="night"/>, su variante nocturna.
        /// </summary>
        public void SetMusic(int world, int layers, bool night)
        {
            MusicWorld = Math.Max(0, world);
            MusicLayers = MusicWorld > 0 ? Math.Max(0, Math.Min(AudioConfig.MusicPercussionLayersMax, layers)) : 0;
            MusicNight = MusicWorld > 0 && night;
            MusicMenu = false;
        }

        /// <summary>Pide el tema de menú.</summary>
        public void SetMenuMusic()
        {
            MusicWorld = 0;
            MusicLayers = 0;
            MusicNight = false;
            MusicMenu = true;
        }

        /// <summary>
        /// Pide el ambiente del mundo <paramref name="world"/> (0 = ninguno), con o sin lluvia, y arranca el reloj de su fauna
        /// sembrado con el nivel <paramref name="level"/>.
        /// </summary>
        public void SetAmbient(int world, bool rain, int level)
        {
            AmbientWorld = Math.Max(0, world);
            AmbientRain = AmbientWorld > 0 && rain;
            _fauna.Reset(AmbientWorld, level);
        }

        /// <summary>
        /// Avanza el reloj de la fauna <paramref name="dtMs"/> ms (parado durante un anuncio y sin ambiente): true si toca un
        /// animal (<paramref name="id"/>). Quien lo llama lo hace sonar solo si <see cref="AmbientAudible"/>.
        /// </summary>
        public bool AdvanceFauna(float dtMs, out SfxId id)
        {
            if (AdActive || FocusPaused || AmbientWorld == 0)
            {
                id = default;
                return false;
            }

            return _fauna.Advance(dtMs, out id);
        }

        /// <summary>
        /// Como <see cref="AdvanceFauna(float, out SfxId)"/>, eligiendo entre los <paramref name="choices"/> animales grabados del
        /// mundo (tarea AU3): true si toca uno (<paramref name="pick"/>).
        /// </summary>
        public bool AdvanceFauna(float dtMs, int choices, out int pick)
        {
            if (AdActive || FocusPaused || AmbientWorld == 0)
            {
                pick = 0;
                return false;
            }

            return _fauna.Advance(dtMs, choices, out pick);
        }

        /// <summary>
        /// La sonda <c>AF_STATE music</c> cambió desde la última vez que se tomó (la primera vez, siempre): devuelve el estado
        /// nuevo en <paramref name="report"/> y lo da por escrito.
        /// </summary>
        public bool TakeMusicReport(out MusicReport report)
        {
            report = CurrentMusic;
            if (_hasReported && report.SameAs(_reported))
            {
                return false;
            }

            _reported = report;
            _hasReported = true;
            return true;
        }

        /// <summary>Empieza o termina un anuncio: empieza callando todo; al terminar, el fundido arranca desde 0.</summary>
        public void SetAdActive(bool active)
        {
            if (active)
            {
                _adFade = 0f;
            }

            AdActive = active;
        }

        /// <summary>
        /// Cambia el foco de audio (observación 10 de la jornada 6): con <see cref="AudioFocusState.LossTransient"/> o
        /// <see cref="AudioFocusState.Loss"/> se calla todo y, al volver, el fundido arranca desde 0 como tras un anuncio; con
        /// <see cref="AudioFocusState.Duck"/>, la música y el ambiente bajan a <see cref="FocusDuckGain"/>.
        /// </summary>
        public void SetFocus(AudioFocusState focus)
        {
            if (AudioFocusStates.Pauses(focus))
            {
                _adFade = 0f;
            }

            Focus = focus;
        }

        /// <summary>Suena la toada sin largo conocido: el lecho baja durante <see cref="ToadaDuckMs"/>.</summary>
        public void ToadaStarted()
        {
            _toadaLeftMs = ToadaDuckMs;
        }

        /// <summary>
        /// Suena la toada con un clip de <paramref name="lengthMs"/> ms (tarea FX7: <c>AudioClip.length</c> del WAV o del
        /// respaldo por código): el lecho baja y los remates esperan hasta el final exacto del clip. Un largo no positivo (o no
        /// finito) cuenta como <see cref="ToadaDuckMs"/>.
        /// </summary>
        public void ToadaStarted(float lengthMs)
        {
            _toadaLeftMs = lengthMs > 0f && !float.IsInfinity(lengthMs) ? lengthMs : ToadaDuckMs;
        }

        /// <summary>
        /// Empieza (<paramref name="pouring"/>) o termina el último chorro que caía (observación 23 de la jornada 6): la música baja
        /// <see cref="ActionDuckDb"/> mientras cae y sigue abajo <see cref="ActionDuckHoldMs"/> tras cortarse.
        /// </summary>
        public void SetPouring(bool pouring)
        {
            if (_pouring && !pouring)
            {
                _actionHoldMs = Math.Max(_actionHoldMs, ActionDuckHoldMs);
            }

            _pouring = pouring;
        }

        /// <summary>Suena un sellado: la música baja <see cref="ActionDuckDb"/> durante <see cref="SealDuckMs"/> (observación 23 de la jornada 6).</summary>
        public void SealStarted()
        {
            _actionHoldMs = Math.Max(_actionHoldMs, SealDuckMs);
        }

        /// <summary>Avanza el fundido y las atenuaciones <paramref name="dtMs"/> ms.</summary>
        public void Tick(float dtMs)
        {
            if (!_pouring && _actionHoldMs > 0f)
            {
                _actionHoldMs = Math.Max(0f, _actionHoldMs - dtMs);
            }

            _actionDuck = ActionDuckWanted
                ? Math.Min(1f, _actionDuck + (dtMs / ActionDuckAttackMs))
                : Math.Max(0f, _actionDuck - (dtMs / ActionDuckReleaseMs));

            if (!AdActive && !FocusPaused && _adFade < 1f)
            {
                _adFade = Math.Min(1f, _adFade + (dtMs / AdFadeMs));
            }

            if (_toadaLeftMs > 0f)
            {
                _toadaLeftMs = Math.Max(0f, _toadaLeftMs - dtMs);
            }
        }

        /// <summary>
        /// Capas de percusión del lecho (07 §9.3): entra la primera al sellar la primera cuia y gana una cada 25 % de las cuias
        /// selladas, hasta <paramref name="maxLayers"/> (<c>audio_music_percussion_layers</c>).
        /// </summary>
        public static int PercussionLayers(int sealedCuias, int toSeal, int maxLayers)
        {
            if (sealedCuias <= 0 || toSeal <= 0 || maxLayers <= 0)
            {
                return 0;
            }

            int quarters = (4 * Math.Min(sealedCuias, toSeal)) / toSeal;
            return Math.Min(maxLayers, 1 + quarters);
        }
    }

    /// <summary>
    /// Ticks del contador de semillas (07 §9.2: «1 por 10 semillas, máx. 20 ticks»): uno cada 10 semillas que cuenta la cifra y,
    /// si la cuenta es larga, uno cada 1/20 de ella. Sin asignaciones.
    /// </summary>
    public struct SeedTicker
    {
        /// <summary>Semillas por tick.</summary>
        public const int SeedsPerTick = 10;

        /// <summary>Ticks como mucho por cuenta.</summary>
        public const int MaxTicks = 20;

        private int _from;
        private int _step;
        private int _done;
        private int _total;

        /// <summary>Empieza una cuenta de <paramref name="from"/> a <paramref name="to"/>.</summary>
        public void Begin(int from, int to)
        {
            int span = Math.Max(0, to - from);
            _from = from;
            _step = Math.Max(SeedsPerTick, (span + MaxTicks - 1) / MaxTicks);
            _total = Math.Min(MaxTicks, span / _step);
            _done = 0;
        }

        /// <summary>Ticks que tocan al llegar la cifra a <paramref name="value"/> (0 o más; los de esta llamada).</summary>
        public int Advance(int value)
        {
            int due = _step <= 0 ? 0 : Math.Min(_total, Math.Max(0, value - _from) / _step);
            int ticks = Math.Max(0, due - _done);
            _done = Math.Max(_done, due);
            return ticks;
        }

        /// <summary>Ticks que lleva la cuenta.</summary>
        public int Done => _done;

        /// <summary>Ticks que dará la cuenta entera.</summary>
        public int Total => _total;
    }
}
