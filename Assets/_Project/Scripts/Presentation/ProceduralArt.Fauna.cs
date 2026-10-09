#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas de un miquito para su rig recortable (guion de la intro §11 y §12): cabeza con la cara y el rasgo de reposo
    /// de la especie, torso, extremidad (9-slice vertical) y párpado, en una sola textura de 256 × 256 por especie (un lote
    /// de dibujo), con la paleta del catálogo y el contorno de tinta de 2 px de Capi. La cola es una
    /// <see cref="BezierStripGraphic"/> con los colores de la paleta. Las medidas van en S (el tamaño del miquito en pantalla).
    /// </summary>
    public sealed class MonkeyArt
    {
        internal MonkeyArt(string id, FaunaSkeleton skeleton)
        {
            Id = id;
            Skeleton = skeleton;
        }

        /// <summary>Id de la especie.</summary>
        public string Id { get; }

        /// <summary>Esqueleto.</summary>
        public FaunaSkeleton Skeleton { get; }

        /// <summary>Cabeza (cuadrada, centro en el centro del cráneo).</summary>
        public Sprite Head { get; internal set; } = null!;

        /// <summary>Torso (elipse que llena el sprite).</summary>
        public Sprite Torso { get; internal set; } = null!;

        /// <summary>Extremidad: cápsula vertical 9-slice con la mano o el pie abajo.</summary>
        public Sprite Limb { get; internal set; } = null!;

        /// <summary>Párpado para el parpadeo de 120 ms.</summary>
        public Sprite Eyelid { get; internal set; } = null!;

        /// <summary>Retrato de la tarjeta (el PNG <c>fauna_&lt;id&gt;_retrato</c> o la cabeza procedural).</summary>
        public Sprite Portrait { get; internal set; } = null!;

        /// <summary>Lado de la cabeza en pantalla (S).</summary>
        public float HeadS { get; internal set; } = 0.6f;

        /// <summary>Centro del párpado respecto al centro de la cabeza (S; mirando a la derecha).</summary>
        public Vector2 EyelidOffsetS { get; internal set; }

        /// <summary>Tamaño del párpado (S).</summary>
        public Vector2 EyelidSizeS { get; internal set; }

        /// <summary>Ancho de las extremidades (S).</summary>
        public float LimbWidthS { get; internal set; } = 0.13f;

        /// <summary>Largo relativo de las extremidades (D: brazos larguísimos).</summary>
        public float LimbScale { get; internal set; } = 1f;

        /// <summary>Color de la cola.</summary>
        public Color Tail { get; internal set; }

        /// <summary>Color de la punta de la cola.</summary>
        public Color TailTip { get; internal set; }

        /// <summary>Grosor de la cola (S).</summary>
        public float TailWidthS { get; internal set; } = 0.07f;

        /// <summary>Largo de la cola (1 = la del mono ardilla; 0,35 la del huapo).</summary>
        public float TailLength { get; internal set; } = 1f;

        /// <summary>La cola se enrolla (cola prensil de los atélidos).</summary>
        public bool TailCurls { get; internal set; }

        /// <summary>Ancho relativo del torso (1; el churuco lanudo, 1,25: más ancho que el maquisapa).</summary>
        public float TorsoWidthScale { get; internal set; } = 1f;

        /// <summary>Lleva la fruta abrazada al pecho hasta soltarla (rasgo <c>lanudo</c>: el churuco).</summary>
        public bool HugsFruit { get; internal set; }

        /// <summary>Colgado, un brazo arriba agarrado a la rama además de la cola (rasgo <c>brazos_largos</c>: braquiación).</summary>
        public bool ArmUp { get; internal set; }

        /// <summary>Bigote blanco largo (rasgo <c>bigote_blanco</c>: el pichico emperador), pieza aparte de la cabeza; null si no.</summary>
        public Sprite? Whiskers { get; internal set; }

        /// <summary>Centro del bigote respecto al centro de la cabeza (S; mirando a la derecha).</summary>
        public Vector2 WhiskersOffsetS { get; internal set; }

        /// <summary>Tamaño de la pieza del bigote (S).</summary>
        public Vector2 WhiskersSizeS { get; internal set; }

        /// <summary>Las piezas salen del PNG del artista.</summary>
        public bool Painted { get; internal set; }
    }

    public sealed partial class ProceduralArt
    {
        private const int FaunaAtlasPx = 256;

        // Celda del bigote (pichico emperador): 176 × 78 px a la derecha de la extremidad y el párpado, en el espacio de la
        // celda de la cabeza desplazado (−8, −20) px: el bigote baja más allá de la cabeza, hasta pasar los hombros.
        private const int WhiskersCellX = 80;
        private const int WhiskersCellW = 176;
        private const int WhiskersCellH = 78;
        private const float WhiskersHeadX0 = -8f;
        private const float WhiskersHeadY0 = -20f;
        private const float FaunaPxPerS = 128f / 0.6f; // la celda de la cabeza (128 px) mide 0,6 S

        // Babero del sauim en la celda de la cabeza (A: 128 px = 0,58 S): 0,45 del ancho del torso colgado (0,34 S) ≈ 34 px de
        // ancho, y 12 px de alto, de los que asoman unos 14 bajo el cráneo.
        private const float CollarRxPx = 18f;
        private const float CollarRyPx = 12f;
        private static readonly Color FaunaInk = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);

        private readonly Dictionary<string, MonkeyArt> _monkeys = new Dictionary<string, MonkeyArt>();

        /// <summary>Tiempo de generación de la última especie (≤ 10 ms; 0 si salió de la caché o del PNG).</summary>
        public double FaunaArtMs { get; private set; }

        /// <summary>
        /// Piezas de la especie <paramref name="species"/>, generadas de forma perezosa la primera vez (≤ 10 ms) y guardadas en
        /// caché. Si existe <c>fauna_&lt;id&gt;_partes</c> (rejilla de 4 × 4 celdas de 256 px: cabeza, torso, extremidad y
        /// párpado en la primera fila), se usan sus celdas. Con <paramref name="pair"/> y una <c>paleta_pareja</c> en el
        /// catálogo, las del segundo individuo (dimorfismo: la hembra del saquí); sin ella, las mismas.
        /// </summary>
        public MonkeyArt MonkeyArtFor(FaunaSpecies species, bool pair = false)
        {
            pair = pair && species.PairPalette != null;
            string key = pair ? species.Id + "#pareja" : species.Id;
            if (_monkeys.TryGetValue(key, out MonkeyArt? cached))
            {
                FaunaArtMs = 0;
                return cached;
            }

            Stopwatch watch = Stopwatch.StartNew();
            var art = new MonkeyArt(species.Id, species.Skeleton);
            FaunaPalette p = pair ? species.PairPalette! : species.Palette;
            art.Tail = Hex(p.Tail);
            art.TailTip = Hex(p.TailTip);
            switch (species.Skeleton)
            {
                case FaunaSkeleton.A:
                    art.HeadS = 0.58f;
                    art.TailWidthS = 0.06f;
                    art.LimbWidthS = 0.11f;
                    break;
                case FaunaSkeleton.C:
                    art.HeadS = 0.62f;
                    art.TailWidthS = species.HasTrait("cola_gruesa") ? 0.16f : 0.12f;
                    art.TailLength = species.HasTrait("cola_corta") ? 0.35f : 1f;
                    art.LimbWidthS = 0.14f;
                    break;
                case FaunaSkeleton.D:
                    // Atélidos (guion §4.2): el churuco, grande y lanudo, de torso ancho y extremidades cortas, con la fruta
                    // abrazada al pecho; el maquisapa, de brazos larguísimos y cabeza pequeña, colgado de la cola y de un brazo.
                    bool woolly = species.HasTrait("lanudo");
                    bool longArms = species.HasTrait("brazos_largos");
                    art.HeadS = longArms ? 0.48f * 0.8f : 0.48f;
                    art.TailWidthS = 0.07f;
                    art.TailCurls = true;
                    art.LimbScale = longArms ? 1.4f : (woolly ? 0.8f : 1.2f);
                    art.LimbWidthS = longArms ? 0.09f : (woolly ? 0.14f : 0.11f);
                    art.TorsoWidthScale = woolly ? 1.25f : (longArms ? 0.85f : 1f);
                    art.HugsFruit = woolly;
                    art.ArmUp = longArms;
                    break;
                default:
                    art.HeadS = 0.6f;
                    art.TailWidthS = 0.07f;
                    art.LimbWidthS = 0.12f;
                    break;
            }

            Sprite? parts = ArtSlots.Find(species.PartsSlot);
            if (parts != null)
            {
                Texture2D tex = parts.texture;
                int cell = tex.width / 4;
                art.Head = SubSprite(tex, new Rect(0, tex.height - cell, cell, cell), "head", 0f);
                art.Torso = SubSprite(tex, new Rect(cell, tex.height - cell, cell, cell), "torso", 0f);
                art.Limb = SubSprite(tex, new Rect(2 * cell, tex.height - cell, cell / 4f, cell), "limb", cell * 0.2f);
                art.Eyelid = SubSprite(tex, new Rect(3 * cell, tex.height - (cell / 4f), cell * 0.4f, cell * 0.16f), "eyelid", 0f);
                art.Painted = true;
            }
            else
            {
                PixelCanvas pc = BuildFaunaCanvas(species, art, p);
                Texture2D tex = pc.ToTexture("fauna_" + key);
                _owned.Add(tex);
                art.Head = SubSprite(tex, new Rect(0, 128, 128, 128), "head", 0f);
                art.Torso = SubSprite(tex, new Rect(128, 128, 128, 128), "torso", 0f);
                art.Limb = SubSprite(tex, new Rect(0, 0, 32, 112), "limb", 24f);
                art.Eyelid = SubSprite(tex, new Rect(32, 0, 48, 20), "eyelid", 0f);
                if (species.HasTrait("bigote_blanco"))
                {
                    art.Whiskers = SubSprite(tex, new Rect(WhiskersCellX, 0, WhiskersCellW, WhiskersCellH), "whiskers", 0f);
                    float k = art.HeadS / 128f; // px de la celda de la cabeza → S
                    art.WhiskersOffsetS = new Vector2((WhiskersHeadX0 + (WhiskersCellW * 0.5f)) - 64f, (WhiskersHeadY0 + (WhiskersCellH * 0.5f)) - 64f) * k;
                    art.WhiskersSizeS = new Vector2(WhiskersCellW, WhiskersCellH) * k;
                }
            }

            Sprite? portrait = ArtSlots.Find(species.PortraitSlot);
            art.Portrait = portrait != null ? portrait : art.Head;
            _monkeys[key] = art;
            watch.Stop();
            FaunaArtMs = watch.Elapsed.TotalMilliseconds;
            return art;
        }

        private Sprite SubSprite(Texture2D tex, Rect rect, string name, float borderPx)
        {
            var border = new Vector4(0f, borderPx, 0f, borderPx);
            Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f * UiPxPerDp, 0, SpriteMeshType.FullRect, border);
            sprite.name = tex.name + "_" + name;
            sprite.hideFlags = HideFlags.DontSave;
            _owned.Add(sprite);
            return sprite;
        }

        // Celdas: cabeza (0, 128), torso (128, 128), extremidad (0, 0, 32 × 112) y párpado (32, 0, 48 × 20). Mirando a la derecha,
        // luz desde arriba a la izquierda; contorno de tinta al 80 % (como Capi).
        private static PixelCanvas BuildFaunaCanvas(FaunaSpecies sp, MonkeyArt art, FaunaPalette p)
        {
            var pc = new PixelCanvas(FaunaAtlasPx, FaunaAtlasPx);
            Color body = Hex(p.Body);
            Color limbs = Hex(p.Limbs);
            Color face = Hex(p.Face);
            Color muzzle = Hex(p.Muzzle);
            Color cap = Hex(p.Cap);
            DrawHead(pc, sp, art, 0f, 128f, body, face, muzzle, cap, limbs);
            DrawTorso(pc, sp, 128f, 128f, body, limbs, face);
            if (sp.HasTrait("collar_blanco"))
            {
                limbs = body; // el blanco del sauim es el collar (el babero del torso): los brazos, del color del cuerpo
            }

            if (sp.HasTrait("bigote_blanco"))
            {
                DrawWhiskers(pc, muzzle);
            }

            // Extremidad: cápsula vertical con la mano o el pie más oscuros abajo.
            pc.FillCapsule(16f, 100f, 16f, 12f, 13f, FaunaInk);
            pc.FillCapsule(16f, 100f, 16f, 12f, 11f, limbs);
            pc.FillCircle(16f, 13f, 9.5f, ColorMath.Darken(limbs, 0.22f));
            pc.FillCapsule(11f, 96f, 11f, 30f, 2.4f, ColorMath.WithAlpha(ColorMath.Lighten(limbs, 0.35f), 0.55f)); // luz

            // Párpado: elipse del color de la cara con su línea de tinta.
            Color lid = sp.HasTrait("cara_roja") || sp.HasTrait("mascara_redondeada") || sp.HasTrait("cara_blanca") || sp.HasTrait("ojos_grandes") || sp.HasTrait("orejas_desnudas") ? face : cap;
            pc.FillEllipseSoft(56f, 10f, 22f, 8.5f, 1f, FaunaInk);
            pc.FillEllipseSoft(56f, 11f, 20f, 7f, 1f, lid);
            return pc;
        }

        private static void DrawHead(PixelCanvas pc, FaunaSpecies sp, MonkeyArt art, float ox, float oy, Color body, Color face, Color muzzle, Color cap, Color limbs)
        {
            float cx = ox + 60f;
            float cy = oy + 58f;
            float r = 40f;
            bool fluffy = sp.Skeleton == FaunaSkeleton.C || sp.HasTrait("lanudo");
            bool bigEars = sp.HasTrait("orejas_desnudas") || sp.Skeleton == FaunaSkeleton.A;

            // Orejas, detrás del cráneo.
            if (bigEars)
            {
                Color ear = sp.HasTrait("orejas_desnudas") ? face : cap;
                FaunaDisc(pc, cx - 26f, cy + 30f, 14f, ear);
                FaunaDisc(pc, cx - 6f, cy + 38f, 10f, ColorMath.Darken(ear, 0.12f));
                pc.FillCircle(cx - 26f, cy + 30f, 7f, ColorMath.Darken(ear, 0.25f));
            }
            else if (!fluffy)
            {
                FaunaDisc(pc, cx - 24f, cy + 26f, 8f, face);
            }

            if (sp.HasTrait("collar_blanco"))
            {
                // Sauim: el babero blanco asoma bajo la cabeza negra, por delante (va en la pieza de la cabeza, por encima del
                // torso: el brazo de delante tapa el pecho del torso cuando cuelga). El blanco del catálogo va en «extremidades».
                Color white = limbs;
                pc.FillEllipseSoft(cx + 14f, oy + 14f, CollarRxPx + 2f, CollarRyPx + 2f, 1f, FaunaInk);
                pc.FillEllipseSoft(cx + 14f, oy + 14f, CollarRxPx, CollarRyPx, 1.5f, white);
            }

            // Pelo mullido o lanudo alrededor (pitécidos y churuco).
            if (fluffy)
            {
                Color fur = body;
                for (int i = 0; i < 11; i++)
                {
                    float a = (95f + (i * 22f)) * Mathf.Deg2Rad;
                    pc.FillCircle(cx + (Mathf.Cos(a) * (r + 2f)), cy + (Mathf.Sin(a) * (r + 2f)), 13f, FaunaInk);
                }

                for (int i = 0; i < 11; i++)
                {
                    float a = (95f + (i * 22f)) * Mathf.Deg2Rad;
                    pc.FillCircle(cx + (Mathf.Cos(a) * (r + 2f)), cy + (Mathf.Sin(a) * (r + 2f)), 11f, fur);
                }
            }

            // Cráneo con la gorra (el color de arriba de la cabeza).
            pc.FillCircle(cx, cy, r + 3.5f, FaunaInk);
            pc.FillCircle(cx, cy, r, cap);
            pc.FillEllipseSoft(cx - 12f, cy + 16f, 16f, 10f, 8f, ColorMath.WithAlpha(ColorMath.Lighten(cap, 0.3f), 0.45f)); // luz

            Vector2 nearEye = new Vector2(cx + 26f, cy + 6f);
            Vector2 farEye = new Vector2(cx + 9f, cy + 9f);
            float eyeR = 7f; // ojos grandes con brillo: se leen a 40 dp
            float nose = 0f;
            if (sp.HasTrait("mascara_redondeada"))
            {
                // Máscara blanca redondeada y cálida alrededor de los ojos (que no se lea como calavera) y hocico negro.
                pc.FillEllipseSoft(cx + 19f, cy + 7f, 24f, 18f, 2f, face);
                pc.FillEllipseSoft(cx + 24f, cy - 7f, 18f, 14f, 2f, face);
                pc.FillEllipseSoft(cx + 33f, cy - 12f, 11f, 8.5f, 1.5f, muzzle);
                nose = 1f;
            }
            else if (sp.HasTrait("cara_roja"))
            {
                // Cara roja desnuda (señal de salud, nunca burla): cubre la frente y la cara.
                pc.FillEllipseSoft(cx + 14f, cy + 2f, 30f, 32f, 2f, face);
                pc.FillEllipseSoft(cx + 30f, cy - 12f, 9f, 7f, 2f, muzzle);
                pc.FillEllipseSoft(cx + 6f, cy + 22f, 14f, 6f, 6f, ColorMath.WithAlpha(ColorMath.Lighten(face, 0.3f), 0.5f));
            }
            else if (sp.HasTrait("cara_blanca"))
            {
                // Saquí macho: disco blanco alrededor de una cara oscura pequeña.
                pc.FillEllipseSoft(cx + 16f, cy + 1f, 27f, 29f, 2f, face);
                pc.FillEllipseSoft(cx + 29f, cy - 9f, 9f, 8f, 2f, muzzle);
            }
            else if (sp.HasTrait("ojos_grandes"))
            {
                // Mono nocturno: ojos enormes (radio × 1,8) con el iris ámbar, manchas blancas sobre los ojos entre tres rayas
                // oscuras y un anillo pardo alrededor que separa el blanco de la cara clara.
                eyeR = 7f * 1.8f;
                nearEye = new Vector2(cx + 30f, cy + 2f);
                farEye = new Vector2(cx + 4f, cy + 6f);
                pc.FillEllipseSoft(cx + 16f, cy + 1f, 30f, 29f, 2f, face);
                Color eyeRing = ColorMath.WithAlpha(muzzle, 0.85f);
                pc.FillCircle(nearEye.x, nearEye.y, eyeR + 3.2f, eyeRing);
                pc.FillCircle(farEye.x, farEye.y, (eyeR * 0.85f) + 3f, eyeRing);
                var white = new Color(0.98f, 0.97f, 0.94f, 1f);
                pc.FillEllipseSoft(nearEye.x + 1f, nearEye.y + eyeR + 6f, eyeR * 0.85f, 4.6f, 1.5f, white);
                pc.FillEllipseSoft(farEye.x, farEye.y + (eyeR * 0.85f) + 6f, eyeR * 0.72f, 4.2f, 1.5f, white);
                pc.FillCapsule(cx + 17f, cy + 22f, cx + 17f, cy + 40f, 3.2f, cap);
                pc.FillCapsule(cx - 6f, cy + 20f, cx - 12f, cy + 34f, 2.8f, cap);
                pc.FillCapsule(cx + 42f, cy + 18f, cx + 44f, cy + 30f, 2.8f, cap);
                pc.FillEllipseSoft(cx + 33f, cy - 15f, 7f, 5f, 2f, muzzle);
            }
            else
            {
                // Cara desnuda (sagui: color carne; sauim, churuco, maquisapa: negra; pichico: oscura).
                pc.FillEllipseSoft(cx + 17f, cy + 1f, 22f, 25f, 2f, face);
                pc.FillEllipseSoft(cx + 30f, cy - 10f, 8f, 6f, 2f, muzzle);
            }

            // Ojos (3/4 mirando a la derecha) con brillo; en caras oscuras, un anillo claro para que se lean a 40 dp.
            bool darkFace = ColorMath.RelativeLuminance(face) < 0.08f;
            Color ring = ColorMath.Lighten(face, 0.45f);
            if (darkFace)
            {
                pc.FillCircle(nearEye.x, nearEye.y, eyeR + 2.2f, ring);
                pc.FillCircle(farEye.x, farEye.y, (eyeR * 0.85f) + 2f, ring);
            }

            bool bigEyes = sp.HasTrait("ojos_grandes");
            Color iris = bigEyes ? Hex(0xC8902A) : new Color(0x2A / 255f, 0x1A / 255f, 0x10 / 255f, 1f);
            pc.FillCircle(nearEye.x, nearEye.y, eyeR, iris);
            pc.FillCircle(farEye.x, farEye.y, eyeR * 0.85f, iris);
            if (bigEyes)
            {
                var pupil = new Color(0.07f, 0.05f, 0.04f, 1f);
                pc.FillCircle(nearEye.x + (eyeR * 0.08f), nearEye.y - (eyeR * 0.04f), eyeR * 0.5f, pupil);
                pc.FillCircle(farEye.x + (eyeR * 0.06f), farEye.y - (eyeR * 0.04f), eyeR * 0.42f, pupil);
            }
            pc.FillCircle(nearEye.x - (eyeR * 0.35f), nearEye.y + (eyeR * 0.35f), Mathf.Max(1.6f, eyeR * 0.3f), Color.white);
            pc.FillCircle(farEye.x - (eyeR * 0.3f), farEye.y + (eyeR * 0.3f), Mathf.Max(1.4f, eyeR * 0.26f), Color.white);
            if (sp.Lighting == FaunaLighting.Night)
            {
                pc.FillCircle(nearEye.x + (eyeR * 0.3f), nearEye.y - (eyeR * 0.3f), 1.4f, new Color(0.85f, 0.92f, 1f, 0.9f)); // destello de luna
            }

            if (nose > 0f)
            {
                pc.FillCircle(cx + 39f, cy - 10f, 2f, FaunaInk);
            }

            art.EyelidOffsetS = new Vector2(((nearEye.x + farEye.x) * 0.5f) - (ox + 64f), ((nearEye.y + farEye.y) * 0.5f) - (oy + 64f)) / FaunaPxPerS * (art.HeadS / 0.6f);
            art.EyelidSizeS = new Vector2((Mathf.Abs(nearEye.x - farEye.x) + (eyeR * 2.6f)) / FaunaPxPerS, eyeR * 2.4f / FaunaPxPerS) * (art.HeadS / 0.6f);
        }

        private static void DrawTorso(PixelCanvas pc, FaunaSpecies sp, float ox, float oy, Color body, Color limbs, Color face)
        {
            float cx = ox + 64f;
            float cy = oy + 62f;
            bool woolly = sp.HasTrait("lanudo") || sp.Skeleton == FaunaSkeleton.C;
            if (woolly)
            {
                for (int i = 0; i < 14; i++)
                {
                    float a = i * (Mathf.PI * 2f / 14f);
                    pc.FillCircle(cx + (Mathf.Cos(a) * 50f), cy + (Mathf.Sin(a) * 40f), 11f, FaunaInk);
                }
            }

            pc.FillEllipseSoft(cx, cy, 55f, 45f, 1f, FaunaInk);
            if (woolly)
            {
                for (int i = 0; i < 14; i++)
                {
                    float a = i * (Mathf.PI * 2f / 14f);
                    pc.FillCircle(cx + (Mathf.Cos(a) * 50f), cy + (Mathf.Sin(a) * 40f), 9f, body);
                }
            }

            pc.FillEllipseSoft(cx, cy, 51.5f, 41.5f, 1f, body);
            pc.FillEllipseSoft(cx - 14f, cy + 18f, 26f, 12f, 10f, ColorMath.WithAlpha(ColorMath.Lighten(body, 0.3f), 0.45f)); // luz

            if (sp.HasTrait("collar_blanco"))
            {
                // Sauim: babero blanco elíptico de 0,45 del ancho del torso, en la mitad de arriba del pecho, que sigue al de la
                // pieza de la cabeza (más arriba, la cabeza lo tapa; el blanco del catálogo va en «extremidades» y los brazos
                // salen del color del cuerpo).
                float bibRx = 0.45f * 51.5f;
                pc.FillEllipseSoft(cx + 12f, cy + 2f, bibRx + 2f, 22f, 1f, FaunaInk);
                pc.FillEllipseSoft(cx + 12f, cy + 2f, bibRx, 20f, 1.5f, limbs);
            }
            else if (sp.HasTrait("pecho_rojizo"))
            {
                pc.FillEllipseSoft(cx + 28f, cy - 4f, 22f, 30f, 3f, limbs);
            }
            else if (sp.Skeleton == FaunaSkeleton.B)
            {
                pc.FillEllipseSoft(cx + 24f, cy - 12f, 24f, 22f, 6f, ColorMath.WithAlpha(ColorMath.Lighten(body, 0.35f), 0.8f)); // vientre claro
            }

            if (sp.HasTrait("cria_espalda"))
            {
                // El padre carga a la cría en la espalda.
                pc.FillCircle(cx - 30f, cy + 38f, 17f, FaunaInk);
                pc.FillCircle(cx - 30f, cy + 38f, 14.5f, body);
                pc.FillEllipseSoft(cx - 24f, cy + 37f, 8f, 8f, 1f, face);
                pc.FillCircle(cx - 21f, cy + 39f, 2.6f, new Color(0.16f, 0.1f, 0.06f, 1f));
                pc.FillCircle(cx - 27f, cy + 40f, 2.2f, new Color(0.16f, 0.1f, 0.06f, 1f));
            }
        }

        // Bigote del pichico emperador (guion §4.2: bigote blanco largo que pasa de los hombros): dos trazos de 3 dp que se
        // afinan, curvados hacia fuera y hacia abajo desde el hocico hasta 0,6 de la cabeza por cada lado, con contorno de
        // tinta. Coordenadas de la celda de la cabeza (centro del cráneo en (60, 58), hocico en (90, 48)), llevadas a la celda
        // del bigote.
        private static void DrawWhiskers(PixelCanvas pc, Color white)
        {
            float reach = 0.6f * 80f; // 0,6 del diámetro del cráneo (80 px) por lado
            for (int pass = 0; pass < 2; pass++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var a = new Vector2(90f + (side * 5f), 45f);
                    var c = new Vector2(90f + (side * reach * 0.95f), 46f);
                    var b = new Vector2(90f + (side * reach), -14f);
                    Vector2 prev = a;
                    for (int i = 1; i <= 16; i++)
                    {
                        float t = i / 16f;
                        float u = 1f - t;
                        Vector2 q = (u * u * a) + (2f * u * t * c) + (t * t * b);
                        float r = Mathf.Lerp(8f, 2.4f, t) + (pass == 0 ? 2f : 0f);
                        pc.FillCapsule(WhiskersCellX + prev.x - WhiskersHeadX0, prev.y - WhiskersHeadY0, WhiskersCellX + q.x - WhiskersHeadX0, q.y - WhiskersHeadY0, r, pass == 0 ? FaunaInk : white);
                        prev = q;
                    }
                }
            }
        }

        private static void FaunaDisc(PixelCanvas pc, float x, float y, float r, Color c)
        {
            pc.FillCircle(x, y, r + 3f, FaunaInk);
            pc.FillCircle(x, y, r, c);
        }
    }
}
