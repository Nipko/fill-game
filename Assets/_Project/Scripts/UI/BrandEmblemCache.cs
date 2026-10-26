#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AF.Presentation;
using Unity.Collections;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Caché de disco de la escena de la marca rasterizada (<see cref="BrandEmblemArt"/>; jornada 3 de la prueba completa, 2026-10-26,
    /// observaciones 1 y 2 del añadido de D-051): sus piezas tal cual (RGBA32) en un solo archivo de
    /// <see cref="Application.temporaryCachePath"/> (la caché de la app: fuera de la copia de seguridad de Android, y el sistema puede
    /// vaciarla), para que, desde la segunda apertura, la escena se lea en vez de dibujarse antes del primer fotograma.
    /// <list type="bullet">
    /// <item>La clave (<see cref="KeyFor"/>) lleva el lienzo (ancho, alto y densidad), la huella de los datos generados y del dibujo
    /// (<see cref="BrandRiverScene.DataHash"/>) y el <see cref="Application.buildGUID"/> (cada build la cambia): con otra pantalla,
    /// otros datos u otro build no sirve y se vuelve a dibujar.</item>
    /// <item>Un archivo con otra clave, de otro formato, corto o largo, se descarta (se borra) y se dibuja: nunca se sirve a
    /// medias.</item>
    /// <item>Se escribe en segundo plano tras dibujar (<see cref="SaveInBackground"/>; a un temporal y luego en su sitio), sin
    /// alargar la carga; un fallo de disco solo deja un aviso.</item>
    /// <item>En el Editor está apagada (se itera el arte), salvo con <see cref="DirectoryOverride"/> (los tests).</item>
    /// </list>
    /// </summary>
    internal static class BrandEmblemCache
    {
        /// <summary>Nombre del archivo, dentro de su carpeta.</summary>
        public const string FileName = "af_brand_emblem.bin";

        // "AFBE", formato 1.
        private const int Magic = 0x45424641;
        private const int Format = 1;

        private static Task? s_pending;

        /// <summary>
        /// Carpeta de los tests (null = la de siempre: <c>temporaryCachePath/afart</c> fuera del Editor, y apagada en él). Los tests
        /// que la cambian la vuelven a null en su TearDown.
        /// </summary>
        public static string? DirectoryOverride { get; set; }

        /// <summary>Solo para los tests: sustituye a <see cref="Application.buildGUID"/> en la clave (null = el del build).</summary>
        public static string? BuildOverride { get; set; }

        /// <summary>Solo para los tests: las texturas leídas siguen legibles (para comparar sus píxeles).</summary>
        internal static bool KeepReadableForTests { get; set; }

        /// <summary>La caché se usa: fuera del Editor o con <see cref="DirectoryOverride"/>.</summary>
        public static bool Active => DirectoryOverride != null || !Application.isEditor;

        /// <summary>Ruta del archivo (null si está apagada).</summary>
        public static string? FilePath
        {
            get
            {
                if (DirectoryOverride != null)
                {
                    return Path.Combine(DirectoryOverride, FileName);
                }

                return Application.isEditor ? null : Path.Combine(Application.temporaryCachePath, "afart", FileName);
            }
        }

        /// <summary>La escritura en segundo plano que sigue en curso (null si no hay): los tests la esperan.</summary>
        public static Task? PendingWrite => s_pending != null && !s_pending.IsCompleted ? s_pending : null;

        /// <summary>
        /// La clave de un lienzo de <paramref name="width"/> × <paramref name="height"/> dp a <paramref name="pxPerDp"/> px por dp
        /// con los datos de hoy: <c>w…h…p…d…b…</c>.
        /// </summary>
        public static string KeyFor(float width, float height, float pxPerDp)
        {
            var sb = new StringBuilder(96);
            sb.Append('w').Append(Mathf.RoundToInt(width * 100f).ToString(CultureInfo.InvariantCulture));
            sb.Append('h').Append(Mathf.RoundToInt(height * 100f).ToString(CultureInfo.InvariantCulture));
            sb.Append('p').Append(Mathf.RoundToInt(pxPerDp * 1000f).ToString(CultureInfo.InvariantCulture));
            sb.Append('d').Append(BrandRiverScene.DataHash.ToString("x16", CultureInfo.InvariantCulture));
            sb.Append('b').Append(BuildOverride ?? Application.buildGUID ?? string.Empty);
            return sb.ToString();
        }

        /// <summary>
        /// Lee las <paramref name="textures"/> y sus rectángulos (<paramref name="rects"/>, dp) de la clave <paramref name="key"/>.
        /// False (sin tocar nada de lo de fuera) si no está, si es de otra clave o si el archivo no está entero: entonces lo borra.
        /// </summary>
        public static bool TryLoad(string key, Texture2D?[] textures, Rect[] rects)
        {
            string? path = FilePath;
            if (path == null || PendingWrite != null)
            {
                return false;
            }

            byte[] raw;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                raw = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.BrandEmblemCache] no se pudo leer la caché: " + e.Message);
                return false;
            }

            int count = textures.Length;
            var made = new Texture2D?[count];
            var placed = new Rect[count];
            bool ok = false;
            try
            {
                ok = Parse(raw, key, made, placed);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.BrandEmblemCache] caché ilegible: " + e.Message);
                ok = false;
            }

            if (!ok)
            {
                for (int i = 0; i < count; i++)
                {
                    if (made[i] != null)
                    {
                        UnityEngine.Object.Destroy(made[i]);
                    }
                }

                Discard(path);
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                textures[i] = made[i];
                rects[i] = placed[i];
            }

            return true;
        }

        /// <summary>
        /// Guarda en segundo plano las piezas recién dibujadas (<paramref name="pixels"/> de <paramref name="sizes"/> px, en
        /// <paramref name="rects"/> dp) con la clave <paramref name="key"/>. No hace nada si está apagada, si ya hay una escritura en
        /// curso o si falta alguna pieza. Los arreglos pasan a ser de la caché: quien llama no los toca más.
        /// </summary>
        public static void SaveInBackground(string key, Color32[]?[] pixels, Vector2Int[] sizes, Rect[] rects)
        {
            string? path = FilePath;
            if (path == null || PendingWrite != null)
            {
                return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] == null || pixels[i]!.Length != sizes[i].x * sizes[i].y)
                {
                    return;
                }
            }

            var px = (Color32[]?[])pixels.Clone();
            var sz = (Vector2Int[])sizes.Clone();
            var rc = (Rect[])rects.Clone();
            s_pending = Task.Run(() => Write(path, key, px, sz, rc));
        }

        /// <summary>Escribe el archivo (a un temporal y luego en su sitio). Público para los tests; el juego usa <see cref="SaveInBackground"/>.</summary>
        public static void Write(string path, string key, Color32[]?[] pixels, Vector2Int[] sizes, Rect[] rects)
        {
            string tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
                using (var bw = new BinaryWriter(fs, Encoding.UTF8))
                {
                    bw.Write(Magic);
                    bw.Write(Format);
                    bw.Write(key);
                    bw.Write(pixels.Length);
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Rect r = rects[i];
                        bw.Write(r.x);
                        bw.Write(r.y);
                        bw.Write(r.width);
                        bw.Write(r.height);
                        bw.Write(sizes[i].x);
                        bw.Write(sizes[i].y);
                        bw.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<Color32>(pixels[i])));
                    }
                }

                if (File.Exists(path))
                {
                    File.Replace(tmp, path, null); // sobre el destino, sin un instante sin archivo
                }
                else
                {
                    File.Move(tmp, path);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.BrandEmblemCache] no se pudo guardar la caché: " + e.Message);
                try
                {
                    if (File.Exists(tmp))
                    {
                        File.Delete(tmp);
                    }
                }
                catch (Exception)
                {
                    // nada más que hacer: el temporal se sobrescribe la próxima vez
                }
            }
        }

        private static bool Parse(byte[] raw, string key, Texture2D?[] made, Rect[] placed)
        {
            using var ms = new MemoryStream(raw, false);
            using var br = new BinaryReader(ms, Encoding.UTF8);
            if (raw.Length < 12 || br.ReadInt32() != Magic || br.ReadInt32() != Format)
            {
                return false;
            }

            if (!string.Equals(br.ReadString(), key, StringComparison.Ordinal) || br.ReadInt32() != made.Length)
            {
                return false;
            }

            for (int i = 0; i < made.Length; i++)
            {
                if (raw.Length - ms.Position < 24)
                {
                    return false;
                }

                var r = new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                int w = br.ReadInt32();
                int h = br.ReadInt32();
                long bytes = (long)w * h * 4L;
                if (w <= 0 || h <= 0 || w > 8192 || h > 8192 || raw.Length - ms.Position < bytes)
                {
                    return false;
                }

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "AF_BrandCache" + i,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                made[i] = tex;
                NativeArray<byte> dst = tex.GetPixelData<byte>(0);
                NativeArray<byte>.Copy(raw, (int)ms.Position, dst, 0, (int)bytes);
                tex.Apply(false, !KeepReadableForTests);
                ms.Position += bytes;
                placed[i] = r;
            }

            return ms.Position == raw.Length; // ni corto ni largo
        }

        private static void Discard(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                // se sobrescribe al guardar la próxima vez
            }
        }
    }
}
