#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El «Zzz» de TiTino dormido en el inicio (añadido de D-050 del 2026-10-26: «De noche TiTino duerme callado (solo un Zzz)»):
    /// tres zetas dibujadas por código con barras (sin texto que traducir), en marfil con una sombra suave de 1 dp, que salen junto
    /// a su cabeza y suben en diagonal hacia la derecha, creciendo y desvaneciéndose en un bucle de <see cref="CycleMs"/> ms, una
    /// tras otra. Con Reducir movimiento, quietas: las tres a la vez, de menor a mayor. Sin raycast (el toque es el de TiTino) y
    /// sin asignaciones por fotograma: solo mueve, escala y funde lo creado una vez.
    /// </summary>
    internal sealed class HomeSlothZzz
    {
        /// <summary>Zetas del «Zzz».</summary>
        public const int Count = 3;

        /// <summary>Lo que tarda una zeta en subir desde la cabeza hasta desvanecerse (ms) [P].</summary>
        public const float CycleMs = 3600f;

        /// <summary>Fundido de entrada del «Zzz» al dormirse (ms; sin él con Reducir movimiento) [P].</summary>
        public const float FadeInMs = 300f;

        /// <summary>Alto de una zeta a tamaño completo (dp) [P].</summary>
        public const float ZDp = 11f;

        /// <summary>Grosor del trazo de una zeta a tamaño completo (dp) [P].</summary>
        public const float StrokeDp = 2f;

        /// <summary>Escala de una zeta al salir de la cabeza (crece hasta 1 al subir) [P].</summary>
        public const float StartScale = 0.55f;

        /// <summary>Lo que se desplaza una zeta hacia la derecha en su subida (dp) [P].</summary>
        public const float RiseXDp = 30f;

        /// <summary>Lo que sube una zeta (dp) [P].</summary>
        public const float RiseYDp = 18f;

        // Marfil del vidrio del bocadillo (D-003) y una sombra suave para leerse sobre el cielo claro.
        private static readonly Color FaceColor = new Color(0xF4 / 255f, 0xFB / 255f, 0xF8 / 255f, 1f);
        private static readonly Color ShadowColor = new Color(0.04f, 0.14f, 0.16f, 0.45f);

        private const float ShadowDp = 1f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _rootGroup;
        private readonly RectTransform[] _z = new RectTransform[Count];
        private readonly CanvasGroup[] _groups = new CanvasGroup[Count];
        private float _ms;
        private float _shownMs;
        private float _x;
        private float _y;

        /// <summary>Crea el «Zzz» (oculto) bajo <paramref name="parent"/>.</summary>
        public HomeSlothZzz(Transform parent)
        {
            _root = UiBuilder.Rect("HomeSlothZzz", parent);
            UiBuilder.Place(_root, 0f, 0f, 0f, 0f);
            _rootGroup = _root.gameObject.AddComponent<CanvasGroup>();
            _rootGroup.blocksRaycasts = false;
            _rootGroup.interactable = false;
            for (int i = 0; i < Count; i++)
            {
                RectTransform z = UiBuilder.Rect("Z" + i, _root);
                z.anchorMin = new Vector2(0.5f, 0.5f);
                z.anchorMax = new Vector2(0.5f, 0.5f);
                z.pivot = new Vector2(0.5f, 0.5f);
                z.sizeDelta = new Vector2(ZDp, ZDp);
                CanvasGroup group = z.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                BuildZ(z, ShadowDp, -ShadowDp, ShadowColor);
                BuildZ(z, 0f, 0f, FaceColor);
                _z[i] = z;
                _groups[i] = group;
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>El «Zzz» está a la vista.</summary>
        public bool Shown => _root.gameObject.activeSelf;

        /// <summary>Raíz del «Zzz» (tests).</summary>
        public RectTransform Root => _root;

        /// <summary>
        /// Caja que ocupa el «Zzz» en todo su bucle, en dp del lienzo con la y hacia abajo (desde la esquina de arriba a la
        /// izquierda): de la zeta que sale, pequeña, junto a la cabeza, a la que se va, entera, arriba a la derecha.
        /// </summary>
        public Rect BoundsDp
        {
            get
            {
                float half = ZDp * 0.5f;
                float start = half * StartScale;
                return Rect.MinMaxRect(_x - start, _y - RiseYDp - half, _x + RiseXDp + half, _y + start);
            }
        }

        /// <summary>
        /// Coloca el arranque del «Zzz» (donde sale la primera zeta) en (<paramref name="x"/>, <paramref name="y"/>) dp del lienzo,
        /// con la y hacia abajo.
        /// </summary>
        public void Place(float x, float y)
        {
            _x = x;
            _y = y;
            var pos = new Vector2(x, -y);
            if (_root.anchoredPosition != pos)
            {
                _root.anchoredPosition = pos;
            }
        }

        /// <summary>Lo enseña (<paramref name="on"/>) o lo oculta; al enseñarlo, el bucle empieza de nuevo.</summary>
        public void SetShown(bool on, bool reduceMotion)
        {
            if (on == Shown)
            {
                return;
            }

            _root.gameObject.SetActive(on);
            if (on)
            {
                _ms = 0f;
                _shownMs = 0f;
                Apply(reduceMotion);
            }
        }

        /// <summary>Avanza <paramref name="dtMs"/> ms (quieto con <paramref name="reduceMotion"/>). Sin asignaciones.</summary>
        public void Tick(float dtMs, bool reduceMotion)
        {
            if (!Shown)
            {
                return;
            }

            _ms = Mathf.Repeat(_ms + dtMs, CycleMs);
            _shownMs = Mathf.Min(_shownMs + dtMs, FadeInMs);
            Apply(reduceMotion);
        }

        /// <summary>
        /// Avance de la zeta <paramref name="i"/> en su subida (0 junto a la cabeza, 1 al irse) a los <paramref name="ms"/> ms del
        /// bucle; con Reducir movimiento, fijo (las tres repartidas, de menor a mayor).
        /// </summary>
        public static float Progress(int i, float ms, bool reduceMotion)
        {
            if (reduceMotion)
            {
                return (i + 0.5f) / Count;
            }

            return Mathf.Repeat((ms / CycleMs) - ((float)i / Count), 1f);
        }

        /// <summary>
        /// Opacidad de una zeta con avance <paramref name="u"/>: entra en el primer 20 % de su subida y se va en el último 35 %;
        /// con Reducir movimiento, entera.
        /// </summary>
        public static float AlphaAt(float u, bool reduceMotion)
        {
            if (reduceMotion)
            {
                return 1f;
            }

            return Mathf.Clamp01(Mathf.Min(u / 0.2f, (1f - u) / 0.35f));
        }

        private void Apply(bool reduceMotion)
        {
            float rootAlpha = reduceMotion ? 1f : Mathf.Clamp01(_shownMs / FadeInMs);
            if (!Mathf.Approximately(_rootGroup.alpha, rootAlpha))
            {
                _rootGroup.alpha = rootAlpha;
            }

            for (int i = 0; i < Count; i++)
            {
                float u = Progress(i, _ms, reduceMotion);
                float scale = Mathf.Lerp(StartScale, 1f, u);
                _z[i].anchoredPosition = new Vector2(u * RiseXDp, u * RiseYDp);
                _z[i].localScale = new Vector3(scale, scale, 1f);
                float alpha = AlphaAt(u, reduceMotion);
                if (!Mathf.Approximately(_groups[i].alpha, alpha))
                {
                    _groups[i].alpha = alpha;
                }
            }
        }

        // Una zeta de ZDp de alto (0,8 de ancho) con tres barras: la de arriba, la diagonal y la de abajo, desplazada (dx, dy) dp.
        private static void BuildZ(RectTransform z, float dx, float dy, Color color)
        {
            const float h = ZDp;
            const float w = ZDp * 0.8f;
            const float k = StrokeDp;
            Bar(z, color, dx, dy + ((h - k) * 0.5f), w, k, 0f);
            Bar(z, color, dx, dy - ((h - k) * 0.5f), w, k, 0f);
            float rise = h - (2f * k);
            float length = Mathf.Sqrt((w * w) + (rise * rise)) + (k * 0.5f);
            Bar(z, color, dx, dy, length, k, Mathf.Atan2(rise, w) * Mathf.Rad2Deg);
        }

        private static void Bar(RectTransform parent, Color color, float cx, float cy, float w, float h, float angle)
        {
            Image bar = UiBuilder.Image("Bar", parent, null, color);
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, cy);
            rt.sizeDelta = new Vector2(w, h);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
