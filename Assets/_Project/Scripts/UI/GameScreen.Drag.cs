#nullable enable
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// C10, el arrastre (02 §3.1; tarea B11; regla en <see cref="DragPour"/>): el dedo baja en X (que se eleva en el touch-down,
    /// C1) y sube sobre otra cuia Y tras moverse ≥ 24 dp → el toque en X y el toque en Y, por el mismo camino que dos toques
    /// (<c>IGameActions.TapCuia</c>: el flujo y el núcleo deciden el vertido, C4). Si el vertido es ilegal
    /// (<c>PourRules.IsLegal</c>, del núcleo), el arrastre no selecciona nada: Y se sacude con el sonido del rechazo, como en C6, y
    /// X baja (sin C5) [P]. Soltar sobre el fondo, sobre otra cosa o sin
    /// moverse lo bastante cancela, como antes. Los 24 dp son del lienzo (360 dp de ancho, como todo el layout; Q-B11-5). Lo llama <c>OnCuiaUp</c> al soltar fuera de X (parche B11-drag), con las
    /// posiciones del puntero que guarda <see cref="CuiaView"/> en el touch-down y el touch-up (el parche se las añade). Interruptor
    /// <c>drag_pour_enabled</c> (08 §9, default true): <see cref="DragPourEnabled"/>, el valor de Remote Config del arranque
    /// (<c>RcConfigs.DragPourEnabled</c>, <c>RcGameplayBinder</c>; cierre de B11).
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>Solo tests: fuerza <c>drag_pour_enabled</c> (null = el default de 08 §9).</summary>
        internal static bool? DragPourOverride { get; set; }

        /// <summary>
        /// <c>drag_pour_enabled</c> en esta partida (08 §9; C10): el del test, si no el de Remote Config del arranque
        /// (<see cref="GameBootstrap.ActiveRcConfigs"/>) y, sin arranque, el default de 08 §9.
        /// </summary>
        internal static bool DragPourEnabled => DragPourOverride ?? GameBootstrap.ActiveRcConfigs?.DragPourEnabled ?? DragPour.EnabledDefault;

        /// <summary>El último soltar fuera de la cuia pulsada que se tomó como arrastre (tests).</summary>
        internal int LastDragSource { get; private set; } = -1;

        /// <summary>Destino del último arrastre (tests).</summary>
        internal int LastDragTarget { get; private set; } = -1;

        // Soltar fuera de la cuia index: con un arrastre válido (DragPour.Resolve), el toque en X y el toque en Y. Devuelve si se
        // tomó como arrastre. liftedAtDown: la cuia se elevó en el touch-down (_pressPreview == index antes de soltar); down y up,
        // las posiciones de pantalla (px) del touch-down y del touch-up, con la cámara del evento (null en Screen Space Overlay).
        internal bool TryDragPour(int index, bool liftedAtDown, Vector2 down, Vector2 up, Camera? camera)
        {
            LevelFlow? flow = _flow;
            if ((uint)index >= (uint)_views.Length || flow == null || flow.Selected != TapController.NoSelection || !flow.AcceptsBoardInput
                || flow.RevealSelecting)
            {
                return false; // con una cuia seleccionada entre medias (otro dedo, C11) o sin aceptar toques, soltar fuera cancela [P]
            }

            int target = CuiaUnder(up, camera, index);
            float scale = _scaler.scaleFactor > 0f ? _scaler.scaleFactor : 1f;
            float distanceDp = Vector2.Distance(down, up) / scale;
            if (DragPour.Resolve(DragPourEnabled, liftedAtDown, index, target, distanceDp) != DragOutcome.Pour)
            {
                return false;
            }

            LastDragSource = index;
            LastDragTarget = target;
            if (!PourRules.IsLegal(flow.Session.Current, index, target))
            {
                // Vertido ilegal (la regla, del núcleo): un arrastre que no vierte no selecciona nada, ni X ni Y (sin C5) [P]
                // (Q-B11-11); Y se sacude con el sonido neutro del rechazo, como en C6, y X baja al soltar (OnCuiaUp).
                _views[target].Shake();
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.Illegal);
                _probe.State("reject", flow, target, null); // AF_STATE reject, como el toque ilegal (C6)
                return true;
            }

            _actions.TapCuia(index); // C1: X queda seleccionada (ya estaba elevada desde el touch-down)
            if (flow.Selected == index)
            {
                _actions.TapCuia(target); // C4 contra el modelo
            }

            return true;
        }

        // La cuia bajo el punto de pantalla screen, sin contar except ni las vistas inactivas; −1 si ninguna. Primero la que tiene
        // el punto en su cuerpo (lo que se ve) y, si ninguna, la de su área táctil (Root, con el aire de encima para alcanzarla
        // elevada, 02 §3.3), que puede pisar la de la fila de arriba: así gana la cuia que está bajo el dedo, no la de índice menor.
        private int CuiaUnder(Vector2 screen, Camera? camera, int except)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < _views.Length; i++)
                {
                    CuiaView view = _views[i];
                    if (i == except || !view.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    RectTransform area = pass == 0 ? view.Body : view.Root;
                    if (RectTransformUtility.RectangleContainsScreenPoint(area, screen, camera))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
