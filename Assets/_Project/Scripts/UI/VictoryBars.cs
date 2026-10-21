#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Icono de un premio de las barras de la victoria (tarea B1b).</summary>
    internal enum RewardIcon
    {
        /// <summary>Semillas (<see cref="ProceduralArt.IconSeed"/>), con «+{n}».</summary>
        Seed = 0,

        /// <summary>Marea (<see cref="ProceduralArt.IconUndo"/>), con «×{n}».</summary>
        Undo = 1,

        /// <summary>Morpho (<see cref="ProceduralArt.IconMorpho"/>).</summary>
        Hint = 2,

        /// <summary>Luciérnaga (<see cref="ProceduralArt.IconFirefly"/>).</summary>
        Reveal = 3,

        /// <summary>Cuia extra (<see cref="ProceduralArt.IconCuiaPlus"/>).</summary>
        ExtraCuia = 4,

        /// <summary>La Canasta del hito 6, sin cifra (<see cref="ProceduralArt.IconBasket"/>): su contenido se ve al abrirla.</summary>
        Basket = 5,
    }

    /// <summary>Un premio de una barra: icono y cantidad (0 = sin cifra).</summary>
    internal readonly struct RewardItem
    {
        public RewardItem(RewardIcon icon, int amount)
        {
            Icon = icon;
            Amount = amount;
        }

        public RewardIcon Icon { get; }

        public int Amount { get; }
    }

    /// <summary>
    /// Qué llevan las barras del panel único de la victoria (D-033, puntos 8-10; 02 §6, filas «Crecida» y «Barras de evento»; 06
    /// §4.3-§4.4; tarea B1b), sin motor de UI: la réplica de la Crecida (desde <c>streak_unlock_level</c>, con el chip del hito) y la
    /// barra «Canasta · {sitio} k/10» (desde <c>live_unlock_level_daily</c>), que se abre sola en el <c>10k</c>. La tercera barra
    /// rotativa (skin ganada &gt; Bitácora lista &gt; Reto) aún no existe: <see cref="Extra"/> es su hueco (B4). Se rearma en cada
    /// victoria (asigna sus listas una vez).
    /// </summary>
    internal sealed class VictoryBarsPlan
    {
        /// <summary>Los hitos de la escalera (05 §2.4).</summary>
        private static readonly int[] Milestones = { StreakRecord.HitSeeds, StreakRecord.HitBasket, StreakRecord.HitRiver };

        /// <summary>Lleva la réplica de la Crecida.</summary>
        public bool Streak { get; private set; }

        /// <summary>Barra de la Crecida al abrir el panel (la de antes de esta victoria).</summary>
        public int StreakFrom { get; private set; }

        /// <summary>
        /// Barra de la Crecida al terminar su animación: la de después de la victoria o, si esta pagó el hito 10, 10 (la barra se
        /// vacía en el HUD del nivel siguiente) [P].
        /// </summary>
        public int StreakTo { get; private set; }

        /// <summary>Esta victoria pagó un hito: el chip enseña lo pagado y vuela al saldo (02 §6: «la recompensa se anima aquí»).</summary>
        public bool StreakPaid { get; private set; }

        /// <summary>Hito del chip (el pagado o el siguiente que se pagaría hoy; 0 sin chip).</summary>
        public int StreakMilestone { get; private set; }

        /// <summary>La Hoja escudo de hoy sigue disponible: se dibuja al pie de la barra (06 §4.3, «HUD»).</summary>
        public bool Shield { get; private set; }

        /// <summary>El chip del hito: lo pagado o lo que se pagaría (sin prometer lo que la regla del día no daría; nota B1a §6.1).</summary>
        public List<RewardItem> StreakReward { get; } = new List<RewardItem>(5);

        /// <summary>Lleva la barra de la Canasta.</summary>
        public bool Basket { get; private set; }

        /// <summary>Niveles ganados del tramo de la Canasta, de 1 a 10 (el k de «k/10»).</summary>
        public int BasketCount { get; private set; }

        /// <summary>La parada a la que se llega al cerrar el tramo (su nombre), o null sin catálogo del Mapa.</summary>
        public string? BasketPlace { get; private set; }

        /// <summary>Esta victoria paga la Canasta de nivel: se abre sola dentro del panel (D-033, punto 8).</summary>
        public bool BasketOpens { get; private set; }

        /// <summary>El contenido de la Canasta del tramo, visible antes de abrirla (06 §4.4; K6).</summary>
        public List<RewardItem> BasketReward { get; } = new List<RewardItem>(5);

        /// <summary>Barras rotativas de más (la tercera de D-033, punto 10: skin ganada, Bitácora o Reto): 0 hasta B4.</summary>
        public int Extra { get; set; }

        /// <summary>Barras que lleva esta victoria.</summary>
        public int BarCount => (Streak ? 1 : 0) + (Basket ? 1 : 0) + Extra;

        /// <summary>Semillas que esta victoria paga en las barras (las que el saldo no enseña hasta que llegan; D-011).</summary>
        public int PaidSeeds { get; private set; }

        /// <summary>Lo vacía todo.</summary>
        public void Clear()
        {
            Streak = false;
            StreakFrom = 0;
            StreakTo = 0;
            StreakPaid = false;
            StreakMilestone = 0;
            Shield = false;
            StreakReward.Clear();
            Basket = false;
            BasketCount = 0;
            BasketPlace = null;
            BasketOpens = false;
            BasketReward.Clear();
            Extra = 0;
            PaidSeeds = 0;
        }

        /// <summary>
        /// Arma las barras de la victoria del nivel <paramref name="level"/>: <paramref name="after"/> es el estado de la Crecida tras
        /// la victoria (<c>LevelFlow.PeekStreak</c>; null sin servicio), <paramref name="result"/> lo que pagó
        /// (<c>LevelFlow.LastStreakResult</c>), <paramref name="config"/> y <paramref name="basket"/> las cifras del servicio y
        /// <paramref name="basketUnlock"/> el nivel desde el que hay Canasta (<c>live_unlock_level_daily</c>; ≤ 0 = sin Canasta).
        /// </summary>
        public void Build(int level, bool campaign, StreakSnapshot? after, StreakServiceResult? result, StreakConfig? config,
            BasketConfig? basket, int basketUnlock, string? place)
        {
            Clear();
            if (level < 1 || !campaign)
            {
                return;
            }

            StreakOutcome outcome = result != null ? result.Streak : default;
            bool won = result != null && outcome.Event == StreakEvent.Victory && outcome.Counted;
            if (after.HasValue && after.Value.Active && config != null)
            {
                StreakSnapshot s = after.Value;
                Streak = true;
                Shield = s.ShieldAvailable;
                StreakFrom = won ? Mathf.Clamp(outcome.WinsBefore, 0, StreakRecord.MaxWins) : s.Wins;
                StreakTo = won ? (outcome.BarEmptied ? StreakRecord.MaxWins : Mathf.Clamp(outcome.Wins, 0, StreakRecord.MaxWins)) : s.Wins;
                if (won && outcome.MilestonePaid)
                {
                    StreakPaid = true;
                    StreakMilestone = outcome.Milestone;
                    AddMilestoneReward(StreakReward, outcome.Milestone, outcome.HitSeeds, outcome.Basket, true);
                    PaidSeeds += result!.StreakSeeds;
                }
                else
                {
                    StreakMilestone = NextMilestone(StreakTo >= StreakRecord.MaxWins ? 0 : StreakTo, in s, config, out int seeds);
                    if (StreakMilestone > 0)
                    {
                        AddMilestoneReward(StreakReward, StreakMilestone, seeds, BasketContents.None, false);
                    }
                }
            }

            if (basket != null && basketUnlock > 0 && level >= basketUnlock)
            {
                Basket = true;
                BasketCount = ((level - 1) % BasketRules.Every) + 1;
                BasketPlace = string.IsNullOrEmpty(place) ? null : place;
                int slot = ((level + BasketRules.Every - 1) / BasketRules.Every) * BasketRules.Every;
                BasketContents paid = result != null ? result.LevelBasket : BasketContents.None;
                BasketOpens = !paid.IsNone;
                AddContents(BasketReward, BasketOpens ? paid : BasketRules.For(slot, basketUnlock, basket));
                if (BasketOpens)
                {
                    PaidSeeds += result!.BasketSeeds;
                }
            }
        }

        /// <summary>
        /// El siguiente hito por encima de <paramref name="wins"/> que se pagaría hoy (con <c>streak_hits_once_per_day</c>, sin los
        /// ya cobrados; el del 3, solo si el tope de 600 aún deja semillas: <paramref name="seeds"/>), o 0 si ninguno.
        /// </summary>
        internal static int NextMilestone(int wins, in StreakSnapshot s, StreakConfig config, out int seeds)
        {
            seeds = 0;
            for (int i = 0; i < Milestones.Length; i++)
            {
                int m = Milestones[i];
                if (m <= wins || (config.StreakHitsOncePerDay && s.HitPaid(m)))
                {
                    continue;
                }

                if (m == StreakRecord.HitSeeds)
                {
                    seeds = Mathf.Min(config.StreakMilestones.Hit3Seeds, Mathf.Max(0, config.StreakDailyCap - s.SeedsToday));
                    if (seeds <= 0)
                    {
                        continue;
                    }
                }

                return m;
            }

            return 0;
        }

        // El premio del hito m: 300 semillas (o lo que deje el tope), la Canasta (pagada: su contenido; si no, su icono) o la Cuia extra.
        private static void AddMilestoneReward(List<RewardItem> into, int m, int seeds, BasketContents paidBasket, bool paid)
        {
            switch (m)
            {
                case StreakRecord.HitSeeds:
                    into.Add(new RewardItem(RewardIcon.Seed, seeds));
                    break;
                case StreakRecord.HitBasket:
                    if (paid && !paidBasket.IsNone)
                    {
                        AddContents(into, paidBasket);
                    }
                    else
                    {
                        into.Add(new RewardItem(RewardIcon.Basket, 0));
                    }

                    break;
                case StreakRecord.HitRiver:
                    into.Add(new RewardItem(RewardIcon.ExtraCuia, 1));
                    break;
            }
        }

        /// <summary>El contenido de una Canasta en el orden de su JSON (semillas, Marea, Morpho, Luciérnaga, Cuia extra).</summary>
        internal static void AddContents(List<RewardItem> into, BasketContents contents)
        {
            if (contents.IsNone)
            {
                return;
            }

            if (contents.Seeds > 0)
            {
                into.Add(new RewardItem(RewardIcon.Seed, contents.Seeds));
            }

            Add(into, RewardIcon.Undo, contents.Stock.Get(BoosterKind.Undo));
            Add(into, RewardIcon.Hint, contents.Stock.Get(BoosterKind.Hint));
            Add(into, RewardIcon.Reveal, contents.Stock.Get(BoosterKind.Reveal));
            Add(into, RewardIcon.ExtraCuia, contents.Stock.Get(BoosterKind.ExtraCuia));
        }

        private static void Add(List<RewardItem> into, RewardIcon icon, int units)
        {
            if (units > 0)
            {
                into.Add(new RewardItem(icon, units));
            }
        }
    }

    /// <summary>
    /// Disposición de las barras del panel único de la victoria según el escalón de la victoria compacta (D-033, punto 10, con
    /// D-032, punto 2) [P]: hasta 3 barras a lo ancho; desde el escalón 2, la Crecida y la Canasta a media anchura, en una fila; en
    /// el escalón 3, 2 barras y un chevron con el resto.
    /// </summary>
    internal static class VictoryBarsRule
    {
        /// <summary>Barras como mucho a la vista en el panel (02 §6: «máximo 3 barras visibles, el resto en un chevron»).</summary>
        public const int MaxVisible = 3;

        /// <summary>Las dos primeras barras van a media anchura, en una fila.</summary>
        public static bool HalfWidth(int compact, int bars)
        {
            return compact >= GameScreen.VictoryCompactText && bars >= 2;
        }

        /// <summary>Barras a la vista.</summary>
        public static int Visible(int compact, int bars)
        {
            return Mathf.Min(bars, compact >= GameScreen.VictoryCompactRow ? 2 : MaxVisible);
        }

        /// <summary>Lleva el chevron con las barras que no se ven.</summary>
        public static bool Chevron(int compact, int bars)
        {
            return bars > Visible(compact, bars);
        }
    }

    /// <summary>
    /// Las barras del panel único de la victoria (tarea B1b; D-033, puntos 8-10; 02 §6; 06 §4.3-§4.4), entre la Cosecha y la
    /// Cosecha doble. Cada barra es una línea de texto y una pista de agua de 8 dp: la réplica de la Crecida («Crecida {n}/10», el
    /// agua que sube de la barra de antes a la de ahora de 400 a 900 ms, las muescas de los hitos 3 y 6, la Hoja escudo a la
    /// izquierda y, a la derecha, el chip del hito) y «Canasta · {sitio} k/10» (de 900 a 1.500 ms; sin sitio si no cabe en una
    /// línea a 12 sp [P]). Al pagarse un hito o abrirse sola la Canasta del <c>10k</c>, su contenido salta en lugar de la línea de
    /// texto (sin reservar alto) y vuela al saldo y a la bandeja. Un toque en Siguiente las completa (<see cref="Complete"/>). Sin
    /// raycast; sin asignaciones por fotograma (los textos se ponen al empezar y al acabar cada barra).
    /// </summary>
    internal sealed class VictoryBars
    {
        /// <summary>Cuerpo de las etiquetas (sp; bajan hasta 12 si no caben).</summary>
        public const float LabelSp = 14f;

        /// <summary>Cuerpo de las cifras de los premios (sp).</summary>
        public const float ItemSp = 13f;

        /// <summary>Alto de la pista de agua (dp).</summary>
        public const float TrackDp = 8f;

        /// <summary>Lado de los iconos de los premios (dp).</summary>
        public const float IconDp = 18f;

        /// <summary>Hueco entre barras y bajo el bloque (dp).</summary>
        public const float GapDp = 4f;

        /// <summary>Hueco entre la etiqueta y la pista (dp).</summary>
        public const float TrackGapDp = 2f;

        /// <summary>Hoja escudo a la izquierda de la pista de la Crecida (dp).</summary>
        public const float LeafDp = 18f;

        /// <summary>Chevron de las barras que no se ven (dp).</summary>
        public const float ChevronDp = 20f;

        /// <summary>Empieza a subir la Crecida (ms desde la pantalla de victoria; D-033, punto 10).</summary>
        public const float StreakStartMs = 400f;

        /// <summary>Termina la Crecida.</summary>
        public const float StreakEndMs = 900f;

        /// <summary>Empieza la Canasta.</summary>
        public const float BasketStartMs = 900f;

        /// <summary>La Canasta llega a k/10 y, en el <c>10k</c>, se abre.</summary>
        public const float BasketOpenMs = 1200f;

        /// <summary>Termina la Canasta (su contenido ya llegó).</summary>
        public const float BasketEndMs = 1500f;

        /// <summary>Lo que tarda en volar un premio al saldo o a la bandeja (ms).</summary>
        public const float FlyMs = 300f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Row _streak;
        private readonly Row _basket;
        private readonly Image _chevron;
        private readonly Strip _burst;
        private readonly Flyer[] _flyers = new Flyer[10];
        private readonly VictoryBarsPlan _plan = new VictoryBarsPlan();
        private readonly List<RewardItem> _chipItems = new List<RewardItem>(1);
        private float _ms = -1f;
        private float _endMs;
        private int _landedSeeds;
        private bool _reduce;
        private float _width;
        private Row? _bursting;
        private Row? _burstOther;
        private Vector2 _seedTarget;
        private Vector2 _stockTarget;
        private Func<RectTransform, Vector2>? _toCanvas;

        public VictoryBars(RectTransform panel, RectTransform flyLayer, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("Bars", panel);
            _streak = new Row("StreakBar", _root, art, Palette.FamilyWater.Base, true);
            _basket = new Row("BasketBar", _root, art, Palette.FamilyLeaf.Base, false);
            _chevron = UiBuilder.Image("Chevron", _root, art.FingerArrowHead, Palette.FamilyWood.Deep);
            _chevron.preserveAspect = true;
            _chevron.gameObject.SetActive(false);
            _burst = new Strip("Opening", _root);
            _burst.Root.gameObject.SetActive(false);
            for (int i = 0; i < _flyers.Length; i++)
            {
                _flyers[i] = new Flyer(flyLayer, i);
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>Lo que llevan las barras de esta victoria.</summary>
        public VictoryBarsPlan Plan => _plan;

        /// <summary>Contenedor de las barras en el panel.</summary>
        public RectTransform Root => _root;

        /// <summary>Barra de la Crecida (tests).</summary>
        internal RectTransform StreakRect => _streak.Root;

        /// <summary>Barra de la Canasta (tests).</summary>
        internal RectTransform BasketRect => _basket.Root;

        /// <summary>Texto de la barra de la Crecida (tests).</summary>
        internal string StreakText => _streak.Label.text;

        /// <summary>Texto de la barra de la Canasta (tests).</summary>
        internal string BasketText => _basket.Label.text;

        /// <summary>Opacidad del texto de la barra de la Crecida (tests: vuelve a 1 tras el salto de un premio).</summary>
        internal float StreakTextAlpha => _streak.Label.color.a;

        /// <summary>Opacidad del texto de la barra de la Canasta (tests).</summary>
        internal float BasketTextAlpha => _basket.Label.color.a;

        /// <summary>Lo lleno de la pista de la Crecida ahora (0-1; tests).</summary>
        internal float StreakFill => _streak.Fill01;

        /// <summary>Lo lleno de la pista de la Canasta ahora (0-1; tests).</summary>
        internal float BasketFill => _basket.Fill01;

        /// <summary>Textos del chip del hito que se ve (tests).</summary>
        internal void ChipTexts(List<string> into)
        {
            _streak.Chip.Texts(into);
        }

        /// <summary>Hay premios saltando en lugar de una línea (tests).</summary>
        internal bool Bursting => _burst.Root.gameObject.activeSelf;

        /// <summary>Las barras van a media anchura (tests).</summary>
        public bool Half { get; private set; }

        /// <summary>
        /// La Crecida y la Canasta van en una fila a media anchura en todos los escalones, también en el 0 y el 1 (lo decide la
        /// victoria compacta cuando a lo ancho no caben sin compactar lo demás) [P].
        /// </summary>
        public bool ForceHalf { get; set; }

        /// <summary>Se ve el chevron (tests).</summary>
        public bool ChevronShown => _chevron.gameObject.activeSelf;

        /// <summary>Alto del bloque colocado, con su hueco de abajo (dp; 0 sin barras).</summary>
        public float Height { get; private set; }

        /// <summary>Quedan animaciones por terminar (un toque en Siguiente las completa y espera 250 ms; D-033, punto 9).</summary>
        public bool Pending => _ms >= 0f && _ms < _endMs;

        /// <summary>
        /// Lo pendiente lleva un premio (un hito pagado o la Canasta que se abre): un toque en Siguiente lo completa y enseña el
        /// estado final 250 ms antes de seguir (D-033, punto 9) [P]; si solo sube el agua, Siguiente la completa y sigue en el acto.
        /// </summary>
        public bool PendingReward => Pending && (_plan.StreakPaid || _plan.BasketOpens);

        /// <summary>Semillas de las barras que aún no llegaron al saldo (D-011: nada se da por ganado antes de que se vea).</summary>
        public int PendingSeeds => _root.gameObject.activeSelf && _ms >= 0f ? Mathf.Max(0, _plan.PaidSeeds - _landedSeeds) : 0;

        /// <summary>
        /// Arma las barras del plan (ya construido con <see cref="Plan"/>) en su estado de partida, sin animarlas aún.
        /// <paramref name="toCanvas"/> da el centro de un rectángulo en dp del lienzo (para el vuelo).
        /// </summary>
        public void Prepare(bool reduceMotion, Func<RectTransform, Vector2> toCanvas)
        {
            _reduce = reduceMotion;
            _toCanvas = toCanvas;
            _landedSeeds = 0;
            _bursting = null;
            _burstOther = null;
            _burst.Root.gameObject.SetActive(false);
            for (int i = 0; i < _flyers.Length; i++)
            {
                _flyers[i].Hide();
            }

            bool any = _plan.BarCount > 0;
            _root.gameObject.SetActive(any);
            if (!any)
            {
                _ms = -1f;
                _endMs = 0f;
                return;
            }

            _streak.Root.gameObject.SetActive(_plan.Streak);
            if (_plan.Streak)
            {
                _streak.SetLabel(UiText.StreakBar(_plan.StreakFrom), UiText.StreakBar(_plan.StreakFrom));
                // El chip: el premio del hito (pagado o el siguiente); la Canasta del hito 6, con su cesta (su contenido salta al pagarse).
                _chipItems.Clear();
                if (_plan.StreakMilestone == StreakRecord.HitBasket)
                {
                    _chipItems.Add(new RewardItem(RewardIcon.Basket, 0));
                }
                else if (_plan.StreakReward.Count > 0)
                {
                    _chipItems.Add(_plan.StreakReward[0]);
                }

                _streak.Chip.Set(_chipItems, _art, 1);
                _streak.SetLeaf(_plan.Shield);
                _streak.SetFill(_plan.StreakFrom / (float)StreakRecord.MaxWins);
                _streak.SetTextAlpha(1f);
            }

            _basket.Root.gameObject.SetActive(_plan.Basket);
            if (_plan.Basket)
            {
                int k = _plan.BasketCount - 1;
                _basket.SetLabel(UiText.BasketBar(_plan.BasketPlace, k), UiText.BasketBar(null, k));
                _basket.Chip.Set(_plan.BasketReward, _art, 0); // el contenido sale al abrirse, no en la fila (sin alto propio)
                _basket.SetLeaf(false);
                _basket.SetFill(k / (float)BasketRules.Every);
                _basket.SetTextAlpha(1f);
            }

            // El final: lo último que pasa, el agua de la Canasta o el aterrizaje del último premio que vuela (D-011: el saldo lo enseña al llegar).
            float streakEnd = _plan.StreakPaid ? StreakEndMs + FlightMs(_plan.StreakReward.Count) : StreakEndMs;
            float basketEnd = _plan.Basket ? (_plan.BasketOpens ? Mathf.Max(BasketEndMs, BasketOpenMs + FlightMs(_plan.BasketReward.Count)) : BasketOpenMs) : 0f;
            _endMs = Mathf.Max(streakEnd, basketEnd);
            _ms = 0f;
            if (_reduce)
            {
                Complete(); // Reducir movimiento: corte directo al estado final
            }
        }

        /// <summary>Oculta el bloque (fuera de la victoria).</summary>
        public void Hide()
        {
            _root.gameObject.SetActive(false);
            _ms = -1f;
            _bursting = null;
            _burstOther = null;
            _plan.Clear();
            for (int i = 0; i < _flyers.Length; i++)
            {
                _flyers[i].Hide();
            }
        }

        /// <summary>
        /// Las barras no caben en la victoria (ni con su último escalón): no salen y lo que pagan llega al saldo sin animarse
        /// (<see cref="Complete"/>). Devuelve 0 (el alto que ocupan).
        /// </summary>
        public float Collapse()
        {
            _ms = -1f; // sin animación ni sonido: lo pagado ya cuenta en el saldo
            _landedSeeds = _plan.PaidSeeds;
            EndBurst();
            _root.gameObject.SetActive(false);
            Height = 0f;
            return 0f;
        }

        /// <summary>Destino del vuelo: el saldo de semillas y la bandeja (dp del lienzo).</summary>
        public void SetTargets(Vector2 seeds, Vector2 stock)
        {
            _seedTarget = seeds;
            _stockTarget = stock;
        }

        /// <summary>
        /// Coloca las barras de <paramref name="width"/> dp con el escalón <paramref name="compact"/> desde (<paramref name="x"/>,
        /// <paramref name="y"/>) del panel y devuelve su alto con el hueco de abajo (0 sin barras). Mide con TextMeshPro: al colocar,
        /// nunca por fotograma.
        /// </summary>
        public float Layout(float x, float y, float width, int compact)
        {
            int bars = _plan.BarCount;
            _width = width;
            if (bars == 0 || !_root.gameObject.activeSelf)
            {
                Height = 0f;
                return 0f;
            }

            int visible = VictoryBarsRule.Visible(compact, bars);
            Half = (ForceHalf || VictoryBarsRule.HalfWidth(compact, bars)) && _plan.Streak && _plan.Basket;
            bool chevron = VictoryBarsRule.Chevron(compact, bars);
            _chevron.gameObject.SetActive(chevron);
            float right = chevron ? ChevronDp + GapDp : 0f;
            float h;
            if (Half)
            {
                float half = (width - right - GapDp) * 0.5f;
                h = Mathf.Max(_streak.Layout(half), _basket.Layout(half));
                UiBuilder.Place(_streak.Root, 0f, 0f, half, h);
                UiBuilder.Place(_basket.Root, half + GapDp, 0f, half, h);
            }
            else
            {
                h = 0f;
                int shown = 0;
                if (_plan.Streak && shown < visible)
                {
                    float a = _streak.Layout(width - right);
                    UiBuilder.Place(_streak.Root, 0f, h, width - right, a);
                    h += a + GapDp;
                    shown++;
                }

                if (_plan.Basket && shown < visible)
                {
                    float b = _basket.Layout(width - right);
                    UiBuilder.Place(_basket.Root, 0f, h, width - right, b);
                    h += b + GapDp;
                }

                h = Mathf.Max(0f, h - GapDp);
            }

            if (chevron)
            {
                UiBuilder.Place(_chevron.rectTransform, width - ChevronDp, (h - ChevronDp) * 0.5f, ChevronDp, ChevronDp);
                _chevron.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            }

            UiBuilder.Place(_root, x, y, width, h);
            Height = h + GapDp;
            return Height;
        }

        /// <summary>
        /// Alto del bloque más alto posible con el escalón <paramref name="compact"/> (dp, con su hueco): dos barras (la Crecida y la
        /// Canasta), a lo ancho una sobre otra o, desde el escalón 2, en una fila; cada una, su línea de texto a 14 sp × la escala (las
        /// etiquetas nunca pasan de una línea: bajan a 12 sp y la Canasta pierde el sitio) y la pista. No depende del idioma más que
        /// por el alto de la línea.
        /// </summary>
        public float WorstHeight(int compact)
        {
            const int bars = 2;
            float row = _streak.RowHeight();
            float h = ForceHalf || VictoryBarsRule.HalfWidth(compact, bars) ? row : row + GapDp + row;
            return h + GapDp;
        }

        /// <summary>Avanza las animaciones (ms desde la pantalla de victoria). Sin asignaciones salvo los cambios de texto.</summary>
        public void Tick(float dtMs)
        {
            if (_ms < 0f || !_root.gameObject.activeSelf)
            {
                return;
            }

            float before = _ms;
            _ms += dtMs;
            Apply(before, _ms);
            for (int i = 0; i < _flyers.Length; i++)
            {
                if (_flyers[i].Tick(dtMs))
                {
                    _landedSeeds += _flyers[i].Seeds;
                }
            }

            if (before < _endMs && _ms >= _endMs)
            {
                _landedSeeds = _plan.PaidSeeds; // terminada la secuencia, todo lo pagado ya se ve en el saldo
                EndBurst();
            }
        }

        /// <summary>Lleva las barras a su estado final al instante (Siguiente, Inicio o Reducir movimiento; D-033, punto 9).</summary>
        public void Complete()
        {
            if (_ms < 0f)
            {
                return;
            }

            float before = _ms;
            _ms = Mathf.Max(_ms, _endMs);
            Apply(before, _ms);
            for (int i = 0; i < _flyers.Length; i++)
            {
                _flyers[i].Land();
            }

            _landedSeeds = _plan.PaidSeeds; // todo llegó
            EndBurst();
        }

        // Lo que toca entre before y now: el agua de cada barra, sus textos al acabar y el premio que salta y vuela.
        private void Apply(float before, float now)
        {
            if (_plan.Streak)
            {
                float t = Mathf.Clamp01((now - StreakStartMs) / (StreakEndMs - StreakStartMs));
                float from = _plan.StreakFrom / (float)StreakRecord.MaxWins;
                float to = _plan.StreakTo / (float)StreakRecord.MaxWins;
                _streak.SetFill(Mathf.Lerp(from, to, Ease.OutCubic(t)));
                if (before < StreakEndMs && now >= StreakEndMs)
                {
                    _streak.SetLabel(UiText.StreakBar(_plan.StreakTo), UiText.StreakBar(_plan.StreakTo));
                    _streak.Relayout();
                    if (_plan.StreakPaid)
                    {
                        AF.Audio.AudioCues.Play(AF.Audio.SfxId.StreakUp); // la nutria gigante chilla en cada hito (07 §9.2)
                        Burst(_streak, _plan.StreakReward);
                    }
                }
            }

            if (_plan.Basket)
            {
                float t = Mathf.Clamp01((now - BasketStartMs) / (BasketOpenMs - BasketStartMs));
                float from = (_plan.BasketCount - 1) / (float)BasketRules.Every;
                float to = _plan.BasketCount / (float)BasketRules.Every;
                _basket.SetFill(Mathf.Lerp(from, to, Ease.OutCubic(t)));
                if (before < BasketOpenMs && now >= BasketOpenMs)
                {
                    _basket.SetLabel(UiText.BasketBar(_plan.BasketPlace, _plan.BasketCount), UiText.BasketBar(null, _plan.BasketCount));
                    _basket.Relayout();
                    if (_plan.BasketOpens)
                    {
                        AF.Audio.AudioCues.Play(AF.Audio.SfxId.BasketOpen); // la Canasta se abre sola (D-033, punto 8)
                        Burst(_basket, _plan.BasketReward);
                    }
                }
            }

            if (_bursting != null)
            {
                float start = ReferenceEquals(_bursting, _streak) ? StreakEndMs : BasketOpenMs;
                float ms = now - start;
                _burst.Root.localScale = Vector3.one * (_reduce || ms > 200f ? 1f : 1f + (0.2f * Mathf.Sin((Mathf.Max(0f, ms) / 200f) * Mathf.PI)));
                if (ms >= FlyMs)
                {
                    EndBurst();
                }
            }
        }

        // El premio salta en lugar de la línea de texto de su fila (centrado en el bloque, sin reservar alto) y vuela al saldo
        // (semillas) o a la bandeja (comodines).
        private void Burst(Row row, List<RewardItem> items)
        {
            EndBurst();
            if (items.Count == 0)
            {
                return;
            }

            _burst.Set(items, _art, int.MaxValue);
            float w = _burst.Width();
            float lineH = Mathf.Max(row.LineHeight, IconDp);
            Vector2 rowPos = row.Root.anchoredPosition;
            float rowW = row.Root.sizeDelta.x;
            float y = -rowPos.y + ((row.LineHeight - lineH) * 0.5f);
            float x = Mathf.Clamp(rowPos.x + ((rowW - w) * 0.5f), 0f, Mathf.Max(0f, _width - w)); // sobre su fila
            _burst.Place(x, y, w, lineH);
            _burst.Root.gameObject.SetActive(true);
            _burst.Root.SetAsLastSibling();
            row.SetTextAlpha(0f);
            _bursting = row;
            if (Half && w > rowW)
            {
                // A media anchura, si el premio no cabe en su mitad, la otra fila apaga su texto mientras salta (no se pisan).
                _burstOther = ReferenceEquals(row, _streak) ? _basket : _streak;
                _burstOther.SetTextAlpha(0f);
            }
            Func<RectTransform, Vector2>? toCanvas = _toCanvas;
            if (toCanvas == null)
            {
                return;
            }

            for (int i = 0; i < _burst.Count; i++)
            {
                RewardItem item = _burst.ItemAt(i);
                Flyer? flyer = FreeFlyer();
                if (flyer == null)
                {
                    break;
                }

                bool seeds = item.Icon == RewardIcon.Seed;
                flyer.Fly(_burst.IconSprite(i), toCanvas(_burst.IconRect(i)), seeds ? _seedTarget : _stockTarget, i * FlyStaggerMs,
                    seeds ? item.Amount : 0, _reduce);
            }
        }

        private void EndBurst()
        {
            if (_bursting != null)
            {
                _bursting.SetTextAlpha(1f);
                _bursting = null;
            }

            if (_burstOther != null)
            {
                _burstOther.SetTextAlpha(1f);
                _burstOther = null;
            }

            _burst.Root.gameObject.SetActive(false);
        }

        // Del salto de una tira de n premios al aterrizaje del último (ms): 30 ms entre premio y premio y el vuelo (la Canasta grande, de
        // 4 premios, aterriza a los 1.590 ms, dentro de la fila 1,40-1,60 de 07 §5.4).
        private static float FlightMs(int n)
        {
            return (FlyStaggerMs * Mathf.Max(0, Mathf.Min(n, 5) - 1)) + FlyMs;
        }

        /// <summary>Entre el despegue de un premio y el del siguiente (ms).</summary>
        private const float FlyStaggerMs = 30f;

        private Flyer? FreeFlyer()
        {
            for (int i = 0; i < _flyers.Length; i++)
            {
                if (!_flyers[i].Busy)
                {
                    return _flyers[i];
                }
            }

            return null;
        }

        // ------------------------------------------------------------------------------------------------------------ premios

        // Una tira de premios en una línea: icono y, si lo lleva, su cifra («+300», «×3»).
        private sealed class Strip
        {
            private const int MaxItems = 5;
            private readonly Image[] _icons = new Image[MaxItems];
            private readonly TextMeshProUGUI[] _amounts = new TextMeshProUGUI[MaxItems];
            private readonly RewardItem[] _shown = new RewardItem[MaxItems];

            public Strip(string name, RectTransform parent)
            {
                Root = UiBuilder.Rect(name, parent);
                for (int i = 0; i < MaxItems; i++)
                {
                    _icons[i] = UiBuilder.Image("Icon" + i, Root, null, Color.white);
                    _icons[i].preserveAspect = true;
                    _amounts[i] = UiBuilder.Label("Amount" + i, Root, TextStyle.PlainBold, string.Empty, ItemSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
                    _icons[i].gameObject.SetActive(false);
                    _amounts[i].gameObject.SetActive(false);
                }
            }

            public RectTransform Root { get; }

            public int Count { get; private set; }

            public RewardItem ItemAt(int i) => _shown[i];

            public RectTransform IconRect(int i) => _icons[i].rectTransform;

            public Sprite? IconSprite(int i) => _icons[i].sprite;

            public void Texts(List<string> into)
            {
                if (!Root.gameObject.activeInHierarchy)
                {
                    return;
                }

                for (int i = 0; i < Count; i++)
                {
                    into.Add(_amounts[i].gameObject.activeSelf ? _amounts[i].text : _shown[i].Icon.ToString());
                }
            }

            // Los primeros max premios de items (0 = ninguno).
            public void Set(List<RewardItem> items, ProceduralArt art, int max)
            {
                Count = Mathf.Min(items.Count, Mathf.Clamp(max, 0, MaxItems));

                for (int i = 0; i < MaxItems; i++)
                {
                    bool on = i < Count;
                    _icons[i].gameObject.SetActive(on);
                    if (!on)
                    {
                        _amounts[i].gameObject.SetActive(false);
                        continue;
                    }

                    RewardItem item = items[i];
                    _shown[i] = item;
                    _icons[i].sprite = SpriteFor(item.Icon, art);
                    _icons[i].color = Color.white;
                    bool amount = item.Amount > 0;
                    _amounts[i].gameObject.SetActive(amount);
                    _amounts[i].fontSize = ItemSp * UiBuilder.TextScale;
                    if (amount)
                    {
                        _amounts[i].text = item.Icon == RewardIcon.Seed ? UiText.VictoryBarSeeds(item.Amount) : UiText.VictoryBarUnits(item.Amount);
                    }
                }
            }

            public void SetAlpha(float a)
            {
                for (int i = 0; i < Count; i++)
                {
                    _icons[i].color = new Color(1f, 1f, 1f, a);
                    Color c = Palette.FamilyWood.Deep;
                    c.a = a;
                    _amounts[i].color = c;
                }
            }

            // Ancho de la tira en una línea (dp).
            public float Width()
            {
                float w = 0f;
                for (int i = 0; i < Count; i++)
                {
                    w += IconDp;
                    if (_amounts[i].gameObject.activeSelf)
                    {
                        w += 1f + AmountWidth(i);
                    }

                    if (i < Count - 1)
                    {
                        w += 6f;
                    }
                }

                return w;
            }

            public float LineHeight()
            {
                float h = IconDp;
                for (int i = 0; i < Count; i++)
                {
                    if (_amounts[i].gameObject.activeSelf)
                    {
                        h = Mathf.Max(h, Mathf.Ceil(_amounts[i].GetPreferredValues(_amounts[i].text, float.PositiveInfinity, float.PositiveInfinity).y));
                    }
                }

                return h;
            }

            // Coloca la tira en (x, y) del padre con w × h, con el pivote al centro (para el salto).
            public void Place(float x, float y, float w, float h)
            {
                RectTransform rt = Root;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x + (w * 0.5f), -(y + (h * 0.5f)));
                rt.sizeDelta = new Vector2(w, h);
                rt.localScale = Vector3.one;
                float x0 = 0f;
                for (int i = 0; i < Count; i++)
                {
                    UiBuilder.Place(_icons[i].rectTransform, x0, (h - IconDp) * 0.5f, IconDp, IconDp);
                    x0 += IconDp;
                    if (_amounts[i].gameObject.activeSelf)
                    {
                        float aw = AmountWidth(i);
                        UiBuilder.Place(_amounts[i].rectTransform, x0 + 1f, 0f, aw + 1f, h);
                        x0 += 1f + aw;
                    }

                    x0 += 6f;
                }
            }

            private float AmountWidth(int i)
            {
                return Mathf.Ceil(_amounts[i].GetPreferredValues(_amounts[i].text, float.PositiveInfinity, float.PositiveInfinity).x);
            }

            private static Sprite? SpriteFor(RewardIcon icon, ProceduralArt art)
            {
                switch (icon)
                {
                    case RewardIcon.Seed: return art.IconSeed;
                    case RewardIcon.Undo: return art.IconUndo;
                    case RewardIcon.Hint: return art.IconMorpho;
                    case RewardIcon.Reveal: return art.IconFirefly;
                    case RewardIcon.ExtraCuia: return art.IconCuiaPlus;
                    case RewardIcon.Basket: return art.IconBasket;
                    default: return null;
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------------- fila

        // Una barra: la línea de texto («Crecida {n}/10» con su chip a la derecha, o «Canasta · {sitio} k/10») y, debajo, la pista con
        // el agua (en la Crecida, con las muescas de los hitos 3 y 6 y la Hoja escudo a la izquierda).
        private sealed class Row
        {
            private readonly Image _track;
            private readonly Image _fill;
            private readonly Image _leaf;
            private readonly Image[] _notches = new Image[2];
            private readonly bool _streak;
            private string _full = string.Empty;
            private string _short = string.Empty;
            private float _width;
            private float _trackW;
            private bool _leafOn;

            public Row(string name, RectTransform parent, ProceduralArt art, Color water, bool streak)
            {
                _streak = streak;
                Root = UiBuilder.Rect(name, parent);
                Label = UiBuilder.Label("Label", Root, TextStyle.PlainBold, string.Empty, LabelSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
                _track = UiBuilder.Sliced("Track", Root, art.RoundedRect, new Color(Palette.FamilyWood.Edge.r, Palette.FamilyWood.Edge.g, Palette.FamilyWood.Edge.b, 0.45f));
                _track.pixelsPerUnitMultiplier = 6f;
                _fill = UiBuilder.Sliced("Water", _track.transform, art.RoundedRect, water);
                _fill.pixelsPerUnitMultiplier = 6f;
                for (int i = 0; i < _notches.Length; i++)
                {
                    _notches[i] = UiBuilder.Image("Notch" + (i + 1), _track.transform, null, Palette.Cream);
                    _notches[i].gameObject.SetActive(streak);
                }

                _leaf = UiBuilder.Image("Shield", Root, art.LilyPad, Color.white);
                _leaf.preserveAspect = true;
                _leaf.gameObject.SetActive(false);
                Chip = new Strip("Chip", Root);
            }

            public RectTransform Root { get; }

            public TextMeshProUGUI Label { get; }

            public Strip Chip { get; }

            public float Fill01 { get; private set; }

            /// <summary>Alto de la línea de texto colocada (dp).</summary>
            public float LineHeight { get; private set; }

            // La etiqueta con su versión corta (la Canasta sin sitio) por si la larga no cabe en una línea a 12 sp.
            public void SetLabel(string full, string shortText)
            {
                _full = full;
                _short = shortText;
            }

            public void SetLeaf(bool on)
            {
                _leafOn = on && _streak;
                _leaf.gameObject.SetActive(_leafOn);
            }

            public void SetTextAlpha(float a)
            {
                Color c = Palette.FamilyWood.Deep;
                c.a = a;
                Label.color = c;
                Chip.SetAlpha(a);
            }

            public void SetFill(float t)
            {
                Fill01 = Mathf.Clamp01(t);
                float w = Mathf.Max(0f, _trackW - 2f) * Fill01;
                _fill.gameObject.SetActive(w > 0.5f);
                UiBuilder.Place(_fill.rectTransform, 1f, 1f, Mathf.Max(TrackDp - 2f, w), TrackDp - 2f);
            }

            // Alto de una fila con la línea a 14 sp × la escala (el más alto posible: las etiquetas nunca pasan de una línea).
            public float RowHeight()
            {
                TextMeshProUGUI t = Label;
                float keep = t.fontSize;
                t.fontSize = LabelSp * UiBuilder.TextScale;
                float line = Mathf.Ceil(t.GetPreferredValues("Ag", float.PositiveInfinity, float.PositiveInfinity).y);
                t.fontSize = keep;
                return Mathf.Max(line, IconDp) + TrackGapDp + TrackDp;
            }

            public void Relayout()
            {
                if (_width > 0f)
                {
                    Layout(_width);
                }
            }

            // Coloca la fila en width dp: la etiqueta en una línea, de 14 a 12 sp (con la escala), a la izquierda del chip; si ni a 12
            // cabe, la versión corta (la Canasta sin sitio) [P]. Devuelve su alto.
            public float Layout(float width)
            {
                _width = width;
                float scale = UiBuilder.TextScale;
                float chipW = Chip.Count > 0 ? Chip.Width() + 6f : 0f;
                float avail = Mathf.Max(24f, width - chipW);
                TextMeshProUGUI t = Label;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = false;
                string text = _full;
                float size = LabelSp * scale;
                t.fontSize = size;
                if (t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x > avail)
                {
                    size = UiBuilder.MinTextSp * scale;
                    t.fontSize = size;
                    if (t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x > avail && _short.Length > 0)
                    {
                        text = _short;
                    }
                }

                if (!string.Equals(t.text, text, StringComparison.Ordinal))
                {
                    t.text = text;
                }

                float line = Mathf.Max(Mathf.Ceil(t.GetPreferredValues("Ag", float.PositiveInfinity, float.PositiveInfinity).y), IconDp);
                t.fontSize = size;
                LineHeight = line;
                UiBuilder.Place(t.rectTransform, 0f, 0f, avail, line);
                if (Chip.Count > 0)
                {
                    float cw = Chip.Width();
                    Chip.Place(width - cw, 0f, cw, line);
                }

                float trackY = line + TrackGapDp;
                float leafW = _leafOn ? LeafDp + 4f : 0f;
                _trackW = width - leafW;
                UiBuilder.Place(_track.rectTransform, leafW, trackY, _trackW, TrackDp);
                if (_leafOn)
                {
                    UiBuilder.Place(_leaf.rectTransform, 0f, trackY + (TrackDp * 0.5f) - (LeafDp * 0.5f), LeafDp, LeafDp);
                }

                for (int i = 0; i < _notches.Length; i++)
                {
                    float x = (_trackW * ((i + 1) * StreakRecord.HitSeeds) / StreakRecord.MaxWins) - 1f;
                    UiBuilder.Place(_notches[i].rectTransform, x, 0f, 2f, TrackDp);
                }

                SetFill(Fill01);
                return trackY + TrackDp;
            }
        }

        // ------------------------------------------------------------------------------------------------------------- vuelo

        // Un premio que vuela de la tira al saldo o a la bandeja (300 ms, con su retraso) y se desvanece al llegar; sus semillas
        // cuentan en el saldo al llegar (D-011).
        private sealed class Flyer
        {
            private readonly Image _image;
            private Vector2 _from;
            private Vector2 _to;
            private float _ms = -1f;
            private float _delay;

            public Flyer(RectTransform layer, int i)
            {
                _image = UiBuilder.Image("Flyer" + i, layer, null, Color.white);
                _image.preserveAspect = true;
                _image.gameObject.SetActive(false);
            }

            public bool Busy => _ms >= 0f;

            public int Seeds { get; private set; }

            public void Fly(Sprite? sprite, Vector2 from, Vector2 to, float delayMs, int seeds, bool reduce)
            {
                _image.sprite = sprite;
                _from = from;
                _to = to;
                _delay = reduce ? 0f : delayMs;
                Seeds = seeds;
                _ms = reduce ? FlyMs : 0f;
                _image.gameObject.SetActive(!reduce);
                Place(0f);
            }

            // true al aterrizar en este paso.
            public bool Tick(float dtMs)
            {
                if (_ms < 0f)
                {
                    return false;
                }

                _ms += dtMs;
                float t = Mathf.Clamp01((_ms - _delay) / FlyMs);
                Place(t);
                return t >= 1f && Land();
            }

            // Lo deja en su destino; true si aún volaba.
            public bool Land()
            {
                if (_ms < 0f)
                {
                    return false;
                }

                _ms = -1f;
                _image.gameObject.SetActive(false);
                return true;
            }

            public void Hide()
            {
                _ms = -1f;
                Seeds = 0;
                _image.gameObject.SetActive(false);
            }

            private void Place(float t)
            {
                float e = Ease.OutCubic(t);
                Vector2 p = Vector2.LerpUnclamped(_from, _to, e) + new Vector2(0f, -40f * Mathf.Sin(t * Mathf.PI)); // un arco hacia arriba
                float size = Mathf.Lerp(IconDp, IconDp * 0.7f, t);
                RectTransform rt = _image.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(p.x, -p.y);
                rt.sizeDelta = new Vector2(size, size);
                _image.color = new Color(1f, 1f, 1f, t < 0.85f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.85f) / 0.15f));
            }
        }
    }
}
