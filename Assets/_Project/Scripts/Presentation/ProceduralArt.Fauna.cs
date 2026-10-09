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

        /// <summary>Las piezas salen del PNG del artista.</summary>
        public bool Painted { get; internal set; }
    }

    public sealed partial class ProceduralArt
    {
        private const int FaunaAtlasPx = 256;
        private const float FaunaPxPerS = 128f / 0.6f; // la celda de la cabeza (128 px) mide 0,6 S
        private static readonly Color FaunaInk = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);

        private readonly Dictionary<string, MonkeyArt> _monkeys = new Dictionary<string, MonkeyArt>();

        /// <summary>Tiempo de generación de la última especie (≤ 10 ms; 0 si salió de la caché o del PNG).</summary>
        public double FaunaArtMs { get; private set; }

        /// <summary>
        /// Piezas de la especie <paramref name="species"/>, generadas de forma perezosa la primera vez (≤ 10 ms) y guardadas en
        /// caché. Si existe <c>fauna_&lt;id&gt;_partes</c> (rejilla de 4 × 4 celdas de 256 px: cabeza, torso, extremidad y
        /// párpado en la primera fila), se usan sus celdas.
        /// </summary>
        public MonkeyArt MonkeyArtFor(FaunaSpecies species)
        {
            if (_monkeys.TryGetValue(species.Id, out MonkeyArt? cached))
            {
                FaunaArtMs = 0;
                return cached;
            }

            Stopwatch watch = Stopwatch.StartNew();
            var art = new MonkeyArt(species.Id, species.Skeleton);
            FaunaPalette p = species.Palette;
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
                    art.HeadS = 0.48f;
                    art.TailWidthS = 0.07f;
                    art.TailCurls = true;
                    art.LimbScale = 1.35f;
                    art.LimbWidthS = 0.11f;
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
                PixelCanvas pc = BuildFaunaCanvas(species, art);
                Texture2D tex = pc.ToTexture("fauna_" + species.Id);
                _owned.Add(tex);
                art.Head = SubSprite(tex, new Rect(0, 128, 128, 128), "head", 0f);
                art.Torso = SubSprite(tex, new Rect(128, 128, 128, 128), "torso", 0f);
                art.Limb = SubSprite(tex, new Rect(0, 0, 32, 112), "limb", 24f);
                art.Eyelid = SubSprite(tex, new Rect(32, 0, 48, 20), "eyelid", 0f);
            }

            Sprite? portrait = ArtSlots.Find(species.PortraitSlot);
            art.Portrait = portrait != null ? portrait : art.Head;
            _monkeys[species.Id] = art;
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
        private static PixelCanvas BuildFaunaCanvas(FaunaSpecies sp, MonkeyArt art)
        {
            var pc = new PixelCanvas(FaunaAtlasPx, FaunaAtlasPx);
            FaunaPalette p = sp.Palette;
            Color body = Hex(p.Body);
            Color limbs = Hex(p.Limbs);
            Color face = Hex(p.Face);
            Color muzzle = Hex(p.Muzzle);
            Color cap = Hex(p.Cap);
            DrawHead(pc, sp, art, 0f, 128f, body, face, muzzle, cap);
            DrawTorso(pc, sp, 128f, 128f, body, limbs, face);

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

        private static void DrawHead(PixelCanvas pc, FaunaSpecies sp, MonkeyArt art, float ox, float oy, Color body, Color face, Color muzzle, Color cap)
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
                // Mono nocturno: cara clara, ojos enormes y tres rayas oscuras en la frente.
                pc.FillEllipseSoft(cx + 14f, cy + 2f, 27f, 27f, 2f, face);
                pc.FillCapsule(cx + 14f, cy + 22f, cx + 13f, cy + 38f, 3.2f, cap);
                pc.FillCapsule(cx + 3f, cy + 18f, cx - 6f, cy + 34f, 2.8f, cap);
                pc.FillCapsule(cx + 26f, cy + 17f, cx + 33f, cy + 31f, 2.8f, cap);
                pc.FillEllipseSoft(cx + 30f, cy - 12f, 7f, 5f, 2f, muzzle);
                eyeR = 10.5f;
                nearEye = new Vector2(cx + 27f, cy + 4f);
                farEye = new Vector2(cx + 7f, cy + 7f);
            }
            else
            {
                // Cara desnuda (sagui: color carne; sauim, churuco, maquisapa: negra; pichico: oscura).
                pc.FillEllipseSoft(cx + 17f, cy + 1f, 22f, 25f, 2f, face);
                pc.FillEllipseSoft(cx + 30f, cy - 10f, 8f, 6f, 2f, muzzle);
            }

            if (sp.HasTrait("bigote_blanco"))
            {
                // Pichico emperador: bigote blanco largo que pasa de los hombros.
                Color m = muzzle;
                pc.FillCapsule(cx + 32f, cy - 12f, cx + 48f, cy - 34f, 5f, FaunaInk);
                pc.FillCapsule(cx + 48f, cy - 34f, cx + 50f, cy - 52f, 4.5f, FaunaInk);
                pc.FillCapsule(cx + 24f, cy - 14f, cx + 30f, cy - 40f, 5f, FaunaInk);
                pc.FillCapsule(cx + 30f, cy - 40f, cx + 26f, cy - 56f, 4.5f, FaunaInk);
                pc.FillCapsule(cx + 32f, cy - 12f, cx + 48f, cy - 34f, 3.4f, m);
                pc.FillCapsule(cx + 48f, cy - 34f, cx + 50f, cy - 52f, 3f, m);
                pc.FillCapsule(cx + 24f, cy - 14f, cx + 30f, cy - 40f, 3.4f, m);
                pc.FillCapsule(cx + 30f, cy - 40f, cx + 26f, cy - 56f, 3f, m);
            }

            // Ojos (3/4 mirando a la derecha) con brillo; en caras oscuras, un anillo claro para que se lean a 40 dp.
            bool darkFace = ColorMath.RelativeLuminance(face) < 0.08f;
            Color ring = ColorMath.Lighten(face, 0.45f);
            if (darkFace)
            {
                pc.FillCircle(nearEye.x, nearEye.y, eyeR + 2.2f, ring);
                pc.FillCircle(farEye.x, farEye.y, (eyeR * 0.85f) + 2f, ring);
            }

            Color iris = new Color(0x2A / 255f, 0x1A / 255f, 0x10 / 255f, 1f);
            pc.FillCircle(nearEye.x, nearEye.y, eyeR, iris);
            pc.FillCircle(farEye.x, farEye.y, eyeR * 0.85f, iris);
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
                pc.FillEllipseSoft(cx + 30f, cy + 14f, 22f, 28f, 2f, limbs); // pecho y hombros blancos (sauim)
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

        private static void FaunaDisc(PixelCanvas pc, float x, float y, float r, Color c)
        {
            pc.FillCircle(x, y, r + 3f, FaunaInk);
            pc.FillCircle(x, y, r, c);
        }
    }
}
