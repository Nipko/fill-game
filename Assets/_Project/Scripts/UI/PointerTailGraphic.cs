#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Cola de un bocadillo: un triángulo que llena su rectángulo con la base arriba y la punta abajo, en el centro, del color
    /// del gráfico (sin textura). La usa la tarjeta de TiTino para señalar el botón de Marea en el tooltip del nivel 3 (tarea
    /// TU1a; 04 §2.1). Gráfico provisional por código; no recibe toques (<see cref="Graphic.raycastTarget"/> = false al crearla).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class PointerTailGraphic : MaskableGraphic
    {
        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Color32 c = color;
            vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector4.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), c, Vector4.zero);
            vh.AddVert(new Vector3(r.center.x, r.yMin), c, Vector4.zero);
            vh.AddTriangle(0, 1, 2);
        }
    }
}
