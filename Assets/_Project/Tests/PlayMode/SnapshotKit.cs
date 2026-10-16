#nullable enable
using System.Collections;
using System.IO;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using UnityEngine;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Utilidades de las capturas visuales de revisión (<c>[Explicit]</c>; las usan <see cref="VisualSnapshotTests"/> y los
    /// archivos de capturas de cada tarea): una cámara y una textura de 1080 × 2340 px (20:9 aprox., 360 × 780 dp) con zona
    /// segura, el ambiente congelado (guía visual v2, 9) y los PNG en <c>Logs/snapshots/&lt;idioma&gt;/</c> (ignorada por git;
    /// propuesta de idiomas §3.8; tarea L2b): <c>es-419</c> por defecto y, con <see cref="Prepare(string)"/>, el idioma de la
    /// captura (<c>en-XA</c> para la pseudolocalización). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// &lt;Fixture&gt;</c> sin <c>-nographics</c>. Quien llama a <see cref="Prepare()"/> llama a <see cref="Release"/> en su
    /// <c>[TearDown]</c>.
    /// </summary>
    public static class SnapshotKit
    {
        /// <summary>Ancho de la captura en píxeles.</summary>
        public const int Width = 1080;

        /// <summary>Alto de la captura en píxeles.</summary>
        public const int Height = 2340;

        private static Camera? s_camera;
        private static RenderTexture? s_target;
        private static string s_locale = LocLocale.SourceLocale;

        /// <summary>Cámara de las capturas (null antes de <see cref="Prepare()"/> y después de <see cref="Release"/>).</summary>
        public static Camera? CaptureCamera => s_camera;

        /// <summary>Idioma de las capturas (el de <see cref="Prepare(string)"/>; es-419 por defecto).</summary>
        public static string Locale => s_locale;

        /// <summary>
        /// Cámara y textura de las capturas (1080 × 2340 px, 20:9 aprox., 360 × 780 dp) con zona segura y el ambiente congelado,
        /// en es-419; devuelve la carpeta de las capturas (<c>Logs/snapshots/es-419/</c>).
        /// </summary>
        public static string Prepare()
        {
            return Prepare(LocLocale.SourceLocale);
        }

        /// <summary>
        /// Como <see cref="Prepare()"/>, con las capturas en <paramref name="locale"/> (D-018; propuesta de idiomas §3.8): fija el
        /// idioma de desarrollo de la escena (<see cref="LaunchOptions.LanguageOverride"/>, el de <c>af_lang</c>) y el de
        /// <see cref="Loc"/>, y devuelve <c>Logs/snapshots/&lt;locale&gt;/</c>. Con en-XA todo texto de la tabla sale entre
        /// corchetes. Quien llame a <see cref="LaunchOptions.Reset"/> después lo vuelve a es-419 en las escenas que cargue.
        /// </summary>
        public static string Prepare(string locale)
        {
            s_locale = locale;
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }

            GameScreen.SimulatedScreen = new Vector4(Width, Height, 96f, 48f); // 32 dp de cutout arriba, 16 dp de gestos abajo
            AmbientLayer.Frozen = true;
            s_target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("SnapshotCamera");
            Object.DontDestroyOnLoad(camGo);
            s_camera = camGo.AddComponent<Camera>();
            s_camera.targetTexture = s_target;
            s_camera.clearFlags = CameraClearFlags.SolidColor;
            s_camera.backgroundColor = Color.black;
            s_camera.orthographic = true;
            s_camera.enabled = false;
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots", locale);
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Deshace <see cref="Prepare()"/>: pantalla real, ambiente vivo, sin cámara ni textura y los textos otra vez en es-419
        /// (<see cref="LocSetUpFixture.ConfigureEs419"/>).
        /// </summary>
        public static void Release()
        {
            GameScreen.SimulatedScreen = null;
            AmbientLayer.Frozen = false;
            if (s_locale != LocLocale.SourceLocale || (Loc.IsConfigured && Loc.Locale != LocLocale.SourceLocale))
            {
                LaunchOptions.LanguageOverride = null;
                LocSetUpFixture.ConfigureEs419();
            }

            s_locale = LocLocale.SourceLocale;
            if (s_camera != null)
            {
                Object.Destroy(s_camera.gameObject);
            }

            if (s_target != null)
            {
                s_target.Release();
                Object.Destroy(s_target);
            }

            s_camera = null;
            s_target = null;
        }

        /// <summary>Pinta el lienzo de la pantalla de nivel en la cámara de las capturas.</summary>
        public static void Attach(GameBootstrap boot)
        {
            Canvas canvas = boot.View!.Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = s_camera;
            canvas.planeDistance = 10f;
        }

        /// <summary>Pinta el lienzo de la pantalla de inicio en la cámara de las capturas.</summary>
        public static void AttachLobby(GameBootstrap boot)
        {
            Canvas canvas = boot.Lobby!.Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = s_camera;
            canvas.planeDistance = 10f;
        }

        /// <summary>Pinta un lienzo superpuesto (anuncio, tienda, aviso) por delante de la pantalla.</summary>
        public static void AttachOverlay(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = s_camera;
            canvas.planeDistance = 5f;
        }

        /// <summary>Escribe la captura <c>&lt;dir&gt;/&lt;name&gt;.png</c>.</summary>
        public static void Save(string dir, string name)
        {
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), Capture());
        }

        /// <summary>
        /// Como <see cref="Save"/>, con solo las <paramref name="heightPx"/> filas de arriba de la captura: la pantalla simulada
        /// entera cuando es más baja que la captura (16:9, 1080 × 1920 px: GameScreen la maqueta arriba del lienzo y lo de abajo
        /// es solo fondo, que en ese teléfono no existe; revisión de FX5). Con <paramref name="heightPx"/> ≥ <see cref="Height"/>,
        /// la captura entera.
        /// </summary>
        public static void SaveTop(string dir, string name, int heightPx)
        {
            int h = Mathf.Clamp(heightPx, 1, Height);
            if (h == Height)
            {
                Save(dir, name);
                return;
            }

            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!full.LoadImage(Capture()))
            {
                Object.Destroy(full);
                throw new System.InvalidOperationException("SnapshotKit.SaveTop: no se pudo leer la captura " + name);
            }

            var top = new Texture2D(full.width, h, TextureFormat.RGB24, false);
            top.SetPixels(full.GetPixels(0, full.height - h, full.width, h)); // las filas de la textura empiezan por abajo
            top.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), top.EncodeToPNG());
            Object.Destroy(full);
            Object.Destroy(top);
        }

        /// <summary>Renderiza la cámara de las capturas y devuelve el PNG.</summary>
        public static byte[] Capture()
        {
            Canvas.ForceUpdateCanvases();
            s_camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = s_target;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            byte[] png = tex.EncodeToPNG();
            Object.Destroy(tex);
            return png;
        }

        /// <summary>Espera <paramref name="seconds"/> segundos de tiempo real (con fotogramas de verdad).</summary>
        public static IEnumerator Wait(float seconds)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Medidas del tablero para la comparación con Magic Sort (ficha §2.1 y §2.2) en una línea <c>AF_BOARD</c>: cuias, filas,
        /// ancho y alto de la cuia y paso horizontal, en dp (× 3 = px de la captura).
        /// </summary>
        public static void LogBoard(string name, GameBootstrap boot)
        {
            CuiaView[] views = boot.View!.Views;
            var line = new System.Text.StringBuilder("AF_BOARD " + name + ": " + views.Length + " cuias");
            float u = views[0].Unit;
            line.Append(", ancho ").Append((2f * u).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                .Append(" dp, alto ").Append((views[0].HeightU * u).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" dp");
            float pitch = 0f;
            int rows = 1;
            for (int i = 1; i < views.Length; i++)
            {
                if (Mathf.Abs(views[i].FootPosition.y - views[i - 1].FootPosition.y) < 0.5f)
                {
                    if (pitch <= 0f)
                    {
                        pitch = views[i].FootPosition.x - views[i - 1].FootPosition.x;
                    }
                }
                else
                {
                    rows++;
                }
            }

            line.Append(", paso ").Append(pitch.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" dp, filas ").Append(rows);
            for (int i = 0; i < views.Length; i++)
            {
                line.Append(i == 0 ? " | pies (dp): " : " ").Append('(').Append(views[i].FootPosition.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(';').Append(views[i].FootPosition.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(')');
            }

            Debug.Log(line.ToString());
        }
    }
}
