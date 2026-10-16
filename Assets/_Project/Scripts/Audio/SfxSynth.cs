#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Síntesis «en crudo» de cada uno de los 61 efectos, portada 1:1 del laboratorio (<c>synthesizeSfx</c>, sección 4 de
    /// <c>docs/audio/laboratorio-sonidos.html</c>): las mismas primitivas (<see cref="Dsp"/>), tiempos, frecuencias, amplitudes y
    /// semillas. Lo único que cambia está fuera, en <see cref="SfxMaster"/> (los arreglos de la revisión del 2026-10-16: sin el
    /// <c>tanh</c> global, cada efecto a su pico, graves al 50 % por encima de 300 Hz y la cola de dosel solo donde va) y en
    /// <see cref="PourLoopSynth"/> (el bucle continuo del chorro). Determinista; se llama al construir los clips, en un hilo de
    /// trabajo, nunca por fotograma.
    /// </summary>
    public static class SfxSynth
    {
        /// <summary>Nota por defecto de <c>sfx_select</c> sin fruta: Aguaje, D5 (la del laboratorio).</summary>
        public const double SelectDefaultHz = 587.33;

        /// <summary>Nota por defecto de <c>sfx_tint</c> sin fruta: Lulo, E5 (la del laboratorio).</summary>
        public const double TintDefaultHz = 659.25;

        private const double SR = Dsp.SampleRate;

        /// <summary>
        /// El efecto <paramref name="id"/> tal como lo genera el laboratorio antes de su <c>finalizeBuffer</c> (sin reverberación ni
        /// saturación), con la nota <paramref name="fruitHz"/> si es un efecto afinado (0 = la del laboratorio).
        /// </summary>
        public static float[] RenderRaw(SfxId id, double fruitHz = 0.0)
        {
            float[] buf;
            switch (id)
            {
                // --- §1. TABLERO Y VERTIDO ---
                case SfxId.Select:
                {
                    buf = Dsp.Buffer(0.16);
                    double f = fruitHz > 0.0 ? fruitHz : SelectDefaultHz;
                    Dsp.Bubble(buf, 0.002, f * 0.85, 0.065, 0.34, 1.38);
                    Dsp.Marimba(buf, 0.005, f, 0.125, 0.36, 0.45);
                    break;
                }

                case SfxId.PourStart:
                    buf = Dsp.Buffer(0.22);
                    Dsp.WoodTap(buf, 0.005, 290, 0.06, 0.25, true);
                    Dsp.Bubble(buf, 0.015, 340, 0.08, 0.42, 1.55);
                    Dsp.Bubble(buf, 0.065, 420, 0.07, 0.35, 1.48);
                    Dsp.Bubble(buf, 0.115, 470, 0.06, 0.30, 1.42);
                    break;

                case SfxId.PourLoop:
                {
                    // El sonido de 0,72 s del laboratorio; el juego usa el bucle continuo de PourLoopSynth (revisión del 2026-10-16).
                    const double dur = 0.72;
                    buf = Dsp.Buffer(dur);
                    var rng = new XorShift32(20261015);
                    const int numDrops = 34;
                    for (int k = 0; k < numDrops; k++)
                    {
                        double u = (double)k / numDrops;
                        double t = (u * (dur - 0.07)) + (rng.Next() * 0.012);
                        double baseF = 360.0 + (u * 520.0) + ((rng.Next() - 0.5) * 65.0);
                        double dDur = 0.032 + (rng.Next() * 0.025);
                        double env = Math.Sin(Math.PI * Math.Min(1.0, u * 1.15));
                        Dsp.Bubble(buf, t, baseF, dDur, 0.24 * env, 1.42);
                    }

                    var murmur = new float[buf.Length];
                    for (int i = 0; i < murmur.Length; i++)
                    {
                        double u = (double)i / murmur.Length;
                        murmur[i] = (float)(((rng.Next() * 2.0) - 1.0) * Math.Sin(Math.PI * u) * 0.06);
                    }

                    AddInto(buf, Dsp.Biquad(murmur, BiquadType.Bandpass, 720, 2.2), 0, 1.0);
                    break;
                }

                case SfxId.PourEnd:
                    buf = Dsp.Buffer(0.22);
                    Dsp.Bubble(buf, 0.005, 680, 0.055, 0.35, 1.45);
                    Dsp.Bubble(buf, 0.065, 860, 0.048, 0.26, 1.52);
                    Dsp.Marimba(buf, 0.070, 880, 0.11, 0.18, 0.5);
                    break;

                case SfxId.Illegal:
                    buf = Dsp.Buffer(0.20);
                    Dsp.WoodTap(buf, 0.005, 195, 0.065, 0.48, true);
                    Dsp.WoodTap(buf, 0.078, 168, 0.075, 0.42, true);
                    break;

                case SfxId.Seal:
                    buf = Dsp.Buffer(0.55);
                    Dsp.MaracaShake(buf, 0.005, 0.085, 0.42, 1.00, 24);
                    Dsp.MaracaShake(buf, 0.125, 0.090, 0.52, 1.08, 28);
                    Dsp.WoodTap(buf, 0.235, 175, 0.09, 0.68, true);
                    Dsp.Bubble(buf, 0.242, 520, 0.055, 0.45, 1.65);
                    Dsp.Marimba(buf, 0.245, 523.25, 0.22, 0.32, 0.55);
                    Dsp.Marimba(buf, 0.275, 659.25, 0.22, 0.32, 0.55);
                    Dsp.Marimba(buf, 0.305, 783.99, 0.24, 0.36, 0.65);
                    Dsp.Marimba(buf, 0.335, 1046.50, 0.21, 0.30, 0.70);
                    break;

                case SfxId.RiverRise:
                {
                    const double dur = 0.45;
                    buf = Dsp.Buffer(dur);
                    var rng = new XorShift32(101);
                    var raw = new float[buf.Length];
                    for (int i = 0; i < raw.Length; i++)
                    {
                        double u = (double)i / raw.Length;
                        double env = Math.Sin(Math.PI * u);
                        raw[i] = (float)(((rng.Next() * 2.0) - 1.0) * env * 0.32);
                    }

                    AddInto(buf, Dsp.Biquad(raw, BiquadType.Bandpass, 260, 1.2), 0, 1.0);
                    Dsp.Bubble(buf, 0.12, 220, 0.09, 0.22, 1.3);
                    Dsp.Bubble(buf, 0.24, 290, 0.08, 0.20, 1.35);
                    break;
                }

                case SfxId.Fill:
                {
                    buf = Dsp.Buffer(0.42);
                    var rng = new XorShift32(202);
                    for (int k = 0; k < 18; k++)
                    {
                        double u = k / 18.0;
                        Dsp.Bubble(buf, u * 0.34, 310.0 + (u * 360.0) + (rng.Next() * 40.0), 0.045, 0.28 * Math.Sin(Math.PI * u), 1.45);
                    }

                    break;
                }

                case SfxId.FruitPlip:
                    buf = Dsp.Buffer(0.10);
                    Dsp.Bubble(buf, 0.002, 620, 0.058, 0.42, 1.62);
                    break;

                case SfxId.Tint:
                {
                    buf = Dsp.Buffer(0.14);
                    double f = fruitHz > 0.0 ? fruitHz : TintDefaultHz;
                    Dsp.Marimba(buf, 0.002, f, 0.11, 0.40, 0.65);
                    Dsp.Bubble(buf, 0.004, f * 0.9, 0.045, 0.22, 1.35);
                    break;
                }

                case SfxId.WinToada:
                    buf = WinToada();
                    break;

                case SfxId.Confetti:
                {
                    buf = Dsp.Buffer(1.05);
                    double[] flaps = { 0.04, 0.24, 0.46, 0.70 };
                    double[] freqs = { 620, 740, 680, 820 };
                    for (int k = 0; k < flaps.Length; k++)
                    {
                        Dsp.LeafRustle(buf, flaps[k], 0.18, 0.52 * (1.0 - (k * 0.12)), freqs[k], 1.1);
                    }

                    break;
                }

                case SfxId.Stuck:
                    buf = Dsp.Buffer(1.55);
                    Dsp.Marimba(buf, 0.02, 196.00, 0.55, 0.36, 0.2);
                    Dsp.Marimba(buf, 0.32, 164.81, 0.65, 0.34, 0.2);
                    Dsp.Marimba(buf, 0.65, 130.81, 0.85, 0.38, 0.15);
                    Dsp.Bubble(buf, 0.95, 310, 0.08, 0.25, 1.25);
                    break;

                // --- §2. COMODINES, ECONOMÍA E INTERFAZ ---
                case SfxId.Undo:
                    buf = Dsp.Buffer(0.42);
                    for (int k = 0; k < 10; k++)
                    {
                        double u = k / 10.0;
                        Dsp.Bubble(buf, u * 0.32, 760.0 - (u * 340.0), 0.045, 0.28 * (0.4 + (0.6 * u)), 0.78);
                    }

                    Dsp.Marimba(buf, 0.05, 587.33, 0.18, 0.22, 0.4);
                    Dsp.Marimba(buf, 0.20, 440.00, 0.20, 0.26, 0.4);
                    break;

                case SfxId.CapiPaddle:
                    buf = Dsp.Buffer(0.85);
                    Dsp.LeafRustle(buf, 0.02, 0.08, 0.35, 1400, 1.5);
                    Dsp.LeafRustle(buf, 0.14, 0.08, 0.38, 1550, 1.5);
                    Dsp.WoodTap(buf, 0.25, 220, 0.08, 0.38, true);
                    Dsp.Bubble(buf, 0.28, 340, 0.09, 0.42, 1.45);
                    Dsp.Bubble(buf, 0.35, 460, 0.07, 0.35, 1.40);
                    Dsp.Marimba(buf, 0.48, 1318.5, 0.16, 0.36, 0.9);
                    Dsp.Marimba(buf, 0.51, 1760.0, 0.14, 0.28, 0.9);
                    Dsp.WoodTap(buf, 0.63, 620, 0.035, 0.38, true);
                    Dsp.Bubble(buf, 0.66, 920, 0.075, 0.36, 1.35);
                    break;

                case SfxId.Hint:
                    buf = Dsp.Buffer(0.52);
                    Dsp.LeafRustle(buf, 0.01, 0.09, 0.32, 1100, 1.3);
                    Dsp.LeafRustle(buf, 0.12, 0.09, 0.35, 1250, 1.3);
                    Dsp.LeafRustle(buf, 0.23, 0.09, 0.30, 1180, 1.3);
                    Dsp.Marimba(buf, 0.08, 783.99, 0.30, 0.36, 0.75);
                    Dsp.Marimba(buf, 0.20, 1174.66, 0.30, 0.42, 0.85);
                    break;

                case SfxId.Shuffle:
                {
                    buf = Dsp.Buffer(0.92);
                    Dsp.LeafRustle(buf, 0.01, 0.55, 0.42, 480, 0.9);
                    for (int i = 0; i < LabFruitOrder.Length; i++)
                    {
                        double t = 0.04 + (i * 0.052);
                        double f = LabFruitOrder[i];
                        Dsp.Marimba(buf, t, f, 0.14, 0.28, 0.6);
                        if (i % 2 == 0)
                        {
                            Dsp.Bubble(buf, t, f * 0.7, 0.045, 0.20, 1.45);
                        }
                    }

                    break;
                }

                case SfxId.Reveal:
                    buf = Dsp.Buffer(0.52);
                    Dsp.Marimba(buf, 0.02, 1318.51, 0.26, 0.35, 0.85);
                    Dsp.Marimba(buf, 0.11, 1760.00, 0.32, 0.38, 0.90);
                    Dsp.Bubble(buf, 0.16, 260, 0.12, 0.42, 1.65);
                    Dsp.Bubble(buf, 0.27, 480, 0.09, 0.36, 1.55);
                    break;

                case SfxId.Skip:
                    buf = Dsp.Buffer(1.22);
                    Dsp.Bubble(buf, 0.02, 280, 0.11, 0.45, 1.6);
                    Dsp.Bubble(buf, 0.10, 460, 0.09, 0.40, 1.5);
                    Dsp.LeafRustle(buf, 0.28, 0.32, 0.55, 920, 1.6);
                    Dsp.Marimba(buf, 0.32, 587.33, 0.25, 0.25, 0.6);
                    Dsp.Marimba(buf, 0.44, 880.00, 0.28, 0.28, 0.7);
                    Dsp.LeafRustle(buf, 0.70, 0.22, 0.48, 680, 1.1);
                    Dsp.Bubble(buf, 0.74, 240, 0.12, 0.46, 1.5);
                    Dsp.Bubble(buf, 0.88, 390, 0.09, 0.35, 1.45);
                    Dsp.Bubble(buf, 1.00, 540, 0.07, 0.28, 1.45);
                    break;

                case SfxId.Restart:
                    buf = Dsp.Buffer(0.52);
                    for (int k = 0; k < 6; k++)
                    {
                        Dsp.Bubble(buf, 0.02 + (k * 0.045), 520.0 - (k * 35.0), 0.05, 0.28, 1.3);
                    }

                    Dsp.WoodTap(buf, 0.28, 240, 0.11, 0.52, true);
                    Dsp.WoodTap(buf, 0.36, 320, 0.09, 0.38, false);
                    break;

                case SfxId.RiverDrop:
                    buf = Dsp.Buffer(1.02);
                    for (int k = 0; k < 8; k++)
                    {
                        double u = k / 8.0;
                        Dsp.Bubble(buf, 0.02 + (u * 0.45), 580.0 - (u * 260.0), 0.065, 0.30, 1.25);
                    }

                    Dsp.LeafRustle(buf, 0.42, 0.38, 0.50, 860, 1.5);
                    Dsp.Marimba(buf, 0.50, 392.00, 0.35, 0.28, 0.45);
                    Dsp.Marimba(buf, 0.64, 523.25, 0.36, 0.32, 0.55);
                    break;

                case SfxId.SeedsTick:
                    buf = Dsp.Buffer(0.065);
                    Dsp.WoodTap(buf, 0.002, 920, 0.042, 0.46, false);
                    Dsp.MaracaShake(buf, 0.004, 0.032, 0.28, 1.25, 5);
                    break;

                case SfxId.BasketOpen:
                    buf = Dsp.Buffer(0.82);
                    Dsp.LeafRustle(buf, 0.02, 0.18, 0.42, 1650, 1.4);
                    Dsp.WoodTap(buf, 0.08, 310, 0.07, 0.38, true);
                    for (int k = 0; k < 7; k++)
                    {
                        Dsp.WoodTap(buf, 0.18 + (k * 0.055), 680.0 + (k * 70.0), 0.045, 0.32, false);
                    }

                    Dsp.Marimba(buf, 0.25, 523.25, 0.35, 0.35, 0.55);
                    Dsp.Marimba(buf, 0.35, 659.25, 0.35, 0.35, 0.60);
                    Dsp.Marimba(buf, 0.45, 783.99, 0.35, 0.40, 0.65);
                    break;

                case SfxId.StreakUp:
                {
                    buf = Dsp.Buffer(0.82);
                    Dsp.Bubble(buf, 0.02, 340, 0.08, 0.35, 1.5);
                    Dsp.Bubble(buf, 0.10, 480, 0.08, 0.35, 1.5);
                    int i0 = (int)Math.Floor(0.16 * SR);
                    int len = (int)Math.Floor(0.42 * SR);
                    double ph = 0.0;
                    for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
                    {
                        double u = (double)i / len;
                        double t = i / SR;
                        double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 1.2);
                        double f = (720.0 + (540.0 * Math.Sin(Math.PI * u))) * (1.0 + (0.06 * Math.Sin(2.0 * Math.PI * 32.0 * t)));
                        ph += 2.0 * Math.PI * f / SR;
                        buf[i0 + i] = (float)(buf[i0 + i] + (((Math.Sin(ph) * 0.6) + (Math.Sin(2.0 * ph) * 0.25)) * env * 0.36));
                    }

                    Dsp.Marimba(buf, 0.28, 659.25, 0.28, 0.30, 0.5);
                    Dsp.Marimba(buf, 0.42, 880.00, 0.34, 0.36, 0.6);
                    break;
                }

                case SfxId.StreakShield:
                    buf = Dsp.Buffer(0.62);
                    Dsp.LeafRustle(buf, 0.02, 0.36, 0.48, 920, 1.1);
                    Dsp.WoodTap(buf, 0.12, 165, 0.14, 0.48, true);
                    Dsp.Bubble(buf, 0.22, 320, 0.09, 0.34, 1.35);
                    Dsp.Marimba(buf, 0.24, 523.25, 0.32, 0.30, 0.45);
                    break;

                case SfxId.CanoeLost:
                    buf = Dsp.Buffer(0.62);
                    Dsp.WoodTap(buf, 0.02, 240, 0.10, 0.42, true);
                    Dsp.Bubble(buf, 0.10, 360, 0.09, 0.36, 0.85);
                    Dsp.Marimba(buf, 0.14, 293.66, 0.26, 0.32, 0.25);
                    Dsp.Marimba(buf, 0.28, 220.00, 0.30, 0.34, 0.20);
                    break;

                case SfxId.CanoeBack:
                    buf = Dsp.Buffer(0.62);
                    Dsp.WoodTap(buf, 0.02, 260, 0.08, 0.40, true);
                    Dsp.Bubble(buf, 0.08, 380, 0.08, 0.38, 1.45);
                    Dsp.Bubble(buf, 0.16, 520, 0.07, 0.32, 1.45);
                    Dsp.Marimba(buf, 0.18, 440.00, 0.28, 0.32, 0.45);
                    Dsp.Marimba(buf, 0.30, 587.33, 0.30, 0.36, 0.55);
                    break;

                case SfxId.UiTap:
                    buf = Dsp.Buffer(0.09);
                    Dsp.WoodTap(buf, 0.002, 420, 0.055, 0.45, false);
                    break;

                case SfxId.UiBack:
                    buf = Dsp.Buffer(0.11);
                    Dsp.WoodTap(buf, 0.002, 285, 0.070, 0.45, true);
                    break;

                case SfxId.UiBuy:
                    buf = Dsp.Buffer(0.44);
                    for (int k = 0; k < 6; k++)
                    {
                        Dsp.WoodTap(buf, 0.01 + (k * 0.032), 780.0 + (k * 65.0), 0.038, 0.34, false);
                    }

                    Dsp.MaracaShake(buf, 0.02, 0.14, 0.35, 1.15, 18);
                    Dsp.Marimba(buf, 0.14, 523.25, 0.22, 0.36, 0.55);
                    Dsp.Marimba(buf, 0.22, 783.99, 0.21, 0.42, 0.65);
                    break;

                // --- §3. MECÁNICAS Y PERSONAJES ---
                case SfxId.MechHoja:
                    buf = Dsp.Buffer(0.52);
                    Dsp.LeafRustle(buf, 0.01, 0.36, 0.52, 2100, 1.2);
                    Dsp.Marimba(buf, 0.12, 659.25, 0.25, 0.24, 0.4);
                    break;

                case SfxId.MechMatapi:
                    buf = Dsp.Buffer(0.82);
                    for (int k = 0; k < 5; k++)
                    {
                        Dsp.WoodTap(buf, 0.02 + (k * 0.065), 480.0 + ((k % 2) * 140.0), 0.045, 0.38, false);
                    }

                    Dsp.LeafRustle(buf, 0.34, 0.28, 0.46, 1450, 1.5);
                    Dsp.WoodTap(buf, 0.52, 260, 0.12, 0.46, true);
                    break;

                case SfxId.MechLiana:
                    buf = Dsp.Buffer(0.62);
                    Dsp.Marimba(buf, 0.01, 1046.50, 0.32, 0.44, 0.85);
                    Dsp.WoodTap(buf, 0.16, 340, 0.06, 0.42, false);
                    Dsp.WoodTap(buf, 0.23, 270, 0.08, 0.38, true);
                    Dsp.LeafRustle(buf, 0.26, 0.24, 0.40, 1650, 1.4);
                    break;

                case SfxId.MechLianaCuenta:
                    buf = Dsp.Buffer(0.16);
                    Dsp.WoodTap(buf, 0.003, 540, 0.048, 0.46, false);
                    Dsp.WoodTap(buf, 0.038, 390, 0.055, 0.36, true);
                    break;

                case SfxId.MechRemolino:
                    buf = Dsp.Buffer(0.52);
                    Dsp.Bubble(buf, 0.01, 520, 0.10, 0.40, 0.62);
                    Dsp.Bubble(buf, 0.08, 340, 0.10, 0.42, 0.68);
                    for (int k = 0; k < 7; k++)
                    {
                        Dsp.Bubble(buf, 0.18 + (k * 0.042), 360.0 + (k * 85.0), 0.042, 0.34, 1.52);
                    }

                    break;

                case SfxId.MechBarro:
                    buf = Dsp.Buffer(0.52);
                    Dsp.LeafRustle(buf, 0.01, 0.26, 0.46, 950, 1.1);
                    Dsp.Bubble(buf, 0.05, 310, 0.09, 0.42, 1.45);
                    Dsp.Bubble(buf, 0.14, 490, 0.08, 0.38, 1.50);
                    Dsp.Marimba(buf, 0.24, 1174.66, 0.24, 0.34, 0.85);
                    break;

                case SfxId.MechLluvia:
                    buf = Dsp.Buffer(0.42);
                    Dsp.LeafRustle(buf, 0.01, 0.28, 0.42, 2600, 1.1);
                    Dsp.Bubble(buf, 0.14, 520, 0.085, 0.48, 1.58);
                    Dsp.Bubble(buf, 0.23, 740, 0.060, 0.32, 1.48);
                    break;

                case SfxId.MechPirana:
                    buf = Dsp.Buffer(0.42);
                    Dsp.Bubble(buf, 0.01, 380, 0.065, 0.40, 1.45);
                    Dsp.WoodTap(buf, 0.085, 460, 0.045, 0.52, true);
                    Dsp.WoodTap(buf, 0.125, 340, 0.050, 0.48, true);
                    Dsp.Bubble(buf, 0.18, 420, 0.08, 0.44, 1.55);
                    Dsp.Bubble(buf, 0.26, 610, 0.06, 0.32, 1.45);
                    break;

                case SfxId.MechBrote:
                    buf = Dsp.Buffer(0.46);
                    Dsp.Bubble(buf, 0.015, 440, 0.065, 0.45, 1.65);
                    Dsp.Marimba(buf, 0.03, 587.33, 0.20, 0.32, 0.5);
                    Dsp.LeafRustle(buf, 0.09, 0.22, 0.40, 2200, 1.3);
                    Dsp.Marimba(buf, 0.16, 880.00, 0.24, 0.36, 0.6);
                    break;

                case SfxId.MechFlor:
                    buf = Dsp.Buffer(0.66);
                    Dsp.LeafRustle(buf, 0.02, 0.35, 0.42, 1500, 1.2);
                    Dsp.Bubble(buf, 0.08, 390, 0.09, 0.32, 1.4);
                    Dsp.Marimba(buf, 0.16, 659.25, 0.42, 0.36, 0.75);
                    Dsp.Marimba(buf, 0.26, 987.77, 0.38, 0.40, 0.85);
                    break;

                case SfxId.MechHojaAtada:
                    buf = Dsp.Buffer(0.52);
                    Dsp.WoodTap(buf, 0.01, 610, 0.045, 0.44, false);
                    Dsp.WoodTap(buf, 0.06, 440, 0.055, 0.38, true);
                    Dsp.LeafRustle(buf, 0.11, 0.32, 0.48, 2000, 1.2);
                    Dsp.Marimba(buf, 0.18, 783.99, 0.24, 0.28, 0.55);
                    break;

                case SfxId.TinajaSelect:
                    buf = Dsp.Buffer(0.15);
                    Dsp.WoodTap(buf, 0.003, 190, 0.095, 0.58, true);
                    break;

                case SfxId.TinajaScoop:
                {
                    buf = Dsp.Buffer(0.32);
                    int i0 = (int)Math.Floor(0.01 * SR);
                    int len = (int)Math.Floor(0.24 * SR);
                    double ph = 0.0;
                    for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
                    {
                        double u = (double)i / len;
                        double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 1.8);
                        double f = 185.0 + (210.0 * Math.Pow(u, 1.3));
                        ph += 2.0 * Math.PI * f / SR;
                        buf[i0 + i] = (float)(buf[i0 + i] + (((Math.Sin(ph) * 0.75) + (Math.Sin(2.0 * ph) * 0.25)) * env * 0.48));
                    }

                    Dsp.WoodTap(buf, 0.015, 230, 0.07, 0.32, true);
                    break;
                }

                case SfxId.TinajaReveal:
                    buf = Dsp.Buffer(0.18);
                    Dsp.Bubble(buf, 0.005, 480, 0.075, 0.46, 1.48);
                    Dsp.WoodTap(buf, 0.010, 260, 0.08, 0.24, true);
                    break;

                case SfxId.TinajaRelease:
                    buf = Dsp.Buffer(0.82);
                    Dsp.LeafRustle(buf, 0.02, 0.34, 0.44, 1550, 1.4);
                    Dsp.WoodTap(buf, 0.38, 310, 0.10, 0.52, true);
                    Dsp.WoodTap(buf, 0.50, 245, 0.12, 0.46, true);
                    Dsp.Marimba(buf, 0.52, 587.33, 0.24, 0.28, 0.4);
                    break;

                case SfxId.SlothNod:
                {
                    buf = Dsp.Buffer(0.38);
                    var raw = new float[buf.Length];
                    double ph = 0.0;
                    for (int i = 0; i < raw.Length; i++)
                    {
                        double u = (double)i / raw.Length;
                        double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 1.1);
                        double f = 185.0 + (32.0 * Math.Sin(Math.PI * u));
                        ph += 2.0 * Math.PI * f / SR;
                        raw[i] = (float)(((Math.Sin(ph) * 0.65) + (Math.Sin(2.0 * ph) * 0.25) + (Math.Sin(3.0 * ph) * 0.10)) * env * 0.52);
                    }

                    AddInto(buf, Dsp.Biquad(raw, BiquadType.Lowpass, 620, 1.4), 0, 1.0);
                    break;
                }

                case SfxId.SlothHappy:
                {
                    buf = Dsp.Buffer(0.48);
                    var raw = new float[buf.Length];
                    var rng = new XorShift32(555);
                    double ph = 0.0;
                    for (int i = 0; i < raw.Length; i++)
                    {
                        double u = (double)i / raw.Length;
                        double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 1.2);
                        double f = 240.0 - (45.0 * u);
                        ph += 2.0 * Math.PI * f / SR;
                        double voice = (Math.Sin(ph) * 0.55) + (Math.Sin(2.0 * ph) * 0.25);
                        double breath = ((rng.Next() * 2.0) - 1.0) * 0.18;
                        raw[i] = (float)((voice + breath) * env * 0.46);
                    }

                    AddInto(buf, Dsp.Biquad(raw, BiquadType.Bandpass, 540, 1.5), 0, 1.0);
                    Dsp.LeafRustle(buf, 0.05, 0.28, 0.22, 1800, 1.2);
                    break;
                }

                case SfxId.SlothBranch:
                    buf = Dsp.Buffer(0.38);
                    Dsp.WoodTap(buf, 0.02, 175, 0.09, 0.30, true);
                    Dsp.LeafRustle(buf, 0.03, 0.30, 0.48, 1950, 1.2);
                    break;

                case SfxId.SlothYawn:
                {
                    buf = Dsp.Buffer(0.62);
                    var raw = Dsp.Buffer(0.42);
                    var rng = new XorShift32(777);
                    double ph = 0.0;
                    for (int i = 0; i < raw.Length; i++)
                    {
                        double u = (double)i / raw.Length;
                        double env = Math.Sin(Math.PI * u);
                        double f = 260.0 - (95.0 * u);
                        ph += 2.0 * Math.PI * f / SR;
                        raw[i] = (float)(((Math.Sin(ph) * 0.45) + (((rng.Next() * 2.0) - 1.0) * 0.25)) * env * 0.40);
                    }

                    AddInto(buf, Dsp.Biquad(raw, BiquadType.Bandpass, 580, 1.4), 0, 1.0);
                    Dsp.Bubble(buf, 0.44, 520, 0.035, 0.20, 1.3);
                    Dsp.Bubble(buf, 0.51, 460, 0.035, 0.18, 1.3);
                    break;
                }

                case SfxId.RosaSurface:
                    buf = RosaSurface();
                    break;

                case SfxId.MonkeyChirp:
                {
                    buf = Dsp.Buffer(0.18);
                    int i0 = (int)Math.Floor(0.01 * SR);
                    int len = (int)Math.Floor(0.14 * SR);
                    double ph = 0.0;
                    for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
                    {
                        double u = (double)i / len;
                        double t = i / SR;
                        double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 1.3);
                        double f = (3300.0 + (950.0 * Math.Sin(Math.PI * u))) * (1.0 + (0.045 * Math.Sin(2.0 * Math.PI * 55.0 * t)));
                        ph += 2.0 * Math.PI * f / SR;
                        buf[i0 + i] = (float)(buf[i0 + i] + (Math.Sin(ph) * env * 0.16));
                    }

                    break;
                }

                // --- §4. MAPA DEL RÍO, LLEGADAS Y PANTALLAS ESPECIALES ---
                case SfxId.Fact:
                    buf = Dsp.Buffer(0.34);
                    Dsp.WoodTap(buf, 0.005, 340, 0.075, 0.44, true);
                    Dsp.Marimba(buf, 0.035, 523.25, 0.24, 0.34, 0.75);
                    Dsp.Marimba(buf, 0.085, 783.99, 0.24, 0.38, 0.85);
                    break;

                case SfxId.WorldBanner:
                    buf = WorldBannerPiha();
                    break;

                case SfxId.MapPaddle:
                    buf = Dsp.Buffer(0.82);
                    Dsp.WoodTap(buf, 0.03, 230, 0.08, 0.36, true);
                    Dsp.Bubble(buf, 0.07, 330, 0.09, 0.38, 1.4);
                    Dsp.Bubble(buf, 0.16, 440, 0.07, 0.30, 1.38);
                    Dsp.WoodTap(buf, 0.42, 245, 0.08, 0.36, true);
                    Dsp.Bubble(buf, 0.46, 360, 0.09, 0.38, 1.42);
                    Dsp.Bubble(buf, 0.55, 470, 0.07, 0.30, 1.40);
                    break;

                case SfxId.MapSample:
                    buf = Dsp.Buffer(0.48);
                    Dsp.Bubble(buf, 0.01, 480, 0.06, 0.34, 1.5);
                    Dsp.Bubble(buf, 0.06, 640, 0.06, 0.34, 1.5);
                    Dsp.Marimba(buf, 0.10, 523.25, 0.34, 0.34, 0.85);
                    Dsp.Marimba(buf, 0.15, 659.25, 0.32, 0.34, 0.85);
                    Dsp.Marimba(buf, 0.20, 783.99, 0.30, 0.38, 0.90);
                    break;

                case SfxId.MapRosa:
                {
                    // El laboratorio: el soplo de Rosa a 0,75 (el nivel lo vuelve a fijar SfxMaster, a −21 dBFS).
                    buf = RosaSurface();
                    for (int i = 0; i < buf.Length; i++)
                    {
                        buf[i] = (float)(buf[i] * 0.75);
                    }

                    break;
                }

                case SfxId.MapPequepeque:
                {
                    buf = Dsp.Buffer(0.92);
                    const int puffs = 7;
                    for (int k = 0; k < puffs; k++)
                    {
                        double t = 0.03 + (k * 0.118);
                        double env = Math.Sin(Math.PI * ((k + 0.5) / puffs));
                        Dsp.WoodTap(buf, t, 145.0 + ((k % 2) * 12.0), 0.055, 0.32 * env, true);
                        Dsp.Bubble(buf, t + 0.025, 290, 0.045, 0.20 * env, 1.25);
                    }

                    break;
                }

                case SfxId.FruitNew:
                    buf = Dsp.Buffer(0.72);
                    Dsp.Bubble(buf, 0.015, 540, 0.075, 0.44, 1.6);
                    Dsp.Marimba(buf, 0.09, 523.25, 0.28, 0.36, 0.55);
                    Dsp.Marimba(buf, 0.19, 659.25, 0.28, 0.38, 0.65);
                    Dsp.Marimba(buf, 0.29, 783.99, 0.36, 0.42, 0.70);
                    Dsp.Marimba(buf, 0.39, 1046.50, 0.32, 0.40, 0.80);
                    break;

                case SfxId.MechCard:
                    buf = Dsp.Buffer(0.38);
                    Dsp.WoodTap(buf, 0.01, 260, 0.08, 0.45, true);
                    Dsp.MaracaShake(buf, 0.04, 0.08, 0.34, 1.05, 16);
                    Dsp.Marimba(buf, 0.08, 440.00, 0.24, 0.32, 0.45);
                    break;

                case SfxId.MorphoIdle:
                    buf = Dsp.Buffer(0.38);
                    Dsp.LeafRustle(buf, 0.01, 0.10, 0.18, 1150, 1.3);
                    Dsp.LeafRustle(buf, 0.13, 0.10, 0.18, 1250, 1.3);
                    Dsp.Marimba(buf, 0.08, 783.99, 0.26, 0.20, 0.65);
                    break;

                case SfxId.StoreOpen:
                    buf = Dsp.Buffer(0.58);
                    Dsp.LeafRustle(buf, 0.01, 0.18, 0.38, 1600, 1.3);
                    Dsp.Marimba(buf, 0.09, 1046.50, 0.20, 0.32, 0.9);
                    Dsp.Marimba(buf, 0.16, 1318.51, 0.22, 0.34, 0.9);
                    Dsp.MaracaShake(buf, 0.22, 0.12, 0.32, 1.15, 18);
                    Dsp.Marimba(buf, 0.28, 783.99, 0.26, 0.34, 0.6);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(id), "Efecto desconocido.");
            }

            return buf;
        }

        /// <summary>
        /// Arreglo de graves de la revisión del 2026-10-16 donde no basta con subir armónicos (la revisión: «súbelo con armónicos
        /// (2.º y 3.º) o una octava, conservando el carácter»). Añade a <paramref name="raw"/> las notas de
        /// marimba del efecto una octava más arriba (el remanso de <c>sfx_stuck</c>, sol-mi-do de 196-131 Hz, y la calabaza de
        /// <c>sfx_canoe_lost</c>, 294-220 Hz), con algo menos de nivel, para que la melodía grave se oiga en el altavoz del
        /// teléfono sin perder su caída. Los demás efectos no cambian.
        /// </summary>
        public static void AddOctaveLayer(SfxId id, float[] raw)
        {
            switch (id)
            {
                case SfxId.Stuck:
                    Dsp.Marimba(raw, 0.02, 392.00, 0.55, 0.30, 0.35);
                    Dsp.Marimba(raw, 0.32, 329.63, 0.65, 0.28, 0.35);
                    Dsp.Marimba(raw, 0.65, 261.63, 0.85, 0.32, 0.30);
                    break;
                case SfxId.CanoeLost:
                    Dsp.Marimba(raw, 0.14, 587.33, 0.26, 0.26, 0.30);
                    Dsp.Marimba(raw, 0.28, 440.00, 0.30, 0.28, 0.25);
                    break;
            }
        }

        // Las 14 frutas en el orden del laboratorio (FRUITS: de la más oscura a la más clara), para el glissando de Pororoca.
        private static readonly double[] LabFruitOrder =
        {
            261.63, 293.66, 329.63, 392.00, 440.00, 523.25, 587.33, 659.25, 783.99, 880.00, 1046.50, 1174.66, 1318.51, 1567.98,
        };

        // sfx_win_toada: redoble de curimbó, maracás, melodía pentatónica y coro «aah» sin letra (con dosel).
        private static float[] WinToada()
        {
            float[] buf = Dsp.Buffer(2.25);
            double[] beats = { 0.02, 0.20, 0.38, 0.56, 0.74, 0.92, 1.10, 1.28 };
            double[] pitches = { 130, 165, 130, 185, 130, 165, 145, 196 };
            for (int b = 0; b < beats.Length; b++)
            {
                Dsp.WoodTap(buf, beats[b], pitches[b], 0.11, 0.48, true);
            }

            Dsp.MaracaShake(buf, 0.02, 0.08, 0.35, 1.0, 20);
            Dsp.MaracaShake(buf, 0.38, 0.08, 0.40, 1.05, 22);
            Dsp.MaracaShake(buf, 0.74, 0.08, 0.38, 1.0, 20);
            Dsp.MaracaShake(buf, 1.10, 0.12, 0.48, 1.1, 28);
            double[,] mel =
            {
                { 0.02, 523.25, 0.25 }, { 0.20, 587.33, 0.25 }, { 0.38, 659.25, 0.28 },
                { 0.56, 783.99, 0.28 }, { 0.74, 880.00, 0.32 }, { 1.08, 1046.50, 0.95 },
            };
            for (int k = 0; k < mel.GetLength(0); k++)
            {
                double t = mel[k, 0];
                double f = mel[k, 1];
                double d = mel[k, 2];
                Dsp.Marimba(buf, t, f, d, 0.42, 0.55);
                Dsp.Marimba(buf, t, f * 0.5, d * 1.1, 0.28, 0.25);
            }

            int choirStart = (int)Math.Floor(0.85 * SR);
            int choirLen = (int)Math.Floor(1.30 * SR);
            var choirRaw = new float[choirLen];
            double[] chord = { 261.63, 329.63, 392.00, 523.25 };
            for (int i = 0; i < choirLen; i++)
            {
                double t = i / SR;
                double u = (double)i / choirLen;
                double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 0.8);
                double vib = 1.0 + (0.004 * Math.Sin(2.0 * Math.PI * 5.5 * t));
                double s = 0.0;
                for (int c = 0; c < chord.Length; c++)
                {
                    double w = 2.0 * Math.PI * chord[c] * vib * t;
                    s += (Math.Sin(w) * 0.5) + (Math.Sin(2.0 * w) * 0.35) + (Math.Sin(3.0 * w) * 0.2) + (Math.Sin(4.0 * w) * 0.1);
                }

                choirRaw[i] = (float)(s * env * 0.14);
            }

            float[] f1 = Dsp.Biquad(choirRaw, BiquadType.Bandpass, 720, 2.5);
            float[] f2 = Dsp.Biquad(choirRaw, BiquadType.Bandpass, 1220, 3.0);
            for (int i = 0; i < choirLen && (choirStart + i) < buf.Length; i++)
            {
                buf[choirStart + i] = (float)(buf[choirStart + i] + ((double)f1[i] + (f2[i] * 0.7)));
            }

            return buf;
        }

        // sfx_rosa_surface: ondulación, soplo del espiráculo (síntesis provisional por código, no una grabación) y chapoteo.
        private static float[] RosaSurface()
        {
            float[] buf = Dsp.Buffer(1.02);
            Dsp.Bubble(buf, 0.02, 260, 0.09, 0.38, 1.45);
            int breathLen = (int)Math.Floor(0.36 * SR);
            var breathRaw = new float[breathLen];
            var rng = new XorShift32(888);
            for (int i = 0; i < breathLen; i++)
            {
                double u = (double)i / breathLen;
                double attack = Math.Min(1.0, i / (0.015 * SR));
                double env = attack * Math.Exp(-u * 2.4);
                breathRaw[i] = (float)(((rng.Next() * 2.0) - 1.0) * env * 0.72);
            }

            float[] chuff1 = Dsp.Biquad(breathRaw, BiquadType.Bandpass, 520, 1.8);
            float[] chuff2 = Dsp.Biquad(breathRaw, BiquadType.Bandpass, 1350, 2.2);
            int bStart = (int)Math.Floor(0.10 * SR);
            for (int i = 0; i < breathLen && (bStart + i) < buf.Length; i++)
            {
                buf[bStart + i] = (float)(buf[bStart + i] + ((chuff1[i] * 0.7) + (chuff2[i] * 0.45)));
            }

            Dsp.Bubble(buf, 0.50, 340, 0.09, 0.32, 1.38);
            Dsp.Bubble(buf, 0.64, 420, 0.08, 0.26, 1.35);
            return buf;
        }

        // sfx_world_banner: las tres sílabas de la piha gritona («qui-qui-YOOO»), síntesis provisional por código sobre una brisa.
        private static float[] WorldBannerPiha()
        {
            float[] buf = Dsp.Buffer(1.65);
            PihaSyllable(buf, 0.08, 0.16, 0, 0.38);
            PihaSyllable(buf, 0.36, 0.17, 1, 0.44);
            PihaSyllable(buf, 0.64, 0.72, 2, 0.56);
            Dsp.LeafRustle(buf, 0.02, 1.35, 0.14, 900, 0.9);
            return buf;
        }

        private static void PihaSyllable(float[] buf, double tStart, double sDur, int curve, double amp)
        {
            int i0 = (int)Math.Floor(tStart * SR);
            int len = (int)Math.Floor(sDur * SR);
            double ph = 0.0;
            for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
            {
                double u = (double)i / len;
                double t = i / SR;
                double env = Math.Sin(Math.PI * Math.Min(1.0, u * 1.1)) * Math.Exp(-u * 0.85);
                double f = PihaCurve(curve, u, t);
                ph += 2.0 * Math.PI * f / SR;
                buf[i0 + i] = (float)(buf[i0 + i] + (((Math.Sin(ph) * 0.88) + (Math.Sin(2.0 * ph) * 0.12)) * env * amp));
            }
        }

        private static double PihaCurve(int curve, double u, double t)
        {
            switch (curve)
            {
                case 0:
                    return 1780.0 + (820.0 * Math.Pow(u, 1.4));
                case 1:
                    return 1880.0 + (940.0 * Math.Pow(u, 1.3));
                default:
                {
                    const double peakU = 0.34;
                    double b = u < peakU
                        ? 1650.0 + ((3920.0 - 1650.0) * Math.Sin((u / peakU) * (Math.PI / 2.0)))
                        : 1820.0 + ((3920.0 - 1820.0) * Math.Pow(1.0 - ((u - peakU) / (1.0 - peakU)), 1.55));
                    return b * (1.0 + (0.008 * Math.Sin(2.0 * Math.PI * 26.0 * t)));
                }
            }
        }

        // buf[offset + i] += src[i] · gain (redondeando a float, como el laboratorio).
        internal static void AddInto(float[] buf, float[] src, int offset, double gain)
        {
            for (int i = 0; i < src.Length && (offset + i) < buf.Length; i++)
            {
                buf[offset + i] = (float)(buf[offset + i] + (src[i] * gain));
            }
        }
    }
}
