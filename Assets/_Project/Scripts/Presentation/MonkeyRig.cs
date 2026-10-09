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

        /// <summary>Giro de la cabeza (grados; + mira arriba).</summary>
        public float HeadAngle;

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

        /// <summary>Párpados cerrados (parpadeo de 120 ms).</summary>
        public bool Blink;
    }

    /// <summary>
    /// Rig recortable de un miquito en uGUI (guion de la intro §11, Q16 A): 8 piezas (cola, pierna y brazo de detrás, torso,
    /// pierna de delante, cabeza con su párpado y brazo de delante) con las piezas de <see cref="MonkeyArt"/> y poses por
    /// código (<see cref="Sit"/>, <see cref="Walk"/>, <see cref="Hang"/>). Postura animal, sin ropa ni accesorios, y nunca
    /// mira una cuia: mira a lo largo de la rama o a la fruta de su mano (guion §4.6). Sin raycast.
    /// </summary>
    public sealed class MonkeyRig
    {
        /// <summary>Piezas del rig.</summary>
        public const int PieceCount = 8;

        /// <summary>Mano del brazo de delante al soltar, sentado (S): la fruta sale de aquí.</summary>
        public static readonly Vector2 SitHandS = new Vector2(0.30f, 0.36f);

        /// <summary>Mano libre colgado (S, bajo el agarre): la fruta sale de aquí.</summary>
        public static readonly Vector2 HangHandS = new Vector2(0.14f, -0.95f);

        private readonly RectTransform _root;
        private readonly BezierStripGraphic _tail;
        private readonly Image _backLeg;
        private readonly Image _backArm;
        private readonly Image _torso;
        private readonly Image _frontLeg;
        private readonly Image _head;
        private readonly Image _eyelid;
        private readonly Image _frontArm;
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
            _frontArm = Piece("FrontArm", _root, new Vector2(0.5f, 1f));
            _backLeg.type = Image.Type.Sliced;
            _backArm.type = Image.Type.Sliced;
            _frontLeg.type = Image.Type.Sliced;
            _frontArm.type = Image.Type.Sliced;
            _graphics = new Graphic[] { _tail, _backLeg, _backArm, _torso, _frontLeg, _head, _eyelid, _frontArm };
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
            _tail.SetColors(art.Tail, art.TailTip, 0.72f, new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f), Mathf.Max(0.8f, 0.03f * _s));
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

        /// <summary>Coloca el apoyo en (<paramref name="x"/>, <paramref name="yDown"/>) dp del lienzo, mirando a <paramref name="facing"/> (±1).</summary>
        public void Place(float x, float yDown, float facing)
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
            Set(_torso.rectTransform, pose.TorsoCenter * s, pose.TorsoSize * s, pose.TorsoAngle);
            RectTransform head = _head.rectTransform;
            Vector2 hp = pose.HeadCenter * s;
            if (head.anchoredPosition != hp)
            {
                head.anchoredPosition = hp;
            }

            Quaternion hr = Quaternion.Euler(0f, 0f, pose.HeadAngle);
            if (head.localRotation != hr)
            {
                head.localRotation = hr;
            }

            if (_eyelid.enabled != pose.Blink)
            {
                _eyelid.enabled = pose.Blink;
            }

            float w = _art.TailWidthS * s;
            _tail.SetCurve(pose.Tail0 * s, pose.Tail1 * s, pose.Tail2 * s, pose.Tail3 * s, w, w * 0.65f);
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
                FrontArmTo = Vector2.Lerp(new Vector2(0.13f + (0.09f * l), 0.46f - (0.38f * l)), SitHandS, Mathf.Clamp01(lift)),
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

        /// <summary>
        /// Colgado bajo el agarre (modelo <c>cuelga</c>): de un brazo (calitrícidos) o de la cola (atélidos), con la mano libre
        /// abajo y la fruta en ella; <paramref name="lift"/> la adelanta al soltar.
        /// </summary>
        public static MonkeyPose Hang(MonkeyArt art, float lift, float sway)
        {
            bool tailGrip = art.TailCurls;
            var p = new MonkeyPose
            {
                TorsoCenter = new Vector2(0.02f + sway, -0.52f),
                TorsoSize = new Vector2(0.34f, 0.46f),
                TorsoAngle = sway * 40f,
                HeadCenter = tailGrip ? new Vector2(0.10f + sway, -0.26f) : new Vector2(0.13f + (sway * 0.5f), -0.24f),
                BackArmFrom = new Vector2(-0.02f + sway, -0.36f),
                BackArmTo = tailGrip ? new Vector2(-0.12f + sway, -0.86f) : Vector2.zero,
                FrontArmFrom = new Vector2(0.08f + sway, -0.38f),
                FrontArmTo = Vector2.Lerp(new Vector2(0.10f + sway, -0.90f), HangHandS, Mathf.Clamp01(lift)),
                BackLegFrom = new Vector2(-0.06f + sway, -0.70f),
                BackLegTo = new Vector2(-0.12f + (sway * 1.5f), -0.92f),
                FrontLegFrom = new Vector2(0.06f + sway, -0.70f),
                FrontLegTo = new Vector2(0.02f + (sway * 1.5f), -0.92f),
            };

            if (tailGrip)
            {
                // La cola prensil sube hasta la rama y se enrolla en el agarre.
                p.Tail0 = new Vector2(-0.06f + sway, -0.62f);
                p.Tail1 = new Vector2(-0.42f, -0.40f);
                p.Tail2 = new Vector2(-0.22f, 0.12f);
                p.Tail3 = new Vector2(0.02f, 0.02f);
            }
            else
            {
                float k = art.TailLength;
                p.Tail0 = new Vector2(-0.06f + sway, -0.72f);
                p.Tail1 = p.Tail0 + (new Vector2(-0.30f, -0.08f) * k);
                p.Tail2 = p.Tail0 + (new Vector2(-0.46f, 0.20f) * k);
                p.Tail3 = p.Tail0 + (new Vector2(-0.34f, 0.47f) * k); // hacia arriba: nada cuelga hacia el panel
            }

            return p;
        }

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
