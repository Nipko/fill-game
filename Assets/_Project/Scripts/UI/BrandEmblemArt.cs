#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El emblema v2 colocado en un lienzo (añadido de D-051): dónde va en la pantalla (<see cref="Scale"/> dp por unidad del icono
    /// y el origen) y sus piezas rasterizadas a la escala de la pantalla (<see cref="BrandEmblem"/>), una por grupo, para que la
    /// intro las anime y la pantalla de arranque las enseñe quietas, las mismas y en el mismo sitio. Lo hace y lo destruye la
    /// pantalla de arranque; la intro solo lo usa.
    /// <list type="bullet">
    /// <item>El icono (1024 unidades) ocupa el 94 % del ancho (como mucho 380 dp; un 8 % menos en las pantallas bajas), centrado, con
    /// el impacto del chorro al 44 % del alto (al 50 % en las pantallas bajas, para que TiTino quepa encima) [P].</item>
    /// <item>La base, el açaí y la orilla de acá van de borde a borde; la orilla de acá, hasta <see cref="BankBottomUnits"/>: debajo
    /// sigue el suelo de la pantalla de arranque, del mismo color.</item>
    /// </list>
    /// </summary>
    internal sealed class BrandEmblemArt : IDisposable
    {
        /// <summary>Hasta dónde llega la orilla de acá del emblema (unidades del icono); debajo, el suelo de la pantalla.</summary>
        public const float BankBottomUnits = 960f;

        private const float WidthShare = 0.94f;
        private const float AcaiFadeUnits = 260f;
        private const float MaxWidthDp = 380f;
        private const float ShortShrink = 0.92f;
        private const float ImpactAt = 0.44f;
        private const float ImpactAtShort = 0.50f;

        private readonly Texture2D?[] _textures = new Texture2D?[BrandEmblem.GroupCount];
        private readonly Sprite?[] _sprites = new Sprite?[BrandEmblem.GroupCount];
        private readonly Rect[] _windows = new Rect[BrandEmblem.GroupCount];
        private readonly float _pxPerDp;

        private BrandEmblemArt(float width, float height, float pxPerDp)
        {
            Width = width;
            Height = height;
            _pxPerDp = pxPerDp;
            bool shortScreen = height < 2f * width;
            float s = Mathf.Min(width * WidthShare, MaxWidthDp) / 1024f;
            Scale = shortScreen ? s * ShortShrink : s;
            float impactY = Mathf.Round(height * (shortScreen ? ImpactAtShort : ImpactAt));
            OriginX = (width * 0.5f) - (512f * Scale);
            OriginY = impactY - (BrandEmblem.ImpactY * Scale);

            float left = -OriginX / Scale;
            float right = (width - OriginX) / Scale;
            for (int group = 0; group < BrandEmblem.GroupCount; group++)
            {
                Rect gb = BrandEmblem.GroupBounds(group);
                float x0 = gb.xMin - 2f;
                float x1 = gb.xMax + 2f;
                float y1 = gb.yMax + 2f;
                if (group == BrandEmblem.Base || group == BrandEmblem.Acai || group == BrandEmblem.Bank)
                {
                    x0 = Mathf.Max(x0, left);
                    x1 = Mathf.Min(x1, right);
                }

                if (group == BrandEmblem.Acai)
                {
                    // El açaí sigue más allá del borde derecho, donde se funde (AcaiFadeUnits): así, encogido mientras se abre en la
                    // intro, no enseña un corte recto, y entero el fundido queda fuera de la pantalla.
                    x1 = Mathf.Min(gb.xMax, right + AcaiFadeUnits);
                }

                if (group == BrandEmblem.Bank)
                {
                    y1 = BankBottomUnits;
                }

                _windows[group] = Rect.MinMaxRect(x0, gb.yMin - 2f, x1, y1);
            }
        }

        /// <summary>Ancho del lienzo (dp).</summary>
        public float Width { get; }

        /// <summary>Alto del lienzo (dp).</summary>
        public float Height { get; }

        /// <summary>dp por unidad del icono.</summary>
        public float Scale { get; }

        /// <summary>x (dp) de la unidad 0 del icono.</summary>
        public float OriginX { get; }

        /// <summary>y (dp desde arriba) de la unidad 0 del icono.</summary>
        public float OriginY { get; }

        /// <summary>Lo más alto del emblema (dp desde arriba): las palmeras, la cuia y el chorro.</summary>
        public float TopDp => Mathf.Min(Mathf.Min(DpRect(BrandEmblem.Base).yMin, DpRect(BrandEmblem.Cuia).yMin), DpRect(BrandEmblem.Stream).yMin);

        /// <summary>Donde acaba la orilla de acá del emblema (dp desde arriba).</summary>
        public float BankBottomDp => Y(BankBottomUnits);

        /// <summary>
        /// Hace el emblema para un lienzo de <paramref name="width"/> × <paramref name="height"/> dp a <paramref name="pxPerDp"/>
        /// px por dp, con todas sus piezas menos la orilla de acá (<see cref="EnsureBank"/>), que la intro no enseña.
        /// </summary>
        public static BrandEmblemArt Create(float width, float height, float pxPerDp)
        {
            var art = new BrandEmblemArt(width, height, Mathf.Max(1f, pxPerDp));
            for (int group = 0; group < BrandEmblem.GroupCount; group++)
            {
                if (group != BrandEmblem.Bank)
                {
                    art.Build(group);
                }
            }

            return art;
        }

        /// <summary>El emblema sirve tal cual para este lienzo y esta densidad.</summary>
        public bool Fits(float width, float height, float pxPerDp)
        {
            return Mathf.Abs(width - Width) < 0.01f && Mathf.Abs(height - Height) < 0.01f && Mathf.Abs(Mathf.Max(1f, pxPerDp) - _pxPerDp) < 0.01f;
        }

        /// <summary>x en dp de la unidad <paramref name="ix"/> del icono.</summary>
        public float X(float ix)
        {
            return OriginX + (ix * Scale);
        }

        /// <summary>y en dp (desde arriba) de la unidad <paramref name="iy"/> del icono.</summary>
        public float Y(float iy)
        {
            return OriginY + (iy * Scale);
        }

        /// <summary>La pieza del grupo <paramref name="group"/> (null si aún no está hecha).</summary>
        public Sprite? Sprite(int group)
        {
            return _sprites[group];
        }

        /// <summary>Hace la orilla de acá (la primera vez que se pide).</summary>
        public Sprite EnsureBank()
        {
            if (_sprites[BrandEmblem.Bank] == null)
            {
                Build(BrandEmblem.Bank);
            }

            return _sprites[BrandEmblem.Bank]!;
        }

        /// <summary>El rectángulo de la pieza (dp, desde arriba a la izquierda), el de su textura.</summary>
        public Rect DpRect(int group)
        {
            Rect win = _windows[group];
            float k = Scale * _pxPerDp;
            float w = Mathf.Max(1, Mathf.CeilToInt(win.width * k)) / _pxPerDp;
            float h = Mathf.Max(1, Mathf.CeilToInt(win.height * k)) / _pxPerDp;
            return new Rect(X(win.xMin), Y(win.yMin), w, h);
        }

        /// <summary>
        /// Coloca <paramref name="image"/> con la pieza <paramref name="group"/>, con el pivote en <paramref name="pivotUnits"/>
        /// (unidades del icono; null: arriba a la izquierda), sin moverla de su sitio.
        /// </summary>
        public void Place(Image image, int group, Vector2? pivotUnits = null)
        {
            Sprite? sprite = _sprites[group];
            if (image.sprite != sprite)
            {
                image.sprite = sprite;
            }

            Rect r = DpRect(group);
            RectTransform rt = image.rectTransform;
            UiBuilder.Place(rt, r.x, r.y, r.width, r.height);
            if (pivotUnits.HasValue)
            {
                float px = (X(pivotUnits.Value.x) - r.x) / r.width;
                float py = 1f - ((Y(pivotUnits.Value.y) - r.y) / r.height);
                rt.pivot = new Vector2(px, py);
                rt.anchoredPosition = new Vector2(r.x + (px * r.width), -(r.y + ((1f - py) * r.height)));
            }
        }

        /// <summary>Destruye las texturas y los sprites.</summary>
        public void Dispose()
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_sprites[i] != null)
                {
                    UnityEngine.Object.Destroy(_sprites[i]);
                    _sprites[i] = null;
                }

                if (_textures[i] != null)
                {
                    UnityEngine.Object.Destroy(_textures[i]);
                    _textures[i] = null;
                }
            }
        }

        private void Build(int group)
        {
            float fade = group == BrandEmblem.Acai ? AcaiFadeUnits : 0f;
            Texture2D tex = BrandEmblem.Render(1 << group, _windows[group], Scale * _pxPerDp, "AF_Emblem" + group, fade);
            _textures[group] = tex;
            Sprite sprite = UnityEngine.Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name;
            _sprites[group] = sprite;
        }
    }
}
