#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.Profiling;

namespace AF.UI
{
    /// <summary>
    /// Lo que el Mapa tiene vivo, para la sonda <c>AF_PERF mem</c> (observación 5 de la prueba de la 1.1 del 2026-11-07;
    /// <see cref="DevProbe.FormatMapGfx"/>): las texturas propias del Mapa v2, los lienzos con sus lotes, los
    /// <see cref="CanvasRenderer"/> activos y los vértices de la geometría por código. Con el Mapa cerrado vuelve todo a cero (los
    /// lienzos, a lo que pesan vacíos): lo comprueban <c>MapMemoryCycleTests</c> en el Editor y la línea <c>map_close</c> en el
    /// teléfono. Asigna (busca componentes): solo en los momentos de la sonda.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Lee lo que el Mapa tiene vivo ahora; <paramref name="openCount"/> es la vuelta (aperturas en la sesión).</summary>
        public MapGfxSample SampleGfx(int openCount)
        {
            long tex = _liveArt.TextureBytes + _townArt.TextureBytes + _capiArt.TextureBytes + _crewArt.TextureBytes;
            Canvas root = _canvas.Canvas;
            long canvasBytes = 0;
            int canvases = 0;
            Canvas[] all = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < all.Length; i++)
            {
                canvasBytes += Profiler.GetRuntimeMemorySizeLong(all[i]);
                canvases += all[i].isActiveAndEnabled ? 1 : 0;
            }

            // GetComponentsInChildren(false) mira el activeSelf de cada uno, no el de la raíz: se filtra por activeInHierarchy.
            int renderers = 0;
            CanvasRenderer[] crs = root.GetComponentsInChildren<CanvasRenderer>(false);
            for (int i = 0; i < crs.Length; i++)
            {
                renderers += crs[i].gameObject.activeInHierarchy ? 1 : 0;
            }

            int verts = 0;
            RiverRibbonGraphic[] ribbons = root.GetComponentsInChildren<RiverRibbonGraphic>(false);
            for (int i = 0; i < ribbons.Length; i++)
            {
                verts += ribbons[i].gameObject.activeInHierarchy ? ribbons[i].LastVertexCount : 0;
            }

            LandGraphic[] lands = root.GetComponentsInChildren<LandGraphic>(false);
            for (int i = 0; i < lands.Length; i++)
            {
                verts += lands[i].gameObject.activeInHierarchy ? lands[i].LastVertexCount : 0;
            }

            CanopyGraphic[] canopies = root.GetComponentsInChildren<CanopyGraphic>(false);
            for (int i = 0; i < canopies.Length; i++)
            {
                verts += canopies[i].gameObject.activeInHierarchy ? canopies[i].LastQuadCount * 4 : 0;
            }

            return new MapGfxSample(openCount, tex, canvasBytes, canvases, renderers, verts);
        }

        /// <summary>Los miembros <c>map_*</c> de la sonda (con coma delante) para la vuelta <paramref name="openCount"/>.</summary>
        internal string GfxMemJson(int openCount)
        {
            return DevProbe.FormatMapGfx(SampleGfx(openCount));
        }
    }
}
