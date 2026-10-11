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
    /// rama en primer plano del río (Bézier de 6 dp con corteza y 4 hojas, sin reflejo: la tropa nunca cruza el río), con su
    /// punto más bajo en panel.top − 8 para <c>fila</c> y <c>sacude</c> y a hudBottom + 6 para <c>cuelga</c>, y hasta 6
    /// miquitos con su rig recortable (<see cref="MonkeyRig"/>) guiados por las trayectorias del planificador
    /// (<see cref="IntroChoreography"/>): entran trepando por la rama desde el follaje de su esquina en ≈ 500 ms mientras el río
    /// llena (también en la corta, si les da tiempo a ≤ 450 dp/s; si no, posados desde t = 0), frenan con la mano sobre sus
    /// columnas, sueltan la fruta, reposan ≈ 1.500 ms en la rama ya en juego con un gesto de reposo según la especie y el
    /// nivel (<see cref="ChooseRest"/>; D-011, punto 3) y salen en 500 ms trepando hacia el follaje de la esquina más
    /// cercana, con alfa 1 hasta los últimos 80 ms. Orden de capas (guion §4.6.5; revisión del 2026-10-09): agua &lt; rama
    /// &lt; tropa. <see cref="WaterSlot"/>, donde la intro dibuja el labio del río que desborda, va por detrás de la rama: la
    /// rama queda encima de la crecida y los miquitos, sobre la rama, nunca de pie sobre el río. Rasgos de reposo por especie:
    /// la pareja de <c>colas_entrelazadas</c> se sienta lado a lado mirando al mismo sitio con las colas colgando bajo la rama,
    /// trenzadas dos vueltas; el churuco lleva la fruta abrazada al pecho; el segundo individuo usa la <c>paleta_pareja</c>
    /// si la hay; y con <c>iluminacion = noche</c> un velo nocturno cubre la franja mientras no exista la pintura de noche del
    /// mundo (07 §2.1). Todo es función del tiempo de la intro, así que una captura en t es siempre la misma. Nada depende de
    /// la selección, de un vertido ni de la legalidad (D-002); nada recibe toques.
    /// </summary>
    public sealed class TroopLayer
    {
        private const float BranchWidthDp = 6f;
        private const float LeafWidthDp = 12f;
        private const float LeafHeightDp = 20f;
        private const float HangDropMs = 120f;
        private const float BraidBlendMs = 120f;
        private const float ClimbTiltDeg = 18f;
        private const float EnterTiltDeg = 6f;
        private const int Leaves = 4;

        /// <summary>El gesto de reposo entra y sale en 250 ms (sin saltos de pose).</summary>
        public const float RestBlendMs = 250f;

        /// <summary>La fruta que come mide el 75 % de la de vuelo y mengua hasta el 60 % de eso a bocados.</summary>
        public const float EatFruitScale = 0.75f;

        /// <summary>Medias ondas de la trenza de la pareja: 4 cruces, dos vueltas (revisión del 2026-10-09).</summary>
        public const float BraidHalfWaves = 4f;

        /// <summary>Separación de cada cola al eje de la trenza (S).</summary>
        public const float BraidAmplitudeS = 0.07f;

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
        private readonly float[] _restFrom = new float[IntroChoreography.MaxMonkeys];
        private readonly RestGesture[] _restGesture = new RestGesture[IntroChoreography.MaxMonkeys];
        private readonly int[] _restTint = new int[IntroChoreography.MaxMonkeys];
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

            // Agua < rama < tropa: el labio del río que desborda, por detrás de la rama (guion §4.6.5).
            var waterGo = new GameObject("Water", typeof(RectTransform));
            waterGo.transform.SetParent(_layer, false);
            _water = (RectTransform)waterGo.transform;
            _water.anchorMin = Vector2.zero;
            _water.anchorMax = Vector2.one;
            _water.offsetMin = Vector2.zero;
            _water.offsetMax = Vector2.zero;

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

            for (int m = 0; m < _rigs.Length; m++)
            {
                _rigs[m] = new MonkeyRig(_layer, m);
            }

            // La capa no se desactiva nunca (ni aquí ni en Hide): desactivar un Canvas anidado desregistra sus gráficos ocultos
            // y volver a mostrarlos en mitad de la intro asignaría memoria (GraphicRegistry, A10). Todo empieza oculto pieza a pieza.
        }

        /// <summary>Raíz de la capa (orden de capas y tests).</summary>
        public RectTransform Layer => _layer;

        /// <summary>Hueco por detrás de la rama (y de la tropa) para el labio del río que desborda (lo rellena la intro).</summary>
        public RectTransform WaterSlot => _water;

        /// <summary>La rama (orden de capas y tests de visibilidad).</summary>
        public Graphic Branch => _branch;

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

        /// <summary>Gesto de reposo del miquito <paramref name="m"/> tras su última suelta (tests y capturas).</summary>
        public RestGesture RestGestureOf(int m)
        {
            return _restGesture[m];
        }

        /// <summary>Desde cuándo reposa el miquito <paramref name="m"/> (ms de la intro: tras su última suelta y su último paso).</summary>
        public float RestFromMs(int m)
        {
            return _restFrom[m];
        }

        /// <summary>
        /// Gesto de reposo del miquito <paramref name="monkey"/> de una tropa de <paramref name="count"/> (D-011, punto 3),
        /// determinista por nivel: rota con la variación del nivel (<paramref name="variant"/>, F13) y la especie pone su gesto
        /// propio, que hace siempre un miquito de la tropa (el que marca la variación): balancear la cola con
        /// <c>cola_balancin</c> (nunca con la cola trenzada de la pareja), comer la fruta si la lleva abrazada (churuco) y,
        /// colgados de la cola (<c>cola_enrollada</c>), balancearse. Los demás rotan entre los gestos de su modelo: sentados,
        /// rascarse, mirar alrededor, acicalarse y comer; colgados, comer, rascarse, mirar alrededor y balancearse. Ninguno saluda
        /// ni señala (D-010, punto 6) ni mira una cuia (D-002).
        /// </summary>
        public static RestGesture ChooseRest(int variant, int monkey, int count, TroopModel model, bool pair, bool swingTail, bool hugsFruit, bool tailGrip)
        {
            int v = variant < 0 ? -variant : variant;
            RestGesture signature = RestGesture.None;
            if (model == TroopModel.Hang)
            {
                signature = hugsFruit ? RestGesture.EatFruit : (tailGrip ? RestGesture.SwingBody : RestGesture.None);
            }
            else if (swingTail && !pair)
            {
                signature = RestGesture.SwingTail;
            }
            else if (hugsFruit)
            {
                signature = RestGesture.EatFruit;
            }

            if (signature != RestGesture.None && monkey == v % (count < 1 ? 1 : count))
            {
                return signature;
            }

            int i = (v + monkey) % 4;
            if (model == TroopModel.Hang)
            {
                return i == 0 ? RestGesture.EatFruit : (i == 1 ? RestGesture.Scratch : (i == 2 ? RestGesture.LookAround : RestGesture.SwingBody));
            }

            return i == 0 ? RestGesture.Scratch : (i == 1 ? RestGesture.LookAround : (i == 2 ? RestGesture.Groom : RestGesture.EatFruit));
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
            GraphicPool.EnsureRegistered(_night);
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
                GraphicPool.EnsureRegistered(_lianas[m]);
                if (!on)
                {
                    continue;
                }

                _rigs[m].SetArt(LookOf(m), plan.BodyDp);
                _rigs[m].SetTailSegments(plan.Pair ? BezierStripGraphic.MaxSegments : 8);
                _rigs[m].EnsureRegistered(); // párpado, fruta abrazada…: mostrarlos en mitad de la intro no asigna (A10)
                _firstRelease[m] = float.MaxValue;
                float lastRelease = -1f;
                int tint = plan.FruitCount > 0 ? plan.FruitTint[0] : 1;
                for (int k = 0; k < plan.FruitCount; k++)
                {
                    if (plan.FruitMonkey[k] == m)
                    {
                        _firstRelease[m] = Mathf.Min(_firstRelease[m], plan.FruitReleaseMs[k]);
                        if (plan.FruitReleaseMs[k] >= lastRelease)
                        {
                            lastRelease = plan.FruitReleaseMs[k];
                            tint = plan.FruitTint[k]; // come una de las frutas que soltó: la última
                        }
                    }
                }

                // Reposa desde que acaba su último gesto de suelta y su último paso (la llegada o el giro de antes de salir).
                float rest = Mathf.Max(plan.MonkeyEnterEndMs[m], lastRelease + IntroChoreography.GestureTailMs);
                int keys = plan.MonkeyKeyCount[m];
                int b = m * IntroChoreography.MaxKeys;
                for (int j = 0; j < keys; j++)
                {
                    float kt = plan.MonkeyKeyT[b + j];
                    if (kt < plan.ExitStartMs - 0.01f)
                    {
                        rest = Mathf.Max(rest, kt);
                    }
                }

                _restFrom[m] = Mathf.Min(rest, plan.ExitStartMs);
                _restTint[m] = tint;
                MonkeyArt look = LookOf(m);
                bool swingTail = _species != null && _species.HasTrait("cola_balancin");
                _restGesture[m] = ChooseRest(plan.RestVariant, m, plan.MonkeyCount, plan.Model, plan.Pair, swingTail, look.HugsFruit, look.TailCurls);
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
        /// Reducir movimiento (guion §9; recomendación Q13 A, pendiente de D-010): solo el miquito más cercano a una esquina, quieto en una pose de reposo, con
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
                float x = plan.MonkeySeatX(m);
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
                float facing = plan.MonkeySeatFacing(m); // ya en su sitio: sin entrada ni salida
                float x = plan.MonkeySeatX(m);
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

        /// <summary>Oculta la rama y la tropa (el agua del hueco la oculta la intro), pieza a pieza: la capa sigue activa.</summary>
        public void Hide()
        {
            HideTroop();
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
            float enterSpan = plan.MonkeyEnterEndMs[m] - plan.MonkeyEnterStartMs[m];
            bool entering = moving && !exiting && enterSpan > 0.5f && t < plan.MonkeyEnterEndMs[m];
            float enterQ = entering ? Mathf.Clamp01((t - plan.MonkeyEnterStartMs[m]) / enterSpan) : 1f;

            // Reposo tras su última suelta, ya en juego (D-011, punto 3): el gesto entra y sale en 250 ms, antes de la salida.
            float rest = 0f;
            RestGesture gesture = _restGesture[m];
            if (!moving && !exiting && lift <= 0f && t >= _restFrom[m] && gesture != RestGesture.None)
            {
                rest = Mathf.Min(Mathf.Clamp01((t - _restFrom[m]) / RestBlendMs), Mathf.Clamp01((plan.ExitStartMs - t) / RestBlendMs));
            }

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
                // sube por la liana mientras sigue la rama hacia la esquina (sin subir sobre el HUD).
                float settled = moving ? 0f : Mathf.Clamp01((t - StillSince(plan, m, t)) / HangDropMs);
                if (exiting)
                {
                    settled = 1f - Mathf.Clamp01((t - plan.ExitStartMs) / HangDropMs);
                }

                float gripTop = plan.BranchY + 1.5f;
                float gripY = Mathf.Lerp(gripTop, plan.HangGripY, Ease.OutCubic(settled));
                float swing = gesture == RestGesture.SwingBody ? 0.06f * rest : 0f; // se balancea colgado
                float sway = moving ? 0.06f * Mathf.Sin(phase * 0.5f) : (0.02f + swing) * Mathf.Sin(t * 0.004f);
                pose = rest > 0f ? MonkeyRig.HangRest(look, sway, gesture, rest, t) : MonkeyRig.Hang(look, lift, sway);
                pose.Blink = blink;
                rig.Place(x, gripY, facing);
                SetLiana(m, x, gripY, alpha);
            }
            else
            {
                float tilt = 0f;
                float rise = 0f;
                if (moving || exiting)
                {
                    // Al salir sigue trepando aunque ya haya llegado a su esquina (fuera de la pantalla, en el follaje).
                    pose = MonkeyRig.Walk(look, phase, 2f / Mathf.Max(8f, s));
                    float room = plan.BranchCurveY(x) - (1.1f * s) - _hudBottom - 1f - hop; // el salto también sube
                    // Trepa por el follaje solo junto a su esquina (a menos de 1 S del borde, o ya fuera de la pantalla): por
                    // el resto de la rama camina; nunca sube más de 30 dp ni pasa sobre el HUD.
                    float climb = CornerClimb(x, s);
                    if (exiting)
                    {
                        // Sale trepando hacia el follaje de la esquina (empieza en la rama: sube en el primer tercio de la salida).
                        float up = climb * Mathf.Clamp01(exitQ * 3f);
                        rise = Mathf.Clamp(room, 0f, IntroChoreography.ExitRiseDp) * up;
                        tilt = ClimbTiltDeg * Mathf.Clamp01(exitQ * 4f) * Mathf.Clamp01(climb * 2f);
                    }
                    else if (entering)
                    {
                        // Entra bajando del follaje de su esquina a la rama (D-011, punto 3; ya en la rama en el último tercio de la
                        // entrada), con la cabeza un poco hacia abajo (6°) solo si hay sitio: la cola, que sube detrás, no pasa
                        // sobre el HUD (con la franja justa de la pareja trenzada, camina por la rama como antes).
                        float down = climb * Mathf.Clamp01((1f - enterQ) * 3f);
                        rise = Mathf.Clamp(room, 0f, IntroChoreography.ExitRiseDp) * down;
                        tilt = -EnterTiltDeg * Mathf.Clamp01(down * 2f) * Mathf.Clamp01(room / (0.1f * s));
                    }
                }
                else if (rest > 0f)
                {
                    pose = MonkeyRig.SitRest(look, breathe, gesture, rest, t);
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

            // El churuco lleva abrazada la próxima fruta hasta el gesto de soltarla (entonces la sigue la de la intro); en el
            // reposo, quien come se lleva a la boca una de las frutas que soltó, que aparece en su mano y mengua a bocados.
            bool hug = look.HugsFruit && next >= 0 && !moving && !exiting;
            if (hug)
            {
                rig.Hold(_art.FruitFlight(plan.FruitTint[next]), MonkeyRig.ChestS(look, hanging), plan.FruitFlightDp * 0.8f);
            }
            else if (gesture == RestGesture.EatFruit && rest > 0f)
            {
                float eaten = Mathf.Clamp01((t - _restFrom[m]) / IntroChoreography.RestMs);
                float size = plan.FruitFlightDp * EatFruitScale * rest * (1f - (0.4f * eaten));
                rig.Hold(_art.FruitFlight(_restTint[m]), pose.FrontArmTo + new Vector2(0.02f, 0.03f), size);
            }
            else
            {
                rig.Hold(null, Vector2.zero, 0f);
            }

            rig.Apply(pose);
            rig.SetAlpha(alpha);
        }

        // 1 en el follaje de su esquina (ya fuera de la pantalla) y 0 a partir de 1 S del borde, suave.
        private float CornerClimb(float x, float s)
        {
            float edge = Mathf.Min(x, _canvasW - x);
            float f = 1f - Mathf.Clamp01(edge / Mathf.Max(1f, s));
            return f * f * (3f - (2f * f));
        }

        // La pareja de colas_entrelazadas, quieta en sus asientos, lado a lado y mirando al mismo sitio (revisión del
        // 2026-10-09): las dos colas bajan de la grupa por delante de la rama, se juntan en un eje común a medio camino entre las
        // dos grupas y cuelgan bajo la rama trenzadas dos vueltas (4 cruces: la misma curva con fases opuestas). La rama de la
        // pareja sube lo que cuelgan las colas, que acaban ≥ 6 dp sobre el panel (IntroChoreography.PairTailHangDp): nada de la
        // tropa entra en el panel (A7).
        private void Braid(IntroChoreography plan, int m, float t, float s, ref MonkeyPose pose)
        {
            int other = m == 0 ? 1 : 0;
            if (plan.MonkeyCount < 2 || m > 1 || plan.PairTailHangDp <= 0f)
            {
                return;
            }

            // Solo sentados juntos en sus asientos (no mientras esperan quietos en el follaje de sus esquinas, en la larga).
            float x = plan.MonkeyX(m, t);
            float xo = plan.MonkeyX(other, t);
            float since = Mathf.Max(StillSince(plan, 0, t), StillSince(plan, 1, t));
            bool still = Mathf.Abs(plan.MonkeyVelocity(0, t)) < 0.0001f && Mathf.Abs(plan.MonkeyVelocity(1, t)) < 0.0001f
                && Mathf.Abs(x - xo) <= (IntroChoreography.PairGapS * s) + 1f;
            // Se trenzan en 120 ms al sentarse juntos (desde D-011 también en la corta, que ya entra por la rama). Posados desde
            // t = 0 (sin entrada), ya trenzados.
            float amount = still ? (since <= 0f ? 1f : Mathf.Clamp01((t - since) / BraidBlendMs)) : 0f;

            if (amount <= 0f)
            {
                return;
            }

            // Grupas de los dos (la base de la cola sentada, pose.Tail0) en el lienzo (dp, y hacia abajo).
            float f = plan.MonkeyFacing(m, t);
            float y = plan.BranchCurveY(x);
            float fo = plan.MonkeyFacing(other, t);
            float baseX = x + (f * pose.Tail0.x * s);
            float baseY = y - (pose.Tail0.y * s);
            float otherBaseX = xo + (fo * pose.Tail0.x * s);
            float axisX = (baseX + otherBaseX) * 0.5f;
            float branchY = plan.BranchCurveY(axisX);
            float endY = branchY + plan.PairTailHangDp;

            // Eje recto de la trenza, de la altura de las grupas hasta la punta; cada cola sale de su grupa (separación inicial)
            // y se junta con la otra al pasar por la rama.
            var top = new Vector2(axisX, baseY);
            var end = new Vector2(axisX, endY);
            Vector2 c0 = Local(top, x, y, f, s);
            Vector2 c3 = Local(end, x, y, f, s);
            pose.Tail0 = Vector2.Lerp(pose.Tail0, c0, amount);
            pose.Tail1 = Vector2.Lerp(pose.Tail1, Vector2.Lerp(c0, c3, 1f / 3f), amount);
            pose.Tail2 = Vector2.Lerp(pose.Tail2, Vector2.Lerp(c0, c3, 2f / 3f), amount);
            pose.Tail3 = Vector2.Lerp(pose.Tail3, c3, amount);

            // Hacia abajo la normal local es +x (en el espacio del rig, que ya refleja la orientación): la base de la cola, a
            // un lado u otro del eje, fija la fase.
            float fromS = (baseX - axisX) * f / s;
            float gather = Mathf.Clamp((branchY + 3f - baseY) / Mathf.Max(1f, endY - baseY), 0.1f, 0.5f);
            pose.TailWaveFromS = amount * Mathf.Abs(fromS);
            pose.TailWaveS = amount * BraidAmplitudeS;
            pose.TailWaveGather = gather;
            pose.TailWaveHalves = BraidHalfWaves;
            pose.TailWavePhase = fromS >= 0f ? 0f : Mathf.PI;
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
