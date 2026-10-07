#nullable enable
using TMPro;
using UnityEngine;

namespace AF.UI
{
    /// <summary>Estilos de texto de la guía visual v2 (5.6): fuente y material de contorno y sombra.</summary>
    internal enum TextStyle
    {
        /// <summary>Cinta de victoria: Baloo 2 ExtraBold, contorno de 3 dp <c>hoja_contorno</c>, sombra.</summary>
        Ribbon,

        /// <summary>Insignia de nivel: Baloo 2 ExtraBold, contorno de 2,5 dp <c>madera_contorno</c>, sombra.</summary>
        Badge,

        /// <summary>Título de modal sobre placa: Baloo 2 ExtraBold, contorno de 2 dp, sombra.</summary>
        ModalTitle,

        /// <summary>Etiqueta de CTA de la familia Hoja: Baloo 2 Bold, contorno de 2 dp <c>hoja_contorno</c>.</summary>
        CtaLeaf,

        /// <summary>Etiqueta de CTA de la familia Agua: Baloo 2 Bold, contorno de 2 dp <c>agua_contorno</c>.</summary>
        CtaWater,

        /// <summary>Etiqueta de ranura de la bandeja: Nunito ExtraBold, contorno de 1,5 dp <c>agua_contorno</c>.</summary>
        SlotWater,

        /// <summary>Píldora de Reiniciar: Nunito ExtraBold, contorno de 1,5 dp <c>madera_contorno</c>.</summary>
        PillWood,

        /// <summary>Etiqueta deshabilitada: Nunito ExtraBold sin contorno (<c>deshabilitado_texto</c>).</summary>
        Disabled,

        /// <summary>Subtítulo y enlace: Nunito Bold sin contorno.</summary>
        PlainBold,

        /// <summary>Texto corrido: Nunito Regular.</summary>
        Body,
    }

    /// <summary>
    /// Fuentes TextMeshPro de la guía visual v2 (5.6): font assets SDF con atlas estático y un material por estilo, creados
    /// por <c>AF.EditorTools.FontSetup</c> en <c>Assets/_Project/Resources/AF/Fonts/</c> (así entran en el build con sus
    /// variantes de shader). Si faltaran, se usa la fuente por defecto de TMP y se avisa una vez.
    /// </summary>
    internal static class UiFonts
    {
        private static readonly TMP_FontAsset?[] Fonts = new TMP_FontAsset?[10];
        private static readonly Material?[] Materials = new Material?[10];
        private static bool _warned;

        /// <summary>Font asset del estilo.</summary>
        public static TMP_FontAsset? Font(TextStyle style)
        {
            Load(style);
            return Fonts[(int)style];
        }

        /// <summary>Material (contorno y sombra) del estilo.</summary>
        public static Material? Material(TextStyle style)
        {
            Load(style);
            return Materials[(int)style];
        }

        private static void Load(TextStyle style)
        {
            int i = (int)style;
            if (Fonts[i] != null)
            {
                return;
            }

            string font;
            string? mat;
            switch (style)
            {
                case TextStyle.Ribbon:
                    font = "Baloo2-ExtraBold";
                    mat = "Ribbon";
                    break;
                case TextStyle.Badge:
                    font = "Baloo2-ExtraBold";
                    mat = "Badge";
                    break;
                case TextStyle.ModalTitle:
                    font = "Baloo2-ExtraBold";
                    mat = "ModalTitle";
                    break;
                case TextStyle.CtaLeaf:
                    font = "Baloo2-Bold";
                    mat = "CtaHoja";
                    break;
                case TextStyle.CtaWater:
                    font = "Baloo2-Bold";
                    mat = "CtaAgua";
                    break;
                case TextStyle.SlotWater:
                    font = "Nunito-ExtraBold";
                    mat = "SlotAgua";
                    break;
                case TextStyle.PillWood:
                    font = "Nunito-ExtraBold";
                    mat = "PillMadera";
                    break;
                case TextStyle.Disabled:
                    font = "Nunito-ExtraBold";
                    mat = null;
                    break;
                case TextStyle.PlainBold:
                    font = "Nunito-Bold";
                    mat = "Plain";
                    break;
                default:
                    font = "Nunito-Regular";
                    mat = "Plain";
                    break;
            }

            TMP_FontAsset? asset = Resources.Load<TMP_FontAsset>("AF/Fonts/" + font + " SDF");
            Material? material = mat != null ? Resources.Load<Material>("AF/Fonts/" + font + " SDF - " + mat) : null;
            if (asset == null)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning("[AF.UiFonts] faltan los font assets de AF/Fonts: ejecuta AF.EditorTools.FontSetup.ApplyAll. Se usa la fuente por defecto de TMP.");
                }

                asset = TMP_Settings.defaultFontAsset;
                material = null;
            }

            Fonts[i] = asset;
            Materials[i] = material != null ? material : (asset != null ? asset.material : null);
        }
    }
}
