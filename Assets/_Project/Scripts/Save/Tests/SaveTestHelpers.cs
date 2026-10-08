#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using AF.Platform;

namespace AF.Save.Tests
{
    /// <summary>Cambia la cultura del hilo mientras dura el bloque <c>using</c>.</summary>
    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        private CultureScope(CultureInfo culture)
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            _uiCulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        public static CultureScope Use(CultureInfo culture)
        {
            return new CultureScope(culture);
        }

        /// <summary>
        /// Cultura que rompe cualquier formato que no sea InvariantCulture: base de-DE (coma decimal, punto de miles),
        /// separador de hora "." y de fecha "/", y signo menos tipográfico.
        /// </summary>
        public static CultureInfo Hostile()
        {
            CultureInfo baseCulture;
            try
            {
                baseCulture = new CultureInfo("de-DE");
            }
            catch (CultureNotFoundException)
            {
                baseCulture = CultureInfo.InvariantCulture;
            }

            var culture = (CultureInfo)baseCulture.Clone();
            culture.DateTimeFormat.TimeSeparator = ".";
            culture.DateTimeFormat.DateSeparator = "/";
            culture.NumberFormat.NegativeSign = "−";
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = ".";
            return culture;
        }

        /// <summary>th-TH (calendario budista por defecto: 2026 = 2569) si existe; si no, <see cref="Hostile"/>.</summary>
        public static CultureInfo ThaiOrHostile()
        {
            try
            {
                return new CultureInfo("th-TH");
            }
            catch (CultureNotFoundException)
            {
                return Hostile();
            }
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _culture;
            Thread.CurrentThread.CurrentUICulture = _uiCulture;
        }
    }

    /// <summary>Carpeta temporal propia de cada test (se borra en el TearDown).</summary>
    internal static class TempDirs
    {
        public static string Create()
        {
            string dir = Path.Combine(Path.GetTempPath(), "af-save-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void Delete(string? dir)
        {
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    /// <summary>Perfiles de ejemplo.</summary>
    internal static class SampleProfiles
    {
        public static readonly DateTime T0 = new DateTime(2026, 10, 5, 13, 10, 0, DateTimeKind.Utc);

        /// <summary>Perfil con todos los campos distintos de su default (incluida una hora con ticks sueltos).</summary>
        public static Profile Full()
        {
            Profile p = Profile.CreateNew(new ManualClock(T0), 5);
            p.EconomyTs = new DateTime(2026, 10, 5, 14, 3, 11, DateTimeKind.Utc).AddTicks(1234567);
            p.Wallet.Seeds = 1240;
            p.Wallet.Canoes.Count = 4;
            p.Wallet.Canoes.NextRegenAt = new DateTime(2026, 10, 5, 14, 21, 0, DateTimeKind.Utc);
            p.Wallet.Canoes.InfiniteUntil = new DateTime(2026, 10, 5, 15, 0, 0, 500, DateTimeKind.Utc);
            p.Stock.Undo = 6;
            p.Stock.Hint = 2;
            p.Stock.Reveal = 1;
            p.Stock.ExtraCuia = 3;
            p.Stock.Shuffle = 4;
            p.Daily.DateLocal = "2026-10-05";
            p.Daily.RvUndoRefill = 1;
            p.Daily.RvHint = 2;
            p.Daily.RvReveal = 3;
            p.Daily.RvExtraCanoe = 1;
            p.Daily.RvDoubleHarvest = 2;
            p.Daily.RvTotal = 9;
            p.Daily.InterstitialsToday = 5;
            p.Daily.RescuesToday = 1;
            p.Daily.AfRescueShown = 1;
            p.Daily.AfRescueDeclinedDays = 2;
            p.Daily.ShufflesToday = 1;
            p.Daily.ShieldAvailable = true;
            p.Progress.MaxLevel = 87;
            p.Progress.NextLevel = 88;
            p.Progress.SinceLastSkip = 14;
            p.Progress.SkippedLevels.Add(63);
            p.Progress.SkippedLevels.Add(71);
            p.Progress.ContentVersion = "1.0.0";
            p.LevelState = new LevelStateData
            {
                Level = 88,
                Seed = "amazonfill:v1:m04:v1:n0088",
                Variant = "plus_empty",
                FailsLevel = 1,
                Pours = 23,
                ExtraCuias = 1,
                RescuesLevel = 1,
                UndoFree = 2,
                History = "0>3;3>1;\"q\\uote\"\nñá€🌴",
                Stuck = true,
            };
            p.LevelState.RevealedCuias.Add(5);
            p.LevelState.RevealedCuias.Add(0);
            p.Entitlements.NoAds = true;
            p.Stats.LevelsWon = 86;
            p.Stats.FailsHard = 9;
            p.Stats.Rescues = 2;
            p.Stats.RvWatched = 31;
            p.Stats.Sessions = 57;
            return p;
        }

        /// <summary>
        /// El perfil más grande plausible en 1.0 para el tope de 100 KB (08 §4.3): enteros en su máximo, un Salto del
        /// Boto cada 10 niveles hasta el 3.000 y un nivel en curso con 2.000 vertidos de 4 caracteres en el historial.
        /// </summary>
        public static Profile Largest()
        {
            Profile p = Full();
            p.Wallet.Seeds = int.MaxValue;
            p.Stats.LevelsWon = int.MaxValue;
            p.Stats.Sessions = int.MaxValue;
            p.Progress.MaxLevel = 3000;
            p.Progress.SkippedLevels.Clear();
            for (int level = 10; level <= 3000; level += 10)
            {
                p.Progress.SkippedLevels.Add(level);
            }

            var history = new System.Text.StringBuilder();
            for (int i = 0; i < 2000; i++)
            {
                history.Append("12>7");
            }

            p.LevelState!.History = history.ToString();
            p.LevelState.RevealedCuias.Clear();
            for (int i = 0; i < 16; i++)
            {
                p.LevelState.RevealedCuias.Add(i);
            }

            return p;
        }

        /// <summary>Todos los campos, uno a uno, para mensajes de fallo legibles.</summary>
        public static List<string> Describe(Profile p)
        {
            var lines = new List<string>
            {
                "schema=" + p.ProfileSchema,
                "created_at=" + p.CreatedAt.Ticks + "/" + p.CreatedAt.Kind,
                "economy_ts=" + (p.EconomyTs.HasValue ? p.EconomyTs.Value.Ticks + "/" + p.EconomyTs.Value.Kind : "null"),
                "seeds=" + p.Wallet.Seeds,
                "canoes=" + p.Wallet.Canoes.Count + "," + p.Wallet.Canoes.NextRegenAt?.Ticks + "," + p.Wallet.Canoes.InfiniteUntil?.Ticks,
                "stock=" + p.Stock.Undo + "," + p.Stock.Hint + "," + p.Stock.Reveal + "," + p.Stock.ExtraCuia + "," + p.Stock.Shuffle,
                "daily=" + p.Daily.DateLocal + "," + p.Daily.RvUndoRefill + "," + p.Daily.RvHint + "," + p.Daily.RvReveal + ","
                    + p.Daily.RvExtraCanoe + "," + p.Daily.RvDoubleHarvest + "," + p.Daily.RvTotal + "," + p.Daily.InterstitialsToday + ","
                    + p.Daily.RescuesToday + "," + p.Daily.AfRescueShown + "," + p.Daily.AfRescueDeclinedDays + "," + p.Daily.ShufflesToday + ","
                    + p.Daily.ShieldAvailable,
                "progress=" + p.Progress.MaxLevel + "," + p.Progress.NextLevel + "," + p.Progress.SinceLastSkip + ",["
                    + string.Join(",", p.Progress.SkippedLevels) + "]," + p.Progress.ContentVersion,
                "no_ads=" + p.Entitlements.NoAds,
                "stats=" + p.Stats.LevelsWon + "," + p.Stats.FailsHard + "," + p.Stats.Rescues + "," + p.Stats.RvWatched + "," + p.Stats.Sessions,
            };

            LevelStateData? s = p.LevelState;
            lines.Add(s == null
                ? "level_state=null"
                : "level_state=" + s.Level + "," + s.Seed + "," + s.Variant + "," + s.FailsLevel + "," + s.Pours + "," + s.ExtraCuias + ","
                    + s.RescuesLevel + "," + s.UndoFree + ",[" + string.Join(",", s.RevealedCuias) + "]," + s.History + "," + s.Stuck);
            return lines;
        }
    }
}
