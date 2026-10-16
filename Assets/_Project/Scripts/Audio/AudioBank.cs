#nullable enable
using System;
using System.Diagnostics;

namespace AF.Audio
{
    /// <summary>
    /// Todo el PCM de los efectos (tarea AU1), generado de una vez al arrancar en un hilo de trabajo (<see cref="Build"/>): los 61
    /// efectos de <see cref="SfxCatalog"/> (con <c>sfx_pour_loop</c> como bucle continuo, <see cref="PourLoopSynth"/>), las 14
    /// frutas de <c>sfx_select</c> y <c>sfx_tint</c> y la variante grave de bajar la selección. Cada clip tiene su índice fijo
    /// (<see cref="Index"/>), así que el juego lo busca sin asignar. Se guarda en una caché estática: la generación se paga una
    /// vez por proceso.
    /// </summary>
    public sealed class AudioBank
    {
        /// <summary>Frutas por efecto afinado, con el 0 (sin fruta: la nota del laboratorio).</summary>
        public const int FruitSlots = SfxCatalog.FruitCount + 1;

        /// <summary>Primer índice de <c>sfx_select</c> por fruta (1-14).</summary>
        public const int SelectFruitBase = SfxCatalog.Count;

        /// <summary>Primer índice de <c>sfx_tint</c> por fruta (1-14).</summary>
        public const int TintFruitBase = SelectFruitBase + SfxCatalog.FruitCount;

        /// <summary>Primer índice de la variante de bajar la selección (fruta 0-14).</summary>
        public const int SelectDownBase = TintFruitBase + SfxCatalog.FruitCount;

        /// <summary>Clips en total (104).</summary>
        public const int ClipCount = SelectDownBase + FruitSlots;

        private static readonly object s_lock = new object();
        private static AudioBank? s_cached;

        private readonly float[][] _pcm;
        private readonly float[] _gainDb;

        private AudioBank(float[][] pcm, float[] gainDb, double buildMs)
        {
            _pcm = pcm;
            _gainDb = gainDb;
            BuildMs = buildMs;
            long samples = 0;
            for (int i = 0; i < pcm.Length; i++)
            {
                samples += pcm[i].Length;
            }

            Samples = samples;
        }

        /// <summary>Lo que costó generar todo (ms, en el hilo que lo generó).</summary>
        public double BuildMs { get; }

        /// <summary>Muestras en total (memoria PCM = 4 bytes cada una en float, 2 en el clip de 16 bits).</summary>
        public long Samples { get; }

        /// <summary>La caché, si ya se generó.</summary>
        public static AudioBank? Cached
        {
            get
            {
                lock (s_lock)
                {
                    return s_cached;
                }
            }
        }

        /// <summary>Índice del clip de <paramref name="id"/> con la fruta <paramref name="tint"/> (los no afinados ignoran la fruta).</summary>
        public static int Index(SfxId id, int tint)
        {
            if (tint >= 1 && tint <= SfxCatalog.FruitCount)
            {
                if (id == SfxId.Select)
                {
                    return SelectFruitBase + (tint - 1);
                }

                if (id == SfxId.Tint)
                {
                    return TintFruitBase + (tint - 1);
                }
            }

            return (int)id;
        }

        /// <summary>Índice de la variante de bajar la selección con la fruta <paramref name="tint"/>.</summary>
        public static int SelectDownIndex(int tint)
        {
            return SelectDownBase + (tint >= 1 && tint <= SfxCatalog.FruitCount ? tint : 0);
        }

        /// <summary>El efecto de un índice (la variante de bajar la selección es <see cref="SfxId.Select"/>).</summary>
        public static SfxId IdOf(int index)
        {
            if (index < SfxCatalog.Count)
            {
                return (SfxId)index;
            }

            return index < TintFruitBase || index >= SelectDownBase ? SfxId.Select : SfxId.Tint;
        }

        /// <summary>Fruta de un índice (0 si no es una variante por fruta).</summary>
        public static int TintOf(int index)
        {
            if (index < SelectFruitBase)
            {
                return 0;
            }

            if (index < TintFruitBase)
            {
                return index - SelectFruitBase + 1;
            }

            if (index < SelectDownBase)
            {
                return index - TintFruitBase + 1;
            }

            return index - SelectDownBase;
        }

        /// <summary>PCM del clip <paramref name="index"/> (mono, 44,1 kHz; no se modifica).</summary>
        public float[] Pcm(int index)
        {
            return _pcm[index];
        }

        /// <summary>Pico del clip <paramref name="index"/> (dBFS): el <c>gain_db</c> de la sonda.</summary>
        public float GainDb(int index)
        {
            return _gainDb[index];
        }

        /// <summary>
        /// Genera todos los clips (o devuelve la caché). Pensado para un hilo de trabajo: no toca el motor. Determinista: dos
        /// generaciones dan las mismas muestras.
        /// </summary>
        public static AudioBank Build()
        {
            AudioBank? cached = Cached;
            if (cached != null)
            {
                return cached;
            }

            AudioBank bank = Generate();
            lock (s_lock)
            {
                if (s_cached == null)
                {
                    s_cached = bank;
                }

                return s_cached;
            }
        }

        /// <summary>Genera todos los clips sin mirar la caché (tests de determinismo).</summary>
        public static AudioBank Generate()
        {
            var watch = Stopwatch.StartNew();
            var pcm = new float[ClipCount][];
            var gain = new float[ClipCount];
            for (int i = 0; i < SfxCatalog.Count; i++)
            {
                var id = (SfxId)i;
                pcm[i] = id == SfxId.PourLoop ? PourLoopSynth.Render() : SfxMaster.Render(id);
            }

            for (int t = 1; t <= SfxCatalog.FruitCount; t++)
            {
                pcm[SelectFruitBase + t - 1] = SfxMaster.Render(SfxId.Select, t);
                pcm[TintFruitBase + t - 1] = SfxMaster.Render(SfxId.Tint, t);
            }

            for (int t = 0; t <= SfxCatalog.FruitCount; t++)
            {
                pcm[SelectDownBase + t] = SfxMaster.RenderSelectDown(t);
            }

            for (int i = 0; i < ClipCount; i++)
            {
                gain[i] = (float)Math.Round(AudioAnalysis.PeakDb(pcm[i]), 1);
            }

            watch.Stop();
            return new AudioBank(pcm, gain, watch.Elapsed.TotalMilliseconds);
        }
    }
}
