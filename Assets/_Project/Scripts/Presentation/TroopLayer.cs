#nullable enable
using System.Collections.Generic;
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// La tropa de «Llenar y teñir» (guion de la intro §4.4, §7.1-§7.3 y §11) en un Canvas anidado propio dentro de la escena,
    /// por detrás del follaje de las esquinas (la rama sale de él y la tropa entra y sale trepando por él) y del tablero: la
    /// rama en primer plano del río (Bézier de 5 dp con corteza y 4 hojas, sin reflejo: la tropa nunca cruza el río), a
    /// panel.top − 8 para <c>fila</c> y <c>sacude</c> y a hudBottom + 6 para <c>cuelga</c>, y hasta 6 miquitos con su rig
    /// recortable (<see cref="MonkeyRig"/>) guiados por las trayectorias del planificador (<see cref="IntroChoreography"/>):
    /// entran por la rama cuando la fila 1 está llena (larga) o ya están posados (corta), frenan con la mano sobre sus
    /// columnas, sueltan la fruta y salen en 300 ms trepando hacia el follaje de la esquina más cercana, con alfa 1 hasta los
    /// últimos 80 ms. Entre la rama y la tropa queda <see cref="WaterSlot"/>, donde la intro dibuja el labio del río que
    /// desborda (delante de la rama, detrás de los miquitos). Rasgos de reposo por especie: la pareja de
    /// <c>colas_entrelazadas</c> se sienta junta con las colas trenzadas, el churuco lleva la fruta abrazada al pecho, el
    /// segundo individuo usa la <c>paleta_pareja</c> si la hay, y con <c>iluminacion = noche</c> un velo nocturno cubre la
    /// franja mientras no exista la pintura de noche del mundo (07 §2.1). Todo es función del tiempo de la intro, así que una
    /// captura en t es siempre la misma. Nada depende de la selección, de un vertido ni de la legalidad (D-002); nada recibe
    /// toques.
    /// </summary>
    public sealed class TroopLayer
    {
        private const float BranchWidthDp = 6f;
        private const float LeafWidthDp = 12f;
        private const float LeafHeightDp = 20f;
        private const float HangDropMs = 120f;
        private const float BraidBlendMs = 120f;
        private const float ClimbTiltDeg = 18f;
        private const int Leaves = 4;

        /// <summary>Velo nocturno sobre la franja (especies con <c>iluminacion = noche</c>).</summary>
        public static readonly Color NightVeil = new Color(0x0E / 255f, 0x16 / 255f, 0x36 / 255f, 0.42f);

        private static readonly Color Bark = Palette.Hex(0x7A5232);
        private static readonly Color BarkTip = Palette.Hex(0x8E6440);
        private static readonly Color InkLine = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);
        private static readonly Color Liana = Palette.Hex(0x4E6B2E);

        private readonly RectTransform _layer;
        private readonly ProceduralArt _art;
        private readonly Image _night;
        private readonly BezierStripGraphic _branch;
        private readonly Image[] _leaves = new Image[Leaves];
        private readonly RectTransform _water;
        private readonly BezierStripGraphic[] _lianas = new BezierStripGraphic[IntroChoreography.MaxMonkeys];
        private readonly MonkeyRig[] _rigs = new MonkeyRig[IntroChoreography.MaxMonkeys];
        private readonly float[] _firstRelease = new float[IntroChoreography.MaxMonkeys];
        private readonly Vector3[] _corners = new Vector3[4];
        private MonkeyArt? _look;
        private MonkeyArt? _lookPair;
        private FaunaSpecies? _species;
        private IntroChoreography? _plan;
        private float _canvasW = 360f;
        private float _hudBottom = 88f;
        private bool _active;
        private float _alpha = -1f;

        /// <summary>Crea la capa (oculta) dentro de <paramref name="parent"/> (la escena, justo antes del follaje).</summary>
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

            var nightGo = new GameObject("NightVeil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nightGo.transform.SetParent(_layer, false);
            _night = nightGo.GetComponent<Image>();
            _night.raycastTarget = false;
            _night.color = NightVeil;
            RectTransform nrt = _night.rectTransform;
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(0f, 1f);
            nrt.pivot = new Vector2(0f, 1f);
            _night.enabled = false;

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
                img.enabled = false;
                _leaves[i] = img;
            }

            var waterGo = new GameObject("Water", typeof(RectTransform));
            waterGo.transform.SetParent(_layer, false);
            _water = (RectTransform)waterGo.transform;
            _water.anchorMin = Vector2.zero;
            _water.anchorMax = Vector2.one;
            _water.offsetMin = Vector2.zero;
            _water.offsetMax = Vector2.zero;

            for (int m = 0; m < _rigs.Length; m++)
            {
                _rigs[m] = new MonkeyRig(_layer, m);
            }

            go.SetActive(false);
        }

        /// <summary>Raíz de la capa (orden de capas y tests).</summary>
        public RectTransform Layer => _layer;

        /// <summary>Hueco entre la rama y la tropa para el labio del río que desborda (lo rellena la intro).</summary>
        public RectTransform WaterSlot => _water;

        /// <summary>La tropa (rama y miquitos) está en pantalla.</summary>
        public bool Active => _active;

        /// <summary>Piezas de la especie preparada (null = dosel).</summary>
        public MonkeyArt? Look => _look;

        /// <summary>Piezas del segundo individuo (la <c>paleta_pareja</c>; null si la especie no la tiene).</summary>
        public MonkeyArt? PairLook => _lookPair;

        /// <summary>Especie preparada.</summary>
        public FaunaSpecies? Species => _species;

        /// <summary>Miquitos en pantalla.</summary>
        public int MonkeyCount => _active && _plan != null ? _plan.MonkeyCount : 0;

        /// <summary>El velo nocturno está a la vista (tests).</summary>
        public bool NightVisible => _night.enabled && _layer.gameObject.activeInHierarchy && _night.canvasRenderer.GetAlpha() > 0.001f;

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
            _lookPair = species != null && species.PairPalette != null ? _art.MonkeyArtFor(species, true) : null;
        }

        /// <summary>
        /// Coloca la rama y la tropa para el plan <paramref name="plan"/> (solo en modo rama y con una especie preparada); en el
        /// modo dosel solo queda activo el hueco del agua.
        /// </summary>
        public void Begin(IntroChoreography plan, IntroInput input)
        {
            _plan = plan;
            _canvasW = input.CanvasWidth;
            _hudBottom = input.HudBottom;
            _alpha = -1f;
            if (!_layer.gameObject.activeSelf)
            {
                _layer.gameObject.SetActive(true);
            }

            if (plan.Perch != IntroPerch.Branch || plan.MonkeyCount == 0 || _look == null)
            {
                HideTroop();
                return;
            }

            _active = true;
            _night.enabled = _species != null && _species.Lighting == FaunaLighting.Night;
            if (_night.enabled)
            {
                _night.rectTransform.anchoredPosition = Vector2.zero;
                _night.rectTransform.sizeDelta = new Vector2(_canvasW, Mathf.Max(0f, input.PanelTop));
            }

            // Rama: parábola de esquina a esquina (sube hacia el follaje de las esquinas), como Bézier cúbica equivalente.
            float w = _canvasW;
            float over = IntroChoreography.BranchOverhangDp;
            float yEnd = plan.BranchCurveY(-over);
            float yMid = plan.BranchCurveY(w * 0.5f);
            float yPeak = yEnd + (2f * (yMid - yEnd));
            var p0 = new Vector2(-over, -yEnd);
            var p3 = new Vector2(w + over, -yEnd);
            var q = new Vector2(w * 0.5f, -yPeak);
            _branch.SetCurve(p0, p0 + ((q - p0) * (2f / 3f)), p3 + ((q - p3) * (2f / 3f)), p3, BranchWidthDp, BranchWidthDp - 1.5f);
            _branch.enabled = true;
            bool high = plan.Model == TroopModel.Hang;
            for (int i = 0; i < Leaves; i++)
            {
                float u = i == 0 ? 0.1f : (i == 1 ? 0.33f : (i == 2 ? 0.67f : 0.9f));
                float x = -over + (u * (w + (2f * over)));
                RectTransform rt = _leaves[i].rectTransform;
                // En la rama baja, hacia arriba (nunca hacia el panel); en la alta, colgando (nunca sobre el HUD).
                rt.anchoredPosition = new Vector2(x, -(plan.BranchCurveY(x) + (high ? 1.5f : -1.5f)));
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

                _rigs[m].SetArt(LookOf(m), plan.BodyDp);
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

            float fadeFrom = plan.ExitStartMs + IntroChoreography.ExitMs - IntroChoreography.ExitFadeMs;
            float exit = 1f - Mathf.Clamp01((t - fadeFrom) / IntroChoreography.ExitFadeMs);
            float a = Mathf.Clamp01(alpha) * exit;
            SetDecorAlpha(a);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                PoseMonkey(plan, m, t, a);
            }
        }

        /// <summary>
        /// Reducir movimiento (guion §9; recomendación Q13 A, pendiente de D-009): solo el miquito más cercano a una esquina, quieto en una pose de reposo, con
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
                MonkeyArt look = LookOf(m);
                float facing = plan.MonkeyStartFacing[m];
                float x = plan.MonkeyX(m, 0f);
                if (plan.Model == TroopModel.Hang)
                {
                    rig.Place(x, plan.HangGripY, facing);
                    rig.Apply(MonkeyRig.Hang(look, 0f, 0f));
                    SetLiana(m, x, plan.HangGripY, alpha);
                }
                else
                {
                    rig.Place(x, plan.BranchCurveY(x), facing);
                    rig.Apply(MonkeyRig.Sit(look, 1f, 0f, -1, 0f));
                }

                rig.Hold(null, Vector2.zero, 0f);
                rig.SetAlpha(alpha);
            }
        }

        /// <summary>Oculta la rama, la tropa y el hueco del agua.</summary>
        public void Hide()
        {
            HideTroop();
            if (_layer.gameObject.activeSelf)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Añade los gráficos de la capa (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_night);
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
            return _plan != null ? _plan.BranchCurveY(x) : 0f;
        }

        private MonkeyArt LookOf(int m)
        {
            return (m % 2 == 1 && _lookPair != null) ? _lookPair : _look!;
        }

        private void HideTroop()
        {
            _active = false;
            _night.enabled = false;
            _branch.enabled = false;
            for (int i = 0; i < Leaves; i++)
            {
                _leaves[i].enabled = false;
            }

            for (int m = 0; m < _rigs.Length; m++)
            {
                _rigs[m].SetVisible(false);
                _lianas[m].enabled = false;
            }
        }

        private void PoseMonkey(IntroChoreography plan, int m, float t, float alpha)
        {
            MonkeyRig rig = _rigs[m];
            MonkeyArt look = LookOf(m);
            float s = plan.BodyDp;
            float x = plan.MonkeyX(m, t);
            float v = plan.MonkeyVelocity(m, t);
            bool moving = Mathf.Abs(v) > 0.0001f;
            float facing = plan.MonkeyFacing(m, t);
            float lift = 0f;
            float shake = 0f;
            int next = -1;
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

                if (g < 0f && (next < 0 || plan.FruitReleaseMs[k] < plan.FruitReleaseMs[next]))
                {
                    next = k; // la próxima fruta que soltará (la que el churuco lleva abrazada)
                }
            }

            bool exiting = t >= plan.ExitStartMs;
            float exitQ = exiting ? Mathf.Clamp01((t - plan.ExitStartMs) / IntroChoreography.ExitMs) : 0f;
            float phase = t * (Mathf.PI * 2f * 8f / 1000f); // 8 Hz
            float breathe = 1f + (0.01f * (1f - Mathf.Cos(t * (Mathf.PI * 2f / 1600f))));
            bool blink = !moving && Mathf.Repeat(t + (m * 731f), 2400f) < 120f;
            bool idle = !moving && lift <= 0f && t < _firstRelease[m] && !plan.IsLong;
            float hop = moving && _species != null && _species.Locomotion == FaunaLocomotion.Jump ? 0.18f * Mathf.Abs(Mathf.Sin(phase * 0.5f)) * s : 0f;
            bool hanging = plan.Model == TroopModel.Hang;
            MonkeyPose pose;
            if (hanging)
            {
                // Avanza mano sobre mano bajo la rama alta y, parado sobre su grupo, baja colgado de una liana corta; al salir,
                // sigue la rama hacia la esquina (sin subir sobre el HUD).
                float settled = moving ? 0f : Mathf.Clamp01((t - StillSince(plan, m, t)) / HangDropMs);
                float gripTop = plan.BranchY + 1.5f;
                float gripY = Mathf.Lerp(gripTop, plan.HangGripY, Ease.OutCubic(settled));
                float sway = moving ? 0.06f * Mathf.Sin(phase * 0.5f) : 0.02f * Mathf.Sin(t * 0.004f);
                pose = MonkeyRig.Hang(look, lift, sway);
                pose.Blink = blink;
                rig.Place(x, gripY, facing);
                SetLiana(m, x, gripY, alpha);
            }
            else
            {
                float tilt = 0f;
                float rise = 0f;
                if (moving)
                {
                    pose = MonkeyRig.Walk(look, phase, 2f / Mathf.Max(8f, s));
                    if (exiting)
                    {
                        // Trepa hacia el follaje de la esquina: hasta 30 dp hacia arriba, sin pasar sobre el HUD.
                        float room = plan.BranchCurveY(x) - (1.1f * s) - _hudBottom - 1f;
                        rise = Mathf.Clamp(room, 0f, IntroChoreography.ExitRiseDp) * Ease.Out(exitQ);
                        tilt = ClimbTiltDeg * Mathf.Clamp01(exitQ * 4f);
                    }
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

                if (plan.Pair && !exiting)
                {
                    Braid(plan, m, t, s, ref pose);
                }

                pose.Blink = blink;
                rig.Place(x, plan.BranchCurveY(x) - hop - rise, facing, tilt);
                _lianas[m].enabled = false;
            }

            // El churuco lleva abrazada la próxima fruta hasta el gesto de soltarla (entonces la sigue la de la intro).
            bool hug = look.HugsFruit && next >= 0 && !moving && !exiting;
            rig.Hold(hug ? _art.FruitFlight(plan.FruitTint[next]) : null, MonkeyRig.ChestS(look, hanging), plan.FruitFlightDp * 0.8f);
            rig.Apply(pose);
            rig.SetAlpha(alpha);
        }

        // La pareja de colas_entrelazadas, quieta en sus asientos de espaldas: las dos colas suben trenzadas por detrás de ellos
        // (dos Bézier de ≈ 1,3 S que se cruzan 3 veces, una el reflejo de la otra). Cuelgan hacia arriba y no bajo la rama: la
        // rama está a 8 dp del panel y nada de la tropa entra en él (A7).
        private void Braid(IntroChoreography plan, int m, float t, float s, ref MonkeyPose pose)
        {
            int other = m == 0 ? 1 : 0;
            if (plan.MonkeyCount < 2 || m > 1)
            {
                return;
            }

            float since = Mathf.Max(StillSince(plan, 0, t), StillSince(plan, 1, t));
            bool still = Mathf.Abs(plan.MonkeyVelocity(0, t)) < 0.0001f && Mathf.Abs(plan.MonkeyVelocity(1, t)) < 0.0001f;
            float amount = still ? Mathf.Clamp01((t - since) / BraidBlendMs) : 0f;
            if (!plan.IsLong)
            {
                amount = still ? 1f : 0f;
            }

            if (amount <= 0f)
            {
                return;
            }

            float x = plan.MonkeyX(m, t);
            float xo = plan.MonkeyX(other, t);
            float mid = (x + xo) * 0.5f;
            float side = x < xo ? -1f : 1f; // este miquito, a la izquierda (−1) o a la derecha (+1) del centro
            float f = plan.MonkeyFacing(m, t);
            float y = plan.BranchCurveY(x);
            float h = 0.93f * s;

            // Puntos de control en el lienzo (dp, y hacia abajo); el de la derecha es el reflejo del de la izquierda.
            Vector2 w0 = new Vector2(mid + (side * 0.23f * s), y - (0.22f * s));
            Vector2 w1 = new Vector2(mid - (side * 0.45f * s), y - (0.22f * s) - (h * 0.33f));
            Vector2 w2 = new Vector2(mid + (side * 0.45f * s), y - (0.22f * s) - (h * 0.66f));
            Vector2 w3 = new Vector2(mid - (side * 0.06f * s), y - (0.22f * s) - h);
            pose.Tail0 = Vector2.Lerp(pose.Tail0, Local(w0, x, y, f, s), amount);
            pose.Tail1 = Vector2.Lerp(pose.Tail1, Local(w1, x, y, f, s), amount);
            pose.Tail2 = Vector2.Lerp(pose.Tail2, Local(w2, x, y, f, s), amount);
            pose.Tail3 = Vector2.Lerp(pose.Tail3, Local(w3, x, y, f, s), amount);
        }

        // Del lienzo (y hacia abajo) al espacio del rig (S, y hacia arriba, mirando a la derecha).
        private static Vector2 Local(Vector2 world, float rootX, float rootY, float facing, float s)
        {
            return new Vector2((world.x - rootX) * facing / s, (rootY - world.y) / s);
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
            float branchY = _plan != null ? _plan.BranchY : gripY;
            bool on = gripY > branchY + 2f;
            if (liana.enabled != on)
            {
                liana.enabled = on;
            }

            if (!on)
            {
                return;
            }

            var top = new Vector2(x, -branchY);
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
            _night.canvasRenderer.SetAlpha(a);
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
