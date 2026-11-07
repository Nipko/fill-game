#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// El atlas del rig de Capi al remo del Mapa v2 (D-069, bloque M2-3; propuesta del Mapa v2 §5.1 y §7.2): las piezas por código de
    /// <see cref="ProceduralArt.PaintCapiRigAtlas"/> en una textura de <see cref="ProceduralArt.CapiRigAtlasWidthPx"/> ×
    /// <see cref="ProceduralArt.CapiRigAtlasHeightPx"/> sin mipmaps (512 KiB). Solo existe con el Mapa v2 abierto: <see cref="Build"/>
    /// lo pinta al abrir y <see cref="Release"/> lo suelta al cerrar (§7.2: con el río vivo y los pueblos, unos 5,2 MiB de los +6).
    /// </summary>
    public sealed class MapCapiArt
    {
        private readonly Sprite?[] _sprites = new Sprite?[ProceduralArt.CapiRigPieceCount];
        private Texture2D? _atlas;

        /// <summary>Está pintado.</summary>
        public bool Built => _atlas != null;

        /// <summary>Bytes del atlas en la GPU (sin mipmaps; sonda <c>AF_PERF mem</c> y tests).</summary>
        public long TextureBytes => _atlas != null ? (long)ProceduralArt.CapiRigAtlasWidthPx * ProceduralArt.CapiRigAtlasHeightPx * 4L : 0L;

        /// <summary>Sprite de <paramref name="piece"/>, o null sin el atlas. Sin asignaciones.</summary>
        public Sprite? Piece(CapiRigPiece piece)
        {
            return _atlas != null ? _sprites[(int)piece] : null;
        }

        /// <summary>Pinta el atlas (una vez por apertura; si ya está, nada). Asigna: solo aquí.</summary>
        public void Build()
        {
            if (_atlas != null)
            {
                return;
            }

            var pc = new PixelCanvas(ProceduralArt.CapiRigAtlasWidthPx, ProceduralArt.CapiRigAtlasHeightPx);
            pc.Fill(new Color(0f, 0f, 0f, 0f));
            ProceduralArt.PaintCapiRigAtlas(pc);
            _atlas = pc.ToTexture("map_capi_rig", false, false, false);
            for (int i = 0; i < ProceduralArt.CapiRigPieceCount; i++)
            {
                var piece = (CapiRigPiece)i;
                RectInt c = ProceduralArt.CapiRigCell(piece);
                Sprite sprite = Sprite.Create(_atlas, new Rect(c.x, c.y, c.width, c.height), ProceduralArt.CapiRigPivot(piece), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = "map_capi_" + piece;
                sprite.hideFlags = HideFlags.DontSave;
                _sprites[i] = sprite;
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
