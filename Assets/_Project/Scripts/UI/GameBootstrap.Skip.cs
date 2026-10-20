#nullable enable
using AF.Boosters;
using AF.Core.Session;
using AF.GameFlow;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Arranque del Salto del Boto (bloque B3; 04 §2.6; nota <c>docs/dev/salto-del-boto.md</c>). Parcial nuevo:
    /// <c>GameBootstrap.cs</c> solo gana la llamada a <see cref="SetUpSkip"/> (parche <c>B3-ui.diff</c>, tras
    /// <c>SetUpStreak();</c>).
    /// <list type="bullet">
    /// <item><see cref="SetUpSkip"/> crea el <see cref="ProfileSkipStore"/> sobre el mismo perfil y el lote (el mismo que el
    /// progreso; dentro del lote del salto, el cobro, la Canasta y el salto salen en una escritura) y lo inyecta con
    /// <see cref="LevelFlow.UseSkip"/>; crea la confirmación (<see cref="SkipConfirm"/>) y el banner (<see cref="SkipBanner"/>)
    /// encima de la pantalla del nivel, y el componente <see cref="SkipRunner"/> que enseña o esconde el banner cada fotograma.</item>
    /// <item>Implementa <see cref="ISkipActions"/>: el modal (rama D) y la pausa lo piden con <c>as</c> (parche
    /// <c>B3-ui.diff</c>).</item>
    /// <item>Sonda (solo en desarrollo): <c>AF_STATE af_skip_used</c> (04 §11: <c>fails_level</c>, <c>since_last_skip</c>,
    /// <c>level_tag</c>) y, si el salto pagó la Canasta del <c>10k</c>, las líneas de la Crecida (<c>basket_open</c>), que el
    /// <see cref="StreakProbeRunner"/> no ve porque cargar el nivel siguiente borra <see cref="LevelFlow.LastStreakResult"/>.</item>
    /// </list>
    /// La animación de Rosa (1,5 s, 04 §2.6 «VFX / SFX»; <c>rosa_jump</c> y <c>sfx_skip</c> de 07) espera al arte y al audio: hoy
    /// el siguiente nivel entra con su intro de siempre [P] (nota §3, Q-S5).
    /// </summary>
    public sealed partial class GameBootstrap : ISkipActions
    {
        private ProfileSkipStore? _skipStore;
        private SkipConfirm? _skipConfirm;
        private SkipBanner? _skipBanner;
        private SkipRunner? _skipRunner;

        /// <summary>Almacén del Salto (null antes de <see cref="SetUpSkip"/>).</summary>
        public ISkipStore? SkipStore => _skipStore;

        /// <summary>Confirmación del Salto (tests).</summary>
        internal SkipConfirm? SkipConfirmView => _skipConfirm;

        /// <summary>Banner del Salto (tests).</summary>
        internal SkipBanner? SkipBannerView => _skipBanner;

        /// <summary>
        /// Crea el almacén y la vista del Salto e inyecta el almacén en el flujo de campaña. False, sin crear nada, si aún no hay
        /// arranque; una segunda llamada no hace nada y devuelve true.
        /// </summary>
        public bool SetUpSkip()
        {
            if (_skipStore != null)
            {
                return true;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            LevelFlow? flow = _flow;
            GameScreen? screen = _screen;
            if (profile == null || batch == null || flow == null || screen == null)
            {
                Debug.LogError(LogPrefix + "SetUpSkip antes del arranque: el Salto del Boto no se crea.");
                return false;
            }

            _skipStore = new ProfileSkipStore(profile, batch);
            flow.UseSkip(_skipStore);
            Transform layer = screen.Pause.Root.parent; // el lienzo de la pantalla del nivel, donde viven la pausa y la confirmación
            _skipBanner = new SkipBanner(layer, screen.CardArt, () => PressSkip());
            _skipConfirm = new SkipConfirm(layer, screen.CardArt, () => ConfirmSkip(), CancelSkip);
            SkipRunner runner = gameObject.GetComponent<SkipRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<SkipRunner>();
            }

            _skipRunner = runner;
            _skipRunner.Bind(this);
            return true;
        }

        /// <inheritdoc />
        public bool PressSkip()
        {
            LevelFlow? flow = _flow;
            GameScreen? screen = _screen;
            if (flow == null || screen == null || _skipConfirm == null || !InLevel)
            {
                return false;
            }

            SkipOffer offer = flow.SkipOffer;
            if (!offer.CanUse)
            {
                return false;
            }

            flow.ClearSelection();
            screen.Pause.Hide();
            _skipBanner?.Hide();
            _skipConfirm.Show(offer, flow.SkipLosesExtraCuia, screen.CanvasWidth, screen.CardCanvasHeight);
            if (DevProbe.Enabled && _probe != null)
            {
                var ids = new System.Collections.Generic.List<string>(2);
                var rects = new System.Collections.Generic.List<RectTransform>(2);
                _skipConfirm.VisibleButtons(ids, rects);
                _probe.Layout("skip_confirm", flow.LevelNumber, null, ids.ToArray(), rects.ToArray());
            }

            return true;
        }

        /// <inheritdoc />
        public bool ConfirmSkip()
        {
            _skipConfirm?.Hide();
            LevelFlow? flow = _flow;
            if (flow == null || !InLevel)
            {
                return false;
            }

            SkipResult? result = flow.Skip();
            if (result == null || !result.Value.Applied)
            {
                return false;
            }

            if (DevProbe.Enabled)
            {
                DevProbe.SkipUsed(result.Value, _wallet?.Seeds ?? 0);
                if (result.Value.Streak != null)
                {
                    DevProbe.StreakResult(result.Value.Streak, null, result.Value.Level);
                }
            }

            return true;
        }

        /// <inheritdoc />
        public void CancelSkip()
        {
            _skipConfirm?.Hide();
        }

        /// <summary>
        /// El banner, cada fotograma (<see cref="SkipRunner"/>): visible mientras la opción se ofrece y el intento aún no empezó
        /// (tarjeta previa, intro o tablero sin vertidos), sin la pausa, la confirmación de salir o la del salto abiertas. Sin
        /// asignaciones salvo al cambiar el precio.
        /// </summary>
        internal void TickSkipBanner()
        {
            SkipBanner? banner = _skipBanner;
            LevelFlow? flow = _flow;
            GameScreen? screen = _screen;
            if (banner == null)
            {
                return;
            }

            if (flow == null || screen == null || !InLevel || !flow.HasLevel || (_skipConfirm != null && _skipConfirm.Visible)
                || screen.Pause.Visible || screen.Confirm.Visible || !BannerPhase(flow))
            {
                banner.Hide();
                return;
            }

            SkipOffer offer = flow.SkipOffer;
            if (!offer.Offered)
            {
                banner.Hide();
                return;
            }

            banner.Show(offer.Price, screen.CanvasWidth);
        }

        private static bool BannerPhase(LevelFlow flow)
        {
            FlowPhase phase = flow.Phase;
            return phase == FlowPhase.LevelCard || phase == FlowPhase.Intro
                || (phase == FlowPhase.Playing && !flow.IsResolving && flow.Session.AppliedPours == 0 && flow.Session.Status == LevelStatus.Playing);
        }
    }
}
