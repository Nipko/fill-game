#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial del modo de captura en la pantalla de nivel (D-066; spark-marketing §6 E1; solo builds de desarrollo, lo enciende
    /// <c>GameBootstrap.Capture.cs</c>):
    /// <list type="bullet">
    /// <item><see cref="SetCaptureHudHidden"/>: el HUD (insignia «Nivel {n}», Reiniciar, la bandeja de comodines, las etiquetas y la
    /// Crecida), la capa superior (el engranaje y los saldos) y los avisos y rótulos (TiTino, el lugar, el miquito) pasan a alfa 0 y no
    /// reciben toques, con un <see cref="CanvasGroup"/> en cada capa. El tablero, el fondo, la tropa, el vertido, la victoria y
    /// las animaciones siguen igual; los toques del fondo y de las cuias, también.</item>
    /// <item><see cref="CaptureBackdropLevel"/>: el fondo del tablero (la parada y su temporada) de otro nivel de campaña.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameScreen
    {
        private CanvasGroup[]? _captureGroups;

        /// <summary>El HUD está oculto por el modo de captura.</summary>
        public bool CaptureHudHidden { get; private set; }

        /// <summary>
        /// Nivel de campaña cuyo fondo (parada y temporada) lleva el tablero en lugar del suyo (modo de captura,
        /// <c>af_capture_world</c> o <c>af_capture_bg_level</c>); 0 = el del nivel cargado, como siempre. Cuenta desde la carga
        /// siguiente.
        /// </summary>
        public int CaptureBackdropLevel { get; set; }

        /// <summary>Oculta (true) o vuelve a enseñar (false) el HUD del nivel para la toma.</summary>
        public void SetCaptureHudHidden(bool hidden)
        {
            if (_captureGroups == null)
            {
                // El cartel «El río bajó» (solo al reintentar un nivel con relief) lleva su propio CanvasGroup animado: no se toca.
                _captureGroups = new[] { GroupOf(_hud), GroupOf(_topLayer), GroupOf(_noticeLayer) };
            }

            for (int i = 0; i < _captureGroups.Length; i++)
            {
                CanvasGroup group = _captureGroups[i];
                group.alpha = hidden ? 0f : 1f;
                group.blocksRaycasts = !hidden;
                group.interactable = !hidden;
            }

            CaptureHudHidden = hidden;
        }

        // El nivel cuyo fondo se pinta: el forzado por la captura o el cargado.
        private int BackdropLevelFor(int level)
        {
            return CaptureBackdropLevel > 0 ? CaptureBackdropLevel : level;
        }

        private static CanvasGroup GroupOf(Transform layer)
        {
            CanvasGroup? group = layer.GetComponent<CanvasGroup>();
            return group != null ? group : layer.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
