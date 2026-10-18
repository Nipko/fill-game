#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>Los 5 remates musicales cortos (<c>music/stingers/</c>; encargo de sonidos §8; tarea AU3).</summary>
    public enum StingerId
    {
        /// <summary><c>sting_world_banner</c>: banner de mundo nuevo, bajo la piha.</summary>
        WorldBanner = 0,

        /// <summary><c>sting_arrival</c>: llegada a una parada (niveles 10k).</summary>
        Arrival = 1,

        /// <summary><c>sting_fruit_new</c>: tarjeta «Fruta nueva» (tarea F5), con <c>sfx_fruit_new</c>.</summary>
        FruitNew = 2,

        /// <summary><c>sting_mech_card</c>: tarjeta previa de una mecánica nueva.</summary>
        MechCard = 3,

        /// <summary><c>sting_level_failed</c>: Nivel fallido, sin dramatismo.</summary>
        LevelFailed = 4,
    }

    /// <summary>Intensidad de la lluvia de un nivel (<c>amb/rain_light</c>, <c>rain_medium</c> y <c>rain_heavy</c>; tarea AU3).</summary>
    public enum RainIntensity
    {
        /// <summary><c>amb_rain_light</c>.</summary>
        Light = 0,

        /// <summary><c>amb_rain_medium</c>.</summary>
        Medium = 1,

        /// <summary><c>amb_rain_heavy</c>.</summary>
        Heavy = 2,
    }

    /// <summary>Cómo se importa y se reproduce un clip de la entrega (tarea AU3; 08 §6; encargo de sonidos §11).</summary>
    public enum AudioClipClass
    {
        /// <summary>Efectos, notas de fruta, bucles del vertido y fauna: mono, «Decompress On Load», Vorbis de calidad alta.</summary>
        Effect = 0,

        /// <summary>Agua, lluvia y tema de menú: «Streaming», Vorbis q0,4 (una sola fuente, sin sincronía con otras).</summary>
        Stream = 1,

        /// <summary>
        /// Lecho del mundo, sus 3 capas y la variante nocturna: «Compressed In Memory», Vorbis q0,4, para que arranquen y se
        /// repitan juntos a la muestra con <c>PlayScheduled</c> (el streaming no lo garantiza).
        /// </summary>
        MusicLayer = 2,

        /// <summary>Remates: «Compressed In Memory», Vorbis q0,4, cargados de antemano (son cortos y suenan sin espera).</summary>
        Stinger = 3,
    }

    /// <summary>
    /// Los 173 clips de la entrega 1 (<c>docs/audio/especificacion-sonidos.json</c>, campos <c>id</c> y <c>file</c>; encargo de
    /// sonidos §3) con un número de ranura fijo, sin motor: así el registro de clips (<c>AF.Audio.Unity.AudioClipRegistry</c>) los
    /// busca sin asignar y sin cadenas. Las ranuras 0-88 son las de <see cref="AudioBank.Index"/> (los 61 efectos y las 14 notas de
    /// <c>sfx_select</c> y de <c>sfx_tint</c>), así que un efecto se busca igual en el registro y en el audio por código de
    /// respaldo. Después: los 4 bucles del vertido, el agua de los mundos 1-9, la lluvia en 3 intensidades, la fauna de cada mundo,
    /// la música de cada mundo (lecho, 3 capas y variante nocturna), el tema de menú y los 5 remates. Los mundos 10-13 no tienen
    /// WAV: suenan con el respaldo por código.
    /// </summary>
    public static class AudioClipIds
    {
        /// <summary>Clips de la entrega 1.</summary>
        public const int Count = 173;

        /// <summary>Mundos con agua, fauna y música grabadas (1-9).</summary>
        public const int Worlds = 9;

        /// <summary>Bucles del vertido, por nivel de la cuia de destino.</summary>
        public const int PourLoops = 4;

        /// <summary>Capas de percusión de cada lecho.</summary>
        public const int PercussionLayers = 3;

        /// <summary>Remates.</summary>
        public const int Stingers = 5;

        /// <summary>Primera ranura de los bucles del vertido (<c>sfx_pour_loop_l1</c>).</summary>
        public const int PourLoopBase = AudioBank.ClipCount;

        /// <summary>Primera ranura del agua (<c>amb_w01_water</c>).</summary>
        public const int WaterBase = PourLoopBase + PourLoops;

        /// <summary>Primera ranura de la lluvia (<c>amb_rain_light</c>).</summary>
        public const int RainBase = WaterBase + Worlds;

        /// <summary>Primera ranura de la fauna (<c>amb_w01_fauna_01</c>).</summary>
        public const int FaunaBase = RainBase + 3;

        /// <summary>Animales grabados en total.</summary>
        public const int FaunaTotal = 17;

        /// <summary>Primera ranura de la música (<c>mus_w01_bed</c>); cada mundo tiene <see cref="MusicPerWorld"/>.</summary>
        public const int MusicBase = FaunaBase + FaunaTotal;

        /// <summary>Piezas de música por mundo: lecho, 3 capas y la variante nocturna.</summary>
        public const int MusicPerWorld = 2 + PercussionLayers;

        /// <summary>Ranura del tema de menú.</summary>
        public const int MenuSlot = MusicBase + (Worlds * MusicPerWorld);

        /// <summary>Primera ranura de los remates.</summary>
        public const int StingerBase = MenuSlot + 1;

        /// <summary>Carpeta de la entrega en el proyecto.</summary>
        public const string Folder = "Assets/_Project/Audio/";

        /// <summary>
        /// Clave de cada fruta por su índice de motor (1-14; 0 = sin fruta): la del nombre de sus notas
        /// (<c>sfx_select_&lt;clave&gt;</c>). El mismo orden que <see cref="SfxCatalog.FruitFrequency"/>.
        /// </summary>
        private static readonly string[] FruitKeys =
        {
            string.Empty,
            "guanabana",
            "cupuacu",
            "tapereba",
            "aguaje",
            "urucum",
            "camu_camu",
            "bacaba",
            "acai",
            "jenipapo",
            "agua_tapajos",
            "lulo",
            "araza",
            "cacao",
            "agua_negro",
        };

        // Animales grabados de cada mundo (amb/fauna/wNN_fauna_KK; especificación, «ambient»).
        private static readonly int[] FaunaPerWorld = { 3, 2, 2, 2, 1, 3, 2, 1, 1 };

        private static readonly string[] StingerNames =
        {
            "sting_world_banner",
            "sting_arrival",
            "sting_fruit_new",
            "sting_mech_card",
            "sting_level_failed",
        };

        private static readonly string[] Ids = new string[Count];
        private static readonly string[] Files = new string[Count];
        private static readonly AudioClipClass[] Classes = new AudioClipClass[Count];
        private static readonly int[] FaunaFirst = new int[Worlds + 1];

        static AudioClipIds()
        {
            for (int i = 0; i < SfxCatalog.Count; i++)
            {
                string name = SfxCatalog.Name((SfxId)i);
                Set(i, name, "sfx/" + name + ".wav", AudioClipClass.Effect);
            }

            for (int t = 1; t <= SfxCatalog.FruitCount; t++)
            {
                string select = "sfx_select_" + FruitKeys[t];
                string tint = "sfx_tint_" + FruitKeys[t];
                Set(AudioBank.SelectFruitBase + t - 1, select, "sfx/fruit/" + select + ".wav", AudioClipClass.Effect);
                Set(AudioBank.TintFruitBase + t - 1, tint, "sfx/fruit/" + tint + ".wav", AudioClipClass.Effect);
            }

            for (int l = 1; l <= PourLoops; l++)
            {
                string id = "sfx_pour_loop_l" + l;
                Set(PourLoopBase + l - 1, id, "sfx/pour/" + id + ".wav", AudioClipClass.Effect);
            }

            for (int w = 1; w <= Worlds; w++)
            {
                string ww = "w" + w.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                Set(WaterBase + w - 1, "amb_" + ww + "_water", "amb/" + ww + "_water.wav", AudioClipClass.Stream);
            }

            string[] rain = { "light", "medium", "heavy" };
            for (int r = 0; r < rain.Length; r++)
            {
                Set(RainBase + r, "amb_rain_" + rain[r], "amb/rain_" + rain[r] + ".wav", AudioClipClass.Stream);
            }

            int slot = FaunaBase;
            for (int w = 1; w <= Worlds; w++)
            {
                FaunaFirst[w - 1] = slot;
                string ww = "w" + w.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                for (int k = 1; k <= FaunaPerWorld[w - 1]; k++)
                {
                    string kk = k.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                    Set(slot, "amb_" + ww + "_fauna_" + kk, "amb/fauna/" + ww + "_fauna_" + kk + ".wav", AudioClipClass.Effect);
                    slot++;
                }
            }

            FaunaFirst[Worlds] = slot;
            for (int w = 1; w <= Worlds; w++)
            {
                string ww = "w" + w.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                Set(BedSlot(w, false), "mus_" + ww + "_bed", "music/" + ww + "_bed.wav", AudioClipClass.MusicLayer);
                for (int l = 1; l <= PercussionLayers; l++)
                {
                    Set(PercussionSlot(w, l), "mus_" + ww + "_perc" + l, "music/" + ww + "_perc" + l + ".wav", AudioClipClass.MusicLayer);
                }

                Set(BedSlot(w, true), "mus_" + ww + "_bed_night", "music/" + ww + "_bed_night.wav", AudioClipClass.MusicLayer);
            }

            Set(MenuSlot, "mus_menu", "music/menu.wav", AudioClipClass.Stream);
            for (int s = 0; s < Stingers; s++)
            {
                Set(StingerBase + s, StingerNames[s], "music/stingers/" + StingerNames[s] + ".wav", AudioClipClass.Stinger);
            }
        }

        /// <summary>Id de la especificación de la ranura <paramref name="slot"/> (<c>sfx_select</c>, <c>mus_w03_bed</c>…).</summary>
        public static string Id(int slot)
        {
            return Ids[Check(slot)];
        }

        /// <summary>Archivo de la ranura, relativo a <see cref="Folder"/> (el campo <c>file</c> de la especificación).</summary>
        public static string File(int slot)
        {
            return Files[Check(slot)];
        }

        /// <summary>Ruta del WAV de la ranura en el proyecto (<c>Assets/_Project/Audio/…</c>).</summary>
        public static string AssetPath(int slot)
        {
            return Folder + Files[Check(slot)];
        }

        /// <summary>Cómo se importa la ranura.</summary>
        public static AudioClipClass ClassOf(int slot)
        {
            return Classes[Check(slot)];
        }

        /// <summary>Ranura del id <paramref name="id"/>, o −1 si no está en la entrega.</summary>
        public static int SlotOf(string? id)
        {
            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(Ids[i], id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Ranura del bucle del vertido <paramref name="loop"/> (0-3: <c>l1</c> casi vacía … <c>l4</c> casi llena).</summary>
        public static int PourLoopSlot(int loop)
        {
            if ((uint)loop >= PourLoops)
            {
                throw new ArgumentOutOfRangeException(nameof(loop), "Bucle del vertido 0-3.");
            }

            return PourLoopBase + loop;
        }

        /// <summary>El mundo <paramref name="world"/> tiene agua, fauna y música grabadas (1-9).</summary>
        public static bool HasWorld(int world)
        {
            return world >= 1 && world <= Worlds;
        }

        /// <summary>Ranura del agua del mundo (1-9).</summary>
        public static int WaterSlot(int world)
        {
            return WaterBase + CheckWorld(world) - 1;
        }

        /// <summary>Ranura de la lluvia de intensidad <paramref name="intensity"/>.</summary>
        public static int RainSlot(RainIntensity intensity)
        {
            int i = (int)intensity;
            if ((uint)i > 2u)
            {
                throw new ArgumentOutOfRangeException(nameof(intensity), "Intensidad de lluvia desconocida.");
            }

            return RainBase + i;
        }

        /// <summary>Animales grabados del mundo <paramref name="world"/> (0 fuera de 1-9).</summary>
        public static int FaunaCount(int world)
        {
            return HasWorld(world) ? FaunaPerWorld[world - 1] : 0;
        }

        /// <summary>Ranura del animal <paramref name="index"/> (0 … <see cref="FaunaCount"/> − 1) del mundo.</summary>
        public static int FaunaSlot(int world, int index)
        {
            CheckWorld(world);
            if ((uint)index >= (uint)FaunaPerWorld[world - 1])
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Animal fuera del mundo.");
            }

            return FaunaFirst[world - 1] + index;
        }

        /// <summary>Ranura del lecho del mundo (con <paramref name="night"/>, su variante nocturna).</summary>
        public static int BedSlot(int world, bool night)
        {
            int first = MusicBase + ((CheckWorld(world) - 1) * MusicPerWorld);
            return night ? first + 1 + PercussionLayers : first;
        }

        /// <summary>Ranura de la capa de percusión <paramref name="layer"/> (1-3) del mundo.</summary>
        public static int PercussionSlot(int world, int layer)
        {
            if (layer < 1 || layer > PercussionLayers)
            {
                throw new ArgumentOutOfRangeException(nameof(layer), "Capa de percusión 1-3.");
            }

            return MusicBase + ((CheckWorld(world) - 1) * MusicPerWorld) + layer;
        }

        /// <summary>Ranura del remate.</summary>
        public static int StingerSlot(StingerId stinger)
        {
            int s = (int)stinger;
            if ((uint)s >= Stingers)
            {
                throw new ArgumentOutOfRangeException(nameof(stinger), "Remate desconocido.");
            }

            return StingerBase + s;
        }

        private static void Set(int slot, string id, string file, AudioClipClass cls)
        {
            Ids[slot] = id;
            Files[slot] = file;
            Classes[slot] = cls;
        }

        private static int Check(int slot)
        {
            if ((uint)slot >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Ranura de clip 0-172.");
            }

            return slot;
        }

        private static int CheckWorld(int world)
        {
            if (!HasWorld(world))
            {
                throw new ArgumentOutOfRangeException(nameof(world), "Mundo grabado 1-9.");
            }

            return world;
        }
    }
}
