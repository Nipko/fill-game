#nullable enable
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-4 «La tripulación» (D-069, P6 (a); <c>docs/dev/propuesta-mapa-rio-v2.md</c> §6, §7.2 y §7.3),
    /// parcial de <see cref="RiverMapScreen"/>. Con el Mapa v2, la canoa de Capi y <c>ui_map_crew</c> (<see cref="CrewRequested"/>):
    /// <list type="bullet">
    /// <item><description><b>A bordo</b> (<see cref="RiverCrew"/>, deducido de <c>max_level</c>): el tucán, polizón en la proa
    /// (<see cref="RiverCrew.ToucanDp"/> dp, mirando hacia delante), y el tucuxi, que nada junto a Rosa. Al cambiar de mundo se quedan
    /// en su tramo (en la parada donde se unieron) y se les ve allí en la vista Mundo.</description></item>
    /// <item><description><b>Presentación</b> (§6.2): la primera vez que se abre el Mapa tras su punto (<c>map_seen_level</c> &lt; su
    /// nivel), al cruzarlo en el viaje (o al salir, si el viaje empieza más allá), llega volando o nadando y se acomoda en
    /// <see cref="CrewIntroMs"/> ms; sale el chip «¡Se une al viaje! · {especie}» <see cref="CrewChipMs"/> ms, Capi hace ♥ y TalkBack
    /// lo anuncia («Se une al viaje: {especie}»). Con Reducir movimiento aparece en su sitio y sale el chip.</description></item>
    /// <item><description><b>Llegada</b> (§6.3): el tucán abre el pico y aletea, y el tucuxi salta (Rosa solo asoma, como el boto real:
    /// D-029, F11; respuesta del dueño a la pregunta 3 de M2-4), en
    /// <see cref="CrewHopMs"/> ms, a la vez que el remo en alto de Capi: «Jugar» no espera nada más (0 ms sobre D-053).</description></item>
    /// <item><description><b>Cameo de los miquitos</b> (§6.1, regla 4): a mitad de un viaje, uno del tramo saluda desde un árbol de la
    /// orilla; nunca sube a la canoa.</description></item>
    /// </list>
    /// El atlas (<see cref="MapCrewArt"/>, 128 KiB) se pinta al abrir y se suelta al cerrar; por fotograma solo se mueven rectángulos y
    /// opacidades, y solo si cambian: sin asignaciones. Con <c>ui_map_crew</c> apagada, el Mapa queda como en M2-3.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Lo que tarda un compañero en llegar y acomodarse al unirse (ms) [P].</summary>
        public const float CrewIntroMs = 700f;

        /// <summary>Lo que se ve el chip «¡Se une al viaje!» (ms; §6.2).</summary>
        public const float CrewChipMs = 2000f;

        /// <summary>Entrada del chip (ms) [P].</summary>
        public const float CrewChipInMs = 200f;

        /// <summary>Salida del chip (ms) [P].</summary>
        public const float CrewChipOutMs = 300f;

        /// <summary>El salto o el aleteo de la tripulación en la llegada (ms; §6.3: ≤ 400 ms, a la vez que el remo en alto).</summary>
        public const float CrewHopMs = 400f;

        /// <summary>Lo que sube el tucuxi en el salto de la llegada (dp de la vista Mundo) [P].</summary>
        public const float CrewJumpDp = 8f;

        /// <summary>Lo que dura el saludo del miquito desde su árbol (ms) [P].</summary>
        public const float CameoWaveMs = 1200f;

        /// <summary>Lo que se queda el árbol con el miquito a la vista, con su entrada y el saludo (ms) [P].</summary>
        public const float CameoShowMs = 2400f;

        /// <summary>Fundidos de entrada y de salida del cameo (ms) [P].</summary>
        public const float CameoFadeMs = 250f;

        /// <summary>Escala del árbol del cameo en la vista Mundo (el árbol del atlas mide 30 × 34 dp) [P].</summary>
        public const float CameoTreeScale = 1f;

        /// <summary>Escala del miquito sobre su árbol (el del atlas mide 10 × 12 dp) [P].</summary>
        public const float CameoMonkeyScale = 1.3f;

        /// <summary>Distancia del árbol del cameo al eje del río, por la orilla (dp de la vista Mundo) [P].</summary>
        public const float CameoBankDp = 24f;

        /// <summary>Margen del chip con el borde de arriba de la zona del mapa (dp) [P].</summary>
        public const float CrewChipInsetDp = 12f;

        // Dónde va el tucán en la canoa (dp de diseño de la canoa de 60 × 44): las patas sobre la borda de la proa, y su escala para
        // que con el pico mida RiverCrew.ToucanDp.
        private static readonly Vector2 ToucanFeet = new Vector2(52.5f, 15.2f);
        private const float ToucanScale = RiverCrew.ToucanDp / 19.8f;

        /// <summary>El tucuxi junto a Rosa: un poco por detrás (× la orientación de la canoa) y más abajo (dp de la vista Mundo; también en el banner) [P].</summary>
        internal static readonly Vector2 TucuxiBesideRosa = new Vector2(-10f, -6f);

        // Dónde se quedan en su tramo [P]: donde se unieron, en el punto de la canoa con su nivel ganado (por el agua: el tucuxi en el
        // cauce del Negro, lejos del lago de Tefé, D-069 «Puntos sensibles»), el tucán apartado a la orilla (dp de la vista Mundo).
        private static readonly Vector2 ToucanHomeOffset = new Vector2(18f, -14f);

        private readonly MapCrewArt _crewArt = new MapCrewArt();
        private bool _crewOn;
        private int _crewIntro = -1; // el compañero que se presenta en esta apertura (CrewKind), o −1
        private bool _crewIntroFired;
        private float _crewIntroMs = -1f;
        private float _crewChipMs = -1f;
        private float _crewHopMs = -1f;
        private float _cameoLevel = -1f;
        private bool _cameoFired;
        private float _cameoMs = -1f;
        private bool _cameoStill;
        private int _crewAnnounced = -1;

        private CrewToucanView? _toucan;
        private CrewToucanView? _toucanHome;
        private Image? _tucuxi;
        private Image? _tucuxiHome;
        private RectTransform? _crewLayer;
        private RectTransform? _cameo;
        private Image? _cameoTree;
        private Image? _cameoBody;
        private Image? _cameoHead;
        private Image? _cameoArm;
        private RectTransform? _crewChip;
        private Image? _crewChipPlate;
        private TextMeshProUGUI? _crewChipLabel;

        /// <summary><c>ui_map_crew</c> (08 §9) antes de cada apertura (lo pone <see cref="GameBootstrap"/>); solo cuenta con el Mapa v2.</summary>
        public bool CrewRequested { get; set; } = true;

        /// <summary>Esta apertura lleva la tripulación (el Mapa v2 con la canoa de Capi y <c>ui_map_crew</c>).</summary>
        public bool CrewActive => _crewOn;

        /// <summary>El atlas de la tripulación (tests y sonda).</summary>
        internal MapCrewArt CrewArt => _crewArt;

        /// <summary>El compañero que se presenta en esta apertura, o null.</summary>
        public CrewKind? CrewIntroKind => _crewIntro >= 0 ? (CrewKind)_crewIntro : (CrewKind?)null;

        /// <summary>La presentación ya salió (o salió sin animar).</summary>
        public bool CrewIntroFired => _crewIntroFired;

        /// <summary>El compañero está llegando ahora (la animación de la presentación).</summary>
        public bool CrewArriving => _crewIntroMs >= 0f;

        /// <summary>El chip «¡Se une al viaje!» se ve.</summary>
        public bool CrewChipShown => _crewChip != null && _crewChip.gameObject.activeSelf && _crewChipMs >= 0f;

        /// <summary>El texto del chip.</summary>
        public string CrewChipText => _crewChipLabel != null ? _crewChipLabel.text : string.Empty;

        /// <summary>El chip (tests: su caja).</summary>
        internal RectTransform? CrewChipRect => _crewChip;

        /// <summary>La tripulación salta o aletea ahora (la llegada).</summary>
        public bool CrewHopping => _crewHopMs >= 0f;

        /// <summary>El tucán va en la proa (a la vista).</summary>
        public bool ToucanAboardShown => _toucan != null && _toucan.Root.gameObject.activeInHierarchy;

        /// <summary>El tucuxi nada junto a Rosa (a la vista).</summary>
        public bool TucuxiAboardShown => _tucuxi != null && _tucuxi.gameObject.activeInHierarchy;

        /// <summary>El tucán se ve en su tramo (Alter do Chão).</summary>
        public bool ToucanHomeShown => _toucanHome != null && _toucanHome.Root.gameObject.activeInHierarchy;

        /// <summary>El tucuxi se ve en su tramo.</summary>
        public bool TucuxiHomeShown => _tucuxiHome != null && _tucuxiHome.gameObject.activeInHierarchy;

        /// <summary>El tucán de la proa (tests y capturas).</summary>
        internal RectTransform? ToucanRect => _toucan?.Root;

        /// <summary>La mandíbula de arriba del tucán de la proa (tests: abre el pico).</summary>
        internal RectTransform? ToucanBeakRect => _toucan?.Upper.rectTransform;

        /// <summary>El tucuxi junto a Rosa (tests y capturas).</summary>
        internal Image? TucuxiImage => _tucuxi;

        /// <summary>El árbol del cameo con su miquito se ve.</summary>
        public bool CameoShown => _cameo != null && _cameo.gameObject.activeInHierarchy;

        /// <summary>El miquito del cameo saluda ahora.</summary>
        public bool CameoWaving => _cameoMs >= 0f && (_cameoStill || _cameoMs < CameoFadeMs + CameoWaveMs);

        /// <summary>El cameo (tests y capturas).</summary>
        internal RectTransform? CameoRect => _cameo;

        /// <summary>Nivel (fraccionario) del viaje en que saluda el miquito (−1 sin cameo).</summary>
        public float CameoLevel => _cameoLevel;

        // ============================================================================================ apertura y cierre

        // Al principio de CapiOpen: la tripulación de esta apertura, sin presentación todavía.
        private void CrewBegin()
        {
            _crewIntro = -1;
            _crewIntroFired = false;
            _crewIntroMs = -1f;
            _crewChipMs = -1f;
            _crewHopMs = -1f;
            _cameoLevel = -1f;
            _cameoFired = false;
            _cameoMs = -1f;
            _cameoStill = false;
            _crewAnnounced = -1;
            _crewOn = _live && CrewRequested && _hasCanoe && _progress != null;
            if (!_crewOn)
            {
                return;
            }

            int max = Mathf.Clamp(_progress!.MaxLevel, 0, WorldTable.LastLevel);
            int seen = ModesCatchUpSeen(Mathf.Clamp(_catchSeen, 0, max)); // también el viaje que se repite (D-053)
            for (int i = 0; i < RiverCrew.Count; i++)
            {
                CrewMember m = RiverCrew.At(i);
                if (RiverCrew.IntroPending(m, seen, max, true))
                {
                    _crewIntro = (int)m.Kind; // el más reciente, si hubiera dos
                }
            }
        }

        // En PlanCapiEvents: la llegada del compañero, al cruzar su nivel (o al salir, si el viaje empieza más allá), con ♥.
        private void CrewPlanEvents(ref int mask)
        {
            if (!_crewOn || _crewIntro < 0)
            {
                return;
            }

            CrewMember m = RiverCrew.Of((CrewKind)_crewIntro);
            float at = Mathf.Min(_catchTo, Mathf.Max(m.JoinLevel, _catchFrom + 0.001f));
            mask |= AddCapiEvent(at, CapiMoment.Join, _crewIntro);
        }

        // En FireCapiEvent: lo que hace la tripulación en cada momento del viaje.
        private void CrewReact(CapiMoment moment, int arg)
        {
            if (!_crewOn)
            {
                return;
            }

            if (moment == CapiMoment.Join && arg == _crewIntro && !_crewIntroFired)
            {
                StartCrewIntro(!ProgressReduceMotion);
            }
            else if (moment == CapiMoment.Arrival && !ProgressReduceMotion)
            {
                _crewHopMs = 0f;
            }
        }

        // Al final de Open, tras CapiOpen: el atlas, las piezas, el miquito del cameo y, sin viaje, la presentación en su sitio.
        partial void CrewOpen(MapOpenSource source)
        {
            if (!_crewOn || _capiRig == null)
            {
                _crewOn = false;
                HideCrew();
                _crewArt.Release();
                return;
            }

            bool still = ProgressReduceMotion;
            FaunaPalette? monkey = null;
            // Un protagonista por viaje [P]: si alguien se une, el miquito no saluda en ese viaje.
            float cameo = _crewIntro >= 0 ? -1f : RiverCrew.CameoLevel(_catchFrom, _catchTo, true);
            if (cameo >= 0f && Fauna != null)
            {
                int lv = Mathf.Clamp(Mathf.CeilToInt(cameo), 1, WorldTable.LastLevel);
                FaunaSpecies? sp = Fauna.IsNoFauna(lv) ? null : Fauna.ForLevel(lv);
                if (sp != null)
                {
                    monkey = sp.Palette;
                    _cameoLevel = cameo;
                }
            }

            _crewArt.Build(monkey);
            EnsureCrewViews();
            SetCrewSprites();
            if (_crewIntro >= 0)
            {
                PrepareCrewChip((CrewKind)_crewIntro);
            }

            if (still)
            {
                // Reducir movimiento (§6.2 y §7.3): sin viaje, el compañero aparece en su sitio y sale el chip; el miquito, quieto.
                if (_crewIntro >= 0)
                {
                    StartCrewIntro(false);
                }

                if (_cameoLevel >= 0f)
                {
                    StartCameo(true);
                }
            }
            else if (_crewIntro >= 0 && !_catchUp)
            {
                StartCrewIntro(true); // sin viaje que cruzar su nivel (no debería pasar: con él pendiente siempre hay tramo)
            }

            ApplyCrew(true);
        }

        partial void CrewClose()
        {
            HideCrew();
            ClearCrewSprites();
            _crewArt.Release();
            _crewOn = false;
        }

        /// <summary>Suelta el atlas sin tocar la pantalla (como <see cref="ReleaseCapiArt"/>).</summary>
        internal void ReleaseCrewArt()
        {
            _crewArt.Release();
        }

        /// <summary>Miembros extra de <c>AF_PERF mem</c> al abrir el Mapa (D-069, M2-4): el atlas de la tripulación.</summary>
        internal string CrewMemJson()
        {
            return ",\"crew_tex_kib\":" + (_crewArt.TextureBytes / 1024L).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // AF_STATE map_journey (D-069, M2-4; §9.3): el compañero que se presenta en este Mapa («toucan» o «tucuxi»), o null. Se deduce
        // como en CrewBegin, sin esperar a él: la sonda escribe al abrir (Reducir movimiento) o al acabar el viaje, antes que la tripulación.
        private string? CrewJoinedId()
        {
            if (!_live || !CrewRequested || !_hasCanoe || _progress == null)
            {
                return null;
            }

            int max = Mathf.Clamp(_progress.MaxLevel, 0, WorldTable.LastLevel);
            int seen = ModesCatchUpSeen(Mathf.Clamp(_catchSeen, 0, max));
            string? id = null;
            for (int i = 0; i < RiverCrew.Count; i++)
            {
                CrewMember m = RiverCrew.At(i);
                if (RiverCrew.IntroPending(m, seen, max, true))
                {
                    id = m.Kind == CrewKind.Tucuxi ? "tucuxi" : "toucan";
                }
            }

            return id;
        }

        // ============================================================================================ cada fotograma

        partial void CrewTick(float dtMs, ref bool animating)
        {
            if (!_crewOn)
            {
                return;
            }

            float dt = Mathf.Max(0f, dtMs);
            if (_crewIntro >= 0 && !_crewIntroFired && !_catchUp)
            {
                _crewIntroFired = true; // al volver de segundo plano (02 §14) el viaje saltó al final: a bordo, sin animar
            }

            if (_cameoLevel >= 0f && !_cameoFired && _catchUp && _canoeX + 1e-4f >= _cameoLevel)
            {
                StartCameo(false);
            }

            Advance(ref _crewIntroMs, dt, CrewIntroMs);
            Advance(ref _crewChipMs, dt, CrewChipMs);
            Advance(ref _crewHopMs, dt, CrewHopMs);
            Advance(ref _cameoMs, dt, _cameoStill ? CapiDirector.StillMs + CameoFadeMs : CameoShowMs);
            ApplyCrew(false);
            if (_crewIntroMs >= 0f || _crewChipMs >= 0f || _crewHopMs >= 0f || _cameoMs >= 0f)
            {
                animating = true;
            }
        }

        private static void Advance(ref float ms, float dt, float end)
        {
            if (ms < 0f)
            {
                return;
            }

            ms += dt;
            if (ms >= end)
            {
                ms = -1f;
            }
        }

        // ============================================================================================ momentos

        private void StartCrewIntro(bool animate)
        {
            _crewIntroFired = true;
            _crewIntroMs = animate ? 0f : -1f;
            _crewChipMs = 0f;
            if (_crewChip != null)
            {
                PlaceCrewChip();
                _crewChip.gameObject.SetActive(true);
                _crewChip.SetAsLastSibling();
            }

            AnnounceCrewJoin();
        }

        // TalkBack: «Se une al viaje: tucán» (§7.3: el chip de «se une» se anuncia), una vez por apertura.
        private void AnnounceCrewJoin()
        {
            A11yService? a11y = A11y;
            if (a11y == null || _crewIntro < 0 || _crewAnnounced == _crewIntro)
            {
                return;
            }

            _crewAnnounced = _crewIntro;
            a11y.Invalidate();
            if (a11y.On)
            {
                a11y.Announce(UiText.A11yMapCrewJoin((CrewKind)_crewIntro));
            }
        }

        // El miquito saluda desde un árbol de la orilla, junto a donde pasa la canoa ahora (con Reducir movimiento, quieto).
        private void StartCameo(bool still)
        {
            _cameoFired = true;
            if (_cameo == null || _map == null || _routes == null || _cameoBody == null || _cameoBody.sprite == null)
            {
                return;
            }

            float level = still ? _catchTo : _canoeX;
            Vector2 a = _space.ToLocal(CanoeAt(_map, _routes, Mathf.Max(0f, level - 0.15f)));
            Vector2 b = _space.ToLocal(CanoeAt(_map, _routes, level + 0.15f));
            Vector2 d = b - a;
            Vector2 n = d.sqrMagnitude > 1e-6f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
            if (n.y < 0f)
            {
                n = -n; // la orilla de arriba: el árbol no tapa la canoa ni su rótulo de abajo
            }

            float k = _view == MapView.World ? 1f : GuideBasinDp / GuideWorldDp;
            Vector2 canoe = _space.ToLocal(CanoeAt(_map, _routes, level));
            _cameo.anchoredPosition = canoe + (n * CameoBankDp * k);
            float s = CameoTreeScale * k;
            _cameo.localScale = new Vector3(s, s, 1f);
            _cameo.localRotation = Quaternion.Euler(0f, 0f, _figureAngle);
            _cameoStill = still;
            _cameoMs = 0f;
        }

        // ============================================================================================ piezas

        private void EnsureCrewViews()
        {
            if (_capiRig != null && (_toucan == null || _toucan.Root.parent != _capiRig.Root))
            {
                _toucan = new CrewToucanView("Toucan", _capiRig.Root);
            }

            if (_tucuxi == null && _canoeGear != null)
            {
                _tucuxi = CrewImage("Tucuxi", _canoeGear, CrewPiece.Tucuxi);
            }

            if (_tucuxi != null && _guide != null)
            {
                _tucuxi.transform.SetSiblingIndex(_guide.transform.GetSiblingIndex()); // detrás de Rosa
            }

            if (_crewLayer == null)
            {
                _crewLayer = UiBuilder.Rect("Crew", _dynamicLayer);
                FillTopLeft(_crewLayer);
                _toucanHome = new CrewToucanView("ToucanHome", _crewLayer);
                _toucanHome.Root.anchorMin = new Vector2(0f, 1f);
                _toucanHome.Root.anchorMax = new Vector2(0f, 1f);
                _tucuxiHome = CrewImage("TucuxiHome", _crewLayer, CrewPiece.Tucuxi);
                _cameo = UiBuilder.Rect("Cameo", _crewLayer);
                _cameo.anchorMin = new Vector2(0f, 1f);
                _cameo.anchorMax = new Vector2(0f, 1f);
                _cameo.pivot = new Vector2(0.5f, 0f);
                _cameo.sizeDelta = Vector2.zero;
                _cameoTree = CrewImage("Tree", _cameo, CrewPiece.Tree);
                _cameoBody = CrewImage("MonkeyBody", _cameo, CrewPiece.MonkeyBody);
                _cameoHead = CrewImage("MonkeyHead", _cameo, CrewPiece.MonkeyHead);
                _cameoArm = CrewImage("MonkeyArm", _cameo, CrewPiece.MonkeyArm);
                _cameo.gameObject.SetActive(false);
            }

            if (_canoeGear != null)
            {
                _crewLayer.SetSiblingIndex(_canoeGear.GetSiblingIndex()); // debajo de la canoa y de su equipo
            }

            if (_crewChip == null)
            {
                _crewChip = UiBuilder.Rect("CrewChip", _frame.transform);
                _crewChipPlate = UiBuilder.Sliced("Plate", _crewChip, _art.WoodPlate, Color.white);
                _crewChipPlate.raycastTarget = false;
                UiBuilder.Stretch(_crewChipPlate.rectTransform);
                _crewChipLabel = UiBuilder.Label("Label", _crewChip, TextStyle.PlainBold, string.Empty, RemainingSp, Palette.Cream, TextAlignmentOptions.Center);
                _crewChipLabel.raycastTarget = false;
                _crewChipLabel.textWrappingMode = TextWrappingModes.NoWrap;
                _crewChip.gameObject.SetActive(false);
            }
        }

        private static Image CrewImage(string name, RectTransform parent, CrewPiece piece)
        {
            Image img = UiBuilder.Image(name, parent, null, Color.white);
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = ProceduralArt.CrewPivot(piece);
            RectInt c = ProceduralArt.CrewCell(piece);
            rt.sizeDelta = new Vector2(c.width / ProceduralArt.UiPxPerDp, c.height / ProceduralArt.UiPxPerDp);
            img.gameObject.SetActive(false);
            return img;
        }

        private void SetCrewSprites()
        {
            _toucan?.SetArt(_crewArt);
            _toucanHome?.SetArt(_crewArt);
            SetSprite(_tucuxi, _crewArt.Piece(CrewPiece.Tucuxi));
            SetSprite(_tucuxiHome, _crewArt.Piece(CrewPiece.Tucuxi));
            SetSprite(_cameoTree, _crewArt.Piece(CrewPiece.Tree));
            SetSprite(_cameoBody, _crewArt.Piece(CrewPiece.MonkeyBody));
            SetSprite(_cameoHead, _crewArt.Piece(CrewPiece.MonkeyHead));
            SetSprite(_cameoArm, _crewArt.Piece(CrewPiece.MonkeyArm));
            if (_cameoTree != null && _cameoBody != null && _cameoHead != null && _cameoArm != null)
            {
                // El miquito en la rama del árbol (coordenadas del árbol: el pie del tronco en el origen).
                Vector2 perch = ProceduralArt.TreePerch - new Vector2(15f, 0f);
                _cameoTree.rectTransform.anchorMin = Vector2.zero;
                _cameoTree.rectTransform.anchorMax = Vector2.zero;
                _cameoTree.rectTransform.anchoredPosition = Vector2.zero;
                SetActive(_cameoTree, true);
                Place(_cameoBody, perch, 0f);
                var monkey = new Vector3(CameoMonkeyScale, CameoMonkeyScale, 1f); // que se lea junto al árbol
                _cameoBody.rectTransform.localScale = monkey;
                _cameoHead.rectTransform.localScale = monkey;
                _cameoArm.rectTransform.localScale = monkey;

                Place(_cameoHead, perch + (((ProceduralArt.MonkeyNeck - new Vector2(5f, 6f)) + new Vector2(0f, 3.4f)) * CameoMonkeyScale), 0f);
                Place(_cameoArm, perch + ((ProceduralArt.MonkeyShoulder - new Vector2(5f, 6f)) * CameoMonkeyScale), 70f);
            }
        }

        private void ClearCrewSprites()
        {
            _toucan?.SetArt(null);
            _toucanHome?.SetArt(null);
            SetSprite(_tucuxi, null);
            SetSprite(_tucuxiHome, null);
            SetSprite(_cameoTree, null);
            SetSprite(_cameoBody, null);
            SetSprite(_cameoHead, null);
            SetSprite(_cameoArm, null);
        }

        private static void SetSprite(Image? img, Sprite? sprite)
        {
            if (img != null)
            {
                img.sprite = sprite;
                img.enabled = sprite != null;
            }
        }

        private static void Place(Image img, Vector2 at, float deg)
        {
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = at;
            rt.localRotation = Quaternion.Euler(0f, 0f, deg);
            if (!img.gameObject.activeSelf)
            {
                img.gameObject.SetActive(true);
            }
        }

        private void HideCrew()
        {
            _toucan?.SetVisible(false);
            _toucanHome?.SetVisible(false);
            SetActive(_tucuxi, false);
            SetActive(_tucuxiHome, false);
            if (_cameo != null && _cameo.gameObject.activeSelf)
            {
                _cameo.gameObject.SetActive(false);
            }

            if (_crewChip != null && _crewChip.gameObject.activeSelf)
            {
                _crewChip.gameObject.SetActive(false);
            }
        }

        private static void SetActive(Image? img, bool on)
        {
            if (img != null && img.gameObject.activeSelf != on)
            {
                img.gameObject.SetActive(on);
            }
        }

        // El chip «¡Se une al viaje! · {especie}» (al abrir: el texto y su medida; mostrarlo después no asigna).
        private void PrepareCrewChip(CrewKind kind)
        {
            if (_crewChip == null || _crewChipLabel == null)
            {
                return;
            }

            _crewChipLabel.text = UiText.MapCrewJoin(kind);
            Vector2 pref = _crewChipLabel.GetPreferredValues(_crewChipLabel.text, float.PositiveInfinity, float.PositiveInfinity);
            float labelW = Mathf.Min(Mathf.Ceil(pref.x) + 2f, Mathf.Max(MinTouchDp, _viewW - (2f * CrewChipInsetDp) - (2f * RemainingLabelXDp)));
            float h = Mathf.Max(RowDp - 8f, Mathf.Ceil(pref.y) + (2f * RemainingChipPadDp));
            _crewChip.sizeDelta = new Vector2(labelW + (2f * RemainingLabelXDp), h);
            UiBuilder.Place(_crewChipLabel.rectTransform, RemainingLabelXDp, RemainingChipPadDp, labelW, h - (2f * RemainingChipPadDp));
            UiBuilder.AutoSize(_crewChipLabel, UiBuilder.MinTextSp, RemainingSp);
        }

        // Arriba en el centro de la zona del mapa; si pisaría «Andes ↑» o la placa de «Faltan N», justo debajo.
        private void PlaceCrewChip()
        {
            if (_crewChip == null)
            {
                return;
            }

            Vector2 size = _crewChip.sizeDelta;
            var chip = new Rect((_viewW - size.x) * 0.5f, CrewChipInsetDp, size.x, size.y);
            Rect andes = FixedViewRect(AndesLabel);
            if (andes.width > 0f && andes.Overlaps(chip))
            {
                chip.y = andes.yMax + 4f;
            }

            Rect plate = _remainingChipView;
            if (plate.width > 0f && plate.Overlaps(chip))
            {
                chip.y = plate.yMax + 4f;
            }

            UiBuilder.Place(_crewChip, ViewPadDp + chip.x, ViewPadDp + chip.y, size.x, size.y);
        }

        partial void CrewLayout()
        {
            if (CrewChipShown)
            {
                PlaceCrewChip();
            }
        }

        // ============================================================================================ pintar

        // Coloca y anima la tripulación con el nivel que enseña la canoa. Solo escribe lo que cambia (el lienzo no se rehace en
        // reposo). Sin asignaciones.
        private void ApplyCrew(bool force)
        {
            if (!_crewOn || _progress == null)
            {
                return;
            }

            int level = Mathf.Clamp(Mathf.FloorToInt(_canoeX + 1e-4f), 0, WorldTable.LastLevel);
            bool world = _view == MapView.World;
            ApplyToucan(level);
            ApplyTucuxi(level);
            ApplyHomes(level, world);
            ApplyCameo();
            ApplyChip();
        }

        private bool Present(CrewKind kind, int level)
        {
            return RiverCrew.IsAboardCapped(kind, level, true) && (_crewIntro != (int)kind || _crewIntroFired);
        }

        private float IntroT(CrewKind kind)
        {
            return _crewIntro == (int)kind && _crewIntroMs >= 0f ? Mathf.Clamp01(_crewIntroMs / CrewIntroMs) : 1f;
        }

        private float HopT => _crewHopMs >= 0f ? Mathf.Clamp01(_crewHopMs / CrewHopMs) : -1f;

        private void ApplyToucan(int level)
        {
            CrewToucanView? t = _toucan;
            if (t == null)
            {
                return;
            }

            bool on = Present(CrewKind.Toucan, level) && t.HasArt;
            t.SetVisible(on);
            if (!on)
            {
                return;
            }

            // Llega volando desde atrás y arriba, aleteando, y se posa (§6.2); en la llegada, un saltito, abre el pico y aletea (§6.3).
            float intro = IntroT(CrewKind.Toucan);
            float ease = Ease.OutCubic(intro);
            Vector2 from = new Vector2(-30f, 26f);
            Vector2 at = ToucanFeet + (from * (1f - ease)) + new Vector2(0f, 8f * Mathf.Sin(intro * Mathf.PI) * (1f - intro));
            float wing = intro < 1f ? 55f * Mathf.Sin(intro * 6f * Mathf.PI) : 0f;
            float beak = 0f;
            float hop = HopT;
            if (hop >= 0f)
            {
                float s = Mathf.Sin(hop * Mathf.PI);
                at.y += 2.5f * s;
                beak = s;
                wing = 40f * Mathf.Sin(hop * 4f * Mathf.PI) * s;
            }

            t.Pose(at, ToucanScale, wing, beak, intro < 1f ? Mathf.Clamp01(intro * 3f) : 1f);
        }

        private void ApplyTucuxi(int level)
        {
            Image? tx = _tucuxi;
            Image? guide = _guide;
            if (tx == null)
            {
                return;
            }

            bool on = Present(CrewKind.Tucuxi, level) && tx.sprite != null && guide != null && guide.gameObject.activeInHierarchy && _guideShown;
            SetActive(tx, on);
            if (!on)
            {
                return;
            }

            // Nada junto a Rosa; al presentarse, llega nadando desde atrás y aparece; en la llegada salta él solo y Rosa asoma, como el
            // boto real (D-029, F11; respuesta del dueño a la pregunta 3 de M2-4): Rosa no se mueve.
            float k = _view == MapView.World ? 1f : GuideBasinDp / GuideWorldDp;
            float intro = IntroT(CrewKind.Tucuxi);
            float ease = Ease.OutCubic(intro);
            Vector2 offset = new Vector2(TucuxiBesideRosa.x * _canoeFacing, TucuxiBesideRosa.y) * k;
            offset += new Vector2(-_canoeFacing * 30f * k * (1f - ease), 0f);
            float tilt = 0f;
            float hop = HopT;
            if (hop >= 0f)
            {
                float s = Mathf.Sin(hop * Mathf.PI);
                offset += new Vector2(0f, CrewJumpDp * k * s);
                tilt = 22f * Mathf.Cos(hop * Mathf.PI) * -_canoeFacing;
            }

            RectTransform r = tx.rectTransform;
            float w = RiverCrew.TucuxiDp * k;
            CrewDraw.SetSize(r, new Vector2(w, w * (ProceduralArt.CrewCell(CrewPiece.Tucuxi).height / (float)ProceduralArt.CrewCell(CrewPiece.Tucuxi).width)));
            CrewDraw.SetPos(r, guide!.rectTransform.anchoredPosition + Turn(offset, _figureAngle));
            CrewDraw.SetScale(r, new Vector3(-_canoeFacing, 1f, 1f));
            CrewDraw.SetRotation(r, _figureAngle + tilt);
            CrewDraw.SetAlpha(tx, intro < 1f ? Mathf.Clamp01(intro * 2f) : 1f);
        }

        private void ApplyHomes(int level, bool world)
        {
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            bool toucan = world && map != null && routes != null && RiverCrew.StaysBehind(RiverCrew.Of(CrewKind.Toucan), level, true);
            bool tucuxi = world && map != null && routes != null && RiverCrew.StaysBehind(RiverCrew.Of(CrewKind.Tucuxi), level, true);
            if (_toucanHome != null)
            {
                _toucanHome.SetVisible(toucan && _toucanHome.HasArt);
                if (toucan && _toucanHome.HasArt)
                {
                    Vector2 at = HomePoint(map!, routes!, RiverCrew.ToucanJoinLevel) + ToucanHomeOffset;
                    _toucanHome.Pose(at, ToucanScale, 0f, 0f, 1f);
                }
            }

            if (_tucuxiHome != null)
            {
                bool on = tucuxi && _tucuxiHome.sprite != null;
                SetActive(_tucuxiHome, on);
                if (on)
                {
                    RectTransform r = _tucuxiHome.rectTransform;
                    float w = RiverCrew.TucuxiDp;
                    CrewDraw.SetSize(r, new Vector2(w, w * (ProceduralArt.CrewCell(CrewPiece.Tucuxi).height / (float)ProceduralArt.CrewCell(CrewPiece.Tucuxi).width)));
                    CrewDraw.SetPos(r, HomePoint(map!, routes!, RiverCrew.TucuxiJoinLevel)); // en el cauce
                    CrewDraw.SetRotation(r, _figureAngle);
                }
            }
        }

        // Donde se unió: el punto de la canoa con su nivel ganado (21: saliendo de Alter do Chão; 51: saliendo de Manaos por el Negro),
        // por el agua, en coordenadas locales del contenido.
        private Vector2 HomePoint(RiverMap map, RiverRoutes routes, int joinLevel)
        {
            return _space.ToLocal(CanoeAt(map, routes, joinLevel));
        }

        /// <summary>El tucuxi que se quedó en su tramo (tests: dónde está), o null.</summary>
        internal RectTransform? TucuxiHomeRect => _tucuxiHome != null ? _tucuxiHome.rectTransform : null;

        /// <summary>El tucán que se quedó en su tramo (tests), o null.</summary>
        internal RectTransform? ToucanHomeRect => _toucanHome?.Root;

        private void ApplyCameo()
        {
            if (_cameo == null)
            {
                return;
            }

            bool on = _cameoMs >= 0f;
            if (_cameo.gameObject.activeSelf != on)
            {
                _cameo.gameObject.SetActive(on);
            }

            if (!on)
            {
                return;
            }

            float total = _cameoStill ? CapiDirector.StillMs + CameoFadeMs : CameoShowMs;
            float a = _cameoStill ? 1f : Mathf.Clamp01(_cameoMs / CameoFadeMs);
            a = Mathf.Min(a, Mathf.Clamp01((total - _cameoMs) / CameoFadeMs));
            float arm = 95f;
            if (!_cameoStill)
            {
                float wave = Mathf.Clamp01((_cameoMs - CameoFadeMs) / CameoWaveMs);
                arm = wave < 1f ? 95f + (30f * Mathf.Sin(wave * 3f * 2f * Mathf.PI)) : 60f;
            }

            CrewDraw.SetAlpha(_cameoTree, a);
            CrewDraw.SetAlpha(_cameoBody, a);
            CrewDraw.SetAlpha(_cameoHead, a);
            CrewDraw.SetAlpha(_cameoArm, a);
            if (_cameoArm != null)
            {
                CrewDraw.SetRotation(_cameoArm.rectTransform, arm);
            }
        }

        private void ApplyChip()
        {
            if (_crewChip == null)
            {
                return;
            }

            bool on = _crewChipMs >= 0f;
            if (_crewChip.gameObject.activeSelf != on)
            {
                _crewChip.gameObject.SetActive(on);
            }

            if (!on)
            {
                return;
            }

            float a = Mathf.Min(Mathf.Clamp01(_crewChipMs / CrewChipInMs), Mathf.Clamp01((CrewChipMs - _crewChipMs) / CrewChipOutMs));
            CrewDraw.SetAlpha(_crewChipPlate, a);
            if (_crewChipLabel != null && _crewChipLabel.canvasRenderer.GetAlpha() != a)
            {
                _crewChipLabel.canvasRenderer.SetAlpha(a);
            }
        }
    }
}
