#nullable enable
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// El acabado del Mapa v2 de la cinta (D-069, M2-1; propuesta del Mapa v2 §3.1, capas 1-3 y §9.3): las bandas de la várzea
    /// (<see cref="RibbonPart.Varzea"/>), de las orillas (<see cref="RibbonPart.Banks"/>) y del agua con canal profundo y la franja
    /// del Encuentro (<see cref="RibbonPart.Core"/> con <see cref="SetLive"/>). Cada banda lleva un color en cada borde (color por
    /// vértice), sobre el material de UI por defecto: sin shader propio. Sin asignaciones al reconstruir.
    /// </summary>
    public sealed partial class RiverRibbonGraphic
    {
        /// <summary>Canal del centro del agua: el color del agua por este factor (propuesta del Mapa v2 §3.1, capa 3: «centro × 0,75»).</summary>
        public const float DeepChannelFactor = 0.75f;

        /// <summary>La franja de la segunda agua de una pareja se funde con la primera en sus últimos dp [P] (propuesta §3.1: unos 150 dp).</summary>
        public const float MeetingFadeDp = 150f;

        /// <summary>Como mucho, la fusión ocupa esta fracción de la franja (las franjas cortas no quedan de un solo color) [P].</summary>
        public const float MeetingFadeMaxFraction = 0.6f;

        /// <summary>Línea de hierba donde no hay playa ni barranco (dp) [P].</summary>
        public const float GrassLineDp = 1.5f;

        /// <summary>Rampa de entrada y salida de un tramo de orilla, en fracción de su largo [P].</summary>
        public const float BankRamp = 0.25f;

        /// <summary>Opacidad de la várzea junto al agua [P]; hacia fuera se apaga a 0.</summary>
        public const float VarzeaAlpha = 0.5f;

        /// <summary>Várzea, más clara que la tierra firme [P] (guía visual: selva brillante, D-003).</summary>
        public static readonly Color VarzeaLive = Palette.Hex(0x6DB15A);

        /// <summary>Arena de la playa junto al agua [P].</summary>
        public static readonly Color BeachInner = Palette.Hex(0xF1DFAE);

        /// <summary>Arena de la playa por fuera, hacia la hierba [P].</summary>
        public static readonly Color BeachOuter = Palette.Hex(0xD8BC82);

        /// <summary>Barranco de tierra roja junto al agua [P].</summary>
        public static readonly Color BluffInner = Palette.Hex(0xB65A3A);

        /// <summary>Barranco por fuera, el estrato oscuro [P].</summary>
        public static readonly Color BluffOuter = Palette.Hex(0x7E3A26);

        /// <summary>Hierba de la orilla [P].</summary>
        public static readonly Color GrassBank = Palette.Hex(0x4F9440);

        // La várzea: una banda que se apaga hacia fuera a cada lado y el trozo bajo el agua (lo tapa el río).
        private void VarzeaBand(int k, float half, out float u0, out float u1, out Color c0, out Color c1)
        {
            float outer = 1f + (_bandDp / half);
            Color solid = VarzeaLive;
            solid.a = VarzeaAlpha;
            Color clear = VarzeaLive;
            clear.a = 0f;
            switch (k)
            {
                case 0:
                    u0 = -outer;
                    u1 = -1f;
                    c0 = clear;
                    c1 = solid;
                    return;
                case 1:
                    u0 = -1f;
                    u1 = 1f;
                    c0 = solid;
                    c1 = solid;
                    return;
                default:
                    u0 = 1f;
                    u1 = outer;
                    c0 = solid;
                    c1 = clear;
                    return;
            }
        }

        // Las orillas: la banda 0, la derecha mirando río abajo (de −1 hacia fuera); la 1, la izquierda (de +1 hacia fuera).
        private void BankBand(int k, float half, out float u0, out float u1, out Color c0, out Color c1)
        {
            bool left = k == 1;
            BankAt(left, out float width, out Color inner, out Color outer);
            float w = width / half;
            if (left)
            {
                u0 = 1f;
                u1 = 1f + w;
                c0 = inner;
                c1 = outer;
            }
            else
            {
                u0 = -1f - w;
                u1 = -1f;
                c0 = outer;
                c1 = inner;
            }
        }

        /// <summary>
        /// La orilla <paramref name="left"/> (mirando río abajo) de la línea en la sección que se está dibujando: el tramo de
        /// <c>banks</c> más ancho que la cubre, con su rampa (<see cref="BankRamp"/>), o la línea de hierba. Sin asignaciones.
        /// </summary>
        private void BankAt(bool left, out float width, out Color inner, out Color outer)
        {
            width = GrassLineDp;
            inner = GrassBank;
            outer = GrassBank;
            RiverBanks? banks = _banks;
            RiverLine? line = _line;
            if (banks == null || line == null || _arcLength <= 0f)
            {
                return;
            }

            float s = _sectionArc / _arcLength;
            int first = banks.FirstOf(line.Index);
            int count = banks.CountOf(line.Index);
            float best = 0f;
            RiverBankKind kind = RiverBankKind.Beach;
            for (int i = first; i >= 0 && i < first + count; i++)
            {
                RiverBank bank = banks.At(i);
                if (bank.S0 > s)
                {
                    break; // van por s0
                }

                if (bank.Left != left || s > bank.S1)
                {
                    continue;
                }

                float t = (s - bank.S0) / Mathf.Max(1e-6f, bank.S1 - bank.S0);
                float ramp = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) / BankRamp);
                float w = bank.WidthDp * ramp;
                if (w > best)
                {
                    best = w;
                    kind = bank.Kind;
                }
            }

            if (best <= GrassLineDp)
            {
                return;
            }

            float blend = Mathf.Clamp01((best - GrassLineDp) / 3f);
            width = best;
            Color a = kind == RiverBankKind.Beach ? BeachInner : BluffInner;
            Color b = kind == RiverBankKind.Beach ? BeachOuter : BluffOuter;
            inner = Color.Lerp(GrassBank, a, blend);
            outer = Color.Lerp(GrassBank, b, blend);
        }

        // El agua del Mapa v2: dos mitades con el canal del centro oscuro (o las dos aguas de una pareja, con su orilla clara) y el
        // reflejo de la 1.0 encima.
        private void LiveCoreBand(RunLook look, int k, float half, float up, out float u0, out float u1, out Color c0, out Color c1)
        {
            if (k == 2)
            {
                CoreBand(look, look.Split ? 2 : 1, half, up, out u0, out u1, out c0);
                c1 = c0;
                return;
            }

            Color left = BodyColor(look.A);
            Color right = left;
            if (look.Split)
            {
                right = BodyColor(look.B);
                float t = MeetingFade(out Color after);
                left = Color.Lerp(left, after, t);
                right = Color.Lerp(right, after, t);
            }
            if (k == 0)
            {
                u0 = -1f;
                u1 = 0f;
                c0 = right;
                c1 = Deep(right);
                return;
            }

            u0 = 0f;
            u1 = 1f;
            c0 = Deep(left);
            c1 = left;
        }

        // Cuánto se ha fundido ya la pareja de aguas en la sección (0-1) y el agua que sigue río abajo: en los últimos MeetingFadeDp dp
        // de su franja, las dos aguas van hacia ella (el Negro se apaga en el Solimões; propuesta del Mapa v2 §3.1, capa 3).
        private float MeetingFade(out Color after)
        {
            after = Color.clear;
            RiverLine? line = _line;
            if (line == null || _arcLength <= 0f || _space.DpPerUnit <= 0f)
            {
                return 0f;
            }

            float s = _sectionArc / _arcLength;
            if (!line.TryGetSplitAt(Mathf.Min(1f, s), out RiverSplit split))
            {
                return 0f;
            }

            RiverWater next = line.WaterAt(Mathf.Min(1f, split.S1 + 1e-4f));
            if (next == RiverWater.None)
            {
                return 0f;
            }

            after = BodyColor(next);
            float span = (split.S1 - split.S0) * _arcLength;
            float fade = Mathf.Min(MeetingFadeDp / _space.DpPerUnit, span * MeetingFadeMaxFraction);
            float start = (split.S1 * _arcLength) - fade;
            return fade <= 0f || _sectionArc <= start ? 0f : Mathf.SmoothStep(0f, 1f, (_sectionArc - start) / fade);
        }

        private static Color Deep(Color c)
        {
            return new Color(c.r * DeepChannelFactor, c.g * DeepChannelFactor, c.b * DeepChannelFactor, c.a);
        }
    }
}
