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

        /// <summary>
        /// Borde de la cola y su grosor relativo (S): el borde fino del color de la cola (D-058) en el arte por código; el contorno
        /// de tinta de antes con el PNG del artista.
        /// </summary>
        internal Color TailEdge { get; set; } = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);

        internal float TailEdgeS { get; set; } = 0.03f;

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

        // Bigote blanco (rasgo bigote_blanco; revisión del 2026-10-09): hebras #F4F1EA con un borde de 1 px #BDB8AE, sin
        // contorno de tinta (con tinta se leía como colmillos de morsa).
        private static readonly Color WhiskerWhite = new Color(0xF4 / 255f, 0xF1 / 255f, 0xEA / 255f, 1f);
        private static readonly Color WhiskerEdge = new Color(0xBD / 255f, 0xB8 / 255f, 0xAE / 255f, 1f);
        private static readonly Color WhiskerStrand = new Color(0xCF / 255f, 0xC9 / 255f, 0xBD / 255f, 1f);

        // Pelo largo rojizo de la cabeza y los hombros con la cara roja (rasgo cara_roja, el huapo colorado; revisión del
        // 2026-10-09). Orientativo: lo fija el artista.
        private static readonly Color LongRedHair = new Color(0xC8 / 255f, 0x74 / 255f, 0x3A / 255f, 1f);

        // Iris ámbar (mono nocturno y huapo).
        private static readonly Color AmberIris = new Color(0xC8 / 255f, 0x90 / 255f, 0x2A / 255f, 1f);

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
                art.TailEdge = ToyShading.Tones(art.Tail).Outline;
                art.TailEdgeS = 0.016f;
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
        // de juguete brillante (D-058; guía docs/art/estilo-personajes-3d.md): cada pieza con su volumen en tres tonos, el barniz
        // y el borde fino de su color; los parches de color (la cara, el hocico, el babero) siguen la cúpula de su pieza.
        private static PixelCanvas BuildFaunaCanvas(FaunaSpecies sp, MonkeyArt art, FaunaPalette p)
        {
            var pc = new PixelCanvas(FaunaAtlasPx, FaunaAtlasPx);
            var tones = new FaunaTones(p);
            DrawHead(pc, sp, art, 0f, 128f, tones);
            DrawTorso(pc, sp, 128f, 128f, tones);
            ToyTones limbs = tones.Limbs;
            if (sp.HasTrait("collar_blanco"))
            {
                limbs = tones.Body; // el blanco del sauim es el collar (el babero del torso): los brazos, del color del cuerpo
            }

            // Extremidad: cápsula vertical (un cilindro de juguete) con la mano o el pie más oscuros abajo.
            ToyCapsule(pc, 16f, 100f, 16f, 12f, 11f, limbs, FaunaFurGloss, FaunaEdgePx);
            ToyEllipse(pc, 16f, 13f, 9.5f, 9.5f, ToyShading.Tones(ColorMath.Darken(limbs.Mid, 0.22f)), FaunaFurGloss, 0f);

            // Párpado: elipse del color de la cara con su borde fino.
            Color lid = sp.HasTrait("cara_roja") || sp.HasTrait("mascara_redondeada") || sp.HasTrait("cara_blanca") || sp.HasTrait("ojos_grandes") || sp.HasTrait("orejas_desnudas") ? tones.Face.Mid : tones.Cap.Mid;
            ToyEllipse(pc, 56f, 11f, 20f, 7f, ToyShading.Tones(lid), FaunaFaceGloss, FaunaEdgePx);

            if (sp.HasTrait("bigote_blanco"))
            {
                DrawWhiskers(pc);
            }

            return pc;
        }

        // Juguete de los miquitos: el borde fino coloreado (px de la celda de 128), el barniz de la felpa y de la cara y la
        // oclusión que deja una pieza sobre la de debajo (px).
        private const float FaunaEdgePx = 1.3f;
        private const float FaunaFurGloss = 0.42f;
        private const float FaunaFaceGloss = 0.62f;
        private const float FaunaAoPx = 3f;

        // Iris pardo muy oscuro de los miquitos de ojos pequeños (en degradado, con dos reflejos).
        private static readonly Color FaunaDarkIris = new Color(0x3A / 255f, 0x24 / 255f, 0x16 / 255f, 1f);

        // Los tonos de juguete de la paleta de una especie (una vez por especie, nunca por píxel).
        private readonly struct FaunaTones
        {
            public FaunaTones(FaunaPalette p)
            {
                Body = ToyShading.Tones(Hex(p.Body));
                Limbs = ToyShading.Tones(Hex(p.Limbs));
                Face = ToyShading.Tones(Hex(p.Face), 0.6f);
                Muzzle = ToyShading.Tones(Hex(p.Muzzle), 0.7f);
                Cap = ToyShading.Tones(Hex(p.Cap));
            }

            public ToyTones Body { get; }

            public ToyTones Limbs { get; }

            public ToyTones Face { get; }

            public ToyTones Muzzle { get; }

            public ToyTones Cap { get; }
        }

        private static void DrawHead(PixelCanvas pc, FaunaSpecies sp, MonkeyArt art, float ox, float oy, in FaunaTones tones)
        {
            if (sp.HasTrait("cara_roja"))
            {
                DrawBaldRedHead(pc, art, ox, oy, tones.Face);
                return;
            }

            float cx = ox + 60f;
            float cy = oy + 58f;
            float r = 40f;
            bool fluffy = sp.Skeleton == FaunaSkeleton.C || sp.HasTrait("lanudo");
            bool bigEars = sp.HasTrait("orejas_desnudas") || sp.Skeleton == FaunaSkeleton.A;
            Color face = tones.Face.Mid;

            // Orejas, detrás del cráneo.
            if (bigEars)
            {
                ToyTones ear = sp.HasTrait("orejas_desnudas") ? tones.Face : tones.Cap;
                ToyEllipse(pc, cx - 26f, cy + 30f, 14f, 14f, ear, FaunaFurGloss, FaunaEdgePx);
                ToyEllipse(pc, cx - 6f, cy + 38f, 10f, 10f, ToyShading.Tones(ColorMath.Darken(ear.Mid, 0.12f)), FaunaFurGloss, FaunaEdgePx);
                ToyEllipseOn(pc, cx - 26f, cy + 30f, 7f, 7f, ToyShading.Tones(ColorMath.Darken(ear.Mid, 0.25f), 0.6f), 0.2f, 0f, 0f, cx - 23f, cy + 27f, 16f, 16f, 1.5f);
            }
            else if (!fluffy)
            {
                ToyEllipse(pc, cx - 24f, cy + 26f, 8f, 8f, tones.Face, FaunaFurGloss, FaunaEdgePx);
            }

            if (sp.HasTrait("collar_blanco"))
            {
                // Sauim: el babero blanco asoma bajo la cabeza negra, por delante (va en la pieza de la cabeza, por encima del
                // torso: el brazo de delante tapa el pecho del torso cuando cuelga). El blanco del catálogo va en «extremidades»;
                // con poco contraste, para que el blanco se lea blanco.
                ToyEllipse(pc, cx + 14f, oy + 14f, CollarRxPx, CollarRyPx, ToyShading.Tones(tones.Limbs.Mid, 0.3f), 0.25f, FaunaEdgePx);
            }

            // Pelo mullido o lanudo alrededor (pitécidos y churuco): mechones grandes y redondos de felpa.
            if (fluffy)
            {
                for (int i = 0; i < 11; i++)
                {
                    float a = (95f + (i * 22f)) * Mathf.Deg2Rad;
                    ToyEllipse(pc, cx + (Mathf.Cos(a) * (r + 2f)), cy + (Mathf.Sin(a) * (r + 2f)), 11f, 11f, tones.Body, FaunaFurGloss, FaunaEdgePx, 1.5f);
                }
            }

            // Cráneo con la gorra (el color de arriba de la cabeza), con la oclusión sobre las orejas y los mechones.
            ToyEllipse(pc, cx, cy, r, r, tones.Cap, FaunaFurGloss, FaunaEdgePx, FaunaAoPx);
            float domeR = r * 1.06f;

            Vector2 nearEye = new Vector2(cx + 26f, cy + 6f);
            Vector2 farEye = new Vector2(cx + 9f, cy + 9f);
            float eyeR = 7f; // ojos grandes con brillo: se leen a 40 dp
            float nose = 0f;
            if (sp.HasTrait("mascara_redondeada"))
            {
                // Máscara blanca redondeada y cálida alrededor de los ojos (que no se lea como calavera) y hocico negro.
                ToyEllipseOn(pc, cx + 19f, cy + 7f, 24f, 18f, tones.Face, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 2f);
                ToyEllipseOn(pc, cx + 24f, cy - 7f, 18f, 14f, tones.Face, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 2f);
                ToyEllipseOn(pc, cx + 33f, cy - 12f, 11f, 8.5f, tones.Muzzle, FaunaFaceGloss, 0f, 0f, cx + 30f, cy - 10f, 14f, 11f, 1.5f);
                nose = 1f;
            }
            else if (sp.HasTrait("cara_blanca"))
            {
                // Saquí macho: disco blanco alrededor de una cara oscura pequeña.
                ToyEllipseOn(pc, cx + 16f, cy + 1f, 27f, 29f, tones.Face, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 2f);
                ToyEllipseOn(pc, cx + 29f, cy - 9f, 9f, 8f, tones.Muzzle, FaunaFaceGloss, 0f, 0f, cx + 28f, cy - 8f, 12f, 11f, 2f);
            }
            else if (sp.HasTrait("ojos_grandes"))
            {
                // Mono nocturno: ojos enormes (radio × 1,8) con el iris ámbar, manchas blancas sobre los ojos entre tres rayas
                // oscuras y un anillo pardo alrededor que separa el blanco de la cara clara.
                eyeR = 7f * 1.8f;
                nearEye = new Vector2(cx + 30f, cy + 2f);
                farEye = new Vector2(cx + 4f, cy + 6f);
                ToyEllipseOn(pc, cx + 16f, cy + 1f, 30f, 29f, tones.Face, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 2f);
                Color eyeRing = ColorMath.WithAlpha(tones.Muzzle.Mid, 0.85f);
                pc.FillCircle(nearEye.x, nearEye.y, eyeR + 3.2f, eyeRing);
                pc.FillCircle(farEye.x, farEye.y, (eyeR * 0.85f) + 3f, eyeRing);
                ToyTones white = ToyShading.Tones(new Color(0.98f, 0.97f, 0.94f, 1f), 0.5f);
                ToyEllipseOn(pc, nearEye.x + 1f, nearEye.y + eyeR + 6f, eyeR * 0.85f, 4.6f, white, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 1.5f);
                ToyEllipseOn(pc, farEye.x, farEye.y + (eyeR * 0.85f) + 6f, eyeR * 0.72f, 4.2f, white, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 1.5f);
                ToyCapsule(pc, cx + 17f, cy + 22f, cx + 17f, cy + 40f, 3.2f, tones.Cap, FaunaFurGloss, 0f);
                ToyCapsule(pc, cx - 6f, cy + 20f, cx - 12f, cy + 34f, 2.8f, tones.Cap, FaunaFurGloss, 0f);
                ToyCapsule(pc, cx + 42f, cy + 18f, cx + 44f, cy + 30f, 2.8f, tones.Cap, FaunaFurGloss, 0f);
                ToyEllipseOn(pc, cx + 33f, cy - 15f, 7f, 5f, tones.Muzzle, FaunaFaceGloss, 0f, 0f, cx + 32f, cy - 14f, 9f, 7f, 2f);
            }
            else
            {
                // Cara desnuda (sagui: color carne; sauim, churuco, maquisapa: negra; pichico: oscura).
                ToyEllipseOn(pc, cx + 17f, cy + 1f, 22f, 25f, tones.Face, FaunaFaceGloss, 0f, 0f, cx, cy, domeR, domeR, 2f);
                ToyEllipseOn(pc, cx + 30f, cy - 10f, 8f, 6f, tones.Muzzle, FaunaFaceGloss, 0f, 0f, cx + 29f, cy - 9f, 10f, 8f, 2f);
            }

            // Ojos de juguete (3/4 mirando a la derecha) con el iris en degradado y dos reflejos; en caras oscuras, un anillo
            // claro para que se lean a 40 dp.
            bool darkFace = ColorMath.RelativeLuminance(face) < 0.08f;
            Color ring = ColorMath.Lighten(face, 0.45f);
            if (darkFace)
            {
                pc.FillCircle(nearEye.x, nearEye.y, eyeR + 2.2f, ring);
                pc.FillCircle(farEye.x, farEye.y, (eyeR * 0.85f) + 2f, ring);
            }

            Color iris = sp.HasTrait("ojos_grandes") ? AmberIris : FaunaDarkIris;
            DrawToyEye(pc, nearEye.x, nearEye.y, eyeR, iris);
            DrawToyEye(pc, farEye.x, farEye.y, eyeR * 0.85f, iris);
            if (sp.Lighting == FaunaLighting.Night)
            {
                pc.FillCircle(nearEye.x + (eyeR * 0.3f), nearEye.y - (eyeR * 0.3f), 1.4f, new Color(0.85f, 0.92f, 1f, 0.9f)); // destello de luna
            }

            if (nose > 0f)
            {
                pc.FillCircle(cx + 39f, cy - 10f, 2f, tones.Muzzle.Outline);
            }

            art.EyelidOffsetS = new Vector2(((nearEye.x + farEye.x) * 0.5f) - (ox + 64f), ((nearEye.y + farEye.y) * 0.5f) - (oy + 64f)) / FaunaPxPerS * (art.HeadS / 0.6f);
            art.EyelidSizeS = new Vector2((Mathf.Abs(nearEye.x - farEye.x) + (eyeR * 2.6f)) / FaunaPxPerS, eyeR * 2.4f / FaunaPxPerS) * (art.HeadS / 0.6f);
        }

        private static void DrawTorso(PixelCanvas pc, FaunaSpecies sp, float ox, float oy, in FaunaTones tones)
        {
            float cx = ox + 64f;
            float cy = oy + 62f;
            bool woolly = sp.HasTrait("lanudo") || sp.Skeleton == FaunaSkeleton.C;
            bool longHair = sp.HasTrait("cara_roja");
            ToyTones fur = longHair ? LongRedHairToy : tones.Body;
            if (woolly)
            {
                // Lana: mechones grandes y redondos alrededor del cuerpo (felpa, no pelos sueltos).
                for (int i = 0; i < 14; i++)
                {
                    float a = i * (Mathf.PI * 2f / 14f);
                    ToyEllipse(pc, cx + (Mathf.Cos(a) * 50f), cy + (Mathf.Sin(a) * 40f), 9f, 9f, fur, FaunaFurGloss, FaunaEdgePx);
                }
            }

            ToyEllipse(pc, cx, cy, 51.5f, 41.5f, tones.Body, FaunaFurGloss, FaunaEdgePx, woolly ? FaunaAoPx : 0f);
            float dx = 51.5f * 1.06f;
            float dy = 41.5f * 1.06f;
            if (longHair)
            {
                // Huapo: mechones de pelo largo rojizo que cuelgan del lomo y de los hombros (mirando a la derecha: el lomo a la
                // izquierda, el hombro arriba a la derecha), por encima del cuerpo y dentro de su celda.
                HairLock(pc, cx - 40f, cy + 22f, cx - 50f, cy + 2f, cx - 52f, cy - 24f, 7f);
                HairLock(pc, cx - 14f, cy + 38f, cx - 26f, cy + 20f, cx - 30f, cy - 8f, 7f);
                HairLock(pc, cx + 14f, cy + 40f, cx + 6f, cy + 22f, cx + 4f, cy - 6f, 6.5f);
                HairLock(pc, cx + 36f, cy + 30f, cx + 44f, cy + 10f, cx + 46f, cy - 16f, 6f);
            }

            if (sp.HasTrait("collar_blanco"))
            {
                // Sauim: babero blanco elíptico de 0,45 del ancho del torso, en la mitad de arriba del pecho, que sigue al de la
                // pieza de la cabeza (más arriba, la cabeza lo tapa; el blanco del catálogo va en «extremidades» y los brazos
                // salen del color del cuerpo). Sigue la cúpula del torso, con poco contraste: un blanco que se lee blanco.
                float bibRx = 0.45f * 51.5f;
                ToyEllipseOn(pc, cx + 12f, cy + 2f, bibRx, 20f, ToyShading.Tones(tones.Limbs.Mid, 0.3f), 0.25f, FaunaEdgePx, 0f, cx + 12f, cy + 6f, bibRx * 1.8f, 34f, 0f);
            }
            else if (sp.HasTrait("pecho_rojizo"))
            {
                ToyEllipseOn(pc, cx + 28f, cy - 4f, 22f, 30f, tones.Limbs, FaunaFurGloss, 0f, 0f, cx, cy, dx, dy, 3f);
            }
            else if (sp.Skeleton == FaunaSkeleton.B)
            {
                // Vientre claro.
                ToyEllipseOn(pc, cx + 24f, cy - 12f, 24f, 22f, ToyShading.Tones(ColorMath.Lighten(tones.Body.Mid, 0.3f), 0.6f), FaunaFurGloss, 0f, 0f, cx, cy, dx, dy, 6f);
            }

            if (sp.HasTrait("cria_espalda"))
            {
                // El padre carga a la cría en la espalda.
                ToyEllipse(pc, cx - 30f, cy + 38f, 14.5f, 14.5f, tones.Body, FaunaFurGloss, FaunaEdgePx, 2f);
                ToyEllipseOn(pc, cx - 24f, cy + 37f, 8f, 8f, tones.Face, FaunaFaceGloss, 0f, 0f, cx - 30f, cy + 38f, 15.4f, 15.4f, 1f);
                pc.FillCircle(cx - 21f, cy + 39f, 2.6f, new Color(0.16f, 0.1f, 0.06f, 1f));
                pc.FillCircle(cx - 27f, cy + 40f, 2.2f, new Color(0.16f, 0.1f, 0.06f, 1f));
                pc.FillCircle(cx - 21.8f, cy + 39.9f, 0.8f, Color.white);
                pc.FillCircle(cx - 27.6f, cy + 40.7f, 0.7f, Color.white);
            }
        }

        // Huapo colorado (rasgo cara_roja; guion §4.2: «cara roja, señal de salud, nunca burla»; revisión del 2026-10-09): la
        // cabeza calva es un óvalo vertical, rojo solo en la cara y el cuero cabelludo (el color «cara» del catálogo), sin nariz
        // saliente (solo dos fosas a ras de la cara) y con ojos pequeños de color ámbar; 7 mechones de pelo largo rojizo le
        // salen de la nuca y de debajo de la mandíbula y cuelgan hacia los hombros (los del lomo van en el torso). Todo dentro
        // de la celda de la cabeza (0..128). De juguete: el óvalo, de vinilo lacado (el más brillante de los miquitos).
        private static void DrawBaldRedHead(PixelCanvas pc, MonkeyArt art, float ox, float oy, in ToyTones faceTones)
        {
            float cx = ox + 62f;
            float cy = oy + 66f;
            const float rx = 25f;
            const float ry = 33f;
            Color face = faceTones.Mid;

            // Mechones de la nuca a la mandíbula (ángulo en grados desde +x, con y hacia arriba; la cara, a la derecha, y el
            // cuero cabelludo quedan calvos): salen del borde del óvalo, se abren un poco hacia fuera y cuelgan.
            for (int i = 0; i < 7; i++)
            {
                float deg = 140f + (i * 25f);
                float a = deg * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var root = new Vector2(cx + (dir.x * (rx - 4f)), cy + (dir.y * (ry - 4f)));
                float len = i == 0 || i == 6 ? 20f : 26f;
                Vector2 mid = root + (dir * (len * 0.45f)) + new Vector2(0f, -len * 0.2f);
                Vector2 tip = root + (dir * (len * 0.5f)) + new Vector2(dir.x * 4f, -len * 0.85f);
                tip.x = Mathf.Clamp(tip.x, ox + 8f, ox + 120f);
                tip.y = Mathf.Max(tip.y, oy + 8f);
                HairLock(pc, root.x, root.y, mid.x, mid.y, tip.x, tip.y, 6.5f);
            }

            // Óvalo vertical calvo de vinilo lacado, con su borde fino y la oclusión sobre los mechones.
            ToyEllipse(pc, cx, cy, rx, ry, faceTones, 0.85f, FaunaEdgePx, FaunaAoPx);

            // Ojos pequeños de color ámbar (3/4 mirando a la derecha), de juguete, y dos fosas nasales a ras.
            var nearEye = new Vector2(cx + 13f, cy + 5f);
            var farEye = new Vector2(cx - 1f, cy + 6f);
            const float eyeR = 4.6f;
            var socket = ColorMath.Darken(face, 0.2f);
            pc.FillCircle(nearEye.x, nearEye.y, eyeR + 1.4f, socket);
            pc.FillCircle(farEye.x, farEye.y, (eyeR * 0.85f) + 1.3f, socket);
            DrawToyEye(pc, nearEye.x, nearEye.y, eyeR, AmberIris);
            DrawToyEye(pc, farEye.x, farEye.y, eyeR * 0.85f, AmberIris);
            Color nostril = ColorMath.WithAlpha(ColorMath.Darken(face, 0.45f), 0.85f);
            pc.FillEllipseSoft(cx + 17f, cy - 10f, 1.6f, 1.1f, 0.8f, nostril);
            pc.FillEllipseSoft(cx + 12f, cy - 10.5f, 1.4f, 1f, 0.8f, nostril);

            art.EyelidOffsetS = new Vector2(((nearEye.x + farEye.x) * 0.5f) - (ox + 64f), ((nearEye.y + farEye.y) * 0.5f) - (oy + 64f)) / FaunaPxPerS * (art.HeadS / 0.6f);
            art.EyelidSizeS = new Vector2((Mathf.Abs(nearEye.x - farEye.x) + (eyeR * 2.6f)) / FaunaPxPerS, eyeR * 2.4f / FaunaPxPerS) * (art.HeadS / 0.6f);
        }

        // Mechón de pelo largo: cápsulas que se afinan a lo largo de una Bézier cuadrática (raíz, control, punta) de
        // radio <paramref name="r0"/> a un cuarto de él: el borde fino del color del pelo, el relleno de juguete (cada tramo, un
        // cilindro con su barniz) y una hebra oscura apenas marcada.
        private static void HairLock(PixelCanvas pc, float x0, float y0, float x1, float y1, float x2, float y2, float r0)
        {
            const int steps = 12;
            ToyTones hair = LongRedHairToy;
            Color dark = ColorMath.WithAlpha(hair.Shadow, 0.6f);
            for (int pass = 0; pass < 3; pass++)
            {
                float px = x0;
                float py = y0;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    float u = 1f - t;
                    float qx = (u * u * x0) + (2f * u * t * x1) + (t * t * x2);
                    float qy = (u * u * y0) + (2f * u * t * y1) + (t * t * y2);
                    float r = Mathf.Lerp(r0, r0 * 0.25f, t);
                    switch (pass)
                    {
                        case 0:
                            pc.FillCapsule(px, py, qx, qy, r + FaunaEdgePx, hair.Outline);
                            break;
                        case 1:
                            ToyCapsule(pc, px, py, qx, qy, r, hair, FaunaFurGloss, 0f);
                            break;
                        default:
                            pc.FillCapsule(px - (r * 0.35f), py, qx - (r * 0.35f), qy, Mathf.Max(0.5f, r * 0.16f), dark); // hebra
                            break;
                    }

                    px = qx;
                    py = qy;
                }
            }
        }

        // Los tonos del pelo largo rojizo, en su propia clase (se inicializan al pintar, con LongRedHair ya listo).
        private static ToyTones LongRedHairToy => FaunaToy.LongRedHair;

        private static class FaunaToy
        {
            public static readonly ToyTones LongRedHair = ToyShading.Tones(ProceduralArt.LongRedHair);
        }

        // Bigote del pichico emperador (guion §4.2: «bigote blanco largo», que pasa de los hombros; revisión del 2026-10-09):
        // dos mechones simétricos del labio superior hacia abajo y hacia fuera, de 4 hebras cada uno, que se afinan de unos
        // 2,5 dp a 0,5 dp (≈ 6,5 px por dp en la celda de la cabeza), blancos #F4F1EA con un borde de 1 px #BDB8AE y sin tinta.
        // Coordenadas de la celda de la cabeza (centro del cráneo en (60, 58), hocico en (90, 48)), llevadas a la del bigote.
        private static void DrawWhiskers(PixelCanvas pc)
        {
            const int steps = 16;
            const int strands = 4;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    // Raíz en el labio, bajo el hocico; baja y se abre, con la punta algo hacia fuera (pasa los hombros: y ≈ 20).
                    var a = new Vector2(90f + (side * 4f), 43f);
                    var c1 = new Vector2(90f + (side * 15f), 30f);
                    var c2 = new Vector2(90f + (side * 24f), 6f);
                    var b = new Vector2(90f + (side * 33f), -13f);
                    for (int j = 0; j < strands; j++)
                    {
                        float lane = j - ((strands - 1) * 0.5f); // −1,5 … 1,5
                        float end = 1f - (0.08f * Mathf.Abs(lane)) - (side > 0 && j == 0 ? 0.05f : 0f); // puntas escalonadas
                        Vector2 prev = Vector2.zero;
                        for (int i = 0; i <= steps; i++)
                        {
                            float t = end * i / steps;
                            Vector2 q = Cubic(a, c1, c2, b, t);
                            Vector2 d = Cubic(a, c1, c2, b, Mathf.Min(1f, t + 0.02f)) - Cubic(a, c1, c2, b, Mathf.Max(0f, t - 0.02f));
                            var n = new Vector2(-d.y, d.x).normalized;
                            // Las hebras se juntan hacia la punta sin cerrarse del todo (se abren un poco: pelo, no un colmillo) y
                            // ondulan apenas.
                            float spread = Mathf.Lerp(3.4f, 1.1f, t);
                            float wave = 0.7f * t * Mathf.Sin((t * 3f * Mathf.PI) + (lane * 1.3f));
                            q += n * ((lane * spread) + wave);
                            if (i > 0)
                            {
                                float r = Mathf.Lerp(2.5f, 0.6f, t);
                                float qx = WhiskersCellX + q.x - WhiskersHeadX0;
                                float qy = q.y - WhiskersHeadY0;
                                float px = WhiskersCellX + prev.x - WhiskersHeadX0;
                                float py = prev.y - WhiskersHeadY0;
                                if (pass == 0)
                                {
                                    pc.FillCapsule(px, py, qx, qy, r + 1f, WhiskerEdge);
                                }
                                else if (pass == 1)
                                {
                                    pc.FillCapsule(px, py, qx, qy, r, WhiskerWhite);
                                }
                                else if (j < strands - 1 && t > 0.12f && t < 0.85f)
                                {
                                    // Raya entre una hebra y la siguiente: se leen los pelos, no una medialuna maciza.
                                    Vector2 gap = n * (spread * 0.5f);
                                    pc.FillCapsule(px + gap.x, py + gap.y, qx + gap.x, qy + gap.y, 0.75f, WhiskerStrand);
                                }
                            }

                            prev = q;
                        }
                    }
                }
            }
        }

        private static Vector2 Cubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1f - t;
            return (u * u * u * a) + (3f * u * u * t * b) + (3f * u * t * t * c) + (t * t * t * d);
        }
    }
}
