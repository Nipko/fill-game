#nullable enable
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// El atlas de la tripulación del Mapa v2 (D-069, bloque M2-4; propuesta del Mapa v2 §6.2 y §7.2): las piezas por código de
    /// <see cref="ProceduralArt.PaintCrewAtlas"/> en una textura de <see cref="ProceduralArt.CrewAtlasWidthPx"/> ×
    /// <see cref="ProceduralArt.CrewAtlasHeightPx"/> sin mipmaps (128 KiB). Solo existe con el Mapa v2 abierto y la tripulación
    /// encendida (<c>ui_map_crew</c>), o mientras el banner de mundo enseña su plano final: <see cref="Build"/> lo pinta al abrir (con la
    /// paleta del miquito del tramo) y <see cref="Release"/> lo suelta al cerrar.
    /// </summary>
    public sealed class MapCrewArt
    {
        private readonly Sprite?[] _sprites = new Sprite?[ProceduralArt.CrewPieceCount];
        private Texture2D? _atlas;
        private FaunaPalette? _monkey;

        /// <summary>Está pintado.</summary>
        public bool Built => _atlas != null;

        /// <summary>Bytes del atlas en la GPU (sin mipmaps; sonda <c>AF_PERF mem</c> y tests).</summary>
        public long TextureBytes => _atlas != null ? (long)ProceduralArt.CrewAtlasWidthPx * ProceduralArt.CrewAtlasHeightPx * 4L : 0L;

        /// <summary>Lleva el miquito pintado (con una paleta).</summary>
        public bool HasMonkey => _atlas != null && _monkey != null;

        /// <summary>Sprite de <paramref name="piece"/>, o null sin el atlas (o sin miquito, en sus piezas). Sin asignaciones.</summary>
        public Sprite? Piece(CrewPiece piece)
        {
            if (_atlas == null)
            {
                return null;
            }

            bool monkeyPiece = piece == CrewPiece.MonkeyBody || piece == CrewPiece.MonkeyHead || piece == CrewPiece.MonkeyArm;
            return monkeyPiece && _monkey == null ? null : _sprites[(int)piece];
        }

        /// <summary>
        /// Pinta el atlas con el miquito de <paramref name="monkey"/> (null: sin miquito). Si ya está con la misma paleta, nada; con
        /// otra, lo repinta. Asigna: solo aquí.
        /// </summary>
        public void Build(FaunaPalette? monkey)
        {
            if (_atlas != null && ReferenceEquals(monkey, _monkey))
            {
                return;
            }

            Release();
            _monkey = monkey;
            var pc = new PixelCanvas(ProceduralArt.CrewAtlasWidthPx, ProceduralArt.CrewAtlasHeightPx);
            pc.Fill(new Color(0f, 0f, 0f, 0f));
            ProceduralArt.PaintCrewAtlas(pc, monkey);
            _atlas = pc.ToTexture("map_crew", false, false, false);
            for (int i = 0; i < ProceduralArt.CrewPieceCount; i++)
            {
                var piece = (CrewPiece)i;
                RectInt c = ProceduralArt.CrewCell(piece);
                Sprite sprite = Sprite.Create(_atlas, new Rect(c.x, c.y, c.width, c.height), ProceduralArt.CrewPivot(piece), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = "map_crew_" + piece;
                sprite.hideFlags = HideFlags.DontSave;
                _sprites[i] = sprite;
            }
        }

        /// <summary>Suelta el atlas y sus sprites (al cerrar el Mapa o el banner). Idempotente.</summary>
        public void Release()
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                Destroy(_sprites[i]);
                _sprites[i] = null;
            }

            Destroy(_atlas);
            _atlas = null;
            _monkey = null;
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
