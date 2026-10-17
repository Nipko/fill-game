#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Servicio de audio y hápticos del juego (tarea AU1): lo implementa <c>AF.Audio.Unity.UnityAudioService</c> en el juego
    /// (pool de <c>AudioSource</c>, clips generados por código, hápticos por JNI en Android) y <see cref="FakeAudioService"/> en los
    /// tests. Nadie lo llama directamente: los ganchos pasan por <see cref="AudioCues"/>. Ninguna llamada asigna memoria.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Suena <paramref name="id"/> (afinado a la fruta <paramref name="tint"/> si el efecto lo va).</summary>
        void Play(SfxId id, int tint);

        /// <summary>Empieza el chorro <paramref name="voice"/> (bucle), con el destino lleno en <paramref name="level"/> (0-1).</summary>
        void PourBegin(int voice, float level);

        /// <summary>El destino del chorro <paramref name="voice"/> está lleno en <paramref name="level"/>: el tono lo sigue.</summary>
        void PourLevel(int voice, float level);

        /// <summary>Se corta el chorro <paramref name="voice"/>.</summary>
        void PourEnd(int voice);

        /// <summary>Vibra <paramref name="kind"/> si Vibración está encendida.</summary>
        void Haptic(HapticKind kind);

        /// <summary>Ajustes de Sonido, Música y Vibración: se aplican al momento (lo que suena se calla).</summary>
        void Configure(SoundSettings settings);

        /// <summary>Empieza o termina un anuncio: todo se calla y vuelve con un fundido de 0,5 s.</summary>
        void SetAdActive(bool active);

        /// <summary>Un modal del nivel está abierto: el lecho baja 6 dB.</summary>
        void SetModalDuck(bool on);

        /// <summary>
        /// El lecho del mundo <paramref name="world"/> (0 = ninguno) con <paramref name="layers"/> capas de percusión y, con
        /// <paramref name="night"/>, su variante nocturna (Muy difícil; tarea AU2).
        /// </summary>
        void SetMusic(int world, int layers, bool night);

        /// <summary>El tema de menú del inicio (tarea AU2).</summary>
        void SetMenuMusic();

        /// <summary>
        /// El ambiente del mundo <paramref name="world"/> (0 = ninguno), con o sin lluvia, y la fauna sembrada con el nivel
        /// <paramref name="level"/> (tarea AU2; 07 §9.1).
        /// </summary>
        void SetAmbient(int world, bool rain, int level);

        /// <summary>
        /// Suena el remate <paramref name="id"/> (tarea AU3): bajo los efectos, con el ajuste Música y nunca encima de la toada
        /// (si suena, espera a que termine). Sin clip grabado, no suena nada.
        /// </summary>
        void PlayStinger(StingerId id);

        /// <summary>Lo de cada fotograma (fundidos, sondeo del modo silencio).</summary>
        void Tick(float dtMs);
    }

    /// <summary>
    /// Vibración del teléfono (02 §4; 07 §9.2): en Android, los <c>VibrationEffect</c> predefinidos por JNI
    /// (<c>AF.Audio.Unity.AndroidHaptics</c>); en el Editor y en los tests, <see cref="NullHaptics"/>. Quien lo llama ya comprobó
    /// el ajuste Vibración (<see cref="AudioMix.HapticsAllowed"/>).
    /// </summary>
    public interface IHaptics
    {
        /// <summary>Vibra con la fuerza de <paramref name="kind"/> (<see cref="Haptics.StrengthOf"/>).</summary>
        void Vibrate(HapticKind kind);
    }

    /// <summary>Sin vibración (Editor, tests y teléfonos sin vibrador).</summary>
    public sealed class NullHaptics : IHaptics
    {
        /// <summary>La instancia.</summary>
        public static readonly NullHaptics Instance = new NullHaptics();

        private NullHaptics()
        {
        }

        /// <inheritdoc />
        public void Vibrate(HapticKind kind)
        {
        }
    }

    /// <summary>Sonda de desarrollo del audio (la implementa <c>AF.UI.DevProbe</c>): <c>AF_STATE sfx</c> y <c>AF_STATE haptic</c>.</summary>
    public interface IAudioProbe
    {
        /// <summary>
        /// Sonó <paramref name="id"/> con un pico de <paramref name="gainDb"/> dBFS (afinado a <paramref name="tint"/>), desde el clip
        /// grabado (<paramref name="clip"/>; tarea AU3) o desde el audio por código de respaldo.
        /// </summary>
        void Sfx(string id, float gainDb, int tint, bool clip);

        /// <summary>Vibró <paramref name="kind"/> (<c>haptic_seal</c>…).</summary>
        void Haptic(string kind);

        /// <summary>
        /// Se construyeron los clips (<c>AF_PERF audio_build</c>): ms en el hilo de trabajo, ms en el hilo principal, clips y
        /// memoria PCM (KiB).
        /// </summary>
        void Build(double workerMs, double mainMs, int clips, long pcmKiB);

        /// <summary>
        /// Cambió la música (<c>AF_STATE music</c>, tarea AU2): mundo del lecho (0 = ninguno), capas de percusión, variante
        /// nocturna, atenuación de 6 dB y tema de menú; y, desde AU3, el id del clip del lecho o del tema de menú que suena
        /// (<paramref name="clip"/>: <c>mus_w03_bed</c>, <c>mus_menu</c>…, <see cref="AudioProbeIds.Synth"/> con el respaldo por
        /// código y vacío sin música).
        /// </summary>
        void Music(in MusicReport report, string clip);

        /// <summary>
        /// Se pidió el ambiente de un nivel (<c>AF_STATE ambient</c>, tarea AU2): su mundo (0 = ninguno) y si llueve; desde AU3,
        /// el id del clip del agua (<paramref name="waterClip"/>) y de la lluvia (<paramref name="rainClip"/>), con
        /// <see cref="AudioProbeIds.Synth"/> para el respaldo por código y vacío si no hay.
        /// </summary>
        void Ambient(int world, bool rain, int level, string waterClip, string rainClip);
    }

    /// <summary>Servicio mudo: el de partida, antes de que el arranque instale uno (y el de los tests que no miran el audio).</summary>
    public sealed class NullAudioService : IAudioService
    {
        /// <summary>La instancia.</summary>
        public static readonly NullAudioService Instance = new NullAudioService();

        private NullAudioService()
        {
        }

        /// <inheritdoc />
        public void Play(SfxId id, int tint)
        {
        }

        /// <inheritdoc />
        public void PourBegin(int voice, float level)
        {
        }

        /// <inheritdoc />
        public void PourLevel(int voice, float level)
        {
        }

        /// <inheritdoc />
        public void PourEnd(int voice)
        {
        }

        /// <inheritdoc />
        public void Haptic(HapticKind kind)
        {
        }

        /// <inheritdoc />
        public void Configure(SoundSettings settings)
        {
        }

        /// <inheritdoc />
        public void SetAdActive(bool active)
        {
        }

        /// <inheritdoc />
        public void SetModalDuck(bool on)
        {
        }

        /// <inheritdoc />
        public void SetMusic(int world, int layers, bool night)
        {
        }

        /// <inheritdoc />
        public void SetMenuMusic()
        {
        }

        /// <inheritdoc />
        public void SetAmbient(int world, bool rain, int level)
        {
        }

        /// <inheritdoc />
        public void PlayStinger(StingerId id)
        {
        }

        /// <inheritdoc />
        public void Tick(float dtMs)
        {
        }
    }

    /// <summary>Valores fijos de la sonda del audio (tarea AU3), sin asignar.</summary>
    public static class AudioProbeIds
    {
        /// <summary>El clip del campo <c>clip</c> sale del audio por código de respaldo (no hay WAV para él).</summary>
        public const string Synth = "synth";
    }

    /// <summary>
    /// Fachada única de los ganchos de audio (tarea AU1): cada gancho del juego es una línea, <c>AudioCues.Play(SfxId.Seal)</c> o
    /// similar, que va al servicio instalado (<see cref="Install"/>; mudo hasta entonces). Estática y sin asignaciones, para
    /// poder llamarla desde la vista (AF.Presentation) y la UI sin que ninguna conozca la implementación. La información nunca va
    /// solo por audio (02 §12): cada sonido acompaña algo que ya se ve.
    /// </summary>
    public static class AudioCues
    {
        private static IAudioService s_service = NullAudioService.Instance;

        /// <summary>El servicio instalado.</summary>
        public static IAudioService Service => s_service;

        /// <summary>
        /// Servicio que el arranque instala en lugar del real (tests PlayMode: <see cref="FakeAudioService"/>). Null: el real.
        /// </summary>
        public static IAudioService? Override { get; set; }

        /// <summary>
        /// Cuenta de sonidos pedidos (también los que no suenan por los ajustes): el toque de un botón solo suena si su acción no
        /// pidió otro (<see cref="TapIfQuiet"/>).
        /// </summary>
        public static int Serial { get; private set; }

        /// <summary>Instala <paramref name="service"/> (null: el mudo).</summary>
        public static void Install(IAudioService? service)
        {
            s_service = service ?? NullAudioService.Instance;
        }

        /// <summary>Suena <paramref name="id"/>.</summary>
        public static void Play(SfxId id)
        {
            Serial++;
            s_service.Play(id, 0);
        }

        /// <summary>Suena <paramref name="id"/> afinado a la fruta <paramref name="tint"/> (índice de motor 1-14).</summary>
        public static void PlayFruit(SfxId id, int tint)
        {
            Serial++;
            s_service.Play(id, tint);
        }

        /// <summary>
        /// Toque de un botón (<c>sfx_ui_tap</c>), solo si desde <paramref name="serialBefore"/> no se pidió ningún otro sonido: así un
        /// botón suena una vez, con el sonido de lo que hace (cerrar, comprar, un comodín…) o, si no tiene, con el toque.
        /// </summary>
        public static void TapIfQuiet(int serialBefore)
        {
            if (Serial == serialBefore)
            {
                Play(SfxId.UiTap);
            }
        }

        /// <summary>Atrás que funcionó (<c>sfx_ui_back</c>), solo si lo que cerró no pidió ya su sonido desde <paramref name="serialBefore"/>.</summary>
        public static void BackIfQuiet(int serialBefore)
        {
            if (Serial == serialBefore)
            {
                Play(SfxId.UiBack);
            }
        }

        /// <summary>Cuia sellada: <c>sfx_seal</c> y su háptico ligero (02 §4; 07 §9.2).</summary>
        public static void Seal()
        {
            Play(SfxId.Seal);
            s_service.Haptic(HapticKind.Seal);
        }

        /// <summary>Empieza el chorro <paramref name="voice"/>: <c>sfx_pour_start</c> y el bucle.</summary>
        public static void PourBegin(int voice, float level)
        {
            Play(SfxId.PourStart);
            s_service.PourBegin(voice, level);
        }

        /// <summary>El destino del chorro <paramref name="voice"/> sube a <paramref name="level"/>.</summary>
        public static void PourLevel(int voice, float level)
        {
            s_service.PourLevel(voice, level);
        }

        /// <summary>Se corta el chorro <paramref name="voice"/>: el bucle termina y suena <c>sfx_pour_end</c>.</summary>
        public static void PourEnd(int voice)
        {
            s_service.PourEnd(voice);
            Play(SfxId.PourEnd);
        }

        /// <summary>El chorro <paramref name="voice"/> se suelta antes de su corte (un salto): el bucle se calla, sin <c>sfx_pour_end</c>.</summary>
        public static void PourCut(int voice)
        {
            s_service.PourEnd(voice);
        }

        /// <summary>Vibra <paramref name="kind"/> (si Vibración está encendida).</summary>
        public static void Haptic(HapticKind kind)
        {
            s_service.Haptic(kind);
        }

        /// <summary>Aplica Sonido, Música y Vibración al momento.</summary>
        public static void Configure(bool sound, bool music, bool haptics)
        {
            s_service.Configure(new SoundSettings(sound, music, haptics));
        }

        /// <summary>Empieza o termina un anuncio.</summary>
        public static void SetAdActive(bool active)
        {
            s_service.SetAdActive(active);
        }

        /// <summary>Un modal del nivel se abre o se cierra (el lecho baja o vuelve).</summary>
        public static void SetModalDuck(bool on)
        {
            s_service.SetModalDuck(on);
        }

        /// <summary>
        /// El lecho del mundo <paramref name="world"/> con sus capas de percusión y, con <paramref name="night"/>, la variante
        /// nocturna (Muy difícil).
        /// </summary>
        public static void SetMusic(int world, int layers, bool night)
        {
            s_service.SetMusic(world, Math.Max(0, layers), night);
        }

        /// <summary>
        /// El ambiente del nivel <paramref name="level"/> del mundo <paramref name="world"/> (07 §9.1): el agua del mundo, la lluvia
        /// si le toca (<see cref="AmbientPlan.HasRain"/> con <c>audio_ambient_rain_scale</c>) y su fauna.
        /// </summary>
        public static void LevelAmbient(int world, int level)
        {
            s_service.SetAmbient(world, AmbientPlan.HasRain(level, world, AudioConfig.AmbientRainScale), level);
        }

        /// <summary>
        /// Remate musical <paramref name="id"/> (tarea AU3; encargo de sonidos §8): una línea junto al efecto del mismo momento
        /// (banner de mundo, llegada a la parada, tarjeta previa, Nivel fallido). Suena bajo los efectos y nunca encima de la
        /// toada; no cuenta en <see cref="Serial"/> (no sustituye al sonido de un botón).
        /// </summary>
        public static void Stinger(StingerId id)
        {
            s_service.PlayStinger(id);
        }

        /// <summary>Se abre el inicio (o una pantalla fuera del nivel): el tema de menú, sin ambiente de río (tarea AU2).</summary>
        public static void Home()
        {
            s_service.SetMenuMusic();
            s_service.SetAmbient(0, false, 0);
        }
    }
}
