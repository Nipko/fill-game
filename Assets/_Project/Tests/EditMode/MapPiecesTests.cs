#nullable enable
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Piezas provisionales del Mapa del río (tarea M1.5; P-M5 de D-023; propuesta del Mapa §12, piezas 7 y 9; propuesta del
    /// personaje c.4-c.7): <see cref="MapArtRule"/> (el provisional solo en el Editor y en desarrollo, forzable en los tests), el
    /// retrato del perezoso con los ojos abiertos y cerrados, la mini cuia y la canoa de Capi. Comprueba que son perezosas (nada
    /// se crea al construir <see cref="ProceduralArt"/>), sus tamaños, que el PNG de la ranura manda (y entonces el provisional no
    /// se crea) y que la canoa del Mapa no es <see cref="ProceduralArt.IconCanoe"/>.
    /// </summary>
    public sealed class MapPiecesTests
    {
        private ProceduralArt _art = null!;
        private Texture2D? _pngTexture;
        private Sprite? _png;

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = null;
            _art = new ProceduralArt("white");
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            _art.Dispose();
            if (_png != null)
            {
                Object.DestroyImmediate(_png);
            }

            if (_pngTexture != null)
            {
                Object.DestroyImmediate(_pngTexture);
            }
        }

        [Test]
        public void MapArtRule_ProvisionalOnlyInTheEditorOrADevelopmentBuild()
        {
            Assert.That(MapArtRule.ShowProvisionalIn(true, false), Is.True, "Editor");
            Assert.That(MapArtRule.ShowProvisionalIn(false, true), Is.True, "build de desarrollo");
            Assert.That(MapArtRule.ShowProvisionalIn(true, true), Is.True);
            Assert.That(MapArtRule.ShowProvisionalIn(false, false), Is.False, "release: nada hasta que exista el PNG");
            Assert.That(MapArtRule.CanShow(false, true), Is.True, "con su PNG final sale también en release");
            Assert.That(MapArtRule.CanShow(true, false), Is.True);
            Assert.That(MapArtRule.CanShow(false, false), Is.False);
        }

        [Test]
        public void MapArtRule_CanBeForcedInTests_AndOtherwiseFollowsTheEditor()
        {
            Assert.That(MapArtRule.ShowProvisional, Is.True, "los tests corren en el Editor");
            MapArtRule.ForceProvisional = false;
            Assert.That(MapArtRule.ShowProvisional, Is.False, "como en release");
            Sprite? png = ArtSlots.Find(ProceduralArt.SlothPortraitSlot);
            Assert.That(_art.SlothPortrait(false), Is.SameAs(png), "como en release: solo el PNG final, o nada");
            Assert.That(_art.SlothPortraitBuilt, Is.False);
            MapArtRule.ForceProvisional = true;
            Assert.That(MapArtRule.ShowProvisional, Is.True);
            Assert.That(_art.SlothPortrait(false), Is.Not.Null, "como en desarrollo: el PNG o el provisional");
        }

        [Test]
        public void Pieces_AreLazy_NothingIsCreatedUntilAsked()
        {
            Assert.That(_art.SlothPortraitBuilt, Is.False, "el constructor no crea el retrato");
            Assert.That(_art.MapIconsBuilt, Is.False, "ni la mini cuia ni la canoa");

            Sprite closed = _art.SlothPortraitProcedural(true);
            Assert.That(_art.SlothPortraitBuilt, Is.True);
            Assert.That(_art.MapIconsBuilt, Is.False, "pedir el retrato no crea las piezas del Mapa");
            Assert.That(_art.SlothPortraitProcedural(true), Is.SameAs(closed), "se crea una vez y se guarda");

            Sprite cuia = _art.MapMiniCuiaFor(null);
            Assert.That(_art.MapIconsBuilt, Is.True);
            Assert.That(_art.MapMiniCuiaFor(null), Is.SameAs(cuia));
            Assert.That(_art.MapCanoeFor(null), Is.SameAs(_art.MapCanoeFor(null)));
            Assert.That(_art.MapMiniCuiaWater, Is.SameAs(_art.MapMiniCuiaWater));

            _art.Dispose();
            Assert.That(_art.SlothPortraitBuilt, Is.False, "tras liberar, nada queda");
            Assert.That(_art.MapIconsBuilt, Is.False);
            Assert.That(_art.SlothPortraitProcedural(false), Is.Not.Null, "y se vuelve a generar al pedirlo");
        }

        [Test]
        public void Sizes_PortraitBetween40And56dp_MiniCuia20x32_Canoe40x32()
        {
            Sprite open = _art.SlothPortraitProcedural(false);
            Sprite closed = _art.SlothPortraitProcedural(true);
            Assert.That(open, Is.Not.SameAs(closed), "ojos abiertos y cerrados");
            AssertSizeDp(open, ProceduralArt.SlothPortraitDp, ProceduralArt.SlothPortraitDp);
            AssertSizeDp(closed, ProceduralArt.SlothPortraitDp, ProceduralArt.SlothPortraitDp);
            Assert.That(ProceduralArt.SlothPortraitMinDp, Is.EqualTo(40f));
            Assert.That(ProceduralArt.SlothPortraitDp, Is.InRange(40f, 56f));

            AssertSizeDp(_art.MapMiniCuiaFor(null), 20f, 32f);
            AssertSizeDp(_art.MapMiniCuiaWater, 20f, 32f);
            AssertSizeDp(_art.MapCanoeFor(null), 40f, 32f);
            Assert.That(ProceduralArt.MapCanoeWidthDp, Is.InRange(32f, 40f), "la canoa a 32-40 dp (propuesta §12, pieza 7)");
        }

        [Test]
        public void TheSlotPng_Rules_AndThenTheProvisionalIsNeverCreated()
        {
            Sprite png = MakePng();

            Assert.That(_art.SlothPortrait(false, png, true), Is.SameAs(png));
            Assert.That(_art.SlothPortrait(true, png, false), Is.SameAs(png), "el PNG vale para los ojos cerrados y también en release");
            Assert.That(_art.MapMiniCuiaFor(png), Is.SameAs(png));
            Assert.That(_art.MapCanoeFor(png), Is.SameAs(png));
            Assert.That(_art.SlothPortraitBuilt, Is.False, "con el PNG, el retrato provisional no se crea");
            Assert.That(_art.MapIconsBuilt, Is.False, "con el PNG, la mini cuia y la canoa provisionales no se crean");

            Assert.That(_art.SlothPortrait(false, null, false), Is.Null, "release sin PNG: nada (P-M5)");
            Assert.That(_art.SlothPortraitBuilt, Is.False);
            Assert.That(_art.SlothPortrait(false, null, true), Is.SameAs(_art.SlothPortraitProcedural(false)), "Editor o desarrollo: el provisional");
        }

        [Test]
        public void TheSlots_AreKnownSlotsOfTheGlossary()
        {
            Assert.That(ProceduralArt.SlothPortraitSlot, Is.EqualTo("fauna_bradypus_variegatus_retrato"));
            Assert.That(ArtSlots.IsKnown(ProceduralArt.SlothPortraitSlot), Is.True);
            Assert.That(ArtSlots.IsKnown(ProceduralArt.MiniCuiaSlot), Is.True);
            Assert.That(ArtSlots.IsKnown(ProceduralArt.MapCanoeSlot), Is.True);
        }

        [Test]
        public void MapCanoe_IsNotTheCanoeIcon()
        {
            Sprite canoe = _art.MapCanoeFor(null);
            Sprite icon = _art.IconCanoe;
            Assert.That(canoe, Is.Not.SameAs(icon));
            Assert.That(canoe.texture, Is.Not.SameAs(icon.texture));
            Assert.That(canoe.name, Is.Not.EqualTo(icon.name));
            Vector2 canoeDp = SizeDp(canoe);
            Vector2 iconDp = SizeDp(icon);
            Assert.That(canoeDp, Is.Not.EqualTo(iconDp), "otra pieza, otro tamaño: la canoa del Mapa lleva a Capi sentado");
            Assert.That(canoeDp.y / canoeDp.x, Is.GreaterThan(iconDp.y / iconDp.x), "más alta que el icono, por Capi");
        }

        private static Vector2 SizeDp(Sprite sprite)
        {
            float pxPerDp = sprite.pixelsPerUnit / 100f;
            return new Vector2(sprite.rect.width / pxPerDp, sprite.rect.height / pxPerDp);
        }

        private static void AssertSizeDp(Sprite sprite, float widthDp, float heightDp)
        {
            Vector2 dp = SizeDp(sprite);
            Assert.That(dp.x, Is.EqualTo(widthDp).Within(0.5f), sprite.name + ": ancho");
            Assert.That(dp.y, Is.EqualTo(heightDp).Within(0.5f), sprite.name + ": alto");
        }

        private Sprite MakePng()
        {
            _pngTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            _png = Sprite.Create(_pngTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            _png.hideFlags = HideFlags.DontSave;
            return _png;
        }
    }
}
