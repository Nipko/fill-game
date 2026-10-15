#nullable enable
using System;
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Tierra del Mapa del río (08 §2.2; propuesta del Mapa §3.3, punto 5, y §11.1): la malla ya triangulada por
    /// <c>tools/river-map/</c> (<see cref="RiverLand"/>), con un color plano por zona [P] hasta que lleguen las texturas en
    /// mosaico del artista (dosel, várzea, sabana, roca y nieve; M5.x). <see cref="SetFlatColor"/> pinta todas las zonas de un
    /// color (la tierra en vidrio de la primera versión de la pantalla). <c>MaskableGraphic</c> sobre el material de UI por
    /// defecto, sin shader propio; coordenadas locales según <see cref="MapSpace"/>. Solo se reconstruye cuando cambian la tierra,
    /// el espacio o los colores, y al reconstruirse no asigna memoria (la tabla de vértices se crea en <see cref="SetLand"/>).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LandGraphic : MaskableGraphic
    {
        /// <summary>Dosel (<c>canopy</c>): <c>selva_media</c> #2C7A4B [P] (paleta de la muestra del Mapa).</summary>
        public static readonly Color CanopyColor = Palette.Hex(0x2C7A4B);

        /// <summary>Várzea (<c>varzea</c>): <c>hoja_media</c> #23824A [P].</summary>
        public static readonly Color VarzeaColor = Palette.Hex(0x23824A);

        /// <summary>Sabana (<c>savanna</c>): <c>selva_luz</c> #63B548 [P].</summary>
        public static readonly Color SavannaColor = Palette.Hex(0x63B548);

        /// <summary>Roca andina (<c>rock</c>): <c>roca_a</c> #B7A07C [P].</summary>
        public static readonly Color RockColor = Palette.Hex(0xB7A07C);

        /// <summary>Nieve (<c>snow</c>): <c>nieve</c> #F4FBFF [P].</summary>
        public static readonly Color SnowColor = Palette.Hex(0xF4FBFF);

        private RiverLand? _land;
        private MapSpace _space = new MapSpace(MapSpace.BasinWidthDp, Vector2.zero);
        private bool _flat;
        private Color _flatColor = Color.white;
        private int[] _remap = Array.Empty<int>();
        private int _vertexCount;

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup()
        {
            raycastTarget = false;
        }

        /// <summary>Tierra que dibuja, o null.</summary>
        public RiverLand? Land => _land;

        /// <summary>Vértices de la última malla construida.</summary>
        public int LastVertexCount => _vertexCount;

        /// <summary>
        /// Color plano de un mosaico [P]: <c>canopy</c>, <c>varzea</c>, <c>savanna</c>, <c>rock</c> y <c>snow</c>, con los tonos
        /// de la muestra del Mapa (<c>generar_mapa.py</c>, D-003); cualquier otro, el dosel.
        /// </summary>
        public static Color TileColor(string tile)
        {
            switch (tile)
            {
                case "varzea":
                    return VarzeaColor;
                case "savanna":
                    return SavannaColor;
                case "rock":
                    return RockColor;
                case "snow":
                    return SnowColor;
                default:
                    return CanopyColor;
            }
        }

        /// <summary>Tierra y espacio del Mapa. Crea la tabla de vértices por zona (una vez por tierra).</summary>
        public void SetLand(RiverLand? land, MapSpace space)
        {
            if (ReferenceEquals(land, _land) && space.Equals(_space))
            {
                return;
            }

            if (!ReferenceEquals(land, _land))
            {
                _remap = land != null ? new int[land.VertexCount] : Array.Empty<int>();
            }

            _land = land;
            _space = space;
            SetVerticesDirty();
        }

        /// <summary>Pinta todas las zonas de <paramref name="flat"/> (null: cada zona con el color de su mosaico).</summary>
        public void SetFlatColor(Color? flat)
        {
            bool on = flat.HasValue;
            Color c = flat ?? Color.white;
            if (on == _flat && (!on || c == _flatColor))
            {
                return;
            }

            _flat = on;
            _flatColor = c;
            SetVerticesDirty();
        }

        /// <summary>Color con el que sale la zona <paramref name="zone"/> (sin la opacidad del gráfico).</summary>
        public Color ZoneColor(int zone)
        {
            if (_flat || _land == null || zone < 0 || zone >= _land.Zones.Count)
            {
                return _flatColor;
            }

            return TileColor(_land.Zones[zone].Tile);
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _vertexCount = 0;
            RiverLand? land = _land;
            if (land == null)
            {
                return;
            }

            float[] xy = land.Xy;
            int[] tris = land.Triangles;
            for (int z = 0; z < land.Zones.Count; z++)
            {
                RiverLandZone zone = land.Zones[z];
                if (zone.Count == 0)
                {
                    continue;
                }

                // Cada zona lleva sus propios vértices (el color es por vértice); dentro de la zona se comparten.
                for (int i = 0; i < _remap.Length; i++)
                {
                    _remap[i] = -1;
                }

                Color c = ZoneColor(z);
                c.a *= color.a;
                Color32 c32 = c;
                int end = (zone.First + zone.Count) * 3;
                for (int t = zone.First * 3; t < end; t += 3)
                {
                    int a = Vertex(vh, xy, tris[t], c32);
                    int b = Vertex(vh, xy, tris[t + 1], c32);
                    int d = Vertex(vh, xy, tris[t + 2], c32);
                    vh.AddTriangle(a, b, d);
                }
            }

            _vertexCount = vh.currentVertCount;
        }

        private int Vertex(VertexHelper vh, float[] xy, int index, Color32 c)
        {
            int mapped = _remap[index];
            if (mapped >= 0)
            {
                return mapped;
            }

            mapped = vh.currentVertCount;
            vh.AddVert(_space.ToLocal(xy[2 * index], xy[(2 * index) + 1]), c, Vector4.zero);
            _remap[index] = mapped;
            return mapped;
        }
    }
}
