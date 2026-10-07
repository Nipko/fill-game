#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Colores del juego con los HEX canónicos: 14 tintes (GDD 07 §3, glosario §9), agua base de los mundos
    /// (07 §2.1, 08 §5.1), tapa de calabaza (07 §4.4) y tokens de UI (07 §7.2). Los tintes solo se usan dentro de las
    /// cuias (07 §3, regla 6); la UI vive en los tokens.
    /// </summary>
    public static class Palette
    {
        private static readonly Color32[] Tints =
        {
            new Color32(0x00, 0x00, 0x00, 0x00), // 0: sin color
            Hex(0xFAFAF7), // 1 guanabana  · Guanábana
            Hex(0xE6D3A3), // 2 cupuacu    · Copoazú
            Hex(0xF2B705), // 3 tapereba   · Taperebá
            Hex(0xEF7D1A), // 4 aguaje     · Aguaje
            Hex(0xCF3E1C), // 5 urucum     · Achiote
            Hex(0x9E1B22), // 6 camu_camu  · Camu camu
            Hex(0xE58FC6), // 7 bacaba     · Bacaba
            Hex(0x6A2C91), // 8 acai       · Açaí
            Hex(0x1F2F6B), // 9 jenipapo   · Huito
            Hex(0x3BB8D9), // 10 agua_tapajos · Agua del Tapajós
            Hex(0x4EB265), // 11 lulo      · Lulo
            Hex(0xBBCC33), // 12 araza     · Arazá
            Hex(0x5C3317), // 13 cacao     · Cacao
            Hex(0xA9672E), // 14 agua_negro · Agua del Río Negro
        };

        /// <summary>Agua base negra #4A2C14 (07 §2.1).</summary>
        public static readonly Color WaterBlack = Hex(0x4A2C14);

        /// <summary>Agua base blanca #B08A5A (07 §2.1; Estuario).</summary>
        public static readonly Color WaterWhite = Hex(0xB08A5A);

        /// <summary>Agua base clara #2E8B8B (07 §2.1).</summary>
        public static readonly Color WaterClear = Hex(0x2E8B8B);

        /// <summary>Tapa de calabaza, madera clara #C9A36B (07 §4.4).</summary>
        public static readonly Color CapWood = Hex(0xC9A36B);

        /// <summary>Fibra de arumã trenzada, ocre: borde y pie de la Cuia de vidrio (07 §4.6). Tono provisional.</summary>
        public static readonly Color Fiber = Hex(0xA8834F);

        /// <summary><c>ui_papel</c> #F4EBDA: paneles y modales (07 §7.2).</summary>
        public static readonly Color UiPaper = Hex(0xF4EBDA);

        /// <summary><c>ui_tinta</c> #2B2118: texto principal (07 §7.2).</summary>
        public static readonly Color UiInk = Hex(0x2B2118);

        /// <summary><c>ui_madera</c> #6B4A2B: marcos, bandeja cumbuca, botón secundario (07 §7.2).</summary>
        public static readonly Color UiWood = Hex(0x6B4A2B);

        /// <summary><c>ui_oliva</c> #3E5C3A: fondo de HUD translúcido (07 §7.2).</summary>
        public static readonly Color UiOlive = Hex(0x3E5C3A);

        /// <summary><c>ui_verde_boton</c> #3F6B33: botón primario (07 §7.2).</summary>
        public static readonly Color UiGreenButton = Hex(0x3F6B33);

        /// <summary><c>ui_ambar</c> #C98A2B (07 §7.2).</summary>
        public static readonly Color UiAmber = Hex(0xC98A2B);

        /// <summary><c>ui_indigo</c> #24305E (07 §7.2).</summary>
        public static readonly Color UiIndigo = Hex(0x24305E);

        /// <summary><c>ui_rojo_aviso</c> #9E3B2E (07 §7.2).</summary>
        public static readonly Color UiRedWarning = Hex(0x9E3B2E);

        /// <summary>
        /// Borde inferior de la "piedra de río" de papel (07 §7.3: 2 dp más oscuro): papel × 0,72. Tono derivado, fuera de
        /// la tabla de tokens de 07 §7.2.
        /// </summary>
        public static readonly Color StoneEdge = new Color(0xF4 / 255f * 0.72f, 0xEB / 255f * 0.72f, 0xDA / 255f * 0.72f, 1f);

        /// <summary>Turquesa del icono de Marea, #3BB8D9 al 80 % (07 §7.6).</summary>
        public static readonly Color IconTurquoise = new Color(0x3B / 255f, 0xB8 / 255f, 0xD9 / 255f, 0.8f);

        /// <summary>Plumas del confeti de guacamayo, rojo y azul (02 §4.1; 07 §5.4). Tonos provisionales, fuera de la paleta de tintes.</summary>
        public static readonly Color FeatherRed = Hex(0xC23A2B);

        /// <summary>Pluma azul de guacamayo (provisional).</summary>
        public static readonly Color FeatherBlue = Hex(0x2F6FD6);

        /// <summary>
        /// Opacidad del patrón por tinte que se dibuja "siempre" (02 §12; 07 §3, regla 1: 15 % hasta el nivel 40, sin
        /// Patrones de fruta; el 20 % de <c>ui_pattern_opacity</c> y el icono llegan con ese ajuste, fuera de v0).
        /// </summary>
        public const float PatternOpacity = 0.15f;

        /// <summary>
        /// Color de las marcas del patrón: tinta <c>ui_tinta</c> sobre los tintes claros y marfil <c>ui_papel</c> sobre
        /// los de L* &lt; 52 de la tabla de 07 §3 (5, 6, 8, 9, 13 y 14; 07 §3 fija "patrón en marfil" para Huito).
        /// </summary>
        public static Color PatternBase(int tint)
        {
            switch (tint)
            {
                case 5:
                case 6:
                case 8:
                case 9:
                case 13:
                case 14:
                    return UiPaper;
                default:
                    return UiInk;
            }
        }

        /// <summary>
        /// Color opaco de las marcas del patrón sobre una dosis de color <paramref name="dose"/>: la mezcla al
        /// <see cref="PatternOpacity"/> se hace aquí, en sRGB, y la marca se pinta opaca. Así el 15 % es el que ve el
        /// jugador: con el espacio de color Linear del proyecto, un alfa del 15 % en la UI se mezclaría en lineal y no
        /// daría el mismo contraste (la misma causa que aclaraba los textos con alfa).
        /// </summary>
        public static Color PatternMark(Color dose, int tint)
        {
            Color mark = Color.Lerp(dose, PatternBase(tint), PatternOpacity);
            mark.a = dose.a;
            return mark;
        }

        /// <summary>Color del tinte con índice de motor <paramref name="tint"/> (1-14).</summary>
        public static Color Tint(int tint)
        {
            if (tint < 1 || tint >= Tints.Length)
            {
                return Color.magenta;
            }

            return Tints[tint];
        }

        /// <summary>
        /// Aclarado del menisco (07 §3, regla 4): +20 % de luminosidad; +25-30 % en los tintes de L* &lt; 35
        /// (6 y 8 al 25 %, 9 y 13 al 30 %, según la tabla de 07 §3).
        /// </summary>
        public static float MeniscusLighten(int tint)
        {
            switch (tint)
            {
                case 6:
                case 8:
                    return 0.25f;
                case 9:
                case 13:
                    return 0.30f;
                default:
                    return 0.20f;
            }
        }

        /// <summary>Agua base del preset <c>water_base</c> (<c>black</c>, <c>white</c>, <c>clear</c>; glosario §11).</summary>
        public static Color WaterBase(string waterBase)
        {
            switch (waterBase)
            {
                case "black":
                    return WaterBlack;
                case "clear":
                    return WaterClear;
                default:
                    return WaterWhite;
            }
        }

        /// <summary>Color opaco desde un entero 0xRRGGBB.</summary>
        public static Color32 Hex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 0xFF);
        }
    }
}
