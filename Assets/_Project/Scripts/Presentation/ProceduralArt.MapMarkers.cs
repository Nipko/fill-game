#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Marcadores provisionales del Mapa del río que faltaban (D-013, puntos 3 y 6; propuesta del Mapa §3.3, §4.1, §6.1 y §12,
    /// piezas 11 y 13; tarea M3.5), en la familia de D-003 (vidrio, madera y luz de juego):
    /// <list type="bullet">
    /// <item><see cref="MapBeacon"/>: la <b>baliza</b> del cierre de mundo (el «castillo» de la propuesta §4.1), un poste de
    /// madera con su farol de vidrio de <see cref="MapBeaconWidthDp"/> × <see cref="MapBeaconHeightDp"/> dp: apagada, el vidrio
    /// azul hielo del Mapa; encendida, la llama cálida con su halo de <c>destello</c> (#FFF4D2). Pivote al pie.</item>
    /// <item><see cref="MapHome"/>: la <b>casa de TiTino</b> a escala de Mapa, su embaúba en la orilla de la bahía de Guajará
    /// (propuesta §3.3, punto 7; c.3 de la propuesta del personaje: no viaja), de <see cref="MapHomeWidthDp"/> ×
    /// <see cref="MapHomeHeightDp"/> dp, con el tronco claro anillado, tres ramas en candelabro con sus hojas palmeadas de envés
    /// plateado y un trozo de orilla. El perezoso no va en el dibujo: lo pinta su rig (<see cref="SlothRig"/>), colgado de la
    /// rama a <see cref="MapHomeBranchFraction"/> del alto. Pivote al pie del tronco.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>Las ranuras mandan: <c>map_beacon</c> (del atlas de UI; con el PNG, la apagada es el mismo dibujo con
    /// <see cref="BeaconUnlitTint"/>) y <c>map_home</c> (256²). Con su PNG no se crea el provisional.</para>
    /// <para><b>Compuerta de P-M5</b> (<see cref="MapArtRule"/>): la casa es arte provisional de TiTino, como su retrato y su
    /// ventanita, así que sin el PNG de <c>map_home</c> solo sale en el Editor y en desarrollo (<see cref="MapHomeFor"/> da null en
    /// release). La baliza no lo es (como la mini cuia y la boya, es una pieza de la UI del Mapa): sale siempre.</para>
    /// <para>La embaúba está pendiente del asesor cultural (propuesta de TiTino g.2, punto 1; encargo del Mapa, fila 11): si la
    /// desaconseja, solo cambia este dibujo (una rama genérica), no la ranura ni quien la usa. Nada de hojas quemadas ni ceniza;
    /// nunca la sumaúma ni la lupuna.</para>
    /// <para>Perezosas: nada se crea al construir <see cref="ProceduralArt"/>, solo la primera vez que se piden
    /// (<see cref="MapMarkersBuilt"/>). Tras <see cref="Dispose"/> se vuelven a generar (el == de Unity).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ranura de la baliza del cierre de mundo (glosario §11).</summary>
        public const string MapBeaconSlot = "map_beacon";

        /// <summary>Ranura de la casa de TiTino a escala de Mapa (glosario §11).</summary>
        public const string MapHomeSlot = "map_home";

        /// <summary>Ancho de la baliza (dp) [P].</summary>
        public const float MapBeaconWidthDp = 12f;

        /// <summary>Alto de la baliza (dp) [P].</summary>
        public const float MapBeaconHeightDp = 20f;

        /// <summary>Ancho de la casa de TiTino en el Mapa (dp) [P].</summary>
        public const float MapHomeWidthDp = 40f;

        /// <summary>Alto de la casa de TiTino en el Mapa (dp) [P].</summary>
        public const float MapHomeHeightDp = 52f;

        /// <summary>
        /// Altura de la rama de la que cuelga el perezoso, en fracción del alto de la casa desde el pie [P]: bajo la copa, para que
        /// el rig quede delante del tronco y no tape las hojas.
        /// </summary>
        public const float MapHomeBranchFraction = 0.66f;

        /// <summary>Tinte de la baliza apagada cuando el dibujo es el PNG de la ranura (uno solo para los dos estados) [P].</summary>
        public static readonly Color BeaconUnlitTint = new Color(0.62f, 0.70f, 0.74f, 0.9f);

        /// <summary>Llama de la baliza encendida, ámbar cálido [P].</summary>
        public static readonly Color BeaconFlame = Palette.Hex(0xFFB84A);

        private static readonly Color BeaconPost = Palette.Hex(0x5B3A28);
        private static readonly Color HomeBank = Palette.Hex(0x3E7A3C);

        private Sprite? _beaconLit;
        private Sprite? _beaconUnlit;
        private Sprite? _mapHome;

        /// <summary>Si ya se creó alguna de las piezas de este archivo.</summary>
        public bool MapMarkersBuilt => _beaconLit != null || _beaconUnlit != null || _mapHome != null;

        /// <summary>La baliza encendida o apagada: el PNG de <see cref="MapBeaconSlot"/> si existe; si no, la provisional.</summary>
        public Sprite MapBeacon(bool lit)
        {
            return MapBeaconFor(lit, ArtSlots.Find(MapBeaconSlot));
        }

        /// <summary>
        /// La regla de <see cref="MapBeacon"/> con el PNG de la ranura explícito (<paramref name="slotPng"/>): si existe, manda en
        /// los dos estados (la apagada se tiñe con <see cref="BeaconUnlitTint"/> quien la pinta).
        /// </summary>
        public Sprite MapBeaconFor(bool lit, Sprite? slotPng)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            if (lit)
            {
                return _beaconLit != null ? _beaconLit : (_beaconLit = BuildMapBeacon(true));
            }

            return _beaconUnlit != null ? _beaconUnlit : (_beaconUnlit = BuildMapBeacon(false));
        }

        /// <summary>
        /// La casa de TiTino que se pinta: el PNG de <see cref="MapHomeSlot"/> si existe; si no, la provisional con
        /// <see cref="MapArtRule.ShowProvisional"/>; si no (release sin arte final), null.
        /// </summary>
        public Sprite? MapHome => MapHomeFor(ArtSlots.Find(MapHomeSlot), MapArtRule.ShowProvisional);

        /// <summary>
        /// La regla de <see cref="MapHome"/> con el PNG de la ranura (<paramref name="slotPng"/>) y la compuerta
        /// (<paramref name="showProvisional"/>) explícitos: el PNG manda y, con él, la provisional no se crea.
        /// </summary>
        public Sprite? MapHomeFor(Sprite? slotPng, bool showProvisional)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            return showProvisional ? MapHomeProcedural : null;
        }

        /// <summary>La casa provisional por código, creada la primera vez que se pide. No mira la ranura ni la compuerta.</summary>
        public Sprite MapHomeProcedural => _mapHome != null ? _mapHome : (_mapHome = BuildMapHome());

        // La baliza en dp (origen abajo a la izquierda, eje en x = 6): el poste de madera con su pie, el farol de vidrio con el
        // tejadillo y, encendida, la llama y un halo cálido alrededor del farol.
        private Sprite BuildMapBeacon(bool lit)
        {
            const float k = UiPxPerDp;
            const float w = MapBeaconWidthDp;
            const float h = MapBeaconHeightDp;
            const float axis = w * 0.5f;
            int pw = Mathf.RoundToInt(w * k);
            int ph = Mathf.RoundToInt(h * k);
            var pc = new PixelCanvas(pw, ph);

            if (lit)
            {
                // Halo: la luz de juego que dice «aquí se cierra el mundo» sin texto.
                for (int y = 0; y < ph; y++)
                {
                    for (int x = 0; x < pw; x++)
                    {
                        float dx = ((x + 0.5f) / k) - axis;
                        float dy = ((y + 0.5f) / k) - 14.5f;
                        float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                        float glow = Mathf.Clamp01(1f - (d / 6f));
                        if (glow > 0f)
                        {
                            pc.Blend(x, y, new Color(Palette.Glint.r, Palette.Glint.g, Palette.Glint.b, 0.55f), glow * glow);
                        }
                    }
                }
            }

            // Pie y poste.
            pc.FillRoundedRect((axis - 3.2f) * k, 0.4f * k, (axis + 3.2f) * k, 2.2f * k, 0.8f * k, Palette.Groove);
            pc.FillRoundedRect((axis - 2.6f) * k, 0.8f * k, (axis + 2.6f) * k, 1.9f * k, 0.6f * k, BeaconPost);
            pc.FillCapsule(axis * k, 1.5f * k, axis * k, 10.5f * k, 1.25f * k, Palette.Groove);
            pc.FillCapsule(axis * k, 1.5f * k, axis * k, 10.5f * k, 0.85f * k, BeaconPost);
            pc.FillCapsule((axis - 0.3f) * k, 2f * k, (axis - 0.3f) * k, 10f * k, 0.25f * k, ColorMath.Lighten(BeaconPost, 0.25f));

            // Farol: canto oscuro, cuerpo de vidrio (o la llama) y brillo a la izquierda.
            pc.FillRoundedRect((axis - 3.4f) * k, 10.2f * k, (axis + 3.4f) * k, 17.4f * k, 1.4f * k, Palette.Groove);
            Color body = lit ? BeaconFlame : new Color(0.81f, 0.94f, 0.95f, 1f);
            pc.FillRoundedRect((axis - 2.6f) * k, 11f * k, (axis + 2.6f) * k, 16.6f * k, 1f * k, body);
            if (lit)
            {
                pc.FillEllipseSoft(axis * k, 13.4f * k, 1.5f * k, 2.2f * k, 0.6f * k, Palette.Glint);
            }
            else
            {
                pc.FillCapsule((axis - 1.6f) * k, 12f * k, (axis - 1.6f) * k, 15.6f * k, 0.35f * k, new Color(1f, 1f, 1f, 0.8f));
            }

            // Tejadillo de madera con su perilla.
            pc.FillTriangle(new Vector2((axis - 4.2f) * k, 17.1f * k), new Vector2((axis + 4.2f) * k, 17.1f * k), new Vector2(axis * k, 19.3f * k), Palette.Groove);
            pc.FillTriangle(new Vector2((axis - 3.4f) * k, 17.5f * k), new Vector2((axis + 3.4f) * k, 17.5f * k), new Vector2(axis * k, 18.8f * k), Palette.Frame);
            pc.FillCircle(axis * k, 19.2f * k, 0.6f * k, Palette.Groove);
            return MakeSprite(pc, lit ? "map_beacon_lit" : "map_beacon_unlit", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // La casa en dp (origen abajo a la izquierda, tronco en x = 20): la orilla, el tronco claro con sus anillos, tres ramas en
        // candelabro y en la punta de cada una una hoja palmeada que cuelga, con los lóbulos de fuera del envés plateado.
        private Sprite BuildMapHome()
        {
            const float k = UiPxPerDp;
            const float w = MapHomeWidthDp;
            const float h = MapHomeHeightDp;
            const float axis = w * 0.5f;
            int pw = Mathf.RoundToInt(w * k);
            int ph = Mathf.RoundToInt(h * k);
            var pc = new PixelCanvas(pw, ph);

            // Orilla: un montículo verde con su canto oscuro.
            pc.FillEllipseSoft(axis * k, 1.6f * k, 15f * k, 3.4f * k, 0.6f * k, ColorMath.Darken(HomeBank, 0.35f));
            pc.FillEllipseSoft(axis * k, 2.1f * k, 13.6f * k, 2.7f * k, 0.6f * k, HomeBank);

            // Tronco claro, con su sombra a la derecha y los anillos de las cicatrices de las hojas.
            float top = 40f;
            pc.FillCapsule((axis + 0.5f) * k, 2.5f * k, (axis + 0.5f) * k, top * k, 1.9f * k, ColorMath.Darken(EmbaubaRing, 0.25f));
            pc.FillCapsule(axis * k, 2.5f * k, axis * k, top * k, 1.6f * k, EmbaubaBark);
            pc.FillCapsule((axis - 0.6f) * k, 3f * k, (axis - 0.6f) * k, (top - 1f) * k, 0.4f * k, ColorMath.Lighten(EmbaubaBark, 0.35f));
            for (int i = 1; i <= 7; i++)
            {
                float y = 3f + (i * 4.6f);
                pc.FillCapsule((axis - 1.5f) * k, y * k, (axis + 1.5f) * k, (y + 0.3f) * k, 0.3f * k, EmbaubaRing);
            }

            // Ramas en candelabro y sus hojas.
            var tips = new[]
            {
                new Vector2(axis - 10f, 47f),
                new Vector2(axis + 0.5f, 50f),
                new Vector2(axis + 10f, 47f),
            };
            for (int i = 0; i < tips.Length; i++)
            {
                Vector2 tip = tips[i];
                pc.FillCapsule(axis * k, (top - 1f) * k, tip.x * k, tip.y * k, 1.1f * k, ColorMath.Darken(EmbaubaRing, 0.15f));
                pc.FillCapsule(axis * k, (top - 1f) * k, tip.x * k, tip.y * k, 0.8f * k, EmbaubaBark);
            }

            DrawEmbaubaLeaf(pc, tips[0].x * k, tips[0].y * k, -0.35f, 11f * k, k, 801);
            DrawEmbaubaLeaf(pc, tips[2].x * k, tips[2].y * k, 0.35f, 11f * k, k, 803);
            DrawEmbaubaLeaf(pc, tips[1].x * k, tips[1].y * k, 0f, 10f * k, k, 802);
            return MakeSprite(pc, "map_home", new Vector2(0.5f, 0f), Vector4.zero, k);
        }
    }
}
