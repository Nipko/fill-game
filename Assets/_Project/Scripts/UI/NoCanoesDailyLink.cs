#nullable enable
using System;
using AF.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La línea «Mientras esperas: Jugar el Reto del río» de Sin canoas (enganche E7; 04 §7.3, wireframe: «no consume canoas»;
    /// 04 §7.1: el Reto del río sigue disponible con 0 canoas; 06 §4.1). Un enlace como «Volver al inicio» (madera_base
    /// subrayada sobre la madera clara, D-003, sin rojo), que <c>NoCanoesPanel</c> coloca justo encima de «Volver al inicio»,
    /// en el orden del wireframe, solo si el Reto se puede jugar hoy (<see cref="Available"/>). El toque va a
    /// <see cref="GameBootstrap.PressNoCanoesDaily"/>: registra <c>af_no_canoes_choice</c> = <c>daily_challenge</c> (04 §11;
    /// 08 §8.2), cierra Sin canoas y abre el tablero de hoy. La lógica (cuándo se ofrece y qué hace) vive en
    /// <c>GameBootstrap.Daily.cs</c>; aquí solo el botón, para que el parche de <c>NoCanoesScreen.cs</c> sea mínimo.
    /// <para>Sin chevron «›» dibujado [P]: el wireframe lo pinta como «>» al final; aquí el enlace subrayado ya dice que se
    /// toca, igual que «Volver al inicio».</para>
    /// </summary>
    internal sealed class NoCanoesDailyLink
    {
        /// <summary>Clave del texto (<c>spark-strings-reto.csv</c>).</summary>
        public const string TextKey = "no_canoes_daily";

        /// <summary>Id del botón en <c>AF_LAYOUT</c> (el mismo valor que <c>choice</c>).</summary>
        public const string LayoutId = "daily_challenge";

        /// <summary>Alto del enlace (dp): el de «Volver al inicio», con área táctil de 48 dp (02 §12).</summary>
        public const float HeightDp = 48f;

        /// <summary>Hueco bajo el enlace, antes de «Volver al inicio» (dp).</summary>
        public const float GapDp = 2f;

        private const float TextSp = 16f;

        private readonly Button _button;
        private readonly TextMeshProUGUI _label;

        /// <summary>Crea el enlace (oculto) dentro del panel de Sin canoas; <paramref name="onPress"/> por defecto, <see cref="Press"/>.</summary>
        public NoCanoesDailyLink(Transform panel, Action? onPress = null)
        {
            _button = ModalKit.Link("DailyButton", panel, string.Empty, onPress ?? (() => Press()), out _label);
            _label.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_label, UiBuilder.MinTextSp, TextSp);
            _button.gameObject.SetActive(false);
        }

        /// <summary>
        /// El Reto se puede jugar hoy desde Sin canoas (<see cref="GameBootstrap.NoCanoesDailyAvailable"/> de la escena). Sin
        /// arranque (tests de la pantalla sola), false.
        /// </summary>
        public static bool Available
        {
            get
            {
                GameBootstrap? boot = GameBootstrap.Current;
                return boot != null && boot.NoCanoesDailyAvailable;
            }
        }

        /// <summary>El toque: <see cref="GameBootstrap.PressNoCanoesDaily"/>.</summary>
        public static bool Press()
        {
            GameBootstrap? boot = GameBootstrap.Current;
            return boot != null && boot.PressNoCanoesDaily();
        }

        /// <summary>El botón (tests y <c>AF_LAYOUT</c>).</summary>
        public Button Button => _button;

        /// <summary>A la vista en el panel.</summary>
        public bool Visible => _button.gameObject.activeSelf;

        /// <summary>Texto que lleva.</summary>
        public string Text => _label.text;

        /// <summary>Texto del enlace.</summary>
        public static string Label => Loc.T(TextKey);

        /// <summary>
        /// Coloca el enlace en <paramref name="y"/> (dp desde arriba del panel), con <paramref name="x"/> de margen y
        /// <paramref name="width"/> de ancho, si <paramref name="shown"/>; si no, lo oculta. Devuelve el alto que ocupa (0 oculto).
        /// </summary>
        public float Layout(bool shown, float x, float y, float width)
        {
            _button.gameObject.SetActive(shown);
            if (!shown)
            {
                return 0f;
            }

            _label.text = Label;
            UiBuilder.Place((RectTransform)_button.transform, x, y, width, HeightDp);
            return HeightDp + GapDp;
        }
    }
}
