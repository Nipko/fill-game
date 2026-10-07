#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Gráfico invisible que solo recibe raycasts de uGUI (sin geometría ni coste de dibujo): áreas táctiles de las
    /// cuias (02 §3.3) y capturadores de toques a pantalla completa.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HitArea : Graphic
    {
        /// <inheritdoc />
        public override void SetMaterialDirty()
        {
        }

        /// <inheritdoc />
        public override void SetVerticesDirty()
        {
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
