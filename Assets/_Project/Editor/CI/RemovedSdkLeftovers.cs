#if UNITY_ANDROID
#nullable enable
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>
    /// Borra del proyecto Gradle que exporta Unity lo que dejaron SDK ya retirados (añadido de D-049 del 2026-10-27: AppLovin MAX
    /// salió del proyecto). Unity reexporta <c>Library/Bee/Android/Prj/IL2CPP/Gradle</c> sin vaciar <c>unityLibrary/src/main/res</c>,
    /// así que el <c>res/raw/applovin_settings.json</c> que escribía el post-proceso de MAX seguía entrando en el APK y en el AAB de
    /// los builds incrementales (y en el CI, que restaura <c>Library</c> de una caché anterior), y <c>ci/check-no-applovin.ps1</c>
    /// los rechaza. Corre en cada build, antes que Gradle; si no hay nada que borrar, no hace nada.
    /// </summary>
    public sealed class RemovedSdkLeftovers : IPostGenerateGradleAndroidProject
    {
        /// <summary>Rutas relativas a <c>unityLibrary</c> que ya no debe llevar ningún build.</summary>
        internal static readonly string[] Leftovers =
        {
            "src/main/res/raw/applovin_settings.json", // AppLovin MAX (post-proceso del plugin, D-042)
            "src/main/res/values/af_max_config.xml",   // AF.Ads.Max.Editor.MaxBuildConfig (D-042)
        };

        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            foreach (string relative in Leftovers)
            {
                string file = Path.Combine(path, relative);
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Debug.Log("[AF.RemovedSdkLeftovers] borrado " + relative + " (SDK retirado; anadido de D-049 del 2026-10-27).");
                }
            }
        }
    }
}
#endif
