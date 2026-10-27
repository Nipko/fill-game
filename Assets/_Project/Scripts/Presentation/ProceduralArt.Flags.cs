#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Banderas de cortesía de la canoa del Mapa del río (D-013, punto 5, Q8-A; propuesta del Mapa §5.3; tarea M3.6): la canoa
    /// iza la bandera del país al entrar en él (al empezar y al cambiar de país; campo <c>flag_raise</c> de la parada) en el viaje
    /// de mundo del banner. Solo banderas oficiales, nunca en tierra, en islas ni en zonas reclamadas; el uso comercial de los
    /// símbolos nacionales lo revisa el abogado [NV].
    /// </summary>
    /// <remarks>
    /// <para><b>Regla del arte</b> (D-052, que retira P-M5 de D-023; <see cref="MapArtRule"/>): la ranura <c>map_flag_&lt;iso2&gt;</c>
    /// (<see cref="ArtSlots.MapFlagPrefix"/>, dos letras minúsculas) manda siempre: es la bandera oficial que entrega el artista
    /// con el visto bueno del abogado. Sin ese PNG, la <b>simplificada</b> por código sale con
    /// <see cref="MapArtRule.ShowProvisional"/>, en todos los builds, release incluido; sin ella (un país sin simplificada, o un
    /// test que apaga el arte por código) <see cref="MapFlagFor"/> da null y la canoa viaja sin bandera.</para>
    /// <para>Simplificadas, provisionales y sin escudos [P] (<see cref="SimplifiedFlags"/>): Brasil (el campo verde, el rombo
    /// amarillo y el disco azul, sin la banda ni las estrellas), Colombia (amarillo, azul y rojo, 2:1:1), Perú (la de uso civil,
    /// rojo, blanco y rojo), Ecuador (los colores de Colombia con un disco dorado en el centro en lugar del escudo, para no
    /// confundirlas) y Bolivia (la tricolor civil, rojo, amarillo y verde; D-013, punto 5, con la revisión del abogado y del
    /// asesor). Todas de <see cref="MapFlagWidthDp"/> × <see cref="MapFlagHeightDp"/> dp, con el pivote en el centro del
    /// borde del asta (izquierda). Perezosas (se crean la primera vez que se piden, <see cref="MapFlagsBuilt"/>) y pequeñas:
    /// se quedan hasta <see cref="Dispose"/>.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho de una bandera de la canoa (dp) [P].</summary>
        public const float MapFlagWidthDp = 16f;

        /// <summary>Alto de una bandera de la canoa (dp) [P].</summary>
        public const float MapFlagHeightDp = 11f;

        /// <summary>
        /// Países con bandera simplificada por código (ISO 3166-1 alfa-2, en mayúsculas): los de los viajes de mundo de la 1.0
        /// (D-013, punto 2: Brasil, Colombia, Perú, Ecuador y Bolivia). Los demás, solo con su PNG oficial.
        /// </summary>
        public static readonly string[] SimplifiedFlags = { "BR", "CO", "PE", "EC", "BO" };

        /// <summary>
        /// Ancho al que se pinta el bufeo boliviano de la franja del viaje (dp; la franja lo pinta a 22 dp): el alto sigue las
        /// proporciones de Rosa (<see cref="RosaWidthDp"/> × <see cref="RosaHeightDp"/>).
        /// </summary>
        public const float MapBufeoWidthDp = 32f;

        /// <summary>Cuerpo del bufeo boliviano: gris azulado, poco saturado [P, pendiente del asesor cultural].</summary>
        public static readonly Color BufeoBody = Palette.Hex(0x8C9DB0);

        /// <summary>Dorso del bufeo boliviano, pizarra [P, pendiente del asesor cultural].</summary>
        public static readonly Color BufeoBack = Palette.Hex(0x5E7187);

        /// <summary>Vientre del bufeo boliviano, gris perla [P, pendiente del asesor cultural].</summary>
        public static readonly Color BufeoBelly = Palette.Hex(0xCDD6DF);

        private readonly Sprite?[] _mapFlags = new Sprite?[5];
        private Sprite? _mapBufeo;

        /// <summary>
        /// El bufeo boliviano que guía la canoa en los mundos 7 y 8 en la franja del viaje de mundo (D-013, Q18-A; M3.6, P-M8):
        /// el mismo rig que Rosa (su silueta, <c>RosaSdf</c>; glosario, Rosa: «el mismo rig con otra paleta, sin nombre propio»)
        /// con otra paleta, gris azulada y poco saturada (<see cref="BufeoBody"/>, <see cref="BufeoBack"/> y
        /// <see cref="BufeoBelly"/>) [P, pendiente del asesor cultural], para que el relevo se lea a 22 dp junto a Rosa (revisión
        /// de M3.6: un tinte multiplicado sobre el rosa daba un malva casi igual). Mira a la izquierda, como Rosa. Perezoso (solo
        /// lo pide un viaje con el bufeo) y pequeño: se queda hasta <see cref="Dispose"/>, como las banderas.
        /// </summary>
        public Sprite MapBufeo => _mapBufeo != null ? _mapBufeo : (_mapBufeo = BuildMapBufeo());

        /// <summary>Si ya se pintó el bufeo de la franja.</summary>
        public bool MapBufeoBuilt => _mapBufeo != null;

        /// <summary>Si ya se creó alguna bandera simplificada.</summary>
        public bool MapFlagsBuilt
        {
            get
            {
                for (int i = 0; i < _mapFlags.Length; i++)
                {
                    if (_mapFlags[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Nombre de la ranura de la bandera de <paramref name="iso2"/>: <c>map_flag_&lt;iso2 en minúsculas&gt;</c> (glosario §11).
        /// Es un identificador, no texto visible.
        /// </summary>
        public static string MapFlagSlot(string iso2)
        {
            if (iso2 == null)
            {
                throw new ArgumentNullException(nameof(iso2));
            }

            return ArtSlots.MapFlagPrefix + iso2.ToLowerInvariant();
        }

        /// <summary>El país <paramref name="iso2"/> tiene bandera simplificada por código (<see cref="SimplifiedFlags"/>).</summary>
        public static bool HasSimplifiedFlag(string? iso2)
        {
            return IndexOfSimplifiedFlag(iso2) >= 0;
        }

        /// <summary>
        /// Bandera de <paramref name="iso2"/> para la canoa: su PNG oficial (<see cref="MapFlagSlot"/>) si existe; si no, la
        /// simplificada con <see cref="MapArtRule.ShowProvisional"/> (en todos los builds, D-052); si no (un país sin simplificada,
        /// o el arte por código apagado en un test), null.
        /// </summary>
        public Sprite? MapFlag(string iso2)
        {
            return MapFlagFor(iso2, ArtSlots.Find(MapFlagSlot(iso2)), MapArtRule.ShowProvisional);
        }

        /// <summary>
        /// La regla de <see cref="MapFlag"/> con el PNG de la ranura (<paramref name="officialPng"/>) y la compuerta
        /// (<paramref name="showProvisional"/>) explícitos: el PNG manda; sin él, la simplificada solo con el provisional permitido.
        /// </summary>
        public Sprite? MapFlagFor(string? iso2, Sprite? officialPng, bool showProvisional)
        {
            if (officialPng != null)
            {
                return officialPng;
            }

            int index = IndexOfSimplifiedFlag(iso2);
            if (index < 0 || !MapArtRule.CanShow(showProvisional, false))
            {
                return null;
            }

            Sprite? flag = _mapFlags[index];
            return flag != null ? flag : (_mapFlags[index] = BuildMapFlag(SimplifiedFlags[index]));
        }

        private static int IndexOfSimplifiedFlag(string? iso2)
        {
            if (iso2 == null)
            {
                return -1;
            }

            for (int i = 0; i < SimplifiedFlags.Length; i++)
            {
                if (string.Equals(SimplifiedFlags[i], iso2, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        // Una bandera simplificada en dp (origen abajo a la izquierda): el paño con un borde de tinta fino y esquinas apenas
        // redondeadas, para que se lea sobre el agua, la tierra y la madera; un brillo suave arriba (luz de juego, D-003).
        private Sprite BuildMapFlag(string iso2)
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(MapFlagWidthDp * k);
            int h = Mathf.RoundToInt(MapFlagHeightDp * k);
            var pc = new PixelCanvas(w, h);
            Color ink = new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.85f);
            const float outline = 0.6f * k; // px
            const float radius = 1.2f * k;
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, 0f, 0f, w, h, radius);
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, ink, cov);
                    float inner = PixelCanvas.Coverage(d + outline);
                    if (inner <= 0f)
                    {
                        continue;
                    }

                    Color c = FlagColor(iso2, xd, yd);
                    float glint = Mathf.Clamp01((yd - (MapFlagHeightDp * 0.62f)) / (MapFlagHeightDp * 0.38f));
                    c = Color.Lerp(c, Color.white, 0.12f * glint);
                    pc.Blend(x, y, c, inner);
                }
            }

            return MakeSprite(pc, "map_flag_" + iso2.ToLowerInvariant() + "_simplified", new Vector2(0f, 0.5f), Vector4.zero, k);
        }

        // El bufeo de la franja: la silueta de Rosa (RosaSdf, en unidades del alto) a MapBufeoWidthDp, de juguete como ella (D-058):
        // el vinilo lacado con el volumen de Rosa (RosaColorAt) y la paleta gris azulada en los tonos, el borde fino de su color
        // (con un mínimo para que se lea), la boca y el ojo de juguete. Solo al pedirlo, en el hilo principal: ≈ 6.000 píxeles.
        private Sprite BuildMapBufeo()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(MapBufeoWidthDp * k);
            int h = Mathf.RoundToInt(MapBufeoWidthDp * RosaHeightDp / RosaWidthDp * k);
            var pc = new PixelCanvas(w, h);
            float inv = 1f / h;

            // El borde fino de Rosa (CapiEdgeDp de su sprite de RosaHeightDp) a esta escala, con un mínimo para que se lea.
            float outline = Mathf.Max(1.2f, CapiEdgeDp * h / RosaHeightDp);
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float x = (px + 0.5f) * inv;
                    float y = (py + 0.5f) * inv;
                    float d = RosaSdf(x, y, out float side, out float body, out float head, out Vector2 spineNormal) * h;
                    if (d > outline + 1f)
                    {
                        continue;
                    }

                    pc.Blend(px, py, BufeoToy.Body.Outline, PixelCanvas.Coverage(d - outline));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov > 0f)
                    {
                        pc.Blend(px, py, RosaColorAt(x, y, side, body, head, spineNormal, BufeoToy.Body, BufeoToy.Back, BufeoToy.Belly), cov);
                    }
                }
            }

            pc.FillCapsule(RosaMouthFrom.x * h, RosaMouthFrom.y * h, RosaMouthTo.x * h, RosaMouthTo.y * h, Mathf.Max(0.6f, 0.012f * h), ColorMath.WithAlpha(BufeoToy.Body.Outline, 0.55f));
            DrawToyEye(pc, RosaEyeAt.x * h, RosaEyeAt.y * h, Mathf.Max(1.1f, 0.0275f * h), BufeoIris);
            return MakeSprite(pc, "map_guide_bufeo", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // El bufeo de juguete: los tonos de su paleta gris azulada, una vez (en su propia clase: se inicializan al pintarlo).
        private static class BufeoToy
        {
            public static readonly ToyTones Body = ToyShading.Tones(BufeoBody);
            public static readonly ToyTones Back = ToyShading.Tones(BufeoBack);
            public static readonly ToyTones Belly = ToyShading.Tones(BufeoBelly, 0.7f);
        }

        private static readonly Color BufeoIris = Palette.Hex(0x2A2E3A);

        // Color del paño de la bandera iso2 en el punto (xd, yd) en dp, con el origen abajo a la izquierda.
        private static Color FlagColor(string iso2, float xd, float yd)
        {
            const float fw = MapFlagWidthDp;
            const float fh = MapFlagHeightDp;
            switch (iso2)
            {
                case "BR":
                {
                    // Campo verde, rombo amarillo y disco azul (sin la banda ni las estrellas).
                    float cx = fw * 0.5f;
                    float cy = fh * 0.5f;
                    float disc = Vector2.Distance(new Vector2(xd, yd), new Vector2(cx, cy));
                    if (disc <= fh * 0.24f)
                    {
                        return Palette.Hex(0x002776);
                    }

                    float rx = (fw * 0.5f) - 1.6f;
                    float ry = (fh * 0.5f) - 1.3f;
                    bool rhombus = (Mathf.Abs(xd - cx) / rx) + (Mathf.Abs(yd - cy) / ry) <= 1f;
                    return rhombus ? Palette.Hex(0xFEDF00) : Palette.Hex(0x009B3A);
                }

                case "CO":
                    // Amarillo arriba (la mitad), azul y rojo abajo (un cuarto cada uno).
                    return yd >= fh * 0.5f ? Palette.Hex(0xFCD116) : yd >= fh * 0.25f ? Palette.Hex(0x003893) : Palette.Hex(0xCE1126);

                case "EC":
                {
                    // Los colores de Colombia, con un disco dorado en el centro en lugar del escudo [P].
                    float disc = Vector2.Distance(new Vector2(xd, yd), new Vector2(fw * 0.5f, fh * 0.5f));
                    if (disc <= fh * 0.2f)
                    {
                        return disc <= fh * 0.12f ? Palette.Hex(0xF6E7B0) : Palette.Hex(0xB08A2E);
                    }

                    return yd >= fh * 0.5f ? Palette.Hex(0xFFD100) : yd >= fh * 0.25f ? Palette.Hex(0x0072CE) : Palette.Hex(0xEF3340);
                }

                case "PE":
                    // La de uso civil: rojo, blanco y rojo, en tercios verticales.
                    return xd < fw / 3f || xd >= fw * 2f / 3f ? Palette.Hex(0xD91023) : Color.white;

                case "BO":
                    // La tricolor civil: rojo, amarillo y verde, en tercios horizontales (D-013, punto 5).
                    return yd >= fh * 2f / 3f ? Palette.Hex(0xD52B1E) : yd >= fh / 3f ? Palette.Hex(0xF9E300) : Palette.Hex(0x007934);

                default:
                    return Color.white;
            }
        }
    }
}
