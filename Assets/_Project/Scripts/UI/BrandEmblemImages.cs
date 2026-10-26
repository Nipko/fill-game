#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Las imágenes de la escena de la marca (<see cref="BrandEmblemArt"/>) en un lienzo, en su orden de pintado: el halo, la base
    /// (selva y río blanco), los tramos del açaí, la isla, los aros del impacto, el chorro y la cuia (añadido de D-051). Las crean
    /// igual la intro y la pantalla de arranque y las coloca <see cref="Place"/>, la misma para las dos: por eso empalman.
    /// </summary>
    internal sealed class BrandEmblemImages
    {
        // El halo del cielo: centrado sobre el horizonte, de lado una vez y un tercio el ancho [P].
        private const float HaloWidthShare = 1.35f;

        private BrandEmblemImages(Transform parent, Sprite halo)
        {
            Halo = UiBuilder.Image("Halo", parent, halo, BrandIntro.HaloColor);
            Base = UiBuilder.Image("EmblemBase", parent, null, Color.white);
            Acai = new Image[BrandRiverScene.ChunkCount];
            for (int k = 0; k < Acai.Length; k++)
            {
                Acai[k] = UiBuilder.Image("EmblemAcai" + k, parent, null, Color.white);
            }

            Island = UiBuilder.Image("EmblemIsland", parent, null, Color.white);
            Impact = UiBuilder.Image("EmblemImpact", parent, null, Color.white);
            Stream = UiBuilder.Image("EmblemStream", parent, null, Color.white);
            Cuia = UiBuilder.Image("EmblemCuia", parent, null, Color.white);
        }

        /// <summary>El halo del cielo.</summary>
        public Image Halo { get; }

        /// <summary>La selva y el río blanco.</summary>
        public Image Base { get; }

        /// <summary>Los tramos del açaí, del impacto al primer plano.</summary>
        public Image[] Acai { get; }

        /// <summary>La isla.</summary>
        public Image Island { get; }

        /// <summary>Los aros del impacto.</summary>
        public Image Impact { get; }

        /// <summary>El chorro.</summary>
        public Image Stream { get; }

        /// <summary>La cuia.</summary>
        public Image Cuia { get; }

        /// <summary>Crea las imágenes, sin piezas aún, dentro de <paramref name="parent"/>.</summary>
        public static BrandEmblemImages Create(Transform parent, Sprite halo)
        {
            return new BrandEmblemImages(parent, halo);
        }

        /// <summary>Las coloca con las piezas de <paramref name="art"/>: los aros, con el pivote en el impacto (crecen desde él).</summary>
        public void Place(BrandEmblemArt art)
        {
            float side = art.Width * HaloWidthShare;
            UiBuilder.Place(Halo.rectTransform, (art.Width - side) * 0.5f, art.Scene.HorizonY - (side * 0.5f), side, side);
            art.Place(Base, BrandEmblemArt.Base);
            for (int k = 0; k < Acai.Length; k++)
            {
                art.Place(Acai[k], BrandEmblemArt.AcaiFirst + k);
            }

            art.Place(Island, BrandEmblemArt.Island);
            art.Place(Impact, BrandEmblemArt.Impact, art.Scene.Impact);
            art.Place(Stream, BrandEmblemArt.Stream);
            art.Place(Cuia, BrandEmblemArt.Cuia);
        }
    }
}
