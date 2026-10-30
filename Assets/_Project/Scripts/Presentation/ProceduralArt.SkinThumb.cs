#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Miniatura de una skin de cuia para el Mercado y las ofertas (observación 4 de la jornada 6 de la prueba completa: las skins
    /// salían como círculos de color lisos y el jugador no veía qué elegía). Es la cuia del tablero en pequeño, con el mismo dibujo:
    /// <list type="bullet">
    /// <item>el vidrio trasero con la cavidad, el vidrio frontal y las marcas de dosis los pintan las mismas funciones que los sprites
    /// del tablero (<see cref="PaintCuiaBack"/>, <see cref="PaintCuiaFront"/> y <see cref="PaintCuiaMarks"/>), con el tinte del vidrio
    /// de la skin (<see cref="CuiaSkin.GlassMultiplier"/>, como <c>CuiaView.ApplySkin</c>);</item>
    /// <item>el borde, el pie, la banda del hombro y el asa, en su color y en el sitio de <see cref="CuiaSkinParts"/>, la fuente que
    /// usa también <c>CuiaView</c>;</item>
    /// <item>dos dosis de líquido (Agua del Tapajós y Taperebá, 07 §3) con su cara de arriba aclarada como el menisco, para que se lea
    /// como una cuia con agua y no como un frasco vacío.</item>
    /// </list>
    /// Va sobre una ficha con el panel del tablero (D-004, azul petróleo, esquinas redondeadas), para que el vidrio se lea como en el
    /// juego y no como tinta negra sobre el papel crema; la banda del hombro y el asa, más gruesas que en el tablero y todas las
    /// piezas con un canto, para que cada skin se reconozca a primera vista (revisión de la observación 4).
    /// Una textura de 104 × 242 px por skin (cuia de 4 a <see cref="SkinThumbPixelsPerU"/> px por u y la ficha; unos 100 KB), hecha
    /// la primera vez que se pide y guardada: la sueltan <see cref="Dispose"/> con el resto del arte (memoria, observación 6).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Densidad de la miniatura (px por u): la ficha mide 242 px de alto, casi 1:1 a 80 dp y 3 px por dp.</summary>
        public const int SkinThumbPixelsPerU = 40;

        /// <summary>Capacidad de la cuia de la miniatura: la estándar (02 §5).</summary>
        public const int SkinThumbCapacity = 4;

        /// <summary>Prefijo del nombre de los sprites de miniatura (tests y sonda).</summary>
        public const string SkinThumbPrefix = "skin_thumb_";

        /// <summary>Dosis de líquido de la miniatura, de abajo arriba (índices de motor, 07 §3).</summary>
        private static readonly int[] SkinThumbDoses = { 10, 3 };

        private readonly Dictionary<string, Sprite> _skinThumbs = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>
        /// La miniatura de <paramref name="skin"/> (null: la base), con el pivote en el centro; se tiñe en blanco (lleva sus
        /// colores). La misma instancia en cada llamada hasta <see cref="Dispose"/>.
        /// </summary>
        public Sprite SkinThumb(CuiaSkin? skin)
        {
            CuiaSkin s = skin ?? CuiaSkin.Base;
            if (_skinThumbs.TryGetValue(s.Id, out Sprite? sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = BuildSkinThumb(s);
            _skinThumbs[s.Id] = sprite;
            return sprite;
        }

        /// <summary>Es una miniatura de skin (<see cref="SkinThumb"/>; tests).</summary>
        public static bool IsSkinThumb(Sprite? sprite, string skinId)
        {
            return sprite != null && string.Equals(sprite.name, SkinThumbPrefix + skinId, StringComparison.Ordinal);
        }

        private Sprite BuildSkinThumb(CuiaSkin skin)
        {
            return MakeSprite(PaintSkinThumb(skin), SkinThumbPrefix + skin.Id, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        /// <summary>
        /// El lienzo de la miniatura de <paramref name="skin"/> (la ficha con la cuia), sin hacer la textura (tests: sus píxeles);
        /// quien lo pide lo suelta con <see cref="PixelCanvas.Release"/>.
        /// </summary>
        internal PixelCanvas PaintSkinThumb(CuiaSkin skin)
        {
            return PaintSkinThumb(skin, SkinThumbPixelsPerU);
        }

        /// <summary>
        /// El lienzo de la ficha de <paramref name="skin"/> a <paramref name="ppu"/> px por u: la miniatura
        /// (<see cref="SkinThumbPixelsPerU"/>) o la cuia en grande de la vista previa del Mercado (<see cref="SkinPreviewPixelsPerU"/>;
        /// observación 24 de la jornada 6). El mismo dibujo; los cantos de las piezas crecen con la densidad.
        /// </summary>
        internal PixelCanvas PaintSkinThumb(CuiaSkin skin, int ppu)
        {
            const int cap = SkinThumbCapacity;
            PixelCanvas cuia = NewCuiaCanvas(cap, ppu);
            PixelCanvas layer = NewCuiaCanvas(cap, ppu);
            Color glass = skin.GlassMultiplier;
            PaintCuiaBack(layer, cap, ppu);
            CompositeLayer(cuia, layer, glass, 0, 0);
            PaintThumbLiquid(cuia, cap, ppu);
            ClearLayer(layer);
            PaintCuiaFront(layer, cap, ppu);
            CompositeLayer(cuia, layer, glass, 0, 0);
            ClearLayer(layer);
            PaintCuiaMarks(layer, cap, ppu);
            CompositeLayer(cuia, layer, Color.white, 0, 0);
            layer.Release();
            if (!skin.IsBase)
            {
                // El orden de CuiaView.BuildSkinParts: pie, borde, banda (o sus dos mitades) y asa. La banda y el asa, más gruesas
                // que en el tablero (ThumbBandHeightU, ThumbHandle*): a 50 dp de alto son lo que distingue una skin de otra.
                FillSkinPiece(cuia, CuiaSkinParts.Foot(), skin.Foot, ppu);
                FillSkinPiece(cuia, CuiaSkinParts.Rim(cap), skin.Rim, ppu);
                if (skin.HasBand && skin.BandBicolor)
                {
                    FillSkinPiece(cuia, Thicker(CuiaSkinParts.Band(cap, -1)), skin.Band, ppu);
                    FillSkinPiece(cuia, Thicker(CuiaSkinParts.Band(cap, 1)), skin.BandB, ppu);
                }
                else if (skin.HasBand)
                {
                    FillSkinPiece(cuia, Thicker(CuiaSkinParts.Band(cap, 0)), skin.Band, ppu);
                }

                if (skin.HasHandle)
                {
                    Rect h = CuiaSkinParts.Handle(cap);
                    FillSkinPiece(cuia, new Rect(h.x + ((ThumbHandleWidthU - h.width) * 0.5f), h.y, ThumbHandleWidthU, ThumbHandleHeightU), skin.Handle, ppu);
                }
            }

            // La ficha: el panel del tablero (D-004, azul petróleo en degradado de panel_alto a panel_bajo) con las esquinas
            // redondeadas, y la cuia encima, centrada en x y con el pie a ThumbPadU: el vidrio se ve como en el juego, no como tinta
            // sobre el papel crema del Mercado.
            int padX = Mathf.RoundToInt(ThumbPadXU * ppu);
            int padY = Mathf.RoundToInt(ThumbPadYU * ppu);
            var pc = new PixelCanvas(cuia.Width + (2 * padX), cuia.Height + (2 * padY));
            PaintPanelTile(pc, ThumbCornerU * ppu);
            CompositeLayer(pc, cuia, Color.white, padX, padY);
            cuia.Release();
            return pc;
        }

        // Margen de la ficha a los lados (u): deja sitio al asa de la Totuma (0,97 + 0,11 u del eje) y algo de aire.
        internal const float ThumbPadXU = 0.30f;

        // Margen de la ficha arriba y abajo (u).
        internal const float ThumbPadYU = 0.22f;

        // Radio de las esquinas de la ficha (u).
        private const float ThumbCornerU = 0.30f;

        // Alto de la banda del hombro en la miniatura (u; 0,12 en el tablero): cabe en el hombro de 0,40 u sin tapar la cavidad.
        internal const float ThumbBandHeightU = 0.20f;

        // El asa de la Totuma en la miniatura (u; 0,12 × 0,40 en el tablero).
        internal const float ThumbHandleWidthU = 0.16f;

        internal const float ThumbHandleHeightU = 0.46f;

        private static Rect Thicker(Rect band)
        {
            return new Rect(band.x, band.y, band.width, ThumbBandHeightU);
        }

        private void PaintPanelTile(PixelCanvas pc, float radiusPx)
        {
            Color top = _panel.Top;
            Color bottom = _panel.Bottom;
            top.a = 1f;
            bottom.a = 1f;
            float w = pc.Width;
            float h = pc.Height;
            for (int y = 0; y < pc.Height; y++)
            {
                Color c = Color.Lerp(bottom, top, (y + 0.5f) / h); // y hacia arriba: panel_alto arriba
                for (int x = 0; x < pc.Width; x++)
                {
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, 0f, 0f, w, h, radiusPx);
                    pc.Blend(x, y, c, PixelCanvas.Coverage(d));
                }
            }
        }

        // Compone la capa sobre el lienzo (encima) desde (ox, oy), con su color multiplicado por tint (el Image.color del tablero).
        private static void CompositeLayer(PixelCanvas into, PixelCanvas layer, Color tint, int ox, int oy)
        {
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    Color c = layer.Get(x, y);
                    if (c.a <= 0f)
                    {
                        continue;
                    }

                    into.Blend(x + ox, y + oy, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a), 1f);
                }
            }
        }

        private static void ClearLayer(PixelCanvas layer)
        {
            layer.Fill(Color.clear);
        }

        // Las dosis en la cavidad, con las fronteras y el fondo en arco (CuiaGeometry.FaceSagU, como el shader del líquido) y la cara
        // de arriba (la elipse de CuiaGeometry.TopFaceU) aclarada como el menisco (Palette.MeniscusLighten, 07 §3, regla 4).
        private static void PaintThumbLiquid(PixelCanvas pc, int cap, int ppu)
        {
            int doses = SkinThumbDoses.Length;
            float level = CuiaGeometry.CavityBottomU + doses;
            int topTint = SkinThumbDoses[doses - 1];
            Color face = ColorMath.Lighten(Palette.Tint(topTint), Palette.MeniscusLighten(topTint));
            face.a = 1f;
            for (int y = 0; y < pc.Height; y++)
            {
                float yu = (y + 0.5f) / ppu;
                if (yu > level + CuiaGeometry.TopFaceU)
                {
                    break;
                }

                float inner = CuiaGeometry.InnerHalfWidth(yu, cap);
                float wallW = yu < CuiaGeometry.CavityBottomU ? CuiaGeometry.CavityHalfWidthU : inner;
                if (wallW <= 0f)
                {
                    continue;
                }

                for (int x = 0; x < pc.Width; x++)
                {
                    float xr = ((x + 0.5f) / ppu) - 1f;
                    float sag = CuiaGeometry.FaceSagU(xr);
                    float cov = PixelCanvas.Coverage((Mathf.Abs(xr) - wallW) * ppu)
                        * Mathf.Clamp01(((yu - CuiaGeometry.CavityFloorU(xr)) * ppu) + 0.5f)
                        * Mathf.Clamp01((((level + sag) - yu) * ppu) + 0.5f);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c;
                    if (yu >= level - sag)
                    {
                        c = face;
                    }
                    else
                    {
                        int k = Mathf.Clamp(Mathf.FloorToInt((yu - CuiaGeometry.CavityBottomU) + sag), 0, doses - 1);
                        c = Palette.Tint(SkinThumbDoses[k]);
                        c.a = 1f;
                    }

                    pc.Blend(x, y, c, cov);
                }
            }
        }

        // Una pieza de la skin (CuiaSkinParts: x es el centro desde el eje, y el pie), en su color, con las esquinas de la del tablero.
        private static void FillSkinPiece(PixelCanvas pc, Rect pieceU, Color color, int ppu)
        {
            float cx = (CuiaGeometry.WidthU * 0.5f) + pieceU.x;
            float x0 = (cx - (pieceU.width * 0.5f)) * ppu;
            float x1 = (cx + (pieceU.width * 0.5f)) * ppu;
            float y0 = pieceU.y * ppu;
            float y1 = (pieceU.y + pieceU.height) * ppu;
            float r = Mathf.Min(CuiaSkinParts.CornerU * ppu, Mathf.Min(x1 - x0, y1 - y0) * 0.5f);
            color.a = 1f;
            // Un canto de ~1,5 px que la separa del vidrio y del panel: más claro en las piezas oscuras (el cumatê de Santarém
            // sobre el petróleo) y más oscuro en las claras.
            Color edge = ColorMath.RelativeLuminance(color) < 0.2f ? ColorMath.Lighten(color, 0.45f) : ColorMath.Darken(color, 0.40f);
            float o = 1.5f * ppu / SkinThumbPixelsPerU;
            pc.FillRoundedRect(x0 - o, y0 - o, x1 + o, y1 + o, r + o, edge);
            pc.FillRoundedRect(x0, y0, x1, y1, r, color);
        }
    }
}
