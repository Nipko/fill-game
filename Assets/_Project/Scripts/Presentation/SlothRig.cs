#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Poses del perezoso protagonista (glosario §3.4 y §11: <c>sloth_*</c>; propuesta del personaje c.6). Los ID son neutros:
    /// ningún identificador lleva el nombre del personaje (T4-A). El orden es el de c.6 y no se guarda en ningún sitio.
    /// </summary>
    public enum SlothPoseId : byte
    {
        /// <summary><c>sloth_idle</c>: cuelga de su rama, respira, parpadea y gira la cabeza muy despacio (bucle de 3 s).</summary>
        Idle = 0,

        /// <summary><c>sloth_wake</c>: abre los ojos, estira un brazo hacia la rama de arriba y gira la cabeza hacia ti (2 s).</summary>
        Wake = 1,

        /// <summary><c>sloth_wave</c>: levanta despacio una garra desde la rama, nunca hacia la cámara (0,8 s).</summary>
        Wave = 2,

        /// <summary><c>sloth_present</c>: ladea la cabeza hacia la instrucción y la señala con la garra (0,6 s y reposo).</summary>
        Present = 3,

        /// <summary><c>sloth_cheer</c>: levanta las garras y caen 4 hojas, piezas del rig (entrada de 0,3 s y bucle de 1,2 s).</summary>
        Cheer = 4,

        /// <summary><c>sloth_cheer_big</c>: igual, con una vuelta colgado de una mano y 2 hojas más (0,3 s y 1,2 s).</summary>
        CheerBig = 5,

        /// <summary><c>sloth_encourage</c>: se balancea suave en la rama y asiente despacio, mirándote (0,8 s y reposo).</summary>
        Encourage = 6,

        /// <summary><c>sloth_look</c>: gira la cabeza casi tres cuartos de vuelta y vuelve (1 s).</summary>
        Look = 7,

        /// <summary><c>sloth_sunbathe</c>: tumbado al sol sobre la rama, la barriga al aire (bucle).</summary>
        Sunbathe = 8,

        /// <summary><c>sloth_sleep</c>: hecho un ovillo sobre la rama (bucle).</summary>
        Sleep = 9,
    }

    /// <summary>
    /// Pose del perezoso en unidades de su tamaño S (el alto colgado; origen en la línea de la rama, y hacia arriba, de frente).
    /// Extremidades de articulación a mano o pie. Las piezas del cuerpo van en un contenedor que gira y se refleja alrededor de
    /// <see cref="BodyPivot"/> (balanceo y vuelta colgado de una mano); la rama y las hojas no.
    /// </summary>
    public struct SlothPose
    {
        /// <summary>Centro del torso.</summary>
        public Vector2 TorsoCenter;

        /// <summary>Ancho y alto del torso.</summary>
        public Vector2 TorsoSize;

        /// <summary>Giro del torso (grados).</summary>
        public float TorsoAngle;

        /// <summary>Centro de la cabeza.</summary>
        public Vector2 HeadCenter;

        /// <summary>Giro de la cabeza (grados; + hacia la izquierda).</summary>
        public float HeadAngle;

        /// <summary>Cuánto se gira la cabeza hacia un lado (0..1): la pieza se estrecha hasta un 40 %.</summary>
        public float HeadTurn;

        /// <summary>Hombro del brazo de detrás.</summary>
        public Vector2 BackArmFrom;

        /// <summary>Mano del brazo de detrás.</summary>
        public Vector2 BackArmTo;

        /// <summary>Hombro del brazo de delante.</summary>
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

        /// <summary>El torso da la espalda (la pieza del lomo con el espéculo): en ovillo y a media vuelta de <c>sloth_cheer_big</c>.</summary>
        public bool TorsoBack;

        /// <summary>Ojos cerrados (parpadeo de 120 ms, sueño, sol).</summary>
        public bool EyesClosed;

        /// <summary>Giro del cuerpo alrededor de <see cref="BodyPivot"/> (grados).</summary>
        public float BodyAngle;

        /// <summary>Escala horizontal del cuerpo alrededor de <see cref="BodyPivot"/> (1; −1 a media vuelta).</summary>
        public float BodySpin;

        /// <summary>Punto fijo del balanceo y de la vuelta: la mano que agarra la rama.</summary>
        public Vector2 BodyPivot;

        /// <summary>Hojas que caen (0, 4 o 6).</summary>
        public int Leaves;

        /// <summary>Fase de la caída de las hojas (0..1, un bucle).</summary>
        public float LeafPhase;

        /// <summary>Opacidad (con Reducir movimiento, el fundido de 150 ms).</summary>
        public float Alpha;
    }

    /// <summary>
    /// Rig recortable provisional del perezoso protagonista en uGUI (tarea TI4; propuesta del personaje c.4-c.7 y paso 4 de (i);
    /// D-016), con el método de <see cref="MonkeyRig"/>: 10 piezas (rama, pierna y brazo de detrás, torso de frente o de
    /// espaldas, pierna y brazo de delante, cabeza con su párpado y la hoja, de la que salen las 6 que caen al celebrar) de <see cref="SlothArt"/> y las 10
    /// poses de c.6 por código (<see cref="Evaluate"/>), funciones puras del tiempo. Postura de perezoso: colgado boca abajo de su
    /// rama y abrazado a ella con manos y pies, o tumbado y en ovillo sobre ella; nunca de pie, nunca aplaude ni estira el brazo
    /// hacia la cámara (c.5, puntos 7 y 8). Sin raycast: nunca bloquea un toque.
    /// </summary>
    /// <remarks>
    /// <para>Solo en desarrollo: sin el PNG de <see cref="ProceduralArt.SlothPartsSlot"/>, <see cref="ProceduralArt.SlothRigArt()"/>
    /// devuelve null fuera del Editor y de los builds de desarrollo (<see cref="MapArtRule"/>; 07 §6.4) y <see cref="Create"/> no
    /// crea nada. Perezoso: nada existe hasta que alguien lo pide.</para>
    /// <para>Con Reducir movimiento (07 §7.8; c.6), cada pose se sustituye por su fotograma de reposo, quieto, con un fundido de
    /// entrada de <see cref="ReducedMotionFadeMs"/> ms, y sin hojas.</para>
    /// <para>Ningún método por fotograma asigna memoria.</para>
    /// </remarks>
    public sealed class SlothRig
    {
        /// <summary>
        /// Piezas del rig: rama, extremidad, torso de frente, torso de espaldas, cabeza, párpado y hoja, con la extremidad en
        /// sus 4 usos (la hoja cuenta una vez: es la misma pieza para las 6 que caen; los dos torsos comparten imagen).
        /// </summary>
        public const int PieceCount = 10;

        /// <summary>Imágenes del rig sin contar las hojas: rama, 4 extremidades, torso, cabeza y párpado.</summary>
        public const int BodyGraphics = 8;

        /// <summary>Hojas que caen en <c>sloth_cheer_big</c>; <c>sloth_cheer</c> usa las 4 primeras (c.6).</summary>
        public const int LeafCount = 6;

        /// <summary>Hojas de <c>sloth_cheer</c> (3-4 en c.6).</summary>
        public const int CheerLeaves = 4;

        /// <summary>Fundido que sustituye a las poses con Reducir movimiento (c.6; como <c>MapConfig.ReducedMotionFadeMs</c>) [P].</summary>
        public const int ReducedMotionFadeMs = 150;

        /// <summary>Bucle de respiración de <c>sloth_idle</c> (≈ 3 s; c.6).</summary>
        public const float IdleLoopMs = 3000f;

        /// <summary>Parpadeo: cada 4,5 s (3-6 s en c.6) [P], 120 ms con los ojos cerrados.</summary>
        public const float BlinkEveryMs = 4500f;

        /// <summary>Duración del parpadeo.</summary>
        public const float BlinkMs = 120f;

        /// <summary>Giro lento de la cabeza en reposo: cada 20 s (15-30 s en c.6) [P], durante 2 s.</summary>
        public const float IdleHeadTurnEveryMs = 20000f;

        /// <summary><c>sloth_wake</c>: 2 s (≤ 2,5 s en c.6) [P].</summary>
        public const float WakeMs = 2000f;

        /// <summary><c>sloth_wave</c>: 0,8 s.</summary>
        public const float WaveMs = 800f;

        /// <summary><c>sloth_present</c>: 0,6 s y reposo.</summary>
        public const float PresentMs = 600f;

        /// <summary>Entrada de <c>sloth_cheer</c> y <c>sloth_cheer_big</c>: 0,3 s.</summary>
        public const float CheerEntryMs = 300f;

        /// <summary>Bucle de <c>sloth_cheer</c> y <c>sloth_cheer_big</c>: 1,2 s.</summary>
        public const float CheerLoopMs = 1200f;

        /// <summary><c>sloth_encourage</c>: 0,8 s y reposo.</summary>
        public const float EncourageMs = 800f;

        /// <summary><c>sloth_look</c>: 1 s.</summary>
        public const float LookMs = 1000f;

        /// <summary>Respiración lenta de <c>sloth_sunbathe</c> y <c>sloth_sleep</c>: 4 s [P].</summary>
        public const float RestLoopMs = 4000f;

        private static readonly string[] s_ids =
        {
            "sloth_idle", "sloth_wake", "sloth_wave", "sloth_present", "sloth_cheer", "sloth_cheer_big", "sloth_encourage",
            "sloth_look", "sloth_sunbathe", "sloth_sleep",
        };

        // Manos y pies en la rama (S): colgado boca abajo y abrazado a ella con los cuatro (c.5, punto 7), los brazos por dentro
        // y las piernas por fuera.
        private static readonly Vector2 BackGrip = new Vector2(-0.22f, 0.02f);
        private static readonly Vector2 FrontGrip = new Vector2(0.22f, 0.02f);
        private static readonly Vector2 BackFoot = new Vector2(-0.44f, 0.02f);
        private static readonly Vector2 FrontFoot = new Vector2(0.44f, 0.02f);

        // Centro de la rama: el punto fijo del balanceo de sloth_encourage y del vaivén de sloth_cheer.
        private static readonly Vector2 BranchCenter = new Vector2(0f, 0.02f);

        // La garra que saluda (apenas suelta la rama, 0,08 S por encima del agarre) y la que celebra (bien alta, sobre la cabeza
        // y la rama): siluetas distintas para gestos distintos.
        private static readonly Vector2 WaveHand = new Vector2(0.30f, 0.10f);
        private static readonly Vector2 CheerHand = new Vector2(0.10f, 0.22f);

        // Columnas de las hojas a lo largo de la rama (S), sus desfases en el bucle: las 4 de sloth_cheer, por fuera del cuerpo,
        // y las 2 de más de sloth_cheer_big.
        private static readonly float[] s_leafX = { -0.46f, 0.42f, -0.62f, 0.58f, -0.34f, 0.30f };
        private static readonly float[] s_leafOffset = { 0f, 0.35f, 0.6f, 0.15f, 0.8f, 0.45f };

        private readonly RectTransform _root;
        private readonly RectTransform _body;
        private readonly Image _branch;
        private readonly Image _backLeg;
        private readonly Image _backArm;
        private readonly Image _torso;
        private readonly Image _frontLeg;
        private readonly Image _frontArm;
        private readonly Image _head;
        private readonly Image _eyelid;
        private readonly Image[] _leaves = new Image[LeafCount];
        private readonly Graphic[] _graphics;
        private readonly Vector3[] _corners = new Vector3[4];
        private SlothArt? _art;
        private float _s = 72f;
        private float _alpha = -1f;
        private SlothPose _pose;

        /// <summary>Crea el rig (oculto) dentro de <paramref name="parent"/>; mejor con <see cref="Create"/>.</summary>
        public SlothRig(RectTransform parent)
        {
            var go = new GameObject("Sloth", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;

            _branch = Piece("Branch", _root, new Vector2(0.5f, 0.5f));
            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(_root, false);
            _body = (RectTransform)bodyGo.transform;
            _body.anchorMin = new Vector2(0.5f, 0.5f);
            _body.anchorMax = new Vector2(0.5f, 0.5f);
            _body.pivot = new Vector2(0.5f, 0.5f);
            _body.sizeDelta = Vector2.zero;
            _backLeg = Piece("BackLeg", _body, new Vector2(0.5f, 1f));
            _backArm = Piece("BackArm", _body, new Vector2(0.5f, 1f));
            _torso = Piece("Torso", _body, new Vector2(0.5f, 0.5f));
            _frontLeg = Piece("FrontLeg", _body, new Vector2(0.5f, 1f));
            _frontArm = Piece("FrontArm", _body, new Vector2(0.5f, 1f));
            _head = Piece("Head", _body, new Vector2(0.5f, 0.5f));
            _eyelid = Piece("Eyelid", _head.rectTransform, new Vector2(0.5f, 0.5f));
            _eyelid.enabled = false;
            _backLeg.type = Image.Type.Sliced;
            _backArm.type = Image.Type.Sliced;
            _frontLeg.type = Image.Type.Sliced;
            _frontArm.type = Image.Type.Sliced;
            for (int i = 0; i < LeafCount; i++)
            {
                _leaves[i] = Piece("Leaf" + i, _root, new Vector2(0.5f, 0.5f));
                _leaves[i].enabled = false;
            }

            _graphics = new Graphic[]
            {
                _branch, _backLeg, _backArm, _torso, _frontLeg, _frontArm, _head, _eyelid,
                _leaves[0], _leaves[1], _leaves[2], _leaves[3], _leaves[4], _leaves[5],
            };
            go.SetActive(false);
        }

        /// <summary>
        /// El rig con sus piezas, dentro de <paramref name="parent"/> y a <paramref name="sizeDp"/> dp; null si no hay piezas
        /// (<paramref name="art"/> null: release sin arte final, <see cref="ProceduralArt.SlothRigArt()"/>): entonces no se crea
        /// ningún objeto.
        /// </summary>
        public static SlothRig? Create(RectTransform parent, SlothArt? art, float sizeDp)
        {
            if (art == null)
            {
                return null;
            }

            var rig = new SlothRig(parent);
            rig.SetArt(art, sizeDp);
            return rig;
        }

        /// <summary>El ID de la pose (<c>sloth_*</c>).</summary>
        public static string IdOf(SlothPoseId pose)
        {
            return s_ids[(int)pose];
        }

        /// <summary>Raíz del rig (la rama, en la línea donde agarra).</summary>
        public RectTransform Root => _root;

        /// <summary>Tamaño S en dp (el alto colgado).</summary>
        public float Size => _s;

        /// <summary>Piezas en uso.</summary>
        public SlothArt? Art => _art;

        /// <summary>El rig está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Pose que se reproduce.</summary>
        public SlothPoseId Pose { get; private set; }

        /// <summary>Reloj de la pose (ms desde <see cref="Play"/>).</summary>
        public float ClockMs { get; private set; }

        /// <summary>Con Reducir movimiento: la pose quieta con el fundido de entrada.</summary>
        public bool ReduceMotion { get; private set; }

        /// <summary>Opacidad actual.</summary>
        public float Alpha => _alpha;

        /// <summary>La última pose aplicada.</summary>
        public SlothPose Current => _pose;

        /// <summary>Hojas a la vista.</summary>
        public int VisibleLeaves
        {
            get
            {
                int n = 0;
                for (int i = 0; i < LeafCount; i++)
                {
                    if (_leaves[i].enabled)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>
        /// Asigna las piezas y el tamaño en pantalla. Con <paramref name="art"/> null el rig se oculta y devuelve false (release
        /// sin arte final: no aparece).
        /// </summary>
        public bool SetArt(SlothArt? art, float sizeDp)
        {
            _art = art;
            if (art == null)
            {
                SetVisible(false);
                return false;
            }

            _s = Mathf.Max(8f, sizeDp);
            _branch.sprite = art.Branch;
            _branch.enabled = art.Branch != null;
            _branch.rectTransform.sizeDelta = new Vector2(art.BranchWidthS * _s, art.BranchWidthS * 0.25f * _s);
            _branch.rectTransform.anchoredPosition = Vector2.zero;
            _backLeg.sprite = art.Limb;
            _backArm.sprite = art.Limb;
            _frontLeg.sprite = art.Limb;
            _frontArm.sprite = art.Limb;
            _torso.sprite = art.Torso;
            _head.sprite = art.Head;
            _eyelid.sprite = art.Eyelid;
            var back = new Color(0.8f, 0.8f, 0.8f, 1f); // las piezas de detrás, a la sombra
            _backLeg.color = back;
            _backArm.color = back;
            float limbPx = art.Limb.rect.width;
            float pxPerDp = limbPx / Mathf.Max(1f, art.LimbWidthS * _s);
            float mult = pxPerDp / ProceduralArt.UiPxPerDp;
            _backLeg.pixelsPerUnitMultiplier = mult;
            _backArm.pixelsPerUnitMultiplier = mult;
            _frontLeg.pixelsPerUnitMultiplier = mult;
            _frontArm.pixelsPerUnitMultiplier = mult;
            _head.rectTransform.sizeDelta = new Vector2(art.HeadS * _s, art.HeadS * _s);
            RectTransform lid = _eyelid.rectTransform;
            lid.anchorMin = new Vector2(0.5f, 0.5f);
            lid.anchorMax = new Vector2(0.5f, 0.5f);
            lid.sizeDelta = art.EyelidSizeS * _s;
            lid.anchoredPosition = art.EyelidOffsetS * _s;
            for (int i = 0; i < LeafCount; i++)
            {
                _leaves[i].sprite = art.Leaf;
                _leaves[i].rectTransform.sizeDelta = new Vector2(art.LeafS * _s, art.LeafS * _s);
            }

            _alpha = -1f;
            return true;
        }

        /// <summary>Muestra u oculta el rig; sin piezas nunca se muestra.</summary>
        public void SetVisible(bool on)
        {
            on = on && _art != null;
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
                _alpha = -1f;
            }
        }

        /// <summary>
        /// Coloca la línea de la rama en (<paramref name="x"/>, <paramref name="yDown"/>) dp del lienzo, mirando a
        /// <paramref name="facing"/> (±1: hacia dónde señala <c>sloth_present</c>).
        /// </summary>
        public void Place(float x, float yDown, float facing = 1f)
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

        /// <summary>
        /// Empieza la pose <paramref name="pose"/> desde 0 ms (con <paramref name="reduceMotion"/>, su fotograma de reposo con el
        /// fundido de 150 ms), muestra el rig y la aplica. Sin piezas, no hace nada.
        /// </summary>
        public void Play(SlothPoseId pose, bool reduceMotion)
        {
            Pose = pose;
            ReduceMotion = reduceMotion;
            ClockMs = 0f;
            SetVisible(true);
            Apply(Evaluate(pose, 0f, reduceMotion));
        }

        /// <summary>Avanza el reloj de la pose <paramref name="dtMs"/> ms y la aplica. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            Seek(ClockMs + Mathf.Max(0f, dtMs));
        }

        /// <summary>Lleva la pose al instante <paramref name="ms"/> y la aplica (capturas deterministas). Sin asignaciones.</summary>
        public void Seek(float ms)
        {
            ClockMs = Mathf.Max(0f, ms);
            if (_art != null && Visible)
            {
                Apply(Evaluate(Pose, ClockMs, ReduceMotion));
            }
        }

        /// <summary>Aplica una pose. Sin asignaciones.</summary>
        public void Apply(in SlothPose pose)
        {
            _pose = pose;
            if (_art == null)
            {
                return;
            }

            float s = _s;
            float limbW = _art.LimbWidthS * s;

            // El cuerpo gira y se refleja alrededor de la mano que agarra: T(p) = P + R · E · (p − P).
            Vector2 pivot = pose.BodyPivot * s;
            float spin = pose.BodySpin;
            Quaternion rot = Quaternion.Euler(0f, 0f, pose.BodyAngle);
            Vector2 scaled = new Vector2(spin * pivot.x, pivot.y);
            Vector2 turned = rot * scaled;
            SetTransform(_body, pivot - turned, rot, new Vector3(spin, 1f, 1f));

            Limb(_backLeg, pose.BackLegFrom * s, pose.BackLegTo * s, limbW);
            Limb(_backArm, pose.BackArmFrom * s, pose.BackArmTo * s, limbW);
            Limb(_frontLeg, pose.FrontLegFrom * s, pose.FrontLegTo * s, limbW);
            Limb(_frontArm, pose.FrontArmFrom * s, pose.FrontArmTo * s, limbW);
            Sprite torso = pose.TorsoBack ? _art.TorsoBack : _art.Torso;
            if (_torso.sprite != torso)
            {
                _torso.sprite = torso;
            }

            Set(_torso.rectTransform, pose.TorsoCenter * s, pose.TorsoSize * s, pose.TorsoAngle);
            SetTransform(_head.rectTransform, pose.HeadCenter * s, Quaternion.Euler(0f, 0f, pose.HeadAngle), new Vector3(1f - (0.4f * Mathf.Clamp01(pose.HeadTurn)), 1f, 1f));
            if (_eyelid.enabled != pose.EyesClosed)
            {
                _eyelid.enabled = pose.EyesClosed;
            }

            float alpha = Mathf.Clamp01(pose.Alpha);
            for (int i = 0; i < LeafCount; i++)
            {
                Image leaf = _leaves[i];
                bool on = i < pose.Leaves;
                float leafAlpha = 0f;
                if (on)
                {
                    LeafAt(i, pose.LeafPhase, out Vector2 at, out float angle, out leafAlpha);
                    Set(leaf.rectTransform, at * s, leaf.rectTransform.sizeDelta, angle);
                    on = leafAlpha > 0.001f;
                }

                if (leaf.enabled != on)
                {
                    leaf.enabled = on;
                }

                if (on)
                {
                    leaf.canvasRenderer.SetAlpha(alpha * leafAlpha);
                }
            }

            SetAlpha(alpha);
        }

        /// <summary>Opacidad de las piezas del cuerpo y de la rama (las hojas llevan además la suya).</summary>
        private void SetAlpha(float alpha)
        {
            if (Mathf.Abs(alpha - _alpha) < 0.002f)
            {
                return;
            }

            _alpha = alpha;
            for (int i = 0; i < _graphics.Length - LeafCount; i++)
            {
                _graphics[i].canvasRenderer.SetAlpha(alpha);
            }
        }

        /// <summary>Añade los gráficos del rig (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.AddRange(_graphics);
        }

        /// <summary>Deja registradas en su Canvas las piezas ocultas (párpado y hojas): ver <see cref="GraphicPool"/>.</summary>
        public void EnsureRegistered()
        {
            for (int i = 0; i < _graphics.Length; i++)
            {
                GraphicPool.EnsureRegistered(_graphics[i]);
            }
        }

        /// <summary>
        /// Añade la caja de cada pieza visible en dp del lienzo con la y desde arriba (<paramref name="space"/> es una capa a
        /// pantalla completa con el pivote en el centro, como el lienzo; como <see cref="MonkeyRig.CollectRects"/>).
        /// </summary>
        public void CollectRects(RectTransform space, List<Rect> rects)
        {
            if (!Visible)
            {
                return;
            }

            for (int i = 0; i < _graphics.Length; i++)
            {
                if (!_graphics[i].enabled || !_graphics[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                _graphics[i].rectTransform.GetWorldCorners(_corners);
                rects.Add(ToCanvas(space));
            }
        }

        /// <summary>
        /// La pose <paramref name="pose"/> a los <paramref name="ms"/> ms de empezar: función pura del tiempo (capturas
        /// deterministas). Con <paramref name="reduceMotion"/>, su fotograma de reposo, quieto y sin hojas, con la opacidad del
        /// fundido de entrada (0 → 1 en <see cref="ReducedMotionFadeMs"/> ms).
        /// </summary>
        public static SlothPose Evaluate(SlothPoseId pose, float ms, bool reduceMotion)
        {
            ms = Mathf.Max(0f, ms);
            SlothPose p = reduceMotion ? Still(pose) : Animate(pose, ms);
            p.Alpha = reduceMotion ? Mathf.Clamp01(ms / ReducedMotionFadeMs) : 1f;
            return p;
        }

        /// <summary>
        /// Hoja <paramref name="index"/> en la fase <paramref name="phase"/> (0..1) de su caída: posición (S, desde la rama),
        /// giro (grados) y opacidad (aparece en el primer 10 % y se funde en el último 25 %). Sin asignaciones.
        /// </summary>
        public static void LeafAt(int index, float phase, out Vector2 position, out float angle, out float alpha)
        {
            float p = Frac(phase + s_leafOffset[index]);
            float wave = Mathf.Sin(2f * Mathf.PI * ((p * 1.5f) + (index * 0.3f)));
            position = new Vector2(s_leafX[index] + (0.06f * wave), 0.02f - (1.0f * p));
            angle = (40f * Mathf.Sin(2f * Mathf.PI * (p + (index * 0.2f)))) + (index * 30f);
            alpha = Mathf.Clamp01(p / 0.1f) * Mathf.Clamp01((1f - p) / 0.25f);
        }

        // Fotograma de reposo de cada pose (Reducir movimiento): lo que dice la pose sin moverse.
        private static SlothPose Still(SlothPoseId pose)
        {
            switch (pose)
            {
                case SlothPoseId.Present:
                    return Present(1f, 1f);
                case SlothPoseId.Cheer:
                case SlothPoseId.CheerBig:
                    return CheerArm(Hang(1f), 1f, 0f);
                case SlothPoseId.Encourage:
                {
                    SlothPose p = Hang(1f);
                    p.HeadAngle = 6f; // te mira con la cabeza ladeada
                    return p;
                }

                case SlothPoseId.Sunbathe:
                    return Sunbathe(1f);
                case SlothPoseId.Sleep:
                    return Sleep(1f);
                default:
                    return Hang(1f); // idle, wake (ya despierto), wave, look
            }
        }

        private static SlothPose Animate(SlothPoseId pose, float ms)
        {
            switch (pose)
            {
                case SlothPoseId.Wake:
                    return Wake(ms);
                case SlothPoseId.Wave:
                    return Wave(ms);
                case SlothPoseId.Present:
                    return Present(Ease(ms / PresentMs), Breath(ms, IdleLoopMs));
                case SlothPoseId.Cheer:
                case SlothPoseId.CheerBig:
                    return Cheer(ms, pose == SlothPoseId.CheerBig);
                case SlothPoseId.Encourage:
                    return Encourage(ms);
                case SlothPoseId.Look:
                    return Look(ms);
                case SlothPoseId.Sunbathe:
                    return Sunbathe(Breath(ms, RestLoopMs));
                case SlothPoseId.Sleep:
                {
                    SlothPose p = Sleep(Breath(ms, RestLoopMs));
                    p.TorsoSize.x *= 1f + (0.5f * (Breath(ms, RestLoopMs) - 1f)); // el ovillo se hincha un poco al respirar
                    return p;
                }

                default:
                    return Idle(ms);
            }
        }

        // Colgado boca abajo bajo su rama y abrazado a ella (c.5, punto 7): las dos manos y los dos pies agarrados a la rama, el
        // cuerpo debajo, de frente, con la cabeza entre los brazos; los brazos por dentro y las piernas por fuera. Respira con
        // breathe (1,00 ↔ 1,02).
        private static SlothPose Hang(float breathe)
        {
            return new SlothPose
            {
                TorsoCenter = new Vector2(0f, -0.60f),
                TorsoSize = new Vector2(0.50f, 0.56f * breathe),
                HeadCenter = new Vector2(0f, -0.28f + ((breathe - 1f) * 0.3f)),
                BackArmFrom = new Vector2(-0.15f, -0.40f),
                BackArmTo = BackGrip,
                FrontArmFrom = new Vector2(0.15f, -0.40f),
                FrontArmTo = FrontGrip,
                BackLegFrom = new Vector2(-0.20f, -0.72f),
                BackLegTo = BackFoot,
                FrontLegFrom = new Vector2(0.20f, -0.72f),
                FrontLegTo = FrontFoot,
                BodySpin = 1f,
                BodyPivot = BackGrip,
                Alpha = 1f,
            };
        }

        // sloth_idle: respira (3 s), parpadea cada 4,5 s y cada 20 s gira la cabeza muy despacio durante 2 s.
        private static SlothPose Idle(float ms)
        {
            SlothPose p = Hang(Breath(ms, IdleLoopMs));
            p.EyesClosed = Blink(ms);
            float u = (Mod(ms, IdleHeadTurnEveryMs) - 10000f) / 2000f;
            if (u > 0f && u < 1f)
            {
                float b = Mathf.Sin(Mathf.PI * u);
                p.HeadAngle = 10f * b;
                p.HeadTurn = 0.25f * b;
            }

            return p;
        }

        // sloth_wake (2 s): con los ojos cerrados y la cabeza caída de lado hasta 0,5 s; estira el brazo de delante hacia la rama
        // de arriba (0,5-1,4 s) y gira la cabeza hacia ti (1,4-2,0 s). Después, en reposo.
        private static SlothPose Wake(float ms)
        {
            if (ms >= WakeMs)
            {
                return Idle(ms - WakeMs);
            }

            SlothPose p = Hang(Breath(ms, IdleLoopMs));
            p.EyesClosed = ms < 500f;
            float aside = 1f - Ease((ms - 1400f) / 600f);
            p.HeadAngle = 14f * aside;
            p.HeadTurn = 0.3f * aside;
            p.HeadCenter.y -= 0.02f * (1f - Ease(ms / 500f)); // la cabeza, caída, se levanta al abrir los ojos
            float reach = Mathf.Sin(Mathf.PI * Mathf.Clamp01((ms - 500f) / 900f));
            p.FrontArmTo = Vector2.Lerp(FrontGrip, new Vector2(0.30f, 0.16f), reach); // sin estirar la pieza más de ≈ 0,6 S
            return p;
        }

        // sloth_wave (0,8 s): la garra de delante se suelta despacio de la rama, apenas 0,08 S por encima del agarre y nunca
        // hacia la cámara (c.5, punto 8), y vuelve; la cabeza te mira de frente.
        private static SlothPose Wave(float ms)
        {
            SlothPose p = Hang(Breath(ms, IdleLoopMs));
            p.HeadAngle = 0f;
            p.HeadTurn = 0f;
            if (ms < WaveMs)
            {
                p.FrontArmTo = Vector2.Lerp(FrontGrip, WaveHand, Mathf.Sin(Mathf.PI * ms / WaveMs));
            }

            return p;
        }

        // sloth_present: ladea la cabeza hacia la instrucción (a su derecha) y la señala con la garra de delante, a la altura del
        // hombro (e = 0..1 de la entrada).
        private static SlothPose Present(float e, float breathe)
        {
            SlothPose p = Hang(breathe);
            p.HeadAngle = -12f * e;
            p.FrontArmTo = Vector2.Lerp(FrontGrip, new Vector2(0.52f, -0.30f), e);
            return p;
        }

        // El brazo de celebrar: sube de la rama a lo alto, por encima de la cabeza y de la rama (e = 0..1), con un vaivén de bob
        // (−1..1) y la cabeza ladeada ±8°. Bien distinto del saludo (que apenas se suelta) y del despertar (hacia fuera).
        private static SlothPose CheerArm(SlothPose p, float e, float bob)
        {
            p.FrontArmTo = Vector2.Lerp(FrontGrip, new Vector2(CheerHand.x + (0.03f * bob), CheerHand.y), e);
            p.HeadAngle = 8f * bob;
            return p;
        }

        // sloth_cheer / sloth_cheer_big: entrada de 0,3 s (la garra sube) y bucle de 1,2 s (vaivén, balanceo y hojas que caen);
        // la grande da una vuelta colgado de la mano de detrás en el primer bucle (suelta los pies, que cuelgan, y a media vuelta
        // enseña el lomo con el espéculo; la cabeza te sigue mirando, como el giro real de c.6) y suelta 6 hojas.
        private static SlothPose Cheer(float ms, bool big)
        {
            float loop = ms < CheerEntryMs ? 0f : Mod(ms - CheerEntryMs, CheerLoopMs) / CheerLoopMs;
            float bob = ms < CheerEntryMs ? 0f : Mathf.Sin(2f * Mathf.PI * loop);
            SlothPose p = CheerArm(Hang(Breath(ms, IdleLoopMs)), Ease(ms / CheerEntryMs), bob);
            p.BodyAngle = 2f * bob;
            p.BodyPivot = BranchCenter;
            p.Leaves = big ? LeafCount : CheerLeaves;
            p.LeafPhase = Mod(ms, CheerLoopMs) / CheerLoopMs;
            if (big && ms >= CheerEntryMs && ms < CheerEntryMs + CheerLoopMs)
            {
                float turn = Ease((ms - CheerEntryMs) / CheerLoopMs);
                p.BodySpin = Mathf.Cos(2f * Mathf.PI * turn);
                p.BodyAngle = 0f;
                p.BodyPivot = BackGrip;
                p.TorsoBack = p.BodySpin < 0f;
                float drop = Mathf.Sin(Mathf.PI * turn);
                p.BackLegTo = Vector2.Lerp(BackFoot, new Vector2(-0.25f, -0.92f), drop);
                p.FrontLegTo = Vector2.Lerp(FrontFoot, new Vector2(0.25f, -0.92f), drop);
            }

            return p;
        }

        // sloth_encourage (0,8 s): se balancea suave en la rama (±5° alrededor de su centro, que se apaga) y asiente dos veces,
        // mirándote; después, en reposo con la cabeza ladeada.
        private static SlothPose Encourage(float ms)
        {
            SlothPose p = Hang(Breath(ms, IdleLoopMs));
            p.HeadAngle = 6f * Ease(ms / 200f);
            p.BodyPivot = BranchCenter;
            if (ms < EncourageMs)
            {
                float u = ms / EncourageMs;
                p.BodyAngle = 5f * Mathf.Sin(2f * Mathf.PI * u) * (1f - u);
                p.HeadCenter.y -= 0.035f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * u));
            }

            return p;
        }

        // sloth_look (1 s): la cabeza gira casi tres cuartos de vuelta hacia un lado (giro de 40° y la pieza estrechada) y vuelve.
        private static SlothPose Look(float ms)
        {
            SlothPose p = Hang(Breath(ms, IdleLoopMs));
            if (ms < LookMs)
            {
                float b = Ease(Mathf.Sin(Mathf.PI * ms / LookMs));
                p.HeadAngle = 40f * b;
                p.HeadTurn = 0.6f * b;
            }

            return p;
        }

        // sloth_sunbathe: tumbado boca arriba sobre la rama, la cabeza inclinada y apoyada en ella, y los ojos cerrados; un brazo
        // y una pierna cuelgan por delante de la rama y los otros descansan sobre la barriga, dentro del contorno del torso (x de
        // −0,24 a 0,32 e y de 0 a 0,50), con las garras incluidas: el brazo cruzado en la mitad baja y la pierna recogida en la
        // alta, hacia el pecho. Nada sobresale por encima.
        private static SlothPose Sunbathe(float breathe)
        {
            return new SlothPose
            {
                TorsoCenter = new Vector2(0.04f, 0.25f),
                TorsoSize = new Vector2(0.50f * breathe, 0.56f),
                TorsoAngle = 90f,
                HeadCenter = new Vector2(-0.36f, 0.20f),
                HeadAngle = 45f,
                EyesClosed = true,
                BackArmFrom = new Vector2(-0.16f, 0.20f),
                BackArmTo = new Vector2(-0.22f, -0.22f),
                FrontArmFrom = new Vector2(-0.14f, 0.24f),
                FrontArmTo = new Vector2(0.10f, 0.22f),
                BackLegFrom = new Vector2(0.22f, 0.18f),
                BackLegTo = new Vector2(0.30f, -0.20f),
                FrontLegFrom = new Vector2(0.22f, 0.32f),
                FrontLegTo = new Vector2(0.06f, 0.38f),
                BodySpin = 1f,
                BodyPivot = Vector2.zero,
                Alpha = 1f,
            };
        }

        // sloth_sleep: hecho un ovillo sobre la rama, de espaldas (el lomo con el espéculo a la vista, arriba a la izquierda), con
        // la cabeza metida abajo a la derecha, apoyada en la rama, y los ojos cerrados. El brazo de detrás va bajo el torso hasta
        // la rama (solo asoma la garra) y el de delante, detrás de la cabeza; los pies, agarrados a la rama.
        private static SlothPose Sleep(float breathe)
        {
            return new SlothPose
            {
                TorsoCenter = new Vector2(-0.06f, 0.28f),
                TorsoSize = new Vector2(0.56f, 0.52f * breathe),
                TorsoAngle = -10f,
                TorsoBack = true,
                HeadCenter = new Vector2(0.20f, 0.13f),
                HeadAngle = -30f,
                EyesClosed = true,
                BackArmFrom = new Vector2(-0.18f, 0.30f),
                BackArmTo = new Vector2(0.02f, 0.06f),
                FrontArmFrom = new Vector2(0.10f, 0.22f),
                FrontArmTo = new Vector2(0.16f, 0.04f),
                BackLegFrom = new Vector2(-0.20f, 0.14f),
                BackLegTo = new Vector2(-0.28f, -0.02f),
                FrontLegFrom = new Vector2(-0.04f, 0.12f),
                FrontLegTo = new Vector2(-0.08f, -0.03f),
                BodySpin = 1f,
                BodyPivot = Vector2.zero,
                Alpha = 1f,
            };
        }

        // Respiración: 1,00 → 1,02 → 1,00 en periodMs.
        private static float Breath(float ms, float periodMs)
        {
            return 1f + (0.01f * (1f - Mathf.Cos(2f * Mathf.PI * ms / periodMs)));
        }

        // Ojos cerrados los últimos 120 ms de cada ciclo de parpadeo (en 0 ms, abiertos).
        private static bool Blink(float ms)
        {
            return Mod(ms, BlinkEveryMs) >= BlinkEveryMs - BlinkMs;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }

        private static float Mod(float a, float m)
        {
            return a - (Mathf.Floor(a / m) * m);
        }

        private static float Frac(float x)
        {
            return x - Mathf.Floor(x);
        }

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

        private static void SetTransform(RectTransform rt, Vector2 pos, Quaternion rotation, Vector3 scale)
        {
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            if (rt.localRotation != rotation)
            {
                rt.localRotation = rotation;
            }

            if (rt.localScale != scale)
            {
                rt.localScale = scale;
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
