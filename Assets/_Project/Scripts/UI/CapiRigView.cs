#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El rig de Capi al remo dentro de la canoa del Mapa v2 (D-069, M2-3; propuesta del Mapa v2 §5.1-§5.3): las piezas de
    /// <see cref="MapCapiArt"/> como <see cref="Image"/> hijas de la canoa, colocadas en dp de diseño de la canoa de
    /// <see cref="ProceduralArt.CapiCanoeDesignWidthDp"/> × <see cref="ProceduralArt.CapiCanoeDesignHeightDp"/> (proa a la derecha; la
    /// canoa se voltea con su escala) y escaladas a su tamaño de la vista. De atrás adelante: la carga de cuias a popa (una por mundo
    /// completado, como mucho <see cref="MaxCargo"/>, con el agua de su mundo), Capi, el casco, el remo, la pata, la mini cuia que se
    /// amarra, las salpicaduras y la burbuja con su emoción (sin texto). Ninguna pieza recibe toques. <see cref="Apply"/> solo mueve
    /// rectángulos y cambia opacidades: sin asignaciones por fotograma.
    /// </summary>
    public sealed class CapiRigView
    {
        /// <summary>Cuias de la carga a la vista como mucho (propuesta §5.1).</summary>
        public const int MaxCargo = 5;

        /// <summary>Hombro de Capi en la canoa (dp de diseño).</summary>
        public static readonly Vector2 Shoulder = new Vector2(31f, 22.3f);

        /// <summary>Del hombro a la mano (dp).</summary>
        public const float ArmDp = 7.3f;

        /// <summary>Pivote de Capi (la grupa) en la canoa (dp de diseño).</summary>
        public static readonly Vector2 Hips = new Vector2(27.1f, 14.4f);

        /// <summary>Dónde nace el rabito de la burbuja, sobre la cabeza (dp de diseño).</summary>
        public static readonly Vector2 BubbleAt = new Vector2(37f, 35f);

        private const float Px = ProceduralArt.UiPxPerDp;
        private static readonly Vector2 SampleFrom = new Vector2(47f, 20f);
        private static readonly Vector2 SampleTo = new Vector2(12f, 21f);
        private static readonly Vector2 StowedAt = new Vector2(33f, 17f);

        private readonly RectTransform _root;
        private readonly Image[] _cargoGlass = new Image[MaxCargo];
        private readonly Image[] _cargoWater = new Image[MaxCargo];
        private readonly Image _body;
        private readonly Image _hull;
        private readonly Image _paddle;
        private readonly Image _paw;
        private readonly Image _sampleWater;
        private readonly Image _sampleGlass;
        private readonly Image _splashA;
        private readonly Image _splashB;
        private readonly RectTransform _bubble;
        private readonly Image _bubbleImage;
        private readonly Image _icon;
        private readonly Image _iconWater;
        private int _cargo;
        private float _scale = 1f;
        private CapiEmotion _shownEmotion = (CapiEmotion)(-1);

        /// <summary>Crea las piezas bajo <paramref name="canoe"/>, sin sprites hasta <see cref="SetArt"/>.</summary>
        public CapiRigView(RectTransform canoe)
        {
            _root = UiBuilder.Rect("CapiRig", canoe);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.zero;
            _root.pivot = Vector2.zero;
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(ProceduralArt.CapiCanoeDesignWidthDp, ProceduralArt.CapiCanoeDesignHeightDp);
            for (int i = 0; i < MaxCargo; i++)
            {
                _cargoWater[i] = Piece("CargoWater" + i);
                _cargoGlass[i] = Piece("CargoGlass" + i);
            }

            _body = Piece("Capi");
            _hull = Piece("Hull");
            _paddle = Piece("Paddle");
            _paw = Piece("Paw");
            _sampleWater = Piece("SampleWater");
            _sampleGlass = Piece("SampleGlass");
            _splashA = Piece("SplashA");
            _splashB = Piece("SplashB");
            _bubbleImage = Piece("Bubble");
            _bubble = _bubbleImage.rectTransform;
            _iconWater = UiBuilder.Image("IconWater", _bubble, null, Color.white);
            _iconWater.raycastTarget = false;
            _icon = UiBuilder.Image("Icon", _bubble, null, Color.white);
            _icon.raycastTarget = false;
            _icon.preserveAspect = true;
            _iconWater.preserveAspect = true;
            _root.gameObject.SetActive(false);
        }

        /// <summary>La raíz del rig (tests y capturas).</summary>
        public RectTransform Root => _root;

        /// <summary>La burbuja (tests: se ve con una emoción).</summary>
        public RectTransform Bubble => _bubble;

        /// <summary>La burbuja se ve.</summary>
        public bool BubbleShown => _root.gameObject.activeInHierarchy && _bubble.localScale.y > 0f;

        /// <summary>La emoción que enseña la burbuja (None sin burbuja).</summary>
        public CapiEmotion ShownEmotion => BubbleShown ? _shownEmotion : CapiEmotion.None;

        /// <summary>Cuias de la carga a la vista.</summary>
        public int CargoShown => _cargo;

        /// <summary>El remo (tests: sube en alto en la llegada).</summary>
        public RectTransform PaddleRect => _paddle.rectTransform;

        /// <summary>La pata (tests: la visera, el saludo).</summary>
        public RectTransform PawRect => _paw.rectTransform;

        /// <summary>La mini cuia que se amarra.</summary>
        public Image SampleGlass => _sampleGlass;

        /// <summary>La mini cuia que se amarra se ve (sube a popa).</summary>
        public bool SampleShown => _sampleGlass.enabled && _sampleGlass.canvasRenderer.GetAlpha() > 0f;

        /// <summary>Las salpicaduras del remo fuerte.</summary>
        public Image Splash => _splashA;

        /// <summary>Enciende o apaga el rig.</summary>
        public void SetActive(bool on)
        {
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
            }
        }

        /// <summary>Pone los sprites del atlas (o null para soltarlos) y la mini cuia del juego para la muestra que se amarra.</summary>
        public void SetArt(MapCapiArt? art, Sprite? miniGlass, Sprite? miniWater)
        {
            for (int i = 0; i < MaxCargo; i++)
            {
                SetSprite(_cargoGlass[i], art?.Piece(CapiRigPiece.CargoGlass), CapiRigPiece.CargoGlass);
                SetSprite(_cargoWater[i], art?.Piece(CapiRigPiece.CargoWater), CapiRigPiece.CargoWater);
            }

            SetSprite(_body, art?.Piece(CapiRigPiece.Body), CapiRigPiece.Body);
            SetSprite(_hull, art?.Piece(CapiRigPiece.Hull), CapiRigPiece.Hull);
            SetSprite(_paddle, art?.Piece(CapiRigPiece.Paddle), CapiRigPiece.Paddle);
            SetSprite(_paw, art?.Piece(CapiRigPiece.Paw), CapiRigPiece.Paw);
            SetSprite(_splashA, art?.Piece(CapiRigPiece.Splash), CapiRigPiece.Splash);
            SetSprite(_splashB, art?.Piece(CapiRigPiece.Splash), CapiRigPiece.Splash);
            SetSprite(_bubbleImage, art?.Piece(CapiRigPiece.Bubble), CapiRigPiece.Bubble);
            Mini(_sampleGlass, art != null ? miniGlass : null);
            Mini(_sampleWater, art != null ? miniWater : null);
            _shownEmotion = (CapiEmotion)(-1);
            _art = art;
            _miniGlass = art != null ? miniGlass : null;
            _miniWater = art != null ? miniWater : null;
            Place(_hull, new Vector2(30f, 4.5f), 0f);
            for (int i = 0; i < MaxCargo; i++)
            {
                Show(_cargoGlass[i], false);
                Show(_cargoWater[i], false);
            }

            Show(_sampleGlass, false);
            Show(_sampleWater, false);
            Show(_splashA, false);
            Show(_splashB, false);
            _bubble.localScale = new Vector3(1f, 0f, 1f);
            Prepare(CapiEmotion.Note);
        }

        /// <summary>
        /// Deja lista la emoción <paramref name="emotion"/> en la burbuja (sus sprites), sin enseñarla: al abrir, con la del viaje, para
        /// que el viaje no cambie nada más que escalas y opacidades.
        /// </summary>
        public void Prepare(CapiEmotion emotion)
        {
            if (emotion != CapiEmotion.None)
            {
                ShowEmotion(emotion);
            }
        }

        private MapCapiArt? _art;
        private Sprite? _miniGlass;
        private Sprite? _miniWater;

        /// <summary>
        /// La carga: las <paramref name="count"/> últimas cuias de <paramref name="colors"/> (el agua de cada mundo completado), como
        /// mucho <see cref="MaxCargo"/>, de popa hacia el centro.
        /// </summary>
        public void SetCargo(Color[] colors, int count)
        {
            int n = Mathf.Clamp(count, 0, MaxCargo);
            int first = Mathf.Max(0, count - n);
            _cargo = n;
            for (int i = 0; i < MaxCargo; i++)
            {
                bool on = i < n;
                Show(_cargoGlass[i], on);
                Show(_cargoWater[i], on);
                if (!on)
                {
                    continue;
                }

                var at = new Vector2(5f + (i * 3.3f), 14.6f + ((i % 2) * 0.8f));
                Place(_cargoGlass[i], at, 0f);
                Place(_cargoWater[i], at, 0f);
                if (_cargoWater[i].color != colors[first + i])
                {
                    _cargoWater[i].color = colors[first + i];
                }
            }
        }

        /// <summary>
        /// Aplica <paramref name="pose"/>, la burbuja con <paramref name="emotion"/> a la escala <paramref name="bubbleScale"/>, la
        /// orientación de la canoa <paramref name="facing"/> (la burbuja se lee siempre derecha) y la escala de la canoa
        /// <paramref name="scale"/> (su ancho / 60). Sin asignaciones.
        /// </summary>
        public void Apply(in CapiPose pose, CapiEmotion emotion, float bubbleScale, Color sampleWater, float facing, float scale)
        {
            if (!Mathf.Approximately(_scale, scale))
            {
                _scale = scale;
                _root.localScale = new Vector3(scale, scale, 1f);
            }

            // Capi, inclinado sobre su grupa y algo levantado.
            Place(_body, Hips + new Vector2(0f, pose.BodyLift), pose.BodyTilt);

            // La pata: el hombro gira con el cuerpo; la mano, a ArmDp del hombro con el ángulo de la postura (más la inclinación).
            Vector2 shoulder = Hips + Rotate(Shoulder - Hips, pose.BodyTilt) + new Vector2(0f, pose.BodyLift);
            float arm = pose.Arm + pose.BodyTilt;
            Place(_paw, shoulder, arm);
            float rad = arm * Mathf.Deg2Rad;
            Vector2 hand = shoulder + new Vector2(Mathf.Cos(rad) * ArmDp, Mathf.Sin(rad) * ArmDp);
            if (pose.PaddleHeld)
            {
                Place(_paddle, hand, pose.Paddle);
            }
            else
            {
                Place(_paddle, StowedAt, 78f);
            }

            // La mini cuia que se amarra: de la proa a la popa por un arco, y se desvanece en la carga.
            bool sample = pose.Sample >= 0f && _sampleGlass.sprite != null;
            if (!sample)
            {
                Show(_sampleGlass, false);
                Show(_sampleWater, false);
            }
            else
            {
                float t = pose.Sample;
                Vector2 at = Vector2.Lerp(SampleFrom, SampleTo, t) + new Vector2(0f, 7f * Mathf.Sin(t * Mathf.PI));
                float a = t > 0.8f ? Mathf.Clamp01((1f - t) / 0.2f) : 1f;
                Mini(_sampleGlass, at, a, Color.white);
                Mini(_sampleWater, at, a, sampleWater);
            }

            // Las salpicaduras del remo fuerte, delante de la proa.
            bool splash = pose.Splash > 0.01f && _splashA.sprite != null;
            if (!splash)
            {
                Show(_splashA, false);
                Show(_splashB, false);
            }
            else
            {
                Place(_splashA, new Vector2(52f, 5.5f), 0f);
                Place(_splashB, new Vector2(44f, 4.8f), -12f);
                _splashA.canvasRenderer.SetAlpha(pose.Splash);
                _splashB.canvasRenderer.SetAlpha(pose.Splash * 0.8f);
            }

            // La burbuja: sobre la cabeza, siempre derecha (deshace el volteo de la canoa).
            bool bubble = emotion != CapiEmotion.None && bubbleScale > 0f && _bubbleImage.sprite != null;
            if (!bubble)
            {
                if (_bubble.localScale.y != 0f)
                {
                    _bubble.localScale = new Vector3(1f, 0f, 1f);
                }
            }
            else
            {
                Place(_bubbleImage, BubbleAt + new Vector2(0f, pose.BodyLift), 0f);
                float flip = facing < 0f ? -1f : 1f;
                _bubble.localScale = new Vector3(bubbleScale * flip, bubbleScale, 1f);
                ShowEmotion(emotion);
            }
        }

        // El icono de la emoción dentro de la burbuja (solo cambia de sprite si cambia la emoción).
        private void ShowEmotion(CapiEmotion emotion)
        {
            if (emotion == _shownEmotion)
            {
                return;
            }

            _shownEmotion = emotion;
            bool cuia = emotion == CapiEmotion.Sample;
            Sprite? icon = cuia ? _miniGlass : _art?.Piece(IconPiece(emotion));
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _iconWater.sprite = _miniWater;
            _iconWater.enabled = _miniWater != null;
            _iconWater.canvasRenderer.SetAlpha(cuia ? 1f : 0f);
            Vector2 size = cuia ? new Vector2(8f, 12.8f) : new Vector2(12f, 12f);
            PlaceIcon(_icon.rectTransform, size);
            PlaceIcon(_iconWater.rectTransform, size);
            _iconWater.color = new Color(0.36f, 0.7f, 0.86f, 1f);
        }

        private static void PlaceIcon(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(12f, 13.4f);
        }

        private static CapiRigPiece IconPiece(CapiEmotion e)
        {
            switch (e)
            {
                case CapiEmotion.Heart:
                    return CapiRigPiece.Heart;
                case CapiEmotion.Bang:
                    return CapiRigPiece.Bang;
                case CapiEmotion.Question:
                    return CapiRigPiece.Question;
                default:
                    return CapiRigPiece.Note;
            }
        }

        private Image Piece(string name)
        {
            Image img = UiBuilder.Image(name, _root, null, Color.white);
            img.raycastTarget = false;
            img.enabled = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            return img;
        }

        private static void SetSprite(Image img, Sprite? sprite, CapiRigPiece piece)
        {
            img.sprite = sprite;
            img.enabled = sprite != null;
            RectInt c = ProceduralArt.CapiRigCell(piece);
            RectTransform rt = img.rectTransform;
            rt.pivot = ProceduralArt.CapiRigPivot(piece);
            rt.sizeDelta = new Vector2(c.width / Px, c.height / Px);
        }

        // Se ve o no, con la opacidad (la pieza sigue encendida si tiene sprite: sin asignaciones por fotograma).
        private static void Show(Image img, bool on)
        {
            float a = on ? 1f : 0f;
            if (img.canvasRenderer.GetAlpha() != a)
            {
                img.canvasRenderer.SetAlpha(a);
            }
        }

        private static void Mini(Image img, Sprite? sprite)
        {
            img.sprite = sprite;
            img.enabled = sprite != null;
            RectTransform rt = img.rectTransform;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(ProceduralArt.MiniCuiaWidthDp * 0.36f, ProceduralArt.MiniCuiaHeightDp * 0.36f);
        }

        private static void Mini(Image img, Vector2 at, float alpha, Color color)
        {
            img.rectTransform.anchoredPosition = at;
            if (img.color != color)
            {
                img.color = color;
            }

            img.canvasRenderer.SetAlpha(alpha);
        }

        private static void Place(Image img, Vector2 at, float deg)
        {
            RectTransform rt = img.rectTransform;
            rt.anchoredPosition = at;
            rt.localRotation = Quaternion.Euler(0f, 0f, deg);
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2((v.x * c) - (v.y * s), (v.x * s) + (v.y * c));
        }
    }
}
