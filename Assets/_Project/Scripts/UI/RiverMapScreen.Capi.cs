#nullable enable
using AF.Levels;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-3 «Capi al remo» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §5, §7.2, §7.3, §9.3 y §9.4),
    /// parcial de <see cref="RiverMapScreen"/>. Con el Mapa v2 (<see cref="LiveActive"/>, <c>ui_map_v2</c>):
    /// <list type="bullet">
    /// <item><description><b>La canoa</b> de <see cref="ProceduralArt.CapiCanoeDesignWidthDp"/> × <see cref="ProceduralArt.CapiCanoeDesignHeightDp"/>
    /// dp en la vista Mundo (40 de ancho en la Cuenca), con Capi en un rig por código (<see cref="CapiRigView"/>, <see cref="MapCapiArt"/>)
    /// y la carga a popa: una cuia por mundo completado, con el agua de su mundo (como mucho <see cref="CapiRigView.MaxCargo"/>).</description></item>
    /// <item><description><b>Gestos y emociones</b> (§5.2-§5.4; <see cref="CapiDirector"/>): rema en el viaje; mira con la pata de
    /// visera en la anticipación de D-053 y en las bocas; saluda al pasar un pueblo (que enciende una luz), a la bandera nueva y al abrir
    /// el Mapa sin viaje una vez por sesión; rema fuerte en los rápidos; se asoma por la borda al cambiar el agua (el Encuentro); en la
    /// llegada, el remo en alto y amarra la muestra. Una emoción sin texto como mucho por viaje, en su burbuja (P3 (a)).</description></item>
    /// <item><description><b>La cámara</b> (§5.5): acercamiento de 1 a <see cref="CamLeadZoom"/> en la anticipación; en el viaje sigue a
    /// la canoa con <see cref="CamFollowLagMs"/> ms de retraso y la deja en el tercio de abajo; en la llegada, <see cref="CamArrivalZoom"/>
    /// y vuelta en <see cref="CamArrivalBackMs"/> ms. Nada retrasa «Jugar»: entra al llegar, como en D-053. La luz del mundo se funde en
    /// <see cref="LightFadeMs"/> ms al cambiar de mundo dentro del Mapa (lo que M2-1 dejó pendiente).</description></item>
    /// <item><description><b>El pie</b> [P]: el conmutador y el Muestrario en una sola fila, y «Faltan N» sale al Mapa, en una placa
    /// arriba a la derecha (como en la maqueta, donde va junto a la parada).</description></item>
    /// <item><description><b>Reducir movimiento</b> (§7.3): sin cámara ni bucles; el gesto y la emoción, quietos
    /// <see cref="CapiDirector.StillMs"/> ms; la luz, con su fundido. Los gestos y la burbuja no reciben toques ni los lee TalkBack
    /// (son decoración: la llegada ya la anuncia D-053); «Faltan N» sigue en el recorrido del lector.</description></item>
    /// </list>
    /// Con <c>ui_map_v2</c> apagada, la canoa y el pie de la 1.0. El atlas (512 KiB) se pinta al abrir y se suelta al cerrar. Por
    /// fotograma solo se mueven rectángulos y opacidades: sin asignaciones.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Acercamiento de la anticipación (§5.5).</summary>
        public const float CamLeadZoom = 1.12f;

        /// <summary>Lo que tarda en volver de la anticipación al salir la canoa (ms) [P].</summary>
        public const float CamLeadOutMs = 400f;

        /// <summary>Retraso con que la cámara sigue a la canoa en el viaje (ms; §5.5).</summary>
        public const float CamFollowLagMs = 120f;

        /// <summary>La canoa, en el viaje, a esta fracción del alto de la vista desde arriba (el tercio de abajo; §5.5).</summary>
        public const float CamFollowY = 2f / 3f;

        /// <summary>Acercamiento de la llegada (§5.5).</summary>
        public const float CamArrivalZoom = 1.15f;

        /// <summary>Entrada del acercamiento de la llegada (ms) [P].</summary>
        public const float CamArrivalInMs = 150f;

        /// <summary>Vuelta de la llegada al encuadre (ms; §5.5).</summary>
        public const float CamArrivalBackMs = 300f;

        /// <summary>
        /// Primer plano de la canoa cuando se une un compañero o saluda el miquito (×) [P] (añadido de D-069 del 2026-11-08,
        /// observación 8 de la prueba de la 1.1, propuesta del dueño: «ahí debería hacerse como un zoom y luego volver a verlo al
        /// tamaño normal»): la canoa de 60 dp, a 150; el tucán de 18, a 45; el tucuxi de 17, a 42.
        /// </summary>
        public const float CamCloseUpZoom = 2.5f;

        /// <summary>Constante de tiempo de la entrada del primer plano (ms) [P]: a los ~900 ms está del todo.</summary>
        public const float CamCloseUpInMs = 200f;

        /// <summary>Constante de tiempo de la vuelta suave del primer plano a la vista normal (ms) [P]: desde ×2,5, unos 1,5 s.</summary>
        public const float CamCloseUpOutMs = 300f;

        /// <summary>Fundido de la luz al cambiar de mundo dentro del Mapa (ms; propuesta §3.3, pendiente de M2-1).</summary>
        public const float LightFadeMs = 600f;

        /// <summary>Lo que dura la luz que enciende un pueblo al pasarlo (ms) [P].</summary>
        public const float TownGlowMs = 900f;

        /// <summary>Lado de la luz del pueblo (dp) [P].</summary>
        public const float TownGlowDp = 64f;

        /// <summary>Margen de la placa de «Faltan N» con el borde de la zona del mapa (dp) [P].</summary>
        public const float RemainingChipInsetDp = 12f;

        /// <summary>La placa de «Faltan N» ocupa como mucho esta fracción del ancho de la zona del mapa (no llega a «Andes ↑») [P].</summary>
        public const float RemainingChipMaxFraction = 0.5f;

        /// <summary>Ancho máximo de la placa de «Faltan N» (dp) [P].</summary>
        public const float RemainingChipMaxDp = 176f;

        /// <summary>Ancho mínimo de la placa de «Faltan N» (dp).</summary>
        public const float RemainingChipMinDp = 2f * MinTouchDp;

        /// <summary>Aire de arriba y de abajo del texto de la placa de «Faltan N» en el Mapa (dp) [P].</summary>
        public const float RemainingChipPadDp = 8f;

        private const int MaxCapiEvents = 48;

        private static bool s_capiWaved;

        private readonly CapiDirector _capi = new CapiDirector();
        private readonly MapCapiArt _capiArt = new MapCapiArt();
        private readonly float[] _evLevel = new float[MaxCapiEvents];
        private readonly CapiMoment[] _evMoment = new CapiMoment[MaxCapiEvents];
        private readonly int[] _evArg = new int[MaxCapiEvents];
        private readonly Color[] _cargoColors = new Color[16];
        private CapiRigView? _capiRig;
        private Image? _townGlow;
        private int _evCount;
        private int _evNext;
        private bool _capiTraveling;
        private bool _capiDeparted;
        private Color _sampleWater = Color.white;
        private float _camZoom = 1f;
        private int _camPhase; // 0 nada, 1 anticipación, 2 vuelta de la anticipación, 3 llegada
        private float _closeZoom = 1f; // el primer plano de la tripulación (añadido de D-069 del 2026-11-08), que multiplica al de _camPhase
        private float _camPhaseZoom = 1f; // la escala de _camPhase sola, sin el primer plano
        private float _camMs;
        private float _camFrom = 1f;
        private Color _lightFrom;
        private Color _lightTo;
        private float _lightMs = -1f;
        private float _townGlowMs = -1f;

        /// <summary>El rig de Capi (null antes de la primera apertura con el Mapa v2).</summary>
        internal CapiRigView? CapiRig => _capiRig;

        /// <summary>Los gestos y la emoción de Capi (tests).</summary>
        internal CapiDirector Capi => _capi;

        /// <summary>El atlas del rig (tests y sonda).</summary>
        internal MapCapiArt CapiArt => _capiArt;

        /// <summary>Esta apertura lleva a Capi al remo (el Mapa v2 con canoa).</summary>
        public bool CapiActive => _live && _capiRig != null && _capiRig.Root.gameObject.activeInHierarchy;

        /// <summary>Acercamiento de la cámara ahora (1 sin acercar).</summary>
        public float CameraZoom => _camZoom;

        /// <summary>La escala de la cámara de §5.5 sola (anticipación y llegada), sin el primer plano de la tripulación.</summary>
        public float CameraPhaseZoom => _camPhaseZoom;

        /// <summary>El primer plano de la tripulación en este fotograma (1 sin él; añadido de D-069 del 2026-11-08).</summary>
        public float CameraCloseUp => _closeZoom;

        // Se pide el primer plano: un compañero llega o enseña su chip, o el miquito saluda (RiverMapScreen.Crew.cs).
        private bool CloseUpWanted => CrewArriving || CrewChipShown || CameoWaving;

        /// <summary>Centro de la canoa que se pinta ahora (al ponerse al día, por el agua) en la zona visible, en dp (y hacia abajo).</summary>
        internal Vector2 CanoeShownInViewport
        {
            get
            {
                Vector2 c = _canoe.rectTransform.anchoredPosition;
                return new Vector2(_offset.x + c.x, _offset.y - c.y);
            }
        }

        /// <summary>La luz del mundo se está fundiendo.</summary>
        public bool LightFading => _lightMs >= 0f;

        /// <summary>El color de la luz del mundo que se ve.</summary>
        internal Color LightColor => _light != null ? _light.color : Color.clear;

        /// <summary>Un pueblo enciende su luz ahora.</summary>
        public bool TownGlowing => _townGlowMs >= 0f;

        /// <summary>Momentos que el viaje de esta apertura cruzará (tests).</summary>
        internal int CapiEventCount => _evCount;

        /// <summary>El momento <paramref name="i"/> del viaje (tests).</summary>
        internal CapiMoment CapiEventAt(int i)
        {
            return _evMoment[i];
        }

        /// <summary>El nivel (fraccionario) en que la canoa cruza el momento <paramref name="i"/> (tests).</summary>
        internal float CapiEventLevel(int i)
        {
            return _evLevel[i];
        }

        /// <summary>Vuelve a permitir el saludo de una vez por sesión al abrir el Mapa sin viaje (tests).</summary>
        internal static void ResetCapiSession()
        {
            s_capiWaved = false;
        }

        // Escala del rig: 1 en la vista Mundo; en la Cuenca, la canoa de 40 dp de siempre.
        private float CapiScale => _view == MapView.World ? 1f : ProceduralArt.MapCanoeWidthDp / ProceduralArt.CapiCanoeDesignWidthDp;

        /// <summary>Ancho de la canoa en esta vista (dp): 60 en la vista Mundo del Mapa v2 (§5.1), 40 si no.</summary>
        internal float CanoeWidthDp => _live && _view == MapView.World ? ProceduralArt.CapiCanoeDesignWidthDp : ProceduralArt.MapCanoeWidthDp;

        /// <summary>
        /// Alto de la canoa en esta vista (dp): 44 en la vista Mundo del Mapa v2, 32 si no (en la Cuenca, la caja de la 1.0, con el rig
        /// a 40/60 dentro: así las placas y los rótulos esquivan lo mismo que antes).
        /// </summary>
        internal float CanoeHeightDp => _live && _view == MapView.World ? ProceduralArt.CapiCanoeDesignHeightDp : ProceduralArt.MapCanoeHeightDp;

        // ============================================================================================ apertura y cierre

        // Al final de Open (tras «ponerse al día» y el viaje de D-053): el rig, la carga, los momentos del viaje y su emoción, la luz.
        partial void CapiOpen(MapOpenSource source)
        {
            ResetCamera();
            _lightMs = -1f;
            _townGlowMs = -1f;
            if (_townGlow != null)
            {
                _townGlow.enabled = false;
            }

            _evCount = 0;
            _evNext = 0;
            _capiDeparted = false;
            _capiFollowed = false;
            _capiSnap = false;
            _capiRestShown = false;
            _capi.Begin(ProgressReduceMotion, CapiEmotion.None);
            CrewBegin(); // D-069, M2-4: quién va a bordo y quién se presenta (RiverMapScreen.Crew.cs)
            if (!_live || !_hasCanoe)
            {
                _canoe.enabled = true;
                _capiRig?.SetActive(false);
                _capiArt.Release();
                _capiTraveling = false;
                return;
            }

            _capiArt.Build();
            if (_capiRig == null)
            {
                _capiRig = new CapiRigView(_canoe.rectTransform);
            }

            _capiRig.SetArt(_capiArt, _art.MapMiniCuia, _art.MapMiniCuiaWater);
            _capiRig.SetActive(true);
            _canoe.enabled = false; // el sprite de la 1.0 deja paso al rig (el rectángulo sigue colocando la canoa)
            PrepareCapiWater();
            SetCargo(_catchFrom);
            PrepareTownGlow();

            bool still = ProgressReduceMotion;
            int mask = PlanCapiEvents(source);
            _capiTraveling = _catchUp;
            if (_catchUp)
            {
                mask |= CapiDirector.Bit(CapiMoment.Depart);
            }

            _capi.Begin(still, CapiDirector.PickEmotion(mask));
            _capiRig.Prepare(_capi.PlannedEmotion); // los sprites de la burbuja, ya: el viaje solo cambia escalas y opacidades

            // La luz: la del mundo de la canoa al salir; si llega a otro, se funde a la de «Jugar» al cruzar la parada.
            int fromWorld = WorldTable.WorldOf(Mathf.Clamp(_catchFrom + 1, 1, WorldTable.LastLevel));
            if (_light != null && fromWorld != _world && _catchFrom < _catchTo)
            {
                _light.color = WorldLight(fromWorld);
                if (still)
                {
                    StartLightFade();
                }
            }

            if (_catchUp)
            {
                _capi.Traveling = true;
                if (_journeyLock && _catchHoldMs > 0f)
                {
                    _capi.React(CapiMoment.Lead); // D-053: la pata de visera en los 250 ms de antes de salir
                    if (CameraAllowed)
                    {
                        _camPhase = 1;
                        _camMs = 0f;
                        PrimeJourneyRegion();
                    }
                }
            }
            else if (still && _catchFrom < _catchTo)
            {
                // Sin viaje (Reducir movimiento): el momento más importante del tramo, quieto, y la emoción del viaje, quieta.
                CapiMoment top = StillMoment();
                if (top != CapiMoment.OpenIdle)
                {
                    _capi.React(top);
                }

                _capi.ShowPlanned();
                _evNext = _evCount;
                SetCargo(_catchTo);
            }
            else if (!s_capiWaved)
            {
                s_capiWaved = true;
                _capi.React(CapiMoment.OpenIdle); // §5.2: al abrir el Mapa sin viaje, una vez por sesión
            }

            ApplyCapiPose();
        }

        partial void CapiClose()
        {
            _capiHold = false;
            ResetCamera();
            _capi.Stop();
            _lightMs = -1f;
            _townGlowMs = -1f;
            if (_capiRig != null)
            {
                _capiRig.SetArt(null, null, null);
                _capiRig.SetActive(false);
            }

            if (_townGlow != null)
            {
                _townGlow.sprite = null;
                _townGlow.enabled = false;
            }

            _canoe.enabled = true;
            _capiArt.Release();
        }

        /// <summary>
        /// Suelta el atlas del rig sin tocar la pantalla (que puede estar ya destruida): lo llama <see cref="GameBootstrap"/> si la escena
        /// se va con el Mapa abierto, como <see cref="ReleaseLiveArt"/>.
        /// </summary>
        internal void ReleaseCapiArt()
        {
            _capiArt.Release();
        }

        /// <summary>Miembros extra de <c>AF_PERF mem</c> al abrir el Mapa (D-069, M2-3): el atlas del rig de Capi.</summary>
        internal string CapiMemJson()
        {
            return ",\"capi_tex_kib\":" + (_capiArt.TextureBytes / 1024L).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // ============================================================================================ cada fotograma

        partial void CapiTick(float dtMs, ref bool animating)
        {
            if (!_live || _capiRig == null || !_capiArt.Built)
            {
                return;
            }

            bool traveling = _catchUp;
            if (traveling && _catchHoldMs <= 0f && !_capiDeparted)
            {
                _capiDeparted = true;
                _capi.React(CapiMoment.Depart);
                if (_camPhase == 1)
                {
                    StartCamera(2);
                }
            }

            if (traveling)
            {
                while (_evNext < _evCount && _evLevel[_evNext] <= _canoeX + 1e-4f)
                {
                    FireCapiEvent(_evNext++);
                }
            }
            else if (_capiTraveling)
            {
                // Llegó (por tiempo, con un toque o al cerrar): lo que quedaba por cruzar se salta, salvo lo de la llegada final (el remo
                // en alto, la muestra, el agua, la bandera y el mundo nuevo). Al volver de segundo plano (02 §14), nada se anima: Capi
                // quieto, la cámara en su encuadre y la luz ya en la del mundo de «Jugar».
                bool snap = _capiSnap;
                for (int i = _evNext; i < _evCount && !snap; i++)
                {
                    bool final = _evLevel[i] >= _catchTo - 1e-4f && _evMoment[i] != CapiMoment.TownPass && _evMoment[i] != CapiMoment.Mouth && _evMoment[i] != CapiMoment.Rapids;
                    if (final || _evMoment[i] == CapiMoment.Join) // M2-4: quien se une, también si el toque salta el viaje
                    {
                        FireCapiEvent(i);
                    }
                }

                _evNext = _evCount;
                SettleLight(snap);
                if (snap)
                {
                    _capi.Stop();
                    ResetCamera();
                }

                _capiSnap = false;
                SetCargo(_catchTo);
                if (_capiFollowed)
                {
                    _capiFollowed = false;
                    SetOffset(_offset); // «Volver a la canoa», con la canoa ya en su sitio
                }
            }

            _capiTraveling = traveling;
            _capi.Traveling = traveling;
            _capi.Tick(dtMs);
            TickCamera(dtMs, traveling);
            TickLight(dtMs);
            TickTownGlow(dtMs);
            ApplyCapiPose();
            if (_capi.Animating || _camPhase != 0 || _closeZoom != 1f || _lightMs >= 0f || _townGlowMs >= 0f)
            {
                animating = true;
            }
        }

        private void ApplyCapiPose()
        {
            if (_capiRig == null)
            {
                return;
            }

            if (_capiHold)
            {
                CapiPose held = CapiDirector.Pose(_holdGesture, _holdPhase);
                _capiRig.Apply(held, _holdEmotion, _holdEmotion != CapiEmotion.None ? 1f : 0f, _sampleWater, _canoeFacing, CapiScale);
                return;
            }

            // Quieto y sin burbuja, una vez basta (el lienzo no se rehace a 30 fps en reposo); el volteo y la escala lo rehacen.
            bool rest = _capi.Current == CapiGesture.Rest && _capi.Emotion == CapiEmotion.None;
            if (rest && _capiRestShown && _capiRestFacing == _canoeFacing && _capiRestScale == CapiScale)
            {
                return;
            }

            CapiPose pose = CapiDirector.Pose(_capi.Current, _capi.Phase);
            float bubble = CapiDirector.BubbleScale(_capi.EmotionElapsedMs, _capi.Still);
            _capiRig.Apply(pose, _capi.Emotion, bubble, _sampleWater, _canoeFacing, CapiScale);
            _capiRestShown = rest;
            _capiRestFacing = _canoeFacing;
            _capiRestScale = CapiScale;
        }

        private float _capiRestFacing;
        private float _capiRestScale;

        private bool _capiHold;
        private CapiGesture _holdGesture;
        private float _holdPhase;
        private CapiEmotion _holdEmotion;

        /// <summary>
        /// Capturas: deja a Capi quieto en <paramref name="gesture"/> a la fase <paramref name="phase"/> con <paramref name="emotion"/> en
        /// la burbuja (sin burbuja con None), hasta <see cref="ReleaseCapiPose"/> o el cierre.
        /// </summary>
        internal void HoldCapiPose(CapiGesture gesture, float phase, CapiEmotion emotion)
        {
            _capiHold = true;
            _capiRestShown = false;
            _holdGesture = gesture;
            _holdPhase = phase;
            _holdEmotion = emotion;
            ApplyCapiPose();
        }

        /// <summary>Capi vuelve a lo que diga <see cref="CapiDirector"/>.</summary>
        internal void ReleaseCapiPose()
        {
            _capiHold = false;
            _capiRestShown = false;
            ApplyCapiPose();
        }

        // ============================================================================================ momentos del viaje

        // Los momentos que la canoa cruza de _catchFrom a _catchTo, en orden de nivel: los pueblos de paso dibujados, las bocas y los
        // rápidos (por la fracción de su tramo) y, en cada 10k, la llegada, el agua nueva, la bandera y el mundo nuevo. Devuelve sus bits.
        private int PlanCapiEvents(MapOpenSource source)
        {
            int mask = 0;
            RiverRoutes? routes = _routes;
            RiverStopCatalog? stops = _stops;
            RiverMap? map = _map;
            if (routes == null || stops == null || map == null || _catchFrom >= _catchTo)
            {
                return mask;
            }

            int firstLeg = (_catchFrom / RiverStopCatalog.LevelsPerStop) + 1;
            int lastLeg = (_catchTo + RiverStopCatalog.LevelsPerStop - 1) / RiverStopCatalog.LevelsPerStop;
            for (int n = firstLeg; n <= lastLeg; n++)
            {
                RiverStopRoute? leg = routes.At(n);
                if (leg != null)
                {
                    for (int i = 0; i < leg.Via.Count; i++)
                    {
                        float at = ((n - 1) * RiverStopCatalog.LevelsPerStop) + (RiverStopCatalog.LevelsPerStop * leg.ViaFraction(i));
                        if (at <= _catchFrom || at > _catchTo)
                        {
                            continue;
                        }

                        string id = leg.Via[i];
                        RiverPassingPoint? p = PassingById(map, id);
                        if (p == null)
                        {
                            continue;
                        }

                        if (p.IsTown)
                        {
                            int town = TownIndexOf(id);
                            if (town >= 0)
                            {
                                mask |= AddCapiEvent(at, CapiMoment.TownPass, town); // solo los que se dibujan (con nombre aprobado)
                            }
                        }
                        else
                        {
                            mask |= AddCapiEvent(at, IsRapids(id) ? CapiMoment.Rapids : CapiMoment.Mouth, -1);
                        }
                    }
                }

                int level = n * RiverStopCatalog.LevelsPerStop;
                RiverStop? stop = stops.At(n);
                RiverStop? before = stops.At(n - 1);
                if (stop == null || level <= _catchFrom || level > _catchTo)
                {
                    continue;
                }

                mask |= AddCapiEvent(level, CapiMoment.Arrival, n);
                if (before != null && !string.Equals(before.Sample, stop.Sample, System.StringComparison.Ordinal))
                {
                    mask |= AddCapiEvent(level, CapiMoment.WaterChange, n);
                }

                if (!string.IsNullOrEmpty(stop.FlagRaise))
                {
                    mask |= AddCapiEvent(level, CapiMoment.CountryChange, n);
                }

                if (before != null && before.World != stop.World)
                {
                    mask |= AddCapiEvent(level, CapiMoment.NewWorld, n);
                }
            }

            if (source == MapOpenSource.ContentEnd)
            {
                mask |= AddCapiEvent(_catchTo, CapiMoment.ContentEnd, -1); // ? ante la niebla del fin de contenido
            }

            CrewPlanEvents(ref mask); // D-069, M2-4: el compañero que se une, con ♥

            SortCapiEvents();
            return mask;
        }

        /// <summary>
        /// Los rápidos entre los puntos de paso que no son pueblos (los <see cref="RiverTownTier.Mark"/>): hoy, Teotônio; el resto
        /// son bocas de afluentes [P].
        /// </summary>
        public static bool IsRapids(string passingId)
        {
            return string.Equals(passingId, "teotonio", System.StringComparison.Ordinal);
        }

        private static RiverPassingPoint? PassingById(RiverMap map, string id)
        {
            for (int i = 0; i < map.Passing.Count; i++)
            {
                if (string.Equals(map.Passing[i].Id, id, System.StringComparison.Ordinal))
                {
                    return map.Passing[i];
                }
            }

            return null;
        }

        private int AddCapiEvent(float level, CapiMoment moment, int arg)
        {
            if (_evCount >= MaxCapiEvents)
            {
                return 0;
            }

            _evLevel[_evCount] = level;
            _evMoment[_evCount] = moment;
            _evArg[_evCount] = arg;
            _evCount++;
            return CapiDirector.Bit(moment);
        }

        // Por nivel (estable: lo que cae en el mismo nivel queda en el orden en que se añadió).
        private void SortCapiEvents()
        {
            for (int i = 1; i < _evCount; i++)
            {
                float l = _evLevel[i];
                CapiMoment m = _evMoment[i];
                int a = _evArg[i];
                int j = i - 1;
                while (j >= 0 && _evLevel[j] > l)
                {
                    _evLevel[j + 1] = _evLevel[j];
                    _evMoment[j + 1] = _evMoment[j];
                    _evArg[j + 1] = _evArg[j];
                    j--;
                }

                _evLevel[j + 1] = l;
                _evMoment[j + 1] = m;
                _evArg[j + 1] = a;
            }
        }

        // Con Reducir movimiento, el gesto quieto del tramo: la llegada si la hay; si no, el primer pueblo o lo que haya.
        private CapiMoment StillMoment()
        {
            CapiMoment best = CapiMoment.OpenIdle;
            for (int i = 0; i < _evCount; i++)
            {
                if (_evMoment[i] == CapiMoment.Arrival)
                {
                    SetSampleWater(_evArg[i]);
                    return CapiMoment.Arrival;
                }

                if (best == CapiMoment.OpenIdle && CapiDirector.GesturesFor(_evMoment[i], _stillScratch) > 0)
                {
                    best = _evMoment[i];
                }
            }

            return best;
        }

        private readonly CapiGesture[] _stillScratch = new CapiGesture[3];

        private void FireCapiEvent(int i)
        {
            CapiMoment m = _evMoment[i];
            switch (m)
            {
                case CapiMoment.Arrival:
                    SetSampleWater(_evArg[i]);
                    if (CameraAllowed)
                    {
                        StartCamera(3);
                    }

                    break;
                case CapiMoment.NewWorld:
                    StartLightFade();
                    break;
                case CapiMoment.TownPass:
                    StartTownGlow(_evArg[i]);
                    break;
            }

            CrewReact(m, _evArg[i]); // D-069, M2-4: la tripulación salta o aletea en la llegada; quien se une, llega
            _capi.React(m);
        }

        private void SetSampleWater(int stopN)
        {
            _sampleWater = stopN >= 0 && stopN < _stopWater.Length ? _stopWater[stopN] : Color.white;
        }

        // Al abrir (asigna solo si crece el catálogo): el agua de cada parada y la carga posible, una cuia por mundo con su agua y el
        // nivel con que se completa. Así, en el viaje, la llegada y la carga solo leen tablas (ArrivalStrip.SampleWater parte el
        // texto de las mezclas).
        private void PrepareCapiWater()
        {
            RiverStopCatalog? stops = _stops;
            _cargoCount = 0;
            if (stops == null)
            {
                return;
            }

            if (_stopWater.Length < stops.Count)
            {
                _stopWater = new Color[stops.Count];
            }

            int world = 0;
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop? st = stops.At(n);
                if (st == null)
                {
                    continue;
                }

                _stopWater[n] = ArrivalStrip.SampleWater(st.Sample, out _);
                RiverStop? next = stops.At(n + 1);
                bool lastOfWorld = next == null || next.World != st.World;
                if (lastOfWorld && st.World != world && _cargoCount < _cargoColors.Length)
                {
                    world = st.World;
                    _cargoColors[_cargoCount] = _stopWater[n];
                    _cargoLevel[_cargoCount] = WorldTable.LastLevelOf(st.World);
                    _cargoCount++;
                }
            }
        }

        private Color[] _stopWater = System.Array.Empty<Color>();
        private readonly int[] _cargoLevel = new int[16];
        private int _cargoCount;

        // La carga a popa con level niveles ganados: una cuia por mundo completado (todas sus paradas pasadas), con el agua de la
        // última parada de ese mundo; la vista enseña las CapiRigView.MaxCargo últimas.
        private void SetCargo(int level)
        {
            if (_capiRig == null || _stops == null)
            {
                return;
            }

            int count = 0;
            while (count < _cargoCount && level >= _cargoLevel[count])
            {
                count++;
            }

            _capiRig.SetCargo(_cargoColors, count);
        }

        // ============================================================================================ cámara

        // La cámara se mueve solo en la vista Mundo, sin «Norte arriba» y sin Reducir movimiento (que la deja en cortes).
        private bool CameraAllowed => _view == MapView.World && !_northUpOn && !NorthUpTurning && !ProgressReduceMotion;

        // Antes de un viaje en que la cámara sigue a la canoa: la región de la selva y la corriente (RiverMapScreen.Live.cs) se hace ya
        // con la vista de ahora y las del principio y el final del tramo (con la canoa en el tercio de abajo), más su margen, para que
        // seguirla no rehaga ninguna malla por el camino (sin asignaciones por fotograma). Asigna: solo aquí, al abrir.
        private void PrimeJourneyRegion()
        {
            if (!_liveBuilt || _canopy == null || _map == null || _routes == null || _space.DpPerUnit <= 0f)
            {
                return;
            }

            Rect visible = VisibleMapRect();
            Rect r = visible;
            int samples = Mathf.Max(2, (_catchTo - _catchFrom) * JourneyRegionSamplesPerLevel);
            for (int i = 0; i <= samples; i++)
            {
                float level = Mathf.Lerp(_catchFrom, _catchTo, i / (float)samples);
                r = Union(r, FollowRect(CanoeAt(_map, _routes, level), visible.size)); // por el agua, también en los meandros
            }

            r = Union(r, FollowRect(_canoeMap, visible.size));
            float mx = visible.width * RegionMargin;
            float my = visible.height * RegionMargin;
            _liveRegion = new Rect(r.xMin - mx, r.yMin - my, r.width + (2f * mx), r.height + (2f * my));
            _canopy.SetRegion(_liveRegion);
            ChooseFlowSlots(_liveRegion);
        }

        // La vista de tamaño size (unidades del marco) con la canoa en p, en el centro a lo ancho y en el tercio de abajo.
        private const int JourneyRegionSamplesPerLevel = 4;

        private static Rect FollowRect(Vector2 p, Vector2 size)
        {
            return new Rect(p.x - (size.x * 0.5f), p.y - (size.y * CamFollowY), size.x, size.y);
        }

        private static Rect Union(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        // Como SetOffset, sin decidir «Volver a la canoa» (que mira la canoa ya llegada y, en el viaje, encendería o apagaría el botón:
        // asigna memoria); al llegar, SetOffset lo decide una vez. Sin asignaciones.
        private void FollowOffset(Vector2 offset)
        {
            _offset = new Vector2(ClampAxis(offset.x, _contentW, _viewW), ClampAxis(offset.y, _contentH, _viewH));
            if (_camZoom == 1f)
            {
                _content.anchoredPosition = new Vector2(_offset.x, -_offset.y);
            }

            _capiFollowed = true;
            LiveOffset();
        }

        private bool _capiFollowed;
        private bool _capiSnap;
        private bool _capiRestShown;

        // Al acabar el viaje, la luz acaba en la del mundo de «Jugar» aunque ninguna parada lo marcara (un toque que salta el cambio de
        // mundo, el fin de contenido): con su fundido o, al volver de segundo plano, de golpe.
        private void SettleLight(bool snap)
        {
            if (_light == null)
            {
                return;
            }

            Color target = WorldLight(_world);
            if (snap)
            {
                _lightMs = -1f;
                _light.color = target;
            }
            else if (_lightMs < 0f && _light.color != target)
            {
                StartLightFade();
            }
        }

        private void StartCamera(int phase)
        {
            _camFrom = _camPhaseZoom;
            _camPhase = phase;
            _camMs = 0f;
        }

        private void TickCamera(float dtMs, bool traveling)
        {
            // Seguir a la canoa en el viaje de D-053 (el que bloquea «Jugar»), con el retraso suave, en el tercio de abajo.
            if (traveling && _journeyLock && _catchHoldMs <= 0f && CameraAllowed)
            {
                Vector2 c = _canoe.rectTransform.anchoredPosition;
                var target = new Vector2((_viewW * 0.5f) - c.x, (_viewH * CamFollowY) + c.y);
                float k = 1f - Mathf.Exp(-Mathf.Max(0f, dtMs) / CamFollowLagMs);
                FollowOffset(_offset + ((target - _offset) * k));
            }

            // Añadido de D-069 del 2026-11-08 (observación 8): el primer plano de la canoa mientras un compañero se une o el miquito
            // saluda, que entra y vuelve suave a la vista normal. Sin él con Reducir movimiento, en la Cuenca o arrastrando.
            bool closeOk = CameraAllowed && !_dragging;
            float want = closeOk && CloseUpWanted ? CamCloseUpZoom : 1f;
            if (_closeZoom != want)
            {
                float tc = want > _closeZoom ? CamCloseUpInMs : CamCloseUpOutMs;
                _closeZoom += (want - _closeZoom) * (1f - Mathf.Exp(-Mathf.Max(0f, dtMs) / tc));
                if (Mathf.Abs(_closeZoom - want) < 0.01f || !closeOk)
                {
                    _closeZoom = want;
                }
            }

            CloseUpLabels(); // los rótulos se apagan en el primer plano: a ×2 serían enormes (RiverMapScreen.Towns.cs)
            if (_camPhase == 0)
            {
                ApplyCloseUp(1f);
                return;
            }

            if (!CameraAllowed || _dragging)
            {
                ResetCamera();
                return;
            }

            _camMs += Mathf.Max(0f, dtMs);
            float z;
            switch (_camPhase)
            {
                case 1:
                    z = Mathf.Lerp(1f, CamLeadZoom, Ease.OutCubic(Mathf.Clamp01(_camMs / JourneyLeadMs)));
                    break;
                case 2:
                    z = Mathf.Lerp(_camFrom, 1f, Ease.InOutSine(Mathf.Clamp01(_camMs / CamLeadOutMs)));
                    if (_camMs >= CamLeadOutMs)
                    {
                        _camPhase = 0;
                    }

                    break;
                default:
                    if (_camMs < CamArrivalInMs)
                    {
                        z = Mathf.Lerp(_camFrom, CamArrivalZoom, Ease.OutCubic(_camMs / CamArrivalInMs));
                    }
                    else
                    {
                        float back = Mathf.Clamp01((_camMs - CamArrivalInMs) / CamArrivalBackMs);
                        z = Mathf.Lerp(CamArrivalZoom, 1f, Ease.InOutSine(back));
                        if (back >= 1f)
                        {
                            _camPhase = 0;
                        }
                    }

                    break;
            }

            ApplyCloseUp(_camPhase == 0 ? 1f : z);
        }

        // La escala de la fase de la cámara por el primer plano; en reposo (las dos a 1) no escribe nada.
        private void ApplyCloseUp(float phaseZoom)
        {
            _camPhaseZoom = phaseZoom;
            float z = phaseZoom * _closeZoom;
            if (z != 1f || _camZoom != 1f)
            {
                ApplyCameraZoom(z);
            }
        }

        // El contenido a la escala z alrededor de la canoa (la canoa no se mueve en pantalla); con 1, vuelve a su sitio de siempre.
        private void ApplyCameraZoom(float z)
        {
            _camZoom = z;
            if (Mathf.Approximately(z, 1f))
            {
                _content.localScale = Vector3.one;
                _content.anchoredPosition = new Vector2(_offset.x, -_offset.y);
                return;
            }

            Vector2 c = _canoe.rectTransform.anchoredPosition;
            var at = new Vector2(_offset.x + c.x, -_offset.y + c.y);
            _content.localScale = new Vector3(z, z, 1f);
            _content.anchoredPosition = at - (c * z);
        }

        private void ResetCamera()
        {
            _camPhase = 0;
            _camMs = 0f;
            _closeZoom = 1f;
            _camPhaseZoom = 1f;
            CloseUpLabels();
            if (_camZoom != 1f || _content.localScale != Vector3.one)
            {
                ApplyCameraZoom(1f);
            }

            _camZoom = 1f;
        }

        // ============================================================================================ luz y pueblos

        private void StartLightFade()
        {
            if (_light == null)
            {
                return;
            }

            _lightFrom = _light.color;
            _lightTo = WorldLight(_world);
            _lightMs = _lightFrom == _lightTo ? -1f : 0f;
        }

        private void TickLight(float dtMs)
        {
            if (_lightMs < 0f || _light == null)
            {
                return;
            }

            _lightMs += Mathf.Max(0f, dtMs);
            float t = Mathf.Clamp01(_lightMs / LightFadeMs);
            _light.color = Color.Lerp(_lightFrom, _lightTo, t); // también con Reducir movimiento: es un fundido, no un movimiento
            if (t >= 1f)
            {
                _lightMs = -1f;
            }
        }

        private void StartTownGlow(int town)
        {
            if (town < 0 || town >= TownCount || _townLayer == null)
            {
                return;
            }

            if (_townGlow == null || !_townGlow.enabled)
            {
                return;
            }

            _townGlow.rectTransform.anchoredPosition = TownLocal(town);
            _townGlow.canvasRenderer.SetAlpha(0f);
            _townGlowMs = 0f;
        }

        // Al abrir, con los pueblos: la luz que enciende un pueblo, detrás de las viñetas, encendida y transparente (el viaje solo la
        // mueve y cambia su opacidad).
        private void PrepareTownGlow()
        {
            if (!TownsActive || _townLayer == null)
            {
                if (_townGlow != null)
                {
                    _townGlow.enabled = false;
                }

                return;
            }

            if (_townGlow == null)
            {
                _townGlow = UiBuilder.Image("TownGlow", _townLayer, null, new Color(1f, 0.9f, 0.6f, 1f));
                _townGlow.raycastTarget = false;
                RectTransform rt = _townGlow.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(TownGlowDp, TownGlowDp);
            }

            _townGlow.transform.SetAsFirstSibling(); // detrás de las viñetas
            _townGlow.sprite = _liveArt.Piece(MapLivePiece.Cloud);
            _townGlow.enabled = _townGlow.sprite != null;
            _townGlow.canvasRenderer.SetAlpha(0f);
        }

        private void TickTownGlow(float dtMs)
        {
            if (_townGlowMs < 0f || _townGlow == null)
            {
                return;
            }

            _townGlowMs += Mathf.Max(0f, dtMs);
            float t = Mathf.Clamp01(_townGlowMs / TownGlowMs);
            _townGlow.canvasRenderer.SetAlpha(0.85f * Mathf.Sin(t * Mathf.PI));
            if (t >= 1f)
            {
                _townGlowMs = -1f;
                _townGlow.canvasRenderer.SetAlpha(0f);
            }
        }

        // ============================================================================================ el pie [P]

        // Con el Mapa v2, el conmutador y la pestaña del Muestrario en una sola fila sobre «Jugar»; el Mapa llega hasta ella.
        partial void CapiFooter(float playTop, ref float samplesTop)
        {
            if (!_live)
            {
                return;
            }

            float rowTop = playTop - GapDp - RowDp;
            float switchW = (2f * SegmentDp) + (2f * SwitchPadDp);
            float slotX = 16f + switchW + GapDp;
            UiBuilder.Place(_samplesSlot, slotX, rowTop, Mathf.Max(MinTouchDp, _canvasW - 16f - slotX), SamplesRowDp);
            SamplesLayout(_samplesSlot);
            float x = 16f;
            if (_nothingToPlay)
            {
                // Sin nada que jugar (el fin de contenido), la fila va centrada, como el conmutador solo en la 1.0 (revisión de FX13).
                bool tab = _samplesSlot.gameObject.activeSelf && _samplesTab != null;
                float tabW = tab ? ((RectTransform)_samplesTab!.Value.Button.transform).sizeDelta.x : 0f;
                float groupW = switchW + (tab ? GapDp + tabW : 0f);
                x = (_canvasW - groupW) * 0.5f;
                if (tab)
                {
                    UiBuilder.Place(_samplesSlot, x + switchW + GapDp, rowTop, tabW, SamplesRowDp);
                    SamplesLayout(_samplesSlot);
                }
            }

            UiBuilder.Place(_viewSwitch.rectTransform, x, rowTop, switchW, RowDp);
            _viewWorld.Place(x + SwitchPadDp, rowTop, SegmentDp, RowDp);
            _viewBasin.Place(x + SwitchPadDp + SegmentDp, rowTop, SegmentDp, RowDp);
            samplesTop = rowTop;
        }

        // «Faltan N» en una placa arriba a la izquierda de la zona del mapa, a la medida de su texto y como mucho
        // RemainingChipMaxFraction de su ancho (en dos líneas si no cabe), para no llegar a «Andes ↑», centrado; si lo pisaría (o a
        // «Norte arriba»), justo debajo. El encuadre de las mini cuias la cuenta como lo fijo de arriba (WithRemainingChip).
        partial void CapiLayout(float frameTop)
        {
            _remainingChipView = default;
            _capiFrameTop = frameTop;
            if (!_live || !_remaining.gameObject.activeSelf)
            {
                return; // con el Mapa v2, «Faltan N» suele ir en el rótulo de la parada siguiente (SyncRemainingPlate): sin placa
            }

            float maxW = Mathf.Max(RemainingChipMinDp, Mathf.Min(RemainingChipMaxDp, (_viewW * RemainingChipMaxFraction) - RemainingChipInsetDp));
            float maxLabel = maxW - (2f * RemainingLabelXDp);
            float labelW = maxLabel;
            if (!string.IsNullOrEmpty(_remainingLabel.text))
            {
                labelW = Mathf.Min(maxLabel, Mathf.Ceil(_remainingLabel.GetPreferredValues(_remainingLabel.text, float.PositiveInfinity, float.PositiveInfinity).x) + 2f);
            }

            float h = Mathf.Max(RowDp - 8f, Mathf.Ceil(UiBuilder.MinTextHeight(_remainingLabel, labelW)) + (2f * RemainingChipPadDp));
            float w = labelW + (2f * RemainingLabelXDp);
            var chip = new Rect(RemainingChipInsetDp, RemainingChipInsetDp, w, h);
            TMPro.TextMeshProUGUI? andes = AndesLabel;
            if (andes != null && andes.gameObject.activeInHierarchy && !string.IsNullOrEmpty(andes.text))
            {
                Rect a = FixedViewRect(andes.rectTransform);
                if (a.Overlaps(chip))
                {
                    chip.y = a.yMax + 4f;
                }
            }

            if (NorthUpShown)
            {
                Rect n = NorthUpButtonFrameRect();
                if (n.Overlaps(chip))
                {
                    chip.y = n.yMax + 4f;
                }
            }

            UiBuilder.Place(_remaining, chip.x, frameTop + chip.y, chip.width, chip.height);
            UiBuilder.Place(_remainingLabel.rectTransform, RemainingLabelXDp, RemainingChipPadDp, labelW, chip.height - (2f * RemainingChipPadDp));
            _remainingChipView = _remaining.gameObject.activeSelf ? chip : default;
        }

        private Rect _remainingChipView;
        private float _capiFrameTop;
        private bool _remainingMerged;

        /// <summary>
        /// Con el Mapa v2, «Faltan N» va dentro del rótulo de la parada siguiente (<c>map_stop_label_remaining</c>; respuesta del dueño
        /// a la pregunta 2 de M2-3, hecha en M2-4) y no en la placa.
        /// </summary>
        public bool StopRemainingInLabel => _remainingMerged;

        /// <summary>El texto del rótulo de la parada <paramref name="n"/> («» si no lleva).</summary>
        internal string StopLabelText(int n)
        {
            int i = StopMarkerIndexOf(n);
            return i >= 0 && i < _stopLabels.Count && _stopLabels[i].Pill.gameObject.activeSelf ? _stopLabels[i].Label.text : string.Empty;
        }

        /// <summary>El rótulo de la parada <paramref name="n"/> (tests: su caja y su texto), o null.</summary>
        internal TMPro.TextMeshProUGUI? StopLabelOf(int n)
        {
            int i = StopMarkerIndexOf(n);
            return i >= 0 && i < _stopLabels.Count ? _stopLabels[i].Label : null;
        }

        /// <summary>La parada siguiente cuyo rótulo lleva «Faltan N» con el Mapa v2, o −1 (sin el Mapa v2, sin «Faltan N» o hacia la naciente).</summary>
        internal int RemainingStopN()
        {
            if (!_live || !_remainingReady || _stops == null)
            {
                return -1;
            }

            RiverStop? current = _stops.StopOf(_playLevel);
            RiverStop? next = current != null ? _stops.At(current.N + 1) : null;
            return next != null ? next.N : -1; // tras la última parada, hacia la naciente, la placa de M2-3 [P]
        }

        // La placa de «Faltan N» sale solo si el rótulo de la parada siguiente no lo puede llevar (la naciente, un nombre sin aprobar
        // o, desde el añadido de D-069 del 2026-11-08, una parada en abanico sin ningún sitio libre para su rótulo, o un rótulo que no
        // cabe entero a la vista con el encuadre de apertura); si cambia, se recoloca. Con el Mapa de la 1.0, como siempre.
        private void SyncRemainingPlate()
        {
            int n = RemainingStopN();
            int i = n >= 0 ? StopMarkerIndexOf(n) : -1;
            bool merged = i >= 0 && i < _stopLabels.Count && _stopLabels[i].Pill.gameObject.activeSelf && _markers[i].Root.gameObject.activeSelf
                && _stopLabels[i].Label.text.Length > 0 && !_remainingOffView;
            _remainingMerged = merged;
            bool want = _remainingReady && !merged;
            if (_remaining.gameObject.activeSelf == want)
            {
                return;
            }

            _remaining.gameObject.SetActive(want);
            if (want)
            {
                CapiLayout(_capiFrameTop);
            }
            else
            {
                _remainingChipView = default;
            }
        }

        /// <summary>La placa de «Faltan N» en el Mapa v2, en la zona visible (dp, y hacia abajo); vacía si no sale.</summary>
        internal Rect RemainingChipViewRect => _remainingChipView;

        // Lo fijo de arriba de la zona del mapa para el encuadre de las mini cuias: «Andes ↑» y, en el Mapa v2, la placa de «Faltan N».
        private Rect WithRemainingChip(Rect andes)
        {
            Rect chip = _remainingChipView;
            if (!_live || chip.width <= 0f)
            {
                return andes;
            }

            return andes.width <= 0f ? chip : Rect.MinMaxRect(Mathf.Min(andes.xMin, chip.xMin), Mathf.Min(andes.yMin, chip.yMin), Mathf.Max(andes.xMax, chip.xMax), Mathf.Max(andes.yMax, chip.yMax));
        }
    }
}
