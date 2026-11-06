#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Geometría de la pantalla para todos los lienzos del juego (añadido de D-059 del 2026-11-06, borde a borde y pantallas
    /// grandes): la escala del lienzo (<see cref="GameScreen.CanvasScale(int, int, float)"/>, 02 §5), el lienzo entero, que llega
    /// al borde de la pantalla, y la <b>columna</b> vertical donde va la interfaz.
    /// <para>La columna mide el ancho del lienzo dentro de la zona segura de los lados (recortes, barra de navegación de lado en
    /// apaisado) y, como mucho, <see cref="ColumnMaxAspect"/> × el alto del lienzo: en un teléfono en vertical y en una tableta
    /// 16:10 en vertical es el lienzo entero, como siempre; en una ventana más ancha (apaisado, plegable abierto, pantalla
    /// dividida, ventana libre) es una columna centrada y los fondos llenan los lados (<see cref="ScreenColumn"/>). Nada se
    /// estira ni se corta: el juego es vertical (02 §5, «Orientación: vertical bloqueada»).</para>
    /// <para>Arriba y abajo, cada pantalla suma sus márgenes de siempre a <see cref="InsetTop"/> y <see cref="InsetBottom"/>
    /// (<see cref="GameScreen.MinSafeTop"/> y <see cref="GameScreen.MinSafeBottom"/>).</para>
    /// </summary>
    public readonly struct ScreenFrame
    {
        /// <summary>
        /// Ancho máximo de la columna entre el alto del lienzo (10:16, la tableta 16:10 en vertical de la matriz de 08 §11) [P]: a
        /// 640 dp de alto, 400 dp. Los teléfonos en vertical (9:16 a 9:21) nunca llegan: su columna es el lienzo entero.
        /// </summary>
        public const float ColumnMaxAspect = 0.625f;

        public ScreenFrame(float scale, float canvasWidth, float canvasHeight, float columnX, float columnWidth,
            float insetTop, float insetBottom, float insetLeft, float insetRight)
        {
            Scale = scale;
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
            ColumnX = columnX;
            ColumnWidth = columnWidth;
            InsetTop = insetTop;
            InsetBottom = insetBottom;
            InsetLeft = insetLeft;
            InsetRight = insetRight;
        }

        /// <summary>Píxeles de la pantalla por dp del lienzo (el <c>scaleFactor</c> del <c>CanvasScaler</c>).</summary>
        public float Scale { get; }

        /// <summary>Ancho del lienzo entero (dp): de borde a borde de la pantalla.</summary>
        public float CanvasWidth { get; }

        /// <summary>Alto del lienzo (dp): de borde a borde, también bajo las barras del sistema.</summary>
        public float CanvasHeight { get; }

        /// <summary>Borde izquierdo de la columna en el lienzo (dp).</summary>
        public float ColumnX { get; }

        /// <summary>Ancho de la columna (dp): el ancho de maquetación de cada pantalla.</summary>
        public float ColumnWidth { get; }

        /// <summary>Lo que tapa el sistema arriba (barra de estado, recorte; dp), sin el mínimo de 02 §5.</summary>
        public float InsetTop { get; }

        /// <summary>Lo que tapa el sistema abajo (barra de navegación o de gestos; dp), sin el mínimo de 02 §5.</summary>
        public float InsetBottom { get; }

        /// <summary>Lo que tapa el sistema a la izquierda (recorte o barra de navegación en apaisado; dp).</summary>
        public float InsetLeft { get; }

        /// <summary>Lo que tapa el sistema a la derecha (dp).</summary>
        public float InsetRight { get; }

        /// <summary>La columna es el lienzo entero (teléfono o tableta en vertical sin recortes de lado): nada cambia.</summary>
        public bool IsFullWidth => ColumnX <= 0.001f && Mathf.Abs(ColumnWidth - CanvasWidth) <= 0.001f;

        /// <summary>Lo que el lienzo sobra a la derecha de la columna (dp).</summary>
        public float RightBand => Mathf.Max(0f, CanvasWidth - ColumnX - ColumnWidth);

        /// <summary>La geometría de la pantalla de ahora (<see cref="GameScreen.ReadScreen"/> y <see cref="GameScreen.ReadPxPerDp"/>).</summary>
        public static ScreenFrame Current
        {
            get
            {
                GameScreen.ReadScreen(out int width, out int height, out Rect safe);
                return Compute(width, height, safe, GameScreen.ReadPxPerDp());
            }
        }

        /// <summary>
        /// La geometría de una pantalla de <paramref name="width"/> × <paramref name="height"/> px con la zona segura
        /// <paramref name="safe"/> (px, origen abajo a la izquierda, como <c>Screen.safeArea</c>) y <paramref name="pxPerDp"/> px
        /// por dp físico (≤ 0 = no se sabe). La escala sale del ancho de la zona segura (lo de los lados no se maqueta) y del
        /// alto entero (la regla del alto mínimo de 02 §5).
        /// </summary>
        public static ScreenFrame Compute(int width, int height, Rect safe, float pxPerDp)
        {
            int w = Mathf.Max(1, width);
            int h = Mathf.Max(1, height);
            float leftPx = Mathf.Clamp(safe.xMin, 0f, w);
            float rightPx = Mathf.Clamp(w - safe.xMax, 0f, w - leftPx);
            float bottomPx = Mathf.Clamp(safe.yMin, 0f, h);
            float topPx = Mathf.Clamp(h - safe.yMax, 0f, h - bottomPx);
            if (safe.width <= 0f || safe.height <= 0f)
            {
                leftPx = rightPx = bottomPx = topPx = 0f; // sin zona segura válida, la pantalla entera
            }

            int contentPx = Mathf.Max(1, Mathf.RoundToInt(w - leftPx - rightPx));
            float scale = GameScreen.CanvasScale(contentPx, h, pxPerDp);
            float canvasW = w / scale;
            float canvasH = h / scale;
            float left = leftPx / scale;
            float right = rightPx / scale;
            float safeW = Mathf.Max(1f, canvasW - left - right);
            float columnW = Mathf.Min(safeW, ColumnMaxAspect * canvasH);
            float columnX;
            if (columnW >= safeW - 0.001f)
            {
                columnW = safeW;
                columnX = left;
            }
            else
            {
                // Centrada en la pantalla (como el icono del arranque del sistema) y, si un lado tapa, dentro de la zona segura.
                columnX = Mathf.Clamp((canvasW - columnW) * 0.5f, left, canvasW - right - columnW);
            }

            return new ScreenFrame(scale, canvasW, canvasH, columnX, columnW, topPx / scale, bottomPx / scale, left, right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "lienzo " + CanvasWidth.ToString("0.#") + " × " + CanvasHeight.ToString("0.#") + " dp a " + Scale.ToString("0.###")
                + " px/dp; columna " + ColumnWidth.ToString("0.#") + " dp en x = " + ColumnX.ToString("0.#") + "; márgenes "
                + InsetTop.ToString("0.#") + "/" + InsetBottom.ToString("0.#") + "/" + InsetLeft.ToString("0.#") + "/" + InsetRight.ToString("0.#");
        }
    }
}
