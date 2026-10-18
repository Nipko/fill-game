#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AF.Levels;
using AF.Levels.Json;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Tabla de temporada del cliente y su LUT en el fondo (tarea SE2; pregunta 32 b de docs/GDD.md §6.3, D-020; D-009, punto 6;
    /// 03 §12; 07 §2.1): <see cref="SeasonTableReader"/> con la tabla real (<c>seasons_v1.json</c>, de SE1) y sus rechazos;
    /// <see cref="SeasonTable.SeasonOf"/> igual, nivel a nivel, a las temporadas de los calendarios versionados
    /// (<c>campaign_v1.json</c> y <c>campaign_v2.json</c>, leídos aparte) en toda la tabla (21-200 y lo que añada P7 hasta el
    /// 700), sin temporada en 1-20 ni tras la tabla, y sin asignaciones; y en <see cref="StopBackdrop"/>, la LUT de temporada
    /// sobre la de la parada, la banda de agua sin ella (VA-23), el cielo brillante con las dos (VA-03) y sin la tabla, como antes.
    /// </summary>
    public sealed class SeasonTableTests
    {
        // La ruta de GameSceneBuilder.SeasonTablePath (el ensamblado de Editor no se referencia desde aquí).
        private const string TablePath = "Assets/_Project/Data/Levels/seasons_v1.json";
        private const string CampaignV1 = "tools/level-generator/specs/campaign_v1.json";
        private const string CampaignV2 = "tools/level-generator/specs/campaign_v2.json";

        private const float Horizon2 = 118f;
        private const float PanelTop2 = 162.1f;
        private const float BandBottom2 = 560f;

        private static SeasonTable? s_real;
        private static int s_sink;

        private GameObject? _canvas;

        private static SeasonTable Real => s_real ??= SeasonTableReader.Read(File.ReadAllText(LocTables.ProjectPath(TablePath), Encoding.UTF8));

        private static RiverStopCatalog Stops => MapTestCatalogs.Real;

        [TearDown]
        public void TearDown()
        {
            GameScreen.SeasonLutEnabled = true;
            GameScreen.SeasonOverride = null;
            StopBackdrop.HeroSpriteOverride = null;
            if (_canvas != null)
            {
                Object.DestroyImmediate(_canvas);
            }
        }

        [Test]
        public void TheRealTable_StartsAt21_AndCoversAtLeast21To300()
        {
            SeasonTable t = Real;
            Assert.That(t.FirstLevel, Is.EqualTo(21), "1-20 (tutorial y rampa) no tienen temporada (SE1)");
            Assert.That(t.LastLevel, Is.GreaterThanOrEqualTo(300), "hasta el último nivel publicado (P7a: 300)");
            Assert.That(t.LastLevel, Is.LessThanOrEqualTo(700), "P7 la amplía como mucho hasta el 700");
            Assert.That((t.LastLevel - 20) % SeasonTable.CycleLength, Is.EqualTo(0), "acaba con un ciclo entero");
            StringAssert.Contains(CampaignV1, t.Source);
        }

        [Test]
        public void SeasonOf_MatchesTheVersionedCalendars_LevelByLevel()
        {
            // Las temporadas de los calendarios, leídas aparte (sin la herramienta ni la tabla): "seasons" de cada especificación.
            var expected = new Dictionary<int, string>();
            foreach (string spec in new[] { CampaignV1, CampaignV2 })
            {
                JsonValue root = JsonParser.Parse(File.ReadAllText(LocTables.ProjectPath(spec), Encoding.UTF8));
                JsonValue seasons = root.Get("seasons")!;
                foreach (KeyValuePair<string, JsonValue> kv in seasons.Members)
                {
                    expected[int.Parse(kv.Key, System.Globalization.CultureInfo.InvariantCulture)] = kv.Value.AsString();
                }
            }

            SeasonTable t = Real;
            int a = 0;
            int b = 0;
            for (int level = 1; level <= t.LastLevel; level++)
            {
                Season s = t.SeasonOf(level);
                if (level <= 20)
                {
                    Assert.That(s, Is.EqualTo(Season.None), "nivel " + level + ": sin temporada");
                    continue;
                }

                int from = level - ((level - 1) % SeasonTable.CycleLength);
                Assert.That(expected.ContainsKey(from), Is.True, "ciclo " + from + " en los calendarios");
                Season want = expected[from] == "A" ? Season.HighWater : Season.LowWater;
                Assert.That(s, Is.EqualTo(want), "nivel " + level + " (ciclo " + from + ", " + expected[from] + ")");
                a += s == Season.HighWater ? 1 : 0;
                b += s == Season.LowWater ? 1 : 0;
            }

            Assert.That(a > 0 && b > 0, Is.True, "hay ciclos A y B");
            Assert.That(t.SeasonOf(t.LastLevel + 1), Is.EqualTo(Season.None), "tras la tabla, sin temporada (como antes)");
            Assert.That(t.SeasonOf(0), Is.EqualTo(Season.None));
            Assert.That(t.SeasonOf(-5), Is.EqualTo(Season.None));
            Assert.That(t.SeasonOf(int.MaxValue), Is.EqualTo(Season.None));
        }

        [Test]
        public void SeasonOf_TheCyclesOf21To200_AsInCampaignV1()
        {
            // La tabla d.4 de 21-200 (campaign_v1.json): B A B A B A A B B.
            Season[] cycles =
            {
                Season.LowWater, Season.HighWater, Season.LowWater, Season.HighWater, Season.LowWater, Season.HighWater,
                Season.HighWater, Season.LowWater, Season.LowWater,
            };
            for (int i = 0; i < cycles.Length; i++)
            {
                int from = 21 + (20 * i);
                Assert.That(Real.SeasonOf(from), Is.EqualTo(cycles[i]), "primer nivel del ciclo " + from);
                Assert.That(Real.SeasonOf(from + 19), Is.EqualTo(cycles[i]), "último nivel del ciclo " + from);
                Assert.That(Real.CycleAt(i), Is.EqualTo(cycles[i]));
            }

            Assert.That(SeasonTable.Letter(Season.HighWater), Is.EqualTo("A"));
            Assert.That(SeasonTable.Letter(Season.LowWater), Is.EqualTo("B"));
            Assert.That(SeasonTable.Letter(Season.None), Is.Null);
        }

        [Test]
        public void SeasonOf_DoesNotAllocate()
        {
            SeasonTable t = Real;
            TestDelegate call = () =>
            {
                int sum = 0;
                for (int level = -3; level <= 1005; level++)
                {
                    sum += (int)t.SeasonOf(level);
                    sum += (int)GameScreen.SeasonFor(t, level);
                }

                s_sink = sum;
            };
            call();
            Assert.That(call, Is.Not.AllocatingGCMemory(), "sin asignaciones (SE2)");
            Assert.That(s_sink, Is.GreaterThan(0));
        }

        [Test]
        public void SeasonFor_WithoutTheTable_OrSwitchedOff_IsNone()
        {
            Assert.That(GameScreen.SeasonFor(null, 41), Is.EqualTo(Season.None), "sin la tabla, como antes");
            Assert.That(GameScreen.SeasonFor(Real, 41), Is.EqualTo(Season.HighWater));
            GameScreen.SeasonLutEnabled = false;
            Assert.That(GameScreen.SeasonFor(Real, 41), Is.EqualTo(Season.None));
        }

        [Test]
        public void SeasonFor_WithTheCaptureOverride_IsTheForcedSeason_UnlessSwitchedOff()
        {
            GameScreen.SeasonOverride = Season.LowWater;
            Assert.That(GameScreen.SeasonFor(Real, 41), Is.EqualTo(Season.LowWater), "la B en una parada de ciclo A (capturas)");
            Assert.That(GameScreen.SeasonFor(null, 41), Is.EqualTo(Season.LowWater), "también sin tabla");
            GameScreen.SeasonLutEnabled = false;
            Assert.That(GameScreen.SeasonFor(Real, 41), Is.EqualTo(Season.None), "apagada manda");
            GameScreen.SeasonLutEnabled = true;
            GameScreen.SeasonOverride = null;
            Assert.That(GameScreen.SeasonFor(Real, 41), Is.EqualTo(Season.HighWater), "sin forzar, la de la tabla");
        }

        /// <summary>
        /// La escena <c>Game</c> que regenera <c>GameSceneBuilder.CreateGameScene</c> lleva la tabla en el campo
        /// <c>seasonTable</c> de <see cref="GameBootstrap"/> (se lee el YAML, nunca se edita), y
        /// <see cref="GameBootstrap.LoadSeasons"/> la lee.
        /// </summary>
        [Test]
        public void Scene_CarriesTheTable_AndTheBootstrapLoadsIt()
        {
            string meta = File.ReadAllText(LocTables.ProjectPath(TablePath + ".meta"));
            Match guid = Regex.Match(meta, "^guid: ([0-9a-f]{32})$", RegexOptions.Multiline);
            Assert.That(guid.Success, Is.True, "el .meta de la tabla lo genera Unity");
            string scene = File.ReadAllText(LocTables.ProjectPath("Assets/_Project/Scenes/Game.unity"));
            Assert.That(scene, Does.Contain("seasonTable: {fileID: 4900000, guid: " + guid.Groups[1].Value + ", type: 3}"),
                "Game.unity regenerada con la tabla de temporada (GameSceneBuilder.CreateGameScene)");

            var asset = new TextAsset(File.ReadAllText(LocTables.ProjectPath(TablePath), Encoding.UTF8));
            try
            {
                SeasonTable? table = GameBootstrap.LoadSeasons(asset);
                Assert.That(table, Is.Not.Null);
                Assert.That(table!.SeasonOf(21), Is.EqualTo(Season.LowWater));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void LoadSeasons_WithoutTheFileOrInvalid_WarnsOrLogs_AndGivesNull()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new Regex("falta la tabla de temporada"));
            Assert.That(GameBootstrap.LoadSeasons(null), Is.Null);

            var broken = new TextAsset("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"C\"}]}");
            try
            {
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new Regex(@"cycles\[0\]\.season.*sin LUT de temporada"));
                Assert.That(GameBootstrap.LoadSeasons(broken), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(broken);
            }
        }

        [TestCase("[]", "$")]
        [TestCase("{\"format\": \"af-seasons/2\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"B\"}]}", "format")]
        [TestCase("{\"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"B\"}]}", "format")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"\", \"cycles\": [{\"from_level\": 21, \"season\": \"B\"}]}", "source")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": []}", "cycles")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\"}", "cycles")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [7]}", "cycles[0]")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 22, \"season\": \"B\"}]}", "cycles[0].from_level")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 0, \"season\": \"B\"}]}", "cycles[0].from_level")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"season\": \"B\"}]}", "cycles[0].from_level")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"B\"}, {\"from_level\": 61, \"season\": \"A\"}]}", "cycles[1].from_level")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"B\"}, {\"from_level\": 21, \"season\": \"A\"}]}", "cycles[1].from_level")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"C\"}]}", "cycles[0].season")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21, \"season\": \"a\"}]}", "cycles[0].season")]
        [TestCase("{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21}]}", "cycles[0].season")]
        public void Read_RejectsAnInvalidTable_WithThePathOfTheValue(string json, string where)
        {
            LevelFormatException? e = Assert.Throws<LevelFormatException>(() => SeasonTableReader.Read(json));
            StringAssert.StartsWith("Tabla de temporada, " + where + ":", e!.Message);
        }

        [Test]
        public void Read_RejectsNotJson_AndAFractionalLevel()
        {
            Assert.Throws<LevelFormatException>(() => SeasonTableReader.Read("{"));
            Assert.Throws<LevelFormatException>(() => SeasonTableReader.Read(
                "{\"format\": \"af-seasons/1\", \"source\": \"s\", \"cycles\": [{\"from_level\": 21.5, \"season\": \"B\"}]}"));
        }

        [Test]
        public void Read_AcceptsASmallTable_StartingInAnyCycle()
        {
            SeasonTable t = SeasonTableReader.Read(
                "{\"format\": \"af-seasons/1\", \"source\": \"a, b\", \"cycles\": [{\"from_level\": 41, \"season\": \"A\"}, {\"from_level\": 61, \"season\": \"B\"}]}");
            Assert.That(t.Source, Is.EqualTo("a, b"));
            Assert.That(t.FirstLevel, Is.EqualTo(41));
            Assert.That(t.LastLevel, Is.EqualTo(80));
            Assert.That(t.CycleCount, Is.EqualTo(2));
            Assert.That(t.SeasonOf(40), Is.EqualTo(Season.None));
            Assert.That(t.SeasonOf(41), Is.EqualTo(Season.HighWater));
            Assert.That(t.SeasonOf(60), Is.EqualTo(Season.HighWater));
            Assert.That(t.SeasonOf(61), Is.EqualTo(Season.LowWater));
            Assert.That(t.SeasonOf(80), Is.EqualTo(Season.LowWater));
            Assert.That(t.SeasonOf(81), Is.EqualTo(Season.None));
            Assert.That(t.CycleAt(2), Is.EqualTo(Season.None));
        }

        [Test]
        public void TheSeasonLut_GoesOverTheLightOfTheStop()
        {
            Assert.That(StopBackdrop.SeasonLutOf(Season.None), Is.EqualTo(Color.white), "sin temporada, como antes");
            Assert.That(StopBackdrop.SeasonLutOf(Season.HighWater), Is.EqualTo(StopBackdrop.HighWaterLut));
            Assert.That(StopBackdrop.SeasonLutOf(Season.LowWater), Is.EqualTo(StopBackdrop.LowWaterLut));
            Assert.That(StopBackdrop.HighWaterLut, Is.Not.EqualTo(StopBackdrop.LowWaterLut), "A y B se distinguen");
            Assert.That(StopBackdrop.HighWaterLut.a, Is.EqualTo(1f));
            Assert.That(StopBackdrop.LowWaterLut.a, Is.EqualTo(1f));

            StopBackdrop b = NewBackdrop();
            RiverStop leticia = Stops.At(9)!; // 91-100, atardecer, en el ciclo 81 (A)
            Assert.That(Real.SeasonOf(91), Is.EqualTo(Season.HighWater), "premisa");
            b.Apply(leticia, "white", false, 0f, Real.SeasonOf(91));
            Assert.That(b.Season, Is.EqualTo(Season.HighWater));
            Assert.That(b.LutColor, Is.EqualTo(StopBackdrop.DuskLut * StopBackdrop.HighWaterLut), "la de la parada por la de la temporada");
            Assert.That(b.LutColor, Is.EqualTo(StopBackdrop.LutOf(leticia, Season.HighWater)));

            // La misma parada sin temporada: la luz de la parada sola (sin la tabla, como antes).
            b.Apply(leticia, "white", false, 0f);
            Assert.That(b.Season, Is.EqualTo(Season.None));
            Assert.That(b.LutColor, Is.EqualTo(StopBackdrop.DuskLut));
            Assert.That(b.BuildMs, Is.GreaterThan(0d), "cambiar solo la temporada recompone");

            // Sin parada (del 201 en adelante sin catálogo): la temporada se ve igual, sobre el panorama.
            b.Apply(null, "white", false, 0f, Season.LowWater);
            Assert.That(b.Stop, Is.Null);
            Assert.That(b.Root.gameObject.activeSelf, Is.False, "nada del fondo de la parada");
            Assert.That(b.LutColor, Is.EqualTo(StopBackdrop.LowWaterLut));
            b.Apply(null, "white", false, 0f);
            Assert.That(b.LutColor, Is.EqualTo(Color.white), "sin parada ni temporada, el panorama de siempre");
        }

        [Test]
        public void SameStop_OtherSeason_CrossfadesTheLight()
        {
            // Con ui_bg_per_stop = false, la gran parada del mundo sigue de un ciclo al siguiente: cambia solo la luz.
            StopBackdrop b = NewBackdrop();
            RiverStop stop = Stops.At(5)!; // Manaos (51-60)
            b.Apply(stop, "black", false, 0f, Season.HighWater);
            b.Apply(stop, "black", true, 200f, Season.LowWater);
            Assert.That(b.Fading, Is.True);
            Assert.That(b.DisplayedLutColor, Is.EqualTo(StopBackdrop.HighWaterLut), "al empezar, la de antes");
            b.Tick(100f);
            Assert.That(b.DisplayedLutColor.b, Is.EqualTo(Mathf.Lerp(StopBackdrop.HighWaterLut.b, StopBackdrop.LowWaterLut.b, 0.5f)).Within(0.001f));
            b.Tick(100f);
            Assert.That(b.Fading, Is.False);
            Assert.That(b.DisplayedLutColor, Is.EqualTo(StopBackdrop.LowWaterLut));
            b.Apply(stop, "black", true, 200f, Season.LowWater);
            Assert.That(b.Fading, Is.False, "la misma composición no se repite");
            Assert.That(b.BuildMs, Is.EqualTo(0d));
        }

        [Test]
        public void VA23_TheBand_KeepsTheWaterOfTheWorld_InBothSeasons()
        {
            StopBackdrop b = NewBackdrop();
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                string water = WaterOf(stop.World);
                b.Apply(stop, water, false, 0f);
                Color[] reference = BandColors(b);
                foreach (Season season in new[] { Season.HighWater, Season.LowWater })
                {
                    b.Apply(stop, water, false, 0f, season);
                    Assert.That(b.BandColor, Is.EqualTo(Palette.WaterBase(water)), "parada " + n + ": el preset del mundo");
                    Color[] band = BandColors(b);
                    Assert.That(band, Is.EqualTo(reference), "parada " + n + ", " + season + ": la banda no lleva la LUT de temporada");
                    for (int i = 0; i < band.Length; i++)
                    {
                        if (band[i].a >= 0.99f)
                        {
                            Assert.That(ColorMath.DeltaE00(band[i], Palette.WaterBase(water)), Is.LessThanOrEqualTo(5f),
                                "parada " + n + ", vértice " + i + ": ΔE00 ≤ 5 (VA-23)");
                        }
                    }
                }
            }
        }

        [Test]
        public void VA03_TheSkyStaysBright_WithTheStopLightAndTheSeason()
        {
            Color[] pano = { Palette.SkyTop, Palette.SkyMid, Palette.SkyHorizon };

            // Con la parada de cada nivel y la temporada de su ciclo (las de los 10 niveles de una parada son la misma).
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                int first = (10 * n) + 1;
                Season season = Real.SeasonOf(first);
                Assert.That(Real.SeasonOf(first + 9), Is.EqualTo(season), "parada " + n + ": un solo ciclo");
                StopBackdrop.SkyOf(stop, out Color top, out Color horizon, out float aTop, out float aHorizon);
                Color lut = StopBackdrop.LutOf(stop, season);
                Color[] tint = { top, Color.Lerp(top, horizon, 0.5f), horizon };
                float[] alpha = { aTop, (aTop + aHorizon) * 0.5f, aHorizon };
                AssertBright(pano, lut, tint, alpha, "parada " + n + " (" + season + ")");

                // Y la otra temporada, por si el calendario cambia.
                Season other = season == Season.HighWater ? Season.LowWater : Season.HighWater;
                AssertBright(pano, StopBackdrop.LutOf(stop, other), tint, alpha, "parada " + n + " (" + other + ")");
            }

            // Sin parada: el panorama solo con la luz de la temporada.
            var none = new[] { Color.white, Color.white, Color.white };
            var zero = new[] { 0f, 0f, 0f };
            AssertBright(pano, StopBackdrop.HighWaterLut, none, zero, "aguas altas sin parada");
            AssertBright(pano, StopBackdrop.LowWaterLut, none, zero, "aguas bajas sin parada");
        }

        private static void AssertBright(Color[] pano, Color lut, Color[] tint, float[] alpha, string what)
        {
            float s = 0f;
            float v = 0f;
            for (int i = 0; i < 3; i++)
            {
                Color c = Color.Lerp(pano[i] * lut, tint[i], alpha[i]);
                Color.RGBToHSV(c, out _, out float si, out float vi);
                Assert.That(vi, Is.GreaterThanOrEqualTo(0.25f), what + ": nada por debajo de V 0,25");
                s += si / 3f;
                v += vi / 3f;
            }

            Assert.That(v, Is.GreaterThanOrEqualTo(0.55f), what + ": V medio (VA-03)");
            Assert.That(s, Is.GreaterThanOrEqualTo(0.30f), what + ": S medio (VA-03)");
        }

        private static Color[] BandColors(StopBackdrop b)
        {
            StopShapeGraphic g = b.BandGraphic;
            var colors = new Color[g.VertexCount];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = g.ColorAt(i);
            }

            return colors;
        }

        private static string WaterOf(int world)
        {
            return world == 2 ? "clear" : (world == 3 ? "black" : "white");
        }

        private StopBackdrop NewBackdrop()
        {
            StopBackdrop.HeroSpriteOverride = _ => null;
            _canvas = new GameObject("SeasonTableTestCanvas", typeof(RectTransform), typeof(Canvas));
            var scene = new GameObject("Scene", typeof(RectTransform));
            scene.transform.SetParent(_canvas.transform, false);
            var b = new StopBackdrop((RectTransform)scene.transform, null);
            b.Layout(360f, Horizon2, PanelTop2, true, BandBottom2);
            return b;
        }
    }
}
