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
                default: throw new ArgumentOutOfRangeException(nameof(kind), "Háptico desconocido.");
            }
        }

        /// <summary>Intensidad de la lista (§7): sellado ligero, victoria, Capi y Boto medios, Atrás bloqueado corto.</summary>
        public static HapticStrength StrengthOf(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Seal: return HapticStrength.Light;
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

    /// <summary>
    /// Reglas de mezcla de 07 §9.4 y 02 §4, sin motor: qué suena y a qué ganancia en cada momento. Sonido y Música se apagan por
    /// separado y al momento; el modo silencio del teléfono (y el de vibración) calla los efectos y la música [P: también calla
    /// la música]; en silencio total tampoco vibra; durante un anuncio se calla todo y al terminar vuelve con un fundido de
    /// <see cref="AdFadeMs"/>; el lecho baja <see cref="DuckDb"/> durante la toada y los modales. Sin asignaciones.
    /// </summary>
    public sealed class AudioMix
    {
        /// <summary>Fundido de vuelta tras un anuncio (ms; 07 §9.4).</summary>
        public const float AdFadeMs = 500f;

        /// <summary>Atenuación del lecho durante la toada y los modales (dB; 07 §9.4).</summary>
        public const float DuckDb = -6f;

        /// <summary>Lo que dura la atenuación por la toada (ms): su largo (2,25 s).</summary>
        public const float ToadaDuckMs = 2250f;

        private float _adFade = 1f;
        private float _toadaLeftMs;

        /// <summary>Ajustes de Ajustes.</summary>
        public SoundSettings Settings { get; set; } = SoundSettings.Defaults;

        /// <summary>Modo del timbre del teléfono.</summary>
        public RingerMode Ringer { get; set; } = RingerMode.Normal;

        /// <summary>Hay un anuncio delante (simulado hoy, D-008).</summary>
        public bool AdActive { get; private set; }

        /// <summary>Hay un modal que atenúa el lecho (Aguas estancadas y el embudo).</summary>
        public bool ModalDuck { get; set; }

        /// <summary>Los efectos se pueden oír ahora (sin contar el fundido).</summary>
        public bool SfxAudible => Settings.Sound && Ringer == RingerMode.Normal && !AdActive;

        /// <summary>La música se puede oír ahora.</summary>
        public bool MusicAudible => Settings.Music && Ringer == RingerMode.Normal && !AdActive;

        /// <summary>Se puede vibrar ahora (Vibración encendida, sin anuncio y el teléfono no en silencio total).</summary>
        public bool HapticsAllowed => Settings.Haptics && Ringer != RingerMode.Silent && !AdActive;

        /// <summary>Ganancia de los efectos (0-1): 0 sin sonido; tras un anuncio, el fundido.</summary>
        public float SfxGain => SfxAudible ? _adFade : 0f;

        /// <summary>Ganancia de la música (0-1), con la atenuación de la toada y de los modales.</summary>
        public float MusicGain => MusicAudible ? _adFade * (Ducked ? DuckGain : 1f) : 0f;

        /// <summary>El lecho está atenuado.</summary>
        public bool Ducked => ModalDuck || _toadaLeftMs > 0f;

        /// <summary>Ganancia lineal de <see cref="DuckDb"/>.</summary>
        public static float DuckGain => (float)AudioAnalysis.FromDb(DuckDb);

        /// <summary>Empieza o termina un anuncio: empieza callando todo; al terminar, el fundido arranca desde 0.</summary>
        public void SetAdActive(bool active)
        {
            if (active)
            {
                _adFade = 0f;
            }

            AdActive = active;
        }

        /// <summary>Suena la toada: el lecho baja durante <see cref="ToadaDuckMs"/>.</summary>
        public void ToadaStarted()
        {
            _toadaLeftMs = ToadaDuckMs;
        }

        /// <summary>Avanza el fundido y la atenuación <paramref name="dtMs"/> ms.</summary>
        public void Tick(float dtMs)
        {
            if (!AdActive && _adFade < 1f)
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
