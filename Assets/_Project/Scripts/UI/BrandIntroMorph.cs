#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// «La intro nace del logo» (añadido de D-051 del 2026-10-28): las piezas que hacen que el primer fotograma de la intro sea el
    /// icono de la pantalla del sistema y que ese icono se abra en la escena con un <b>corte sobre la cuia</b>. Las mueve
    /// <see cref="BrandIntro"/>, sin asignaciones.
    /// <list type="bullet">
    /// <item><b>El icono</b>: la vista del icono adaptativo v2, una copia exacta de
    /// <c>docs/art/marca-oficial/v2/out/png/android-vista-432.png</c> (el fondo y el frente juntos, los mismos píxeles de las capas
    /// xxxhdpi que escala Android) en <c>Resources/AF/Brand/af_icon_vista.bytes</c>, decodificada sin compresión ni mipmaps. Un test
    /// EditMode la compara byte a byte con la de <c>docs/</c>.</item>
    /// <item><b>Dos grupos</b>: el fondo de la escena (el halo, la selva, el río, el açaí y la isla), siempre a pantalla completa
    /// (escala ≥ 1, así que nunca enseña sus bordes), y lo que el icono y la escena comparten, la cuia con su chorro y sus aros,
    /// que viaja del encuadre del icono al de la escena. La vista del icono va con la cuia, debajo de ella.</item>
    /// <item><b>El corte</b>: el resto del icono (río, selva, cielo) se funde hacia el petróleo de una capa lisa que tapa el fondo;
    /// después esa capa se funde y el fondo entra desde el petróleo alrededor de la cuia. Los dos ríos nunca se ven a la vez.</item>
    /// <item><b>El círculo</b>: el petróleo con un agujero redondo de borde suave (una textura de 256 px hecha por código) y cuatro
    /// bandas lisas alrededor. En el fotograma 0 el agujero mide <see cref="IconMaskDp"/> dp y la vista del icono
    /// <see cref="IconCanvasDp"/> dp, centrados en la pantalla: la caja de la pantalla del sistema de Android 12+ y la de
    /// <c>af_launch_window</c> (Android 8-11 y el velo nativo del arranque).</item>
    /// <item><b>El aro de luz</b>: un borde fino y suave en el círculo que se abre; invisible en el fotograma 0 y al final.</item>
    /// </list>
    /// </summary>
    internal sealed class BrandIntroMorph
    {
        /// <summary>
        /// Lado de la vista del icono adaptativo en la pantalla del sistema (dp): Android 12+ pinta el icono adaptativo con fondo a
        /// 240 × 240 dp y lo recorta en un círculo de 160 dp (guía «Splash screens» de Android: «App icon with an icon background:
        /// 240×240 dp, fit within a circle 160 dp in diameter»; el tema v31 fija el fondo del icono para que sea siempre esa caja);
        /// <c>af_launch_window</c> (Android 8-11 y el velo nativo) repite esa caja con capas de 240 dp y un anillo que deja un círculo
        /// de 160 dp.
        /// </summary>
        public const float IconCanvasDp = 240f;

        /// <summary>Diámetro del círculo visible del icono en la pantalla del sistema (dp): 2/3 de la vista, como la máscara de 72 de 108 dp.</summary>
        public const float IconMaskDp = 160f;

        /// <summary>
        /// Escala del dibujo de 1024 unidades dentro de las capas adaptativas (<c>ADAPT</c> de <c>generar.py</c>: «scale(0.86)»
        /// alrededor del centro). Las piezas de <see cref="BrandEmblem"/> van en unidades del dibujo; la vista, en las de la capa.
        /// </summary>
        public const float AdaptScale = 0.86f;

        /// <summary>Recurso con los bytes PNG de la vista del icono (<c>Resources/AF/Brand/af_icon_vista.bytes</c>).</summary>
        public const string IconResource = "AF/Brand/af_icon_vista";

        /// <summary>Lado de la textura del círculo y del aro (px) [P].</summary>
        internal const int IrisTextureSize = 256;

        /// <summary>Lo más que brilla el aro de luz, a mitad de la apertura [P].</summary>
        internal const float RimMaxAlpha = 0.3f;

        /// <summary>Escala del fondo de la escena cuando empieza a verse (sobre la cuia; baja a 1 al abrirse) [P].</summary>
        internal const float BackStartScale = 1.12f;

        // Radio del agujero dentro de su textura (px): deja 1 px de petróleo en el borde para el suavizado.
        private const float IrisHoleRadiusPx = (IrisTextureSize * 0.5f) - 1f;

        private static readonly Color RimColor = new Color(0.86f, 0.97f, 0.94f, 1f);

        private readonly RectTransform _back;
        private readonly RectTransform _top;
        private readonly Image _cover;
        private readonly Image _icon;
        private readonly Image _iris;
        private readonly Image[] _bars = new Image[4];
        private readonly Image _rim;
        private readonly Texture2D? _iconTexture;
        private readonly Sprite? _iconSprite;
        private readonly Texture2D _irisTexture;
        private readonly Sprite _irisSprite;
        private readonly Texture2D _rimTexture;
        private readonly Sprite _rimSprite;

        private float _w;
        private float _h;
        private Vector2 _center;
        private float _d0;
        private float _dFull;
        private float _k0;
        private Vector2 _sceneCuia;
        private Vector2 _screenCuia0;
        private Rect _iconScene;
        private Rect _iconFrame0;
        private bool _iconOnTop;

        /// <summary>
        /// Crea las piezas dentro de <paramref name="root"/>, encima de <paramref name="back"/> (el fondo de la escena, ya creado):
        /// la capa de petróleo que tapa el fondo, el grupo de la cuia (al que pasan <paramref name="impact"/>,
        /// <paramref name="stream"/> y <paramref name="cuia"/>, con la vista del icono debajo), el círculo y el aro.
        /// </summary>
        public BrandIntroMorph(RectTransform root, RectTransform back, Image impact, Image stream, Image cuia)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            _back = back;
            _cover = UiBuilder.Image("MorphCover", root, null, BrandIntro.Petroleum);
            UiBuilder.Stretch(_cover.rectTransform);

            var topGo = new GameObject("Cuia", typeof(RectTransform));
            topGo.transform.SetParent(root, false);
            _top = (RectTransform)topGo.transform;
            _iconTexture = LoadIcon();
            if (_iconTexture != null)
            {
                _iconSprite = Sprite.Create(_iconTexture, new Rect(0f, 0f, _iconTexture.width, _iconTexture.height), new Vector2(0.5f, 0.5f), 100f);
                _iconSprite.name = "AF_IconVista";
            }

            _icon = UiBuilder.Image("MorphIcon", _top, _iconSprite, Color.white);
            _icon.gameObject.SetActive(_iconSprite != null);
            impact.transform.SetParent(_top, false);
            stream.transform.SetParent(_top, false);
            cuia.transform.SetParent(_top, false);

            var pixels = new Color32[IrisTextureSize * IrisTextureSize]; // un solo arreglo para las dos texturas
            _irisTexture = RingTexture("AF_MorphIris", false, pixels);
            _irisSprite = Sprite.Create(_irisTexture, new Rect(0f, 0f, IrisTextureSize, IrisTextureSize), new Vector2(0.5f, 0.5f), 100f);
            _iris = UiBuilder.Image("MorphIris", root, _irisSprite, BrandIntro.Petroleum);
            for (int i = 0; i < _bars.Length; i++)
            {
                _bars[i] = UiBuilder.Image("MorphBar" + i, root, null, BrandIntro.Petroleum);
            }

            _rimTexture = RingTexture("AF_MorphRim", true, pixels);
            _rimSprite = Sprite.Create(_rimTexture, new Rect(0f, 0f, IrisTextureSize, IrisTextureSize), new Vector2(0.5f, 0.5f), 100f);
            _rim = UiBuilder.Image("MorphRim", root, _rimSprite, new Color(RimColor.r, RimColor.g, RimColor.b, 0f));
            BuildMs = clock.ElapsedMilliseconds;
        }

        /// <summary>Lo que costó crear las piezas, con la vista del icono decodificada (ms; sonda <c>morph_build_ms</c>).</summary>
        public long BuildMs { get; }

        /// <summary>Hay vista del icono (el recurso se pudo leer).</summary>
        public bool HasIcon => _iconSprite != null;

        /// <summary>Unidades del lienzo por dp de Android (1 si no se sabe la densidad: el Editor).</summary>
        public float UnitsPerDp { get; private set; } = 1f;

        /// <summary>Diámetro del círculo ahora (unidades del lienzo).</summary>
        public float Diameter { get; private set; }

        /// <summary>Diámetro del círculo en el fotograma 0 (unidades del lienzo): <see cref="IconMaskDp"/> dp.</summary>
        public float StartDiameter => _d0;

        /// <summary>Diámetro con el que el círculo ya cubre toda la pantalla (unidades del lienzo).</summary>
        public float FullDiameter => _dFull;

        /// <summary>Centro del círculo (unidades del lienzo, y desde arriba): el centro de la pantalla.</summary>
        public Vector2 Center => _center;

        /// <summary>El círculo (y su petróleo alrededor) está a la vista: aún no cubre la pantalla.</summary>
        public bool IrisVisible => _iris.gameObject.activeSelf;

        /// <summary>Lo que se ve de la vista del icono (0 a 1).</summary>
        public float IconAlpha => _icon.gameObject.activeSelf ? _icon.color.a : 0f;

        /// <summary>Lo que tapa el petróleo al fondo de la escena (0 a 1; 1 = el fondo aún no se ve).</summary>
        public float CoverAlpha => _cover.gameObject.activeSelf ? _cover.color.a : 0f;

        /// <summary>Escala del grupo de la cuia ahora (1 = la de la escena; menor mientras viene del icono).</summary>
        public float CuiaScale => _top.localScale.x;

        /// <summary>Escala del fondo de la escena ahora (≥ 1: nunca enseña sus bordes).</summary>
        public float BackScale => _back.localScale.x;

        /// <summary>El grupo de la cuia (la cuia, el chorro, los aros y la vista del icono).</summary>
        public RectTransform Top => _top;

        /// <summary>La vista del icono (tests).</summary>
        public Image Icon => _icon;

        /// <summary>El cuadrado del círculo (tests).</summary>
        public Image Iris => _iris;

        /// <summary>El aro de luz (tests).</summary>
        public Image Rim => _rim;

        /// <summary>
        /// Rehace la geometría con la escena <paramref name="art"/> y <paramref name="unitsPerDp"/> unidades del lienzo por dp de
        /// Android: el fotograma 0 (el icono centrado, de <see cref="IconCanvasDp"/> dp, con el círculo de <see cref="IconMaskDp"/>) y
        /// el viaje de la cuia de ese encuadre al suyo.
        /// </summary>
        public void Layout(BrandEmblemArt art, float unitsPerDp)
        {
            UnitsPerDp = unitsPerDp > 0f ? unitsPerDp : 1f;
            _w = art.Width;
            _h = art.Height;
            _center = new Vector2(_w * 0.5f, _h * 0.5f);
            _d0 = IconMaskDp * UnitsPerDp;
            _dFull = (2f * Mathf.Sqrt((_center.x * _center.x) + (_center.y * _center.y))) + 4f;
            UiBuilder.Place(_back, 0f, 0f, _w, _h);
            UiBuilder.Place(_top, 0f, 0f, _w, _h);

            // La vista del icono, en coordenadas de la escena: la capa de 1024 cubre el dibujo de 512 ± 512 / 0,86.
            BrandRiverScene scene = art.Scene;
            float s = scene.CuiaScale;
            float half = 512f / AdaptScale;
            _iconScene = new Rect(((512f - half) * s) + scene.CuiaTx, ((512f - half) * s) + scene.CuiaTy, 2f * half * s, 2f * half * s);
            float side0 = IconCanvasDp * UnitsPerDp;
            _iconFrame0 = new Rect(_center.x - (side0 * 0.5f), _center.y - (side0 * 0.5f), side0, side0);
            _k0 = side0 / _iconScene.width;

            // El ancla: el centro de la cuia, el mismo dibujo en el icono y en la escena.
            Rect cuia = BrandEmblem.GroupBounds(BrandEmblem.Cuia);
            _sceneCuia = new Vector2((cuia.center.x * s) + scene.CuiaTx, (cuia.center.y * s) + scene.CuiaTy);
            _screenCuia0 = _iconFrame0.position + (_k0 * (_sceneCuia - _iconScene.position));
        }

        /// <summary>
        /// La apertura en <paramref name="e"/> (0 el icono, 1 la escena; ya suavizado): la cuia viaja del encuadre del icono al suyo
        /// con la vista del icono a <paramref name="iconAlpha"/>; el fondo, a escala <see cref="BackStartScale"/> → 1 sobre la cuia,
        /// tapado por el petróleo a <paramref name="coverAlpha"/>; el círculo crece de 160 dp a toda la pantalla y el aro brilla a
        /// mitad de camino. Sin asignaciones.
        /// </summary>
        public void Apply(float e, float iconAlpha, float coverAlpha)
        {
            e = Mathf.Clamp01(e);
            SetIconOnTop(false);
            float k = Mathf.Pow(_k0, 1f - e); // geométrica: el zoom se ve parejo
            Vector2 cuia = Vector2.LerpUnclamped(_screenCuia0, _sceneCuia, e);
            Vector2 p = cuia - (k * _sceneCuia);
            _top.anchoredPosition = new Vector2(p.x, -p.y);
            _top.localScale = new Vector3(k, k, 1f);

            // El fondo: siempre ≥ 1 y sobre la cuia de la escena, así que cubre la pantalla entera.
            float kb = Mathf.Lerp(BackStartScale, 1f, e);
            Vector2 pb = _sceneCuia - (kb * _sceneCuia);
            _back.anchoredPosition = new Vector2(pb.x, -pb.y);
            _back.localScale = new Vector3(kb, kb, 1f);

            SetIcon(_iconScene, iconAlpha);
            SetCover(coverAlpha);
            float rim = RimMaxAlpha * Mathf.Sin(Mathf.PI * e);
            SetIris(Mathf.Lerp(_d0, _dFull, e), 1f, rim);
        }

        /// <summary>
        /// Reducir movimiento: la escena quieta en su encuadre y, encima de todo, el icono del fotograma 0 con su círculo y su
        /// petróleo, que se funden a <paramref name="fade"/> (0 el icono entero, 1 ya fuera). Sin asignaciones.
        /// </summary>
        public void ApplyFade(float fade)
        {
            fade = Mathf.Clamp01(fade);
            SetIconOnTop(true);
            _back.anchoredPosition = Vector2.zero;
            _back.localScale = Vector3.one;
            _top.anchoredPosition = Vector2.zero;
            _top.localScale = Vector3.one;
            SetIcon(_iconFrame0, 1f - fade);
            SetCover(0f);
            SetIris(_d0, 1f - fade, 0f);
        }

        /// <summary>Destruye las texturas y los sprites propios.</summary>
        public void Dispose()
        {
            Object.Destroy(_irisSprite);
            Object.Destroy(_irisTexture);
            Object.Destroy(_rimSprite);
            Object.Destroy(_rimTexture);
            if (_iconSprite != null)
            {
                Object.Destroy(_iconSprite);
            }

            if (_iconTexture != null)
            {
                Object.Destroy(_iconTexture);
            }
        }

        // En la apertura, la vista del icono va debajo de la cuia (la tapa la misma cuia); con Reducir movimiento, encima de todo
        // (la cuia de la escena está en otro sitio).
        private void SetIconOnTop(bool onTop)
        {
            if (_iconOnTop == onTop)
            {
                return;
            }

            _iconOnTop = onTop;
            if (onTop)
            {
                _icon.transform.SetAsLastSibling();
            }
            else
            {
                _icon.transform.SetAsFirstSibling();
            }
        }

        private void SetIcon(Rect r, float alpha)
        {
            bool show = _iconSprite != null && alpha > 0.001f;
            if (_icon.gameObject.activeSelf != show)
            {
                _icon.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            UiBuilder.Place(_icon.rectTransform, r.x, r.y, r.width, r.height);
            _icon.color = new Color(1f, 1f, 1f, alpha);
        }

        private void SetCover(float alpha)
        {
            bool show = alpha > 0.001f;
            if (_cover.gameObject.activeSelf != show)
            {
                _cover.gameObject.SetActive(show);
            }

            if (show)
            {
                Color c = BrandIntro.Petroleum;
                c.a = alpha;
                _cover.color = c;
            }
        }

        // El círculo de diámetro d (unidades), su petróleo a alpha y el aro a rim; se apaga entero si ya cubre la pantalla.
        private void SetIris(float d, float alpha, float rim)
        {
            Diameter = d;
            bool show = d < _dFull - 0.01f && alpha > 0.001f;
            if (_iris.gameObject.activeSelf != show)
            {
                _iris.gameObject.SetActive(show);
                for (int i = 0; i < _bars.Length; i++)
                {
                    _bars[i].gameObject.SetActive(show);
                }
            }

            bool showRim = show && rim > 0.001f;
            if (_rim.gameObject.activeSelf != showRim)
            {
                _rim.gameObject.SetActive(showRim);
            }

            if (!show)
            {
                return;
            }

            // El agujero de la textura mide 2 · IrisHoleRadiusPx: el cuadrado se agranda lo justo para que mida d.
            float side = d * IrisTextureSize / (2f * IrisHoleRadiusPx);
            float x0 = _center.x - (side * 0.5f);
            float y0 = _center.y - (side * 0.5f);
            float x1 = x0 + side;
            float y1 = y0 + side;
            Color petro = BrandIntro.Petroleum;
            petro.a = alpha;
            _iris.color = petro;
            UiBuilder.Place(_iris.rectTransform, x0, y0, side, side);

            // Las bandas se meten 1 unidad bajo el cuadrado (sus esquinas son petróleo): sin rendijas.
            PlaceBar(_bars[0], 0f, 0f, _w, Mathf.Max(0f, y0 + 1f), petro);
            PlaceBar(_bars[1], 0f, y1 - 1f, _w, Mathf.Max(0f, _h - y1 + 1f), petro);
            PlaceBar(_bars[2], 0f, y0, Mathf.Max(0f, x0 + 1f), side, petro);
            PlaceBar(_bars[3], x1 - 1f, y0, Mathf.Max(0f, _w - x1 + 1f), side, petro);

            if (showRim)
            {
                UiBuilder.Place(_rim.rectTransform, x0, y0, side, side);
                _rim.color = new Color(RimColor.r, RimColor.g, RimColor.b, rim * alpha);
            }
        }

        private static void PlaceBar(Image bar, float x, float y, float w, float h, Color color)
        {
            UiBuilder.Place(bar.rectTransform, x, y, w, h);
            bar.color = color;
        }

        // La vista del icono desde sus bytes PNG (sin compresión ni mipmaps); null si falta el recurso.
        private static Texture2D? LoadIcon()
        {
            var asset = Resources.Load<TextAsset>(IconResource);
            if (asset == null)
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.name = "AF_IconVista";
            bool ok = tex.LoadImage(asset.bytes, true);
            Resources.UnloadAsset(asset);
            if (!ok)
            {
                Object.Destroy(tex);
                return null;
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        // El círculo (blanco fuera del agujero, transparente dentro; se tiñe de petróleo) o el aro de luz (un anillo fino en el
        // borde del agujero), con el borde suavizado en 1 px; escribe en pixels (reutilizado).
        private static Texture2D RingTexture(string name, bool rim, Color32[] pixels)
        {
            int n = IrisTextureSize;
            float c = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                float dy = (y + 0.5f) - c;
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - IrisHoleRadiusPx;
                    float a;
                    if (rim)
                    {
                        float u = (d + 1.2f) / 1.0f; // el anillo, justo por dentro del borde
                        a = Mathf.Exp(-(u * u));
                    }
                    else
                    {
                        a = Mathf.Clamp01(d + 0.5f);
                    }

                    pixels[(y * n) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
