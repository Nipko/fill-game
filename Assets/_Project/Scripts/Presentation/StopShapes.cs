#nullable enable
using System;
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Siluetas geométricas de respaldo de los héroes del fondo de parada (tarea M1.4; propuesta del Mapa §10.2: «dos agujas
    /// sobre un rectángulo para Ver-o-Peso, un semicírculo con tambor para el Teatro, rectángulos sobre balsas para las casas del
    /// Itaya»; D-013, punto 9): <see cref="TwoSpires"/>, <see cref="Dome"/>, <see cref="Rafts"/>, <see cref="GenericCity"/>,
    /// <see cref="GenericNature"/>, <see cref="GenericWater"/> y <see cref="Beacon"/> [P]. Las formas propias de cada sitio llegan en
    /// M2.4. Son arte provisional: solo salen con <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo) y sirven para
    /// validar VA-20 antes de encargar los héroes (direccion-visual-v2 §9); nunca en la ficha, las capturas de la tienda ni la UA.
    /// <para>Cada forma es una lista de primitivas en dp con el pie del héroe en el origen: x de −120 a 120 (240 dp de ancho) e y
    /// hacia arriba desde el horizonte, como mucho <see cref="MaxAboveDp"/> por encima y <see cref="MaxBelowDp"/> por debajo
    /// (07 §2.1: héroe de ≈ 240 × 80 dp, 48 sobre el horizonte y 32 de orilla). Sin personas, canastos, banderas, cuias ni
    /// canoas (VA-22). <see cref="Draw"/> las pinta con su reflejo volteado en un <see cref="StopShapeGraphic"/>, sin
    /// asignar memoria.</para>
    /// </summary>
    public static class StopShapes
    {
        /// <summary>Dos agujas sobre un rectángulo: el mercado de hierro de Ver-o-Peso (parada 0).</summary>
        public const string TwoSpires = "two_spires";

        /// <summary>Cúpula sobre un tambor, con la ciudad baja: el Teatro Amazonas (parada 5).</summary>
        public const string Dome = "dome";

        /// <summary>Casas sobre balsas: la creciente del Itaya (parada 15).</summary>
        public const string Rafts = "rafts";

        /// <summary>Ciudad baja y lejana (malecones de Leticia e Iquitos, Nauta lejana).</summary>
        public const string GenericCity = "generic_city";

        /// <summary>Cerro y orilla de selva con playa (paisaje del tramo).</summary>
        public const string GenericNature = "generic_nature";

        /// <summary>Lago o río ancho con la orilla lejana baja y arbolada (paisaje del tramo).</summary>
        public const string GenericWater = "generic_water";

        /// <summary>Baliza de navegación en el río ancho entre dos orillas de várzea (parada 13, <c>kit:rio_ancho_con_baliza</c>).</summary>
        public const string Beacon = "beacon";

        /// <summary>Las siete formas, en orden.</summary>
        public static readonly string[] All = { TwoSpires, Dome, Rafts, GenericCity, GenericNature, GenericWater, Beacon };

        /// <summary>Ancho de la caja de una forma (dp; 07 §2.1).</summary>
        public const float WidthDp = 240f;

        /// <summary>Tope de silueta sobre el horizonte (dp; 07 §2.1).</summary>
        public const float MaxAboveDp = 48f;

        /// <summary>Tope de orilla bajo el horizonte (dp; 07 §2.1).</summary>
        public const float MaxBelowDp = 32f;

        /// <summary>Tono del cuerpo de la silueta.</summary>
        public const int ToneBody = 0;

        /// <summary>Tono de los remates, techos y copas (más oscuro).</summary>
        public const int ToneAccent = 1;

        /// <summary>Tono de las ventanas y columnas (claro).</summary>
        public const int ToneLight = 2;

        /// <summary>Tono de la orilla, la playa o las balsas.</summary>
        public const int ToneShore = 3;

        /// <summary>Tono propio de la forma (las tejas de la cúpula, las franjas de la baliza, los árboles de detrás de las balsas).</summary>
        public const int ToneFeature = 4;

        private const int KindRect = 0;
        private const int KindTriangle = 1;
        private const int KindDome = 2;
        private const int Stride = 8;
        private const int DomeSegments = 12;

        // Primitivas: {tipo, tono, a, b, c, d, e, f}. Rectángulo (x0, y0, x1, y1); triángulo (x0, y0, x1, y1, x2, y2); cúpula =
        // media elipse superior (cx, cy, rx, ry). x desde el pie, y hacia arriba desde el horizonte, en dp. En orden de dibujo.
        private static readonly float[] TwoSpiresData =
        {
            0, ToneShore, -120, -4, 120, 2, 0, 0,
            0, ToneBody, -118, 0, -80, 8, 0, 0,
            0, ToneBody, 80, 0, 118, 9, 0, 0,
            0, ToneBody, -72, 0, 72, 15, 0, 0,
            0, ToneAccent, -76, 15, 76, 18, 0, 0,
            1, ToneAccent, -18, 18, 18, 18, 0, 25,
            0, ToneBody, -64, 0, -46, 26, 0, 0,
            0, ToneAccent, -66, 26, -44, 28, 0, 0,
            1, ToneAccent, -65, 28, -45, 28, -55, 40,
            0, ToneBody, 46, 0, 64, 26, 0, 0,
            0, ToneAccent, 44, 26, 66, 28, 0, 0,
            1, ToneAccent, 45, 28, 65, 28, 55, 40,
            0, ToneLight, -36, 4, -28, 11, 0, 0,
            0, ToneLight, -16, 4, -8, 11, 0, 0,
            0, ToneLight, 8, 4, 16, 11, 0, 0,
            0, ToneLight, 28, 4, 36, 11, 0, 0,
            0, ToneLight, -58, 16, -52, 22, 0, 0,
            0, ToneLight, 52, 16, 58, 22, 0, 0,
        };

        // La cúpula, lo reconocible, cabe entera en los 30 dp que se ven sobre el horizonte con 2 filas (07 §2.1).
        private static readonly float[] DomeData =
        {
            0, ToneShore, -120, -4, 120, 2, 0, 0,
            0, ToneBody, -116, 0, -92, 8, 0, 0,
            0, ToneBody, -92, 0, -70, 13, 0, 0,
            0, ToneBody, -70, 0, -48, 7, 0, 0,
            0, ToneBody, 48, 0, 70, 9, 0, 0,
            0, ToneBody, 70, 0, 94, 14, 0, 0,
            0, ToneBody, 94, 0, 118, 8, 0, 0,
            0, ToneBody, -40, 0, 40, 12, 0, 0,
            0, ToneLight, -28, 2, -24, 9, 0, 0,
            0, ToneLight, -15, 2, -11, 9, 0, 0,
            0, ToneLight, -2, 2, 2, 9, 0, 0,
            0, ToneLight, 11, 2, 15, 9, 0, 0,
            0, ToneLight, 24, 2, 28, 9, 0, 0,
            1, ToneAccent, -24, 12, 24, 12, 0, 16,
            0, ToneAccent, -14, 14, 14, 18, 0, 0,
            2, ToneFeature, 0, 18, 16, 11, 0, 0,
            0, ToneAccent, -2, 29, 2, 33, 0, 0,
        };

        private static readonly float[] RaftsData =
        {
            2, ToneFeature, -90, 0, 30, 9, 0, 0,
            2, ToneFeature, -30, 0, 34, 8, 0, 0,
            2, ToneFeature, 40, 0, 36, 10, 0, 0,
            2, ToneFeature, 98, 0, 22, 8, 0, 0,
            0, ToneShore, -104, -3, -64, 0, 0, 0,
            0, ToneBody, -101, 0, -67, 12, 0, 0,
            1, ToneAccent, -104, 12, -64, 12, -84, 20,
            0, ToneLight, -87, 3, -81, 8, 0, 0,
            0, ToneShore, -56, -3, -18, 0, 0, 0,
            0, ToneBody, -53, 0, -21, 15, 0, 0,
            1, ToneAccent, -56, 15, -18, 15, -37, 23,
            0, ToneLight, -40, 4, -34, 9, 0, 0,
            0, ToneShore, -8, -3, 30, 0, 0, 0,
            0, ToneBody, -5, 0, 27, 13, 0, 0,
            1, ToneAccent, -8, 13, 30, 13, 11, 21,
            0, ToneLight, 8, 3, 14, 8, 0, 0,
            0, ToneShore, 40, -3, 82, 0, 0, 0,
            0, ToneBody, 43, 0, 79, 16, 0, 0,
            1, ToneAccent, 40, 16, 82, 16, 61, 24,
            0, ToneLight, 58, 4, 64, 9, 0, 0,
            0, ToneShore, 88, -3, 116, 0, 0, 0,
            0, ToneBody, 91, 0, 113, 11, 0, 0,
            1, ToneAccent, 88, 11, 116, 11, 102, 18,
        };

        private static readonly float[] GenericCityData =
        {
            0, ToneShore, -120, -4, 120, 2, 0, 0,
            0, ToneBody, -116, 0, -92, 12, 0, 0,
            0, ToneBody, -92, 0, -70, 20, 0, 0,
            1, ToneAccent, -92, 20, -70, 20, -81, 26,
            0, ToneBody, -70, 0, -50, 10, 0, 0,
            0, ToneBody, -50, 0, -24, 26, 0, 0,
            0, ToneAccent, -38, 26, -36, 36, 0, 0,
            0, ToneBody, -24, 0, -4, 15, 0, 0,
            0, ToneBody, -4, 0, 22, 21, 0, 0,
            0, ToneBody, 22, 0, 44, 11, 0, 0,
            1, ToneAccent, 22, 11, 44, 11, 33, 16,
            0, ToneBody, 44, 0, 70, 24, 0, 0,
            0, ToneBody, 70, 0, 92, 13, 0, 0,
            0, ToneBody, 92, 0, 116, 17, 0, 0,
            0, ToneLight, -46, 14, -40, 19, 0, 0,
            0, ToneLight, -34, 14, -28, 19, 0, 0,
            0, ToneLight, 0, 10, 6, 15, 0, 0,
            0, ToneLight, 48, 14, 54, 19, 0, 0,
            0, ToneLight, 60, 14, 66, 19, 0, 0,
        };

        private static readonly float[] GenericNatureData =
        {
            0, ToneShore, -120, -4, 120, 2, 0, 0,
            2, ToneBody, 10, 0, 104, 30, 0, 0,
            2, ToneAccent, -96, 0, 16, 12, 0, 0,
            2, ToneAccent, -74, 2, 14, 14, 0, 0,
            2, ToneAccent, -50, 8, 16, 14, 0, 0,
            2, ToneAccent, -24, 16, 14, 13, 0, 0,
            2, ToneFeature, 4, 22, 16, 14, 0, 0,
            2, ToneAccent, 30, 20, 14, 12, 0, 0,
            2, ToneFeature, 54, 12, 16, 13, 0, 0,
            2, ToneAccent, 80, 4, 14, 13, 0, 0,
            2, ToneAccent, 104, 0, 14, 10, 0, 0,
        };

        private static readonly float[] GenericWaterData =
        {
            0, ToneShore, -120, 0, 120, 3, 0, 0,
            2, ToneBody, -104, 2, 16, 9, 0, 0,
            2, ToneAccent, -80, 2, 12, 11, 0, 0,
            2, ToneBody, -58, 2, 16, 8, 0, 0,
            2, ToneAccent, -30, 2, 14, 12, 0, 0,
            2, ToneBody, -4, 2, 18, 10, 0, 0,
            2, ToneAccent, 24, 2, 12, 13, 0, 0,
            2, ToneBody, 48, 2, 16, 9, 0, 0,
            2, ToneAccent, 74, 2, 14, 11, 0, 0,
            2, ToneBody, 100, 2, 16, 8, 0, 0,
        };

        private static readonly float[] BeaconData =
        {
            0, ToneShore, -120, 0, -36, 4, 0, 0,
            0, ToneShore, 20, 0, 120, 3, 0, 0,
            2, ToneBody, -104, 3, 14, 7, 0, 0,
            2, ToneBody, -80, 3, 12, 8, 0, 0,
            2, ToneBody, -56, 3, 14, 6, 0, 0,
            2, ToneBody, 44, 2, 14, 7, 0, 0,
            2, ToneBody, 72, 2, 12, 8, 0, 0,
            2, ToneBody, 98, 2, 14, 6, 0, 0,
            0, ToneAccent, -16, -3, 4, 1, 0, 0,
            0, ToneAccent, -8, 0, -4, 26, 0, 0,
            0, ToneFeature, -10, 10, -2, 14, 0, 0,
            0, ToneFeature, -10, 18, -2, 22, 0, 0,
            1, ToneFeature, -14, 26, 2, 26, -6, 38,
        };

        // Héroe pintado (ranura bg_hero_<sitio>) o pieza del kit (kit:<pieza>) → forma de respaldo [P]. Puerto Nariño va como
        // «solo la boca del río» (la alternativa de 07 §2.1 sin aldea identificable) hasta el asesor; la reserva (Casa de Fierro),
        // como ciudad baja y sin el mito de Eiffel.
        private static readonly string[] SourceShapes =
        {
            "bg_hero_ver_o_peso", TwoSpires,
            "bg_hero_teatro_amazonas", Dome,
            "bg_hero_itaya_creciente", Rafts,
            "bg_hero_casa_de_fierro", GenericCity,
            "bg_hero_leticia", GenericCity,
            "bg_hero_iquitos", GenericCity,
            "bg_hero_alter_do_chao", GenericNature,
            "bg_hero_flona_tapajos", GenericNature,
            "bg_hero_amacayacu", GenericNature,
            "bg_hero_allpahuayo", GenericNature,
            "bg_hero_anavilhanas", GenericWater,
            "bg_hero_lago_tefe", GenericWater,
            "bg_hero_puerto_narino", GenericWater,
            "bg_hero_tarapoto", GenericWater,
            "bg_hero_pacaya_samiria", GenericWater,
            "kit:rio_ancho_con_baliza", Beacon,
            "kit:orilla_de_nauta", GenericCity,
        };

        /// <summary>
        /// Forma de respaldo del fondo <paramref name="backdrop"/>: la de su héroe pintado o la de su pieza del kit
        /// (<c>kit:&lt;pieza&gt;</c>); null si el héroe es una pieza por código (<c>codigo:</c>, las líneas entre aguas, que ya son
        /// definitivas) o si no hay fondo. Un héroe sin forma propia va como <see cref="GenericNature"/> y una pieza de kit sin
        /// forma, como <see cref="GenericWater"/> [P].
        /// </summary>
        public static string? ShapeOf(RiverStopBackdrop? backdrop)
        {
            if (backdrop == null)
            {
                return null;
            }

            if (backdrop.Hero != null)
            {
                return Lookup(backdrop.Hero) ?? GenericNature;
            }

            string? code = backdrop.Code;
            if (code != null && code.StartsWith("kit:", StringComparison.Ordinal))
            {
                return Lookup(code) ?? GenericWater;
            }

            return null;
        }

        /// <summary>Si <paramref name="shape"/> es una de las formas de <see cref="All"/>.</summary>
        public static bool IsKnown(string? shape)
        {
            return shape != null && Data(shape) != null;
        }

        /// <summary>Primitivas de <paramref name="shape"/> (0 si no la conoce).</summary>
        public static int PrimitiveCount(string shape)
        {
            float[]? data = Data(shape);
            return data == null ? 0 : data.Length / Stride;
        }

        /// <summary>
        /// Caja de la forma en dp desde el pie (x de izquierda a derecha, y hacia arriba): mínimo y máximo de x y de y de sus
        /// primitivas (las cúpulas, con su radio). Todo cero si no la conoce.
        /// </summary>
        public static Rect Bounds(string shape)
        {
            float[]? data = Data(shape);
            if (data == null)
            {
                return new Rect(0f, 0f, 0f, 0f);
            }

            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int p = 0; p < data.Length; p += Stride)
            {
                int kind = (int)data[p];
                if (kind == KindDome)
                {
                    xMin = Mathf.Min(xMin, data[p + 2] - data[p + 4]);
                    xMax = Mathf.Max(xMax, data[p + 2] + data[p + 4]);
                    yMin = Mathf.Min(yMin, data[p + 3]);
                    yMax = Mathf.Max(yMax, data[p + 3] + data[p + 5]);
                    continue;
                }

                int points = kind == KindTriangle ? 3 : 2;
                for (int i = 0; i < points; i++)
                {
                    float x = data[p + 2 + (2 * i)];
                    float y = data[p + 3 + (2 * i)];
                    xMin = Mathf.Min(xMin, x);
                    xMax = Mathf.Max(xMax, x);
                    yMin = Mathf.Min(yMin, y);
                    yMax = Mathf.Max(yMax, y);
                }
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Color del tono <paramref name="tone"/> (<see cref="ToneBody"/> … <see cref="ToneFeature"/>) de la forma
        /// <paramref name="shape"/> [P]: siluetas lejanas con bruma (perspectiva aérea), claras para que la escena siga brillante
        /// (VA-03: nada por debajo de V 0,25) y sin el turquesa caribeño ni banderas (guía §7; VA-22).
        /// </summary>
        public static Color ToneColor(string shape, int tone)
        {
            switch (shape)
            {
                case Rafts:
                    return Pick(tone, 0xA58566, 0x74604C, 0xF2E6C8, 0x7E6650, 0x5A8A62);
                case GenericNature:
                    return Pick(tone, 0x548E5F, 0x437A4E, 0xF2E6C8, 0xE6D4A8, 0x5E9D66);
                case GenericWater:
                    return Pick(tone, 0x568A60, 0x467852, 0xF2E6C8, 0x8A9670, 0x5E9D66);
                case Beacon:
                    return Pick(tone, 0x568A60, 0x66717F, 0xF2E6C8, 0x8F8C6C, 0xD45A44);
                case Dome:
                    return Pick(tone, 0x8291A2, 0x667386, 0xF2E6C8, 0x8E7C5E, 0xC8B04E);
                default:
                    return Pick(tone, 0x8291A2, 0x667386, 0xF2E6C8, 0x8E7C5E, 0x8291A2);
            }
        }

        /// <summary>
        /// Pinta <paramref name="shape"/> en <paramref name="g"/> con el pie en (<paramref name="footX"/>, <paramref name="horizon"/>)
        /// dp desde arriba y a escala <paramref name="k"/> (ancho del lienzo / 360). Primero el reflejo de lo que queda sobre el
        /// horizonte, volteado, teñido a medias con el agua <paramref name="water"/>, con opacidad <paramref name="reflectionAlpha"/>
        /// en la línea del agua que baja a 0 a <paramref name="reflectionDp"/> dp (y recortado ahí); después la silueta, con
        /// cada tono multiplicado por <paramref name="lut"/>. No vacía el gráfico ni asigna memoria (salvo que su búfer tenga
        /// que crecer). Devuelve false si no conoce la forma.
        /// </summary>
        public static bool Draw(StopShapeGraphic g, string shape, float footX, float horizon, float k, Color lut, Color water,
            float reflectionAlpha, float reflectionDp)
        {
            float[]? data = Data(shape);
            if (data == null)
            {
                return false;
            }

            if (reflectionAlpha > 0f && reflectionDp > 0f)
            {
                for (int p = 0; p < data.Length; p += Stride)
                {
                    Color c = Color.Lerp(Lut(ToneColor(shape, (int)data[p + 1]), lut), water, 0.5f);
                    EmitPrimitive(g, data, p, footX, horizon, k, c, true, reflectionAlpha, reflectionDp);
                }
            }

            for (int p = 0; p < data.Length; p += Stride)
            {
                Color c = Lut(ToneColor(shape, (int)data[p + 1]), lut);
                EmitPrimitive(g, data, p, footX, horizon, k, c, false, 1f, 0f);
            }

            return true;
        }

        private static Color Lut(Color c, Color lut)
        {
            return new Color(c.r * lut.r, c.g * lut.g, c.b * lut.b, 1f);
        }

        // Una primitiva: tal cual o, con mirror, su parte de sobre el horizonte volteada bajo él, con la opacidad del reflejo
        // decreciente con la profundidad y recortada a depth dp (a escala k).
        private static void EmitPrimitive(StopShapeGraphic g, float[] d, int p, float footX, float horizon, float k, Color c,
            bool mirror, float alpha, float depth)
        {
            int kind = (int)d[p];
            if (kind == KindRect)
            {
                float x0 = d[p + 2];
                float y0 = d[p + 3];
                float x1 = d[p + 4];
                float y1 = d[p + 5];
                if (mirror)
                {
                    if (y1 <= 0f)
                    {
                        return;
                    }

                    y0 = Mathf.Max(0f, y0);
                }

                Vertex(g, x0, y1, footX, horizon, k, c, mirror, alpha, depth, out int a);
                Vertex(g, x1, y1, footX, horizon, k, c, mirror, alpha, depth, out int b);
                Vertex(g, x1, y0, footX, horizon, k, c, mirror, alpha, depth, out int e);
                Vertex(g, x0, y0, footX, horizon, k, c, mirror, alpha, depth, out int f);
                g.AddTriangle(a, b, e);
                g.AddTriangle(a, e, f);
                return;
            }

            if (kind == KindTriangle)
            {
                if (mirror && d[p + 3] <= 0f && d[p + 5] <= 0f && d[p + 7] <= 0f)
                {
                    return;
                }

                Vertex(g, d[p + 2], MirrorClamp(d[p + 3], mirror), footX, horizon, k, c, mirror, alpha, depth, out int a);
                Vertex(g, d[p + 4], MirrorClamp(d[p + 5], mirror), footX, horizon, k, c, mirror, alpha, depth, out int b);
                Vertex(g, d[p + 6], MirrorClamp(d[p + 7], mirror), footX, horizon, k, c, mirror, alpha, depth, out int e);
                g.AddTriangle(a, b, e);
                return;
            }

            // Cúpula: abanico de media elipse sobre su base.
            float cx = d[p + 2];
            float cy = d[p + 3];
            float rx = d[p + 4];
            float ry = d[p + 5];
            if (mirror && cy + ry <= 0f)
            {
                return;
            }

            Vertex(g, cx, MirrorClamp(cy, mirror), footX, horizon, k, c, mirror, alpha, depth, out int center);
            int prev = -1;
            for (int s = 0; s <= DomeSegments; s++)
            {
                float t = Mathf.PI * s / DomeSegments;
                float x = cx + (rx * Mathf.Cos(t));
                float y = MirrorClamp(cy + (ry * Mathf.Sin(t)), mirror);
                Vertex(g, x, y, footX, horizon, k, c, mirror, alpha, depth, out int v);
                if (prev >= 0)
                {
                    g.AddTriangle(center, prev, v);
                }

                prev = v;
            }
        }

        private static float MirrorClamp(float y, bool mirror)
        {
            return mirror ? Mathf.Max(0f, y) : y;
        }

        // Vértice de la forma: x desde el pie e y hacia arriba en dp → dp del lienzo desde arriba. En el reflejo, la y se voltea
        // bajo el horizonte, se recorta a depth y la opacidad baja de alpha a 0 con la profundidad.
        private static void Vertex(StopShapeGraphic g, float x, float y, float footX, float horizon, float k, Color c, bool mirror,
            float alpha, float depth, out int index)
        {
            float px = footX + (x * k);
            if (!mirror)
            {
                index = g.AddVertex(px, horizon - (y * k), c);
                return;
            }

            float below = Mathf.Min(y, depth);
            c.a = alpha * (1f - (below / depth));
            index = g.AddVertex(px, horizon + (below * k), c);
        }

        private static Color Pick(int tone, int body, int accent, int light, int shore, int feature)
        {
            switch (tone)
            {
                case ToneAccent: return Palette.Hex(accent);
                case ToneLight: return Palette.Hex(light);
                case ToneShore: return Palette.Hex(shore);
                case ToneFeature: return Palette.Hex(feature);
                default: return Palette.Hex(body);
            }
        }

        private static string? Lookup(string source)
        {
            for (int i = 0; i + 1 < SourceShapes.Length; i += 2)
            {
                if (string.Equals(SourceShapes[i], source, StringComparison.Ordinal))
                {
                    return SourceShapes[i + 1];
                }
            }

            return null;
        }

        private static float[]? Data(string shape)
        {
            switch (shape)
            {
                case TwoSpires: return TwoSpiresData;
                case Dome: return DomeData;
                case Rafts: return RaftsData;
                case GenericCity: return GenericCityData;
                case GenericNature: return GenericNatureData;
                case GenericWater: return GenericWaterData;
                case Beacon: return BeaconData;
                default: return null;
            }
        }
    }
}
