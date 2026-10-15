#nullable enable
using System;
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Siluetas geométricas de respaldo de los héroes del fondo de parada (tareas M1.4 y M2.4; propuesta del Mapa §10.2: «dos
    /// agujas sobre un rectángulo para Ver-o-Peso, un semicírculo con tambor para el Teatro, rectángulos sobre balsas para las
    /// casas del Itaya»; D-013, punto 9). M2.4 da a cada héroe pintado de 1-200 su silueta propia [P], más la de la reserva (la
    /// Casa de Fierro) y las de las dos paradas solo con el kit (la baliza del río frontera y la orilla de Nauta), de modo que
    /// ninguna parada pintada usa una forma genérica (07 §2.1; propuesta del Mapa §10.3): <see cref="TwoSpires"/> (Ver-o-Peso),
    /// <see cref="AlterDoChao"/>, <see cref="FlonaTapajos"/>, <see cref="Dome"/> (Teatro Amazonas), <see cref="Anavilhanas"/>,
    /// <see cref="LagoTefe"/>, <see cref="Leticia"/> (también al atardecer), <see cref="Amacayacu"/>, <see cref="PuertoNarino"/>,
    /// <see cref="Tarapoto"/>, <see cref="Beacon"/> (el río frontera), <see cref="Iquitos"/>, <see cref="Rafts"/> (la creciente
    /// del Itaya), <see cref="CasaDeFierro"/> (su reserva), <see cref="Allpahuayo"/>, <see cref="Nauta"/> y
    /// <see cref="PacayaSamiria"/> (también al alba). Las dos líneas entre aguas (Santarém y el Encuentro) son piezas por código ya
    /// definitivas y no llevan silueta. <see cref="GenericCity"/>, <see cref="GenericNature"/> y <see cref="GenericWater"/> quedan
    /// solo para un héroe o una pieza del kit sin forma propia (201-1.000).
    /// <para>Son arte provisional: solo salen con <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo) y sirven para
    /// validar VA-20 antes de encargar los héroes (direccion-visual-v2 §9; la hoja <c>02f_board_stops_sheet</c> de las 20
    /// composiciones); nunca en la ficha, las capturas de la tienda ni la UA.</para>
    /// <para>Cada forma es una lista de primitivas en dp con el pie del héroe en el origen: x de −120 a 120 (240 dp de ancho) e y
    /// hacia arriba desde el horizonte, como mucho <see cref="MaxAboveDp"/> por encima y <see cref="MaxBelowDp"/> por debajo
    /// (07 §2.1: héroe de ≈ 240 × 80 dp, 48 sobre el horizonte y 32 de orilla). Todas caben enteras en los
    /// <see cref="RecognizableDp"/> dp de sobre el horizonte, que es lo que se ve con un tablero de 2 filas (07 §2.1;
    /// direccion-visual-v2 §3.4; propuesta del Mapa §10.6): las agujas, la cúpula y la marca de la baliza no suben a la fila del
    /// HUD. Lo que llega al borde de la caja es un rectángulo que entra y sale con <see cref="EdgeFadeDp"/> dp de degradado; las
    /// copas, los techos y las cúpulas se quedan dentro. Sin personas, canastos, banderas, cuias ni canoas (VA-22); lo que está
    /// bajo el horizonte es la orilla, el malecón, las lanchas sin nadie y, en Tarapoto, los lomos de delfín (07 §2.1).
    /// <see cref="Draw"/> las pinta con su reflejo volteado en un <see cref="StopShapeGraphic"/>, sin asignar memoria.</para>
    /// <para>Reglas de cada sitio (07 §2.1 y §8.1; propuesta del Mapa §10.3): la Floresta Nacional, dosel parejo sin ningún árbol
    /// emergente (sin samaúma); el lago de Tefé, todo sobre el horizonte, sin botos ni orillas secas; Puerto Nariño, solo la boca
    /// del río, sin pueblo (la alternativa de 07 §2.1 hasta el asesor); la Casa de Fierro, arquitectura de dos pisos con balcón y
    /// sin torre (sin el mito de Eiffel); Leticia, sin islas ni banderas; Nauta, una sola agua.</para>
    /// <para><b>Pendiente del asesor cultural</b> (07 §2.1 y §8.2; D-013: «todo nombre, dato y texto del Mapa queda pendiente
    /// del asesor»): representan lugares reales, así que tanto las formas como su asignación a cada sitio (<c>SourceShapes</c>)
    /// quedan pendientes de revisión, con asesor obligatorio en las paradas 7 (Tefé), 11 (Puerto Nariño) y 15 (Itaya).</para>
    /// </summary>
    public static class StopShapes
    {
        /// <summary>Dos agujas sobre un rectángulo: el mercado de hierro de Ver-o-Peso (parada 0).</summary>
        public const string TwoSpires = "two_spires";

        /// <summary>La lengua de arena de la Ilha do Amor delante del cerro de selva, sin cocoteros (parada 2).</summary>
        public const string AlterDoChao = "alter_do_chao";

        /// <summary>Orilla alta de tierra firme con el dosel parejo y una playa estrecha, sin samaúma (parada 3).</summary>
        public const string FlonaTapajos = "flona_tapajos";

        /// <summary>Cúpula sobre un tambor, con la ciudad baja: el Teatro Amazonas (parada 5).</summary>
        public const string Dome = "dome";

        /// <summary>Islas de selva con playas blancas sobre el agua negra (parada 6).</summary>
        public const string Anavilhanas = "anavilhanas";

        /// <summary>Lago con la orilla verde lejana y la ciudad baja; nada bajo el horizonte: sin botos ni orillas secas (parada 7).</summary>
        public const string LagoTefe = "lago_tefe";

        /// <summary>El malecón con baranda, farolas y árboles ante la ciudad baja, sin islas ni banderas (paradas 8 y 9).</summary>
        public const string Leticia = "leticia";

        /// <summary>Árboles con el pie en el agua bajo el dosel alto (parada 10).</summary>
        public const string Amacayacu = "amacayacu";

        /// <summary>Solo la boca del Loretoyacu entre dos orillas de selva, sin el pueblo (parada 11; asesor obligatorio).</summary>
        public const string PuertoNarino = "puerto_narino";

        /// <summary>Lago quieto con la orilla lejana y dos lomos de delfín, rosado y gris (parada 12).</summary>
        public const string Tarapoto = "tarapoto";

        /// <summary>Baliza de navegación en el río ancho entre dos orillas de várzea (parada 13, <c>kit:rio_ancho_con_baliza</c>).</summary>
        public const string Beacon = "beacon";

        /// <summary>El malecón con lanchas sin nadie y los techos de la ciudad (parada 14).</summary>
        public const string Iquitos = "iquitos";

        /// <summary>Casas sobre balsas: la creciente del Itaya (parada 15; pendiente del asesor cultural, obligatorio en esta parada).</summary>
        public const string Rafts = "rafts";

        /// <summary>
        /// La Casa de Fierro, reserva de la parada 15: dos pisos de hierro con balcón entre casas bajas, sin torre (sin el mito de
        /// Eiffel; 07 §8.1).
        /// </summary>
        public const string CasaDeFierro = "casa_de_fierro";

        /// <summary>Varillal de troncos finos sobre arena blanca, con la quebrada negra en segundo plano (parada 16).</summary>
        public const string Allpahuayo = "allpahuayo";

        /// <summary>El río muy ancho con Nauta lejana y una sola agua (parada 17, <c>kit:orilla_de_nauta</c>).</summary>
        public const string Nauta = "orilla_de_nauta";

        /// <summary>La cocha de agua negra como espejo con los aguajes reflejados (paradas 18 y 19).</summary>
        public const string PacayaSamiria = "pacaya_samiria";

        /// <summary>Ciudad baja y lejana (respaldo de un héroe de ciudad sin forma propia).</summary>
        public const string GenericCity = "generic_city";

        /// <summary>Cerro y orilla de selva con playa (respaldo de un héroe sin forma propia).</summary>
        public const string GenericNature = "generic_nature";

        /// <summary>Lago o río ancho con la orilla lejana baja y arbolada (respaldo de una pieza del kit sin forma propia).</summary>
        public const string GenericWater = "generic_water";

        /// <summary>
        /// Las formas propias de los sitios, en el orden de las paradas de 1-200 (la reserva tras su parada), y después las tres
        /// genéricas.
        /// </summary>
        public static readonly string[] All =
        {
            TwoSpires, AlterDoChao, FlonaTapajos, Dome, Anavilhanas, LagoTefe, Leticia, Amacayacu, PuertoNarino, Tarapoto, Beacon,
            Iquitos, Rafts, CasaDeFierro, Allpahuayo, Nauta, PacayaSamiria, GenericCity, GenericNature, GenericWater,
        };

        /// <summary>Las formas genéricas de respaldo (ninguna parada pintada de 1-200 las usa).</summary>
        public static readonly string[] Generic = { GenericCity, GenericNature, GenericWater };

        /// <summary>Ancho de la caja de una forma (dp; 07 §2.1).</summary>
        public const float WidthDp = 240f;

        /// <summary>Tope de silueta sobre el horizonte (dp; 07 §2.1).</summary>
        public const float MaxAboveDp = 48f;

        /// <summary>Tope de orilla bajo el horizonte (dp; 07 §2.1).</summary>
        public const float MaxBelowDp = 32f;

        /// <summary>
        /// Lo reconocible de la forma, sobre el horizonte, como mucho (dp): lo que se ve con un tablero de 2 filas (07 §2.1;
        /// propuesta del Mapa §10.6).
        /// </summary>
        public const float RecognizableDp = 30f;

        /// <summary>Tono del cuerpo de la silueta.</summary>
        public const int ToneBody = 0;

        /// <summary>Tono de los remates, techos y copas (más oscuro), o de la selva lejana con bruma.</summary>
        public const int ToneAccent = 1;

        /// <summary>Tono de las ventanas, columnas, farolas y cabinas (claro).</summary>
        public const int ToneLight = 2;

        /// <summary>Tono de la orilla, la playa, el malecón, las balsas o la cocha.</summary>
        public const int ToneShore = 3;

        /// <summary>Tono propio de la forma (las tejas de la cúpula, las franjas de la baliza, el barranco, el casco, el delfín rosado…).</summary>
        public const int ToneFeature = 4;

        /// <summary>Segundo tono propio [P] (los troncos finos, el delfín gris, la franja del casco, las tejas de las casas vecinas).</summary>
        public const int ToneDetail = 5;

        /// <summary>Número de tonos de cada forma.</summary>
        public const int ToneCount = 6;

        /// <summary>Degradado con el que entra y sale lo que llega a los bordes de la caja: el zócalo y la arena (dp) [P].</summary>
        public const float EdgeFadeDp = 16f;

        /// <summary>Cuerpo de la silueta a contraluz al atardecer (<c>dusk</c>) [P].</summary>
        public static readonly Color DuskBody = Palette.Hex(0x5E4A5C);

        /// <summary>Remates de la silueta a contraluz al atardecer [P].</summary>
        public static readonly Color DuskAccent = Palette.Hex(0x4B3A4C);

        /// <summary>Ventanas encendidas al atardecer [P].</summary>
        public static readonly Color DuskWindow = Palette.Hex(0xF6C77A);

        /// <summary>Opacidad de las ventanas encendidas al atardecer [P].</summary>
        public const float DuskWindowAlpha = 0.9f;

        /// <summary>Cuerpo de la silueta a contraluz al alba (<c>dawn</c>) [P].</summary>
        public static readonly Color DawnBody = Palette.Hex(0x7A7AA0);

        /// <summary>Remates de la silueta a contraluz al alba [P].</summary>
        public static readonly Color DawnAccent = Palette.Hex(0x66668C);

        /// <summary>Primitiva rectángulo: (x0, y0, x1, y1).</summary>
        public const int KindRect = 0;

        /// <summary>Primitiva triángulo: (x0, y0, x1, y1, x2, y2).</summary>
        public const int KindTriangle = 1;

        /// <summary>Primitiva cúpula: media elipse superior (cx, cy, rx, ry).</summary>
        public const int KindDome = 2;

        private const int Stride = 8;
        private const int DomeSegments = 12;

        // Primitivas: {tipo, tono, a, b, c, d, e, f}. Rectángulo (x0, y0, x1, y1); triángulo (x0, y0, x1, y1, x2, y2); cúpula =
        // media elipse superior (cx, cy, rx, ry). x desde el pie, y hacia arriba desde el horizonte, en dp. En orden de dibujo.
        private static readonly float[] TwoSpiresData =
        {
            0, ToneShore, -120, -4, 120, 2, 0, 0,
            0, ToneBody, -118, 0, -80, 6, 0, 0,
            0, ToneBody, 80, 0, 118, 7, 0, 0,
            0, ToneBody, -72, 0, 72, 11, 0, 0,
            0, ToneAccent, -76, 11, 76, 13, 0, 0,
            1, ToneAccent, -18, 13, 18, 13, 0, 18,
            0, ToneBody, -64, 0, -46, 17, 0, 0,
            0, ToneAccent, -66, 17, -44, 19, 0, 0,
            1, ToneAccent, -65, 19, -45, 19, -55, 30,
            0, ToneBody, 46, 0, 64, 17, 0, 0,
            0, ToneAccent, 44, 17, 66, 19, 0, 0,
            1, ToneAccent, 45, 19, 65, 19, 55, 30,
            0, ToneLight, -36, 3, -28, 8, 0, 0,
            0, ToneLight, -16, 3, -8, 8, 0, 0,
            0, ToneLight, 8, 3, 16, 8, 0, 0,
            0, ToneLight, 28, 3, 36, 8, 0, 0,
            0, ToneLight, -58, 11, -52, 15, 0, 0,
            0, ToneLight, 52, 11, 58, 15, 0, 0,
        };

        // La Ilha do Amor: la selva lejana con bruma, el cerro a la derecha y, delante, la lengua de arena blanca que crece hacia
        // el centro, con unas matas bajas (sin cocoteros: guía §7; sin bañistas ni el Çairé).
        private static readonly float[] AlterDoChaoData =
        {
            0, ToneAccent, -120, 0, 120, 5, 0, 0,
            2, ToneAccent, -100, 4, 16, 5, 0, 0,
            2, ToneAccent, -70, 4, 14, 4, 0, 0,
            2, ToneAccent, -40, 4, 16, 5, 0, 0,
            2, ToneBody, 22, 2, 24, 15, 0, 0,
            2, ToneBody, 56, 2, 52, 26, 0, 0,
            2, ToneFeature, 30, 9, 7, 4, 0, 0,
            2, ToneFeature, 44, 14, 9, 5, 0, 0,
            2, ToneFeature, 66, 16, 9, 5, 0, 0,
            2, ToneFeature, 80, 10, 8, 4, 0, 0,
            0, ToneShore, -120, -3, 120, 0, 0, 0,
            1, ToneShore, -104, 0, -10, 0, -10, 3,
            0, ToneShore, -10, 0, 60, 3, 0, 0,
            1, ToneShore, 60, 0, 60, 3, 86, 0,
            2, ToneDetail, -8, 3, 6, 3.5f, 0, 0,
            2, ToneDetail, 10, 3, 7, 4, 0, 0,
            2, ToneDetail, 30, 3, 6, 3, 0, 0,
        };

        // Floresta Nacional do Tapajós: la playa estrecha, el barranco de tierra firme y el dosel parejo, con copas de la misma
        // altura y ningún árbol emergente (sin samaúma: 07 §8.1); sin comunidades ni madera cortada.
        private static readonly float[] FlonaTapajosData =
        {
            0, ToneShore, -120, -2, 120, 1, 0, 0,
            0, ToneFeature, -120, 1, 120, 7, 0, 0,
            0, ToneBody, -120, 7, 120, 13, 0, 0,
            2, ToneBody, -104, 12, 15, 7, 0, 0,
            2, ToneBody, -78, 12, 15, 6, 0, 0,
            2, ToneBody, -52, 12, 15, 7, 0, 0,
            2, ToneBody, -26, 12, 15, 6, 0, 0,
            2, ToneBody, 0, 12, 15, 7, 0, 0,
            2, ToneBody, 26, 12, 15, 6, 0, 0,
            2, ToneBody, 52, 12, 15, 7, 0, 0,
            2, ToneBody, 78, 12, 15, 6, 0, 0,
            2, ToneBody, 104, 12, 15, 7, 0, 0,
            2, ToneAccent, -91, 11, 9, 5, 0, 0,
            2, ToneAccent, -65, 11, 9, 5, 0, 0,
            2, ToneAccent, -39, 11, 9, 5, 0, 0,
            2, ToneAccent, -13, 11, 9, 5, 0, 0,
            2, ToneAccent, 13, 11, 9, 5, 0, 0,
            2, ToneAccent, 39, 11, 9, 5, 0, 0,
            2, ToneAccent, 65, 11, 9, 5, 0, 0,
            2, ToneAccent, 91, 11, 9, 5, 0, 0,
        };

        // La cúpula, lo reconocible, cabe entera (con su remate) en los 30 dp que se ven sobre el horizonte con 2 filas (07 §2.1).
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
            0, ToneAccent, -14, 13, 14, 17, 0, 0,
            2, ToneFeature, 0, 17, 16, 10, 0, 0,
            0, ToneAccent, -2, 27, 2, 30, 0, 0,
        };

        // Anavilhanas: la orilla lejana con bruma y tres islas de selva escalonadas, cada una con su playa blanca; entre ellas, el
        // agua negra abierta (el espejo lo pone el reflejo).
        private static readonly float[] AnavilhanasData =
        {
            0, ToneAccent, -120, 0, 120, 3, 0, 0,
            2, ToneAccent, -36, 2, 14, 4, 0, 0,
            2, ToneAccent, 34, 2, 12, 4, 0, 0,
            0, ToneShore, -112, -2, -40, 1, 0, 0,
            2, ToneBody, -76, 0, 34, 13, 0, 0,
            2, ToneFeature, -86, 8, 10, 4, 0, 0,
            0, ToneShore, -30, -1, 30, 1, 0, 0,
            2, ToneFeature, 0, 0, 26, 9, 0, 0,
            0, ToneShore, 40, -2, 112, 1, 0, 0,
            2, ToneBody, 76, 0, 34, 15, 0, 0,
            2, ToneFeature, 66, 9, 10, 5, 0, 0,
        };

        // Lago de Tefé: la orilla verde lejana con la ciudad baja a la izquierda. Nada bajo el horizonte: ni botos ni orillas
        // secas (la mortandad de 2023; 07 §2.1 y §8.1).
        private static readonly float[] LagoTefeData =
        {
            0, ToneShore, -120, 0, 120, 3.5f, 0, 0,
            2, ToneAccent, -104, 3, 14, 4, 0, 0,
            2, ToneAccent, -84, 3, 12, 3, 0, 0,
            2, ToneAccent, 20, 3, 16, 5, 0, 0,
            2, ToneAccent, 48, 3, 14, 4, 0, 0,
            2, ToneAccent, 76, 3, 16, 5, 0, 0,
            2, ToneAccent, 104, 3, 13, 4, 0, 0,
            0, ToneBody, -70, 0, -56, 7, 0, 0,
            0, ToneFeature, -56, 0, -44, 10, 0, 0,
            0, ToneBody, -44, 0, -32, 6, 0, 0,
            0, ToneFeature, -32, 0, -18, 12, 0, 0,
            0, ToneBody, -18, 0, -6, 8, 0, 0,
            0, ToneLight, -52, 5, -48, 8, 0, 0,
            0, ToneLight, -28, 6, -24, 9, 0, 0,
            0, ToneLight, -14, 3, -10, 6, 0, 0,
        };

        // Leticia: el malecón con su baranda y cuatro farolas, los árboles del paseo y la ciudad baja detrás. Sin islas, sin
        // banderas (las farolas llevan la lámpara centrada) y sin personas.
        private static readonly float[] LeticiaData =
        {
            0, ToneBody, -114, 3, -70, 15, 0, 0,
            0, ToneBody, -50, 3, -12, 19, 0, 0,
            0, ToneBody, 12, 3, 48, 17, 0, 0,
            0, ToneBody, 70, 3, 114, 14, 0, 0,
            0, ToneLight, -100, 8, -94, 12, 0, 0,
            0, ToneLight, -86, 8, -80, 12, 0, 0,
            0, ToneLight, -38, 10, -32, 15, 0, 0,
            0, ToneLight, -26, 10, -20, 15, 0, 0,
            0, ToneLight, 22, 9, 28, 14, 0, 0,
            0, ToneLight, 34, 9, 40, 14, 0, 0,
            0, ToneLight, 82, 7, 88, 11, 0, 0,
            0, ToneLight, 96, 7, 102, 11, 0, 0,
            2, ToneFeature, -60, 3, 14, 13, 0, 0,
            2, ToneFeature, 0, 3, 13, 12, 0, 0,
            2, ToneFeature, 60, 3, 14, 13, 0, 0,
            0, ToneShore, -120, -3, 120, 3, 0, 0,
            0, ToneAccent, -108.6f, 3, -107.4f, 6, 0, 0,
            0, ToneAccent, -76.6f, 3, -75.4f, 6, 0, 0,
            0, ToneAccent, -44.6f, 3, -43.4f, 6, 0, 0,
            0, ToneAccent, -12.6f, 3, -11.4f, 6, 0, 0,
            0, ToneAccent, 11.4f, 3, 12.6f, 6, 0, 0,
            0, ToneAccent, 43.4f, 3, 44.6f, 6, 0, 0,
            0, ToneAccent, 75.4f, 3, 76.6f, 6, 0, 0,
            0, ToneAccent, 107.4f, 3, 108.6f, 6, 0, 0,
            0, ToneAccent, -112, 6, 112, 6.8f, 0, 0,
            0, ToneAccent, -84.6f, 3, -83.4f, 14, 0, 0,
            0, ToneLight, -86, 14, -82, 16, 0, 0,
            0, ToneAccent, -28.6f, 3, -27.4f, 14, 0, 0,
            0, ToneLight, -30, 14, -26, 16, 0, 0,
            0, ToneAccent, 27.4f, 3, 28.6f, 14, 0, 0,
            0, ToneLight, 26, 14, 30, 16, 0, 0,
            0, ToneAccent, 83.4f, 3, 84.6f, 14, 0, 0,
            0, ToneLight, 82, 14, 86, 16, 0, 0,
        };

        // Amacayacu: la orilla lejana y los troncos con el pie en el agua (bajan 1 dp bajo el horizonte) bajo copas altas; entre
        // los troncos se ve el agua. Sin comunidades.
        private static readonly float[] AmacayacuData =
        {
            0, ToneShore, -120, -1, 120, 2, 0, 0,
            0, ToneAccent, -99.4f, -1, -96.6f, 13, 0, 0,
            0, ToneAccent, -89.4f, -1, -86.6f, 12, 0, 0,
            0, ToneAccent, -61.4f, -1, -58.6f, 13, 0, 0,
            0, ToneAccent, -33.4f, -1, -30.6f, 13, 0, 0,
            0, ToneAccent, -23.4f, -1, -20.6f, 12, 0, 0,
            0, ToneAccent, 4.6f, -1, 7.4f, 13, 0, 0,
            0, ToneAccent, 34.6f, -1, 37.4f, 13, 0, 0,
            0, ToneAccent, 44.6f, -1, 47.4f, 12, 0, 0,
            0, ToneAccent, 72.6f, -1, 75.4f, 13, 0, 0,
            0, ToneAccent, 98.6f, -1, 101.4f, 12, 0, 0,
            2, ToneBody, -93, 12, 20, 14, 0, 0,
            2, ToneBody, -60, 13, 16, 12, 0, 0,
            2, ToneBody, -27, 12, 20, 16, 0, 0,
            2, ToneBody, 6, 12, 16, 12, 0, 0,
            2, ToneBody, 41, 13, 20, 15, 0, 0,
            2, ToneBody, 74, 12, 15, 12, 0, 0,
            2, ToneBody, 102, 11, 13, 12, 0, 0,
            2, ToneFeature, -98, 20, 8, 5, 0, 0,
            2, ToneFeature, -30, 22, 9, 5, 0, 0,
            2, ToneFeature, 38, 22, 9, 5, 0, 0,
            2, ToneFeature, 70, 18, 7, 4, 0, 0,
        };

        // Puerto Nariño, en su alternativa de 07 §2.1: solo la boca del Loretoyacu. Dos orillas de selva que bajan hacia la boca
        // y, en el hueco, la selva lejana con bruma; sin el pueblo ni nada identificable (asesor obligatorio).
        private static readonly float[] PuertoNarinoData =
        {
            0, ToneFeature, -30, 0, 30, 4, 0, 0,
            0, ToneShore, -120, -2, -14, 0, 0, 0,
            0, ToneShore, 14, -2, 120, 0, 0, 0,
            0, ToneBody, -120, 0, -18, 10, 0, 0,
            1, ToneBody, -18, 0, -18, 10, -6, 0,
            2, ToneAccent, -104, 9, 15, 7, 0, 0,
            2, ToneAccent, -80, 9, 14, 8, 0, 0,
            2, ToneAccent, -56, 9, 15, 6, 0, 0,
            2, ToneAccent, -34, 8, 12, 6, 0, 0,
            0, ToneBody, 18, 0, 120, 12, 0, 0,
            1, ToneBody, 18, 0, 18, 12, 6, 0,
            2, ToneAccent, 34, 11, 13, 7, 0, 0,
            2, ToneAccent, 58, 11, 15, 8, 0, 0,
            2, ToneAccent, 82, 11, 14, 7, 0, 0,
            2, ToneAccent, 104, 11, 14, 6, 0, 0,
        };

        // Lagos de Tarapoto: el lago quieto con la orilla lejana baja y, en el agua, un lomo de delfín rosado y otro gris con su
        // aleta (07 §2.1; las victorias regias, lejos del panel: aquí no hay).
        private static readonly float[] TarapotoData =
        {
            0, ToneShore, -120, 0, 120, 3.5f, 0, 0,
            2, ToneAccent, -100, 3, 16, 6, 0, 0,
            2, ToneAccent, -70, 3, 14, 5, 0, 0,
            2, ToneAccent, -40, 3, 16, 7, 0, 0,
            2, ToneAccent, -8, 3, 14, 5, 0, 0,
            2, ToneAccent, 24, 3, 16, 7, 0, 0,
            2, ToneAccent, 56, 3, 14, 5, 0, 0,
            2, ToneAccent, 88, 3, 16, 6, 0, 0,
            2, ToneFeature, -44, -15, 11, 4, 0, 0,
            2, ToneDetail, 34, -10, 8, 3, 0, 0,
            1, ToneDetail, 31, -7.4f, 36, -7.4f, 32, -3.6f,
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
            0, ToneAccent, -8, 0, -4, 18, 0, 0,
            0, ToneFeature, -10, 6, -2, 9, 0, 0,
            0, ToneFeature, -10, 12, -2, 15, 0, 0,
            1, ToneFeature, -14, 18, 2, 18, -6, 28,
        };

        // Iquitos: los techos de la ciudad (a dos aguas y planos con cornisa), el malecón con su baranda y, delante, dos lanchas
        // de dos cubiertas sin nadie (motocarros, ninguno).
        private static readonly float[] IquitosData =
        {
            0, ToneBody, -114, 3, -86, 12, 0, 0,
            1, ToneAccent, -116, 12, -84, 12, -100, 17,
            0, ToneBody, -84, 3, -60, 16, 0, 0,
            0, ToneAccent, -85, 16, -59, 17.5f, 0, 0,
            0, ToneBody, -58, 3, -30, 11, 0, 0,
            1, ToneAccent, -60, 11, -28, 11, -44, 15,
            0, ToneBody, -28, 3, -2, 18, 0, 0,
            0, ToneAccent, -29, 18, -1, 19.5f, 0, 0,
            0, ToneBody, 0, 3, 26, 13, 0, 0,
            1, ToneAccent, -2, 13, 28, 13, 13, 18,
            0, ToneBody, 28, 3, 58, 15, 0, 0,
            0, ToneAccent, 27, 15, 59, 16.5f, 0, 0,
            0, ToneBody, 60, 3, 86, 12, 0, 0,
            1, ToneAccent, 58, 12, 88, 12, 73, 16,
            0, ToneBody, 88, 3, 114, 17, 0, 0,
            0, ToneAccent, 87, 17, 115, 18.5f, 0, 0,
            0, ToneLight, -76, 8, -70, 12, 0, 0,
            0, ToneLight, -20, 9, -14, 13, 0, 0,
            0, ToneLight, 36, 7, 42, 11, 0, 0,
            0, ToneLight, 96, 9, 102, 13, 0, 0,
            0, ToneShore, -120, -3, 120, 3, 0, 0,
            0, ToneAccent, -114, 5, 114, 5.8f, 0, 0,
            0, ToneFeature, -92, -11, -46, -7, 0, 0,
            1, ToneFeature, -46, -11, -46, -7, -38, -6.5f,
            0, ToneDetail, -92, -8.5f, -46, -7.5f, 0, 0,
            0, ToneLight, -86, -7, -52, -3, 0, 0,
            0, ToneAccent, -88, -3, -50, -2, 0, 0,
            0, ToneLight, -80, -2, -60, 1, 0, 0,
            0, ToneAccent, -82, 1, -58, 2, 0, 0,
            0, ToneFeature, 36, -10, 78, -6.5f, 0, 0,
            1, ToneFeature, 36, -10, 28, -6, 36, -6.5f,
            0, ToneDetail, 36, -7.5f, 78, -6.7f, 0, 0,
            0, ToneLight, 42, -6.5f, 72, -3, 0, 0,
            0, ToneAccent, 40, -3, 74, -2, 0, 0,
        };

        private static readonly float[] RaftsData =
        {
            2, ToneFeature, -88, 0, 28, 9, 0, 0,
            2, ToneFeature, -30, 0, 34, 8, 0, 0,
            2, ToneFeature, 40, 0, 36, 10, 0, 0,
            2, ToneFeature, 96, 0, 20, 8, 0, 0,
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

        // La Casa de Fierro (reserva de la parada 15): dos pisos de hierro con balcón corrido, baranda y ventanas altas, cornisa y
        // un tejado bajo, entre casas vecinas de dos aguas. Sin torre ni nada que evoque a Eiffel (07 §8.1).
        private static readonly float[] CasaDeFierroData =
        {
            0, ToneShore, -120, -3, 120, 2, 0, 0,
            0, ToneFeature, -114, 2, -70, 13, 0, 0,
            0, ToneDetail, -115, 13, -69, 14.5f, 0, 0,
            0, ToneFeature, -68, 2, -38, 15, 0, 0,
            0, ToneDetail, -69, 15, -37, 16.5f, 0, 0,
            0, ToneFeature, 38, 2, 70, 14, 0, 0,
            0, ToneDetail, 37, 14, 71, 15.5f, 0, 0,
            0, ToneFeature, 72, 2, 114, 12, 0, 0,
            0, ToneDetail, 71, 12, 115, 13.5f, 0, 0,
            0, ToneLight, -104, 6, -98, 10, 0, 0,
            0, ToneLight, -88, 6, -82, 10, 0, 0,
            0, ToneLight, -58, 7, -52, 11, 0, 0,
            0, ToneLight, 48, 6, 54, 10, 0, 0,
            0, ToneLight, 60, 6, 66, 10, 0, 0,
            0, ToneLight, 84, 5, 90, 9, 0, 0,
            0, ToneLight, 100, 5, 106, 9, 0, 0,
            0, ToneBody, -32, 2, 32, 12, 0, 0,
            0, ToneAccent, -27, 3, -19, 10, 0, 0,
            0, ToneAccent, -13, 3, -5, 10, 0, 0,
            0, ToneAccent, 5, 3, 13, 10, 0, 0,
            0, ToneAccent, 19, 3, 27, 10, 0, 0,
            0, ToneBody, -32, 13.5f, 32, 23, 0, 0,
            0, ToneLight, -27, 15, -21, 21, 0, 0,
            0, ToneLight, -14, 15, -8, 21, 0, 0,
            0, ToneLight, -3, 15, 3, 21, 0, 0,
            0, ToneLight, 8, 15, 14, 21, 0, 0,
            0, ToneLight, 21, 15, 27, 21, 0, 0,
            0, ToneAccent, -36, 12, 36, 13.5f, 0, 0,
            0, ToneAccent, -34.4f, 13.5f, -33.6f, 16, 0, 0,
            0, ToneAccent, -20.8f, 13.5f, -20, 16, 0, 0,
            0, ToneAccent, -7.2f, 13.5f, -6.4f, 16, 0, 0,
            0, ToneAccent, 6.4f, 13.5f, 7.2f, 16, 0, 0,
            0, ToneAccent, 20, 13.5f, 20.8f, 16, 0, 0,
            0, ToneAccent, 33.6f, 13.5f, 34.4f, 16, 0, 0,
            0, ToneAccent, -36, 16, 36, 16.8f, 0, 0,
            0, ToneAccent, -35, 23, 35, 24.5f, 0, 0,
            1, ToneAccent, -32, 24.5f, 32, 24.5f, 0, 28,
        };

        // Allpahuayo-Mishana: la selva lejana con bruma; a la izquierda, la arena blanca con el varillal de troncos finos y copas
        // pequeñas; a la derecha, la quebrada de agua negra en segundo plano (el primer plano es el agua blanca del mundo 5). Sin
        // minería.
        private static readonly float[] AllpahuayoData =
        {
            0, ToneAccent, -120, 1, 120, 6, 0, 0,
            2, ToneAccent, 40, 5, 12, 4, 0, 0,
            2, ToneAccent, 64, 5, 12, 5, 0, 0,
            2, ToneAccent, 88, 5, 12, 4, 0, 0,
            2, ToneAccent, 108, 5, 10, 4, 0, 0,
            0, ToneFeature, 14, -3, 120, 1, 0, 0,
            0, ToneShore, -120, -3, 18, 2, 0, 0,
            1, ToneShore, 18, -3, 18, 2, 34, -3,
            0, ToneDetail, -110.6f, 1.5f, -109.4f, 14, 0, 0,
            0, ToneDetail, -103.6f, 1.5f, -102.4f, 18, 0, 0,
            0, ToneDetail, -96.6f, 1.5f, -95.4f, 13, 0, 0,
            0, ToneDetail, -88.6f, 1.5f, -87.4f, 17, 0, 0,
            0, ToneDetail, -80.6f, 1.5f, -79.4f, 20, 0, 0,
            0, ToneDetail, -72.6f, 1.5f, -71.4f, 15, 0, 0,
            0, ToneDetail, -64.6f, 1.5f, -63.4f, 19, 0, 0,
            0, ToneDetail, -56.6f, 1.5f, -55.4f, 14, 0, 0,
            0, ToneDetail, -48.6f, 1.5f, -47.4f, 18, 0, 0,
            0, ToneDetail, -40.6f, 1.5f, -39.4f, 21, 0, 0,
            0, ToneDetail, -32.6f, 1.5f, -31.4f, 16, 0, 0,
            0, ToneDetail, -24.6f, 1.5f, -23.4f, 19, 0, 0,
            0, ToneDetail, -16.6f, 1.5f, -15.4f, 13, 0, 0,
            0, ToneDetail, -8.6f, 1.5f, -7.4f, 17, 0, 0,
            0, ToneDetail, -0.6f, 1.5f, 0.6f, 15, 0, 0,
            0, ToneDetail, 7.4f, 1.5f, 8.6f, 12, 0, 0,
            2, ToneBody, -110, 12, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -103, 16, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -96, 11, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -88, 15, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -80, 18, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -72, 13, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -64, 17, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -56, 12, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -48, 16, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -40, 19, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -32, 14, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -24, 17, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -16, 11, 4.5f, 3.5f, 0, 0,
            2, ToneBody, -8, 15, 4.5f, 3.5f, 0, 0,
            2, ToneBody, 0, 13, 4.5f, 3.5f, 0, 0,
            2, ToneBody, 8, 10, 4.5f, 3.5f, 0, 0,
        };

        // Nauta (solo kit): el río muy ancho, la orilla lejana con bruma y Nauta pequeña y lejos a la derecha. Una sola agua: la
        // confluencia en Y va en la tarjeta y en el Mapa (propuesta del Mapa §10.3).
        private static readonly float[] NautaData =
        {
            0, ToneShore, -120, 0, 120, 3, 0, 0,
            2, ToneAccent, -96, 2.5f, 16, 4, 0, 0,
            2, ToneAccent, -64, 2.5f, 14, 3.5f, 0, 0,
            2, ToneAccent, -30, 2.5f, 16, 4, 0, 0,
            2, ToneAccent, 0, 2.5f, 12, 3, 0, 0,
            2, ToneAccent, 106, 2.5f, 12, 4, 0, 0,
            0, ToneBody, 30, 0, 40, 6, 0, 0,
            0, ToneBody, 40, 0, 52, 9, 0, 0,
            0, ToneBody, 52, 0, 60, 5, 0, 0,
            0, ToneBody, 60, 0, 72, 10, 0, 0,
            0, ToneBody, 72, 0, 82, 7, 0, 0,
            0, ToneBody, 82, 0, 92, 5, 0, 0,
            0, ToneLight, 44, 4, 47, 6.5f, 0, 0,
            0, ToneLight, 64, 5, 67, 7.5f, 0, 0,
            0, ToneLight, 75, 3, 78, 5.5f, 0, 0,
        };

        // Pacaya-Samiria: la selva lejana, seis aguajes (tronco fino, copa redonda con abanico) y, bajo el horizonte, la cocha de
        // agua negra como espejo con la selva y los troncos reflejados (el primer plano es el agua blanca del mundo 5).
        private static readonly float[] PacayaSamiriaData =
        {
            0, ToneAccent, -120, 0, 120, 6, 0, 0,
            2, ToneAccent, -104, 5, 14, 4, 0, 0,
            2, ToneAccent, -12, 5, 14, 4, 0, 0,
            2, ToneAccent, 104, 5, 12, 4, 0, 0,
            0, ToneShore, -120, -10, 120, 0, 0, 0,
            0, ToneFeature, -120, -6, 120, 0, 0, 0,
            0, ToneDetail, -88.8f, -10, -87.2f, 0, 0, 0,
            0, ToneDetail, -58.8f, -10, -57.2f, 0, 0, 0,
            0, ToneDetail, -30.8f, -10, -29.2f, 0, 0, 0,
            0, ToneDetail, 13.2f, -10, 14.8f, 0, 0, 0,
            0, ToneDetail, 45.2f, -10, 46.8f, 0, 0, 0,
            0, ToneDetail, 83.2f, -10, 84.8f, 0, 0, 0,
            0, ToneDetail, -88.8f, 0, -87.2f, 18, 0, 0,
            2, ToneBody, -88, 17, 5, 4, 0, 0,
            1, ToneBody, -89, 18, -98, 13, -96, 17,
            1, ToneBody, -87, 18, -78, 13, -80, 17,
            1, ToneBody, -94, 19, -90, 19, -95, 23,
            1, ToneBody, -86, 19, -82, 19, -81, 23,
            1, ToneBody, -90, 20, -86, 20, -88, 24,
            0, ToneDetail, -58.8f, 0, -57.2f, 22, 0, 0,
            2, ToneBody, -58, 21, 5, 4, 0, 0,
            1, ToneBody, -59, 22, -68, 17, -66, 21,
            1, ToneBody, -57, 22, -48, 17, -50, 21,
            1, ToneBody, -64, 23, -60, 23, -65, 27,
            1, ToneBody, -56, 23, -52, 23, -51, 27,
            1, ToneBody, -60, 24, -56, 24, -58, 28,
            0, ToneDetail, -30.8f, 0, -29.2f, 16, 0, 0,
            2, ToneBody, -30, 15, 5, 4, 0, 0,
            1, ToneBody, -31, 16, -40, 11, -38, 15,
            1, ToneBody, -29, 16, -20, 11, -22, 15,
            1, ToneBody, -36, 17, -32, 17, -37, 21,
            1, ToneBody, -28, 17, -24, 17, -23, 21,
            1, ToneBody, -32, 18, -28, 18, -30, 22,
            0, ToneDetail, 13.2f, 0, 14.8f, 20, 0, 0,
            2, ToneBody, 14, 19, 5, 4, 0, 0,
            1, ToneBody, 13, 20, 4, 15, 6, 19,
            1, ToneBody, 15, 20, 24, 15, 22, 19,
            1, ToneBody, 8, 21, 12, 21, 7, 25,
            1, ToneBody, 16, 21, 20, 21, 21, 25,
            1, ToneBody, 12, 22, 16, 22, 14, 26,
            0, ToneDetail, 45.2f, 0, 46.8f, 24, 0, 0,
            2, ToneBody, 46, 23, 5, 4, 0, 0,
            1, ToneBody, 45, 24, 36, 19, 38, 23,
            1, ToneBody, 47, 24, 56, 19, 54, 23,
            1, ToneBody, 40, 25, 44, 25, 39, 29,
            1, ToneBody, 48, 25, 52, 25, 53, 29,
            1, ToneBody, 44, 26, 48, 26, 46, 30,
            0, ToneDetail, 83.2f, 0, 84.8f, 17, 0, 0,
            2, ToneBody, 84, 16, 5, 4, 0, 0,
            1, ToneBody, 83, 17, 74, 12, 76, 16,
            1, ToneBody, 85, 17, 94, 12, 92, 16,
            1, ToneBody, 78, 18, 82, 18, 77, 22,
            1, ToneBody, 86, 18, 90, 18, 91, 22,
            1, ToneBody, 82, 19, 86, 19, 84, 23,
        };

        private static readonly float[] GenericCityData =
        {
            0, ToneShore, -120, -4, 120, 2, 0, 0,
            0, ToneBody, -116, 0, -92, 12, 0, 0,
            0, ToneBody, -92, 0, -70, 20, 0, 0,
            1, ToneAccent, -92, 20, -70, 20, -81, 26,
            0, ToneBody, -70, 0, -50, 10, 0, 0,
            0, ToneBody, -50, 0, -24, 24, 0, 0,
            0, ToneAccent, -38, 24, -36, 30, 0, 0,
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
            2, ToneBody, 10, 0, 104, 24, 0, 0,
            2, ToneAccent, -96, 0, 16, 12, 0, 0,
            2, ToneAccent, -74, 2, 14, 14, 0, 0,
            2, ToneAccent, -50, 8, 16, 14, 0, 0,
            2, ToneAccent, -24, 14, 14, 13, 0, 0,
            2, ToneFeature, 4, 18, 16, 12, 0, 0,
            2, ToneAccent, 30, 16, 14, 12, 0, 0,
            2, ToneFeature, 54, 10, 16, 13, 0, 0,
            2, ToneAccent, 80, 4, 14, 13, 0, 0,
            2, ToneAccent, 104, 0, 14, 10, 0, 0,
        };

        private static readonly float[] GenericWaterData =
        {
            0, ToneShore, -120, 0, 120, 3, 0, 0,
            2, ToneBody, -102, 2, 16, 9, 0, 0,
            2, ToneAccent, -80, 2, 12, 11, 0, 0,
            2, ToneBody, -58, 2, 16, 8, 0, 0,
            2, ToneAccent, -30, 2, 14, 12, 0, 0,
            2, ToneBody, -4, 2, 18, 10, 0, 0,
            2, ToneAccent, 24, 2, 12, 13, 0, 0,
            2, ToneBody, 48, 2, 16, 9, 0, 0,
            2, ToneAccent, 74, 2, 14, 11, 0, 0,
            2, ToneBody, 100, 2, 16, 8, 0, 0,
        };

        // Tonos de cada forma [P], en el orden ToneBody, ToneAccent, ToneLight, ToneShore, ToneFeature y ToneDetail: siluetas
        // lejanas con bruma (perspectiva aérea), claras para que la escena siga brillante (VA-03: nada por debajo de V 0,25) y
        // sin el turquesa caribeño ni banderas (guía §7; VA-22).
        private static readonly int[] DefaultTones = { 0x8291A2, 0x667386, 0xF2E6C8, 0x8E7C5E, 0x8291A2, 0x8291A2 };
        private static readonly int[] AlterDoChaoTones = { 0x4F8E5C, 0x6E9C7C, 0xF2E6C8, 0xEFE4C6, 0x62A06C, 0x5A9462 };
        private static readonly int[] FlonaTapajosTones = { 0x4C8758, 0x3F774B, 0xF2E6C8, 0xE6D4A8, 0xB98A5C, 0x5E9D66 };
        private static readonly int[] DomeTones = { 0x8291A2, 0x667386, 0xF2E6C8, 0x8E7C5E, 0xC8B04E, 0xC8B04E };
        private static readonly int[] AnavilhanasTones = { 0x4A8A58, 0x6E9C7C, 0xF2E6C8, 0xF1E8D0, 0x57966A, 0x57966A };
        private static readonly int[] LagoTefeTones = { 0x8A97A6, 0x4E7E58, 0xF2E6C8, 0x5E8E66, 0x6E8296, 0x6E8296 };
        private static readonly int[] LeticiaTones = { 0x9AA6B4, 0x6A7686, 0xF2E6C8, 0xCFC6B2, 0x5E9D66, 0x5E9D66 };
        private static readonly int[] AmacayacuTones = { 0x4E8A58, 0x6E5E4A, 0xF2E6C8, 0x6F8A62, 0x66A46E, 0x66A46E };
        private static readonly int[] PuertoNarinoTones = { 0x4C8A58, 0x40784C, 0xF2E6C8, 0xD8C8A0, 0x7AA286, 0x7AA286 };
        private static readonly int[] TarapotoTones = { 0x5E8E66, 0x4E7E58, 0xF2E6C8, 0x5E8E66, 0xD69A98, 0x8A929A };
        private static readonly int[] BeaconTones = { 0x568A60, 0x66717F, 0xF2E6C8, 0x8F8C6C, 0xD45A44, 0xD45A44 };
        private static readonly int[] IquitosTones = { 0x9AA2AE, 0x7A6656, 0xF2E6C8, 0xCFC6B2, 0x5A7E8C, 0xC0584A };
        private static readonly int[] RaftsTones = { 0xA58566, 0x74604C, 0xF2E6C8, 0x7E6650, 0x5A8A62, 0x5A8A62 };
        private static readonly int[] CasaDeFierroTones = { 0x8C98A6, 0x5F6B78, 0xF2E6C8, 0xCFC6B2, 0xC9B48E, 0xB0704E };
        private static readonly int[] AllpahuayoTones = { 0x5C9466, 0x6E9C7C, 0xF2E6C8, 0xF0EADB, 0x4A3426, 0x8E7E66 };
        private static readonly int[] NautaTones = { 0xA5AEB8, 0x7A9A80, 0xF2E6C8, 0x86A08A, 0xA5AEB8, 0xA5AEB8 };
        private static readonly int[] PacayaSamiriaTones = { 0x4E8C5A, 0x5E8E68, 0xF2E6C8, 0x4C3828, 0x3F5A40, 0x84705A };
        private static readonly int[] GenericNatureTones = { 0x548E5F, 0x437A4E, 0xF2E6C8, 0xE6D4A8, 0x5E9D66, 0x5E9D66 };
        private static readonly int[] GenericWaterTones = { 0x568A60, 0x467852, 0xF2E6C8, 0x8A9670, 0x5E9D66, 0x5E9D66 };

        // Héroe pintado (ranura bg_hero_<sitio>) o pieza del kit (kit:<pieza>) → forma de respaldo [P]. Cada héroe pintado de
        // 1-200 y la reserva (la Casa de Fierro) tienen la suya; las dos paradas solo con el kit, también.
        private static readonly string[] SourceShapes =
        {
            "bg_hero_ver_o_peso", TwoSpires,
            "bg_hero_alter_do_chao", AlterDoChao,
            "bg_hero_flona_tapajos", FlonaTapajos,
            "bg_hero_teatro_amazonas", Dome,
            "bg_hero_anavilhanas", Anavilhanas,
            "bg_hero_lago_tefe", LagoTefe,
            "bg_hero_leticia", Leticia,
            "bg_hero_amacayacu", Amacayacu,
            "bg_hero_puerto_narino", PuertoNarino,
            "bg_hero_tarapoto", Tarapoto,
            "bg_hero_iquitos", Iquitos,
            "bg_hero_itaya_creciente", Rafts,
            "bg_hero_casa_de_fierro", CasaDeFierro,
            "bg_hero_allpahuayo", Allpahuayo,
            "bg_hero_pacaya_samiria", PacayaSamiria,
            "kit:rio_ancho_con_baliza", Beacon,
            "kit:orilla_de_nauta", Nauta,
        };

        /// <summary>Número de pares (héroe o pieza del kit → forma) con forma propia.</summary>
        public static int SourceCount => SourceShapes.Length / 2;

        /// <summary>El héroe pintado o la pieza del kit del par <paramref name="i"/> (0 … <see cref="SourceCount"/> − 1).</summary>
        public static string SourceAt(int i)
        {
            return SourceShapes[2 * i];
        }

        /// <summary>La forma propia del par <paramref name="i"/>.</summary>
        public static string SourceShapeAt(int i)
        {
            return SourceShapes[(2 * i) + 1];
        }

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

        /// <summary>Si <paramref name="shape"/> es una de las formas genéricas de respaldo (<see cref="Generic"/>).</summary>
        public static bool IsGeneric(string? shape)
        {
            return shape == GenericCity || shape == GenericNature || shape == GenericWater;
        }

        /// <summary>Primitivas de <paramref name="shape"/> (0 si no la conoce).</summary>
        public static int PrimitiveCount(string shape)
        {
            float[]? data = Data(shape);
            return data == null ? 0 : data.Length / Stride;
        }

        /// <summary>
        /// Primitiva <paramref name="index"/> de <paramref name="shape"/>: su tipo (<see cref="KindRect"/>,
        /// <see cref="KindTriangle"/> o <see cref="KindDome"/>), su tono y su caja en dp desde el pie (las cúpulas, con su radio).
        /// Para los tests de las reglas de cada sitio.
        /// </summary>
        public static void PrimitiveAt(string shape, int index, out int kind, out int tone, out Rect box)
        {
            float[] d = Data(shape) ?? throw new ArgumentException("forma desconocida: " + shape, nameof(shape));
            int p = index * Stride;
            kind = (int)d[p];
            tone = (int)d[p + 1];
            box = PrimitiveBounds(d, p);
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
                Rect r = PrimitiveBounds(data, p);
                xMin = Mathf.Min(xMin, r.xMin);
                xMax = Mathf.Max(xMax, r.xMax);
                yMin = Mathf.Min(yMin, r.yMin);
                yMax = Mathf.Max(yMax, r.yMax);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Color del tono <paramref name="tone"/> (<see cref="ToneBody"/> … <see cref="ToneDetail"/>) de la forma
        /// <paramref name="shape"/> [P]: siluetas lejanas con bruma (perspectiva aérea), claras para que la escena siga brillante
        /// (VA-03: nada por debajo de V 0,25) y sin el turquesa caribeño ni banderas (guía §7; VA-22).
        /// </summary>
        public static Color ToneColor(string shape, int tone)
        {
            int[] tones = TonesOf(shape);
            return Palette.Hex(tones[tone >= 0 && tone < ToneCount ? tone : ToneBody]);
        }

        /// <summary>
        /// Color con el que se pinta el tono <paramref name="tone"/> de <paramref name="shape"/> con la luz de la parada
        /// (<paramref name="lut"/>, su multiplicador, y <paramref name="light"/>, su clave) [P]. De día, el tono de
        /// <see cref="ToneColor"/> por la luz. Con <c>dusk</c> o <c>dawn</c>, la silueta va a contraluz (D-003: luz de juego
        /// cálida, no un gris de pizarra apagado): el cuerpo y lo propio de la forma en <see cref="DuskBody"/> o
        /// <see cref="DawnBody"/> y los remates en <see cref="DuskAccent"/> o <see cref="DawnAccent"/>; al atardecer, las ventanas
        /// encendidas en <see cref="DuskWindow"/> al <see cref="DuskWindowAlpha"/>. La orilla y las ventanas del alba siguen con la
        /// luz multiplicada.
        /// </summary>
        public static Color LitColor(string shape, int tone, Color lut, string? light)
        {
            bool dusk = light == StopBackdrop.LutDusk;
            bool dawn = light == StopBackdrop.LutDawn;
            if (dusk || dawn)
            {
                switch (tone)
                {
                    case ToneBody:
                    case ToneFeature:
                    case ToneDetail:
                        return dusk ? DuskBody : DawnBody;
                    case ToneAccent:
                        return dusk ? DuskAccent : DawnAccent;
                    case ToneLight:
                        if (dusk)
                        {
                            Color window = DuskWindow;
                            window.a = DuskWindowAlpha;
                            return window;
                        }

                        break;
                }
            }

            return Lut(ToneColor(shape, tone), lut);
        }

        /// <summary>
        /// Pinta <paramref name="shape"/> en <paramref name="g"/> con el pie en (<paramref name="footX"/>, <paramref name="horizon"/>)
        /// dp desde arriba y a escala <paramref name="k"/> (ancho del lienzo / 360). Primero el reflejo de lo que queda sobre el
        /// horizonte, volteado, teñido a medias con el agua <paramref name="water"/>, con opacidad <paramref name="reflectionAlpha"/>
        /// en la línea del agua que baja a 0 a <paramref name="reflectionDp"/> dp (y recortado ahí); después la silueta, con
        /// cada tono según la luz de la parada (<see cref="LitColor"/>: <paramref name="lut"/> y su clave
        /// <paramref name="light"/>). Lo que llega a los bordes de la caja de 240 dp (el zócalo y la arena) entra y sale con un
        /// degradado de <see cref="EdgeFadeDp"/> dp, sin aristas verticales. No vacía el gráfico ni asigna memoria (salvo que su
        /// búfer tenga que crecer). Devuelve false si no conoce la forma.
        /// </summary>
        public static bool Draw(StopShapeGraphic g, string shape, float footX, float horizon, float k, Color lut, string? light,
            Color water, float reflectionAlpha, float reflectionDp)
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
                    Color lit = LitColor(shape, (int)data[p + 1], lut, light);
                    Color c = Color.Lerp(lit, water, 0.5f);
                    EmitPrimitive(g, data, p, footX, horizon, k, c, true, reflectionAlpha * lit.a, reflectionDp);
                }
            }

            for (int p = 0; p < data.Length; p += Stride)
            {
                Color c = LitColor(shape, (int)data[p + 1], lut, light);
                EmitPrimitive(g, data, p, footX, horizon, k, c, false, c.a, 0f);
            }

            return true;
        }

        private static Rect PrimitiveBounds(float[] d, int p)
        {
            int kind = (int)d[p];
            if (kind == KindDome)
            {
                return Rect.MinMaxRect(d[p + 2] - d[p + 4], d[p + 3], d[p + 2] + d[p + 4], d[p + 3] + d[p + 5]);
            }

            int points = kind == KindTriangle ? 3 : 2;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < points; i++)
            {
                float x = d[p + 2 + (2 * i)];
                float y = d[p + 3 + (2 * i)];
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Color Lut(Color c, Color lut)
        {
            return new Color(c.r * lut.r, c.g * lut.g, c.b * lut.b, 1f);
        }

        // Una primitiva: tal cual o, con mirror, su parte de sobre el horizonte volteada bajo él, con la opacidad del reflejo
        // decreciente con la profundidad y recortada a depth dp (a escala k). alpha es su opacidad (en el reflejo, en la línea
        // del agua).
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

                // Lo que llega a un borde de la caja (el zócalo, la arena) se funde en EdgeFadeDp dp.
                const float half = WidthDp * 0.5f;
                bool fadeLeft = x0 <= -half + 0.01f;
                bool fadeRight = x1 >= half - 0.01f;
                float xa = fadeLeft ? x0 + EdgeFadeDp : x0;
                float xb = fadeRight ? x1 - EdgeFadeDp : x1;
                if ((fadeLeft || fadeRight) && xb > xa)
                {
                    if (fadeLeft)
                    {
                        RectPart(g, x0, xa, y0, y1, 0f, 1f, footX, horizon, k, c, mirror, alpha, depth);
                    }

                    RectPart(g, xa, xb, y0, y1, 1f, 1f, footX, horizon, k, c, mirror, alpha, depth);
                    if (fadeRight)
                    {
                        RectPart(g, xb, x1, y0, y1, 1f, 0f, footX, horizon, k, c, mirror, alpha, depth);
                    }

                    return;
                }

                RectPart(g, x0, x1, y0, y1, 1f, 1f, footX, horizon, k, c, mirror, alpha, depth);
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

        // Tramo de x0 a x1 de un rectángulo de y0 a y1, con la opacidad multiplicada por fa a la izquierda y por fb a la derecha.
        private static void RectPart(StopShapeGraphic g, float x0, float x1, float y0, float y1, float fa, float fb, float footX,
            float horizon, float k, Color c, bool mirror, float alpha, float depth)
        {
            Vertex(g, x0, y1, footX, horizon, k, c, mirror, alpha * fa, depth, out int a);
            Vertex(g, x1, y1, footX, horizon, k, c, mirror, alpha * fb, depth, out int b);
            Vertex(g, x1, y0, footX, horizon, k, c, mirror, alpha * fb, depth, out int e);
            Vertex(g, x0, y0, footX, horizon, k, c, mirror, alpha * fa, depth, out int f);
            g.AddTriangle(a, b, e);
            g.AddTriangle(a, e, f);
        }

        private static float MirrorClamp(float y, bool mirror)
        {
            return mirror ? Mathf.Max(0f, y) : y;
        }

        // Vértice de la forma: x desde el pie e y hacia arriba en dp → dp del lienzo desde arriba, con opacidad alpha. En el
        // reflejo, la y se voltea bajo el horizonte, se recorta a depth y la opacidad baja de alpha a 0 con la profundidad.
        private static void Vertex(StopShapeGraphic g, float x, float y, float footX, float horizon, float k, Color c, bool mirror,
            float alpha, float depth, out int index)
        {
            float px = footX + (x * k);
            if (!mirror)
            {
                c.a = alpha;
                index = g.AddVertex(px, horizon - (y * k), c);
                return;
            }

            float below = Mathf.Min(y, depth);
            c.a = alpha * (1f - (below / depth));
            index = g.AddVertex(px, horizon + (below * k), c);
        }

        private static int[] TonesOf(string shape)
        {
            switch (shape)
            {
                case AlterDoChao: return AlterDoChaoTones;
                case FlonaTapajos: return FlonaTapajosTones;
                case Dome: return DomeTones;
                case Anavilhanas: return AnavilhanasTones;
                case LagoTefe: return LagoTefeTones;
                case Leticia: return LeticiaTones;
                case Amacayacu: return AmacayacuTones;
                case PuertoNarino: return PuertoNarinoTones;
                case Tarapoto: return TarapotoTones;
                case Beacon: return BeaconTones;
                case Iquitos: return IquitosTones;
                case Rafts: return RaftsTones;
                case CasaDeFierro: return CasaDeFierroTones;
                case Allpahuayo: return AllpahuayoTones;
                case Nauta: return NautaTones;
                case PacayaSamiria: return PacayaSamiriaTones;
                case GenericNature: return GenericNatureTones;
                case GenericWater: return GenericWaterTones;
                default: return DefaultTones; // TwoSpires, GenericCity
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
                case AlterDoChao: return AlterDoChaoData;
                case FlonaTapajos: return FlonaTapajosData;
                case Dome: return DomeData;
                case Anavilhanas: return AnavilhanasData;
                case LagoTefe: return LagoTefeData;
                case Leticia: return LeticiaData;
                case Amacayacu: return AmacayacuData;
                case PuertoNarino: return PuertoNarinoData;
                case Tarapoto: return TarapotoData;
                case Beacon: return BeaconData;
                case Iquitos: return IquitosData;
                case Rafts: return RaftsData;
                case CasaDeFierro: return CasaDeFierroData;
                case Allpahuayo: return AllpahuayoData;
                case Nauta: return NautaData;
                case PacayaSamiria: return PacayaSamiriaData;
                case GenericCity: return GenericCityData;
                case GenericNature: return GenericNatureData;
                case GenericWater: return GenericWaterData;
                default: return null;
            }
        }
    }
}
