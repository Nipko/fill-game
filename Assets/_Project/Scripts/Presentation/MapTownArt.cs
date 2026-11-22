#nullable enable
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// El atlas de los pueblos de paso del Mapa v2 (D-069, bloque M2-2; propuesta del Mapa v2 §4.2 y §7.2): las 5 viñetas por código
    /// de <see cref="ProceduralArt.PaintTownAtlas"/> en sus 3 juegos de colores, en una textura de <see cref="AtlasWidthPx"/> ×
    /// <see cref="AtlasHeightPx"/> con mipmaps (las viñetas se pintan de ~36 a ~80 dp). Solo existe con el Mapa v2 abierto y
    /// <c>ui_map_towns</c>: <see cref="Build"/> lo pinta al abrir y <see cref="Release"/> lo suelta al cerrar (§7.2: 2 MiB, más su
    /// cadena de mipmaps, dentro de los +6 MiB solo con el Mapa abierto).
    /// </summary>
    public sealed class MapTownArt
    {
        /// <summary>Ancho del atlas (px; §7.2).</summary>
        public const int AtlasWidthPx = 1024;

        /// <summary>Alto del atlas (px; §7.2).</summary>
        public const int AtlasHeightPx = 512;

        private const int Kinds = 5;

        private readonly Sprite?[] _sprites = new Sprite?[Kinds * ProceduralArt.TownVariants];
        private Texture2D? _atlas;

        /// <summary>Está pintado.</summary>
        public bool Built => _atlas != null;

        /// <summary>El atlas (null antes de <see cref="Build"/> o tras <see cref="Release"/>).</summary>
        public Texture2D? Atlas => _atlas;

        /// <summary>Bytes del atlas en la GPU, con sus mipmaps (sonda <c>AF_PERF mem</c> y tests).</summary>
        public long TextureBytes => _atlas != null ? (long)AtlasWidthPx * AtlasHeightPx * 4L * 4L / 3L : 0L;

        /// <summary>
        /// Sprite de <paramref name="vignette"/> en el juego <paramref name="variant"/> (0-2), o null sin el atlas o sin viñeta.
        /// Sin asignaciones.
        /// </summary>
        public Sprite? Piece(RiverTownVignette vignette, int variant)
        {
            if (_atlas == null || vignette == RiverTownVignette.None)
            {
                return null;
            }

            int k = (((int)vignette - 1) * ProceduralArt.TownVariants) + Mathf.Clamp(variant, 0, ProceduralArt.TownVariants - 1);
            return k >= 0 && k < _sprites.Length ? _sprites[k] : null;
        }

        /// <summary>Pinta el atlas (una vez por apertura; si ya está, nada). Asigna: solo aquí.</summary>
        public void Build()
        {
            if (_atlas != null)
            {
                return;
            }

            var pc = new PixelCanvas(AtlasWidthPx, AtlasHeightPx);
            pc.Fill(new Color(0f, 0f, 0f, 0f));
            ProceduralArt.PaintTownAtlas(pc);
            _atlas = pc.ToTexture("map_town_atlas", false, false, true);
            for (int v = 1; v <= Kinds; v++)
            {
                for (int variant = 0; variant < ProceduralArt.TownVariants; variant++)
                {
                    Vector2Int o = ProceduralArt.TownCell((RiverTownVignette)v, variant);
                    var rect = new Rect(o.x + 1, o.y + 1, ProceduralArt.TownCellWidthPx - 2, ProceduralArt.TownCellHeightPx - 2);
                    Sprite sprite = Sprite.Create(_atlas, rect, new Vector2(0.5f, 0.2f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = "map_town_" + v + "_" + variant;
                    sprite.hideFlags = HideFlags.DontSave;
                    _sprites[((v - 1) * ProceduralArt.TownVariants) + variant] = sprite;
                }
            }
        }

        /// <summary>Suelta el atlas y sus sprites (al cerrar el Mapa). Idempotente.</summary>
        public void Release()
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                Destroy(_sprites[i]);
                _sprites[i] = null;
            }

            Destroy(_atlas);
            _atlas = null;
        }

        private static void Destroy(Object? obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
