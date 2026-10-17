#nullable enable
using System;
using System.IO;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

namespace AF.Audio.Editor
{
    /// <summary>
    /// Permiso Android <c>VIBRATE</c> para los hápticos (02 §4; 07 §9.2; aprobado por el dueño el 2026-10-16, D-026; regla dura
    /// 4 de CLAUDE.md), por un mecanismo versionado: el manifiesto que genera Unity se completa al crear el proyecto de Gradle
    /// (<c>IPostGenerateGradleAndroidProject</c>), sin tocar el YAML de <c>ProjectSettings</c> ni añadir un manifiesto de plugin.
    /// Solo añade ese permiso, y solo si falta: ningún otro permiso nuevo. La transformación del XML es pura
    /// (<see cref="WithVibrate"/>) para probarla en EditMode.
    /// </summary>
    public static class AndroidVibratePermission
    {
        /// <summary>Nombre del permiso.</summary>
        public const string Permission = "android.permission.VIBRATE";

        /// <summary>
        /// <paramref name="manifest"/> con <c>&lt;uses-permission android:name="android.permission.VIBRATE" /&gt;</c> justo antes de
        /// <c>&lt;application</c> (o antes de <c>&lt;/manifest&gt;</c>), si no lo tenía; si ya lo tiene, el mismo texto.
        /// </summary>
        public static string WithVibrate(string manifest)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (manifest.IndexOf("\"" + Permission + "\"", StringComparison.Ordinal) >= 0)
            {
                return manifest;
            }

            string line = "<uses-permission android:name=\"" + Permission + "\" />";
            int at = manifest.IndexOf("<application", StringComparison.Ordinal);
            if (at < 0)
            {
                at = manifest.LastIndexOf("</manifest>", StringComparison.Ordinal);
            }

            if (at < 0)
            {
                throw new FormatException("AndroidManifest.xml sin <application> ni </manifest>.");
            }

            int lineStart = manifest.LastIndexOf('\n', Math.Max(0, at - 1)) + 1;
            string indent = manifest.Substring(lineStart, at - lineStart);
            if (indent.Trim().Length > 0)
            {
                indent = "  ";
            }

            return manifest.Substring(0, lineStart) + indent + line + "\n" + manifest.Substring(lineStart);
        }

        /// <summary>Aplica <see cref="WithVibrate"/> al manifiesto del módulo <c>unityLibrary</c> de <paramref name="gradleModulePath"/>.</summary>
        public static bool ApplyTo(string gradleModulePath)
        {
            string path = Path.Combine(gradleModulePath, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(path))
            {
                return false;
            }

            string before = File.ReadAllText(path);
            string after = WithVibrate(before);
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                File.WriteAllText(path, after);
            }

            return true;
        }
    }

#if UNITY_ANDROID
    /// <summary>Gancho del build de Android: Unity lo llama tras generar el proyecto de Gradle (<see cref="AndroidVibratePermission"/>).</summary>
    internal sealed class AndroidVibrateGradleHook : IPostGenerateGradleAndroidProject
    {
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!AndroidVibratePermission.ApplyTo(path))
            {
                UnityEngine.Debug.LogWarning("AF.Audio: no está el AndroidManifest.xml de " + path + "; sin permiso VIBRATE.");
            }
        }
    }
#endif
}
