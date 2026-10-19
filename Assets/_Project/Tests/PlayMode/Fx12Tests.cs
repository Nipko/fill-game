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
    /// <item><description>el giro entre dos cuerdas seguidas de cada cinta dibujada (vidrio, lleno, frente y sus orlas) no pasa de
    /// <see cref="MaxTurnDegrees"/> (ni de <see cref="MaxTightTurnDegrees"/> en un codo entre cuerdas cortas, un meandro de los
    /// datos más cerrado que la cinta);</description></item>
    /// <item><description>el agua y la orla del lleno y del frente quedan a ≤ 2 dp del eje del vidrio de su línea en todo su
    /// largo (sus secciones y el punto medio de cada paso);</description></item>
    /// <item><description>la boca de cada afluente sigue cayendo sobre su anfitrión: la punta dibujada del afluente queda del eje
    /// dibujado del anfitrión a no más que del de cuerdas de antes de FX12, de su polilínea (las bocas de los datos que no caen en
    /// ella) o de medio ancho del anfitrión;</description></item>
    /// <item><description>en la Cuenca, ninguna cinta lleva la curva (sus capturas no cambian).</description></item>
    /// </list>
    /// Las capturas son las de <see cref="Fx12SnapshotTests"/>.
    /// </summary>
    public sealed class Fx12Tests
    {
        /// <summary>
        /// Giro máximo entre dos cuerdas seguidas de una cinta del Mundo, en grados. La curva se parte donde una cuerda gira más de
        /// <see cref="RiverRibbonGraphic.CurveMaxTurnDegrees"/> (25°) respecto de la siguiente o de la curva junto a sus puntas,
        /// así que entre cuerdas quedan unos 25° y algo más donde se unen dos tramos de la curva o donde la corta una sección del
        /// plan (una punta del lleno, un cambio de agua). 30° es el umbral: a 8 dp de ancho, una esquina de 30° entre cuerdas de
        /// varios dp deja el inglete 4 · (1 / cos 15° − 1) ≈ 0,14 dp por fuera de un arco (menos de medio píxel a 3 px/dp), y antes
        /// de FX12 los giros entre cuerdas del vidrio pasaban de 120° (el registro lo mide con las cuerdas de antes).
        /// </summary>
        public const float MaxTurnDegrees = 30f;

        /// <summary>
        /// Cuerdas cortas, en dp: un codo entre dos cuerdas de menos de esto es un meandro de los datos más cerrado que el medio
        /// ancho de la cinta (r = c / giro &lt; 2 dp / 30° ≈ 3,8 dp), que ninguna cinta de ese ancho puede dibujar redondo; se mide
        /// aparte, con <see cref="MaxTightTurnDegrees"/>. Pasa en la vista Mundo a 15× (el zoom máximo), con el Madre de Dios.
        /// </summary>
        public const float TightChordDp = 2f;

        /// <summary>Giro máximo en un codo de cuerdas cortas (<see cref="TightChordDp"/>), en grados: sin picos ni vueltas atrás.</summary>
        public const float MaxTightTurnDegrees = 90f;

        /// <summary>Distancia máxima del agua (y su orla) al eje del vidrio de su línea, en dp.</summary>
        public const float MaxWaterApartDp = 2f;

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

        private static void CheckWorld(RiverMapScreen map, RiverMap data, string at)
        {
            float worst = 0f;
            float tight = 0f;
            string worstAt = string.Empty;
            string tightAt = string.Empty;
            int ribbons = 0;
            int bevels = 0;
            foreach (RiverRibbonGraphic ribbon in Ribbons(map, data))
            {
                Assert.That(ribbon.Curve, Is.True, at + ": " + ribbon.name + " con curva en el Mundo");
                ribbons++;
                bevels += ribbon.LastBevelCount;
                float turn = MaxTurn(ribbon, out int where, out float tightTurn, out int tightWhere);
                if (tightTurn > tight)
                {
                    tight = tightTurn;
                    tightAt = ribbon.name + " en la sección " + tightWhere + Around(ribbon, tightWhere);
                }

                if (turn > worst)
                {
                    worst = turn;
                    worstAt = ribbon.name + " en la sección " + where + " de " + ribbon.LastSectionPointCount + Around(ribbon, where);
                }
            }

            Debug.Log("AF_FX12 " + at + ": " + ribbons + " cintas, giro máximo " + worst.ToString("0.0", CultureInfo.InvariantCulture)
                + "° (" + worstAt + "), en cuerdas cortas " + tight.ToString("0.0", CultureInfo.InvariantCulture) + "° (" + tightAt + "), "
                + bevels + " biseles; zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture));
            Assert.That(ribbons, Is.GreaterThan(3), at + ": cintas dibujadas");
            Expect(worst <= MaxTurnDegrees, at + ": giro máximo entre pasos, " + worstAt);
            Expect(tight <= MaxTightTurnDegrees, at + ": giro máximo en cuerdas cortas, " + tightAt);

            // Lo más que la curva se aparta de los puntos de la línea (solo el registro: los datos no cambian, la canoa y las paradas
            // van sobre la polilínea).
            float drift = 0f;
            float oldDrift = 0f;
            float oldTurn = 0f;
            string driftAt = string.Empty;
            foreach (RiverRibbonGraphic ribbon in Ribbons(map, data))
            {
                if (!ribbon.name.StartsWith("Glass_", StringComparison.Ordinal))
                {
                    continue;
                }

                PolylineCursor cursor = ribbon.Cursor!;
                RiverRibbonGraphic chords = Chords(ribbon);
                oldTurn = Mathf.Max(oldTurn, MaxTurn(chords, out _, out float oldTight, out _), oldTight);
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
                + " dp, y giro máximo del vidrio " + oldTurn.ToString("0.0", CultureInfo.InvariantCulture) + "°");

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
                float allowed = Mathf.Max(Mathf.Max(chords, polyline) + (RiverRibbonGraphic.CurveAnchorWidths * host.WidthDp), 0.5f * host.WidthDp);
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

        // Las cintas dibujadas del Mapa: el vidrio, el lleno y su orla, y el frente y su orla (las activas con malla).
        private static System.Collections.Generic.List<RiverRibbonGraphic> Ribbons(RiverMapScreen map, RiverMap data)
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

        // Giro máximo (grados) entre dos cuerdas seguidas de la cinta, en pantalla, y aparte el de los codos entre dos cuerdas cortas
        // (TightChordDp); las de menos de 0,25 dp (las dos secciones de un cambio de agua, en el mismo punto) no cuentan.
        private static float MaxTurn(RiverRibbonGraphic ribbon, out int where, out float tight, out int tightWhere)
        {
            float dp = ribbon.Space.DpPerUnit;
            float worst = 0f;
            where = -1;
            tight = 0f;
            tightWhere = -1;
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
                    bool shortChords = previous.magnitude < TightChordDp && d.magnitude < TightChordDp;
                    if (shortChords && turn > tight)
                    {
                        tight = turn;
                        tightWhere = k;
                    }
                    else if (!shortChords && turn > worst)
                    {
                        worst = turn;
                        where = k;
                    }
                }

                previous = d;
                hasPrevious = true;
            }

            return worst;
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
        private static float ApartDp(RiverRibbonGraphic ribbon, RiverRibbonGraphic glass)
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
