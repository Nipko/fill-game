#nullable enable
using AF.Economy;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Chip de la etiqueta de dificultad de D-009 (propuesta-mecanicas-dificultad.md §c.1-c.2; 07 §7.7; 02 §10; glosario §6.1),
    /// reutilizable en el inicio (sobre «Jugar · Nivel {n}»), en Siguiente y en el HUD (T09): píldora de 26 dp ámbar
    /// <c>ui_ambar</c> con texto tinta y el sello de la rana dardo (Difícil) o índigo <c>ui_indigo</c> con texto marfil y el sello
    /// del jaguar (Muy difícil), y la cinta «×3»/«×4» clavada con un remache a la derecha (como la «x5» de Magic Sort, ficha
    /// §3.6). Sin calavera (glosario §6.1). En <c>normal</c> se oculta. Los sellos de fauna (rana dardo y jaguar) quedan
    /// pendientes de revisión del asesor cultural (07 §8.2).
    /// </summary>
    /// <remarks>
    /// Geometría (dp, desde la esquina superior izquierda de <see cref="Root"/>): el sello de <see cref="MedallionDp"/> a la
    /// izquierda monta <see cref="MedallionOverlapDp"/> sobre la píldora, que va centrada en vertical; la cinta monta
    /// <see cref="RibbonBodyOverlapDp"/> sobre el extremo derecho de la píldora, con el remache en su esquina superior izquierda,
    /// por encima de la altura de la «x» del texto. <see cref="Root"/> abarca todo (sello, píldora, sombra y cinta), así que
    /// sirve para comprobar solapes. Solo asigna memoria al cambiar la etiqueta o el multiplicador.
    /// <para>Variante compacta (<see cref="SetCompact"/>; tarea T09) para el HUD cuando no cabe la de 30 dp bajo la insignia
    /// (360 × 640 o tableros de 3 filas): <see cref="CompactHeightDp"/> de alto, sello de 20 dp, píldora de
    /// <see cref="CompactPillDp"/> con la misma pintura (9-slice reducido) y texto de <see cref="CompactTextDp"/>, el mínimo de
    /// 07 §7.7; la cinta, si la hay, a 3/4 [P].</para>
    /// </remarks>
    internal sealed class DifficultyChip
    {
        /// <summary>Alto total del chip (dp): el sello, que sobresale 2 dp por arriba y por abajo de la píldora.</summary>
        public const float HeightDp = MedallionDp;

        /// <summary>Lado del sello de la rana o del jaguar (dp).</summary>
        public const float MedallionDp = 30f;

        /// <summary>Lo que el sello monta sobre la píldora (dp).</summary>
        public const float MedallionOverlapDp = 14f;

        /// <summary>Tamaño del texto de la etiqueta (07 §7.7: 12-14 dp).</summary>
        public const float TextDp = 14f;

        /// <summary>Distancia de la parte superior del chip al borde inferior de la píldora (sin la sombra).</summary>
        public const float PillBottomDp = PillTopDp + ProceduralArt.LabelPillDp;

        /// <summary>Lo que el cuerpo de la cinta monta sobre el extremo derecho de la píldora (dp): ahí va el remache.</summary>
        public const float RibbonBodyOverlapDp = 5f;

        /// <summary>Hueco entre el final de la palabra y el cuerpo de la cinta (dp).</summary>
        public const float RibbonGapDp = 10f;

        /// <summary>Alto total de la variante compacta (dp; T09: «chip de 20 dp pegado bajo la placa» [P]).</summary>
        public const float CompactHeightDp = 20f;

        /// <summary>Alto de la píldora de la variante compacta, sin la sombra (dp).</summary>
        public const float CompactPillDp = 18f;

        /// <summary>Texto de la variante compacta: el mínimo de 07 §7.7 (12-14 dp).</summary>
        public const float CompactTextDp = 12f;

        private const float PillTopDp = (HeightDp - ProceduralArt.LabelPillDp) * 0.5f;
        private const float RibbonTextDp = 16f;
        private const float RibbonTopDp = 4f;
        private const float TextGapDp = 5f;
        private const float EndPadDp = 12f;

        // Variante compacta: el sello monta 9 dp (14 × 20 / 30), el texto empieza 3,5 dp después y la píldora acaba 8 dp
        // después de la palabra. La píldora pintada (28 dp con la sombra) se reduce a 18 + 1,4 con el multiplicador del
        // 9-slice, así su extremo sigue siendo un semicírculo; la cinta va a 3/4.
        private const float CompactMedallionDp = CompactHeightDp;
        private const float CompactMedallionOverlapDp = 9f;
        private const float CompactTextGapDp = 3.5f;
        private const float CompactEndPadDp = 8f;
        private const float CompactShadowDp = ProceduralArt.LabelPillShadowDp * CompactPillDp / ProceduralArt.LabelPillDp;
        private const float CompactPillTopDp = (CompactHeightDp - CompactPillDp) * 0.5f;
        private const float CompactRibbonScale = 0.75f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Image _pill;
        private readonly Image _medallion;
        private readonly TextMeshProUGUI _text;
        private readonly Image _ribbon;
        private readonly TextMeshProUGUI _ribbonText;
        private string _label = LevelDefinition.LabelNormal;
        private int _multiplier = -1;
        private float _width;
        private bool _compact;

        private DifficultyChip(Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("DifficultyChip", parent);
            _pill = UiBuilder.Image("Pill", _root, null, Color.white); // sprites perezosos: se ponen en Set
            _pill.type = Image.Type.Sliced;
            _text = UiBuilder.Label("Label", _pill.transform, TextStyle.PlainBold, string.Empty, TextDp, Palette.Ink, TextAlignmentOptions.MidlineLeft);
            _ribbon = UiBuilder.Image("Ribbon", _root, null, Color.white);
            _ribbonText = UiBuilder.Label("Value", _ribbon.transform, TextStyle.CtaWater, string.Empty, RibbonTextDp, Palette.Cream, TextAlignmentOptions.Center);
            _medallion = UiBuilder.Image("Medallion", _root, null, Color.white);
            _medallion.preserveAspect = true;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Contenedor del chip (abarca el sello, la píldora y la cinta).</summary>
        public RectTransform Root => _root;

        /// <summary>El chip se ve (Difícil o Muy difícil).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary><c>difficulty.label</c> aplicado (<c>normal</c>, <c>hard</c> o <c>very_hard</c>).</summary>
        public string Label => _label;

        /// <summary>Muy difícil (índigo con el jaguar).</summary>
        public bool VeryHard => _label == LevelDefinition.LabelVeryHard;

        /// <summary>Texto de la píldora («Difícil» o «Muy difícil»), o cadena vacía si no se ve.</summary>
        public string Text => _text.text;

        /// <summary>Multiplicador del premio mostrado en la cinta; ≤ 1 si la cinta no se ve.</summary>
        public int Multiplier => _multiplier;

        /// <summary>
        /// La cinta «×n» se ve: el chip se ve y la cinta está activa. Al ocultar el chip (<c>normal</c>), la cinta conserva su
        /// estado activo para el siguiente <see cref="Set"/>, pero ya no se ve (del 60 al 61 en el mismo inicio).
        /// </summary>
        public bool RibbonShown => Visible && _ribbon.gameObject.activeSelf;

        /// <summary>Texto de la cinta («×3» o «×4»), o cadena vacía si no se ve.</summary>
        public string RibbonText => RibbonShown ? _ribbonText.text : string.Empty;

        /// <summary>Sello mostrado (rana dardo o jaguar).</summary>
        public Sprite? Icon => _medallion.sprite;

        /// <summary>
        /// Color del token de la etiqueta aplicada (<c>ui_ambar</c> o <c>ui_indigo</c>). Se deduce de <see cref="Label"/>, no de lo
        /// que se pinta: para comprobar la píldora real, <see cref="PillSprite"/>.
        /// </summary>
        public Color PillColor => VeryHard ? (Color)Palette.UiIndigo : (Color)Palette.UiAmber;

        /// <summary>
        /// Sprite que pinta la píldora (<c>label_pill_hard</c> o <c>label_pill_very_hard</c>, de
        /// <see cref="ProceduralArt.DifficultyPill"/>), o null antes del primer <see cref="Set"/> con etiqueta.
        /// </summary>
        public Sprite? PillSprite => _pill.sprite;

        /// <summary>Color del texto de la píldora (tinta o marfil).</summary>
        public Color TextColor => _text.color;

        /// <summary>Píldora (tests de solape).</summary>
        public RectTransform PillRect => _pill.rectTransform;

        /// <summary>Sello (tests de solape).</summary>
        public RectTransform MedallionRect => _medallion.rectTransform;

        /// <summary>Cinta (tests de solape).</summary>
        public RectTransform RibbonRect => _ribbon.rectTransform;

        /// <summary>Ancho del chip con su contenido actual (dp).</summary>
        public float Width => _width;

        /// <summary>Alto del chip (dp): <see cref="HeightDp"/>, o <see cref="CompactHeightDp"/> en la variante compacta.</summary>
        public float Height => _compact ? CompactHeightDp : HeightDp;

        /// <summary>De la parte superior del chip al borde inferior de la píldora, sin la sombra (dp).</summary>
        public float PillBottom => _compact ? CompactPillTopDp + CompactPillDp : PillBottomDp;

        /// <summary>Variante compacta del HUD (<see cref="SetCompact"/>).</summary>
        public bool Compact => _compact;

        /// <summary>Tamaño del texto de la píldora (dp).</summary>
        public float TextSize => _text.fontSize;

        /// <summary>Crea el chip, oculto, bajo <paramref name="parent"/>.</summary>
        public static DifficultyChip Create(Transform parent, ProceduralArt art)
        {
            return new DifficultyChip(parent, art);
        }

        /// <summary>
        /// Multiplicador del premio de la etiqueta <paramref name="label"/> sobre el de un nivel normal (propuesta §c.2: «×3» y
        /// «×4» = <c>eco_reward_hard</c> / <c>eco_reward_normal</c> = 60 / 20 y <c>eco_reward_very_hard</c> / … = 80 / 20; 08 §9),
        /// solo si el cociente es exacto: con premios que no son múltiplos (p. ej. 25 y 60) la cinta anunciaría un multiplicador
        /// que no se paga (02 §10), así que devuelve 1 y la cinta no sale. 1 también en <c>normal</c>, con una etiqueta
        /// desconocida o sin premio normal. Nunca deduce la etiqueta del premio: Remote Config puede igualar dos tramos, y
        /// entonces la cinta no sale pero el chip sí.
        /// </summary>
        public static int MultiplierFor(EconomyConfig config, string? label)
        {
            if (!RewardTiers.TryParse(label, out RewardTier tier) || tier == RewardTier.Normal)
            {
                return 1;
            }

            int normal = config.WinReward(RewardTier.Normal);
            int reward = config.WinReward(tier);
            if (normal <= 0 || reward <= 0 || reward % normal != 0)
            {
                return 1;
            }

            return Mathf.Max(1, reward / normal);
        }

        /// <summary>
        /// Pares texto / fondo del chip para la comprobación de contraste ≥ 4,5:1 (02 §12; 07 §7.2 y §7.8): tinta sobre ámbar
        /// (5,4:1), marfil sobre índigo (10,7:1) y crema sobre el agua profunda de la cinta.
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("chip Difícil", TextOn(false), (Color)Palette.UiAmber),
                ("chip Muy difícil", TextOn(true), (Color)Palette.UiIndigo),
                ("cinta ×3/×4", (Color)Palette.Cream, ProceduralArt.RibbonTagColor),
                ("cinta ×3/×4 (parte alta)", (Color)Palette.Cream, ProceduralArt.RibbonTagLight),
            };
        }

        /// <summary>Color del texto sobre la píldora: tinta sobre ámbar y marfil (<c>ui_papel</c>) sobre índigo (07 §7.7).</summary>
        public static Color TextOn(bool veryHard)
        {
            return veryHard ? (Color)Palette.UiPaper : (Color)Palette.Ink;
        }

        /// <summary>
        /// Muestra la etiqueta <paramref name="label"/> (<c>hard</c> o <c>very_hard</c>) con la cinta «×<paramref name="multiplier"/>»;
        /// con un multiplicador ≤ 1 la cinta no sale (el HUD la omite). En <c>normal</c> (o una etiqueta desconocida) se oculta.
        /// Recalcula <see cref="Width"/>; quien lo coloca llama después a <see cref="Place"/> o a <see cref="PlaceCentered"/>.
        /// </summary>
        public void Set(string? label, int multiplier)
        {
            string? text = UiText.DifficultyChip(label ?? LevelDefinition.LabelNormal);
            if (text == null)
            {
                _label = LevelDefinition.LabelNormal;
                _multiplier = -1;
                _text.text = string.Empty;
                _root.gameObject.SetActive(false);
                return;
            }

            bool veryHard = label == LevelDefinition.LabelVeryHard;
            bool changed = !string.Equals(label, _label, System.StringComparison.Ordinal) || multiplier != _multiplier || !Visible;
            _label = label!;
            _multiplier = multiplier;
            _root.gameObject.SetActive(true);
            if (!changed)
            {
                return;
            }

            _pill.sprite = _art.DifficultyPill(veryHard);
            _medallion.sprite = _art.DifficultyIcon(veryHard);
            _text.text = text;
            _text.color = TextOn(veryHard);
            bool ribbon = multiplier > 1;
            _ribbon.gameObject.SetActive(ribbon);
            if (ribbon)
            {
                _ribbon.sprite = _art.RibbonTag;
                _ribbonText.text = UiText.Ribbon(multiplier);
            }

            Layout();
        }

        /// <summary>
        /// Pasa a la variante compacta del HUD (<see cref="CompactHeightDp"/>) o vuelve a la normal y recalcula
        /// <see cref="Width"/>; quien lo coloca llama después a <see cref="Place"/>. Sin cambios, no hace nada.
        /// </summary>
        public void SetCompact(bool compact)
        {
            if (compact == _compact)
            {
                return;
            }

            _compact = compact;
            _text.fontSize = compact ? CompactTextDp : TextDp;
            _ribbonText.fontSize = compact ? RibbonTextDp * CompactRibbonScale : RibbonTextDp;
            _pill.pixelsPerUnitMultiplier = compact ? (ProceduralArt.LabelPillDp + ProceduralArt.LabelPillShadowDp) / (CompactPillDp + CompactShadowDp) : 1f;
            if (Visible)
            {
                Layout();
            }
        }

        /// <summary>Coloca el chip con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) del padre.</summary>
        public void Place(float x, float yTop)
        {
            UiBuilder.Place(_root, x, yTop, _width, Height);
        }

        /// <summary>Coloca el chip centrado en <paramref name="centerX"/> con su parte superior en <paramref name="yTop"/>.</summary>
        public void PlaceCentered(float centerX, float yTop)
        {
            Place(Mathf.Round(centerX - (_width * 0.5f)), yTop);
        }

        private void Layout()
        {
            float textW = Mathf.Ceil(_text.preferredWidth);
            bool ribbon = _ribbon.gameObject.activeSelf;
            float medallion = _compact ? CompactMedallionDp : MedallionDp;
            float overlap = _compact ? CompactMedallionOverlapDp : MedallionOverlapDp;
            float pillTop = _compact ? CompactPillTopDp : PillTopDp;
            float pillH = _compact ? CompactPillDp : ProceduralArt.LabelPillDp;
            float shadow = _compact ? CompactShadowDp : ProceduralArt.LabelPillShadowDp;
            float rs = _compact ? CompactRibbonScale : 1f;
            float ribbonGap = RibbonGapDp * rs;
            float pillX = medallion - overlap;
            float textX = overlap + (_compact ? CompactTextGapDp : TextGapDp); // dentro de la píldora
            float pillW = textX + textW + (ribbon ? ribbonGap + (RibbonBodyOverlapDp * rs) : (_compact ? CompactEndPadDp : EndPadDp));
            UiBuilder.Place(_pill.rectTransform, pillX, pillTop, pillW, pillH + shadow);
            UiBuilder.Place(_text.rectTransform, textX, 0f, textW + 2f, pillH);
            UiBuilder.Place(_medallion.rectTransform, 0f, 0f, medallion, medallion);
            _width = pillX + pillW;
            if (ribbon)
            {
                // El cuerpo de la cinta empieza RibbonGapDp después de la palabra y cubre el extremo redondeado de la píldora.
                float ribbonX = pillX + textX + textW + ribbonGap - (ProceduralArt.RibbonTagBodyLeftDp * rs);
                UiBuilder.Place(_ribbon.rectTransform, ribbonX, RibbonTopDp * rs, ProceduralArt.RibbonTagWidthDp * rs, ProceduralArt.RibbonTagHeightDp * rs);
                Rect t = ProceduralArt.RibbonTagTextDp;
                UiBuilder.Place(_ribbonText.rectTransform, t.x * rs, t.y * rs, t.width * rs, t.height * rs);
                _width = ribbonX + (ProceduralArt.RibbonTagWidthDp * rs);
            }

            _width = Mathf.Ceil(_width);
        }
    }
}
