#nullable enable
using AF.Core.Mechanics;
using AF.Core.Session;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Contador de la Lluvia o de la Piraña en el HUD (02 §9.1 M6-M7; traspaso §6 paso 1.3: «Lluvia (con su contador en el
    /// HUD)»; tarea T09): una píldora de madera oscura como las de Semillas y Canoas de la cabecera (07 §7.4) con el icono de
    /// la mecánica (la nube de la Lluvia, sin gotas, o la piraña de <see cref="ProceduralArt.MechanicIcon"/>), los vertidos
    /// útiles que faltan (<see cref="LevelCounter.Remaining"/>, que solo baja con vertidos útiles: Q18 B) y, en la Lluvia,
    /// separadas de la cifra por un hueco con un filete, una gotita del agua del mundo por cada gota pendiente
    /// (<see cref="LevelCounter.PendingDrops"/>). Atenuado si <see cref="LevelCounter.Blocked"/> (la gota espera o el mordisco
    /// se perderá). Oculto sin contador.
    /// </summary>
    /// <remarks>
    /// <para>Son dos cantidades distintas (los vertidos que faltan y las gotas que quedan por caer): para que «2 ·· » no se
    /// lea como «2 gotas», las gotitas van tras <see cref="DropsGapDp"/> con un filete de 1 dp <c>muelle_sombra</c> en medio,
    /// y la nube del chip es la nube sola (<see cref="CloudSprite"/>): el icono de la Lluvia lleva tres gotas grises dibujadas
    /// y en el chip las únicas gotas son las pendientes [P].</para>
    /// <para>La vista no decide reglas: pinta el <see cref="LevelCounter"/> del núcleo tal cual. Sin asignaciones al refrescar
    /// (la cifra va con <see cref="TMP_Text.SetCharArray(char[], int, int)"/>); dos alturas, la normal (<see cref="HeightDp"/>)
    /// y la compacta del HUD (<see cref="CompactHeightDp"/>).</para>
    /// </remarks>
    internal sealed class HudCounterChip
    {
        /// <summary>Alto de la píldora normal (dp).</summary>
        public const float HeightDp = 28f;

        /// <summary>Alto de la píldora compacta (dp; la del chip de dificultad compacto).</summary>
        public const float CompactHeightDp = DifficultyChip.CompactHeightDp;

        /// <summary>Gotitas como mucho (la Lluvia del nivel; <c>n</c> ≤ 4 por <c>U_agua</c> = 4, 02 §9.1 M6).</summary>
        public const int MaxDrops = 4;

        /// <summary>Opacidad del chip con el contador bloqueado [P].</summary>
        public const float BlockedAlpha = 0.5f;

        /// <summary>Hueco entre la cifra y las gotitas (dp), con el filete de 1 dp en medio [P].</summary>
        public const float DropsGapDp = 6f;

        /// <summary>Ancho del filete entre la cifra y las gotitas (dp).</summary>
        public const float DividerDp = 1f;

        /// <summary>Alto de la nube respecto al de la píldora [P].</summary>
        public const float CloudHeightFraction = 0.58f;

        // La placa de madera mide 48 × 48 dp con 18 dp de 9-slice (como WalletBar): el borde se reduce a la mitad del alto.
        private const float PlateSliceDp = 18f;

        // Sprite de la nube a 3 px/dp: 76 × 48 px (≈ 25 × 16 dp).
        private const int CloudPxW = 76;
        private const int CloudPxH = 48;

        private static Sprite? s_cloud;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _value;
        private readonly Image _divider;
        private readonly Image[] _drops = new Image[MaxDrops];
        private readonly char[] _buf = new char[8];
        private MechanicType _type = MechanicType.None;
        private int _remaining = -1;
        private int _dropCount = -1;
        private int _dropTint = -1;
        private bool _blocked;
        private bool _compact;
        private float _width;

        private HudCounterChip(Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("CounterChip", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _plate = UiBuilder.Sliced("Plate", _root, art.WoodPlate, Color.white);
            _icon = UiBuilder.Image("Icon", _root, null, Color.white);
            _icon.preserveAspect = true;
            _value = UiBuilder.Label("Value", _root, TextStyle.PillWood, string.Empty, 17f, Palette.Cream, TextAlignmentOptions.Center);
            _divider = UiBuilder.Image("Divider", _root, null, Palette.DockShadow);
            _divider.gameObject.SetActive(false);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = UiBuilder.Image("Drop" + i, _root, art.DropIcon, Color.white);
                _drops[i].preserveAspect = true;
                _drops[i].gameObject.SetActive(false);
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>Contenedor del chip.</summary>
        public RectTransform Root => _root;

        /// <summary>Se ve (hay contador).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Mecánica del contador mostrado (<see cref="MechanicType.None"/> si no se ve).</summary>
        public MechanicType Type => Visible ? _type : MechanicType.None;

        /// <summary>Cifra mostrada (−1 si no se ve).</summary>
        public int Remaining => Visible ? _remaining : -1;

        /// <summary>Texto de la cifra.</summary>
        public string Text => _value.text;

        /// <summary>Gotitas a la vista.</summary>
        public int DropsShown => Visible ? _dropCount : 0;

        /// <summary>Atenuado por <see cref="LevelCounter.Blocked"/>.</summary>
        public bool Blocked => Visible && _blocked;

        /// <summary>Opacidad aplicada (1 o <see cref="BlockedAlpha"/>).</summary>
        public float Alpha => _group.alpha;

        /// <summary>Icono de la mecánica (la nube o la piraña).</summary>
        public Sprite? Icon => _icon.sprite;

        /// <summary>Rectángulo del icono (tests).</summary>
        public RectTransform IconRect => _icon.rectTransform;

        /// <summary>Rectángulo de la cifra (tests).</summary>
        public RectTransform ValueRect => _value.rectTransform;

        /// <summary>Filete entre la cifra y las gotitas (tests).</summary>
        public Image Divider => _divider;

        /// <summary>Gotita <paramref name="i"/> (tests).</summary>
        public Image Drop(int i)
        {
            return _drops[i];
        }

        /// <summary>Color de las gotitas (el agua del mundo).</summary>
        public Color DropColor => _drops[0].color;

        /// <summary>Variante compacta.</summary>
        public bool Compact => _compact;

        /// <summary>Alto de la píldora (dp).</summary>
        public float Height => _compact ? CompactHeightDp : HeightDp;

        /// <summary>Ancho con el contenido actual (dp).</summary>
        public float Width => _width;

        /// <summary>Crea el chip, oculto, bajo <paramref name="parent"/>.</summary>
        public static HudCounterChip Create(Transform parent, ProceduralArt art)
        {
            return new HudCounterChip(parent, art);
        }

        /// <summary>
        /// Pinta <paramref name="counter"/> (oculto si no está activo) con las gotitas del tinte <paramref name="dropTint"/> (el
        /// agua del mundo de la Lluvia: <c>Cuia.MechanicKey</c>). Devuelve true si cambió el ancho o la visibilidad (hay que
        /// recolocar). Sin asignaciones.
        /// </summary>
        public bool Show(in LevelCounter counter, int dropTint)
        {
            bool wasVisible = Visible;
            float oldWidth = _width;
            if (!counter.IsActive)
            {
                if (wasVisible)
                {
                    _root.gameObject.SetActive(false);
                }

                return wasVisible;
            }

            if (!wasVisible)
            {
                _root.gameObject.SetActive(true);
            }

            bool typeChanged = counter.Type != _type;
            if (typeChanged)
            {
                _type = counter.Type;
                _icon.sprite = _type == MechanicType.Lluvia ? CloudSprite() : _art.MechanicIcon(_type);
            }

            int remaining = counter.Remaining;
            bool digitsChanged = (remaining >= 10) != (_remaining >= 10);
            if (remaining != _remaining)
            {
                _remaining = remaining;
                int n = UiText.WriteNumber(_buf, 0, remaining, false);
                _value.SetCharArray(_buf, 0, n);
            }

            int drops = counter.Type == MechanicType.Lluvia ? Mathf.Clamp(counter.PendingDrops, 0, MaxDrops) : 0;
            bool dropsChanged = drops != _dropCount;
            if (dropsChanged)
            {
                _dropCount = drops;
                for (int i = 0; i < _drops.Length; i++)
                {
                    _drops[i].gameObject.SetActive(i < drops);
                }
            }

            if (drops > 0 && dropTint != _dropTint)
            {
                _dropTint = dropTint;
                Color water = Palette.Tint(dropTint); // el agua del mundo, como las gotitas de la nube
                for (int i = 0; i < _drops.Length; i++)
                {
                    _drops[i].color = water;
                }
            }

            _blocked = counter.Blocked;
            float alpha = _blocked ? BlockedAlpha : 1f;
            if (!Mathf.Approximately(_group.alpha, alpha))
            {
                _group.alpha = alpha;
            }

            if (dropsChanged || digitsChanged || typeChanged || !wasVisible)
            {
                Layout();
            }

            return !wasVisible || !Mathf.Approximately(oldWidth, _width);
        }

        /// <summary>Pasa a la variante compacta (o vuelve) y recalcula el ancho; quien lo coloca llama después a <see cref="Place"/>.</summary>
        public void SetCompact(bool compact)
        {
            if (compact == _compact)
            {
                return;
            }

            _compact = compact;
            Layout();
        }

        /// <summary>Coloca el chip con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) del padre.</summary>
        public void Place(float x, float yTop)
        {
            UiBuilder.Place(_root, x, yTop, _width, Height);
        }

        // Placa, icono, cifra y gotitas de izquierda a derecha (dp). La cifra lleva una caja fija por dígito (sin medir el
        // texto) para no asignar. Entre la cifra y las gotitas, DropsGapDp con el filete en medio.
        private void Layout()
        {
            float h = Height;
            float pad = _compact ? 3f : 4f;
            bool cloud = _type == MechanicType.Lluvia;
            float iconH = cloud ? Mathf.Round(h * CloudHeightFraction) : h - (_compact ? 3f : 4f);
            float iconW = cloud ? Mathf.Round(iconH * CloudPxW / CloudPxH) : iconH;
            float textSize = _compact ? 13f : 17f;
            int digits = _remaining >= 10 ? 2 : 1;
            float digitW = Mathf.Ceil(textSize * 0.62f) * digits;
            float dropW = _compact ? 6f : 8f;
            float dropGap = _compact ? 1.5f : 2f;
            float x = pad;
            UiBuilder.Place(_icon.rectTransform, x, (h - iconH) * 0.5f, iconW, iconH);
            x += iconW + (cloud ? 2f : 1f);
            _value.fontSize = textSize;
            UiBuilder.Place(_value.rectTransform, x, 0f, digitW + 2f, h);
            x += digitW + 2f;
            int drops = Mathf.Max(0, _dropCount);
            _divider.gameObject.SetActive(drops > 0);
            if (drops > 0)
            {
                float side = (DropsGapDp - DividerDp) * 0.5f;
                float lineH = Mathf.Round(h * 0.6f);
                UiBuilder.Place(_divider.rectTransform, x + side, (h - lineH) * 0.5f, DividerDp, lineH);
                x += DropsGapDp;
                float dropH = dropW * 1.25f;
                for (int i = 0; i < drops && i < _drops.Length; i++)
                {
                    UiBuilder.Place(_drops[i].rectTransform, x, (h - dropH) * 0.5f, dropW, dropH);
                    x += dropW + dropGap;
                }

                x -= dropGap;
            }

            x += pad + 2f;
            _width = Mathf.Ceil(x);
            _plate.pixelsPerUnitMultiplier = PlateSliceDp / (h * 0.5f);
            UiBuilder.Place(_plate.rectTransform, 0f, 0f, _width, h);
        }

        /// <summary>
        /// Nube de la Lluvia sin gotas para el chip (<c>hud_counter_cloud</c>): la del icono de la mecánica
        /// (<c>ProceduralArt.DrawCloud</c>: base redondeada y tres copos en gris perla <see cref="ProceduralArt.Pearl"/>, con la
        /// panza, el 30 % de abajo, al 84 %), dibujada aquí porque el icono trae tres gotas grises debajo y el sprite de la nube
        /// del tablero no es público. Una sola textura legible para toda la app (se rehace si Unity la destruyó).
        /// </summary>
        internal static Sprite CloudSprite()
        {
            if (s_cloud != null)
            {
                return s_cloud;
            }

            const int w = CloudPxW;
            const int h = CloudPxH;
            // La geometría de DrawCloud en px (y hacia arriba) con la caja de 88 × 50 desplazada para llenar el sprite.
            const float x0 = -6f;
            const float y0 = -1.5f;
            const float cw = 88f;
            const float ch = 50f;
            var pixels = new Color32[w * h];
            Color pearl = ProceduralArt.Pearl;
            var belly = new Color(pearl.r * 0.84f, pearl.g * 0.84f, pearl.b * 0.84f, 1f);
            float bellyTop = y0 + (ch * 0.30f);
            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float d = RoundedRectDistance(px, py, x0 + (cw * 0.08f), y0 + (ch * 0.05f), x0 + (cw * 0.92f), y0 + (ch * 0.50f), ch * 0.22f);
                    d = Mathf.Min(d, CircleDistance(px, py, x0 + (cw * 0.30f), y0 + (ch * 0.48f), ch * 0.30f));
                    d = Mathf.Min(d, CircleDistance(px, py, x0 + (cw * 0.53f), y0 + (ch * 0.58f), ch * 0.38f));
                    d = Mathf.Min(d, CircleDistance(px, py, x0 + (cw * 0.75f), y0 + (ch * 0.45f), ch * 0.27f));
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = py < bellyTop ? belly : pearl;
                    c.a = cov;
                    pixels[(y * w) + x] = c;
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "hud_counter_cloud",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, false); // legible: los tests miran que no tenga gotas
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f * ProceduralArt.UiPxPerDp, 0, SpriteMeshType.FullRect);
            sprite.name = "hud_counter_cloud";
            sprite.hideFlags = HideFlags.DontSave;
            s_cloud = sprite;
            return sprite;
        }

        // Distancia con signo (px) a un rectángulo [x0, x1] × [y0, y1] de esquinas de radio r.
        private static float RoundedRectDistance(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            float qx = Mathf.Abs(px - ((x0 + x1) * 0.5f)) - (((x1 - x0) * 0.5f) - r);
            float qy = Mathf.Abs(py - ((y0 + y1) * 0.5f)) - (((y1 - y0) * 0.5f) - r);
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float CircleDistance(float px, float py, float cx, float cy, float r)
        {
            float dx = px - cx;
            float dy = py - cy;
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
        }
    }
}
