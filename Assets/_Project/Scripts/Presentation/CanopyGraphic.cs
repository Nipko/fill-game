#nullable enable
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// La selva en capas del Mapa v2 (D-069, M2-1; propuesta del Mapa v2 §3.1, capa 5, y §7.2): los puntos de <c>veg</c>
    /// (<see cref="RiverVegetation"/>) dibujados como quads del atlas de <see cref="MapLiveArt"/>, en una sola llamada de dibujo.
    /// <list type="bullet">
    /// <item><description><b>Solo lo visible</b>: la región que se le da (<see cref="SetRegion"/>: la vista con un margen), con un
    /// tope de <see cref="MaxQuads"/> quads; la propuesta pide ≤ 600 copas en pantalla (≤ 2.400 vértices) y la región con margen es
    /// unas 2,2 veces la vista.</description></item>
    /// <item><description><b>Por zoom</b>: con poco zoom, solo la capa gruesa (<see cref="RiverVegetation.Lod"/> 0, una celda de
    /// cada cuatro); cada punto, un racimo de 1 a <see cref="MaxCluster"/> copas según el espacio que le toca en pantalla; las copas
    /// miden de <see cref="MinCopaDp"/> a <see cref="MaxCopaDp"/> dp.</description></item>
    /// <item><description><b>Fuera del agua y de las orillas</b>: un punto no sale si su distancia al eje del río más cercano, en dp
    /// de esta vista, no deja sitio al río (<see cref="SetRiverClearance"/>) más la copa; las palmeras pueden quedar más cerca
    /// (<see cref="PalmClearDp"/>).</description></item>
    /// <item><description><b>Más oscuro lejos del río</b>: un tinte por vértice que baja hasta <see cref="FarTint"/>.</description></item>
    /// </list>
    /// Solo se reconstruye cuando cambian la región, el espacio o los datos; sin animación.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CanopyGraphic : MaskableGraphic
    {
        /// <summary>Tope de quads de la región (con su margen).</summary>
        public const int MaxQuads = 1400;

        /// <summary>Copas por punto como mucho.</summary>
        public const int MaxCluster = 4;

        /// <summary>Copa más pequeña (dp).</summary>
        public const float MinCopaDp = 9f;

        /// <summary>Copa más grande (dp).</summary>
        public const float MaxCopaDp = 48f;

        /// <summary>Con menos de estos dp por celda, solo la capa gruesa (lod 0) [P].</summary>
        public const float FineLodCellDp = 22f;

        /// <summary>Las palmeras pueden quedar a este tanto del borde del agua (dp) [P]; las copas, a <see cref="CopaClearDp"/>.</summary>
        public const float PalmClearDp = 3f;

        /// <summary>Hueco entre el borde del agua (con su orilla) y una copa (dp) [P].</summary>
        public const float CopaClearDp = 12f;

        /// <summary>Tinte de las copas más lejanas del río [P].</summary>
        public const float FarTint = 0.80f;

        /// <summary>Desde esta distancia al río (dp) las copas se oscurecen, hasta <see cref="FarTint"/> a 4 veces ella [P].</summary>
        public const float FarFromDp = 110f;

        // Racimo: desplazamientos (en fracción del espacio del punto) y tamaño relativo de cada copa.
        private static readonly Vector3[] Cluster =
        {
            new Vector3(0f, 0f, 1f), new Vector3(0.30f, 0.16f, 0.86f), new Vector3(-0.28f, 0.20f, 0.9f), new Vector3(0.04f, -0.30f, 0.8f),
        };

        private static readonly float[] ClusterScale = { 1.05f, 0.80f, 0.68f, 0.60f };
        private static readonly float[] SizeScale = { 0.86f, 1f, 1.14f };

        private RiverVegetation? _veg;
        private MapSpace _space = new MapSpace(MapSpace.BasinWidthDp, Vector2.zero);
        private Rect _region;
        private Texture? _atlas;
        private float _riverHalfDp = 13f;
        private int _quads;
        private int _points;

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup()
        {
            raycastTarget = false;
        }

        /// <inheritdoc />
        public override Texture mainTexture => _atlas != null ? _atlas : s_WhiteTexture;

        /// <summary>Quads de la última malla.</summary>
        public int LastQuadCount => _quads;

        /// <summary>Puntos de <c>veg</c> que salieron en la última malla.</summary>
        public int LastPointCount => _points;

        /// <summary>Región construida (unidades del marco).</summary>
        public Rect Region => _region;

        /// <summary>Espacio con el que se dibuja.</summary>
        public MapSpace Space => _space;

        /// <summary>Vegetación, espacio y atlas (null: nada que dibujar).</summary>
        public void SetData(RiverVegetation? veg, MapSpace space, Texture? atlas)
        {
            if (ReferenceEquals(veg, _veg) && space.Equals(_space) && atlas == _atlas)
            {
                return;
            }

            _veg = veg;
            _space = space;
            _atlas = atlas;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        /// <summary>Medio ancho del río más ancho con su orilla en esta vista (dp): lo que la selva deja libre alrededor del eje.</summary>
        public void SetRiverClearance(float halfDp)
        {
            if (halfDp != _riverHalfDp)
            {
                _riverHalfDp = halfDp;
                SetVerticesDirty();
            }
        }

        /// <summary>Región que se dibuja (unidades del marco, y hacia abajo). Sin efecto si no cambia.</summary>
        public void SetRegion(Rect region)
        {
            if (region != _region)
            {
                _region = region;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Cuántas copas lleva cada punto y con qué capa a <paramref name="dpPerKm"/> dp por km con celdas de
        /// <paramref name="cellKm"/> km: (capa máxima, copas por punto, espacio de cada punto en dp). Público para los tests.
        /// </summary>
        public static void Density(float dpPerKm, float cellKm, out int lodMax, out int cluster, out float spacingDp)
        {
            float cellDp = cellKm * dpPerKm;
            lodMax = cellDp < FineLodCellDp ? 0 : 1;
            spacingDp = lodMax == 0 ? 2f * cellDp : cellDp;
            cluster = spacingDp < 24f ? 1 : spacingDp < 36f ? 2 : spacingDp < 52f ? 3 : MaxCluster;
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _quads = 0;
            _points = 0;
            RiverVegetation? veg = _veg;
            if (veg == null || veg.Count == 0 || _space.DpPerUnit <= 0f || _region.width <= 0f || _region.height <= 0f)
            {
                return;
            }

            float dpPerKm = _space.DpPerUnit / veg.KmPerUnit;
            Density(dpPerKm, veg.CellKm, out int lodMax, out int cluster, out float spacingDp);
            float[] xy = veg.Xy;
            float alpha = color.a;
            for (int i = 0; i < veg.Count && _quads < MaxQuads; i++)
            {
                float x = xy[2 * i];
                float y = xy[(2 * i) + 1];
                if (!_region.Contains(new Vector2(x, y)) || veg.Lod(i) > lodMax)
                {
                    continue;
                }

                RiverVegKind kind = veg.Kind(i);
                float distDp = veg.DistanceKm(i) * dpPerKm;
                int copies = kind == RiverVegKind.Palm ? 1 : cluster;
                float baseDp = spacingDp * ClusterScale[copies - 1] * SizeScale[veg.Size(i)];
                float clear = _riverHalfDp + (kind == RiverVegKind.Palm ? PalmClearDp : CopaClearDp);
                if (veg.DistanceKm(i) < RiverVegetation.MaxDistanceKm && distDp - (0.5f * baseDp) < clear)
                {
                    continue; // bajo el agua o la orilla a este zoom
                }

                _points++;
                float far = Mathf.Clamp01((distDp - FarFromDp) / (3f * FarFromDp));
                float tint = Mathf.Lerp(1f, FarTint, far) * (0.94f + (0.02f * veg.Tone(i)));
                tint = Mathf.Min(1f, tint);
                Vector2 center = _space.ToLocal(x, y);
                for (int k = 0; k < copies && _quads < MaxQuads; k++)
                {
                    Vector3 off = Cluster[(k + i) % Cluster.Length];
                    float size = Mathf.Clamp(baseDp * (k == 0 ? 1f : off.z), MinCopaDp, MaxCopaDp);
                    Vector2 c = center + new Vector2(off.x * spacingDp, off.y * spacingDp);
                    int piece = PieceOf(kind, veg.Tone(i), k, i);
                    float ps = kind == RiverVegKind.Palm ? size * 0.9f : size;
                    Quad(vh, c, ps, piece, new Color(tint, tint, tint, alpha));
                }
            }
        }

        private static int PieceOf(RiverVegKind kind, int tone, int copy, int index)
        {
            switch (kind)
            {
                case RiverVegKind.Palm:
                    return (int)MapLivePiece.Palm;
                case RiverVegKind.Flower when copy == 0:
                    return (index & 1) == 0 ? (int)MapLivePiece.FlowerYellow : (int)MapLivePiece.FlowerPink;
                default:
                    return (tone + copy) % MapLiveArt.CanopyTones;
            }
        }

        private void Quad(VertexHelper vh, Vector2 center, float size, int piece, Color tint)
        {
            Rect uv = MapLiveArt.Uv(piece);
            float h = size * 0.5f;
            int start = vh.currentVertCount;
            Color32 c = tint;
            vh.AddVert(new Vector3(center.x - h, center.y - h), c, new Vector2(uv.xMin, uv.yMin));
            vh.AddVert(new Vector3(center.x - h, center.y + h), c, new Vector2(uv.xMin, uv.yMax));
            vh.AddVert(new Vector3(center.x + h, center.y + h), c, new Vector2(uv.xMax, uv.yMax));
            vh.AddVert(new Vector3(center.x + h, center.y - h), c, new Vector2(uv.xMax, uv.yMin));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
            _quads++;
        }
    }
}
