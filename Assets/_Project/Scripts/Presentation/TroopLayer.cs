#nullable enable
using System.Collections.Generic;
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// La tropa de «Llenar y teñir» (guion de la intro §4.4, §7.1-§7.3 y §11) en un Canvas anidado propio entre el tablero y
    /// el muelle: la rama en primer plano (Bézier de 5 dp con corteza y 4 hojas, sin reflejo en el río, que sale del follaje
    /// de las esquinas de arriba: la tropa nunca cruza el río), a panel.top − 8 para <c>fila</c> y <c>sacude</c> y a
    /// hudBottom + 6 para <c>cuelga</c>, y hasta 6 miquitos con su rig recortable (<see cref="MonkeyRig"/>) guiados por las
    /// trayectorias del planificador (<see cref="IntroChoreography"/>): entran por la rama cuando la fila 1 está llena (larga)
    /// o ya están posados (corta), frenan sobre sus columnas, sueltan la fruta desde la mano y salen de espaldas en 300 ms.
    /// Todo es función del tiempo de la intro (sin estado por fotograma), así que una captura en t es siempre la misma. Nada
    /// depende de la selección, de un vertido ni de la legalidad (D-002); nada recibe toques.
    /// </summary>
    public sealed class TroopLayer
    {
        private const float BranchOverhangDp = 12f;
        private const float BranchWidthDp = 6f;
        private const float LeafWidthDp = 12f;
        private const float LeafHeightDp = 20f;
        private const float HangDropMs = 120f;
        private const int Leaves = 4;

        private static readonly Color Bark = Palette.Hex(0x7A5232);
        private static readonly Color BarkTip = Palette.Hex(0x8E6440);
        private static readonly Color InkLine = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);
        private static readonly Color Liana = Palette.Hex(0x4E6B2E);

        private readonly RectTransform _layer;
        private readonly ProceduralArt _art;
        private readonly BezierStripGraphic _branch;
        private readonly Image[] _leaves = new Image[Leaves];
        private readonly BezierStripGraphic[] _lianas = new BezierStripGraphic[IntroChoreography.MaxMonkeys];
        private readonly MonkeyRig[] _rigs = new MonkeyRig[IntroChoreography.MaxMonkeys];
        private readonly float[] _startFacing = new float[IntroChoreography.MaxMonkeys];
        private readonly float[] _firstRelease = new float[IntroChoreography.MaxMonkeys];
        private readonly Vector3[] _corners = new Vector3[4];
        private MonkeyArt? _look;
        private FaunaSpecies? _species;
        private IntroChoreography? _plan;
        private float _canvasW = 360f;
        private float _branchY;
        private float _corner;
        private float _bodyS = 40f;
        private float _gripY;
        private bool _active;
        private float _alpha = -1f;

        /// <summary>Crea la capa (oculta) dentro de <paramref name="parent"/> (la raíz del lienzo, entre el tablero y el muelle).</summary>
        public TroopLayer(RectTransform parent, ProceduralArt art)
        {
            _art = art;
            var go = new GameObject("TroopLayer", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            _layer = (RectTransform)go.transform;
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = Vector2.zero;
            _layer.offsetMax = Vector2.zero;

            for (int m = 0; m < _lianas.Length; m++)
            {
                _lianas[m] = Strip("Liana" + m, 6);
                _lianas[m].SetColors(Liana, ColorMath.Darken(Liana, 0.2f), 0.8f, InkLine, 0.6f);
            }

            _branch = Strip("Branch", 20);
            _branch.SetColors(Bark, BarkTip, 0.5f, InkLine, 1f);
            for (int i = 0; i < Leaves; i++)
            {
                var leafGo = new GameObject("Leaf" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                leafGo.transform.SetParent(_layer, false);
                Image img = leafGo.GetComponent<Image>();
                img.sprite = art.LeafBit;
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(LeafWidthDp, LeafHeightDp);
                _leaves[i] = img;
            }

            for (int m = 0; m < _rigs.Length; m++)
            {
                _rigs[m] = new MonkeyRig(_layer, m);
            }

            go.SetActive(false);
        }

        /// <summary>Raíz de la capa (orden de capas y tests).</summary>
        public RectTransform Layer => _layer;

        /// <summary>La tropa está en pantalla.</summary>
        public bool Active => _active;

        /// <summary>Piezas de la especie preparada (null = dosel).</summary>
        public MonkeyArt? Look => _look;

        /// <summary>Especie preparada.</summary>
        public FaunaSpecies? Species => _species;

        /// <summary>Miquitos en pantalla.</summary>
        public int MonkeyCount => _active && _plan != null ? _plan.MonkeyCount : 0;

        /// <summary>Rig del miquito <paramref name="m"/> (tests).</summary>
        public MonkeyRig Rig(int m)
        {
            return _rigs[m];
        }

        /// <summary>
        /// Prepara la especie de la intro (null = dosel). Genera sus piezas la primera vez (≤ 10 ms) y las guarda en caché.
        /// </summary>
        public void Prepare(FaunaSpecies? species)
        {
            _species = species;
            _look = species != null ? _art.MonkeyArtFor(species) : null;
        }

        /// <summary>
        /// Coloca la rama y la tropa para el plan <paramref name="plan"/> (solo en modo rama y con una especie preparada).
        /// </summary>
        public void Begin(IntroChoreography plan, IntroInput input)
        {
            _plan = plan;
            if (plan.Perch != IntroPerch.Branch || plan.MonkeyCount == 0 || _look == null)
            {
                Hide();
                return;
            }

            _active = true;
            _alpha = -1f;
            _canvasW = input.CanvasWidth;
            _bodyS = plan.MonkeyDp;
            _branchY = plan.BranchY;
            _corner = plan.Model == TroopModel.Hang ? 1f : 6f;
            if (plan.Model == TroopModel.Hang)
            {
                // El cuerpo colgado acaba ≥ 8 dp por encima del panel (la mano libre, con la fruta, en la suelta del plan).
                float release = input.PanelTop - IntroChoreography.LowBranchAbovePanelDp - 2f;
                float room = release - (_branchY + 4f);
                _bodyS = Mathf.Max(12f, Mathf.Min(_bodyS, room / -MonkeyRig.HangHandS.y));
                _gripY = release + (MonkeyRig.HangHandS.y * _bodyS);
            }

            if (!_layer.gameObject.activeSelf)
            {
                _layer.gameObject.SetActive(true);
            }

            // Rama: parábola de esquina a esquina (sube hacia el follaje de las esquinas), como Bézier cúbica equivalente.
            float w = _canvasW;
            float yc = _branchY - _corner;
            float yPeak = yc + (2f * (3f + _corner));
            var p0 = new Vector2(-BranchOverhangDp, -yc);
            var p3 = new Vector2(w + BranchOverhangDp, -yc);
            var q = new Vector2(w * 0.5f, -yPeak);
            _branch.SetCurve(p0, p0 + ((q - p0) * (2f / 3f)), p3 + ((q - p3) * (2f / 3f)), p3, BranchWidthDp, BranchWidthDp - 1.5f);
            _branch.enabled = true;
            for (int i = 0; i < Leaves; i++)
            {
                float u = i == 0 ? 0.1f : (i == 1 ? 0.33f : (i == 2 ? 0.67f : 0.9f));
                float x = -BranchOverhangDp + (u * (w + (2f * BranchOverhangDp)));
                RectTransform rt = _leaves[i].rectTransform;
                // En la rama baja, hacia arriba (nunca hacia el panel); en la alta, colgando (nunca sobre el HUD).
                bool high = plan.Model == TroopModel.Hang;
                rt.anchoredPosition = new Vector2(x, -(BranchYAt(x) + (high ? 1.5f : -1.5f)));
                rt.localRotation = Quaternion.Euler(0f, 0f, (high ? 180f : 0f) + ((i % 2 == 0 ? 1f : -1f) * (30f + (i * 6f))));
                _leaves[i].enabled = true;
            }

            for (int m = 0; m < _rigs.Length; m++)
            {
                bool on = m < plan.MonkeyCount;
                _rigs[m].SetVisible(on);
                _lianas[m].enabled = false;
                if (!on)
                {
                    continue;
                }

                _rigs[m].SetArt(_look, _bodyS);
                float x0 = plan.MonkeyX(m, 0f);
                _startFacing[m] = plan.IsLong ? (plan.MonkeyFromRight[m] ? -1f : 1f) : (x0 < _canvasW * 0.5f ? 1f : -1f);
                _firstRelease[m] = float.MaxValue;
                for (int k = 0; k < plan.FruitCount; k++)
                {
                    if (plan.FruitMonkey[k] == m)
                    {
                        _firstRelease[m] = Mathf.Min(_firstRelease[m], plan.FruitReleaseMs[k]);
                    }
                }
            }
        }

        /// <summary>
        /// Evalúa la tropa en el instante <paramref name="t"/> de la intro con la opacidad <paramref name="alpha"/> (el fundido
        /// del salto: los restos se funden en su sitio, sin moverse).
        /// </summary>
        public void Apply(float t, float alpha)
        {
            IntroChoreography? plan = _plan;
            if (!_active || plan == null || _look == null)
            {
                return;
            }

            float exit = 1f - Mathf.Clamp01((t - plan.TotalMs) / IntroChoreography.ExitMs);
            float a = Mathf.Clamp01(alpha) * exit;
            SetDecorAlpha(a);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                PoseMonkey(plan, m, t, a);
            }
        }

        /// <summary>
        /// Reducir movimiento (guion §9, Q13 A): solo el miquito más cercano a una esquina, quieto en una pose de reposo, con
        /// la opacidad <paramref name="alpha"/>; ninguna pieza cambia de posición.
        /// </summary>
        public void ApplyStill(float alpha)
        {
            IntroChoreography? plan = _plan;
            if (!_active || plan == null || _look == null)
            {
                return;
            }

            int best = 0;
            float bestDist = float.MaxValue;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float x = plan.MonkeyX(m, 0f);
                float d = Mathf.Min(x, _canvasW - x);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = m;
                }
            }

            SetDecorAlpha(alpha);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                if (m != best)
                {
                    _rigs[m].SetVisible(false);
                    _lianas[m].enabled = false;
                    continue;
                }

                MonkeyRig rig = _rigs[m];
                float facing = _startFacing[m];
                float x = plan.MonkeyX(m, 0f);
                if (plan.Model == TroopModel.Hang)
                {
                    float gx = x - (facing * MonkeyRig.HangHandS.x * _bodyS);
                    rig.Place(gx, _gripY, facing);
                    rig.Apply(MonkeyRig.Hang(_look, 0f, 0f));
                    SetLiana(m, gx, _gripY, alpha);
                }
                else
                {
                    float rx = plan.Model == TroopModel.Shake ? x : x - (facing * MonkeyRig.SitHandS.x * _bodyS);
                    rig.Place(rx, BranchYAt(rx), facing);
                    rig.Apply(MonkeyRig.Sit(_look, 1f, 0f, -1, 0f));
                }

                rig.SetAlpha(alpha);
            }
        }

        /// <summary>Oculta la rama y la tropa.</summary>
        public void Hide()
        {
            _active = false;
            for (int m = 0; m < _rigs.Length; m++)
            {
                _rigs[m].SetVisible(false);
            }

            if (_layer.gameObject.activeSelf)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Añade los gráficos de la capa (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_branch);
            for (int i = 0; i < Leaves; i++)
            {
                graphics.Add(_leaves[i]);
            }

            for (int m = 0; m < _rigs.Length; m++)
            {
                graphics.Add(_lianas[m]);
                _rigs[m].CollectGraphics(graphics);
            }
        }

        /// <summary>
        /// Cajas de las piezas visibles de la rama y de la tropa en dp del lienzo con la y desde arriba (pruebas de exclusión:
        /// ≥ 8 dp de las cuias y nada sobre el HUD).
        /// </summary>
        public void CollectRects(List<Rect> rects)
        {
            if (!_active)
            {
                return;
            }

            Rect b = _branch.LocalBounds();
            rects.Add(Rect.MinMaxRect(b.xMin, -b.yMax, b.xMax, -b.yMin));
            for (int i = 0; i < Leaves; i++)
            {
                if (_leaves[i].enabled)
                {
                    _leaves[i].rectTransform.GetWorldCorners(_corners);
                    rects.Add(ToCanvas());
                }
            }

            for (int m = 0; m < _rigs.Length; m++)
            {
                if (_lianas[m].enabled)
                {
                    Rect l = _lianas[m].LocalBounds();
                    rects.Add(Rect.MinMaxRect(l.xMin, -l.yMax, l.xMax, -l.yMin));
                }

                if (_rigs[m].Visible && _rigs[m].Alpha != 0f)
                {
                    _rigs[m].CollectRects(_layer, rects);
                }
            }
        }

        /// <summary>y de la rama (dp desde arriba) en <paramref name="x"/>.</summary>
        public float BranchYAt(float x)
        {
            float u = Mathf.Clamp01((x + BranchOverhangDp) / (_canvasW + (2f * BranchOverhangDp)));
            return _branchY - _corner + (4f * u * (1f - u) * (3f + _corner));
        }

        private void PoseMonkey(IntroChoreography plan, int m, float t, float alpha)
        {
            MonkeyRig rig = _rigs[m];
            MonkeyArt look = _look!;
            float s = _bodyS;
            float x = plan.MonkeyX(m, t);
            float v = plan.MonkeyVelocity(m, t);
            bool moving = Mathf.Abs(v) > 0.0001f;
            float facing = Facing(plan, m, t);
            float lift = 0f;
            float shake = 0f;
            for (int k = 0; k < plan.FruitCount; k++)
            {
                if (plan.FruitMonkey[k] != m)
                {
                    continue;
                }

                // Gesto de suelta de unos 100 ms: el brazo sube con la fruta y la suelta a los 60 ms.
                float g = (t - (plan.FruitReleaseMs[k] - IntroChoreography.HoldMs)) / (IntroChoreography.HoldMs + IntroChoreography.GestureTailMs);
                if (g >= 0f && g <= 1f)
                {
                    lift = Mathf.Max(lift, Mathf.Sin(Mathf.PI * Mathf.Min(1f, g / 1.2f)));
                    shake = Mathf.Max(shake, Mathf.Sin(Mathf.PI * g));
                }
            }

            float phase = t * (Mathf.PI * 2f * 8f / 1000f); // 8 Hz
            float breathe = 1f + (0.01f * (1f - Mathf.Cos(t * (Mathf.PI * 2f / 1600f))));
            bool blink = !moving && Mathf.Repeat(t + (m * 731f), 2400f) < 120f;
            bool idle = !moving && lift <= 0f && t < _firstRelease[m] && !plan.IsLong;
            float hop = moving && _species != null && _species.Locomotion == FaunaLocomotion.Jump ? 0.18f * Mathf.Abs(Mathf.Sin(phase * 0.5f)) * s : 0f;
            MonkeyPose pose;
            if (plan.Model == TroopModel.Hang)
            {
                // Avanza mano sobre mano bajo la rama alta y, parado sobre su grupo, baja colgado de una liana corta.
                float settled = moving ? 0f : Mathf.Clamp01((t - StillSince(plan, m, t)) / HangDropMs);
                float gripTop = _branchY + 1.5f;
                float gripY = Mathf.Lerp(gripTop, _gripY, Ease.OutCubic(settled));
                float sway = moving ? 0.06f * Mathf.Sin(phase * 0.5f) : 0.02f * Mathf.Sin(t * 0.004f);
                pose = MonkeyRig.Hang(look, lift, sway);
                pose.Blink = blink;
                float gx = x - (facing * MonkeyRig.HangHandS.x * s);
                rig.Place(gx, gripY, facing);
                SetLiana(m, gx, gripY, alpha);
            }
            else
            {
                if (moving)
                {
                    pose = MonkeyRig.Walk(look, phase, 2f / Mathf.Max(8f, s));
                }
                else
                {
                    pose = MonkeyRig.Sit(look, breathe, plan.Model == TroopModel.Shake ? 0f : lift, idle ? plan.RestVariant : -1, t * 0.012f);
                    if (plan.Model == TroopModel.Shake && shake > 0f)
                    {
                        // Sacude la rama con las dos manos: el cuerpo se mece ±8° y la fruta cae de la rama.
                        float rock = 8f * shake * Mathf.Sin(t * 0.06f);
                        pose.TorsoAngle += rock;
                        pose.HeadAngle += rock * 0.5f;
                        pose.FrontArmTo = new Vector2(0.24f, 0.02f);
                        pose.BackArmTo = new Vector2(0.10f, 0.02f);
                    }
                }

                pose.Blink = blink;
                float rx = plan.Model == TroopModel.Shake ? x : x - (facing * MonkeyRig.SitHandS.x * s);
                rig.Place(rx, BranchYAt(rx) - hop, facing);
                _lianas[m].enabled = false;
            }

            rig.Apply(pose);
            rig.SetAlpha(alpha);
        }

        // Hacia dónde mira: el sentido de su último desplazamiento (o el de entrada); a lo largo de la rama, nunca a una cuia.
        private float Facing(IntroChoreography plan, int m, float t)
        {
            int count = plan.MonkeyKeyCount[m];
            int b = m * IntroChoreography.MaxKeys;
            float facing = _startFacing[m];
            for (int j = 1; j < count; j++)
            {
                if (plan.MonkeyKeyT[b + j - 1] > t)
                {
                    break;
                }

                float dx = plan.MonkeyKeyX[b + j] - plan.MonkeyKeyX[b + j - 1];
                if (Mathf.Abs(dx) > 0.01f)
                {
                    facing = dx > 0f ? 1f : -1f;
                }
            }

            return facing;
        }

        // Desde cuándo está quieto en su posición actual.
        private static float StillSince(IntroChoreography plan, int m, float t)
        {
            int count = plan.MonkeyKeyCount[m];
            int b = m * IntroChoreography.MaxKeys;
            float since = 0f;
            for (int j = 1; j < count; j++)
            {
                float t1 = plan.MonkeyKeyT[b + j];
                if (t1 > t)
                {
                    break;
                }

                if (Mathf.Abs(plan.MonkeyKeyX[b + j] - plan.MonkeyKeyX[b + j - 1]) > 0.01f)
                {
                    since = t1;
                }
            }

            return since;
        }

        private void SetLiana(int m, float x, float gripY, float alpha)
        {
            BezierStripGraphic liana = _lianas[m];
            bool on = gripY > _branchY + 2f;
            if (liana.enabled != on)
            {
                liana.enabled = on;
            }

            if (!on)
            {
                return;
            }

            var top = new Vector2(x, -_branchY);
            var bottom = new Vector2(x, -gripY);
            liana.SetCurve(top, Vector2.Lerp(top, bottom, 0.33f) + new Vector2(1.5f, 0f), Vector2.Lerp(top, bottom, 0.66f) + new Vector2(-1f, 0f), bottom, 2f, 1.6f);
            liana.canvasRenderer.SetAlpha(alpha);
        }

        private void SetDecorAlpha(float a)
        {
            if (Mathf.Abs(a - _alpha) < 0.002f)
            {
                return;
            }

            _alpha = a;
            _branch.canvasRenderer.SetAlpha(a);
            for (int i = 0; i < Leaves; i++)
            {
                _leaves[i].canvasRenderer.SetAlpha(a);
            }
        }

        private BezierStripGraphic Strip(string name, int segments)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(BezierStripGraphic));
            go.transform.SetParent(_layer, false);
            BezierStripGraphic strip = go.GetComponent<BezierStripGraphic>();
            strip.Setup(segments);
            RectTransform rt = strip.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            strip.enabled = false;
            return strip;
        }

        private Rect ToCanvas()
        {
            Rect area = _layer.rect;
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 l = _layer.InverseTransformPoint(_corners[i]);
                float x = l.x - area.xMin;
                float y = area.yMax - l.y;
                x0 = Mathf.Min(x0, x);
                x1 = Mathf.Max(x1, x);
                y0 = Mathf.Min(y0, y);
                y1 = Mathf.Max(y1, y);
            }

            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
    }
}
