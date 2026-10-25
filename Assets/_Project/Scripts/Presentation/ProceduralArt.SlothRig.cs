#nullable enable
using System.Diagnostics;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas del rig recortable del perezoso protagonista (<see cref="SlothRig"/>; tarea TI4; propuesta del personaje c.4-c.7 y
    /// paso 4 de (i); D-016): cabeza de frente con el antifaz, torso de frente (el vientre) y de espaldas (el lomo con el espéculo del macho), extremidad
    /// (9-slice vertical con las tres garras abajo), párpado (los dos ojos cerrados sobre el antifaz), hoja de embaúba (las 6 hojas
    /// de <c>sloth_cheer</c> y <c>sloth_cheer_big</c> son piezas del rig, no partículas: c.6) y la rama. Las medidas van en S, el
    /// alto del perezoso colgado en pantalla.
    /// </summary>
    public sealed class SlothArt
    {
        internal SlothArt()
        {
        }

        /// <summary>Cabeza (cuadrada, centro en el centro del cráneo; de frente).</summary>
        public Sprite Head { get; internal set; } = null!;

        /// <summary>Torso de frente, el vientre (óvalo vertical que llena el sprite): sin el espéculo, que está en el lomo.</summary>
        public Sprite Torso { get; internal set; } = null!;

        /// <summary>
        /// Torso de espaldas, el lomo con el velo de algas y el espéculo del macho (c.4 y c.5, punto 6): las poses que dan la
        /// espalda (<c>sloth_sleep</c>, en ovillo, y <c>sloth_cheer_big</c>, a media vuelta). Celda [P]: segunda fila, segunda
        /// columna.
        /// </summary>
        public Sprite TorsoBack { get; internal set; } = null!;

        /// <summary>Extremidad: cápsula vertical 9-slice con las tres garras abajo.</summary>
        public Sprite Limb { get; internal set; } = null!;

        /// <summary>Párpado: los dos ojos cerrados sobre su antifaz (parpadeo, <c>sloth_sleep</c> y <c>sloth_sunbathe</c>).</summary>
        public Sprite Eyelid { get; internal set; } = null!;

        /// <summary>Hoja de embaúba que cae al celebrar (pendiente del asesor cultural: ver <see cref="ProceduralArt.SlothBranchSlot"/>).</summary>
        public Sprite Leaf { get; internal set; } = null!;

        /// <summary>
        /// La rama (<c>arbol_embauba</c> o la provisional). Lo que entrega <see cref="ProceduralArt.SlothRigArt()"/> siempre la
        /// lleva: sin rama no hay perezoso (c.4: su rama entra con él, nunca cuelga de un botón, de una cuia ni de una tarjeta).
        /// </summary>
        public Sprite? Branch { get; internal set; }

        /// <summary>Lado de la cabeza (S).</summary>
        public float HeadS { get; internal set; } = 0.46f;

        /// <summary>Ancho de la extremidad (S).</summary>
        public float LimbWidthS { get; internal set; } = 0.12f;

        /// <summary>Lado de la hoja (S).</summary>
        public float LeafS { get; internal set; } = 0.2f;

        /// <summary>Ancho de la rama (S); el alto es la cuarta parte (la proporción de 1024 × 256 de la ranura).</summary>
        public float BranchWidthS { get; internal set; } = 1.5f;

        /// <summary>Centro del párpado respecto al centro de la cabeza (S).</summary>
        public Vector2 EyelidOffsetS { get; internal set; }

        /// <summary>Tamaño del párpado (S).</summary>
        public Vector2 EyelidSizeS { get; internal set; }

        /// <summary>Las piezas salen del PNG del artista (<see cref="ProceduralArt.SlothPartsSlot"/>).</summary>
        public bool Painted { get; internal set; }

        // Las mismas piezas con otra rama (dos por juego de piezas, la del PNG y la provisional; se guardan): cada compuerta
        // recibe su propio SlothArt y ninguna llamada cambia el que ya se entregó a otro consumidor.
        private SlothArt? _withBranch;
        private SlothArt? _withBranch2;

        /// <summary>
        /// Estas piezas con la rama <paramref name="branch"/>: este mismo objeto si ya la lleva; si no, una copia (se guardan
        /// las dos últimas para la próxima vez). Nunca cambia este objeto.
        /// </summary>
        internal SlothArt WithBranch(Sprite? branch)
        {
            if (Branch == branch)
            {
                return this;
            }

            if (_withBranch != null && _withBranch.Branch == branch)
            {
                return _withBranch;
            }

            if (_withBranch2 != null && _withBranch2.Branch == branch)
            {
                return _withBranch2;
            }

            _withBranch2 = _withBranch;
            _withBranch = new SlothArt
            {
                Head = Head,
                Torso = Torso,
                TorsoBack = TorsoBack,
                Limb = Limb,
                Eyelid = Eyelid,
                Leaf = Leaf,
                Branch = branch,
                HeadS = HeadS,
                LimbWidthS = LimbWidthS,
                LeafS = LeafS,
                BranchWidthS = BranchWidthS,
                EyelidOffsetS = EyelidOffsetS,
                EyelidSizeS = EyelidSizeS,
                Painted = Painted,
            };
            return _withBranch;
        }
    }

    /// <remarks>
    /// <para>Rig del perezoso (TI4). La ranura <see cref="SlothPartsSlot"/> (1024 × 1024, rejilla de 4 × 4 celdas; c.7) manda:
    /// con su PNG no se crea nada provisional. Celdas [P] de esta tarea, por fila desde arriba: cabeza, torso de frente,
    /// extremidad (un cuarto de ancho) y párpado (tres cuartos de ancho por un cuarto de alto, arriba); en la segunda fila, la
    /// hoja y el torso de espaldas (el lomo con el espéculo). La garganta parda va en lo alto del torso de frente, bajo la
    /// barbilla, y no en la cabeza: así no gira con ella. Sin PNG, el de código sale con
    /// <see cref="MapArtRule.ShowProvisional"/>, en todos los builds, release incluido (D-052; como los miquitos).</para>
    /// <para>La rama es otra ranura, <see cref="SlothBranchSlot"/> (1024 × 256, c.7), con su propio provisional, y la regla vale
    /// para el conjunto (c.4: su rama entra con él; nunca cuelga de un botón, de una cuia ni de una tarjeta): sin el arte
    /// por código (solo si un test lo apaga), el perezoso solo sale con los dos PNG, el de las piezas y el de la rama. Cada llamada a
    /// <see cref="SlothRigArt(Sprite, Sprite, bool)"/> entrega su propio <see cref="SlothArt"/> con su rama: las piezas se
    /// comparten, pero la rama de una compuerta nunca cambia la de otra.</para>
    /// <para><b>La embaúba (la hoja que cae al celebrar y la rama) está pendiente del asesor cultural</b> (propuesta de TiTino
    /// g.2, punto 1, y c.4: la ceniza de sus hojas va en el mambe, de uso ceremonial; encargo, recuadro de la sección 0,
    /// condición 1). Si la desaconseja, la alternativa prevista es una rama genérica sin especie reconocible (o la que proponga
    /// el asesor), como en la ventanita de casa de M1.5 (ProceduralArt.Home.cs): solo cambian <c>DrawSlothLeafCell</c>,
    /// <c>DrawEmbaubaBranch</c> y la ranura de la rama, no el rig. Nada de hojas quemadas ni ceniza.</para>
    /// <para>Perezoso: el atlas de 512 × 512 y la rama se crean la primera vez que se piden (<see cref="SlothRigArtBuilt"/>,
    /// <see cref="SlothBranchBuilt"/>), nunca al construir <see cref="ProceduralArt"/>; tras <see cref="Dispose"/> se vuelven
    /// a generar.</para>
    /// <para>Paleta orientativa [P] de c.4 (la del retrato de M1.5: <see cref="SlothFur"/>, <see cref="SlothAlgae"/>,
    /// <see cref="SlothFace"/>, <see cref="SlothMask"/>); el espéculo #E39A3B con su raya #2B2118 y las garras #CDBFA3 son de c.4;
    /// la hoja y la corteza de la embaúba son las de la ventanita de casa de M1.5 (<see cref="EmbaubaLeaf"/>, <see cref="EmbaubaBark"/>).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ranura de las piezas del rig (glosario §11: <c>fauna_&lt;id&gt;_partes</c> de <i>Bradypus variegatus</i>).</summary>
        public const string SlothPartsSlot = ArtSlots.FaunaPrefix + "bradypus_variegatus" + ArtSlots.FaunaPartsSuffix;

        /// <summary>
        /// Ranura de la rama de embaúba (glosario §11; c.7): no usa el patrón <c>rama_&lt;mundo&gt;</c>. La embaúba está pendiente
        /// del asesor cultural (g.2, punto 1); la alternativa prevista es una rama genérica.
        /// </summary>
        public const string SlothBranchSlot = "arbol_embauba";

        /// <summary>Lado del atlas provisional de las piezas (px): celdas de 128 px.</summary>
        public const int SlothAtlasPx = 512;

        // Celda del atlas provisional (px) y medidas en ella.
        private const int SlothCellPx = SlothAtlasPx / 4;
        private const float SlothHeadRadiusPx = 50f;
        private const float SlothInkPx = 3f; // ≈ 2 px en pantalla a los tamaños de victoria e inicio (c.4)
        private const float SlothLimbBorderPx = 28f;

        /// <summary>
        /// Diámetro del cráneo dibujado respecto al lado de la pieza de la cabeza (el radio de 50 px de la celda de 128; sin los
        /// mechones ni el contorno). El PNG del artista sigue la misma plantilla [P].
        /// </summary>
        public const float SlothSkullFraction = 2f * SlothHeadRadiusPx / SlothCellPx;

        /// <summary>
        /// Centro del espéculo del macho en la pieza del lomo (<see cref="SlothArt.TorsoBack"/>), en fracción de la pieza (0..1,
        /// y hacia arriba): en la mitad alta del lomo, sobre la columna.
        /// </summary>
        public static readonly Vector2 SlothSpeculumCenterUv = new Vector2(0.5f, 78f / SlothCellPx);

        /// <summary>Ancho y alto del espéculo en la pieza del lomo, en fracción de la pieza.</summary>
        public static readonly Vector2 SlothSpeculumSizeUv = new Vector2(18f / SlothCellPx, 34f / SlothCellPx);

        // Espéculo del macho (c.4) y garras de hueso claro (c.4).
        private static readonly Color SlothSpeculum = Palette.Hex(0xE39A3B);
        private static readonly Color SlothClaw = Palette.Hex(0xCDBFA3);

        private SlothArt? _slothRig;
        private SlothArt? _slothPainted;
        private Sprite? _slothPaintedFrom;
        private Sprite? _slothBranch;

        /// <summary>Si ya se creó el atlas provisional de las piezas del rig.</summary>
        public bool SlothRigArtBuilt => _slothRig != null && SlothOwned(_slothRig.Head);

        /// <summary>Si ya se creó la rama provisional.</summary>
        public bool SlothBranchBuilt => SlothOwned(_slothBranch);

        /// <summary>Tiempo de generación del último atlas provisional (ms; 0 si salió de la caché o del PNG).</summary>
        public double SlothArtMs { get; private set; }

        /// <summary>
        /// Las piezas del rig que se pintan, con su rama: las del PNG de <see cref="SlothPartsSlot"/> si existe; si no, las
        /// de código con <see cref="MapArtRule.ShowProvisional"/> (en todos los builds, D-052); y la rama de
        /// <see cref="SlothBranchSlot"/> o, con el arte por código, la suya. Si falta cualquiera de las dos (solo si un test apaga
        /// el arte por código), null: el perezoso no aparece.
        /// </summary>
        public SlothArt? SlothRigArt()
        {
            return SlothRigArt(ArtSlots.Find(SlothPartsSlot), ArtSlots.Find(SlothBranchSlot), MapArtRule.ShowProvisional);
        }

        /// <summary>
        /// La regla de <see cref="SlothRigArt()"/> con los PNG (<paramref name="partsPng"/>, <paramref name="branchPng"/>) y la
        /// compuerta (<paramref name="showProvisional"/>) explícitos. Vale para el conjunto: con la compuerta cerrada, sin
        /// cualquiera de los dos PNG, null (y no se crea nada). El PNG de las piezas manda y, con él, el atlas provisional no se
        /// crea; la rama sale de su PNG o, con la compuerta abierta, de la provisional. Nunca devuelve un perezoso sin rama.
        /// </summary>
        public SlothArt? SlothRigArt(Sprite? partsPng, Sprite? branchPng, bool showProvisional)
        {
            if (!showProvisional && (partsPng == null || branchPng == null))
            {
                return null; // release sin el arte final de las piezas o de la rama: ni piezas ni rama (07 §6.4; c.4)
            }

            SlothArt art;
            if (partsPng != null)
            {
                art = SlothPaintedArt(partsPng);
                SlothArtMs = 0;
            }
            else
            {
                art = SlothRigArtProcedural();
            }

            return art.WithBranch(branchPng != null ? branchPng : SlothBranchProcedural());
        }

        /// <summary>
        /// Las piezas provisionales por código, creadas la primera vez que se piden. No miran las ranuras ni la compuerta: para
        /// eso, <see cref="SlothRigArt()"/>. La rama no va aquí (<see cref="SlothBranchProcedural"/>).
        /// </summary>
        public SlothArt SlothRigArtProcedural()
        {
            if (SlothRigArtBuilt)
            {
                SlothArtMs = 0;
                return _slothRig!;
            }

            Stopwatch watch = Stopwatch.StartNew();
            var art = new SlothArt();
            var pc = new PixelCanvas(SlothAtlasPx, SlothAtlasPx);
            const int c = SlothCellPx;
            int top = SlothAtlasPx - c;
            DrawSlothHead(pc, 0, top);
            DrawSlothTorso(pc, c, top, false);
            DrawSlothTorso(pc, c, top - c, true);
            DrawSlothLimb(pc, 2 * c, top);
            DrawSlothEyelid(pc, 3 * c, SlothAtlasPx - (c / 4));
            DrawSlothLeafCell(pc, 0, top - c);
            Texture2D tex = pc.ToTexture("fauna_bradypus_variegatus_provisional");
            _owned.Add(tex);
            AssignSlothCells(art, tex);
            _slothRig = art;
            watch.Stop();
            SlothArtMs = watch.Elapsed.TotalMilliseconds;
            return art;
        }

        /// <summary>La rama de embaúba provisional (512 × 128 px, la proporción de la ranura), creada la primera vez que se pide.</summary>
        public Sprite SlothBranchProcedural()
        {
            if (SlothBranchBuilt && _slothBranch != null)
            {
                return _slothBranch;
            }

            var pc = new PixelCanvas(SlothAtlasPx, SlothAtlasPx / 4);
            DrawEmbaubaBranch(pc);
            Texture2D tex = pc.ToTexture("arbol_embauba_provisional");
            _owned.Add(tex);
            _slothBranch = SubSprite(tex, new Rect(0, 0, tex.width, tex.height), "branch", 0f);
            return _slothBranch;
        }

        // Piezas del PNG del artista (una vez por PNG): las mismas celdas que el provisional, escaladas a su lado.
        private SlothArt SlothPaintedArt(Sprite png)
        {
            if (_slothPainted != null && SlothOwned(_slothPainted.Head) && _slothPaintedFrom == png)
            {
                return _slothPainted;
            }

            var art = new SlothArt { Painted = true };
            AssignSlothCells(art, png.texture);
            _slothPainted = art;
            _slothPaintedFrom = png;
            return art;
        }

        // Celdas (desde arriba): cabeza, torso de frente, extremidad (¼ de ancho) y párpado (¾ × ¼, arriba); en la segunda fila,
        // la hoja (primera columna) y el torso de espaldas (segunda columna).
        private void AssignSlothCells(SlothArt art, Texture2D tex)
        {
            float cell = tex.width / 4f;
            float k = cell / SlothCellPx; // del atlas provisional al de la textura
            float top = tex.height - cell;
            art.Head = SubSprite(tex, new Rect(0f, top, cell, cell), "head", 0f);
            art.Torso = SubSprite(tex, new Rect(cell, top, cell, cell), "torso", 0f);
            art.Limb = SubSprite(tex, new Rect(2f * cell, top, cell / 4f, cell), "limb", SlothLimbBorderPx * k);
            art.Eyelid = SubSprite(tex, new Rect(3f * cell, tex.height - (cell / 4f), cell * 0.75f, cell / 4f), "eyelid", 0f);
            art.Leaf = SubSprite(tex, new Rect(0f, top - cell, cell, cell), "leaf", 0f);
            art.TorsoBack = SubSprite(tex, new Rect(cell, top - cell, cell, cell), "torso_back", 0f);

            // El párpado tapa los ojos: su centro está a la altura de los ojos (SlothEyeDyPx px de 128 sobre el de la cabeza).
            float s = art.HeadS / SlothCellPx;
            art.EyelidOffsetS = new Vector2(0f, SlothEyeDyPx) * s;
            art.EyelidSizeS = new Vector2(SlothCellPx * 0.75f, SlothCellPx / 4f) * s;
        }

        // Garganta parda en el torso de frente (px de la celda de 128, sobre el centro del torso): bajo la barbilla de la cabeza
        // colgada (Hang: la cabeza tapa el torso por encima de ≈ +28 px), de modo que asoma su mitad baja.
        private const float SlothThroatDyPx = 26f;

        // Ojos respecto al centro de la cabeza (px de la celda de 128): a ±14,7 y 0,5 por encima (la geometría del retrato de
        // M1.5 escalada a una cabeza de 50 px de radio). El párpado se centra a esa altura.
        private const float SlothEyeDxPx = 14.7f;
        private const float SlothEyeDyPx = 0.5f;

        // Cabeza de frente: borde de pelo en mechones, velo de algas en la coronilla, cara clara con el hocico corto, antifaz,
        // ojos pequeños con brillo, nariz ancha y la línea natural de la boca. Sin orejas redondas. La garganta parda no va aquí,
        // sino en lo alto del torso de frente (DrawSlothTorso): así no gira con la cabeza en sloth_look ni en sloth_present.
        private static void DrawSlothHead(PixelCanvas pc, int ox, int oy)
        {
            float cx = ox + 64f;
            float cy = oy + 64f;
            Color furLight = ColorMath.Lighten(SlothFur, 0.16f);
            Color furDark = ColorMath.Darken(SlothFur, 0.22f);
            for (int y = oy; y < oy + SlothCellPx; y++)
            {
                for (int x = ox; x < ox + SlothCellPx; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = px - cx;
                    float dy = py - cy;
                    float a = Mathf.Atan2(dy, dx);
                    float r = SlothHeadRadiusPx + (1.8f * Mathf.Sin(a * 11f)) + (1f * Mathf.Sin((a * 23f) + 1.3f));
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
                    if (d > SlothInkPx + 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage(d - SlothInkPx));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // Pelo con luz desde arriba a la izquierda, velo de algas en la coronilla y mechones de baja frecuencia.
                    Color col = Color.Lerp(furDark, furLight, Mathf.Clamp01(0.55f + (0.012f * dy) - (0.008f * dx)));
                    col = Color.Lerp(col, SlothAlgae, Mathf.Clamp01((dy - 18f) / 22f) * 0.3f);
                    float strand = ArtNoise.Value2(px * 0.3f, py * 0.12f, 17) - 0.5f;
                    col = strand > 0f ? Color.Lerp(col, furLight, strand * 0.35f) : Color.Lerp(col, furDark, -strand * 0.35f);
                    col.a = 1f;
                    pc.Blend(x, y, col, cov);

                    float face = Mathf.Min(EllipseDp(px, py, cx, cy - 2.6f, 33f, 27.6f), EllipseDp(px, py, cx, cy - 14.5f, 19.7f, 14.5f));
                    float faceCov = PixelCanvas.SoftCoverage(face, 3f);
                    if (faceCov > 0f)
                    {
                        Color f = Color.Lerp(ColorMath.Darken(SlothFace, 0.08f), SlothFace, Mathf.Clamp01(0.5f + (0.02f * (dy + 10f))));
                        pc.Blend(x, y, f, faceCov);
                    }

                    float mask = SlothMaskDistance(px, py, cx, cy);
                    float maskCov = PixelCanvas.Coverage(mask);
                    if (maskCov > 0f)
                    {
                        pc.Blend(x, y, SlothMask, maskCov);
                    }
                }
            }

            // Ojos negros pequeños con su brillo.
            for (int side = -1; side <= 1; side += 2)
            {
                float ex = cx + (side * SlothEyeDxPx);
                float ey = cy + SlothEyeDyPx;
                pc.FillCircle(ex, ey, 4.5f, SlothInk);
                pc.FillCircle(ex - 1.3f, ey + 1.6f, 1.6f, new Color(1f, 1f, 1f, 0.95f));
            }

            // Nariz ancha y oscura con su brillo, y la línea natural de la boca (sin labios ni dientes).
            pc.FillEllipseSoft(cx, cy - 11.6f, 7.1f, 4.7f, 1.2f, SlothInk);
            pc.FillEllipseSoft(cx - 2.1f, cy - 10f, 2.1f, 1.2f, 1f, new Color(1f, 1f, 1f, 0.45f));
            pc.StrokeArc(cx, cy - 14.2f, 8.4f, 1.8f, 215f, 325f, SlothOutline);
        }

        // Antifaz: dos manchas alargadas que bajan hacia fuera desde los ojos (las del retrato de M1.5).
        private static float SlothMaskDistance(float px, float py, float cx, float cy)
        {
            return Mathf.Min(EllipseDp(px, py, cx - 17.9f, cy - 1.1f, 14.2f, 6.6f, 0.38f), EllipseDp(px, py, cx + 17.9f, cy - 1.1f, 14.2f, 6.6f, -0.38f));
        }

        // Párpado: el antifaz de los dos ojos con los ojos cerrados (arcos claros), centrado a la altura de los ojos.
        private static void DrawSlothEyelid(PixelCanvas pc, int ox, int oy)
        {
            float cx = ox + (SlothCellPx * 0.375f);
            float cy = oy + (SlothCellPx * 0.125f) - SlothEyeDyPx; // el centro de la cabeza en la celda del párpado
            int w = (int)(SlothCellPx * 0.75f);
            int h = SlothCellPx / 4;
            for (int y = oy; y < oy + h; y++)
            {
                for (int x = ox; x < ox + w; x++)
                {
                    float cov = PixelCanvas.Coverage(SlothMaskDistance(x + 0.5f, y + 0.5f, cx, cy));
                    if (cov > 0f)
                    {
                        pc.Blend(x, y, SlothMask, cov);
                    }
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                pc.StrokeArc(cx + (side * SlothEyeDxPx), cy + SlothEyeDyPx + 2.4f, 4.7f, 2f, 200f, 340f, SlothLid);
            }
        }

        // Torso: óvalo vertical de pelo largo y áspero con mechones en el borde y el velo de algas en los hombros (20-30 %). De
        // frente, el vientre algo más claro y la garganta parda arriba, justo bajo la barbilla (la cabeza tapa su mitad alta);
        // de espaldas (back), más algas en el lomo y el espéculo del macho (parche naranja con su raya negra) en el centro de la
        // espalda: de frente no se ve (con él en el pecho se leía como un collar, c.4).
        private static void DrawSlothTorso(PixelCanvas pc, int ox, int oy, bool back)
        {
            float cx = ox + 64f;
            float cy = oy + 64f;
            Color furLight = ColorMath.Lighten(SlothFur, 0.14f);
            Color furDark = ColorMath.Darken(SlothFur, 0.24f);
            float specX = ox + (SlothSpeculumCenterUv.x * SlothCellPx);
            float specY = oy + (SlothSpeculumCenterUv.y * SlothCellPx);
            for (int y = oy; y < oy + SlothCellPx; y++)
            {
                for (int x = ox; x < ox + SlothCellPx; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = px - cx;
                    float dy = py - cy;
                    float a = Mathf.Atan2(dy / 56f, dx / 49f);
                    float d = EllipseDp(px, py, cx, cy, 49f, 56f) - (1.5f * Mathf.Sin(a * 9f)) - 1.5f;
                    if (d > SlothInkPx + 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage(d - SlothInkPx));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color col = Color.Lerp(furDark, furLight, Mathf.Clamp01(0.5f + (0.01f * dy) - (0.007f * dx)));
                    col = Color.Lerp(col, SlothAlgae, Mathf.Clamp01((dy - (back ? -10f : 8f)) / 30f) * 0.3f);
                    float strand = ArtNoise.Value2(px * 0.28f, py * 0.1f, 23) - 0.5f;
                    col = strand > 0f ? Color.Lerp(col, furLight, strand * 0.4f) : Color.Lerp(col, furDark, -strand * 0.4f);
                    if (!back)
                    {
                        col = Color.Lerp(col, ColorMath.Lighten(SlothFur, 0.1f), PixelCanvas.SoftCoverage(EllipseDp(px, py, cx + 4f, cy - 14f, 26f, 32f), 10f) * 0.5f);
                        col = Color.Lerp(col, SlothThroat, PixelCanvas.SoftCoverage(EllipseDp(px, py, cx, cy + SlothThroatDyPx, 16f, 9f), 4f));
                    }

                    col.a = 1f;
                    pc.Blend(x, y, col, cov);
                    if (!back)
                    {
                        continue;
                    }

                    float spec = EllipseDp(px, py, specX, specY, SlothSpeculumSizeUv.x * SlothCellPx * 0.5f, SlothSpeculumSizeUv.y * SlothCellPx * 0.5f);
                    float specCov = PixelCanvas.Coverage(spec);
                    if (specCov > 0f)
                    {
                        pc.Blend(x, y, SlothSpeculum, specCov);
                    }
                }
            }

            // La raya negra del espéculo, a lo largo de la columna.
            if (back)
            {
                pc.FillCapsule(specX, specY - 11f, specX, specY + 11f, 1.8f, SlothInk);
            }
        }

        // Extremidad: cápsula de pelo (de arriba, el hombro o la cadera, hacia abajo), la mano o el pie más oscuros y las tres
        // garras largas y curvas, redondeadas y claras (c.5, punto 4).
        private static void DrawSlothLimb(PixelCanvas pc, int ox, int oy)
        {
            float cx = ox + 16f;
            Color fur = ColorMath.Darken(SlothFur, 0.06f);
            pc.FillCapsule(cx, oy + 113f, cx, oy + 24f, 14.5f, SlothOutline);
            pc.FillCapsule(cx, oy + 113f, cx, oy + 24f, 11.5f, fur);
            pc.FillCapsule(cx - 4f, oy + 108f, cx - 4f, oy + 34f, 2.6f, ColorMath.WithAlpha(SlothAlgae, 0.5f)); // velo de algas
            pc.FillCircle(cx, oy + 22f, 9.5f, ColorMath.Darken(SlothFur, 0.3f));
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    float x0 = cx + (i * 5.5f);
                    float x1 = cx + (i * 6.5f) + 1.5f;
                    float r = pass == 0 ? 3.4f : 2.2f;
                    Color col = pass == 0 ? SlothOutline : SlothClaw;
                    pc.FillCapsule(x0, oy + 18f, x1, oy + 9f, r, col);
                    pc.FillCapsule(x1, oy + 9f, x1 + 2.5f, oy + 5f, r * 0.85f, col);
                }
            }
        }

        // Hoja de embaúba (Cecropia): lámina palmeada de 7 lóbulos con los nervios claros del envés y su peciolo. Pendiente del
        // asesor cultural (propuesta de TiTino g.2, punto 1, y c.4); si la desaconseja, la alternativa prevista es una hoja
        // genérica sin especie reconocible, como la rama (ProceduralArt.Home.cs, M1.5). Nunca quemada ni con ceniza.
        private static void DrawSlothLeafCell(PixelCanvas pc, int ox, int oy)
        {
            float cx = ox + 64f;
            float cy = oy + 58f;
            Color ink = SlothOutline;
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? 2.5f : 0f;
                Color col = pass == 0 ? ink : EmbaubaLeaf;
                pc.FillCapsule(cx, cy, cx, oy + 10f, 2.5f + grow, col);
                pc.FillCircle(cx, cy, 15f + grow, col);
                for (int i = 0; i < 7; i++)
                {
                    float a = (90f + ((i - 3) * 38f)) * Mathf.Deg2Rad;
                    float lx = cx + (Mathf.Cos(a) * 28f);
                    float ly = cy + (Mathf.Sin(a) * 28f);
                    pc.FillEllipseSoft(lx, ly, 11f + grow, 11f + grow, 1f, col);
                    pc.FillCapsule(cx + (Mathf.Cos(a) * 10f), cy + (Mathf.Sin(a) * 10f), cx + (Mathf.Cos(a) * 40f), cy + (Mathf.Sin(a) * 40f), 9f + grow, col);
                }
            }

            pc.FillEllipseSoft(cx - 10f, cy + 14f, 16f, 10f, 8f, ColorMath.WithAlpha(ColorMath.Lighten(EmbaubaLeaf, 0.3f), 0.4f)); // luz
            for (int i = 0; i < 7; i++)
            {
                float a = (90f + ((i - 3) * 38f)) * Mathf.Deg2Rad;
                pc.FillCapsule(cx, cy, cx + (Mathf.Cos(a) * 42f), cy + (Mathf.Sin(a) * 42f), 1.1f, EmbaubaLeafUnder);
            }
        }

        // Rama de embaúba: corteza lisa y pálida con los anillos de las cicatrices de las hojas, un poco arqueada, con su luz
        // arriba y el contorno de tinta; los extremos redondeados. Pendiente del asesor cultural (propuesta de TiTino g.2, punto
        // 1, y c.4); si la desaconseja, la alternativa prevista es una rama genérica sin especie reconocible (o la que proponga
        // el asesor), como en ProceduralArt.Home.cs (M1.5).
        private static void DrawEmbaubaBranch(PixelCanvas pc)
        {
            int w = pc.Width;
            const float r = 18f;
            const int steps = 32;
            Color light = ColorMath.Lighten(EmbaubaBark, 0.3f);
            Color ring = EmbaubaRing;
            for (int pass = 0; pass < 3; pass++)
            {
                float px = 14f;
                float py = BranchY(px, w);
                for (int i = 1; i <= steps; i++)
                {
                    float qx = 14f + ((w - 28f) * i / steps);
                    float qy = BranchY(qx, w);
                    if (pass == 0)
                    {
                        pc.FillCapsule(px, py, qx, qy, r + SlothInkPx, SlothOutline);
                    }
                    else if (pass == 1)
                    {
                        pc.FillCapsule(px, py, qx, qy, r, EmbaubaBark);
                    }
                    else
                    {
                        pc.FillCapsule(px, py + 8f, qx, qy + 8f, 4f, ColorMath.WithAlpha(light, 0.6f));
                    }

                    px = qx;
                    py = qy;
                }
            }

            for (float x = 62f; x < w - 40f; x += 72f)
            {
                float y = BranchY(x, w);
                pc.FillCapsule(x, y - (r - 3f), x + 2f, y + (r - 3f), 1.4f, ring);
            }
        }

        // La pieza sigue viva: la creó este ProceduralArt y no se liberó (tras Dispose, la lista está vacía aunque Unity aún no
        // haya destruido el objeto, que en juego se destruye al final del fotograma).
        private bool SlothOwned(Object? piece)
        {
            return piece != null && _owned.Contains(piece);
        }

        private static float BranchY(float x, int width)
        {
            return 64f + (6f * Mathf.Sin(x / width * Mathf.PI));
        }
    }
}
