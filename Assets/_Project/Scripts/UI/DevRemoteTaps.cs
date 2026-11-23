#nullable enable
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AF.UI
{
    /// <summary>
    /// Toques remotos para probar en el iPhone físico (equipo Mac, D-067): el equivalente de <c>adb shell input tap</c>, que iOS no
    /// tiene. Solo existe en builds de desarrollo (<c>DEVELOPMENT_BUILD</c>; release ni lo compila) y solo arranca en iOS.
    ///
    /// Cada 0,2 s mira <c>af_taps.txt</c> en <see cref="Application.persistentDataPath"/> (Documents en iOS). Si está, lo lee, lo
    /// borra y ejecuta sus líneas en orden con un ratón virtual del Input System (los toques del juego van por el EventSystem):
    /// <list type="bullet">
    /// <item><c>tap x y</c>: pulsa y suelta en (x, y);</item>
    /// <item><c>drag x1 y1 x2 y2 ms</c>: arrastra de un punto a otro en <c>ms</c> milisegundos;</item>
    /// <item><c>wait ms</c>: espera.</item>
    /// </list>
    /// Las coordenadas son las de <c>AF_LAYOUT</c>: píxeles de pantalla con el origen arriba a la izquierda. Escribe
    /// <c>AF_TAP {json}</c> con cada orden y al terminar el archivo. El archivo se copia desde la Mac con
    /// <c>xcrun devicectl device copy to --domain-type appDataContainer --domain-identifier com.amazonfill.dev
    /// --destination Documents/af_taps.txt</c>.
    /// </summary>
    public sealed class DevRemoteTaps : MonoBehaviour
    {
        /// <summary>El archivo de órdenes, dentro de <see cref="Application.persistentDataPath"/>.</summary>
        public const string FileName = "af_taps.txt";

        private const float PollSeconds = 0.2f;
        private const float HoldSeconds = 0.08f;

        private readonly Queue<string> _pending = new Queue<string>();
        private Mouse? _mouse;
        private string _path = string.Empty;
        private float _nextPoll;
        private bool _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                return;
            }

            var go = new GameObject("AF.DevRemoteTaps");
            DontDestroyOnLoad(go);
            go.AddComponent<DevRemoteTaps>();
        }

        private void Awake()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
            _mouse = InputSystem.AddDevice<Mouse>("AFDevRemoteTaps");
            Debug.Log("AF_TAP {\"event\":\"ready\",\"file\":\"" + FileName + "\"}");
        }

        private void OnDestroy()
        {
            if (_mouse != null && _mouse.added)
            {
                InputSystem.RemoveDevice(_mouse);
            }
        }

        private void Update()
        {
            if (_running || Time.unscaledTime < _nextPoll)
            {
                return;
            }

            _nextPoll = Time.unscaledTime + PollSeconds;
            try
            {
                if (!File.Exists(_path))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(_path);
                File.Delete(_path);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length > 0 && trimmed[0] != '#')
                    {
                        _pending.Enqueue(trimmed);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("AF_TAP {\"event\":\"error\",\"what\":\"read\",\"msg\":\"" + Escape(e.Message) + "\"}");
                return;
            }

            if (_pending.Count > 0)
            {
                StartCoroutine(RunPending());
            }
        }

        private IEnumerator RunPending()
        {
            _running = true;
            int n = 0;
            while (_pending.Count > 0)
            {
                string line = _pending.Dequeue();
                string[] p = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                IEnumerator? step = null;
                if (p[0] == "tap" && p.Length == 3 && TryNum(p[1], out float x) && TryNum(p[2], out float y))
                {
                    step = Tap(x, y);
                }
                else if (p[0] == "drag" && p.Length == 6 && TryNum(p[1], out float x1) && TryNum(p[2], out float y1)
                    && TryNum(p[3], out float x2) && TryNum(p[4], out float y2) && TryNum(p[5], out float ms))
                {
                    step = Drag(x1, y1, x2, y2, ms / 1000f);
                }
                else if (p[0] == "wait" && p.Length == 2 && TryNum(p[1], out float waitMs))
                {
                    step = Wait(waitMs / 1000f);
                }

                if (step == null)
                {
                    Debug.LogWarning("AF_TAP {\"event\":\"error\",\"what\":\"line\",\"line\":\"" + Escape(line) + "\"}");
                    continue;
                }

                n++;
                Debug.Log("AF_TAP {\"event\":\"cmd\",\"i\":" + n.ToString(CultureInfo.InvariantCulture) + ",\"line\":\"" + Escape(line) + "\"}");
                yield return step;
            }

            Debug.Log("AF_TAP {\"event\":\"done\",\"n\":" + n.ToString(CultureInfo.InvariantCulture) + "}");
            _running = false;
        }

        private IEnumerator Tap(float x, float y)
        {
            Vector2 pos = ToScreen(x, y);
            Queue(pos, false);
            yield return null;
            Queue(pos, true);
            yield return new WaitForSecondsRealtime(HoldSeconds);
            Queue(pos, false);
            yield return null;
            yield return null;
        }

        private IEnumerator Drag(float x1, float y1, float x2, float y2, float seconds)
        {
            Vector2 from = ToScreen(x1, y1);
            Vector2 to = ToScreen(x2, y2);
            Queue(from, false);
            yield return null;
            Queue(from, true);
            yield return null;
            float start = Time.realtimeSinceStartup;
            float t = 0f;
            while (t < 1f)
            {
                t = seconds <= 0f ? 1f : Mathf.Clamp01((Time.realtimeSinceStartup - start) / seconds);
                Queue(Vector2.Lerp(from, to, t), true);
                yield return null;
            }

            Queue(to, false);
            yield return null;
            yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private void Queue(Vector2 position, bool pressed)
        {
            if (_mouse == null)
            {
                return;
            }

            var state = new MouseState { position = position }.WithButton(MouseButton.Left, pressed);
            InputSystem.QueueStateEvent(_mouse, state);
        }

        // AF_LAYOUT: origen arriba a la izquierda; el Input System: abajo a la izquierda.
        private static Vector2 ToScreen(float x, float y) => new Vector2(x, Screen.height - y);

        private static bool TryNum(string s, out float value) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif
