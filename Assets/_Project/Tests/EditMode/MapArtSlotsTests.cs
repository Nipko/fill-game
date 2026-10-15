#nullable enable
using System.IO;
using AF.Levels;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Ranuras de arte del Mapa del río en <see cref="ArtSlots.IsKnown"/> (D-013; glosario §11; tarea M1.2): los kits de los 13
    /// mundos, los héroes, las <c>map_*</c> y los ambientes, con id en <c>[a-z0-9_]</c>; ninguna ranura con «belen» (D-013, punto
    /// 11); y todos los fondos del archivo real de paradas son ranuras conocidas.
    /// </summary>
    public sealed class MapArtSlotsTests
    {
        [TestCase("bg_kit_w01")]
        [TestCase("bg_kit_w09")]
        [TestCase("bg_kit_w10")]
        [TestCase("bg_kit_w13")]
        [TestCase("bg_hero_ver_o_peso")]
        [TestCase("bg_hero_alter_do_chao")]
        [TestCase("bg_hero_iquitos2")]
        [TestCase("map_tile_rock")]
        [TestCase("map_canoe")]
        [TestCase("map_flag_co")]
        [TestCase("map_flag_br")]
        [TestCase("map_minicuia")]
        [TestCase("map_rack")]
        [TestCase("map_compass")]
        [TestCase("map_plaque")]
        [TestCase("map_buoy")]
        [TestCase("map_beacon")]
        [TestCase("map_mirador")]
        [TestCase("map_home")]
        [TestCase("map_home_window")]
        [TestCase("amb_mist")]
        [TestCase("amb_lluvia_2")]
        public void IsKnown_AcceptsTheMapSlotsOfTheGlossary(string slot)
        {
            Assert.That(ArtSlots.IsKnown(slot), Is.True, slot);
        }

        [TestCase("bg_kit_w1")]
        [TestCase("bg_kit_w00")]
        [TestCase("bg_kit_w14")]
        [TestCase("bg_kit_w20")]
        [TestCase("bg_kit_w013")]
        [TestCase("bg_hero_")]
        [TestCase("bg_hero_Iquitos")]
        [TestCase("bg_hero_alter-do-chao")]
        [TestCase("bg_hero_belen")]
        [TestCase("bg_hero_ver_o_peso_belen")]
        [TestCase("amb_belen")]
        [TestCase("map_tile_belen")]
        [TestCase("map_tile_")]
        [TestCase("map_flag_col")]
        [TestCase("map_flag_c")]
        [TestCase("map_flag_CO")]
        [TestCase("map_flag_c1")]
        [TestCase("map_canoa")]
        [TestCase("map_")]
        [TestCase("amb_")]
        [TestCase("amb_niebla densa")]
        [TestCase("bg_kit")]
        public void IsKnown_RejectsMalformedMapSlots(string slot)
        {
            Assert.That(ArtSlots.IsKnown(slot), Is.False, slot);
        }

        [Test]
        public void IsKnown_KeepsTheExistingSlots()
        {
            Assert.That(ArtSlots.IsKnown(ArtSlots.Panorama), Is.True);
            Assert.That(ArtSlots.IsKnown("fruta_acai"), Is.True);
            Assert.That(ArtSlots.IsKnown("fauna_saimiri_collinsi_partes"), Is.True);
            Assert.That(ArtSlots.IsKnown("rama_estuario"), Is.True);
            Assert.That(ArtSlots.IsKnown("cualquier_cosa"), Is.False);
        }

        [Test]
        public void RealStops_EveryKitAndHeroIsAKnownSlot()
        {
            string path = Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json");
            RiverStopCatalog catalog = RiverStopCatalogReader.Read(File.ReadAllText(path));
            for (int n = 0; n < catalog.Count; n++)
            {
                RiverStopBackdrop bg = catalog.At(n)!.Backdrop;
                Assert.That(ArtSlots.IsKnown(bg.Kit), Is.True, bg.Kit);
                if (bg.Hero != null)
                {
                    Assert.That(ArtSlots.IsKnown(bg.Hero), Is.True, bg.Hero);
                }

                string flag = catalog.At(n)!.FlagRaise ?? "BR";
                Assert.That(ArtSlots.IsKnown(ArtSlots.MapFlagPrefix + flag.ToLowerInvariant()), Is.True, flag);
            }
        }
    }
}
