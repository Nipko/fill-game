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
    /// Una textura de 64 × 179 px por skin (cuia de 4 a <see cref="SkinThumbPixelsPerU"/> px por u; unos 46 KB), hecha la primera
    /// vez que se pide y guardada: la sueltan <see cref="Dispose"/> con el resto del arte (memoria, observación 6).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Densidad de la miniatura (px por u): a 56 dp de alto y 3 px por dp es casi 1:1.</summary>
        public const int SkinThumbPixelsPerU = 32;

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
            const int cap = SkinThumbCapacity;
            const int ppu = SkinThumbPixelsPerU;
            PixelCanvas pc = NewCuiaCanvas(cap, ppu);
            PixelCanvas layer = NewCuiaCanvas(cap, ppu);
            Color glass = skin.GlassMultiplier;
            PaintCuiaBack(layer, cap, ppu);
            CompositeLayer(pc, layer, glass);
            PaintThumbLiquid(pc, cap, ppu);
            ClearLayer(layer);
            PaintCuiaFront(layer, cap, ppu);
            CompositeLayer(pc, layer, glass);
            ClearLayer(layer);
            PaintCuiaMarks(layer, cap, ppu);
            CompositeLayer(pc, layer, Color.white);
            layer.Release();
            if (!skin.IsBase)
            {
                // El orden de CuiaView.BuildSkinParts: pie, borde, banda (o sus dos mitades) y asa.
                FillSkinPiece(pc, CuiaSkinParts.Foot(cap), skin.Foot, ppu);
                FillSkinPiece(pc, CuiaSkinParts.Rim(cap), skin.Rim, ppu);
                if (skin.HasBand && skin.BandBicolor)
                {
                    FillSkinPiece(pc, CuiaSkinParts.Band(cap, -1), skin.Band, ppu);
                    FillSkinPiece(pc, CuiaSkinParts.Band(cap, 1), skin.BandB, ppu);
                }
                else if (skin.HasBand)
                {
                    FillSkinPiece(pc, CuiaSkinParts.Band(cap, 0), skin.Band, ppu);
                }

                if (skin.HasHandle)
                {
                    FillSkinPiece(pc, CuiaSkinParts.Handle(cap), skin.Handle, ppu);
                }
            }

            return MakeSprite(pc, SkinThumbPrefix + skin.Id, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Compone la capa sobre el lienzo (encima), con su color multiplicado por tint (el Image.color del tablero).
        private static void CompositeLayer(PixelCanvas into, PixelCanvas layer, Color tint)
        {
            for (int y = 0; y < into.Height; y++)
            {
                for (int x = 0; x < into.Width; x++)
                {
                    Color c = layer.Get(x, y);
                    if (c.a <= 0f)
                    {
                        continue;
                    }

                    into.Blend(x, y, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a), 1f);
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
            pc.FillRoundedRect(x0, y0, x1, y1, r, color);
        }
    }
}
