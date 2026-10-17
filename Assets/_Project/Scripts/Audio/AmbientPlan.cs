#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Qué suena de ambiente en cada nivel (tarea AU2; 07 §9.1: agua del mundo, lluvia con una probabilidad por nivel y fauna
    /// cada 20-45 s), sin motor y determinista: la lluvia de un nivel sale siempre igual (un hash entero de su número), y la fauna
    /// de un nivel, en el mismo orden (<see cref="FaunaClock"/>, sembrado con el nivel). Sin asignaciones.
    /// </summary>
    public static class AmbientPlan
    {
        /// <summary>Espera mínima entre dos animales (ms; 07 §9.1: «eventos aleatorios cada 20-45 s»).</summary>
        public const float FaunaMinMs = 20000f;

        /// <summary>Espera máxima entre dos animales (ms).</summary>
        public const float FaunaMaxMs = 45000f;

        /// <summary>
        /// Atenuación de un animal respecto al pico de su efecto (dB) [P]: la fauna es parte del lecho de ambiente, por debajo
        /// del juego, y no un aviso (la información nunca va solo por audio, 02 §12).
        /// </summary>
        public const float FaunaGainDb = -10f;

        // Probabilidad de lluvia por nivel de cada mundo, en % (07 §9.1; índice 0 = mundo 1).
        private static readonly int[] RainPercent = { 20, 10, 25, 50, 30, 40, 20, 30, 20, 15, 10, 10, 10 };

        // Animales de cada mundo con los efectos de AU1 que encajan (07 §9.1). Nunca la piha (sfx_world_banner): solo va en el
        // banner de mundo, nunca en bucle (la lista, §7). Sin efecto que encaje (el 6: ranas, grillos y búho; del 10 al 13),
        // no hay animales hasta las grabaciones (07 §9.5) [P].
        private static readonly SfxId[][] Fauna =
        {
            new[] { SfxId.Confetti },                    // 1 Estuario: garzas (aleteo)
            new[] { SfxId.MapRosa },                     // 2 Tapajós: exhalación de Rosa
            new[] { SfxId.MapRosa, SfxId.Confetti },     // 3 Manaos: botos y guacamayos
            new[] { SfxId.Confetti },                    // 4 Trapecio: pericos al atardecer
            new[] { SfxId.MapRosa },                     // 5 Loreto: el pirarucú que respira
            Array.Empty<SfxId>(),                        // 6 Oriente: ranas, grillos y búho (sin efecto que encaje)
            new[] { SfxId.Confetti },                    // 7 Madre de Dios: guacamayos de la collpa
            new[] { SfxId.Confetti },                    // 8 Madidi: coro de aves de altura
            new[] { SfxId.MapRosa },                     // 9 Ucayali: el paiche
        };

        /// <summary>Probabilidad de lluvia de un nivel del mundo <paramref name="world"/> (0-1; 07 §9.1). 0 fuera de 1-13.</summary>
        public static double RainProbability(int world)
        {
            return world >= 1 && world <= RainPercent.Length ? RainPercent[world - 1] / 100.0 : 0.0;
        }

        /// <summary>
        /// El nivel <paramref name="level"/> del mundo <paramref name="world"/> tiene lluvia: un número fijo del nivel en [0, 1)
        /// (<see cref="LevelUnit"/>) por debajo de su probabilidad por <paramref name="scale"/> (<c>audio_ambient_rain_scale</c>,
        /// 0-2), con tope 1. Siempre igual para el mismo nivel.
        /// </summary>
        public static bool HasRain(int level, int world, double scale)
        {
            double s = double.IsNaN(scale) ? 0.0 : Math.Max(0.0, Math.Min(AudioConfig.AmbientRainScaleMax, scale));
            double p = Math.Min(1.0, RainProbability(world) * s);
            return p > 0.0 && LevelUnit(level) < p;
        }

        /// <summary>Parte de los niveles con lluvia que la tienen ligera [P] (tarea AU3).</summary>
        public const double RainLightShare = 0.5;

        /// <summary>Parte de los niveles con lluvia que la tienen ligera o media [P]; el resto, fuerte (tarea AU3).</summary>
        public const double RainMediumShare = 0.85;

        /// <summary>
        /// Intensidad de la lluvia del nivel <paramref name="level"/> (tarea AU3) [P]: <c>rain_light</c>, <c>rain_medium</c> o
        /// <c>rain_heavy</c> en proporción 50/35/15, con otro número fijo del nivel (<see cref="LevelUnit"/> de un número derivado),
        /// independiente del que decide si llueve: subir <c>audio_ambient_rain_scale</c> hace llover en más niveles, pero un nivel
        /// que ya llovía conserva su intensidad. Siempre igual para el mismo nivel.
        /// </summary>
        public static RainIntensity RainIntensityOf(int level)
        {
            double u = LevelUnit(unchecked((level * 31) + 0x5EED));
            return u < RainLightShare ? RainIntensity.Light : u < RainMediumShare ? RainIntensity.Medium : RainIntensity.Heavy;
        }

        /// <summary>Número fijo del nivel en [0, 1): el finalizador de splitmix32 sobre el número del nivel.</summary>
        public static double LevelUnit(int level)
        {
            uint x = unchecked((uint)level * 0x9E3779B9u);
            x ^= x >> 16;
            x = unchecked(x * 0x85EBCA6Bu);
            x ^= x >> 13;
            x = unchecked(x * 0xC2B2AE35u);
            x ^= x >> 16;
            return x / 4294967296.0;
        }

        /// <summary>Animales que pueden sonar en el mundo <paramref name="world"/> (vacío si ninguno; no se modifica).</summary>
        public static SfxId[] FaunaOf(int world)
        {
            return world >= 1 && world <= Fauna.Length ? Fauna[world - 1] : Array.Empty<SfxId>();
        }
    }

    /// <summary>
    /// Reloj de la fauna de un nivel (07 §9.1): espera 20-45 s, suena un animal del mundo y vuelve a esperar. Sembrado con el
    /// nivel y el mundo, así que un nivel da siempre la misma secuencia. Valor (no clase): sin asignaciones.
    /// </summary>
    public struct FaunaClock
    {
        private XorShift32 _rng;
        private float _leftMs;
        private int _world;

        /// <summary>Mundo del reloj (0 = ninguno: nunca suena).</summary>
        public int World => _world;

        /// <summary>Lo que falta para el próximo animal (ms).</summary>
        public float LeftMs => _leftMs;

        /// <summary>Empieza el reloj del nivel <paramref name="level"/> del mundo <paramref name="world"/> (0 = sin ambiente).</summary>
        public void Reset(int world, int level)
        {
            _world = world;
            _rng = new XorShift32(((double)(uint)level * 131.0) + (world * 7919.0) + 17.0);
            _leftMs = NextDelay();
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/>: true si toca un animal (<paramref name="id"/>), y el reloj vuelve a esperar 20-45 s.
        /// Sin animales en el mundo, nunca.
        /// </summary>
        public bool Advance(float dtMs, out SfxId id)
        {
            id = default;
            SfxId[] fauna = AmbientPlan.FaunaOf(_world);
            if (!Advance(dtMs, fauna.Length, out int pick))
            {
                return false;
            }

            id = fauna[pick];
            return true;
        }

        /// <summary>
        /// Como <see cref="Advance(float, out SfxId)"/>, pero eligiendo entre <paramref name="choices"/> animales (los grabados
        /// del mundo, tarea AU3): true si toca uno (<paramref name="pick"/>, 0 … <paramref name="choices"/> − 1). Con 0, nunca.
        /// </summary>
        public bool Advance(float dtMs, int choices, out int pick)
        {
            pick = 0;
            if (choices <= 0 || dtMs <= 0f || _world == 0)
            {
                return false;
            }

            _leftMs -= dtMs;
            if (_leftMs > 0f)
            {
                return false;
            }

            pick = Math.Min(choices - 1, (int)(_rng.Next() * choices));
            _leftMs = NextDelay();
            return true;
        }

        private float NextDelay()
        {
            return AmbientPlan.FaunaMinMs + (float)(_rng.Next() * (AmbientPlan.FaunaMaxMs - AmbientPlan.FaunaMinMs));
        }
    }
}
