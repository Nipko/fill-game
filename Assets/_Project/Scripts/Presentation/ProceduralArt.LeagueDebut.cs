#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas por código del estreno de la Liga del río (añadido de D-046 del 2026-11-06; D-059, añadido del 2026-10-31, punto 3:
    /// «la llegada de la Liga, anunciada y llamativa»), con la dirección «Selva brillante premium» (D-003: madera, vidrio y luz de
    /// juego, sin papel ni acuarela):
    /// <list type="bullet">
    /// <item><see cref="LeagueEmblem"/>: el emblema de la Liga, un medallón índigo (el color de la tarjeta «Liga» del muelle) con el
    /// canto de oro, una corona de hojas genéricas (sin especie reconocible: nada que revisar con el asesor) y la copa de oro con su
    /// estrella.</item>
    /// <item><see cref="LeagueRays"/>: los rayos de luz que giran despacio detrás del emblema (blancos, se tiñen), a 1 px por dp:
    /// son luz difusa y no piden más resolución.</item>
    /// </list>
    /// Perezosas, como las del muelle: nada se crea hasta que se piden (la primera vez que sale el estreno) y se liberan con
    /// <see cref="ReleaseLeagueDebutArt"/> al cerrarlo o con <see cref="Dispose"/>.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del emblema de la Liga (dp).</summary>
        public const float LeagueEmblemDp = 132f;

        /// <summary>Lado de los rayos del estreno (dp).</summary>
        public const float LeagueRaysDp = 360f;

        /// <summary>Número de rayos del estreno.</summary>
        public const int LeagueRayCount = 14;

        private static readonly Color EmblemGold = Hex(0xF2C24B);
        private static readonly Color EmblemGoldLight = Hex(0xFFE38A);
        private static readonly Color EmblemGoldDeep = Hex(0xB9822A);

        private Sprite? _leagueEmblem;
        private Sprite? _leagueRays;

        /// <summary>El emblema de la Liga: <see cref="LeagueEmblemDp"/> dp de lado, pivote al centro.</summary>
        public Sprite LeagueEmblem => _leagueEmblem != null ? _leagueEmblem : (_leagueEmblem = BuildLeagueEmblem());

        /// <summary>Los rayos del estreno: <see cref="LeagueRaysDp"/> dp de lado, blancos (se tiñen), pivote al centro.</summary>
        public Sprite LeagueRays => _leagueRays != null ? _leagueRays : (_leagueRays = BuildLeagueRays());

        /// <summary>El arte del estreno ya está creado (tests de memoria).</summary>
        public bool LeagueDebutArtLoaded => _leagueEmblem != null || _leagueRays != null;

        /// <summary>
        /// Libera el emblema y los rayos (el estreno sale una vez por jugador: no tiene sentido guardarlos). Volver a pedirlos los
        /// rehace.
        /// </summary>
        public void ReleaseLeagueDebutArt()
        {
            ReleaseOwned(ref _leagueEmblem);
            ReleaseOwned(ref _leagueRays);
        }

        private void ReleaseOwned(ref Sprite? sprite)
        {
            Sprite? s = sprite;
            sprite = null;
            if (s == null)
            {
                return;
            }

            Texture2D tex = s.texture;
            _owned.Remove(s);
            _owned.Remove(tex);
            DestroyOwned(s);
            if (tex != null)
            {
                DestroyOwned(tex);
            }
        }

        private static void DestroyOwned(Object o)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(o);
            }
            else
            {
                Object.DestroyImmediate(o); // tests EditMode
            }
        }

        private Sprite BuildLeagueEmblem()
        {
            const float k = UiPxPerDp;
            const float size = LeagueEmblemDp;
            int s = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(s, s);
            float c = size * 0.5f;

            // El medallón: sombra, canto de oro con bisel y el disco índigo con su luz de arriba (el vidrio de la guía).
            pc.FillCircle(c * k, (c - 2.5f) * k, 62f * k, new Color(0f, 0f, 0f, 0.28f));
            pc.FillCircle(c * k, c * k, 62f * k, EmblemGoldDeep);
            pc.FillCircle(c * k, (c + 1f) * k, 60.5f * k, EmblemGold);
            pc.StrokeArc(c * k, c * k, 59f * k, 2.2f * k, 25f, 155f, EmblemGoldLight);
            pc.FillCircle(c * k, c * k, 53f * k, EmblemGoldDeep);
            Color indigo = Palette.UiIndigo;
            pc.FillCircle(c * k, c * k, 51.5f * k, indigo);
            pc.FillEllipseSoft(c * k, (c + 22f) * k, 40f * k, 22f * k, 14f * k, new Color(0.55f, 0.62f, 1f, 0.30f));
            pc.FillEllipseSoft(c * k, (c - 30f) * k, 36f * k, 14f * k, 12f * k, new Color(0f, 0f, 0.1f, 0.25f));

            // La corona de hojas: dos ramas que suben por los lados desde abajo, con un par de hojas en V en cada nudo.
            for (int side = -1; side <= 1; side += 2)
            {
                pc.StrokeArc(c * k, c * k, 41f * k, 1.6f * k, side < 0 ? 136f : -64f, side < 0 ? 244f : 44f, Palette.LeafDark);
                for (int i = 0; i < 6; i++)
                {
                    float ang = (-90f + (side * (30f + (i * 19f)))) * Mathf.Deg2Rad;
                    float x = c + (Mathf.Cos(ang) * 41f);
                    float y = c + (Mathf.Sin(ang) * 41f);
                    float tx = -Mathf.Sin(ang) * side; // hacia arriba por la rama
                    float ty = Mathf.Cos(ang) * side;
                    float len = 7.2f - (i * 0.3f);
                    for (int v = -1; v <= 1; v += 2)
                    {
                        float rot = v * 38f * Mathf.Deg2Rad;
                        float lx = (tx * Mathf.Cos(rot)) - (ty * Mathf.Sin(rot));
                        float ly = (tx * Mathf.Sin(rot)) + (ty * Mathf.Cos(rot));
                        float ex = x + (lx * len);
                        float ey = y + (ly * len);
                        Color leaf = v < 0 ? Palette.LeafLight : Palette.LeafMid;
                        pc.FillCapsule((x + (lx * 1.5f)) * k, (y + (ly * 1.5f)) * k, ex * k, ey * k, 3.2f * k, Palette.LeafDark);
                        pc.FillCapsule((x + (lx * 1.5f)) * k, (y + (ly * 1.5f)) * k, ex * k, ey * k, 2.5f * k, leaf);
                        pc.FillCapsule((x + (lx * 2f)) * k, (y + (ly * 2f)) * k, (x + (lx * len * 0.8f)) * k, (y + (ly * len * 0.8f)) * k, 0.5f * k, Palette.LeafShine);
                    }
                }
            }

            // La copa de oro: pie, peana, tallo, asas y cuenco con su borde.
            Color shade = EmblemGoldDeep;
            pc.FillRoundedRect((c - 17f) * k, (c - 33f) * k, (c + 17f) * k, (c - 25f) * k, 2.5f * k, shade);
            pc.FillRoundedRect((c - 16f) * k, (c - 31.5f) * k, (c + 16f) * k, (c - 25.5f) * k, 2f * k, EmblemGold);
            pc.FillRoundedRect((c - 10f) * k, (c - 26f) * k, (c + 10f) * k, (c - 21f) * k, 2f * k, EmblemGold);
            pc.FillRoundedRect((c - 3.5f) * k, (c - 22f) * k, (c + 3.5f) * k, (c - 8f) * k, 1.5f * k, EmblemGold);
            pc.FillCircle(c * k, (c - 9f) * k, 5f * k, EmblemGold);
            pc.StrokeArc((c - 20f) * k, (c + 9f) * k, 8.5f * k, 3.4f * k, 80f, 280f, shade);
            pc.StrokeArc((c + 20f) * k, (c + 9f) * k, 8.5f * k, 3.4f * k, -100f, 100f, shade);
            pc.StrokeArc((c - 20f) * k, (c + 9f) * k, 8.5f * k, 2.2f * k, 85f, 275f, EmblemGold);
            pc.StrokeArc((c + 20f) * k, (c + 9f) * k, 8.5f * k, 2.2f * k, -95f, 95f, EmblemGold);
            pc.FillCircle(c * k, (c + 7f) * k, 21f * k, shade);
            pc.FillRoundedRect((c - 21f) * k, (c + 7f) * k, (c + 21f) * k, (c + 24f) * k, 2f * k, shade);
            pc.FillCircle(c * k, (c + 7.5f) * k, 19.5f * k, EmblemGold);
            pc.FillRoundedRect((c - 19.5f) * k, (c + 7.5f) * k, (c + 19.5f) * k, (c + 22.5f) * k, 1.5f * k, EmblemGold);
            pc.FillEllipseSoft((c + 9f) * k, (c + 6f) * k, 9f * k, 14f * k, 6f * k, new Color(0.62f, 0.38f, 0.08f, 0.35f));
            pc.FillCapsule((c - 12f) * k, (c + 2f) * k, (c - 13f) * k, (c + 18f) * k, 2.2f * k, new Color(1f, 0.98f, 0.85f, 0.65f));
            pc.FillRoundedRect((c - 21f) * k, (c + 21.5f) * k, (c + 21f) * k, (c + 25f) * k, 1.6f * k, EmblemGoldLight);

            // La estrella en el frente de la copa.
            FillStar(pc, c * k, (c + 9f) * k, 8.5f * k, 3.6f * k, Palette.Cream);

            // Destellos en el canto.
            FillStar(pc, (c - 38f) * k, (c + 40f) * k, 4.5f * k, 1.1f * k, new Color(1f, 1f, 1f, 0.9f), 4);
            FillStar(pc, (c + 44f) * k, (c - 30f) * k, 3.5f * k, 0.9f * k, new Color(1f, 1f, 1f, 0.8f), 4);
            return MakeSprite(pc, "league_emblem", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Una estrella de <paramref name="points"/> puntas con el centro en (cx, cy) px.
        private static void FillStar(PixelCanvas pc, float cx, float cy, float rOuter, float rInner, Color color, int points = 5)
        {
            float step = Mathf.PI / points;
            var center = new Vector2(cx, cy);
            for (int i = 0; i < points * 2; i++)
            {
                float a0 = (Mathf.PI * 0.5f) + (i * step);
                float a1 = a0 + step;
                float r0 = i % 2 == 0 ? rOuter : rInner;
                float r1 = i % 2 == 0 ? rInner : rOuter;
                var p0 = new Vector2(cx + (Mathf.Cos(a0) * r0), cy + (Mathf.Sin(a0) * r0));
                var p1 = new Vector2(cx + (Mathf.Cos(a1) * r1), cy + (Mathf.Sin(a1) * r1));
                pc.FillTriangle(center, p0, p1, color);
            }
        }

        private Sprite BuildLeagueRays()
        {
            const float k = 1f; // luz difusa: 1 px por dp
            int s = Mathf.RoundToInt(LeagueRaysDp * k);
            var pc = new PixelCanvas(s, s);
            float c = s * 0.5f;
            float wedge = Mathf.PI * 2f / LeagueRayCount;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy)) / c;
                    if (r >= 1f)
                    {
                        continue;
                    }

                    float ang = Mathf.Atan2(dy, dx);
                    float phase = Mathf.Repeat(ang / wedge, 1f); // 0..1 dentro de cada par rayo-hueco
                    float ray = Mathf.Clamp01(1f - (Mathf.Abs(phase - 0.5f) * 4f)); // rayo en la mitad, bordes suaves
                    ray = ray * ray * (3f - (2f * ray));
                    float fall = Mathf.Clamp01(1f - r);
                    float a = ray * fall * fall * 0.85f;
                    if (a > 0.003f)
                    {
                        pc.Set(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }

            return MakeSprite(pc, "league_rays", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }
    }
}
