#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Escrituras de la tripulación del Mapa v2 (D-069, M2-4) que solo tocan el lienzo si el valor cambia: en reposo no se rehace nada.</summary>
    internal static class CrewDraw
    {
        internal static void SetPos(RectTransform rt, Vector2 p)
        {
            if ((rt.anchoredPosition - p).sqrMagnitude > 1e-6f)
            {
                rt.anchoredPosition = p;
            }
        }

        internal static void SetSize(RectTransform rt, Vector2 s)
        {
            if ((rt.sizeDelta - s).sqrMagnitude > 1e-6f)
            {
                rt.sizeDelta = s;
            }
        }

        internal static void SetScale(RectTransform rt, Vector3 s)
        {
            if ((rt.localScale - s).sqrMagnitude > 1e-8f)
            {
                rt.localScale = s;
            }
        }

        internal static void SetRotation(RectTransform rt, float deg)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(rt.localEulerAngles.z, deg)) > 0.01f)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, deg);
            }
        }

        internal static void SetAlpha(Graphic? g, float a)
        {
            if (g != null && g.canvasRenderer.GetAlpha() != a)
            {
                g.canvasRenderer.SetAlpha(a);
            }
        }

        internal static void SetSprite(Image? img, Sprite? sprite)
        {
            if (img != null)
            {
                img.sprite = sprite;
                img.enabled = sprite != null;
            }
        }
    }

    /// <summary>
    /// El tucán en piezas (D-069, M2-4): un contenedor con las patas en su origen, el cuerpo, el ala que gira sobre el hombro y las
    /// dos mandíbulas, que se abren sobre la bisagra. En dp de diseño de la canoa (en la proa) o de la vista (en su tramo).
    /// </summary>
    internal sealed class CrewToucanView
    {
        public CrewToucanView(string name, RectTransform parent)
        {
            Root = UiBuilder.Rect(name, parent);
            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.zero;
            Root.pivot = Vector2.zero;
            Root.sizeDelta = Vector2.zero;
            Wing = Part("Wing", CrewPiece.ToucanWing);
            Body = Part("Body", CrewPiece.ToucanBody);
            Lower = Part("BeakLower", CrewPiece.ToucanBeakLower);
            Upper = Part("BeakUpper", CrewPiece.ToucanBeakUpper);
            Wing.transform.SetAsLastSibling(); // el ala, sobre el cuerpo
            Vector2 body = new Vector2(ProceduralArt.ToucanBodyDp * 0.5f, 0f);
            Body.rectTransform.anchoredPosition = Vector2.zero;
            Vector2 hinge = ProceduralArt.ToucanBeakHinge - body;
            Upper.rectTransform.anchoredPosition = hinge;
            Lower.rectTransform.anchoredPosition = hinge;
            Wing.rectTransform.anchoredPosition = ProceduralArt.ToucanShoulder - body;
            Root.gameObject.SetActive(false);
        }

        public RectTransform Root { get; }

        public Image Body { get; }

        public Image Upper { get; }

        public Image Lower { get; }

        public Image Wing { get; }

        public bool HasArt => Body.sprite != null;

        public void SetArt(MapCrewArt? art)
        {
            CrewDraw.SetSprite(Body, art?.Piece(CrewPiece.ToucanBody));
            CrewDraw.SetSprite(Upper, art?.Piece(CrewPiece.ToucanBeakUpper));
            CrewDraw.SetSprite(Lower, art?.Piece(CrewPiece.ToucanBeakLower));
            CrewDraw.SetSprite(Wing, art?.Piece(CrewPiece.ToucanWing));
        }

        public void SetVisible(bool on)
        {
            if (Root.gameObject.activeSelf != on)
            {
                Root.gameObject.SetActive(on);
            }
        }

        // Las patas en at, a la escala scale (volteado con flipX = −1); el ala girada wing grados; el pico abierto open (0-1); la opacidad alpha.
        public void Pose(Vector2 at, float scale, float wing, float open, float alpha, float flipX = 1f)
        {
            CrewDraw.SetPos(Root, at);
            CrewDraw.SetScale(Root, new Vector3(scale * flipX, scale, 1f));
            CrewDraw.SetRotation(Wing.rectTransform, wing);
            CrewDraw.SetRotation(Upper.rectTransform, 16f * open);
            CrewDraw.SetRotation(Lower.rectTransform, -14f * open);
            CrewDraw.SetAlpha(Body, alpha);
            CrewDraw.SetAlpha(Upper, alpha);
            CrewDraw.SetAlpha(Lower, alpha);
            CrewDraw.SetAlpha(Wing, alpha);
        }

        private Image Part(string name, CrewPiece piece)
        {
            Image img = UiBuilder.Image(name, Root, null, Color.white);
            img.raycastTarget = false;
            img.enabled = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = ProceduralArt.CrewPivot(piece);
            RectInt c = ProceduralArt.CrewCell(piece);
            rt.sizeDelta = new Vector2(c.width / ProceduralArt.UiPxPerDp, c.height / ProceduralArt.UiPxPerDp);
            return img;
        }
    }
}
