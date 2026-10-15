#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Atenúa con la profundidad el reflejo volteado del PNG del héroe del fondo de parada (tarea M1.4; 07 §2.1; VA-21 de
    /// direccion-visual-v2 §9), igual que el de las siluetas de <see cref="StopShapes"/>: la opacidad del <see cref="Image"/>
    /// (la de su color, <see cref="StopBackdrop.ReflectionAlpha"/>) se queda entera en la línea del agua y baja a 0 a
    /// <see cref="FadeDp"/> dp de ella. Así el reflejo de un héroe pintado de mucho contraste no llega al borde del panel con
    /// fuerza (VA-21: en los 16 dp sobre el panel, nada con ΔL* &gt; 30).
    /// <para>Va en la imagen del reflejo, con escala y = −1 y el pivote en el horizonte del PNG: en su espacio local, y = 0 es la
    /// línea del agua y la y positiva (lo que el PNG tiene sobre su horizonte) queda, volteada, bajo ella; la profundidad es esa
    /// y. Parte el cuadrilátero de la imagen <c>Simple</c> en franjas horizontales (en la línea del agua y a
    /// <see cref="FadeDp"/>) con la opacidad por vértice, interpolando posición, UV y color. Solo trabaja al reconstruir la
    /// malla, nunca por fotograma, y no asigna memoria.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StopReflectionFade : BaseMeshEffect
    {
        private float _fadeDp;

        /// <summary>Profundidad bajo la línea del agua a la que el reflejo ya no se ve (dp del lienzo; ≤ 0 = sin atenuar).</summary>
        public float FadeDp
        {
            get => _fadeDp;
            set
            {
                if (Mathf.Approximately(value, _fadeDp))
                {
                    return;
                }

                _fadeDp = value;
                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        /// <summary>
        /// Factor de opacidad del reflejo a <paramref name="depth"/> dp bajo la línea del agua: 1 en ella (y por encima) y 0 a
        /// <paramref name="fadeDp"/> dp y más abajo, lineal entre medias (1 siempre con <paramref name="fadeDp"/> ≤ 0).
        /// </summary>
        public static float DepthFactor(float depth, float fadeDp)
        {
            if (fadeDp <= 0f || depth <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (depth / fadeDp));
        }

        /// <inheritdoc />
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || _fadeDp <= 0f || vh.currentVertCount != 4)
            {
                return;
            }

            UIVertex v0 = default;
            UIVertex v1 = default;
            UIVertex v2 = default;
            UIVertex v3 = default;
            vh.PopulateUIVertex(ref v0, 0);
            vh.PopulateUIVertex(ref v1, 1);
            vh.PopulateUIVertex(ref v2, 2);
            vh.PopulateUIVertex(ref v3, 3);
            float yMin = Mathf.Min(Mathf.Min(v0.position.y, v1.position.y), Mathf.Min(v2.position.y, v3.position.y));
            float yMax = Mathf.Max(Mathf.Max(v0.position.y, v1.position.y), Mathf.Max(v2.position.y, v3.position.y));
            if (yMax - yMin <= 0.0001f)
            {
                return;
            }

            // Las cuatro esquinas, sea cual sea el orden en que las dio la imagen.
            float yMid = (yMin + yMax) * 0.5f;
            UIVertex bl = default;
            UIVertex br = default;
            UIVertex tl = default;
            UIVertex tr = default;
            int bottoms = 0;
            int tops = 0;
            Sort(v0, yMid, ref bl, ref br, ref tl, ref tr, ref bottoms, ref tops);
            Sort(v1, yMid, ref bl, ref br, ref tl, ref tr, ref bottoms, ref tops);
            Sort(v2, yMid, ref bl, ref br, ref tl, ref tr, ref bottoms, ref tops);
            Sort(v3, yMid, ref bl, ref br, ref tl, ref tr, ref bottoms, ref tops);
            if (bottoms != 2 || tops != 2)
            {
                return;
            }

            vh.Clear();
            int rows = 0;
            AddRow(vh, bl, br, tl, tr, yMin, yMax, yMin, ref rows);
            if (yMin < 0f && 0f < yMax)
            {
                AddRow(vh, bl, br, tl, tr, yMin, yMax, 0f, ref rows);
            }

            if (yMin < _fadeDp && _fadeDp < yMax && _fadeDp > 0f)
            {
                AddRow(vh, bl, br, tl, tr, yMin, yMax, _fadeDp, ref rows);
            }

            AddRow(vh, bl, br, tl, tr, yMin, yMax, yMax, ref rows);
            for (int r = 0; r + 1 < rows; r++)
            {
                int i = 2 * r;
                vh.AddTriangle(i, i + 2, i + 3);
                vh.AddTriangle(i, i + 3, i + 1);
            }
        }

        private static void Sort(UIVertex v, float yMid, ref UIVertex bl, ref UIVertex br, ref UIVertex tl, ref UIVertex tr,
            ref int bottoms, ref int tops)
        {
            if (v.position.y < yMid)
            {
                if (bottoms == 0)
                {
                    bl = v;
                }
                else if (v.position.x < bl.position.x)
                {
                    br = bl;
                    bl = v;
                }
                else
                {
                    br = v;
                }

                bottoms++;
                return;
            }

            if (tops == 0)
            {
                tl = v;
            }
            else if (v.position.x < tl.position.x)
            {
                tr = tl;
                tl = v;
            }
            else
            {
                tr = v;
            }

            tops++;
        }

        // Una fila de dos vértices (izquierda y derecha) a la altura y, interpolada entre los bordes de abajo y de arriba, con la
        // opacidad multiplicada por el factor de su profundidad.
        private void AddRow(VertexHelper vh, UIVertex bl, UIVertex br, UIVertex tl, UIVertex tr, float yMin, float yMax, float y,
            ref int rows)
        {
            float t = (y - yMin) / (yMax - yMin);
            float f = DepthFactor(y, _fadeDp);
            vh.AddVert(Lerp(bl, tl, t, f));
            vh.AddVert(Lerp(br, tr, t, f));
            rows++;
        }

        private static UIVertex Lerp(UIVertex a, UIVertex b, float t, float alphaFactor)
        {
            UIVertex v = a;
            v.position = Vector3.LerpUnclamped(a.position, b.position, t);
            v.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t);
            Color32 c = Color32.Lerp(a.color, b.color, t);
            c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * alphaFactor), 0, 255);
            v.color = c;
            return v;
        }
    }
}
