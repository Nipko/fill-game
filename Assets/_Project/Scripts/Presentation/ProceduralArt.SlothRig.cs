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

        /// <summary>Extremidad: pieza vertical 9-slice que se afina del hombro a la muñeca, con la mano y las tres garras en gancho abajo.</summary>
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
    /// <para>Perezoso: el atlas de 1024 × 1024 (con mipmaps) y la rama se crean la primera vez que se piden (<see cref="SlothRigArtBuilt"/>,
    /// <see cref="SlothBranchBuilt"/>), nunca al construir <see cref="ProceduralArt"/>; tras <see cref="Dispose"/> se vuelven
    /// a generar.</para>
    /// <para>Paleta orientativa [P] de c.4 (la del retrato de M1.5: <see cref="SlothFur"/>, <see cref="SlothAlgae"/>,
    /// <see cref="SlothFace"/>, <see cref="SlothMask"/>); el espéculo #E39A3B con su raya #2B2118 y las garras #CDBFA3 son de c.4;
    /// la hoja y la corteza de la embaúba son las de la ventanita de casa de M1.5 (<see cref="EmbaubaLeaf"/>, <see cref="EmbaubaBark"/>).</para>
    /// <para>Dibujo pulido a petición del dueño del 2026-10-25 (D-052: el arte por código es el definitivo): pelaje en mechones
    /// con volumen, luz de juego y luz de borde (D-003), el matiz de algas a parches, extremidades que se afinan con garras
    /// marfil en gancho, la máscara con las bandas oscuras de los ojos y la sonrisa; mismas celdas, medidas, pivotes y poses.
    /// La cabeza (<c>DrawSlothHeadAt</c>) es la misma en el rig y en el retrato.</para>
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

        /// <summary>
        /// Lado del atlas provisional de las piezas (px): celdas de 256 px, la plantilla del PNG del artista (1024 × 1024), con
        /// mipmaps para que se vea nítido de la ventanita (≈ 48 dp) al arranque (≈ 160 dp). El dibujo se hace en unidades de la
        /// celda de 128 (<see cref="SlothCellPx"/>) y se pinta a <see cref="SlothK"/> px por unidad.
        /// </summary>
        public const int SlothAtlasPx = 1024;

        // Unidades de diseño por celda (todas las medidas del dibujo, de las celdas y de las constantes públicas van en ellas)
        // y px del atlas por unidad.
        private const int SlothCellPx = 128;
        private const float SlothK = SlothAtlasPx / 4f / SlothCellPx;
        private const float SlothHeadRadiusPx = 50f;
        private const float SlothInkPx = 2.6f; // ≈ 2 px en pantalla a los tamaños de victoria e inicio (c.4)
        private const float SlothLimbBorderPx = 28f;

        /// <summary>
        /// Diámetro del cráneo dibujado respecto al lado de la pieza de la cabeza (el radio de 50 unidades de la celda de 128;
        /// sin los mechones ni el contorno). El PNG del artista sigue la misma plantilla [P].
        /// </summary>
        public const float SlothSkullFraction = 2f * SlothHeadRadiusPx / SlothCellPx;

        /// <summary>
        /// Centro del espéculo del macho en la pieza del lomo (<see cref="SlothArt.TorsoBack"/>), en fracción de la pieza (0..1,
        /// y hacia arriba): en la mitad alta del lomo, sobre la columna.
        /// </summary>
        public static readonly Vector2 SlothSpeculumCenterUv = new Vector2(0.5f, 78f / SlothCellPx);

        /// <summary>Ancho y alto del espéculo en la pieza del lomo, en fracción de la pieza.</summary>
        public static readonly Vector2 SlothSpeculumSizeUv = new Vector2(18f / SlothCellPx, 34f / SlothCellPx);

        // Espéculo del macho (c.4) y garras de hueso claro (c.4), con su base más oscura y su punta casi marfil.
        private static readonly Color SlothSpeculum = Palette.Hex(0xE39A3B);
        private static readonly Color SlothClaw = Palette.Hex(0xCDBFA3);
        private static readonly Color SlothClawBase = Palette.Hex(0x8C7A5E);
        private static readonly Color SlothClawTip = Palette.Hex(0xF1E8D4);
        private static readonly Color SlothClawInk = Palette.Hex(0x3A2C20);

        // Luz de juego (D-003; dirección visual v2): desde arriba a la izquierda y algo de frente; la luz de borde cálida y el
        // rebote fresco de la selva en el lado de la sombra.
        private const float SlothLightX = -0.5f;
        private const float SlothLightY = 0.62f;
        private const float SlothLightZ = 0.6f;
        private static readonly Color SlothRimLight = Palette.Hex(0xFFF0CF);
        private static readonly Color SlothBounce = Palette.Hex(0x7FA597);
        private static readonly Color SlothEye = Palette.Hex(0x160E09);
        private static readonly Color SlothEyeWarm = Palette.Hex(0x6A4730);
        private static readonly Color SlothBlush = Palette.Hex(0xD98C76);
        private static readonly Color SlothMouth = Palette.Hex(0x3B2A1E);

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
            const int c = SlothAtlasPx / 4;
            const float k = SlothK;
            int top = SlothAtlasPx - c;
            DrawSlothHeadAt(pc, c * 0.5f, top + (c * 0.5f), k, false, 0, top, c, SlothAtlasPx);
            DrawSlothTorso(pc, c, top, k, false);
            DrawSlothTorso(pc, c, top - c, k, true);
            DrawSlothLimb(pc, 2 * c, top, k);
            DrawSlothEyelid(pc, 3 * c, SlothAtlasPx - (c / 4), k);
            DrawSlothLeafCell(pc, 0, top - c, k);
            Texture2D tex = pc.ToTexture("fauna_bradypus_variegatus_provisional", mipmaps: true);
            _owned.Add(tex);
            AssignSlothCells(art, tex);
            _slothRig = art;
            watch.Stop();
            SlothArtMs = watch.Elapsed.TotalMilliseconds;
            return art;
        }

        /// <summary>
        /// La rama de embaúba provisional (1024 × 256 px, la proporción y el tamaño de la ranura, con mipmaps), creada la primera
        /// vez que se pide.
        /// </summary>
        public Sprite SlothBranchProcedural()
        {
            if (SlothBranchBuilt && _slothBranch != null)
            {
                return _slothBranch;
            }

            var pc = new PixelCanvas(SlothAtlasPx, SlothAtlasPx / 4);
            DrawEmbaubaBranch(pc, SlothK);
            Texture2D tex = pc.ToTexture("arbol_embauba_provisional", mipmaps: true);
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
            float k = cell / SlothCellPx; // de las unidades de la celda de 128 a los px de la textura
            float top = tex.height - cell;
            art.Head = SubSprite(tex, new Rect(0f, top, cell, cell), "head", 0f);
            art.Torso = SubSprite(tex, new Rect(cell, top, cell, cell), "torso", 0f);
            art.Limb = SubSprite(tex, new Rect(2f * cell, top, cell / 4f, cell), "limb", SlothLimbBorderPx * k);
            art.Eyelid = SubSprite(tex, new Rect(3f * cell, tex.height - (cell / 4f), cell * 0.75f, cell / 4f), "eyelid", 0f);
            art.Leaf = SubSprite(tex, new Rect(0f, top - cell, cell, cell), "leaf", 0f);
            art.TorsoBack = SubSprite(tex, new Rect(cell, top - cell, cell, cell), "torso_back", 0f);

            // El párpado tapa los ojos: su centro está a la altura de los ojos (SlothEyeDyPx unidades sobre el de la cabeza).
            float s = art.HeadS / SlothCellPx;
            art.EyelidOffsetS = new Vector2(0f, SlothEyeDyPx) * s;
            art.EyelidSizeS = new Vector2(SlothCellPx * 0.75f, SlothCellPx / 4f) * s;
        }

        // Ojos respecto al centro de la cabeza (unidades de la celda de 128): a ±14,7 y 0,5 por encima. El párpado se centra a
        // esa altura.
        private const float SlothEyeDxPx = 14.7f;
        private const float SlothEyeDyPx = 0.5f;

        // Los tres tonos de un pelaje o de una piel (sombra, medio y luz), calculados una vez por pieza.
        private struct SlothTones
        {
            public Color Shadow;
            public Color Mid;
            public Color Light;
        }

        private static SlothTones Tones(Color c, float dark, float light)
        {
            return new SlothTones { Shadow = ColorMath.Darken(c, dark), Mid = c, Light = ColorMath.Lighten(c, light) };
        }

        private static Color Shade(in SlothTones t, float diffuse)
        {
            return diffuse < 0.55f ? Color.Lerp(t.Shadow, t.Mid, diffuse / 0.55f) : Color.Lerp(t.Mid, t.Light, (diffuse - 0.55f) / 0.45f);
        }

        // Luz difusa de una normal aparente (nx, ny en −1..1): la luz de juego de arriba a la izquierda.
        private static float SlothDiffuse(float nx, float ny, out float nz)
        {
            float nn = Mathf.Clamp01((nx * nx) + (ny * ny));
            nz = Mathf.Sqrt(1f - nn);
            return Mathf.Clamp01((nx * SlothLightX) + (ny * SlothLightY) + (nz * SlothLightZ) + 0.08f);
        }

        // El pelaje con volumen: sombra propia y luz por la normal aparente, el matiz de las algas (algae, 0..1) mezclado con su
        // propia luz, los mechones (strand, −1..1), la luz de borde cálida en el canto que mira a la luz y el rebote fresco en el
        // de la sombra.
        private static Color SlothFurColor(in SlothTones fur, in SlothTones algae, float nx, float ny, float strand, float algaeAmount)
        {
            float diffuse = SlothDiffuse(nx, ny, out float nz);
            Color col = Shade(fur, diffuse);
            if (algaeAmount > 0f)
            {
                col = Color.Lerp(col, Shade(algae, diffuse), Mathf.Clamp01(algaeAmount));
            }

            float m = 1f + (0.15f * strand);
            col.r *= m;
            col.g *= m;
            col.b *= m;
            float edge = 1f - nz;
            float facing = ((nx * SlothLightX) + (ny * SlothLightY)) * 1.25f;
            if (facing > 0f)
            {
                col = Color.Lerp(col, SlothRimLight, edge * edge * edge * Mathf.Clamp01(facing) * 0.6f);
            }
            else
            {
                col = Color.Lerp(col, SlothBounce, edge * edge * edge * Mathf.Clamp01(-facing) * 0.22f);
            }

            col.a = 1f;
            return col;
        }

        // Mechones: vetas suaves en la dirección del pelo (along, de la raíz a la punta) y más estrechas a lo ancho (across), en
        // unidades, ondeadas con un ruido lento para que no sean rectas: matas que se leen a 48 dp y pelos finos para los
        // tamaños grandes, con poco contraste (pelo, no veta de madera ni tejas). lane y len son el ancho y el largo de una mata.
        // −1..1.
        private static float SlothStrand(float along, float across, int seed, float lane = 7f, float len = 15f)
        {
            float warp = (ArtNoise.Value2(across * 0.06f, along * 0.05f, seed + 3) - 0.5f) * 1.1f * lane;
            float ax = (across + warp) / lane;
            float ay = along / len;
            float n1 = ArtNoise.Value2(ax * 1.5f, ay * 0.85f, seed) - 0.5f;
            float n2 = ArtNoise.Value2(ax * 4f, ay * 2.4f, seed + 11) - 0.5f;
            return Mathf.Clamp((n1 * 1.7f) + (n2 * 0.9f), -1f, 1f);
        }

        // Mechones que salen del centro de la cabeza (r, a en radianes): sin costura donde el ángulo da la vuelta (±π), que se
        // funde con la vuelta anterior.
        private static float SlothRadialStrand(float r, float a, int seed)
        {
            float across = Mathf.Max(r, 24f);
            float s1 = SlothStrand(r, a * across, seed, 8f, 13f);
            float w = 0.5f * SlothSmooth(Mathf.PI - 0.6f, Mathf.PI, Mathf.Abs(a));
            if (w <= 0f)
            {
                return s1;
            }

            float a2 = a < 0f ? a + (2f * Mathf.PI) : a - (2f * Mathf.PI);
            return Mathf.Lerp(s1, SlothStrand(r, a2 * across, seed, 8f, 13f), w);
        }

        // Mechones del borde: n puntas por vuelta (ángulo a), de alto amp (cada una con su propio alto), puntiagudas y con el
        // valle redondo: desgreñado pero limpio. valley (0..1) marca el surco entre dos mechones.
        private static float SlothTuft(float a, int n, float amp, float phase, int seed, out float valley)
        {
            float u = ((a * n) / (2f * Mathf.PI)) + phase;
            float i = Mathf.Floor(u + 0.5f);
            float f = u - i; // −0,5..0,5
            float t = 1f - Mathf.Abs(2f * f);
            int idx = (((int)i % n) + n) % n;
            float jitter = 0.55f + (0.9f * ArtNoise.Hash(idx, 3, seed));
            float af = Mathf.Abs(f);
            valley = Mathf.Clamp01((af - 0.32f) / 0.18f);
            float shape = t * t * (3f - (2f * t)); // punta y valle redondos: matas, no dientes de sierra
            return amp * jitter * shape * Mathf.Sqrt(shape);
        }

        private static float SlothSmoothMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + (0.5f * (b - a) / k));
            return Mathf.Lerp(b, a, h) - (k * h * (1f - h));
        }

        private static float SlothSmooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - (2f * t));
        }

        // Distancia a un trazo que se afina (de a con radio ra a b con radio rb).
        private static float SlothTaper(float px, float py, float ax, float ay, float ra, float bx, float by, float rb)
        {
            float vx = bx - ax;
            float vy = by - ay;
            float t = Mathf.Clamp01((((px - ax) * vx) + ((py - ay) * vy)) / Mathf.Max(1e-5f, (vx * vx) + (vy * vy)));
            float dx = px - ax - (vx * t);
            float dy = py - ay - (vy * t);
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - Mathf.Lerp(ra, rb, t);
        }

        // Manchas de algas (0..1, baja frecuencia): el matiz verdoso va a parches, nunca en raya.
        private static float SlothAlgaePatch(float ux, float uy, int seed)
        {
            return Mathf.Clamp01(0.35f + (1.3f * (ArtNoise.Fbm2(ux * 0.055f, uy * 0.055f, seed) - 0.5f)));
        }

        // Bandas oscuras de los ojos del perezoso de tres dedos: nacen junto al hocico, envuelven el ojo y se afinan hacia la
        // sien, un poco hacia abajo (unidades respecto al centro de la cabeza).
        private static float SlothBandDistance(float ux, float uy)
        {
            float s = ux < 0f ? -1f : 1f;
            float x = ux * s;
            float a = SlothTaper(x, uy, 8.6f, 2f, 5.4f, SlothEyeDxPx, SlothEyeDyPx + 0.3f, 8.4f);
            float b = SlothTaper(x, uy, SlothEyeDxPx, SlothEyeDyPx + 0.3f, 8.4f, 32.5f, -7.5f, 3.6f);
            return SlothSmoothMin(a, b, 2f);
        }

        // Color de las bandas: más claras arriba, con su veta de pelo hacia la sien. Lo usan la cabeza y el párpado, idéntico.
        private static Color SlothBandColor(float ux, float uy)
        {
            float light = Mathf.Clamp01(0.45f + (0.06f * (uy - SlothEyeDyPx)) - (0.012f * Mathf.Abs(ux)));
            Color col = Color.Lerp(ColorMath.Darken(SlothMask, 0.22f), ColorMath.Lighten(SlothMask, 0.1f), light);
            float strand = SlothStrand(Mathf.Abs(ux), uy, 61, 3f, 8f);
            float m = 1f + (0.1f * strand);
            col.r *= m;
            col.g *= m;
            col.b *= m;
            col.a = 1f;
            return col;
        }

        // Pinta la cabeza de frente con centro (cx, cy) px y k px por unidad (la celda de 128), recortada a [x0, x1) × [y0, y1):
        // borde de pelo en mechones con su copete, volumen con luz de juego, luz de borde y matiz de algas en la coronilla; la
        // máscara clara con el pico de pelo de la frente y el flequillo; las bandas oscuras de los ojos; ojos con brillo
        // (abiertos) o cerrados en arco; el hocico con su luz, la nariz ancha y brillante, y la sonrisa amable. La usan el rig
        // (la pieza de la cabeza, ojos abiertos) y el retrato (abiertos o cerrados). Sin orejas redondas, labios ni dientes. La
        // garganta parda no va aquí, sino en lo alto del torso de frente: así no gira con la cabeza.
        private static void DrawSlothHeadAt(PixelCanvas pc, float cx, float cy, float k, bool eyesClosed, int x0, int y0, int x1, int y1)
        {
            SlothTones fur = Tones(SlothFur, 0.36f, 0.2f);
            SlothTones algae = Tones(Color.Lerp(SlothFur, SlothAlgae, 0.75f), 0.34f, 0.16f);
            SlothTones face = Tones(SlothFace, 0.2f, 0.05f);
            Color faceLight = ColorMath.Lighten(SlothFace, 0.07f);
            float reach = (SlothHeadRadiusPx + 10f) * k;
            int ax = Mathf.Max(x0, Mathf.FloorToInt(cx - reach));
            int bx = Mathf.Min(x1, Mathf.CeilToInt(cx + reach));
            int ay = Mathf.Max(y0, Mathf.FloorToInt(cy - reach));
            int by = Mathf.Min(y1, Mathf.CeilToInt(cy + reach));
            float aa = 1f / k; // un píxel en unidades
            for (int y = ay; y < by; y++)
            {
                for (int x = ax; x < bx; x++)
                {
                    float ux = (x + 0.5f - cx) / k;
                    float uy = (y + 0.5f - cy) / k;
                    float r = Mathf.Sqrt((ux * ux) + (uy * uy));
                    if (r > SlothHeadRadiusPx + 9f)
                    {
                        continue;
                    }

                    float a = Mathf.Atan2(uy, ux);
                    float amp = 1.6f + (2.3f * Mathf.Clamp01(Mathf.Sin(a) + 0.25f)); // copete arriba, mejillas más cortas
                    float rr = 47.4f + SlothTuft(a, 13, amp, 0.35f, 31, out float valley);
                    float d = r - rr;
                    if (d > SlothInkPx + aa)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage((d - SlothInkPx) * k));
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float strand = SlothRadialStrand(r, a, 17);
                    float algaeAmount = 0.34f * SlothSmooth(6f, 46f, uy) * SlothAlgaePatch(ux, uy, 5);
                    Color col = SlothFurColor(fur, algae, ux / 54f, uy / 54f, strand, algaeAmount);
                    float groove = valley * Mathf.Clamp01(1f - ((rr - r) / 9f)) * 0.3f;
                    col.r *= 1f - groove;
                    col.g *= 1f - groove;
                    col.b *= 1f - groove;
                    pc.Blend(x, y, col, cov);

                    // Máscara clara: la cara con el hocico corto, el pico de pelo de la frente y el flequillo dentado arriba.
                    float faceD = SlothSmoothMin(EllipseDp(ux, uy, 0f, -3f, 32.5f, 26.5f), EllipseDp(ux, uy, 0f, -14f, 20f, 14.5f), 6f);
                    faceD = Mathf.Max(faceD, -EllipseDp(ux, uy, 0f, 26f, 6.5f, 9.5f));
                    float fa = Mathf.Atan2(uy + 3f, ux);
                    faceD += ((0.9f * Mathf.Sin((fa * 17f) + 0.6f)) + (0.55f * Mathf.Sin((fa * 31f) + 2.1f))) * Mathf.Clamp01((uy + 16f) / 22f);
                    float faceCov = PixelCanvas.SoftCoverage(faceD * k, 2.2f * k);
                    if (faceCov > 0f)
                    {
                        float diffuse = SlothDiffuse(ux / 36f, (uy + 5f) / 34f, out _);
                        Color f = Shade(face, diffuse);
                        f = Color.Lerp(f, faceLight, PixelCanvas.SoftCoverage(EllipseDp(ux, uy, -2.5f, -7.5f, 10f, 6f) * k, 8f * k) * 0.6f);
                        float fine = SlothRadialStrand(r, a, 23) * 0.05f;
                        f.r *= 1f + fine;
                        f.g *= 1f + fine;
                        f.b *= 1f + fine;
                        f.a = 1f;
                        pc.Blend(x, y, f, faceCov);
                        float blush = PixelCanvas.SoftCoverage(EllipseDp(Mathf.Abs(ux), uy, 19.5f, -11.5f, 5.5f, 3.4f) * k, 3.5f * k);
                        if (blush > 0f)
                        {
                            pc.Blend(x, y, SlothBlush, blush * faceCov * 0.32f);
                        }
                    }

                    float bandCov = PixelCanvas.SoftCoverage(SlothBandDistance(ux, uy) * k, 1.2f * k);
                    if (bandCov > 0f)
                    {
                        pc.Blend(x, y, SlothBandColor(ux, uy), bandCov);
                    }
                }
            }

            if (eyesClosed)
            {
                DrawSlothClosedEyes(pc, cx, cy, k);
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float ex = cx + (side * SlothEyeDxPx * k);
                    float ey = cy + ((SlothEyeDyPx + 0.3f) * k);
                    pc.FillCircle(ex, ey, 5.1f * k, SlothEye);
                    pc.FillEllipseSoft(ex + (0.4f * k), ey - (2f * k), 3f * k, 1.7f * k, 1.4f * k, ColorMath.WithAlpha(SlothEyeWarm, 0.7f));
                    pc.FillCircle(ex - (1.6f * k), ey + (1.8f * k), 1.9f * k, new Color(1f, 1f, 1f, 0.97f));
                    pc.FillCircle(ex + (1.8f * k), ey - (1.3f * k), 0.8f * k, new Color(1f, 1f, 1f, 0.75f));
                }
            }

            DrawSlothNose(pc, cx, cy, k);

            // La sonrisa amable: el surco bajo la nariz y la boca en ω con las comisuras hacia arriba (sin labios ni dientes).
            float lw = Mathf.Max(1.2f * k, 1.1f);
            pc.FillCapsule(cx, cy - (15.4f * k), cx, cy - (17f * k), lw * 0.5f, SlothMouth);
            for (int side = -1; side <= 1; side += 2)
            {
                float mx = cx + (side * 3.1f * k);
                pc.StrokeArc(mx, cy - (16.5f * k), 3.1f * k, lw, 190f, 350f, SlothMouth);
                pc.FillCapsule(cx + (side * 6.1f * k), cy - (17f * k), cx + (side * 7.6f * k), cy - (15.4f * k), lw * 0.45f, SlothMouth);
            }
        }

        // Nariz ancha y oscura (un óvalo con la parte baja más estrecha), más clara arriba a la izquierda, con las fosas y su brillo.
        private static void DrawSlothNose(PixelCanvas pc, float cx, float cy, float k)
        {
            Color dark = Palette.Hex(0x1A120C);
            Color lit = Palette.Hex(0x5A4536);
            int ax = Mathf.FloorToInt(cx - (10f * k));
            int bx = Mathf.CeilToInt(cx + (10f * k));
            int ay = Mathf.FloorToInt(cy - (18f * k));
            int by = Mathf.CeilToInt(cy - (5f * k));
            for (int y = ay; y < by; y++)
            {
                for (int x = ax; x < bx; x++)
                {
                    float ux = (x + 0.5f - cx) / k;
                    float uy = (y + 0.5f - cy) / k;
                    float d = SlothSmoothMin(EllipseDp(ux, uy, 0f, -10.9f, 7.6f, 4.5f), EllipseDp(ux, uy, 0f, -13.2f, 4.2f, 3.2f), 2.5f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(dark, lit, Mathf.Clamp01(((uy + 13.5f) / 7f) - (ux * 0.05f)) * 0.75f);
                    pc.Blend(x, y, c, cov);
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                pc.FillEllipseSoft(cx + (side * 2.8f * k), cy - (12.3f * k), 1.5f * k, 0.85f * k, 0.8f * k, Palette.Hex(0x0C0805));
            }

            pc.FillEllipseSoft(cx - (2.4f * k), cy - (9.4f * k), 2.6f * k, 1.05f * k, 1.1f * k, new Color(1f, 1f, 1f, 0.55f));
        }

        // Ojos cerrados en arco sobre las bandas, con una pestaña corta hacia la sien: dormido, al sol y el parpadeo.
        private static void DrawSlothClosedEyes(PixelCanvas pc, float cx, float cy, float k)
        {
            Color lid = Color.Lerp(SlothLid, SlothFace, 0.5f);
            float lw = Mathf.Max(1.7f * k, 1.3f);
            for (int side = -1; side <= 1; side += 2)
            {
                float ex = cx + (side * SlothEyeDxPx * k);
                float ey = cy + ((SlothEyeDyPx + 1.6f) * k);
                pc.StrokeArc(ex, ey, 4.6f * k, lw, 198f, 342f, lid);
                float endX = ex + (side * 4.35f * k);
                float endY = ey - (1.5f * k);
                pc.FillCapsule(endX, endY, endX + (side * 1.8f * k), endY + (1.5f * k), lw * 0.42f, lid);
            }
        }

        // Párpado: las bandas de los dos ojos (idénticas a las de la cabeza) con los ojos cerrados, centrado a la altura de los
        // ojos.
        private static void DrawSlothEyelid(PixelCanvas pc, int ox, int oy, float k)
        {
            float cx = ox + (SlothCellPx * 0.375f * k);
            float cy = oy + (((SlothCellPx * 0.125f) - SlothEyeDyPx) * k); // el centro de la cabeza en la celda del párpado
            int w = Mathf.RoundToInt(SlothCellPx * 0.75f * k);
            int h = Mathf.RoundToInt(SlothCellPx * 0.25f * k);
            for (int y = oy; y < oy + h; y++)
            {
                for (int x = ox; x < ox + w; x++)
                {
                    float ux = (x + 0.5f - cx) / k;
                    float uy = (y + 0.5f - cy) / k;
                    float cov = PixelCanvas.SoftCoverage(SlothBandDistance(ux, uy) * k, 1.2f * k);
                    if (cov > 0f)
                    {
                        pc.Blend(x, y, SlothBandColor(ux, uy), cov);
                    }
                }
            }

            DrawSlothClosedEyes(pc, cx, cy, k);
        }

        // Silueta del torso (unidades respecto al centro de la celda; negativa dentro): de frente, hombros anchos arriba y la
        // panza redonda abajo; de espaldas, más ovalada (el lomo en ovillo), con mechones que cuelgan más largos por debajo.
        private static float SlothTorsoDistance(float ux, float uy, bool back, int seed, out float valley, out float rim)
        {
            float body = back
                ? SlothSmoothMin(EllipseDp(ux, uy, 0f, -3f, 47f, 52f), EllipseDp(ux, uy, 0f, 20f, 42f, 30f), 20f)
                : SlothSmoothMin(EllipseDp(ux, uy, 0f, -5f, 44f, 50f), EllipseDp(ux, uy, 0f, 25f, 46f, 27f), 16f);
            float a = Mathf.Atan2(uy + 4f, ux);
            float amp = 1.1f + (2.9f * Mathf.Clamp01(-Mathf.Sin(a)));
            float tuft = SlothTuft(a, 15, amp, 0.15f, seed, out valley);
            rim = body;
            return body - tuft + 0.6f;
        }

        // Torso de frente (back = false) o de espaldas (true): pelo largo con volumen y mechones que caen. De frente, la panza
        // algo más clara, el matiz de algas a parches en los costados y la garganta parda arriba, bajo la barbilla (la cabeza tapa
        // su mitad alta), fundida con el pelo; de espaldas, más algas en el lomo y el espéculo del macho (parche naranja con su
        // raya negra) en el centro de la espalda: de frente no se ve (con él en el pecho se leía como un collar, c.4).
        private static void DrawSlothTorso(PixelCanvas pc, int ox, int oy, float k, bool back)
        {
            float cx = ox + (64f * k);
            float cy = oy + (64f * k);
            int size = Mathf.RoundToInt(SlothCellPx * k);
            SlothTones fur = Tones(SlothFur, 0.36f, 0.2f);
            SlothTones algae = Tones(Color.Lerp(SlothFur, SlothAlgae, 0.75f), 0.34f, 0.16f);
            SlothTones throat = Tones(SlothThroat, 0.3f, 0.14f);
            Color belly = ColorMath.Lighten(SlothFur, 0.1f);
            Color specDark = ColorMath.Darken(SlothSpeculum, 0.16f);
            Color specLight = ColorMath.Lighten(SlothSpeculum, 0.14f);
            float specX = (SlothSpeculumCenterUv.x * SlothCellPx) - 64f;
            float specY = (SlothSpeculumCenterUv.y * SlothCellPx) - 64f;
            float specRx = SlothSpeculumSizeUv.x * SlothCellPx * 0.5f;
            float specRy = SlothSpeculumSizeUv.y * SlothCellPx * 0.5f;
            float aa = 1f / k;
            for (int y = oy; y < oy + size; y++)
            {
                for (int x = ox; x < ox + size; x++)
                {
                    float ux = (x + 0.5f - cx) / k;
                    float uy = (y + 0.5f - cy) / k;
                    float d = SlothTorsoDistance(ux, uy, back, back ? 41 : 37, out float valley, out float bodyD);
                    if (d > SlothInkPx + aa)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage((d - SlothInkPx) * k));
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float nx = ux / 52f;
                    float ny = (uy - 6f) / 62f;
                    float across = ux * (1f + (0.25f * Mathf.Clamp01(-uy / 50f)));
                    float strand = SlothStrand(-uy, across, back ? 29 : 23, 8f, 16f);
                    float patch = SlothAlgaePatch(ux, uy, back ? 13 : 11);
                    float algaeAmount = back
                        ? (0.2f + (0.16f * SlothSmooth(-30f, 40f, uy))) * patch
                        : 0.24f * SlothSmooth(16f, 46f, Mathf.Abs(ux)) * SlothSmooth(-24f, 30f, uy) * patch;
                    float diffuse = SlothDiffuse(nx, ny, out _);
                    Color col = SlothFurColor(fur, algae, nx, ny, strand, algaeAmount);
                    if (!back)
                    {
                        col = Color.Lerp(col, belly, PixelCanvas.SoftCoverage(EllipseDp(ux, uy, 3f, -12f, 26f, 32f) * k, 16f * k) * 0.35f * diffuse);
                        float throatD = EllipseDp(ux, uy, 0f, 28f, 20f, 13f) + (3f * (ArtNoise.Value2(ux * 0.3f, uy * 0.12f, 9) - 0.5f));
                        float t = PixelCanvas.SoftCoverage(throatD * k, 7f * k) * 0.92f;
                        if (t > 0f)
                        {
                            Color th = Shade(throat, diffuse);
                            float m = 1f + (0.14f * strand);
                            th.r *= m;
                            th.g *= m;
                            th.b *= m;
                            col = Color.Lerp(col, th, t * 0.92f);
                        }
                    }

                    float groove = valley * Mathf.Clamp01(1f - (-bodyD / 9f)) * 0.3f;
                    col.r *= 1f - groove;
                    col.g *= 1f - groove;
                    col.b *= 1f - groove;
                    col.a = 1f;
                    pc.Blend(x, y, col, cov);
                    if (!back)
                    {
                        continue;
                    }

                    float spec = EllipseDp(ux, uy, specX, specY, specRx, specRy) + (1.2f * (ArtNoise.Value2(ux * 0.5f, uy * 0.2f, 47) - 0.5f));
                    float specCov = PixelCanvas.SoftCoverage(spec * k, 1.8f * k);
                    if (specCov > 0f)
                    {
                        Color sc = Color.Lerp(specDark, specLight, Mathf.Clamp01(0.5f - (0.05f * (ux - specX)) + (0.025f * (uy - specY))));
                        float m = 1f + (0.12f * strand);
                        sc.r *= m;
                        sc.g *= m;
                        sc.b *= m;
                        sc.a = 1f;
                        pc.Blend(x, y, sc, specCov);
                    }

                    // La raya negra del espéculo, a lo largo de la columna, afinada en las puntas.
                    float stripe = Mathf.Min(
                        SlothTaper(ux, uy, specX, specY - 12f, 0.8f, specX, specY, 1.9f),
                        SlothTaper(ux, uy, specX, specY, 1.9f, specX, specY + 12f, 0.8f));
                    float stripeCov = PixelCanvas.SoftCoverage(stripe * k, 0.9f * k);
                    if (stripeCov > 0f)
                    {
                        pc.Blend(x, y, SlothInk, stripeCov);
                    }
                }
            }
        }

        // Radio de la extremidad (unidades) a la altura uy desde abajo: del hombro (12,6) a la muñeca (9,4), afinándose con una
        // curva suave; la mitad (28-100) es la parte que estira el 9-slice.
        private static float SlothLimbRadius(float uy)
        {
            if (uy >= 100f)
            {
                return 12.6f;
            }

            if (uy <= 28f)
            {
                return 9.4f;
            }

            float t = (uy - 28f) / 72f;
            return Mathf.Lerp(9.4f, 12.6f, t) + (0.5f * Mathf.Sin(Mathf.PI * t));
        }

        // Extremidad (de arriba, el hombro o la cadera, hacia abajo): forma orgánica que se afina hacia la muñeca, con pelo en la
        // dirección del brazo, el borde desgreñado, volumen simétrico (la pieza gira en todas las poses: la luz no tiene lado),
        // el matiz de algas a parches, la mano más oscura y las tres garras largas y curvas de hueso claro, en gancho como las
        // que se enganchan a la rama (c.5, punto 4).
        private static void DrawSlothLimb(PixelCanvas pc, int ox, int oy, float k)
        {
            int w = Mathf.RoundToInt(32f * k);
            int h = Mathf.RoundToInt(SlothCellPx * k);
            SlothTones fur = Tones(ColorMath.Darken(SlothFur, 0.04f), 0.34f, 0.18f);
            SlothTones algae = Tones(Color.Lerp(SlothFur, SlothAlgae, 0.75f), 0.32f, 0.14f);
            SlothTones paw = Tones(ColorMath.Darken(SlothFur, 0.26f), 0.3f, 0.12f);
            float aa = 1f / k;
            for (int y = oy; y < oy + h; y++)
            {
                for (int x = ox; x < ox + w; x++)
                {
                    float ux = ((x + 0.5f - ox) / k) - 16f;
                    float uy = (y + 0.5f - oy) / k;
                    float r = SlothLimbRadius(uy);
                    float arm;
                    if (uy > 112.5f)
                    {
                        arm = Mathf.Sqrt((ux * ux) + ((uy - 112.5f) * (uy - 112.5f))) - 12.6f;
                    }
                    else
                    {
                        arm = Mathf.Max(Mathf.Abs(ux) - r, 20f - uy);
                    }

                    if (uy > 22f && uy < 108f)
                    {
                        float f = (uy / 6.5f) + (ux < 0f ? 0.3f : 0.75f);
                        float t = 1f - Mathf.Abs((2f * (f - Mathf.Floor(f))) - 1f);
                        arm -= 0.9f * t * t * SlothSmooth(22f, 30f, uy);
                    }

                    float pawD = EllipseDp(ux, uy, 0f, 19f, 11f, 9.5f);
                    float d = SlothSmoothMin(arm, pawD, 3f);
                    if (d > SlothInkPx + aa)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage((d - SlothInkPx) * k));
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float nx = Mathf.Clamp(ux / (r + 1.5f), -1f, 1f);
                    float round = Mathf.Sqrt(1f - (nx * nx));
                    float diffuse = 0.18f + (0.82f * round);
                    float strand = SlothStrand(SlothCellPx - uy, ux, 53, 6.5f, 13f);
                    float algaeAmount = 0.26f * SlothAlgaePatch(ux * 2f, uy * 0.8f, 19) * SlothSmooth(30f, 46f, uy);
                    Color col = Color.Lerp(Shade(fur, diffuse), Shade(algae, diffuse), algaeAmount);
                    col = Color.Lerp(col, Shade(paw, diffuse), SlothSmooth(28f, 16f, uy));
                    float m = 1f + (0.15f * strand);
                    col.r *= m;
                    col.g *= m;
                    col.b *= m;
                    float edge = Mathf.Abs(nx);
                    col = Color.Lerp(col, SlothRimLight, edge * edge * edge * edge * 0.16f);
                    col.a = 1f;
                    pc.Blend(x, y, col, cov);
                }
            }

            // Las tres garras: de la mano hacia abajo, en gancho (bajan y vuelven a subir de lado), afinándose hacia la punta.
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    float bx = (i * 5.8f) - 1.2f;
                    float b0x = bx;
                    float b0y = 17f;
                    float b1x = bx + 0.2f + (i * 0.7f);
                    float b1y = 0.2f;
                    float b2x = bx + 6.4f + (i * 0.5f);
                    float b2y = 3.4f;
                    DrawSlothClaw(pc, ox, oy, k, b0x, b0y, b1x, b1y, b2x, b2y, pass);
                }
            }
        }

        // Una garra como trazo de Bézier cuadrática que se afina (unidades de la pieza de la extremidad, x desde su eje): pass 0
        // el contorno, 1 el hueso con su degradado (base parda, punta marfil) y 2 el brillo a lo largo del lomo de la garra.
        private static void DrawSlothClaw(PixelCanvas pc, int ox, int oy, float k, float x0, float y0, float x1, float y1, float x2, float y2, int pass)
        {
            const int steps = 14;
            float px = x0;
            float py = y0;
            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                float u = 1f - t;
                float qx = (u * u * x0) + (2f * u * t * x1) + (t * t * x2);
                float qy = (u * u * y0) + (2f * u * t * y1) + (t * t * y2);
                float r = Mathf.Lerp(2.5f, 0.4f, t);
                float ax = ox + ((16f + px) * k);
                float ay = oy + (py * k);
                float bx = ox + ((16f + qx) * k);
                float by = oy + (qy * k);
                if (pass == 0)
                {
                    pc.FillCapsule(ax, ay, bx, by, (r + 1.1f) * k, SlothClawInk);
                }
                else if (pass == 1)
                {
                    Color c = t < 0.5f ? Color.Lerp(SlothClawBase, SlothClaw, t / 0.5f) : Color.Lerp(SlothClaw, SlothClawTip, (t - 0.5f) / 0.5f);
                    pc.FillCapsule(ax, ay, bx, by, r * k, c);
                }
                else if (t < 0.8f)
                {
                    Color c = Color.Lerp(SlothClawTip, Color.white, 0.4f);
                    pc.FillCapsule(ax - (0.7f * r * k), ay + (0.2f * k), bx - (0.7f * r * k), by + (0.2f * k), Mathf.Max(0.32f * r * k, 0.6f), c);
                }

                px = qx;
                py = qy;
            }
        }

        // Hoja de embaúba (Cecropia): lámina palmeada de 7 lóbulos con los nervios claros del envés y su peciolo. Pendiente del
        // asesor cultural (propuesta de TiTino g.2, punto 1, y c.4); si la desaconseja, la alternativa prevista es una hoja
        // genérica sin especie reconocible, como la rama (ProceduralArt.Home.cs, M1.5). Nunca quemada ni con ceniza.
        private static void DrawSlothLeafCell(PixelCanvas pc, int ox, int oy, float k)
        {
            float cx = ox + (64f * k);
            float cy = oy + (58f * k);
            Color ink = SlothOutline;
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? 2.5f : 0f;
                Color col = pass == 0 ? ink : EmbaubaLeaf;
                pc.FillCapsule(cx, cy, cx, oy + (10f * k), (2.5f + grow) * k, col);
                pc.FillCircle(cx, cy, (15f + grow) * k, col);
                for (int i = 0; i < 7; i++)
                {
                    float a = (90f + ((i - 3) * 38f)) * Mathf.Deg2Rad;
                    float lx = cx + (Mathf.Cos(a) * 28f * k);
                    float ly = cy + (Mathf.Sin(a) * 28f * k);
                    pc.FillEllipseSoft(lx, ly, (11f + grow) * k, (11f + grow) * k, k, col);
                    pc.FillCapsule(cx + (Mathf.Cos(a) * 10f * k), cy + (Mathf.Sin(a) * 10f * k), cx + (Mathf.Cos(a) * 40f * k), cy + (Mathf.Sin(a) * 40f * k), (9f + grow) * k, col);
                }
            }

            pc.FillEllipseSoft(cx - (10f * k), cy + (14f * k), 16f * k, 10f * k, 8f * k, ColorMath.WithAlpha(ColorMath.Lighten(EmbaubaLeaf, 0.3f), 0.4f)); // luz
            for (int i = 0; i < 7; i++)
            {
                float a = (90f + ((i - 3) * 38f)) * Mathf.Deg2Rad;
                pc.FillCapsule(cx, cy, cx + (Mathf.Cos(a) * 42f * k), cy + (Mathf.Sin(a) * 42f * k), 1.1f * k, EmbaubaLeafUnder);
            }
        }

        // Rama de embaúba: corteza lisa y pálida con volumen de cilindro (luz arriba, sombra abajo), vetas finas a lo largo y los
        // anillos de las cicatrices de las hojas; un poco arqueada, con el contorno de tinta y los extremos
        // redondeados. Pendiente del asesor cultural (propuesta de TiTino g.2, punto 1, y c.4); si la desaconseja, la alternativa
        // prevista es una rama genérica sin especie reconocible (o la que proponga el asesor), como en ProceduralArt.Home.cs
        // (M1.5). k: px por unidad del lienzo de 512 × 128.
        private static void DrawEmbaubaBranch(PixelCanvas pc, float k)
        {
            float w = pc.Width / k; // en unidades: 512
            const float r = 18f;
            const int steps = 48;
            for (int pass = 0; pass < 2; pass++)
            {
                float px = 14f;
                float py = BranchY(px, w);
                for (int i = 1; i <= steps; i++)
                {
                    float qx = 14f + ((w - 28f) * i / steps);
                    float qy = BranchY(qx, w);
                    if (pass == 0)
                    {
                        pc.FillCapsule(px * k, py * k, qx * k, qy * k, (r + SlothInkPx) * k, new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 1f));
                    }
                    else
                    {
                        pc.FillCapsule(px * k, py * k, qx * k, qy * k, r * k, EmbaubaBark);
                    }

                    px = qx;
                    py = qy;
                }
            }

            // Volumen y veta: cada píxel de la corteza se sombrea por su altura en el cilindro (v = −1 abajo, 1 arriba).
            Color shadow = ColorMath.Darken(EmbaubaBark, 0.3f);
            Color light = ColorMath.Lighten(EmbaubaBark, 0.1f);
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    Color o = pc.Get(x, y);
                    if (o.a <= 0f)
                    {
                        continue;
                    }

                    float ux = (x + 0.5f) / k;
                    float uy = (y + 0.5f) / k;
                    float v = (uy - BranchY(ux, w)) / r;
                    float inside = Mathf.Clamp01((1f - Mathf.Abs(v)) * r * k * 0.5f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float diffuse = Mathf.Clamp01(0.55f + (0.55f * v) - (0.35f * v * v));
                    Color c = diffuse < 0.6f ? Color.Lerp(shadow, EmbaubaBark, diffuse / 0.6f) : Color.Lerp(EmbaubaBark, light, (diffuse - 0.6f) / 0.4f);
                    float grain = (ArtNoise.Value2(ux * 0.05f, uy * 0.6f, 71) - 0.5f) * 0.12f;
                    float m = 1f + grain;
                    c.r *= m;
                    c.g *= m;
                    c.b *= m;
                    float hl = Mathf.Clamp01(1f - (Mathf.Abs(v - 0.5f) / 0.14f)) * 0.35f;
                    c = Color.Lerp(c, Color.white, hl);
                    c.a = 1f;
                    pc.Set(x, y, Color.Lerp(o, c, inside));
                }
            }

            for (float x = 62f; x < w - 40f; x += 72f)
            {
                float y = BranchY(x, w);
                pc.StrokeArc((x - 30f) * k, y * k, 31f * k, 1.6f * k, -34f, 34f, ColorMath.WithAlpha(EmbaubaRing, 0.85f));
                pc.StrokeArc((x - 29f) * k, y * k, 31f * k, 1f * k, -30f, 30f, ColorMath.WithAlpha(ColorMath.Lighten(EmbaubaBark, 0.12f), 0.7f));
            }
        }

        // La pieza sigue viva: la creó este ProceduralArt y no se liberó (tras Dispose, la lista está vacía aunque Unity aún no
        // haya destruido el objeto, que en juego se destruye al final del fotograma).
        private bool SlothOwned(Object? piece)
        {
            return piece != null && _owned.Contains(piece);
        }

        private static float BranchY(float x, float width)
        {
            return 64f + (6f * Mathf.Sin(x / width * Mathf.PI));
        }
    }
}
