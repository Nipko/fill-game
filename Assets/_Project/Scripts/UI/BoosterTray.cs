#nullable enable
using System;
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Bandeja cumbuca de comodines (tarea B2-1; GDD 07 §7.4, 02 §5, 04 §1.1, §2.1, §2.2 y §2.5) con el orden canónico y los
    /// equivalentes de Magic Sort (ficha §2.3 y §5.2: deshacer, añadir botella; barajar llega con la Pororoca en I6):
    /// <list type="bullet">
    /// <item><description>[Deshacer · Marea]: sin contador en 1-10; desde el 11, badge blanco con el cupo gratis y «+N» ámbar con
    /// el stock; sin cupo ni stock, «+3» con el icono de video (<c>rv_undo_refill</c>) o la etiqueta de precio (30), y tocarlo
    /// recarga; gris sin historial;</description></item>
    /// <item><description>[+1 cuia · Cuia extra] desde el nivel 8: «+N» con stock o el precio (150), en rojo y con «Te faltan
    /// {n}» encima sin saldo; gris si ya se usó en el nivel, si no queda vertido útil o con 16 cuias;</description></item>
    /// <item><description>[Revelar · Luciérnaga] (tarea T14) solo en los niveles de Aguas turbias mientras quede alguna dosis
    /// oculta, siempre la última a la derecha: «+N» con stock; sin stock, el precio (120, <c>reveal_seed_price</c>) y, sin
    /// saldo, en rojo con «Te faltan {n}»; sin stock ni saldo y con <c>rv_reveal</c>, «+1» con el icono de video
    /// (<see cref="SetRevealBadge"/>). La visibilidad, la insignia y la acción las pone la pantalla de nivel
    /// (<c>GameScreen.Reveal.cs</c>).</description></item>
    /// </list>
    /// Antes del desbloqueo la ranura no existe y la bandeja se centra (07 §7.4): 16 + n·64 + (n − 1)·8 + 16 dp (240 con las
    /// tres ranuras de un nivel turbio). Con 5 ranuras pasarían a 56 dp (07 §7.4), pero Morpho (12) y Pororoca (30) no están
    /// en el cliente y en la 1.0 nunca hay más de 3: sus ranuras llegan con sus comodines, y entonces Revelar pasa a ser la 5.ª
    /// (04 §2.5, a la derecha de Pororoca). Se refresca cada fotograma solo si algo cambió, sin asignar memoria (08 §5.4): el
    /// video de Marea se pregunta a los anuncios como mucho cada <see cref="UndoVideoPollMs"/> (<see cref="Tick"/>).
    /// </summary>
    internal sealed class BoosterTray
    {
        /// <summary>Lo que da un video de <c>rv_reveal</c> (04 §2.5: «1 revelado»): la cifra de la insignia con el icono de video.</summary>
        public const int RevealVideoUnits = 1;

        /// <summary>
        /// Cada cuánto se vuelve a preguntar a los anuncios si hay video de <c>rv_undo_refill</c> mientras Marea no tiene cupo
        /// [P], el mismo plazo que Revelar (<see cref="GameScreen.RevealVideoPollMs"/>, T14): preguntar lee los contadores del
        /// día con la clave de la fecha local, que crea una cadena cada vez, así que no se hace en cada fotograma (08 §5.4); el
        /// tope diario y la espera entre videos no cambian más deprisa. Tocar Marea vuelve a preguntar en el acto.
        /// </summary>
        public const float UndoVideoPollMs = GameScreen.RevealVideoPollMs;

        private const float SlotDp = 64f;
        private const float GapDp = 8f;
        private const float PadDp = 16f;

        // «Te faltan {n}» de dos ranuras vecinas a la vez: cada aviso cabe en su paso de ranura (64 + 8) con 2 dp de aire entre
        // los dos. A 12 sp, «Te faltan» y una cifra de 2 caben en una línea («Te faltan 70» mide 67,8 dp); con 3 (el máximo con
        // los precios de 08 §9; 75 dp) la cifra baja a una segunda línea (Slot.PlaceMissing).
        private const float MissingNarrowDp = SlotDp + GapDp - 2f;

        private readonly ProceduralArt _art;
        private readonly Image _tray;
        private readonly Slot _undo;
        private readonly Slot _extra;
        private readonly Slot _reveal;
        private readonly char[] _buf = new char[24];

        private LevelFlow? _flow;
        private GameServices? _services;
        private Action? _onReveal;
        private int _key = int.MinValue;
        private int _revealKey = int.MinValue;
        private float _canvasW = 360f;
        private float _trayTop;
        private int _slots = 1;
        private int _slotsVersion;
        private bool _visible = true;
        private bool _revealPressed;
        private bool _undoVideoReady;
        private float _undoVideoPollMs;

        public BoosterTray(Transform hud, ProceduralArt art, Action onUndo, Action onExtraCuia)
        {
            _art = art;
            _tray = UiBuilder.Image("Tray", hud, art.Tray(96f), Color.white);
            _undo = new Slot("UndoButton", hud, art, art.IconUndo, art.IconUndoDisabled, UiText.Undo, onUndo);
            _extra = new Slot("ExtraCuiaButton", hud, art, art.IconCuiaPlus, art.IconCuiaPlusDisabled, UiText.ExtraCuia, onExtraCuia);
            _extra.SetActive(false);
            _reveal = new Slot("RevealButton", hud, art, art.IconFirefly, art.IconFireflyDisabled, UiText.RevealLabel, () => _onReveal?.Invoke());
            _reveal.SetActive(false);
        }

        /// <summary>Botón de Marea.</summary>
        public Button UndoButton => _undo.Parts.Button;

        /// <summary>Botón de Cuia extra (activo desde el nivel 8).</summary>
        public Button ExtraCuiaButton => _extra.Parts.Button;

        /// <summary>Botón de Revelar · Luciérnaga (solo en los niveles de Aguas turbias con ocultas).</summary>
        public Button RevealButton => _reveal.Parts.Button;

        /// <summary>La ranura de Cuia extra existe en el nivel.</summary>
        public bool ExtraCuiaShown => _extra.Active;

        /// <summary>La ranura de Revelar existe en el nivel (<see cref="SetRevealVisible"/>).</summary>
        public bool RevealShown => _reveal.Active;

        /// <summary>Ranuras visibles.</summary>
        public int SlotCount => _slots;

        /// <summary>
        /// Sube cada vez que aparece o desaparece una ranura (Cuia extra o Revelar) y la bandeja se vuelve a centrar: las
        /// ranuras que quedan cambian de sitio y AF_LAYOUT tiene que volver a emitirse (<c>GameScreen.Reveal.cs</c>).
        /// </summary>
        public int SlotsVersion => _slotsVersion;

        /// <summary>Ancho de la bandeja (dp).</summary>
        public float Width => PadDp + (_slots * SlotDp) + ((_slots - 1) * GapDp) + PadDp;

        /// <summary>Texto del badge de Marea (diagnóstico y tests).</summary>
        public string UndoBadgeText => _undo.BadgeText;

        /// <summary>Texto del badge de Cuia extra («+N»), o vacío.</summary>
        public string ExtraBadgeText => _extra.BadgeText;

        /// <summary>Precio que muestra la Cuia extra (0 si no lo muestra).</summary>
        public int ExtraPriceShown => _extra.PriceShown;

        /// <summary>La Cuia extra muestra «Te faltan {n}».</summary>
        public bool ExtraShowsMissing => _extra.MissingShown;

        /// <summary>Texto de la insignia de Revelar («+N» del stock o «+1» del video), o vacío.</summary>
        public string RevealBadgeText => _reveal.BadgeText;

        /// <summary>Precio que muestra Revelar (0 si no lo muestra).</summary>
        public int RevealPriceShown => _reveal.PriceShown;

        /// <summary>Revelar muestra «Te faltan {n}».</summary>
        public bool RevealShowsMissing => _reveal.MissingShown;

        /// <summary>Revelar ofrece el video (<c>rv_reveal</c>): la insignia lleva el icono de video.</summary>
        public bool RevealShowsVideo => _reveal.VideoShown;

        /// <summary>Texto del aviso «Te faltan {n}» de Revelar (vacío si no se ve).</summary>
        public string RevealMissingText => _reveal.MissingText;

        /// <summary>Texto del aviso «Te faltan {n}» de Cuia extra (vacío si no se ve; tests).</summary>
        public string ExtraMissingText => _extra.MissingText;

        /// <summary>Ancho del aviso «Te faltan {n}» de Revelar (dp; tests de solapes).</summary>
        public float RevealMissingWidth => _reveal.MissingWidth;

        /// <summary>Ancho del aviso «Te faltan {n}» de Cuia extra (dp; tests de solapes).</summary>
        public float ExtraMissingWidth => _extra.MissingWidth;

        /// <summary>Aviso «Te faltan {n}» de Cuia extra (tests de solapes).</summary>
        public RectTransform ExtraMissingRect => _extra.MissingRect;

        /// <summary>Aviso «Te faltan {n}» de Revelar (tests de solapes).</summary>
        public RectTransform RevealMissingRect => _reveal.MissingRect;

        /// <summary>Insignia de arriba a la derecha de Marea (el cupo o «+3»; un disco de 24 dp; tests de solapes).</summary>
        public RectTransform UndoBadgeRect => _undo.BadgeRect;

        /// <summary>Glifo de la ranura de Revelar (tests y capturas: el icono de la luciérnaga).</summary>
        public Image RevealGlyph => _reveal.Glyph;

        /// <summary>La cara de Revelar está pulsada porque se está eligiendo la cuia.</summary>
        public bool RevealPressed => _revealPressed;

        /// <summary>Relieve del botón de Revelar: la cara que baja mientras se elige la cuia (tests).</summary>
        public ReliefButton RevealRelief => _reveal.Parts.Relief;

        /// <summary>Marea ofrece el video (<c>rv_undo_refill</c>): «+3» con el icono de video.</summary>
        public bool UndoShowsVideo => _undo.VideoShown;

        /// <summary>Precio que muestra Marea sin cupo ni video (0 si no lo muestra; tests).</summary>
        public int UndoPriceShown => _undo.PriceShown;

        /// <summary>Texto del aviso «Te faltan {n}» de Marea (vacío si no se ve; tests).</summary>
        public string UndoMissingText => _undo.MissingText;

        /// <summary>
        /// Ms que faltan para volver a preguntar por el video de Marea (0 con cupo, o si la próxima <see cref="Refresh"/> pregunta;
        /// tests).
        /// </summary>
        public float UndoVideoPollLeftMs => _undoVideoPollMs;

        /// <summary>Texto del aviso «Te faltan {n}» de Revelar (tests de tamaño y desbordes).</summary>
        public TextMeshProUGUI RevealMissingLabel => _reveal.MissingLabel;

        /// <summary>Texto del aviso «Te faltan {n}» de Cuia extra (tests de tamaño y desbordes).</summary>
        public TextMeshProUGUI ExtraMissingLabel => _extra.MissingLabel;

        /// <summary>Etiqueta de precio de Revelar (tests de solapes).</summary>
        public RectTransform RevealPriceRect => _reveal.PriceRect;

        /// <summary>Etiqueta de precio de Cuia extra (tests de solapes).</summary>
        public RectTransform ExtraPriceRect => _extra.PriceRect;

        public void Bind(LevelFlow flow, GameServices? services)
        {
            _flow = flow;
            _services = services;
            _key = int.MinValue;
            _revealKey = int.MinValue;
            ForgetUndoVideo();
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/> ms el plazo de la próxima pregunta por el video de Marea (<see cref="UndoVideoPollMs"/>):
        /// lo llama la pantalla en cada fotograma, con <see cref="Refresh"/>. Sin asignaciones.
        /// </summary>
        public void Tick(float dtMs)
        {
            if (_undoVideoPollMs > 0f)
            {
                _undoVideoPollMs -= dtMs;
            }
        }

        /// <summary>Acción del botón Revelar (la pone <c>GameScreen.Reveal.cs</c>: <c>IGameActions.PressReveal</c>).</summary>
        public void SetRevealAction(Action onReveal)
        {
            _onReveal = onReveal ?? throw new ArgumentNullException(nameof(onReveal));
        }

        /// <summary>
        /// La ranura de Revelar existe (nivel turbio con ocultas) o no: aparece a la derecha de las demás o desaparece, y la
        /// bandeja se vuelve a centrar (07 §7.4). Devuelve si cambió.
        /// </summary>
        public bool SetRevealVisible(bool visible)
        {
            if (visible == _reveal.Active)
            {
                return false;
            }

            _reveal.SetActive(visible);
            _slots = CountSlots();
            _slotsVersion++;
            _revealKey = int.MinValue;
            Layout(_canvasW, _trayTop);
            return true;
        }

        /// <summary>
        /// Insignia y habilitado de Revelar (04 §2.5; 04 §1.1: el stock se gasta antes que las semillas): con
        /// <paramref name="stock"/> &gt; 0, «+N» en ámbar; sin stock y con <paramref name="seeds"/> ≥ <paramref name="price"/>, el
        /// precio; sin saldo y con <paramref name="video"/> (<c>rv_reveal</c> listo), «+1» con el icono de video; sin nada, el
        /// precio en rojo y «Te faltan {n}» encima, como la Cuia extra. Es el mismo orden en que cobra el botón
        /// (<c>IGameActions.PressReveal</c>). Con <paramref name="bare"/> queda gris y sin insignia, precio ni aviso, nada que
        /// comprar ahora: la pantalla lo pide cuando ya no queda ninguna oculta (la ranura solo espera a que la luciérnaga salga
        /// de ella y se vaya) y, sin stock, cuando la ranura no se puede usar ni se está eligiendo (fuera de PLAYING), como la
        /// Cuia extra (<c>GameScreen.Reveal.cs</c>). Solo repinta si algo cambió; sin asignaciones salvo al cambiar el aviso.
        /// </summary>
        public void SetRevealBadge(int stock, int price, int seeds, bool video, bool enabled, bool selecting, bool bare = false)
        {
            if (_revealPressed && !_reveal.Parts.Relief.Pressed)
            {
                // El botón que soltó el dedo (OnPointerUp u OnPointerExit al levantar el toque) vuelve abajo mientras dure la
                // selección.
                _reveal.Parts.Relief.Press(true);
            }

            int key = Hash(stock, price, seeds, video ? 1 : 0, enabled ? 1 : 0, selecting ? 1 : 0, _reveal.Active ? 1 : 0, _visible ? 1 : 0, bare ? 1 : 0);
            if (key == _revealKey)
            {
                return;
            }

            _revealKey = key;
            enabled &= !bare;
            UiBuilder.SetEnabled(_reveal.Parts.Button, enabled);
            bool pressed = enabled && selecting;
            if (pressed != _revealPressed)
            {
                // Mientras se elige la cuia, la cara del botón se queda abajo (pulsada); otro toque cancela.
                _revealPressed = pressed;
                _reveal.Parts.Relief.Press(pressed);
            }

            if (bare)
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(0);
                _reveal.HidePrice();
            }
            else if (stock > 0)
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(stock);
                _reveal.HidePrice();
            }
            else if (seeds >= price)
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(0);
                _reveal.ShowPrice(price, 0);
            }
            else if (video)
            {
                _reveal.ShowStockBadge(0);
                _reveal.ShowBadge(Number(RevealVideoUnits, true), true);
                _reveal.HidePrice();
            }
            else
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(0);
                _reveal.ShowPrice(price, Math.Max(0, price - seeds));
            }

            LayoutMissing();
        }

        public void SetVisible(bool visible)
        {
            if (visible && !_visible)
            {
                // Al volver (tras el embudo o la victoria), los avisos «Te faltan {n}» se vuelven a pintar y el video de Marea se
                // pregunta en el acto (en el embudo pudo verse un video: la espera entre videos ya cuenta).
                _key = int.MinValue;
                _revealKey = int.MinValue;
                ForgetUndoVideo();
            }

            // Cada ranura se enseña si la bandeja se ve y la ranura existe (Slot.SetVisible lo combina con Active): pasarle
            // «visible && Active» la dejaba oculta para siempre si aparecía después (Revelar al cargar un nivel turbio).
            _visible = visible;
            _tray.gameObject.SetActive(visible);
            _undo.SetVisible(visible);
            _extra.SetVisible(visible);
            _reveal.SetVisible(visible);
        }

        /// <summary>Coloca la bandeja: 80 dp de alto con su borde superior en <paramref name="trayTop"/>, centrada.</summary>
        public void Layout(float canvasW, float trayTop)
        {
            _canvasW = canvasW;
            _trayTop = trayTop;
            float width = Width;
            _tray.sprite = _art.Tray(width);
            float trayX = (canvasW - width) / 2f;
            if (_art.TrayIsPainted)
            {
                _tray.type = Image.Type.Sliced;
                UiBuilder.Place(_tray.rectTransform, trayX, trayTop, width, 80f);
            }
            else
            {
                UiBuilder.Place(_tray.rectTransform, trayX - 8f, trayTop - 2f, width + 16f, 94f);
            }

            // Orden canónico de 07 §7.4, de izquierda a derecha y sin huecos: Marea, Cuia extra y Revelar.
            float x = trayX + PadDp;
            _undo.Place(x, trayTop + 4f);
            if (_extra.Active)
            {
                x += SlotDp + GapDp;
                _extra.Place(x, trayTop + 4f);
            }

            if (_reveal.Active)
            {
                x += SlotDp + GapDp;
                _reveal.Place(x, trayTop + 4f);
            }

            LayoutMissing();
        }

        /// <summary>
        /// Refresca badges, precios y habilitado si el estado cambió. Con la victoria pendiente
        /// (<see cref="LevelFlow.IsResolvingWin"/>: termina el último vertido) no repinta nada y se queda como estaba (D-011,
        /// punto 1: nada se da por ganado antes de verse); sus toques no hacen nada (R15: el flujo no acepta input, y
        /// <c>GameScreen.OnTrayUndo</c>, <c>GameBootstrap.PressExtraCuia</c> y <c>GameBootstrap.PressReveal</c> no actúan ni
        /// terminan antes de tiempo el revelado en marcha). Al empezar la celebración, el estado ya no es el último pintado y se
        /// apaga. Los precios y los avisos «Te faltan {n}» se calculan con el saldo que enseña el de arriba
        /// (<see cref="WalletBar.Refresh"/>): las semillas menos <paramref name="seedsPending"/>, la Cosecha que aún no se ve
        /// (durante la celebración, entera; D-011, punto 1).
        /// </summary>
        public void Refresh(int seedsPending = 0)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || flow.IsResolvingWin)
            {
                return;
            }

            UndoStatus undo = flow.UndoStatus;
            ExtraCuiaOffer extra = flow.ExtraCuia;
            bool video = UndoVideo(undo);
            int seeds = Math.Max(0, (_services?.Wallet.Seeds ?? 0) - Math.Max(0, seedsPending));
            bool undoEnabled = undo.HasQuota ? flow.CanUndo : flow.AcceptsBoardInput;
            int key = Hash(undo.Unlimited ? 1 : 0, undo.FreeLeft, undo.Stock, video ? 1 : 0, undoEnabled ? 1 : 0, (int)extra.State, extra.Stock, extra.Missing, seeds);
            if (key == _key)
            {
                return;
            }

            _key = key;
            bool showExtra = extra.State != ExtraCuiaState.Hidden;
            if (showExtra != _extra.Active)
            {
                _extra.SetActive(showExtra);
                _slots = CountSlots();
                _slotsVersion++;
                Layout(_canvasW, _trayTop);
            }

            // Marea (04 §1.1): ilimitada sin badge; cupo en blanco; stock «+N» en ámbar; sin nada, «+3» con video o el precio.
            UiBuilder.SetEnabled(_undo.Parts.Button, undoEnabled);
            if (undo.Unlimited)
            {
                _undo.ShowBadge(null, false);
                _undo.ShowStockBadge(0);
                _undo.HidePrice();
            }
            else if (undo.HasQuota)
            {
                _undo.ShowBadge(undo.FreeLeft > 0 ? Number(undo.FreeLeft, false) : null, false);
                _undo.ShowStockBadge(undo.Stock);
                _undo.HidePrice();
            }
            else
            {
                int amount = _services?.Wallet.Config.UndoRefillAmount ?? 3;
                int price = _services?.Wallet.Config.UndoRefillSeedPrice ?? 30;
                _undo.ShowStockBadge(0);
                if (video)
                {
                    _undo.ShowBadge(Number(amount, true), true);
                    _undo.HidePrice();
                }
                else
                {
                    _undo.ShowBadge(Number(amount, true), false);
                    _undo.ShowPrice(price, Math.Max(0, price - seeds));
                }
            }

            if (showExtra)
            {
                // Cuia extra (04 §2.2): stock «+N» o precio; gris si ya se usó, sin vertido útil o con 16 cuias.
                UiBuilder.SetEnabled(_extra.Parts.Button, extra.State == ExtraCuiaState.Ready);
                if (extra.FromStock)
                {
                    _extra.ShowBadge(null, false);
                    _extra.ShowStockBadge(extra.Stock);
                    _extra.HidePrice();
                }
                else if (extra.State == ExtraCuiaState.Ready)
                {
                    _extra.ShowBadge(null, false);
                    _extra.ShowStockBadge(0);
                    _extra.ShowPrice(extra.Price, extra.Missing);
                }
                else
                {
                    // Usada en el nivel, sin vertido útil o con 16 cuias: gris y sin precio.
                    _extra.ShowBadge(null, false);
                    _extra.ShowStockBadge(0);
                    _extra.HidePrice();
                }
            }

            LayoutMissing();
        }

        private int CountSlots()
        {
            return 1 + (_extra.Active ? 1 : 0) + (_reveal.Active ? 1 : 0);
        }

        // ¿«+3» de Marea con el icono de video? Solo sin cupo (B2-1). La respuesta de los anuncios se guarda UndoVideoPollMs
        // (Tick descuenta el plazo): preguntar crea la cadena de la fecha del contador diario (08 §5.4). Con cupo o sin
        // servicios se olvida, así que la primera pregunta sin cupo es en el acto.
        private bool UndoVideo(UndoStatus undo)
        {
            GameServices? services = _services;
            if (services == null || undo.HasQuota)
            {
                ForgetUndoVideo();
                return false;
            }

            if (_undoVideoPollMs <= 0f)
            {
                _undoVideoReady = services.RewardedReady(AdPlacement.UndoRefill);
                _undoVideoPollMs = UndoVideoPollMs;
            }

            return _undoVideoReady;
        }

        private void ForgetUndoVideo()
        {
            _undoVideoReady = false;
            _undoVideoPollMs = 0f;
        }

        // «Te faltan {n}» encima de cada ranura sin saldo (104 dp, como desde B2-1); si la vecina también lo enseña, los dos
        // estrechan a su paso de ranura para no pisarse. El texto va a 12 sp, el mínimo de 02 §12, y nunca menos: si no cabe en
        // una línea (una cifra de 3 con el aviso estrecho), la cifra baja a una segunda línea y la placa crece hacia arriba
        // (Slot.PlaceMissing), sin salir del hueco entre el tablero y la bandeja.
        private void LayoutMissing()
        {
            bool undo = _undo.MissingShown;
            bool extra = _extra.Active && _extra.MissingShown;
            bool reveal = _reveal.Active && _reveal.MissingShown;

            // Vecinas en el orden de la bandeja (sin la Cuia extra, Marea y Revelar quedan juntas).
            bool undoNarrow = undo && (extra || (!_extra.Active && reveal));
            bool extraNarrow = extra && (undo || reveal);
            bool revealNarrow = reveal && (extra || (!_extra.Active && undo));
            _undo.PlaceMissing(undoNarrow ? MissingNarrowDp : Slot.MissingWideDp);
            _extra.PlaceMissing(extraNarrow ? MissingNarrowDp : Slot.MissingWideDp);
            _reveal.PlaceMissing(revealNarrow ? MissingNarrowDp : Slot.MissingWideDp);
        }

        private string Number(int value, bool plus)
        {
            int n = 0;
            if (plus)
            {
                _buf[n++] = '+';
            }

            n = UiText.WriteNumber(_buf, n, value, false);
            return new string(_buf, 0, n);
        }

        private static int Hash(int a, int b, int c, int d, int e, int f, int g, int h, int i)
        {
            unchecked
            {
                int x = a;
                x = (x * 397) ^ b;
                x = (x * 397) ^ c;
                x = (x * 397) ^ d;
                x = (x * 397) ^ e;
                x = (x * 397) ^ f;
                x = (x * 397) ^ g;
                x = (x * 397) ^ h;
                return (x * 397) ^ i;
            }
        }

        // Una ranura de 64 dp (guía 3.6): botón de la familia Agua con glifo y etiqueta funcional, badge arriba a la derecha
        // (cupo en blanco o «+3» con video), badge ámbar «+N» del stock arriba a la izquierda, etiqueta de precio montada sobre
        // el borde inferior (como Magic Sort, ficha §2.3) y «Te faltan {n}» encima de la ranura sin saldo.
        private sealed class Slot
        {
            /// <summary>Ancho del aviso «Te faltan {n}» sin vecinas que también lo enseñen (dp).</summary>
            public const float MissingWideDp = SlotDp + 40f;

            // Tamaño del aviso: 12 sp, el mínimo de 02 §12 («Texto ≥ 12 sp»), sin encogerlo nunca (revisión de la limpieza L2b:
            // el autoajuste hasta 11 sp quedaba por debajo); lo que no cabe en una línea va en dos (PlaceMissing).
            private const float MissingSp = 12f;

            // Placa del aviso [P]: con una línea, 18 dp de alto (una línea de Nunito a 12 sp mide 16,4) y su borde de abajo 6 dp
            // sobre el canto de la ranura. Dos líneas solo si la cifra no cabe en una a 12 sp: la placa crece hacia arriba y no
            // a los lados, así que nunca se mete en el aviso de la ranura vecina (02 §12: los contenedores crecen y nada se
            // recorta). El hueco es estrecho: el panel del tablero llega hasta 31,5 dp sobre la bandeja (BoardLayout centra el
            // bloque con ≥ 13,5 dp hasta boardBottom y el panel le suma 22, dentro de los 40 dp entre el tablero y la bandeja) y
            // las ranuras empiezan 4 dp por debajo de ella. Por eso, con dos líneas, el interlineado baja a 1,09 em (−0,28 em:
            // 16,4 + 13 = 29,4 dp, con los glifos a unos 4 dp del borde, como con una) y la placa de 29,5 dp va 4 dp sobre el
            // canto, a ras de la bandeja: queda ≥ 2 dp por debajo del panel y no toca la insignia de la ranura vecina (un disco de
            // 24 dp que asoma 8 dp sobre el canto). L3: con 34 dp a 6 dp del canto, la placa pisaba 4,5 dp el marco del panel en
            // el 106 a 360 × 780.
            private const float MissingGapDp = 6f;
            private const float MissingTwoLinesGapDp = 4f;
            private const float MissingOneLineDp = 18f;
            private const float MissingTwoLinesDp = 29.5f;

            // Interlineado de las dos líneas, en centésimas de em (TextMeshPro): −0,28 em.
            private const float MissingTwoLinesSpacing = -28f;

            // Holgura al decidir si el texto cabe en una línea (dp): la de redondeo de las medidas de TextMeshPro.
            private const float MissingFitSlackDp = 0.5f;

            // Etiqueta de precio: centrada 72 − 3 dp por debajo del canto de arriba de la ranura (montada sobre el borde de
            // abajo) y, como mucho, del ancho de su paso de ranura con 2 dp de aire (70 dp). Un precio de 4 cifras (fuera de los
            // rangos de 08 §9) no cabe: la etiqueta entera se encoge hasta caber, sin que su cifra (16 sp) baje de 12 sp (02 §12).
            private const float PriceCenterDp = 72f - 3f;
            private const float PriceMaxDp = SlotDp + GapDp - 2f;
            private const float PriceMinScale = 12f / 16f;

            private readonly RectTransform _badge;
            private readonly Image _badgeDisc;
            private readonly TextMeshProUGUI _badgeText;
            private readonly Image _badgeVideo;
            private readonly RectTransform _stock;
            private readonly TextMeshProUGUI _stockText;
            private readonly PriceTag _price;
            private readonly RectTransform _missing;
            private readonly TextMeshProUGUI _missingText;
            private readonly char[] _buf = new char[24];
            private bool _visible = true;
            private float _x;
            private float _y;
            private float _missingW = MissingWideDp;
            private int _missingShown = -1;
            private float _missingLineWidth;
            private bool _missingTwoLines;

            public Slot(string name, Transform hud, ProceduralArt art, Sprite icon, Sprite iconDisabled, string label, Action onClick)
            {
                Parts = UiBuilder.Relief(name, hud, art.SlotWater, onClick);
                Glyph = UiBuilder.Image("Icon", Parts.Face, icon, Color.white);
                UiBuilder.Place(Glyph.rectTransform, 16f, 6f, 32f, 32f);
                TextMeshProUGUI text = UiBuilder.Label("Label", Parts.Face, TextStyle.SlotWater, label, 12f, Palette.Cream, TextAlignmentOptions.Center);
                UiBuilder.Place(text.rectTransform, 0f, 41f, 64f, 16f);
                Parts.Relief.Bind(Parts.Face, Parts.Base, Parts.FaceImage, art.SlotWater, Glyph, iconDisabled, text);

                _badge = UiBuilder.Rect("Badge", Parts.Button.transform);
                _badgeDisc = UiBuilder.Image("Disc", _badge, art.Disc, Palette.Cream);
                UiBuilder.Stretch(_badgeDisc.rectTransform);
                _badgeText = UiBuilder.Label("Value", _badge, TextStyle.PlainBold, string.Empty, 13f, Palette.Ink, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_badgeText.rectTransform);
                _badgeVideo = UiBuilder.Image("Video", _badge, art.IconVideo, Color.white);
                _badgeVideo.preserveAspect = true;
                _badge.gameObject.SetActive(false);

                _stock = UiBuilder.Rect("Stock", Parts.Button.transform);
                Image stockDisc = UiBuilder.Sliced("Pill", _stock, art.RoundedRect, Palette.UiAmber);
                stockDisc.pixelsPerUnitMultiplier = 24f / 10f;
                UiBuilder.Stretch(stockDisc.rectTransform);
                _stockText = UiBuilder.Label("Value", _stock, TextStyle.PlainBold, string.Empty, 13f, Palette.Ink, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_stockText.rectTransform);
                _stock.gameObject.SetActive(false);

                _price = new PriceTag("Price", Parts.Button.transform, art);
                _price.Root.gameObject.SetActive(false);

                _missing = UiBuilder.Rect("Missing", hud);
                Image missingPlate = UiBuilder.Sliced("Plate", _missing, art.RoundedRect, Palette.PanelLight);
                missingPlate.pixelsPerUnitMultiplier = 24f / 9f;
                UiBuilder.Stretch(missingPlate.rectTransform);
                _missingText = UiBuilder.Label("Value", _missing, TextStyle.PlainBold, string.Empty, MissingSp, Palette.UiRedWarning, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_missingText.rectTransform);
                _missing.gameObject.SetActive(false);
            }

            public ReliefParts Parts { get; }

            public Image Glyph { get; }

            public bool Active { get; private set; } = true;

            public string BadgeText => (_badge.gameObject.activeSelf ? _badgeText.text : string.Empty) + (_stock.gameObject.activeSelf ? _stockText.text : string.Empty);

            public int PriceShown => _price.Root.gameObject.activeSelf ? _price.Value : 0;

            public bool MissingShown => _missing.gameObject.activeSelf;

            public bool VideoShown => _badge.gameObject.activeSelf && _badgeVideo.gameObject.activeSelf;

            public string MissingText => MissingShown ? _missingText.text : string.Empty;

            public float MissingWidth => _missingW;

            public RectTransform MissingRect => _missing;

            public RectTransform BadgeRect => _badge;

            public TextMeshProUGUI MissingLabel => _missingText;

            public RectTransform PriceRect => _price.Root;

            public void SetActive(bool active)
            {
                Active = active;
                SetVisible(_visible);
            }

            public void SetVisible(bool visible)
            {
                _visible = visible;
                bool on = visible && Active;
                Parts.Button.gameObject.SetActive(on);
                if (!on)
                {
                    _missing.gameObject.SetActive(false);
                }
            }

            public void Place(float x, float y)
            {
                _x = x;
                _y = y;
                UiBuilder.Place((RectTransform)Parts.Button.transform, x, y, SlotDp, 72f);
                UiBuilder.Place(_badge, SlotDp - 16f, -8f, 24f, 24f);
                UiBuilder.Place(_badgeText.rectTransform, 0f, 0f, 24f, 24f);
                PlaceMissing(_missingW);
            }

            public void ShowBadge(string? text, bool video)
            {
                bool on = text != null;
                _badge.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                _badgeText.text = text;
                _badgeVideo.gameObject.SetActive(video);
                float w = video ? 44f : 24f;
                UiBuilder.Place(_badge, SlotDp - w + 8f, -8f, w, 24f);
                _badgeDisc.type = Image.Type.Simple;
                UiBuilder.Place(_badgeText.rectTransform, 0f, 0f, video ? 22f : 24f, 24f);
                if (video)
                {
                    UiBuilder.Place(_badgeVideo.rectTransform, 22f, 3f, 20f, 18f);
                }
            }

            public void ShowStockBadge(int stock)
            {
                bool on = stock > 0;
                _stock.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                int n = 0;
                _buf[n++] = '+';
                n = UiText.WriteNumber(_buf, n, stock, false);
                _stockText.SetCharArray(_buf, 0, n);
                UiBuilder.Place(_stock, -8f, -8f, 14f + (8f * n), 22f);
            }

            public void ShowPrice(int price, int missing)
            {
                _price.Root.gameObject.SetActive(true);
                _price.Set(price, 0, missing > 0);
                float k = Mathf.Clamp(PriceMaxDp / _price.Width, PriceMinScale, 1f);
                _price.Root.localScale = new Vector3(k, k, 1f);
                _price.PlaceAt((SlotDp - (_price.Width * k)) * 0.5f, PriceCenterDp - (PriceTag.HeightDp * 0.5f * k));
                _missing.gameObject.SetActive(missing > 0 && Parts.Button.gameObject.activeSelf);
                if (missing > 0 && missing != _missingShown)
                {
                    _missingShown = missing;
                    string text = string.Format(System.Globalization.CultureInfo.InvariantCulture, UiText.Missing, UiText.Grouped(missing));
                    _missingText.text = text;

                    // Ancho en una línea a 12 sp, solo cuando cambia la cifra.
                    _missingLineWidth = _missingText.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
                }

                if (missing > 0)
                {
                    PlaceMissing(_missingW);
                }
            }

            public void HidePrice()
            {
                _price.Root.gameObject.SetActive(false);
                _missing.gameObject.SetActive(false);
            }

            // «Te faltan {n}» de widthDp centrado sobre la ranura: en una línea si a 12 sp cabe en widthDp, con su borde de abajo
            // 6 dp por encima del canto, y, si no, en dos («Te faltan» y la cifra), con la placa más alta y más cerca del canto.
            public void PlaceMissing(float widthDp)
            {
                _missingW = widthDp;
                bool twoLines = _missingLineWidth > widthDp - MissingFitSlackDp;
                if (twoLines != _missingTwoLines)
                {
                    _missingTwoLines = twoLines;
                    _missingText.textWrappingMode = twoLines ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                    _missingText.lineSpacing = twoLines ? MissingTwoLinesSpacing : 0f;
                }

                float h = twoLines ? MissingTwoLinesDp : MissingOneLineDp;
                float gap = twoLines ? MissingTwoLinesGapDp : MissingGapDp;
                UiBuilder.Place(_missing, _x + ((SlotDp - widthDp) * 0.5f), _y - gap - h, widthDp, h);
            }
        }
    }
}
