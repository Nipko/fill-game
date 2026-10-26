#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// «El Amazonas que se tiñe» colocado en un lienzo (añadido de D-051): la escena (<see cref="BrandRiverScene"/>: el río con
    /// meandros del horizonte al primer plano, la selva, las playas y la isla, con la cuia del icono que vierte sobre él) rasterizada
    /// a la escala de la pantalla en piezas, para que la intro las anime y la pantalla de arranque las enseñe quietas, las mismas y
    /// en el mismo sitio. Lo hace y lo destruye la pantalla de arranque; la intro solo lo usa.
    /// </summary>
    internal sealed class BrandEmblemArt : IDisposable
    {
        /// <summary>Pieza: la selva, el río blanco, las playas, las palmeras y las ondas.</summary>
        public const int Base = 0;

        /// <summary>Pieza: la isla, por encima del açaí.</summary>
        public const int Island = 1;

        /// <summary>Pieza: los aros del impacto.</summary>
        public const int Impact = 2;

        /// <summary>Pieza: el chorro.</summary>
        public const int Stream = 3;

        /// <summary>Pieza: la cuia.</summary>
        public const int Cuia = 4;

        /// <summary>Primera pieza del açaí; las siguientes, sus tramos río abajo (<see cref="BrandRiverScene.ChunkCount"/>).</summary>
        public const int AcaiFirst = 5;

        /// <summary>Número de piezas.</summary>
        public const int Count = AcaiFirst + BrandRiverScene.ChunkCount;

        private readonly Texture2D?[] _textures = new Texture2D?[Count];
        private readonly Sprite?[] _sprites = new Sprite?[Count];
        private readonly Rect[] _rects = new Rect[Count];
        private readonly float _pxPerDp;

        // El paisaje (la selva, el río y la isla) se rasteriza a 2 px por dp como mucho: es blando por naturaleza y así su coste
        // antes del primer fotograma queda acotado; la cuia, el chorro y los aros, a la densidad de la pantalla [P].
        private const float LandMaxPxPerDp = 2f;

        private BrandEmblemArt(float width, float height, float pxPerDp)
        {
            Width = width;
            Height = height;
            _pxPerDp = pxPerDp;
            float land = Mathf.Min(pxPerDp, LandMaxPxPerDp);
            Scene = new BrandRiverScene(width, height);
            Build(Base, Scene.BaseWindow, Scene.RenderBase, land);
            for (int k = 0; k < BrandRiverScene.ChunkCount; k++)
            {
                int chunk = k;
                Build(AcaiFirst + k, Scene.ChunkWindow(k), ppd => Scene.RenderChunk(chunk, ppd), land);
            }

            Build(Island, Scene.IslandWindow, Scene.RenderIsland, land);
            Build(Impact, Scene.DataWindow(BrandEmblem.Impact), ppd => Scene.RenderData(BrandEmblem.Impact, ppd), pxPerDp);
            Build(Stream, Scene.DataWindow(BrandEmblem.Stream), ppd => Scene.RenderData(BrandEmblem.Stream, ppd), pxPerDp);
            Build(Cuia, Scene.DataWindow(BrandEmblem.Cuia), ppd => Scene.RenderData(BrandEmblem.Cuia, ppd), pxPerDp);
        }

        /// <summary>La escena (geometría del río y de la cuia).</summary>
        public BrandRiverScene Scene { get; }

        /// <summary>Ancho del lienzo (dp).</summary>
        public float Width { get; }

        /// <summary>Alto del lienzo (dp).</summary>
        public float Height { get; }

        /// <summary>Lo más alto de lo que TiTino no puede tapar (dp desde arriba): la selva del horizonte, la cuia y el chorro.</summary>
        public float TopDp => Mathf.Min(Scene.SkylineDp, Mathf.Min(_rects[Cuia].yMin, _rects[Stream].yMin));

        /// <summary>Lo más bajo del impacto (dp desde arriba): la cuia, el chorro y los aros quedan por encima.</summary>
        public float ImpactBottomDp => Mathf.Max(_rects[Impact].yMax, Mathf.Max(_rects[Cuia].yMax, _rects[Stream].yMax));

        /// <summary>Hace la escena para un lienzo de <paramref name="width"/> × <paramref name="height"/> dp a <paramref name="pxPerDp"/>.</summary>
        public static BrandEmblemArt Create(float width, float height, float pxPerDp)
        {
            return new BrandEmblemArt(width, height, Mathf.Max(1f, pxPerDp));
        }

        /// <summary>La escena sirve tal cual para este lienzo y esta densidad.</summary>
        public bool Fits(float width, float height, float pxPerDp)
        {
            return Mathf.Abs(width - Width) < 0.01f && Mathf.Abs(height - Height) < 0.01f && Mathf.Abs(Mathf.Max(1f, pxPerDp) - _pxPerDp) < 0.01f;
        }

        /// <summary>La pieza <paramref name="piece"/>.</summary>
        public Sprite? Sprite(int piece)
        {
            return _sprites[piece];
        }

        /// <summary>El rectángulo de la pieza (dp, desde arriba a la izquierda), el de su textura.</summary>
        public Rect DpRect(int piece)
        {
            return _rects[piece];
        }

        /// <summary>
        /// Coloca <paramref name="image"/> con la pieza <paramref name="piece"/>; con <paramref name="pivotDp"/>, con el pivote en ese
        /// punto (dp), sin moverla de su sitio.
        /// </summary>
        public void Place(Image image, int piece, Vector2? pivotDp = null)
        {
            Sprite? sprite = _sprites[piece];
            if (image.sprite != sprite)
            {
                image.sprite = sprite;
            }

            Rect r = _rects[piece];
            RectTransform rt = image.rectTransform;
            UiBuilder.Place(rt, r.x, r.y, r.width, r.height);
            if (pivotDp.HasValue)
            {
                float px = (pivotDp.Value.x - r.x) / r.width;
                float py = 1f - ((pivotDp.Value.y - r.y) / r.height);
                rt.pivot = new Vector2(px, py);
                rt.anchoredPosition = new Vector2(pivotDp.Value.x, -pivotDp.Value.y);
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

        private void Build(int piece, Rect window, Func<float, Texture2D> render, float pxPerDp)
        {
            Texture2D tex = render(pxPerDp);
            _textures[piece] = tex;
            _rects[piece] = new Rect(window.x, window.y, tex.width / pxPerDp, tex.height / pxPerDp);
            Sprite sprite = UnityEngine.Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name;
            _sprites[piece] = sprite;
        }
    }
}
