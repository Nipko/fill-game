#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Pose de un miquito en unidades de su tamaño S (origen en el apoyo: los pies sobre la rama o, colgado, el agarre; y
    /// hacia arriba; mirando a la derecha). Extremidades de articulación a mano o pie; cola como Bézier cúbica.
    /// </summary>
    public struct MonkeyPose
    {
        /// <summary>Centro del torso.</summary>
        public Vector2 TorsoCenter;

        /// <summary>Ancho y alto del torso.</summary>
        public Vector2 TorsoSize;

        /// <summary>Giro del torso (grados).</summary>
        public float TorsoAngle;

        /// <summary>Centro de la cabeza.</summary>
        public Vector2 HeadCenter;

        /// <summary>Giro de la cabeza (grados; + mira arriba, también cuando mira atrás).</summary>
        public float HeadAngle;

        /// <summary>
        /// La cabeza mira atrás, a lo largo de la rama (gesto de mirar alrededor): la pieza se refleja en horizontal, con el
        /// cráneo en el mismo sitio; nunca mira hacia abajo (D-002).
        /// </summary>
        public bool HeadBack;

        /// <summary>Hombro y mano del brazo de detrás.</summary>
        public Vector2 BackArmFrom;

        /// <summary>Mano del brazo de detrás.</summary>
        public Vector2 BackArmTo;

        /// <summary>Hombro del brazo de delante (el que suelta).</summary>
        public Vector2 FrontArmFrom;

        /// <summary>Mano del brazo de delante.</summary>
        public Vector2 FrontArmTo;

        /// <summary>Cadera de la pierna de detrás.</summary>
        public Vector2 BackLegFrom;

        /// <summary>Pie de la pierna de detrás.</summary>
        public Vector2 BackLegTo;

        /// <summary>Cadera de la pierna de delante.</summary>
        public Vector2 FrontLegFrom;

        /// <summary>Pie de la pierna de delante.</summary>
        public Vector2 FrontLegTo;

        /// <summary>Base de la cola.</summary>
        public Vector2 Tail0;

        /// <summary>Control 1 de la cola.</summary>
        public Vector2 Tail1;

        /// <summary>Control 2 de la cola.</summary>
        public Vector2 Tail2;

        /// <summary>Punta de la cola.</summary>
        public Vector2 Tail3;

        /// <summary>
        /// Trenza (<see cref="BezierStripGraphic.SetWave"/>, en S): separación de la base de la cola al eje de la curva; 0 sin
        /// trenza.
        /// </summary>
        public float TailWaveFromS;

        /// <summary>Trenza: separación de la cola al eje en el tramo trenzado (S).</summary>
        public float TailWaveS;

        /// <summary>Trenza: fracción de la cola en la que pasa de su base al eje.</summary>
        public float TailWaveGather;

        /// <summary>Trenza: medias ondas (4 = dos vueltas); 0 sin trenza.</summary>
        public float TailWaveHalves;

        /// <summary>Trenza: fase (0 o π: las dos colas de la pareja se cruzan).</summary>
        public float TailWavePhase;

        /// <summary>Párpados cerrados (parpadeo de 120 ms).</summary>
        public bool Blink;
    }

    /// <summary>
    /// Gesto de reposo de un miquito en la rama tras su última suelta, ya en juego (D-011, punto 3). Gestos de animal, sin rasgos
    /// humanos: nada de saludar ni señalar (D-010, punto 6), y la cabeza nunca baja hacia el tablero ni mira una cuia (D-002).
    /// </summary>
    public enum RestGesture
    {
        /// <summary>Sin gesto.</summary>
        None = -1,

        /// <summary>Se rasca detrás de la oreja con la mano libre, a rascadas rápidas (sin cruzar la cara ni subir sobre la cabeza).</summary>
        Scratch = 0,

        /// <summary>Mira alrededor: la cabeza sube hacia el cielo y luego mira atrás a lo largo de la rama, nunca hacia abajo.</summary>
        LookAround = 1,

        /// <summary>Se acicala el pecho.</summary>
        Groom = 2,

        /// <summary>Come una fruta de las que soltó: se la lleva a la boca a bocados y se va haciendo pequeña.</summary>
        EatFruit = 3,

        /// <summary>Balancea la cola (rasgo <c>cola_balancin</c>); con la cola trenzada de la pareja, no.</summary>
        SwingTail = 4,

        /// <summary>Se balancea colgado (modelo <c>cuelga</c>; la firma de los de cola prensil, <c>cola_enrollada</c>).</summary>
        SwingBody = 5,
    }

    /// <summary>
    /// Rig recortable de un miquito en uGUI (guion de la intro §11; recomendación Q16 A, pendiente de D-010): 8 piezas (cola, pierna y brazo de detrás, torso,
    /// pierna de delante, cabeza con su párpado y brazo de delante) más dos opcionales (el bigote del pichico, hijo de la
    /// cabeza, y la fruta abrazada al pecho del churuco) con las piezas de <see cref="MonkeyArt"/> y poses por código
    /// (<see cref="Sit"/>, <see cref="Walk"/>, <see cref="Hang"/>). Postura animal, sin ropa ni accesorios, y nunca mira una
    /// cuia: mira a lo largo de la rama o a la fruta de su mano (guion §4.6). Sin raycast.
    /// </summary>
    public sealed class MonkeyRig
    {
        /// <summary>Piezas del rig.</summary>
        public const int PieceCount = 8;

        /// <summary>Mano del brazo de delante al soltar, sentado (S): la fruta sale de aquí (la misma que planifica la intro).</summary>
        public static readonly Vector2 SitHandS = new Vector2(IntroChoreography.SitHandOffsetS, IntroChoreography.SitHandHeightS);

        /// <summary>Mano libre colgado (S, bajo el agarre): la fruta sale de aquí; es lo más bajo del cuerpo colgado.</summary>
        public static readonly Vector2 HangHandS = new Vector2(IntroChoreography.HangHandOffsetS, -IntroChoreography.HangDepthS);

        private readonly RectTransform _root;
        private readonly BezierStripGraphic _tail;
        private readonly Image _backLeg;
        private readonly Image _backArm;
        private readonly Image _torso;
        private readonly Image _frontLeg;
        private readonly Image _head;
        private readonly Image _eyelid;
        private readonly Image _frontArm;
        private readonly Image _whiskers;
        private readonly Image _held;
        private readonly Graphic[] _graphics;
        private readonly Vector3[] _corners = new Vector3[4];
        private MonkeyArt? _art;
        private float _s = 40f;
        private float _alpha = -1f;

        /// <summary>Crea el rig (oculto) dentro de <paramref name="parent"/>.</summary>
        public MonkeyRig(RectTransform parent, int index)
        {
            var go = new GameObject("Monkey" + index, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;

            var tailGo = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(BezierStripGraphic));
            tailGo.transform.SetParent(_root, false);
            _tail = tailGo.GetComponent<BezierStripGraphic>();
            _tail.Setup(8);
            RectTransform trt = _tail.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = Vector2.zero;
            _backLeg = Piece("BackLeg", _root, new Vector2(0.5f, 1f));
            _backArm = Piece("BackArm", _root, new Vector2(0.5f, 1f));
            _torso = Piece("Torso", _root, new Vector2(0.5f, 0.5f));
            _frontLeg = Piece("FrontLeg", _root, new Vector2(0.5f, 1f));
            _head = Piece("Head", _root, new Vector2(0.5f, 0.5f));
            _eyelid = Piece("Eyelid", _head.rectTransform, new Vector2(0.5f, 0.5f));
            _eyelid.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _eyelid.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _whiskers = Piece("Whiskers", _head.rectTransform, new Vector2(0.5f, 0.5f));
            _whiskers.enabled = false;
            _held = Piece("HeldFruit", _root, new Vector2(0.5f, 0.5f));
            _held.enabled = false;
            _frontArm = Piece("FrontArm", _root, new Vector2(0.5f, 1f));
            _backLeg.type = Image.Type.Sliced;
            _backArm.type = Image.Type.Sliced;
            _frontLeg.type = Image.Type.Sliced;
            _frontArm.type = Image.Type.Sliced;
            _graphics = new Graphic[] { _tail, _backLeg, _backArm, _torso, _frontLeg, _head, _eyelid, _whiskers, _held, _frontArm };
            go.SetActive(false);
        }

        /// <summary>Raíz del rig (apoyo en la rama o agarre).</summary>
        public RectTransform Root => _root;

        /// <summary>Tamaño S en dp.</summary>
        public float Size => _s;

        /// <summary>Piezas de la especie.</summary>
        public MonkeyArt? Art => _art;

        /// <summary>El rig está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Opacidad actual.</summary>
        public float Alpha => _alpha;

        /// <summary>Asigna las piezas de la especie y el tamaño en pantalla.</summary>
        public void SetArt(MonkeyArt art, float sizeDp)
        {
            _art = art;
            _s = Mathf.Max(8f, sizeDp);
            _backLeg.sprite = art.Limb;
            _backArm.sprite = art.Limb;
            _frontLeg.sprite = art.Limb;
            _frontArm.sprite = art.Limb;
            _torso.sprite = art.Torso;
            _head.sprite = art.Head;
            _eyelid.sprite = art.Eyelid;
            var back = new Color(0.78f, 0.78f, 0.78f, 1f); // las piezas de detrás, a la sombra
            _backLeg.color = back;
            _backArm.color = back;
            float limbPx = art.Limb.rect.width;
            float pxPerDp = limbPx / Mathf.Max(1f, art.LimbWidthS * _s);
            float mult = pxPerDp / PiecePxPerDp;
            _backLeg.pixelsPerUnitMultiplier = mult;
            _backArm.pixelsPerUnitMultiplier = mult;
            _frontLeg.pixelsPerUnitMultiplier = mult;
            _frontArm.pixelsPerUnitMultiplier = mult;
            _head.rectTransform.sizeDelta = new Vector2(art.HeadS * _s, art.HeadS * _s);
            _eyelid.rectTransform.sizeDelta = art.EyelidSizeS * _s;
            _eyelid.rectTransform.anchoredPosition = art.EyelidOffsetS * _s;
            _whiskers.sprite = art.Whiskers;
            _whiskers.enabled = art.Whiskers != null;
            if (art.Whiskers != null)
            {
                _whiskers.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _whiskers.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _whiskers.rectTransform.sizeDelta = art.WhiskersSizeS * _s;
                _whiskers.rectTransform.anchoredPosition = art.WhiskersOffsetS * _s;
            }

            _held.enabled = false;
            _tail.SetColors(art.Tail, art.TailTip, 0.72f, art.TailEdge, Mathf.Max(0.6f, art.TailEdgeS * _s));
        }

        /// <summary>
        /// Convierte el rig en una silueta plana de color <paramref name="color"/> (perfil de luz de la tropa lateral, revisión de
        /// TL1): todas las piezas con <paramref name="material"/> (la forma del sprite con el color del vértice:
        /// <c>AF/UI/Silhouette</c>) y la cola del mismo color, con su borde. Tras <see cref="SetArt"/>; sin asignaciones.
        /// </summary>
        public void SetSilhouette(Material? material, Color color)
        {
            for (int i = 1; i < _graphics.Length; i++)
            {
                Graphic g = _graphics[i];
                if (g.material != material)
                {
                    g.material = material;
                }

                if (g.color != color)
                {
                    g.color = color;
                }
            }

            _tail.SetColors(color, color, 0.72f, color, Mathf.Max(0.8f, 0.03f * _s));
        }

        /// <summary>Muestra u oculta el rig.</summary>
        public void SetVisible(bool on)
        {
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
                _alpha = -1f;
            }
        }

        /// <summary>Opacidad de todas las piezas (sin reconstruir mallas).</summary>
        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Abs(alpha - _alpha) < 0.002f)
            {
                return;
            }

            _alpha = alpha;
            for (int i = 0; i < _graphics.Length; i++)
            {
                _graphics[i].canvasRenderer.SetAlpha(alpha);
            }
        }

        /// <summary>
        /// Coloca el apoyo en (<paramref name="x"/>, <paramref name="yDown"/>) dp del lienzo, mirando a <paramref name="facing"/>
        /// (±1) y con la cabeza <paramref name="tiltDeg"/> grados hacia arriba (trepa hacia el follaje al salir).
        /// </summary>
        public void Place(float x, float yDown, float facing, float tiltDeg = 0f)
        {
            var pos = new Vector2(x, -yDown);
            if (_root.anchoredPosition != pos)
            {
                _root.anchoredPosition = pos;
            }

            var scale = new Vector3(facing < 0f ? -1f : 1f, 1f, 1f);
            if (_root.localScale != scale)
            {
                _root.localScale = scale;
            }

            Quaternion r = tiltDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, facing < 0f ? -tiltDeg : tiltDeg);
            if (_root.localRotation != r)
            {
                _root.localRotation = r;
            }
        }

        /// <summary>
        /// La fruta abrazada al pecho (<see cref="MonkeyArt.HugsFruit"/>): <paramref name="sprite"/> en <paramref name="chestS"/>
        /// (S) con lado <paramref name="sizeDp"/>; null la oculta (al soltarla, la sigue la fruta de la intro).
        /// </summary>
        public void Hold(Sprite? sprite, Vector2 chestS, float sizeDp)
        {
            bool on = sprite != null;
            if (_held.enabled != on)
            {
                _held.enabled = on;
            }

            if (!on)
            {
                return;
            }

            if (_held.sprite != sprite)
            {
                _held.sprite = sprite;
            }

            Set(_held.rectTransform, chestS * _s, new Vector2(sizeDp, sizeDp), 0f);
        }

        /// <summary>Aplica la pose.</summary>
        public void Apply(in MonkeyPose pose)
        {
            if (_art == null)
            {
                return;
            }

            float s = _s;
            float limbW = _art.LimbWidthS * s;
            Limb(_backLeg, pose.BackLegFrom * s, pose.BackLegTo * s, limbW);
            Limb(_backArm, pose.BackArmFrom * s, pose.BackArmTo * s, limbW);
            Limb(_frontLeg, pose.FrontLegFrom * s, pose.FrontLegTo * s, limbW);
            Limb(_frontArm, pose.FrontArmFrom * s, pose.FrontArmTo * s, limbW);
            Vector2 torso = pose.TorsoSize * s;
            torso.x *= _art.TorsoWidthScale;
            Set(_torso.rectTransform, pose.TorsoCenter * s, torso, pose.TorsoAngle);
            RectTransform head = _head.rectTransform;
            Vector2 hp = pose.HeadCenter * s;
            if (head.anchoredPosition != hp)
            {
                head.anchoredPosition = hp;
            }

            // Mirando atrás, la pieza se refleja y gira al revés: + sigue siendo mirar arriba.
            Quaternion hr = Quaternion.Euler(0f, 0f, pose.HeadBack ? -pose.HeadAngle : pose.HeadAngle);
            if (head.localRotation != hr)
            {
                head.localRotation = hr;
            }

            var hs = new Vector3(pose.HeadBack ? -1f : 1f, 1f, 1f);
            if (head.localScale != hs)
            {
                head.localScale = hs;
            }

            if (_eyelid.enabled != pose.Blink)
            {
                _eyelid.enabled = pose.Blink;
            }

            float w = _art.TailWidthS * s;
            _tail.SetCurve(pose.Tail0 * s, pose.Tail1 * s, pose.Tail2 * s, pose.Tail3 * s, w, w * 0.65f);
            _tail.SetWave(pose.TailWaveFromS * s, pose.TailWaveS * s, pose.TailWaveGather, pose.TailWaveHalves, pose.TailWavePhase);
        }

        /// <summary>
        /// Caja de la pieza de la cabeza en dp del lienzo con la y desde arriba (<paramref name="space"/>, como en
        /// <see cref="CollectRects"/>; tests de la tropa lateral). Sin asignaciones.
        /// </summary>
        public Rect HeadRect(RectTransform space)
        {
            _head.rectTransform.GetWorldCorners(_corners);
            return ToCanvas(space);
        }

        /// <summary>Cola del rig (tests de la trenza).</summary>
        public BezierStripGraphic Tail => _tail;

        /// <summary>Deja registradas en su Canvas las piezas ocultas (párpado, bigote, fruta abrazada): ver <see cref="GraphicPool"/>.</summary>
        public void EnsureRegistered()
        {
            for (int i = 0; i < _graphics.Length; i++)
            {
                GraphicPool.EnsureRegistered(_graphics[i]);
            }
        }

        /// <summary>Segmentos de la cola: 8 en arco; más para que la trenza de la pareja no se vea poligonal.</summary>
        public void SetTailSegments(int segments)
        {
            _tail.SetSegments(segments);
        }

        /// <summary>Añade los gráficos del rig (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.AddRange(_graphics);
        }

        /// <summary>
        /// Añade la caja de cada pieza visible en dp del lienzo con la y desde arriba (<paramref name="space"/> es una capa a
        /// pantalla completa con el pivote en el centro, como el lienzo).
        /// </summary>
        public void CollectRects(RectTransform space, List<Rect> rects)
        {
            if (!Visible)
            {
                return;
            }

            for (int i = 1; i < _graphics.Length; i++)
            {
                if (!_graphics[i].enabled || !_graphics[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                _graphics[i].rectTransform.GetWorldCorners(_corners);
                rects.Add(ToCanvas(space));
            }

            Rect b = _tail.LocalBounds();
            RectTransform t = _tail.rectTransform;
            _corners[0] = t.TransformPoint(new Vector3(b.xMin, b.yMin, 0f));
            _corners[1] = t.TransformPoint(new Vector3(b.xMin, b.yMax, 0f));
            _corners[2] = t.TransformPoint(new Vector3(b.xMax, b.yMax, 0f));
            _corners[3] = t.TransformPoint(new Vector3(b.xMax, b.yMin, 0f));
            rects.Add(ToCanvas(space));
        }

        /// <summary>Sentado en la rama (reposo, espera y suelta de <c>fila</c> y <c>sacude</c>).</summary>
        /// <param name="art">Piezas de la especie.</param>
        /// <param name="breathe">Escala Y del torso (1,00 ↔ 1,02).</param>
        /// <param name="lift">0..1: el brazo de delante sube con la fruta (−40° → +20° en 100 ms).</param>
        /// <param name="rest">Variante de reposo (0 rascarse, 1 mirar al cielo, 2 acicalarse; −1 ninguna).</param>
        /// <param name="restPhase">Fase de la variante (radianes).</param>
        public static MonkeyPose Sit(MonkeyArt art, float breathe, float lift, int rest, float restPhase)
        {
            float l = art.LimbScale;
            var p = new MonkeyPose
            {
                TorsoCenter = new Vector2(-0.02f, 0.33f),
                TorsoSize = new Vector2(0.50f, 0.42f * breathe),
                TorsoAngle = -12f,
                HeadCenter = new Vector2(0.17f, 0.66f + ((breathe - 1f) * 0.4f)),
                BackLegFrom = new Vector2(-0.10f, 0.22f),
                BackLegTo = new Vector2(0.08f, 0.02f),
                FrontLegFrom = new Vector2(-0.04f, 0.20f),
                FrontLegTo = new Vector2(0.15f, 0.01f),
                BackArmFrom = new Vector2(0.08f, 0.47f),
                BackArmTo = new Vector2(0.08f + (0.08f * l), 0.47f - (0.41f * l)),
                FrontArmFrom = new Vector2(0.13f, 0.46f),
                FrontArmTo = Vector2.Lerp(art.HugsFruit ? new Vector2(0.18f, 0.32f) : new Vector2(0.13f + (0.09f * l), 0.46f - (0.38f * l)), SitHandS, Mathf.Clamp01(lift)),
            };

            if (lift <= 0f)
            {
                if (rest == 0)
                {
                    p.BackArmTo = new Vector2(0.20f, 0.78f + (0.025f * Mathf.Sin(restPhase))); // se rasca la cabeza
                }
                else if (rest == 1)
                {
                    p.HeadAngle = 14f * Mathf.Clamp01(0.5f + (0.5f * Mathf.Sin(restPhase * 0.25f))); // mira al cielo
                }
                else if (rest == 2)
                {
                    p.FrontArmTo = new Vector2(0.12f + (0.02f * Mathf.Sin(restPhase)), 0.36f); // se acicala el pecho
                }
            }

            SitTail(art, ref p, new Vector2(-0.22f, 0.22f));
            return p;
        }

        /// <summary>Boca del miquito sentado (S): adonde se lleva la fruta al comer (delante del hocico, a la altura de la boca).</summary>
        public static readonly Vector2 SitMouthS = new Vector2(0.36f, 0.57f);

        /// <summary>Mano que mastica, sentado (S): la fruta baja aquí entre bocado y bocado, delante del pecho.</summary>
        public static readonly Vector2 SitChewS = new Vector2(0.27f, 0.43f);

        /// <summary>
        /// Ciclo común de los gestos de reposo (ms): todos los gestos lentos (bocado, cola, balanceo colgado, mirar arriba o
        /// atrás) siguen la misma onda (<see cref="Wave"/>), con el reloj de la tropa (desde su última suelta) y medio ciclo de
        /// desfase entre miquitos vecinos: en un mismo instante cada gesto está en uno de sus dos extremos (capturas que lo
        /// prueban) y la tropa no se mueve al unísono.
        /// </summary>
        public const float GestureCycleMs = 900f;

        /// <summary>Cada bocado: la mano sube a la boca y baja en un ciclo de gesto (900 ms).</summary>
        public const float EatCycleMs = GestureCycleMs;

        /// <summary>La cola se balancea ±20° alrededor de su base, un vaivén por ciclo de gesto.</summary>
        public const float SwingTailDeg = 20f;

        /// <summary>Periodo del balanceo de la cola: el ciclo de gesto.</summary>
        public const float SwingTailMs = GestureCycleMs;

        /// <summary>Al mirar alrededor, la cabeza sube hasta 28° (se nota a 48 dp; antes 14°).</summary>
        public const float LookUpDeg = 28f;

        /// <summary>Al mirar atrás por la rama, la cabeza sube como mucho 10° (nunca baja: D-002).</summary>
        public const float LookBackDeg = 10f;

        /// <summary>Colgado, se balancea ±10° alrededor del agarre (TroopLayer gira la raíz; antes ±3°).</summary>
        public const float SwingBodyDeg = 10f;

        /// <summary>Se rasca a 3,5 rascadas por segundo.</summary>
        public const float ScratchHz = 3.5f;

        /// <summary>Recorrido de la mano al rascarse (S, de su centro a cada extremo).</summary>
        public const float ScratchAmplitudeS = 0.08f;

        /// <summary>
        /// Onda de los gestos lentos en el reloj <paramref name="clockMs"/>: −1 en 0 (y cada <see cref="GestureCycleMs"/>) y
        /// +1 a mitad de ciclo; suave en los extremos.
        /// </summary>
        public static float Wave(float clockMs)
        {
            return -Mathf.Cos(clockMs * (Mathf.PI * 2f / GestureCycleMs));
        }

        /// <summary>
        /// Sentado en reposo tras su última suelta (D-011, punto 3) con el gesto <paramref name="gesture"/>, mezclado con
        /// <paramref name="amount"/> (0..1) sobre la pose sentada de siempre y animado con el reloj de gestos
        /// <paramref name="clockMs"/> (ms; ver <see cref="GestureCycleMs"/>): la pose es función del tiempo (capturas
        /// deterministas). Rascarse: la mano de delante va a la oreja, por detrás de la cara y sin subir por encima de la cabeza,
        /// y rasca a 3,5 Hz; mirar alrededor: la cabeza sube 28° y luego mira atrás por la rama; la cola, ±20°. La cabeza solo
        /// sube (D-002).
        /// </summary>
        public static MonkeyPose SitRest(MonkeyArt art, float breathe, RestGesture gesture, float amount, float clockMs)
        {
            MonkeyPose p = Sit(art, breathe, 0f, -1, 0f);
            float a = Mathf.Clamp01(amount);
            if (a <= 0f)
            {
                return p;
            }

            switch (gesture)
            {
                case RestGesture.Scratch:
                    p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, ScratchSpot(p, clockMs), a);
                    break;
                case RestGesture.LookAround:
                    LookAround(ref p, art, a, clockMs);
                    break;
                case RestGesture.Groom:
                    p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, new Vector2(0.12f + (0.03f * Scratching(clockMs)), 0.36f), a);
                    break;
                case RestGesture.EatFruit:
                    p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, Vector2.Lerp(SitChewS, SitMouthS, Bite(clockMs)), a);
                    p.HeadAngle = a * 4f * Bite(clockMs); // asiente un poco hacia arriba en cada bocado, nunca hacia abajo
                    break;
                case RestGesture.SwingTail:
                    RotateTail(ref p, a * SwingTailDeg * Wave(clockMs));
                    break;
            }

            return p;
        }

        /// <summary>
        /// Colgado en reposo tras su última suelta (modelo <c>cuelga</c>; D-011, punto 3): sobre <see cref="Hang"/>, la mano
        /// libre se rasca detrás de la oreja (sin cruzar la cara), se acicala el pecho, se lleva la fruta a la boca o la cabeza
        /// mira arriba y atrás, con el reloj de gestos <paramref name="clockMs"/>; el balanceo
        /// (<see cref="RestGesture.SwingBody"/>, ±<see cref="SwingBodyDeg"/>) lo da TroopLayer girando la raíz en el agarre. Nada
        /// baja de la mano de soltar (A7).
        /// </summary>
        public static MonkeyPose HangRest(MonkeyArt art, float sway, RestGesture gesture, float amount, float clockMs)
        {
            MonkeyPose p = Hang(art, 0f, sway);
            float a = Mathf.Clamp01(amount);
            if (a <= 0f)
            {
                return p;
            }

            switch (gesture)
            {
                case RestGesture.Scratch:
                    p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, ScratchSpot(p, clockMs), a);
                    break;
                case RestGesture.LookAround:
                    LookAround(ref p, art, a, clockMs);
                    break;
                case RestGesture.Groom:
                    p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, p.TorsoCenter + new Vector2(0.10f, 0.06f + (0.03f * Scratching(clockMs))), a);
                    break;
                case RestGesture.EatFruit:
                    p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, HangEatHand(p, clockMs), a);
                    p.HeadAngle = a * 4f * Bite(clockMs);
                    break;
            }

            return p;
        }

        /// <summary>Bocado (0 masticando, 1 en la boca): sube y baja en <see cref="EatCycleMs"/>.</summary>
        public static float Bite(float clockMs)
        {
            return 0.5f + (0.5f * Wave(clockMs));
        }

        // Rascada rápida (−1..1) a ScratchHz.
        private static float Scratching(float clockMs)
        {
            return Mathf.Sin(clockMs * (Mathf.PI * 2f * ScratchHz / 1000f));
        }

        // La mano que se rasca, en la oreja (por detrás del centro de la cabeza, a su altura, nunca por encima ni sobre la cara),
        // rascando arriba y abajo a lo largo del cráneo.
        private static Vector2 ScratchSpot(in MonkeyPose p, float clockMs)
        {
            float w = Scratching(clockMs);
            return p.HeadCenter + new Vector2(-0.13f + (0.02f * w), 0.02f + (ScratchAmplitudeS * w));
        }

        // Mirar alrededor: con la onda arriba, la cabeza sube hasta 28°; con la onda abajo, mira atrás por la rama (la pieza
        // reflejada, el cráneo en su sitio), un poco hacia arriba. Al subir, la cabeza baja lo justo para que su caja girada no
        // pase de la de antes (la misma altura sentado: SitTopS).
        private static void LookAround(ref MonkeyPose p, MonkeyArt art, float a, float clockMs)
        {
            float w = Wave(clockMs);
            float half = 0.5f * art.HeadS;
            if (w >= 0f)
            {
                p.HeadAngle = a * LookUpDeg * w;
                float r = p.HeadAngle * Mathf.Deg2Rad;
                float grow = half * ((Mathf.Cos(r) + Mathf.Sin(r)) - LookAroundBoxBefore);
                p.HeadCenter.y -= Mathf.Max(0f, grow);
                return;
            }

            p.HeadAngle = a * LookBackDeg * -w;
            if (a >= 0.5f && w < -0.2f)
            {
                p.HeadBack = true;
                p.HeadCenter.x -= 2f * SkullOffsetS * art.HeadS; // el cráneo (4 px detrás del centro de la celda) no salta
            }
        }

        // Caja de la cabeza girada 14° (el mirar al cielo de antes), en medios lados: cos 14° + sin 14°.
        private static readonly float LookAroundBoxBefore = Mathf.Cos(14f * Mathf.Deg2Rad) + Mathf.Sin(14f * Mathf.Deg2Rad);

        // Cráneo de la pieza de la cabeza (ProceduralArt.DrawHead): centro 4 px detrás y 6 px por debajo del de la celda de
        // 128 px, radio 40 px más 3,5 de tinta; en fracciones del lado de la cabeza.
        private const float SkullOffsetS = 4f / 128f;
        private const float SkullDropS = 6f / 128f;
        private const float SkullRadiusS = 43.5f / 128f;

        // Mano libre del colgado al comer: de delante del pecho a la boca, bajo la cabeza.
        private static Vector2 HangEatHand(in MonkeyPose p, float clockMs)
        {
            Vector2 mouth = p.HeadCenter + new Vector2(0.17f, -0.07f);
            Vector2 chew = p.HeadCenter + new Vector2(0.13f, -0.20f);
            return Vector2.Lerp(chew, mouth, Bite(clockMs));
        }

        // Gira la cola alrededor de su base (grados; + hacia arriba con el miquito mirando a la derecha).
        private static void RotateTail(ref MonkeyPose p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            p.Tail1 = Rotate(p.Tail1, p.Tail0, c, s);
            p.Tail2 = Rotate(p.Tail2, p.Tail0, c, s);
            p.Tail3 = Rotate(p.Tail3, p.Tail0, c, s);
        }

        private static Vector2 Rotate(Vector2 v, Vector2 pivot, float c, float s)
        {
            Vector2 d = v - pivot;
            return pivot + new Vector2((d.x * c) - (d.y * s), (d.x * s) + (d.y * c));
        }

        /// <summary>De viaje por la rama a cuatro patas (balanceo de ±2 dp a 8 Hz y extremidades de ±25°).</summary>
        public static MonkeyPose Walk(MonkeyArt art, float phase, float bobS)
        {
            float sw = Mathf.Sin(phase);
            float b = bobS * Mathf.Abs(sw);
            float l = Mathf.Min(1.15f, art.LimbScale);
            var p = new MonkeyPose
            {
                TorsoCenter = new Vector2(-0.04f, 0.33f + b),
                TorsoSize = new Vector2(0.58f, 0.32f),
                TorsoAngle = 0f,
                HeadCenter = new Vector2(0.30f, 0.50f + b),
                HeadAngle = -5f,
                FrontArmFrom = new Vector2(0.16f, 0.34f + b),
                FrontArmTo = new Vector2(0.22f + (0.08f * sw), 0.34f - (0.34f * l) + b * 0.2f),
                BackArmFrom = new Vector2(0.12f, 0.34f + b),
                BackArmTo = new Vector2(0.10f - (0.08f * sw), 0.34f - (0.34f * l) + b * 0.2f),
                FrontLegFrom = new Vector2(-0.22f, 0.32f + b),
                FrontLegTo = new Vector2(-0.18f - (0.08f * sw), 0f),
                BackLegFrom = new Vector2(-0.26f, 0.32f + b),
                BackLegTo = new Vector2(-0.30f + (0.08f * sw), 0f),
            };

            SitTail(art, ref p, new Vector2(-0.32f, 0.36f + b));
            return p;
        }

        /// <summary>Ciclo de las extremidades al trepar por la liana lateral (Hz).</summary>
        public const float ClimbHz = 5f;

        /// <summary>
        /// Mano que lanza al trepar (S, en la pose de <see cref="Climb"/>): delante del pecho, por debajo del hombro (0,18 S) y
        /// sobre la liana, del lado del tablero; un lanzamiento de lado, nunca por encima del hombro (guion §4.4; 07 §5.1,
        /// fase d). La misma que planifica la intro: <see cref="IntroChoreography.SideThrowAlongS"/> y
        /// <see cref="IntroChoreography.SideThrowOutS"/> (revisión de TL1).
        /// </summary>
        public static readonly Vector2 SideThrowHandS = new Vector2(IntroChoreography.SideThrowAlongS, IntroChoreography.SideThrowOutS);

        /// <summary>
        /// Mano libre que lanza colgado de la liana lateral (S, en la pose de <see cref="Hang"/>): bajo la cabeza y hacia el
        /// tablero (<see cref="IntroChoreography.SideHangHandInS"/> y <see cref="IntroChoreography.SideHangHandDownS"/>).
        /// </summary>
        public static readonly Vector2 SideHangHandS = new Vector2(IntroChoreography.SideHangHandInS, -IntroChoreography.SideHangHandDownS);

        /// <summary>
        /// Trepando por una liana vertical (tropa lateral, D-021; modelos <c>fila</c> y <c>sacude</c>). En la pose, la liana es el
        /// eje x (y = 0), la cabeza va hacia +x y el lomo hacia +y; <see cref="TroopLayer"/> gira el rig 90° para que la liana
        /// quede vertical, la cabeza arriba (mira a lo largo de la liana, nunca a una cuia: D-002) y el lomo hacia el borde de la
        /// pantalla. El cuerpo va pegado a la liana, compacto: manos, pies y cola asoman ≤ 0,12 S hacia el tablero
        /// (<see cref="IntroChoreography.SideClimbReachS"/>) y el lomo y la cabeza ≤ 0,66 S hacia fuera
        /// (<see cref="IntroChoreography.SideClimbBodyS"/>). <paramref name="phase"/> mueve las extremidades al trepar (0:
        /// agarrado y quieto) y <paramref name="lift"/> lleva el brazo de delante a la mano que lanza.
        /// </summary>
        public static MonkeyPose Climb(MonkeyArt art, float phase, float lift)
        {
            float sw = Mathf.Sin(phase);
            float l = Mathf.Min(1.15f, art.LimbScale);
            var p = new MonkeyPose
            {
                TorsoCenter = new Vector2(0f, 0.20f),
                TorsoSize = new Vector2(0.54f, 0.28f),
                TorsoAngle = 0f,
                HeadCenter = new Vector2(0.34f, 0.25f),
                HeadAngle = 0f,
                FrontArmFrom = new Vector2(0.18f, 0.24f),
                FrontArmTo = new Vector2(0.18f + (0.16f * l) + (0.06f * sw), 0.03f),
                BackArmFrom = new Vector2(0.14f, 0.24f),
                BackArmTo = new Vector2(0.14f + (0.12f * l) - (0.06f * sw), 0.03f),
                FrontLegFrom = new Vector2(-0.16f, 0.18f),
                FrontLegTo = new Vector2(-0.10f - (0.06f * sw), 0.03f),
                BackLegFrom = new Vector2(-0.20f, 0.18f),
                BackLegTo = new Vector2(-0.28f + (0.06f * sw), 0.03f),
            };

            if (lift > 0f)
            {
                p.FrontArmTo = Vector2.Lerp(p.FrontArmTo, SideThrowHandS, Mathf.Clamp01(lift));
            }

            // La cola cuelga a lo largo de la liana (hacia −x), un poco separada de ella; la prensil se enrosca en la liana.
            var b = new Vector2(-0.24f, 0.20f);
            p.Tail0 = b;
            if (art.TailCurls)
            {
                p.Tail1 = b + new Vector2(-0.20f, -0.04f);
                p.Tail2 = b + new Vector2(-0.34f, -0.16f);
                p.Tail3 = b + new Vector2(-0.24f, -0.18f);
                return p;
            }

            float k = art.TailLength;
            p.Tail1 = b + (new Vector2(-0.22f, 0.04f) * k);
            p.Tail2 = b + (new Vector2(-0.42f, 0.08f) * k);
            p.Tail3 = b + (new Vector2(-0.60f, 0.02f) * k);
            return p;
        }

        /// <summary>Ángulo del brazo que agarra la liana con la vertical (grados): sale del hombro hacia la liana, al lado de la cabeza.</summary>
        public const float HangArmDeg = 22f;

        /// <summary>
        /// Colgado bajo el agarre (modelo <c>cuelga</c>): de un brazo (calitrícidos), de la cola (atélidos) o de la cola y de un
        /// brazo (el maquisapa), con la mano libre y la fruta en ella (el churuco la lleva abrazada al pecho hasta soltarla);
        /// <paramref name="lift"/> la lleva a la mano de soltar. Revisión del 2026-10-11 (se leía como una figura colgada de una
        /// cuerda atada a la cabeza): la mano que agarra está en la liana a un lado de la cabeza, con el brazo saliendo del
        /// hombro a 22° y a la vista en un largo como el de la cabeza, que no queda bajo el agarre; las piernas, recogidas
        /// (rodillas hacia delante); y la cola prensil sube a la liana y se enrosca bajo la mano. Lo más bajo del cuerpo nunca
        /// pasa de la mano de soltar (<see cref="HangHandS"/>).
        /// </summary>
        public static MonkeyPose Hang(MonkeyArt art, float lift, float sway)
        {
            return Hang(art, lift, sway, HangHandS);
        }

        /// <summary>
        /// Como <see cref="Hang(MonkeyArt, float, float)"/>, pero <paramref name="lift"/> lleva la mano libre a
        /// <paramref name="handS"/> (la tropa lateral lanza con <see cref="SideHangHandS"/>).
        /// </summary>
        public static MonkeyPose Hang(MonkeyArt art, float lift, float sway, Vector2 handS)
        {
            bool tailGrip = art.TailCurls;
            bool armGrip = art.ArmUp || !tailGrip;
            float l = art.LimbScale;
            float skullR = SkullRadiusS * art.HeadS;

            // Brazo que agarra (el de detrás): del agarre baja hacia delante hasta el hombro; la cabeza, a su lado, delante.
            float arm = Mathf.Clamp(0.50f * l, 0.48f, 0.60f);
            float rad = HangArmDeg * Mathf.Deg2Rad;
            var down = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
            var side = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 shoulder = down * arm;
            Vector2 skull = (down * (arm - 0.06f)) + (side * (skullR + (0.5f * art.LimbWidthS) - 0.01f));
            Vector2 head = skull + new Vector2(SkullOffsetS * art.HeadS, SkullDropS * art.HeadS);

            // Torso bajo el hombro, metido bajo el cráneo; caderas abajo y piernas recogidas hacia delante.
            float torsoH = art.ArmUp ? 0.36f : 0.40f;
            float top = skull.y - skullR + 0.07f;
            var torso = new Vector2(shoulder.x + 0.08f, top - (TorsoFill * torsoH));
            var hip = new Vector2(torso.x - 0.01f, torso.y - (TorsoFill * torsoH) + 0.03f);
            float legLen = art.ArmUp ? 0.17f : Mathf.Min(0.20f, 0.18f * l);
            Vector2 rest = art.HugsFruit ? torso + new Vector2(0.13f + sway, 0.03f) : torso + new Vector2(0.07f + sway, -0.06f);

            var p = new MonkeyPose
            {
                TorsoCenter = new Vector2(torso.x + sway, torso.y),
                TorsoSize = new Vector2(0.34f, torsoH),
                TorsoAngle = 8f + (sway * 40f),
                HeadCenter = new Vector2(head.x + (sway * 0.5f), head.y),
                BackArmFrom = new Vector2(shoulder.x + sway, shoulder.y),
                BackArmTo = Vector2.zero, // en la liana
                FrontArmFrom = new Vector2(shoulder.x + 0.11f + sway, shoulder.y - 0.03f),
                FrontArmTo = Vector2.Lerp(rest, handS, Mathf.Clamp01(lift)),
                BackLegFrom = new Vector2(hip.x - 0.03f + sway, hip.y),
                BackLegTo = new Vector2(hip.x + 0.10f + (sway * 1.5f), hip.y - legLen),
                FrontLegFrom = new Vector2(hip.x + 0.04f + sway, hip.y),
                FrontLegTo = new Vector2(hip.x + 0.18f + (sway * 1.5f), hip.y - (0.85f * legLen)),
            };

            if (!armGrip)
            {
                // Colgado solo de la cola: las dos manos libres, juntas delante del pecho (el churuco abraza ahí su fruta).
                p.BackArmFrom = new Vector2(shoulder.x + 0.03f + sway, shoulder.y);
                p.BackArmTo = rest + new Vector2(-0.04f, 0.03f);
            }

            if (tailGrip)
            {
                // La cola prensil sube por detrás hasta la liana y se enrosca justo bajo el agarre.
                p.Tail0 = new Vector2(hip.x - 0.09f + sway, hip.y + 0.05f);
                p.Tail1 = p.Tail0 + new Vector2(-0.32f, -0.10f);
                p.Tail2 = new Vector2(-0.26f, -0.12f);
                p.Tail3 = new Vector2(-0.01f, -0.04f);
            }
            else
            {
                float k = art.TailLength;
                p.Tail0 = new Vector2(hip.x - 0.09f + sway, hip.y + 0.03f);
                p.Tail1 = p.Tail0 + (new Vector2(-0.26f, -0.12f) * k);
                p.Tail2 = p.Tail0 + (new Vector2(-0.42f, 0.14f) * k);
                p.Tail3 = p.Tail0 + (new Vector2(-0.32f, 0.40f) * k); // hacia arriba: nada cuelga hacia el panel
            }

            return p;
        }

        /// <summary>
        /// Colgado de la liana lateral (tropa lateral, D-021; revisión de TL1): <see cref="Hang(MonkeyArt, float, float, Vector2)"/>
        /// con la mano que lanza en <see cref="SideHangHandS"/> y todo el cuerpo desplazado <paramref name="shiftS"/> hacia el
        /// borde de la pantalla (−x), para que cuelgue justo debajo del agarre, con el centro del cuerpo sobre la liana; el agarre
        /// (la mano que agarra y el enrosque de la cola) se queda en la liana. Ningún punto de control de la cola pasa de
        /// <paramref name="minXS"/> (S, hacia el borde): la cola nunca se sale por el borde de la pantalla.
        /// </summary>
        public static MonkeyPose SideHang(MonkeyArt art, float lift, float sway, float shiftS, float minXS)
        {
            MonkeyPose p = Hang(art, lift, sway, SideHangHandS);
            if (shiftS <= 0f)
            {
                return p;
            }

            bool tailGrip = art.TailCurls;
            bool armGrip = art.ArmUp || !tailGrip;
            var d = new Vector2(-shiftS, 0f);
            p.TorsoCenter += d;
            p.HeadCenter += d;
            p.BackArmFrom += d;
            if (!armGrip)
            {
                p.BackArmTo += d; // sin brazo que agarre, la mano de detrás va con el cuerpo
            }

            p.FrontArmFrom += d;
            p.FrontArmTo += d;
            p.BackLegFrom += d;
            p.BackLegTo += d;
            p.FrontLegFrom += d;
            p.FrontLegTo += d;
            p.Tail0 += d;
            p.Tail1 += d;
            if (!tailGrip)
            {
                p.Tail2 += d;
                p.Tail3 += d;
            }

            p.Tail0.x = Mathf.Max(p.Tail0.x, minXS);
            p.Tail1.x = Mathf.Max(p.Tail1.x, minXS);
            p.Tail2.x = Mathf.Max(p.Tail2.x, minXS);
            p.Tail3.x = Mathf.Max(p.Tail3.x, minXS);
            return p;
        }

        /// <summary>Pecho del miquito (S): donde el churuco abraza la fruta, sentado o colgado.</summary>
        public static Vector2 ChestS(MonkeyArt art, bool hanging)
        {
            if (!hanging)
            {
                return new Vector2(0.14f, 0.34f);
            }

            MonkeyPose p = Hang(art, 0f, 0f);
            return p.TorsoCenter + new Vector2(0.13f, 0.03f);
        }

        // El torso es una elipse que llena el 70 % del alto de su pieza (ProceduralArt.DrawTorso: 45 px de radio en 128).
        private const float TorsoFill = 0.35f;

        // Cola en arco hacia arriba y atrás (balancín), corta (huapo) o enrollada (atélidos); nunca cuelga hacia el panel.
        private static void SitTail(MonkeyArt art, ref MonkeyPose p, Vector2 base0)
        {
            float k = art.TailLength;
            p.Tail0 = base0;
            if (art.TailCurls)
            {
                p.Tail1 = base0 + new Vector2(-0.38f, -0.06f);
                p.Tail2 = base0 + new Vector2(-0.56f, 0.36f);
                p.Tail3 = base0 + new Vector2(-0.30f, 0.34f);
                return;
            }

            p.Tail1 = base0 + (new Vector2(-0.33f, -0.08f) * k);
            p.Tail2 = base0 + (new Vector2(-0.50f, 0.30f) * k);
            p.Tail3 = base0 + (new Vector2(-0.30f, 0.60f) * k);
        }

        private const float PiecePxPerDp = ProceduralArt.UiPxPerDp; // px por dp de los sprites de las piezas (PPU 300)

        private static Image Piece(string name, Transform parent, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            return img;
        }

        private static void Limb(Image img, Vector2 from, Vector2 to, float width)
        {
            Vector2 d = to - from;
            float len = Mathf.Max(width, d.magnitude + (width * 0.5f));
            float angle = (Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg) + 90f;
            Set(img.rectTransform, from, new Vector2(width, len), angle);
        }

        private static void Set(RectTransform rt, Vector2 pos, Vector2 size, float angle)
        {
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }

            Quaternion r = Quaternion.Euler(0f, 0f, angle);
            if (rt.localRotation != r)
            {
                rt.localRotation = r;
            }
        }

        private Rect ToCanvas(RectTransform space)
        {
            Rect area = space.rect;
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 l = space.InverseTransformPoint(_corners[i]);
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
