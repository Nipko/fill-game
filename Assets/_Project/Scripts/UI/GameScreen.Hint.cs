#nullable enable
using System.Globalization;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AF.UI
{
    /// <summary>
    /// Pista por inactividad en la vista (tarea MO3; 02 §3.4; 04 §2.3; 08 §9 <c>hint_idle_from_level</c> y
    /// <c>hint_idle_delay_s</c>; D-002 y D-019): cada fotograma pasa a <see cref="IdleHint"/> (AF.GameFlow) si hubo un toque en la
    /// pantalla y si la vista está ocupada, y pinta lo que pide: el <b>origen</b> pulsa una vez (escala
    /// <see cref="IdleHint.PulseScale"/> en <see cref="IdleHint.PulseMs"/>, sube y baja) con el aleteo bajo de
    /// <c>sfx_morpho_idle</c>. Nunca enseña el destino ni cobra. Un toque o un cambio del tablero apaga el pulso en el acto.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Toques:</b> cualquier puntero apretado en la pantalla (<see cref="Pointer.current"/>), además de
    /// todo lo que cambia el flujo (selección, vertido, Marea, Morpho…), que <see cref="IdleHint"/> ya mira.</description></item>
    /// <item><description><b>Vista ocupada</b> (no cuenta como inactividad): el tablero anima, la pausa, la confirmación, la
    /// tarjeta o la mariposa de Morpho, o un aviso en pantalla (tutorial de TiTino, miquito, Fruta nueva, rótulo).</description></item>
    /// <item><description><b>Ajustes:</b> «Pista por inactividad» (<c>settings.idle_hint</c>, 02 §12) se lee en cada fotograma:
    /// apagarla equivale a <c>hint_idle_delay_s</c> = 0 en el dispositivo.</description></item>
    /// <item><description><b>Segundo plano:</b> al volver a la app la cuenta empieza de cero (<see cref="IdleHintBackFromBackground"/>):
    /// el tiempo fuera no es inactividad en el tablero.</description></item>
    /// <item><description><b>Pulso:</b> solo la escala del cuerpo de la cuia (pivote en el pie), sin asignaciones por fotograma;
    /// si la cuia sale a verter, el pulso se corta y su escala vuelve a 1.</description></item>
    /// </list>
    /// Los ganchos son los de Morpho (<c>HintTick</c> y <c>HintLevelLoaded</c> de GameScreen.Morpho.cs), sin tocar GameScreen.cs.
    /// </remarks>
    public sealed partial class GameScreen
    {
        private readonly IdleHint _idleHint = new IdleHint();
        private int _idlePulseCuia = -1;
        private float _idlePulseMs = -1f;
        private bool _idleTouched;

        /// <summary>
        /// Solo tests: null (el juego) = la pista por inactividad según Ajustes; false = apagada sin mirar Ajustes. Los tests que
        /// no tratan de ella la apagan (<c>PlayModeHelpers.NoIdleHintUnlessSet</c>): con un reloj que salta minutos (canoas,
        /// anuncios) pulsaría en mitad de otras pruebas. Con true, manda Ajustes, como en el juego.
        /// </summary>
        internal static bool? IdleHintOverride { get; set; }

        /// <summary>La pista por inactividad de esta vista (tests).</summary>
        internal IdleHint IdleHint => _idleHint;

        /// <summary>Cuia de origen del último pulso pintado (−1 si aún no hubo ninguno en este nivel; tests).</summary>
        internal int IdlePulseCuia => _idlePulseCuia;

        /// <summary>El pulso del origen está en curso (tests).</summary>
        internal bool IdlePulsing => _idlePulseMs >= 0f;

        /// <summary>Pulsos pintados desde que se creó la vista (tests y sonda).</summary>
        internal int IdlePulses { get; private set; }

        // La pista se puede usar en el dispositivo: el gancho de los tests y Ajustes › Pista por inactividad (activada por
        // defecto; sin servicios, como en los tests del flujo, también).
        private bool IdleHintSetting => (IdleHintOverride ?? true) && (_services == null || _services.Settings.IdleHint);

        /// <summary>Solo tests: un toque en la pantalla que no pasa por el flujo (el fondo, un botón inactivo).</summary>
        internal void IdleHintTouch()
        {
            _idleTouched = true;
        }

        /// <summary>
        /// Vuelta de segundo plano (<c>GameBootstrap.OnApplicationPause(false)</c>): el tiempo fuera de la app no cuenta como
        /// inactividad en el tablero, así que en el siguiente fotograma la cuenta empieza de cero, como tras un toque (abandona la
        /// consulta en curso y apaga el pulso, si lo hay).
        /// </summary>
        internal void IdleHintBackFromBackground()
        {
            _idleTouched = true;
        }

        // Cada fotograma (desde HintTick): toques, vista ocupada, la cuenta y el pulso.
        private void IdleHintTick(float dtMs)
        {
            LevelFlow? flow = _flow;
            if (flow == null)
            {
                return;
            }

            Pointer? pointer = Pointer.current;
            bool touched = _idleTouched || (pointer != null && pointer.press.isPressed);
            _idleTouched = false;
            bool busy = !Visible || !IsViewIdle || _pause.Visible || _confirm.Visible || _hintCard.Visible || _morphoFx.Active
                || NoticeShowing;
            switch (_idleHint.Tick(flow, IdleHintSetting, touched, busy))
            {
                case IdleHintSignal.Pulse:
                    StartIdlePulse(flow, _idleHint.PulseSource);
                    break;
                case IdleHintSignal.Interrupted:
                    StopIdlePulse();
                    break;
                case IdleHintSignal.Silent:
                    IdleHintProbe("silent", flow, -1);
                    break;
            }

            AdvanceIdlePulse(dtMs);
        }

        // Nivel nuevo (desde HintLevelLoaded, con las vistas ya rehechas): sin pulso ni cuenta del nivel anterior.
        private void IdleHintLevelLoaded()
        {
            _idleHint.Reset();
            _idlePulseMs = -1f;
            _idlePulseCuia = -1;
        }

        private void StartIdlePulse(LevelFlow flow, int source)
        {
            StopIdlePulse();
            if ((uint)source >= (uint)_views.Length || _views[source] == null)
            {
                return;
            }

            _idlePulseCuia = source;
            _idlePulseMs = 0f;
            IdlePulses++;
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.MorphoIdle); // el aleteo de sfx_hint, más bajo (lista de sonidos)
            IdleHintProbe("pulse", flow, source);
        }

        private void AdvanceIdlePulse(float dtMs)
        {
            if (_idlePulseMs < 0f)
            {
                return;
            }

            CuiaView? view = IdlePulseView();
            if (view == null || view.InFlight || view.BodyInFront)
            {
                StopIdlePulse();
                return;
            }

            _idlePulseMs += dtMs;
            float t = _idlePulseMs / IdleHint.PulseMs;
            if (t >= 1f)
            {
                StopIdlePulse();
                return;
            }

            float s = 1f + ((IdleHint.PulseScale - 1f) * Mathf.Sin(t * Mathf.PI));
            view.Body.localScale = new Vector3(s, s, 1f);
        }

        // Apaga el pulso en curso: la escala del cuerpo vuelve a 1 (la cuia sigue siendo la del último pulso, para los tests).
        private void StopIdlePulse()
        {
            if (_idlePulseMs < 0f)
            {
                return;
            }

            _idlePulseMs = -1f;
            CuiaView? view = IdlePulseView();
            if (view != null)
            {
                view.Body.localScale = Vector3.one;
            }
        }

        private CuiaView? IdlePulseView()
        {
            int i = _idlePulseCuia;
            return (uint)i < (uint)_views.Length && _views[i] != null ? _views[i] : null;
        }

        /// <summary>Escala del cuerpo de la cuia <paramref name="index"/> ahora mismo (tests: 1 salvo durante el pulso).</summary>
        internal float CuiaBodyScale(int index)
        {
            return (uint)index < (uint)_views.Length && _views[index] != null ? _views[index].Body.localScale.x : 1f;
        }

        // AF_STATE {"event":"idle_hint","phase":"pulse"|"silent",…}: solo en desarrollo, en el pulso o en la respuesta sin vertido
        // (una vez por tramo sin toques).
        private void IdleHintProbe(string phase, LevelFlow flow, int source)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            CultureInfo inv = CultureInfo.InvariantCulture;
            string answer = _idleHint.LastAnswer.HasValue ? ((int)_idleHint.LastAnswer.Value).ToString(inv) : "-1";
            _probe.Event("idle_hint", "\"phase\":\"" + phase + "\",\"level\":" + flow.LevelNumber.ToString(inv)
                + ",\"from\":" + source.ToString(inv) + ",\"answer\":" + answer);
        }
    }
}
