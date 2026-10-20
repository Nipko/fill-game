#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarea FX12 (observación 1 de la prueba de la parte A de la ola 7 en el teléfono): en la vista Mundo los ríos se veían como
    /// líneas quebradas. Desde FX12, las cintas del Mundo siguen la curva de Catmull-Rom centrípeta que pasa por los puntos del
    /// trazo suave (<see cref="RiverRibbonGraphic.SetCurve"/>), solo al dibujar. Aquí, con el Mapa abierto desde el inicio a tamaño
    /// de teléfono (360 × 780 dp) y <c>max_level</c> 58, 120 y 300, en el Mundo:
    /// <list type="bullet">
    /// <item><description>lo que se ve de cada codo entre dos cuerdas seguidas de cada cinta dibujada (vidrio, lleno, frente y sus
    /// orlas): su flecha frente a la curva, que depende del largo de las cuerdas (<see cref="CornerSagittaDp"/>), no pasa de
    /// <see cref="MaxSagittaDp"/>, y su giro no pasa de <see cref="MaxTurnDegrees"/> entre cuerdas largas ni de
    /// <see cref="MaxTightTurnDegrees"/> entre cuerdas cortas (un meandro de los datos más cerrado que la cinta);</description></item>
    /// <item><description>el agua y la orla del lleno y del frente quedan a ≤ 2 dp del eje del vidrio de su línea en todo su
    /// largo (sus secciones y el punto medio de cada paso);</description></item>
    /// <item><description>la boca de cada afluente sigue cayendo sobre su anfitrión: la punta dibujada del afluente queda del eje
    /// dibujado del anfitrión a no más que del de cuerdas de antes de FX12 o de medio ancho del anfitrión (con
    /// <see cref="MouthRoundingDp"/>); las bocas de los datos que no caen en la polilínea del anfitrión se anotan en el registro;</description></item>
    /// <item><description>lo que se coloca sobre la línea (las paradas, las boyas y la canoa) queda a ≤ <see cref="MaxMarkerApartWidths"/>
    /// anchos del eje dibujado del vidrio de su línea;</description></item>
    /// <item><description>en la Cuenca, ninguna cinta lleva la curva (sus capturas no cambian).</description></item>
    /// </list>
    /// Lo mismo (codos y agua sobre el vidrio) en las cintas del viaje del banner del 41 y del 81, que también llevan la curva.
    /// Las capturas son las de <see cref="Fx12SnapshotTests"/>.
    /// </summary>
    public sealed class Fx12Tests
    {
        /// <summary>
        /// Giro máximo entre dos cuerdas largas seguidas (las dos de al menos <see cref="TightChordDp"/>) de una cinta del Mundo, en
        /// grados. La curva se parte donde una cuerda gira más de <see cref="RiverRibbonGraphic.CurveMaxTurnDegrees"/> (10°)
        /// respecto de la siguiente o de la curva junto a sus puntas; queda algo más donde se unen dos tramos de la curva o donde la
        /// corta una sección del plan (una punta del lleno, un cambio de agua). 12° es el umbral: a 8 dp de ancho, un codo de 12°
        /// desplaza el borde de fuera de un arco 4 · (1 / cos 6° − 1) ≈ 0,02 dp en el codo y, con las cuerdas que deja la tolerancia
        /// de la curva, menos de 1 px a 3 px/dp. Con 25° (la primera versión de FX12) las capturas aún mostraban esquinas.
        /// </summary>
        public const float MaxTurnDegrees = 12f;

        /// <summary>
        /// Flecha máxima de un codo, en dp: lo más que el borde de fuera de la cinta en el codo se aparta del arco que lo redondearía
        /// (<see cref="CornerSagittaDp"/>), que crece con el giro y con el largo de las cuerdas. 0,4 dp es algo más de 1 px a 3 px/dp:
        /// la tolerancia de la curva (<see cref="RiverRibbonGraphic.CurveToleranceDp"/>, 0,3 dp) y lo que añade el ancho. Las cuerdas
        /// de unos 14 dp con 25° de la primera versión de FX12 daban 0,8 dp (2,3 px), que se veían como esquinas.
        /// </summary>
        public const float MaxSagittaDp = 0.4f;

        /// <summary>
        /// Cuerdas cortas, en dp (<see cref="RiverRibbonGraphic.CurveShortChordDp"/>): un codo con una cuerda de menos de esto es
        /// un meandro cerrado o la punta de un tramo de la curva, de pocos dp de radio (el Madre de Dios o el Beni en el Mundo), donde un giro algo mayor no se ve; se
        /// mide con <see cref="MaxTightTurnDegrees"/> y, como todos, con <see cref="MaxSagittaDp"/>, que es lo que se ve.
        /// </summary>
        public const float TightChordDp = 4f;

        /// <summary>
        /// Giro máximo en un codo de cuerdas cortas (<see cref="TightChordDp"/>), en grados: la curva los parte a 25°
        /// (<see cref="RiverRibbonGraphic.CurveMaxShortTurnDegrees"/>); con cuerdas de 2 dp, 30° dan una flecha de unos 0,3 dp a 8 dp
        /// de ancho, dentro de <see cref="MaxSagittaDp"/>, y más ya sería una esquina por corta que fuera la cuerda.
        /// </summary>
        public const float MaxTightTurnDegrees = 30f;

        /// <summary>Distancia máxima del agua (y su orla) al eje del vidrio de su línea, en dp.</summary>
        public const float MaxWaterApartDp = 2f;

        /// <summary>Redondeo con el que se compara la boca de un afluente con la de antes de FX12, en dp.</summary>
        public const float MouthRoundingDp = 0.1f;

        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        // Lo que no cumple, de las tres vistas (se comprueba al final, para registrar las tres).
        private static readonly System.Collections.Generic.List<string> s_failures = new System.Collections.Generic.List<string>();

        private static void Expect(bool ok, string what)
        {
            if (!ok)
            {
                s_failures.Add(what);
            }
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.BannerTravelOverride = null;
            LaunchOptions.Reset();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator WorldView_RibbonsAreSmooth_WaterOnTheGlass_MouthsOnTheirHost_AndTheBasinKeepsItsChords()
        {
            s_failures.Clear();
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 59;
            profile.Progress.MaxLevel = 58;
            profile.Progress.MapSeenLevel = 58;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            foreach (int max in new[] { 58, 120, 300 })
            {
                if (max != 58)
                {
                    map.Close();
                    map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!,
                        new MemoryProgressStore(max + 1, max, max), max, false);
                }

                map.SetView(MapView.World);
                map.Tick(16f);
                Canvas.ForceUpdateCanvases();
                Assert.That(map.View, Is.EqualTo(MapView.World));
                CheckWorld(map, boot.RiverMapData!, "max " + max);
                CheckMarkers(map, boot.RiverMapData!, boot.RiverRoutes!, "max " + max);

                map.SetView(MapView.Basin);
                map.Tick(16f);
                Canvas.ForceUpdateCanvases();
                foreach (RiverRibbonGraphic ribbon in Ribbons(map, boot.RiverMapData!))
                {
                    Assert.That(ribbon.Curve, Is.False, "max " + max + ", Cuenca: " + ribbon.name + " sin curva");
                }
            }

            Assert.That(s_failures, Is.Empty, string.Join(" | ", s_failures));
        }

        [UnityTest]
        public IEnumerator BannerTravel_RibbonsAreSmooth_AndWaterOnTheGlass()
        {
            // El viaje del banner del 41 y del 81 (como sus capturas en Fx12SnapshotTests): sus cintas llevan la misma curva.
            s_failures.Clear();
            foreach (int level in new[] { 41, 81 })
            {
                FunnelSetup.Hooks();
                GameScreen.BannerTravelOverride = true;
                GameScreen.SimulatedScreen = Fx10Tests.Phone;
                yield return CardSetup.Load(level - 1);
                GameBootstrap boot = GameBootstrap.Current!;
                boot.Flow!.Load(level);
                WorldBannerView banner = boot.View!.WorldBannerView;
                Assert.That(banner.Visible && banner.Travel != null, Is.True, "nivel " + level + " con su viaje");
                PlayModeHelpers.TickExact(boot, 4200);
                Canvas.ForceUpdateCanvases();
                BannerTravelStrip strip = banner.TravelStrip;
                var ribbons = new System.Collections.Generic.List<RiverRibbonGraphic>();
                for (int i = 0; i < strip.RibbonSlotCount; i++)
                {
                    Add(ribbons, strip.GlassRibbon(i));
                    Add(ribbons, strip.CasingRibbon(i));
                    Add(ribbons, strip.CoreRibbon(i));
                }

                string at = "banner del " + level;
                Assert.That(ribbons.Count, Is.GreaterThan(1), at + ": cintas dibujadas");
                foreach (RiverRibbonGraphic ribbon in ribbons)
                {
                    Assert.That(ribbon.Curve, Is.True, at + ": " + ribbon.name + " con curva");
                }

                CheckCorners(ribbons, at, string.Empty);
                float apart = 0f;
                string apartAt = string.Empty;
                foreach (RiverRibbonGraphic ribbon in ribbons)
                {
                    if (ribbon.Look != RibbonLook.Water)
                    {
                        continue;
                    }

                    float d = ApartDp(ribbon, strip.GlassRibbon(ribbon.Line!.Index));
                    if (d > apart)
                    {
                        apart = d;
                        apartAt = ribbon.name;
                    }
                }

                Debug.Log("AF_FX12 " + at + ": el agua, a " + apart.ToString("0.000", CultureInfo.InvariantCulture) + " dp del vidrio como mucho ("
                    + apartAt + ")");
                Expect(apart <= MaxWaterApartDp, at + ": el agua, sobre el vidrio (" + apartAt + ")");
            }

            Assert.That(s_failures, Is.Empty, string.Join(" | ", s_failures));
        }

        // Los codos de las cintas: la flecha (todos), el giro entre cuerdas largas y el de las cortas, con sus umbrales.
        private static void CheckCorners(System.Collections.Generic.List<RiverRibbonGraphic> ribbons, string at, string extra)
        {
            var worst = new Corner();
            var tight = new Corner();
            var sagitta = new Corner();
            int bevels = 0;
            foreach (RiverRibbonGraphic ribbon in ribbons)
            {
                bevels += ribbon.LastBevelCount;
                Corners(ribbon, worst, tight, sagitta);
            }

            Debug.Log("AF_FX12 " + at + ": " + ribbons.Count + " cintas, giro máximo " + worst.Value.ToString("0.0", CultureInfo.InvariantCulture)
                + "° (" + worst.At + "), en cuerdas cortas " + tight.Value.ToString("0.0", CultureInfo.InvariantCulture) + "° (" + tight.At
                + "), flecha máxima " + sagitta.Value.ToString("0.000", CultureInfo.InvariantCulture) + " dp (" + sagitta.At + "), "
                + bevels + " biseles" + extra);
            Expect(worst.Value <= MaxTurnDegrees, at + ": giro máximo entre cuerdas largas, " + worst.At);
            Expect(tight.Value <= MaxTightTurnDegrees, at + ": giro máximo en cuerdas cortas, " + tight.At);
            Expect(sagitta.Value <= MaxSagittaDp, at + ": flecha máxima de un codo, " + sagitta.At);
        }

        // El peor codo de una medida y dónde está.
        internal sealed class Corner
        {
            public float Value;
            public string At = string.Empty;

            public void Take(float value, RiverRibbonGraphic ribbon, int k)
            {
                if (value > Value)
                {
                    Value = value;
                    At = ribbon.name + " en la sección " + k + " de " + ribbon.LastSectionPointCount + Around(ribbon, k);
                }
            }
        }

        /// <summary>
        /// Flecha (dp) de un codo de giro <paramref name="turnDeg"/> entre dos cuerdas de <paramref name="lengthA"/> y
        /// <paramref name="lengthB"/> dp en una cinta de <paramref name="widthDp"/> dp por fuera: lo más que el borde de fuera se aparta
        /// del arco que lo redondearía. Un polígono de cuerdas c con giro θ inscrito en un arco de radio R = c / θ se aparta de él
        /// R θ² / 8 = c θ / 8; por fuera, a medio ancho w / 2 más del centro, sus cuerdas miden c + θ w / 2. Con c, la media de las dos.
        /// </summary>
        public static float CornerSagittaDp(float turnDeg, float lengthA, float lengthB, float widthDp)
        {
            float theta = turnDeg * Mathf.Deg2Rad;
            float c = 0.5f * (lengthA + lengthB);
            return (c + (0.5f * theta * widthDp)) * theta / 8f;
        }

        private static void CheckWorld(RiverMapScreen map, RiverMap data, string at)
        {
            System.Collections.Generic.List<RiverRibbonGraphic> world = Ribbons(map, data);
            foreach (RiverRibbonGraphic ribbon in world)
            {
                Assert.That(ribbon.Curve, Is.True, at + ": " + ribbon.name + " con curva en el Mundo");
            }

            Assert.That(world.Count, Is.GreaterThan(3), at + ": cintas dibujadas");

            // Las cintas con todos los pasos de la curva, con los umbrales de los codos. Las del frente que no caben en su tope
            // (RiverMapScreen.FrontCurveMaxVertices, el de FX11) eligen los pasos con una tolerancia de la escala de
            // RiverRibbonGraphic.CurveBudgetToleranceDp (≤ 1,75 dp, sin saltarlos a ciegas) y quedan, como todas, a ≤ 2 dp del vidrio.
            var full = new System.Collections.Generic.List<RiverRibbonGraphic>();
            foreach (RiverRibbonGraphic ribbon in world)
            {
                if (ribbon.LastCurveToleranceDp == 0f)
                {
                    full.Add(ribbon);
                    continue;
                }

                Debug.Log("AF_FX12 " + at + ": " + ribbon.name + " con su tope, tolerancia de la curva "
                    + ribbon.LastCurveToleranceDp.ToString("0.00", CultureInfo.InvariantCulture) + " dp");
                Expect(ribbon.name.StartsWith("Front", StringComparison.Ordinal) && ribbon.LastCurveToleranceDp > 0f,
                    at + ": " + ribbon.name + " con todos los pasos de la curva o, el frente, con una tolerancia de la escala");
            }

            CheckCorners(full, at, "; zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture));

            // Lo más que la curva se aparta de los puntos de la línea (solo el registro: los datos no cambian, la canoa y las paradas
            // van sobre la polilínea).
            float drift = 0f;
            float oldDrift = 0f;
            float oldTurn = 0f;
            float oldSag = 0f;
            string driftAt = string.Empty;
            foreach (RiverRibbonGraphic ribbon in Ribbons(map, data))
            {
                if (!ribbon.name.StartsWith("Glass_", StringComparison.Ordinal))
                {
                    continue;
                }

                PolylineCursor cursor = ribbon.Cursor!;
                RiverRibbonGraphic chords = Chords(ribbon);
                var oldWorst = new Corner();
                var oldTight = new Corner();
                var oldSagitta = new Corner();
                Corners(chords, oldWorst, oldTight, oldSagitta);
                oldTurn = Mathf.Max(oldTurn, Mathf.Max(oldWorst.Value, oldTight.Value));
                oldSag = Mathf.Max(oldSag, oldSagitta.Value);
                float a0 = ribbon.FillStart * cursor.Length;
                float a1 = ribbon.FillEnd * cursor.Length;
                for (int v = 0; v < cursor.PointCount; v++)
                {
                    float arc = cursor.ArcOfPoint(v);
                    if (arc < a0 || arc > a1)
                    {
                        continue;
                    }

                    float d = DistanceToSections(ribbon, cursor.Point(v)) * ribbon.Space.DpPerUnit;
                    oldDrift = Mathf.Max(oldDrift, DistanceToSections(chords, cursor.Point(v)) * ribbon.Space.DpPerUnit);
                    if (d > drift)
                    {
                        drift = d;
                        driftAt = ribbon.name + " en el punto " + v;
                    }
                }
            }

            Debug.Log("AF_FX12 " + at + ": la curva, a " + drift.ToString("0.00", CultureInfo.InvariantCulture) + " dp de la polilínea como mucho ("
                + driftAt + "); con las cuerdas de antes de FX12, a " + oldDrift.ToString("0.00", CultureInfo.InvariantCulture)
                + " dp, y giro máximo del vidrio " + oldTurn.ToString("0.0", CultureInfo.InvariantCulture) + "° con flecha de hasta "
                + oldSag.ToString("0.00", CultureInfo.InvariantCulture) + " dp");

            // El agua del lleno y del frente, y sus orlas, sobre el vidrio de su línea.
            float apart = 0f;
            string apartAt = string.Empty;
            foreach (RiverRibbonGraphic ribbon in Ribbons(map, data))
            {
                if (ribbon.Look != RibbonLook.Water)
                {
                    continue;
                }

                RiverRibbonGraphic glass = map.Glass(ribbon.Line!.Index);
                float d = ApartDp(ribbon, glass);
                if (d > apart)
                {
                    apart = d;
                    apartAt = ribbon.name;
                }
            }

            Debug.Log("AF_FX12 " + at + ": el agua, a " + apart.ToString("0.000", CultureInfo.InvariantCulture) + " dp del vidrio como mucho ("
                + apartAt + ")");
            Expect(apart <= MaxWaterApartDp, at + ": el agua, sobre el vidrio (" + apartAt + ")");

            // Las bocas de los afluentes, sobre su anfitrión dibujado.
            float mouthWorst = float.MinValue;
            string mouthAt = string.Empty;
            for (int i = 0; i < data.Lines.Count; i++)
            {
                RiverLine line = data.Lines[i];
                RiverRibbonGraphic glass = map.Glass(i);
                if (line.Mouth == null || !glass.gameObject.activeSelf || glass.LastSectionPointCount < 2)
                {
                    continue;
                }

                RiverMapPlace mouth = line.Mouth.Value;
                RiverRibbonGraphic host = map.Glass(mouth.Line);
                if (!host.gameObject.activeSelf || host.LastSectionPointCount < 2)
                {
                    continue;
                }

                Vector2 place = new PolylineCursor(data.Lines[mouth.Line]).PointAtS(mouth.S);
                Vector2 a = glass.LastSectionPoint(0);
                Vector2 b = glass.LastSectionPoint(glass.LastSectionPointCount - 1);
                Vector2 tip = (a - place).sqrMagnitude <= (b - place).sqrMagnitude ? a : b;
                float dp = glass.Space.DpPerUnit;
                float drawn = DistanceToSections(host, tip) * dp;
                float polyline = DistanceToPolyline(host.Cursor!, tip) * dp;
                float chords = DistanceToSections(Chords(host), tip) * dp;
                // Revisión de FX12: ninguna boca más lejos que con las cuerdas de antes de FX12 ni fuera de la cinta del anfitrión (con
                // 0,1 dp de redondeo); la curva pasa por la punta del afluente (la boca de los datos), aunque no caiga en la polilínea.
                float allowed = Mathf.Max(chords, 0.5f * host.WidthDp) + MouthRoundingDp;
                if (polyline > 0.5f * host.WidthDp)
                {
                    Debug.Log("AF_FX12 " + at + ": la boca de los datos de " + line.Id + " no cae sobre " + data.Lines[mouth.Line].Id + ": su punta, a "
                        + polyline.ToString("0.00", CultureInfo.InvariantCulture) + " dp de la polilínea ("
                        + (polyline / map.Zoom).ToString("0.00", CultureInfo.InvariantCulture) + " dp a 1×); el eje dibujado, a "
                        + drawn.ToString("0.00", CultureInfo.InvariantCulture) + " dp");
                }

                float extra = drawn - allowed;
                if (extra > mouthWorst)
                {
                    mouthWorst = extra;
                    mouthAt = line.Id + " en " + data.Lines[mouth.Line].Id + " (" + drawn.ToString("0.00", CultureInfo.InvariantCulture) + " dp del eje dibujado, "
                        + chords.ToString("0.00", CultureInfo.InvariantCulture) + " del de cuerdas de antes, "
                        + polyline.ToString("0.00", CultureInfo.InvariantCulture) + " de la polilínea)";
                }
            }

            Debug.Log("AF_FX12 " + at + ": la boca más apartada de su anfitrión dibujado, " + mouthAt);
            Expect(mouthWorst <= 0f, at + ": las bocas, sobre su anfitrión: " + mouthAt);
        }

        /// <summary>
        /// Lo que se coloca sobre la línea, sobre el río dibujado: cada parada (su punto real), cada boya (si no van en la fila corta
        /// bajo la parada) y la canoa quedan a no más de <see cref="MaxMarkerApartWidths"/> del ancho del vidrio de su línea del eje
        /// dibujado de ese vidrio. Las boyas y la canoa, en la línea cuya polilínea pasa por ellas (en una boca, la de eje más cerca).
        /// </summary>
        public const float MaxMarkerApartWidths = 0.5f;

        private static void CheckMarkers(RiverMapScreen map, RiverMap data, RiverRoutes routes, string at)
        {
            var points = new System.Collections.Generic.List<(Vector2 Point, int Line, string What)>();
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                int n = map.StopMarkerStop(i);
                RiverStopRoute? route = routes.At(n);
                if (route != null)
                {
                    points.Add((RiverMapScreen.PlacePoint(data, route.At), route.At.Line, "parada " + n));
                }
            }

            if (!map.BuoysCompact)
            {
                for (int i = 0; i < map.BuoyCount; i++)
                {
                    points.Add((map.BuoyMapPoint(i), -1, "boya " + map.BuoyLevel(i)));
                }
            }

            if (map.HasCanoe)
            {
                points.Add((map.CanoeMapPoint, -1, "canoa"));
            }

            float worst = float.MinValue;
            string worstAt = string.Empty;
            int checkedCount = 0;
            foreach ((Vector2 point, int lineIndex, string what) in points)
            {
                int line = lineIndex;
                float best = float.MaxValue;
                if (line < 0)
                {
                    // La línea que pasa por el punto (en una boca, dos: la de eje dibujado más cerca).
                    float onLine = float.MaxValue;
                    for (int i = 0; i < data.Lines.Count; i++)
                    {
                        RiverRibbonGraphic g = map.Glass(i);
                        if (!g.gameObject.activeSelf || g.LastSectionPointCount < 2)
                        {
                            continue;
                        }

                        float d = DistanceToPolyline(g.Cursor!, point) * g.Space.DpPerUnit;
                        float drawnD = DistanceToSections(g, point) * g.Space.DpPerUnit;
                        if (d < onLine - 0.01f || (d <= onLine + 0.01f && drawnD < best))
                        {
                            onLine = Mathf.Min(onLine, d);
                            best = drawnD;
                            line = i;
                        }
                    }
                }

                if (line < 0)
                {
                    continue;
                }

                RiverRibbonGraphic glass = map.Glass(line);
                if (!glass.gameObject.activeSelf || glass.LastSectionPointCount < 2)
                {
                    continue;
                }

                checkedCount++;
                float dp = glass.Space.DpPerUnit;
                float drawn = DistanceToSections(glass, point) * dp;
                float extra = drawn - (MaxMarkerApartWidths * glass.WidthDp);
                if (extra > 0f)
                {
                    Debug.Log("AF_FX12 " + at + ": " + what + " a " + drawn.ToString("0.00", CultureInfo.InvariantCulture) + " dp del eje de "
                        + glass.name + " (antes de FX12, " + (DistanceToSections(Chords(glass), point) * dp).ToString("0.00", CultureInfo.InvariantCulture) + ")");
                }

                if (extra > worst)
                {
                    worst = extra;
                    worstAt = what + " en " + glass.name + ", a " + drawn.ToString("0.00", CultureInfo.InvariantCulture) + " dp de su eje dibujado (ancho "
                        + glass.WidthDp.ToString("0.0", CultureInfo.InvariantCulture) + " dp)";
                }
            }

            Debug.Log("AF_FX12 " + at + ": " + checkedCount + " paradas, boyas y canoa; la más apartada, " + worstAt);
            Assert.That(checkedCount, Is.GreaterThan(0), at + ": hay paradas sobre las líneas");
            Expect(worst <= 0f, at + ": las paradas, las boyas y la canoa, sobre el río dibujado: " + worstAt);
        }

        // Las cintas dibujadas del Mapa: el vidrio, el lleno y su orla, y el frente y su orla (las activas con malla).
        internal static System.Collections.Generic.List<RiverRibbonGraphic> Ribbons(RiverMapScreen map, RiverMap data)
        {
            var list = new System.Collections.Generic.List<RiverRibbonGraphic>();
            for (int i = 0; i < data.Lines.Count; i++)
            {
                Add(list, map.Glass(i));
                Add(list, map.FillRibbon(i));
                Add(list, map.FillCasingRibbon(i));
            }

            for (int i = 0; i < map.FrontPieceCount; i++)
            {
                Add(list, map.FrontRibbon(i));
                Add(list, map.FrontCasingRibbon(i));
            }

            return list;
        }

        private static void Add(System.Collections.Generic.List<RiverRibbonGraphic> list, RiverRibbonGraphic ribbon)
        {
            if (ribbon.gameObject.activeInHierarchy && ribbon.Line != null && ribbon.LastVertexCount > 0 && ribbon.LastSectionPointCount >= 2)
            {
                list.Add(ribbon);
            }
        }

        // Los codos entre dos cuerdas seguidas de la cinta, en pantalla: el giro (grados) entre cuerdas largas en «worst», con alguna
        // cuerda corta (TightChordDp) en «tight» y la flecha (CornerSagittaDp, con el ancho de fuera: el agua y las dos orlas) de
        // todos en «sagitta». Las cuerdas de menos de 0,25 dp (las dos secciones de un cambio de agua, en el mismo punto) no cuentan.
        internal static void Corners(RiverRibbonGraphic ribbon, Corner worst, Corner tight, Corner sagitta)
        {
            float dp = ribbon.Space.DpPerUnit;
            float width = ribbon.WidthDp + (2f * RiverRibbonGraphic.CasingRimDp);
            Vector2 previous = Vector2.zero;
            bool hasPrevious = false;
            for (int k = 0; k + 1 < ribbon.LastSectionPointCount; k++)
            {
                Vector2 d = (ribbon.LastSectionPoint(k + 1) - ribbon.LastSectionPoint(k)) * dp;
                if (d.magnitude < 0.25f)
                {
                    continue;
                }

                if (hasPrevious)
                {
                    float turn = Vector2.Angle(previous, d);
                    bool shortChords = previous.magnitude < TightChordDp || d.magnitude < TightChordDp;
                    (shortChords ? tight : worst).Take(turn, ribbon, k);
                    sagitta.Take(CornerSagittaDp(turn, previous.magnitude, d.magnitude, width), ribbon, k);
                }

                previous = d;
                hasPrevious = true;
            }
        }

        // Las secciones de la cinta alrededor de la «where» (dp), para el registro.
        private static string Around(RiverRibbonGraphic ribbon, int where)
        {
            var text = new System.Text.StringBuilder(": ");
            float dp = ribbon.Space.DpPerUnit;
            for (int k = Mathf.Max(0, where - 3); k < Mathf.Min(ribbon.LastSectionPointCount, where + 4); k++)
            {
                Vector2 p = ribbon.LastSectionPoint(k) * dp;
                text.Append(k).Append(" (").Append(p.x.ToString("0.0", CultureInfo.InvariantCulture)).Append(", ")
                    .Append(p.y.ToString("0.0", CultureInfo.InvariantCulture)).Append(") ");
            }

            return text.ToString();
        }

        // Lo más que se aparta la cinta (sus secciones y el punto medio de cada paso) del eje dibujado del vidrio, en dp.
        internal static float ApartDp(RiverRibbonGraphic ribbon, RiverRibbonGraphic glass)
        {
            float worst = 0f;
            int n = ribbon.LastSectionPointCount;
            for (int k = 0; k < n; k++)
            {
                Vector2 p = ribbon.LastSectionPoint(k);
                worst = Mathf.Max(worst, DistanceToSections(glass, p));
                if (k + 1 < n)
                {
                    worst = Mathf.Max(worst, DistanceToSections(glass, (p + ribbon.LastSectionPoint(k + 1)) * 0.5f));
                }
            }

            return worst * ribbon.Space.DpPerUnit;
        }

        private static readonly System.Reflection.MethodInfo RibbonPopulate = typeof(RiverRibbonGraphic).GetMethod("OnPopulateMesh",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
            new[] { typeof(UnityEngine.UI.VertexHelper) }, null)!;

        // El vidrio de la línea de «ribbon» como antes de FX12 (trazo suave con cuerdas), con su espacio y su ancho: una cinta
        // aparte, fuera del lienzo, con su malla construida.
        private static RiverRibbonGraphic Chords(RiverRibbonGraphic ribbon)
        {
            var go = new GameObject("Fx12Chords", typeof(RectTransform), typeof(CanvasRenderer), typeof(RiverRibbonGraphic));
            RiverRibbonGraphic chords = go.GetComponent<RiverRibbonGraphic>();
            chords.Setup();
            chords.SetLine(ribbon.Line);
            chords.SetSpace(ribbon.Space);
            chords.SetWidth(ribbon.WidthDp);
            chords.SetSmooth(true);
            chords.SetCurve(false);
            chords.SetFill(0f, 1f);
            using (var vh = new UnityEngine.UI.VertexHelper())
            {
                RibbonPopulate.Invoke(chords, new object[] { vh });
            }

            UnityEngine.Object.Destroy(go);
            return chords;
        }

        // Distancia (unidades del Mapa) de «p» a la polilínea de las secciones de la última malla de la cinta.
        internal static float DistanceToSections(RiverRibbonGraphic ribbon, Vector2 p)
        {
            float best = float.MaxValue;
            int n = ribbon.LastSectionPointCount;
            for (int k = 0; k + 1 < n; k++)
            {
                best = Mathf.Min(best, SegmentDistance(p, ribbon.LastSectionPoint(k), ribbon.LastSectionPoint(k + 1)));
            }

            return best;
        }

        private static float DistanceToPolyline(PolylineCursor cursor, Vector2 p)
        {
            float best = float.MaxValue;
            for (int k = 0; k + 1 < cursor.PointCount; k++)
            {
                best = Mathf.Min(best, SegmentDistance(p, cursor.Point(k), cursor.Point(k + 1)));
            }

            return best;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + (ab * t) - p).magnitude;
        }
    }
}
