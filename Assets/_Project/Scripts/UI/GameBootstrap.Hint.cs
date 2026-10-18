#nullable enable
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Pista · Morpho desde la bandeja (tarea MO2; 04 §2.3; 04 §1.1): los dos puntos de entrada de <see cref="IGameActions"/>
    /// (<see cref="PressHint"/> y <see cref="PressHintVideo"/>), sin editar GameBootstrap.cs. La consulta, el corte y el cobro son
    /// de <see cref="LevelFlow.PressHint"/> (MO1): pedirla nunca cobra; el vertido se cobra al darlo. Y el interruptor de la pista
    /// por inactividad en Ajustes (<see cref="ChooseIdleHint"/>; tarea MO3).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <inheritdoc />
        /// <remarks>
        /// Orden de cobro [P], el mismo que pinta la bandeja (<c>BoosterTray.PaintHint</c>) y que el de Revelar (04 §2.5; pregunta
        /// 23 abierta): el stock; si no, las semillas; sin saldo, el video <c>rv_hint</c>; sin nada, la burbuja «Te faltan {n}»
        /// late (pregunta 48 (b), D-022) y no se pide nada. Solo en PLAYING con algún vertido legal (en Aguas estancadas Morpho no
        /// está): con la victoria o el bloqueo pendiente, la bandeja sigue habilitada (D-011, punto 1), pero el toque no hace nada.
        /// </remarks>
        public bool PressHint()
        {
            LevelFlow? flow = InLevel ? _flow : null;
            if (flow == null || !flow.HasLevel)
            {
                return false;
            }

            HintOffer offer = flow.Hint;
            if (offer.State == HintState.Unsupported)
            {
                _screen?.ShowHintUnsupported(); // 04 §2.3 b
                return false;
            }

            if (offer.State != HintState.Ready || flow.IsResolving)
            {
                return false;
            }

            if (offer.CanUse)
            {
                return flow.PressHint();
            }

            if (PressHintVideo())
            {
                return true;
            }

            _screen?.ShowHintMissing(offer.Missing);
            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Video <c>rv_hint</c> (04 §2.3: 1 pista, 3 al día y 60 s de espera, <c>AdsConfig</c>): al terminar el anuncio, en un lote
        /// de guardado, +1 Morpho al stock (<see cref="LevelFlow.GrantHintVideoUnit"/>) y la pista pagando con él. La unidad va al
        /// stock aunque la pista ya no se pueda pedir (el tablero cambió): nunca se pierde.
        /// </remarks>
        public bool PressHintVideo()
        {
            LevelFlow? flow = _flow;
            AdController? ads = _ads;
            if (flow == null || ads == null || !InLevel || !flow.HasLevel || flow.Hint.State != HintState.Ready
                || !ads.CheckRewarded(AdPlacement.Hint).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.Hint, r =>
            {
                if (!r.IsRewarded)
                {
                    return;
                }

                using (Saves())
                {
                    if (flow.GrantHintVideoUnit())
                    {
                        flow.PressHint();
                    }
                }
            });
            return true;
        }

        /// <summary>
        /// Ajustes › Pista por inactividad (02 §3.4 y §12; 04 §2.3; tarea MO3): guarda <paramref name="on"/> en
        /// <c>settings.idle_hint</c> [P] en el acto; apagada equivale a <c>hint_idle_delay_s</c> = 0 en el dispositivo y no afecta a
        /// Morpho. La pista la lee en cada fotograma (<c>GameScreen.Hint.cs</c>). La sonda escribe <c>settings_change</c> con la
        /// clave <c>idle_hint</c>.
        /// </summary>
        public bool ChooseIdleHint(bool on)
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return false;
            }

            if (profile.Settings.IdleHint != on)
            {
                profile.Settings.IdleHint = on;
                SaveSettings(profile);
            }

            _settingsScreen?.MarkIdleHint(on);
            _probe?.SettingsChange(SettingsScreen.IdleHintId, on ? "true" : "false", null);
            return true;
        }
    }
}
