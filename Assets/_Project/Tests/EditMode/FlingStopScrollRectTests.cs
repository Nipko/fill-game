#nullable enable
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La lista desplazable de las pantallas (<see cref="FlingStopScrollRect"/>; observación 3 de la jornada 4 en el teléfono,
    /// 2026-10-27): un toque con la lista en movimiento por inercia la para y no activa la fila, como en Android; con la lista quieta
    /// (o casi), el toque vale.
    /// </summary>
    public sealed class FlingStopScrollRectTests
    {
        private GameObject? _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private FlingStopScrollRect NewList()
        {
            _go = new GameObject("List", typeof(RectTransform));
            return _go.AddComponent<FlingStopScrollRect>();
        }

        private static PointerEventData Tap(PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            return new PointerEventData(null) { button = button, eligibleForClick = true };
        }

        [Test]
        public void ATapWhileFlinging_StopsTheList_AndIsNotAClick()
        {
            FlingStopScrollRect list = NewList();
            list.velocity = new Vector2(0f, 900f);
            Assert.That(list.Flinging, Is.True);
            PointerEventData tap = Tap();
            list.OnInitializePotentialDrag(tap);
            Assert.That(tap.eligibleForClick, Is.False, "el toque no llega a la fila");
            Assert.That(list.velocity, Is.EqualTo(Vector2.zero), "la lista se para");
            Assert.That(list.LastTapStoppedFling, Is.True);
        }

        [Test]
        public void ATapOnAStillList_IsAClick()
        {
            FlingStopScrollRect list = NewList();
            PointerEventData tap = Tap();
            list.OnInitializePotentialDrag(tap);
            Assert.That(tap.eligibleForClick, Is.True);
            Assert.That(list.LastTapStoppedFling, Is.False);

            list.velocity = new Vector2(0f, FlingStopScrollRect.StopTapSpeedDp * 0.5f); // lo que queda de inercia: ya parece quieta
            tap = Tap();
            list.OnInitializePotentialDrag(tap);
            Assert.That(tap.eligibleForClick, Is.True, "por debajo del umbral, el toque vale");
        }

        [Test]
        public void TheThresholdIsInDpPerSecond_InBothAxes()
        {
            FlingStopScrollRect list = NewList();
            list.velocity = new Vector2(FlingStopScrollRect.StopTapSpeedDp + 1f, 0f);
            Assert.That(list.Flinging, Is.True, "también en horizontal");
            list.velocity = new Vector2(FlingStopScrollRect.StopTapSpeedDp - 1f, 0f);
            Assert.That(list.Flinging, Is.False);
        }
    }
}
