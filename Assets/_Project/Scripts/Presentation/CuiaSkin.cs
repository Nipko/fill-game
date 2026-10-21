#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Skin de cuia con arte provisional por código (tarea B14; 07 §4.6: la cuia por capas, <c>rim_{skin}</c> y <c>foot_{skin}</c>,
    /// <c>band_{skin}</c> en la zona del hombro, <c>glass_tint_{skin}</c> ≤ 8 % y <c>cap_{skin}</c>; 06 §4.5; glosario §4). Solo
    /// cambia el borde, el pie, la banda del hombro, el tinte del vidrio y la tapa: «una skin jamás cambia la cavidad, las marcas, la
    /// posición del labio ni el brillo» (07 §4.6), así que la cavidad, el líquido y las 4 marcas de dosis no se tocan. Los colores son
    /// [P] (la descripción de material de 07 §4.6 en una tinta plana); el arte final es del artista y, la Cuia de Santarém, del
    /// encargo de 07 §2.2, regla 7. Sin grabados ni patrones: la decoración final es propia y la revisa el asesor (07 §8.1).
    /// </summary>
    public sealed class CuiaSkin
    {
        /// <summary>Tope del tinte del vidrio (07 §4.6: «≤ 8 % de opacidad»).</summary>
        public const float MaxGlassTint = 0.08f;

        private CuiaSkin(string id, Color rim, Color foot, Color cap, Color band, Color bandB, bool hasBand, bool bicolor,
            Color glassTint, bool handle)
        {
            Id = id;
            Rim = rim;
            Foot = foot;
            Cap = cap;
            Band = band;
            BandB = bandB;
            HasBand = hasBand;
            BandBicolor = bicolor;
            GlassTint = glassTint;
            HasHandle = handle;
        }

        /// <summary>Cuia de vidrio, la base (07 §4.6, fila 1: fibra de arumã ocre, sin banda, vidrio sin tinte, tapa de madera clara): el vidrio horneado tal cual.</summary>
        public static CuiaSkin Base { get; } = new CuiaSkin("cuia_vidrio", Color.white, Color.white, Color.white, Color.white, Color.white, false, false, Color.clear, false);

        /// <summary>
        /// Cuia de Santarém (07 §4.6, mundo 2): borde y pie de calabaza ennegrecida (cumatê), línea fina marfil en el hombro, vidrio
        /// ámbar al 6 % y tapa de cumatê negro [P]. Sin el grabado: el propio llega con el arte (no se copian las cuias de Aritapera).
        /// </summary>
        public static CuiaSkin Santarem { get; } = new CuiaSkin(
            "af_skin_m02", Palette.Hex(0x2B211C), Palette.Hex(0x2B211C), Palette.Hex(0x3A2C24), Palette.Hex(0xEDE3CC), Palette.Hex(0xEDE3CC),
            true, false, new Color(0xC9 / 255f, 0x8A / 255f, 0x2B / 255f, 0.06f), false);

        /// <summary>
        /// Cuia del Encuentro (07 §4.6, mundo 3): borde y pie de madera oscura, banda bicolor ámbar y beige que no se mezcla (Negro +
        /// Solimões), vidrio sin tinte y tapa de dos tonos (aquí la madera, con la banda debajo) [P].
        /// </summary>
        public static CuiaSkin Encuentro { get; } = new CuiaSkin(
            "af_skin_m03", Palette.Hex(0x5A3A22), Palette.Hex(0x5A3A22), Palette.Hex(0x6B4A2E), Palette.Hex(0xA9672E), Palette.Hex(0xB08A5A),
            true, true, Color.clear, false);

        /// <summary>
        /// Totuma (07 §4.6, mundo 4): borde y pie de calabaza clara, banda de cuero, asa decorativa de cuero en el hombro derecho
        /// (0,4 u), vidrio verdoso al 5 % y tapa de calabaza clara [P].
        /// </summary>
        public static CuiaSkin Totuma { get; } = new CuiaSkin(
            "af_skin_m04", Palette.Hex(0xD8C08A), Palette.Hex(0xD8C08A), Palette.Hex(0xE2CE9E), Palette.Hex(0x8A5A34), Palette.Hex(0x8A5A34),
            true, false, new Color(0x6F / 255f, 0xAF / 255f, 0x6A / 255f, 0.05f), true);

        private static readonly CuiaSkin[] All = { Base, Santarem, Encuentro, Totuma };

        /// <summary>Id de la skin (el SKU del pack; la base, <c>cuia_vidrio</c>).</summary>
        public string Id { get; }

        /// <summary>Es la base: el vidrio horneado sin capas de skin.</summary>
        public bool IsBase => ReferenceEquals(this, Base);

        /// <summary>Color del borde (<c>rim_{skin}</c>).</summary>
        public Color Rim { get; }

        /// <summary>Color del pie (<c>foot_{skin}</c>).</summary>
        public Color Foot { get; }

        /// <summary>Tinte de la tapa de sellado (<c>cap_{skin}</c>; blanco = la de madera clara del arte).</summary>
        public Color Cap { get; }

        /// <summary>Color de la banda del hombro (la mitad izquierda si es bicolor).</summary>
        public Color Band { get; }

        /// <summary>Segundo color de la banda bicolor (la mitad derecha).</summary>
        public Color BandB { get; }

        /// <summary>Lleva banda en el hombro (<c>band_{skin}</c>).</summary>
        public bool HasBand { get; }

        /// <summary>La banda es de dos colores que no se mezclan.</summary>
        public bool BandBicolor { get; }

        /// <summary>Tinte del vidrio: su color y, en la alfa, su opacidad (≤ <see cref="MaxGlassTint"/>; 0 = sin tinte).</summary>
        public Color GlassTint { get; }

        /// <summary>Lleva el asa decorativa en el hombro derecho (Totuma).</summary>
        public bool HasHandle { get; }

        /// <summary>Color del asa (el del cuero de la banda).</summary>
        public Color Handle => Band;

        /// <summary>Multiplicador del vidrio trasero y frontal con su tinte (blanco sin tinte).</summary>
        public Color GlassMultiplier => Color.Lerp(Color.white, new Color(GlassTint.r, GlassTint.g, GlassTint.b, 1f), GlassTint.a);

        /// <summary>
        /// Tinte de la mini cuia que enseña la skin como premio (Mapa y tarjeta; B14): el vidrio blanco con un 35 % del color del borde,
        /// que se lee sobre la madera oscura (con el borde entero, el cumatê de Santarém casi no se veía).
        /// </summary>
        public Color PreviewTint => IsBase ? Color.white : Color.Lerp(Color.white, new Color(Rim.r, Rim.g, Rim.b, 1f), 0.35f);

        /// <summary>La skin de <paramref name="id"/>; la base si es null o no se conoce.</summary>
        public static CuiaSkin For(string? id)
        {
            if (id != null)
            {
                for (int i = 0; i < All.Length; i++)
                {
                    if (string.Equals(All[i].Id, id, StringComparison.Ordinal))
                    {
                        return All[i];
                    }
                }
            }

            return Base;
        }
    }
}
