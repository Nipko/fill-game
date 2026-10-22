#nullable enable
using AF.Localization;
using AF.LiveOps;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Presentación del Reto del río tras ganar el nivel 7 (enganche E6; 06 §2.2, fila del nivel 7: «Tras ganar, presentación del
    /// Reto del río (tarjeta de 2 s: "Desde el nivel 10, un tablero nuevo cada día"; 02 §13); se desbloquea en el 10»; 02 §13,
    /// fila 7; 06 §2.3 y §4.1). Una placa de madera clara (la de los avisos, <see cref="Toast"/>; D-003, sin rojo) que sale sola
    /// en la pantalla de victoria del 7, se ve <see cref="VisibleMs"/> y se va sola, sin capturar toques: Siguiente, Inicio y la
    /// Cosecha doble siguen respondiendo, y no cambia nada del flujo de la victoria (02 §6; D-011).
    /// <list type="bullet">
    /// <item><b>Cuándo</b> (<see cref="ShouldShow"/>): solo en la victoria del <see cref="TeaserLevel"/>, con el Reto en el build
    /// (el pool cargado: sin él, D-008 lo deja oculto y la tarjeta no promete nada) y con el interruptor
    /// <c>biz_feature_daily_challenge</c> encendido; no sale si <c>live_unlock_level_daily</c> ya está superado (el Reto ya se ve
    /// en el inicio) [P]. El número del texto es <c>live_unlock_level_daily</c> (08 §9), no un 10 escrito: la clave lleva
    /// <c>{level}</c>.</item>
    /// <item><b>Cuántas veces</b>: el GDD no dice «una sola vez»; sale cada vez que se ve la victoria del 7 (en la campaña, una;
    /// otra si la app se reabre en esa victoria) [P]. No se guarda nada en el perfil.</item>
    /// <item><b>Momento y sitio</b> [P]: entra <see cref="DelayMs"/> después de verse la victoria (el panel ya subió y la Cosecha
    /// terminó de contar: 250 + 400 ms de GameScreen, más 250 de aire) y se centra sobre las cuias, en la franja entre la cima de las cuias
    /// (ya desplazadas por la victoria) y la cinta del panel; nunca tapa el panel (Cosecha, Siguiente, Inicio, la ranura del
    /// Dato) ni la cabecera, y nunca queda debajo de TiTino: va bajo su gesto si cabe y, si no, TiTino cede la franja
    /// (<c>GameScreen.TeaserTopClearOfSloth</c>; observación 2 de la hoja del 2026-10-22). Va dentro de la capa de la victoria: si la
    /// victoria se cierra antes (Siguiente o Inicio), se va con ella.</item>
    /// <item><b>Ningún aviso encima de la victoria</b> (D-011, punto 2) habla de la cola de avisos del tablero; esto no entra en
    /// esa cola: es la presentación que el GDD pone en esta victoria.</item>
    /// </list>
    /// Avanza con el reloj de la pantalla (<see cref="Tick"/>, desde <c>GameScreen.Tick</c>), sin asignaciones por fotograma.
    /// </summary>
    internal sealed class DailyTeaserCard
    {
        /// <summary>Nivel cuya victoria presenta el Reto (06 §2.2; 02 §13).</summary>
        public const int TeaserLevel = 7;

        /// <summary>Tiempo a la vista, con los fundidos de 150 ms de la placa (06 §2.2: «tarjeta de 2 s»).</summary>
        public const float VisibleMs = 2000f;

        /// <summary>Espera desde que se ve la victoria (ms) [P]: el panel sube (250 ms), la Cosecha cuenta (250 + 400 ms) y 250 ms de aire.</summary>
        public const float DelayMs = 900f;

        /// <summary>Clave del texto: «Desde el nivel {level}, un tablero nuevo cada día» (<c>spark-strings-reto.csv</c>).</summary>
        public const string TextKey = "daily_teaser";

        /// <summary>Tamaño del texto (sp), el de los avisos del tablero.</summary>
        public const float TextSp = 15f;

        private readonly RectTransform _parent;
        private readonly Toast _toast;
        private float _delayLeftMs = -1f;
        private float _top;
        private float _bottom;
        private float _canvasW;

        /// <summary>Crea la tarjeta (oculta) dentro de <paramref name="parent"/>, la capa de la victoria.</summary>
        public DailyTeaserCard(RectTransform parent, ProceduralArt art)
        {
            _parent = parent;
            _toast = new Toast("DailyTeaser", parent, art, TextSp);
        }

        /// <summary>Configuración del Reto (08 §9; hasta <c>AF.RemoteConfig</c>, los defaults, como <c>GameBootstrap.Daily.cs</c>).</summary>
        public DailyChallengeConfig Config { get; set; } = DailyChallengeConfig.Defaults;

        /// <summary>A la vista ahora (tras la espera, mientras dura).</summary>
        public bool Visible => _toast.Visible;

        /// <summary>Esperando a entrar o a la vista.</summary>
        public bool Pending => _delayLeftMs >= 0f || _toast.Visible;

        /// <summary>Texto que lleva (tests).</summary>
        public string Text => _toast.Text;

        /// <summary>Ms que le quedan a la vista (−1 si no se ve).</summary>
        public float LeftMs => _toast.LeftMs;

        /// <summary>Rectángulo que ocupa, con el canto de abajo (dp desde arriba del lienzo; tests de solapes).</summary>
        public Rect SeenRect => _toast.SeenRect;

        /// <summary>La placa deja pasar los toques (siempre).</summary>
        public bool BlocksRaycasts => _toast.BlocksRaycasts;

        /// <summary>Raíz de la placa (tests).</summary>
        public RectTransform Root => _toast.Root;

        /// <summary>
        /// La victoria de <paramref name="wonLevel"/> presenta el Reto: es el <see cref="TeaserLevel"/>, el Reto está en el build
        /// (<paramref name="inBuild"/>), el interruptor está encendido y el desbloqueo aún no llegó.
        /// </summary>
        public static bool ShouldShow(int wonLevel, bool inBuild, DailyChallengeConfig config)
        {
            return wonLevel == TeaserLevel && inBuild && config != null && config.BizFeatureDailyChallenge
                && config.LiveUnlockLevelDaily > wonLevel;
        }

        /// <summary>«Desde el nivel {level}, un tablero nuevo cada día», con <c>live_unlock_level_daily</c>.</summary>
        public static string TextFor(DailyChallengeConfig config)
        {
            return Loc.T(TextKey, LocArgs.OfDigits("level", config.LiveUnlockLevelDaily));
        }

        /// <summary>
        /// Victoria de <paramref name="wonLevel"/> a la vista: si toca (<see cref="ShouldShow"/>), la tarjeta entra tras
        /// <see cref="DelayMs"/>, centrada entre <paramref name="cuiasTop"/> (la cima de las cuias ya desplazadas) y
        /// <paramref name="ribbonTop"/> (la cinta del panel), sin subir de <paramref name="minTop"/> (el pie de la cabecera). Si no
        /// toca, se oculta. Devuelve si va a salir.
        /// </summary>
        public bool Show(int wonLevel, bool inBuild, float canvasW, float minTop, float cuiasTop, float ribbonTop)
        {
            if (!ShouldShow(wonLevel, inBuild, Config))
            {
                Hide();
                return false;
            }

            _canvasW = canvasW;
            _top = Mathf.Max(minTop, cuiasTop);
            _bottom = Mathf.Max(_top, ribbonTop);
            _toast.Hide();
            _toast.Show(TextFor(Config), VisibleMs); // mide con su texto
            _toast.Hide();
            _delayLeftMs = DelayMs;
            Place(minTop);
            return true;
        }

        /// <summary>
        /// Alto que tendrá la placa con su texto en un lienzo de <paramref name="canvasW"/> dp (para decidir su franja antes de
        /// <see cref="Show"/>; deja la placa oculta). Mide el texto: solo al abrirse la victoria.
        /// </summary>
        public float HeightDp(float canvasW)
        {
            _toast.Hide();
            _toast.Show(TextFor(Config), VisibleMs);
            float h = _toast.PreferredHeightDp(canvasW);
            _toast.Hide();
            return h;
        }

        /// <summary>Se va ya (también si aún esperaba).</summary>
        public void Hide()
        {
            _delayLeftMs = -1f;
            _toast.Hide();
        }

        /// <summary>Avanza <paramref name="dtMs"/>; fuera de la victoria (su capa oculta) se va.</summary>
        public void Tick(float dtMs)
        {
            if (_delayLeftMs < 0f && !_toast.Visible)
            {
                return;
            }

            if (!_parent.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            if (_delayLeftMs >= 0f)
            {
                _delayLeftMs -= dtMs;
                if (_delayLeftMs >= 0f)
                {
                    return;
                }

                float over = -_delayLeftMs;
                _delayLeftMs = -1f;
                _toast.Show(_toast.Text, VisibleMs);
                _toast.Tick(over);
                return;
            }

            _toast.Tick(dtMs);
        }

        // Centrada a lo ancho y, a lo alto, en la franja de las cuias; si la franja es más baja que la placa, se apoya en la cinta
        // y sube, sin pasar de minTop.
        private void Place(float minTop)
        {
            float h = _toast.PreferredHeightDp(_canvasW);
            float top = _top + (((_bottom - _top) - h) * 0.5f);
            if (top + h > _bottom)
            {
                top = _bottom - h;
            }

            _toast.Place(_canvasW, Mathf.Max(minTop, top));
            if (h > (Toast.DefaultHeightDp * UiBuilder.TextScale) + 0.5f)
            {
                float w = Toast.FullWidthDp(_canvasW);
                _toast.PlaceAt((_canvasW - w) * 0.5f, Mathf.Max(minTop, top), w, h);
            }
        }
    }
}
