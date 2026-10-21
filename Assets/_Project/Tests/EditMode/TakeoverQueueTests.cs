#nullable enable
using System;
using System.IO;
using System.Reflection;
using AF.GameFlow;
using AF.Platform;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Cola de takeovers (B4; 06 §2.3, regla final, y §4.7; D-033, añadido del 2026-10-20, puntos 5 y 11; Q-M4 (a)): uno por
    /// sesión fuera de los hitos (<c>ui_max_takeovers_per_session</c> de 08 §9), en el orden de 06 §4.7, sin repetir lo visto en la
    /// sesión, y la sesión nueva tras ≥ 30 min en segundo plano.
    /// </summary>
    public sealed class TakeoverQueueTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Config_IsTheRowOf08_9()
        {
            string chapter = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs", "gdd", "08-arquitectura-tecnica-publicacion.md"));
            StringAssert.Contains("| `ui_max_takeovers_per_session` | int | 1 | 1 | 05 |", chapter);
            PropertyInfo property = typeof(TakeoverConfig).GetProperty(nameof(TakeoverConfig.UiMaxTakeoversPerSession))!;
            Assert.That(property.GetCustomAttribute<RemoteConfigKeyAttribute>()!.Key, Is.EqualTo("ui_max_takeovers_per_session"));
            Assert.That(TakeoverConfig.Defaults.UiMaxTakeoversPerSession, Is.EqualTo(1));
            Assert.That(TakeoverConfig.Defaults, Is.Not.SameAs(TakeoverConfig.Defaults));
            var bad = new TakeoverConfig { UiMaxTakeoversPerSession = 2 };
            Assert.Throws<InvalidOperationException>(() => bad.Validate(), "rango de 08 §9: 1");
        }

        [Test]
        public void OnePerSession_InThePriorityOrderOf06_4_7()
        {
            var queue = new TakeoverQueue();
            bool logbook = true;
            bool offer = true;
            queue.Register(TakeoverKind.Offer, () => offer);
            queue.Register(TakeoverKind.Logbook, () => logbook);
            Assert.That(queue.Session, Is.EqualTo(1), "el arranque en frío es una sesión");
            Assert.That(queue.Peek(), Is.EqualTo(TakeoverKind.Logbook), "Bitácora pendiente → evento → oferta → liga");
            Assert.That(queue.Peek(), Is.EqualTo(TakeoverKind.Logbook), "Peek no cambia nada");
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.Logbook));
            Assert.That(queue.ShownThisSession, Is.EqualTo(1));
            Assert.That(queue.QuotaLeft, Is.False);
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.None), "un solo pop-up fuera de hitos por sesión");

            queue.BeginSession();
            logbook = false;
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.Offer), "sin Bitácora pendiente, la siguiente");
            offer = false;
            queue.BeginSession();
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.None));
            Assert.That(queue.ShownThisSession, Is.EqualTo(0));
        }

        [Test]
        public void WhatWasSeenInsideAMilestoneOrByHand_DoesNotSpendTheQuota_ButDoesNotComeBackAlone()
        {
            var queue = new TakeoverQueue();
            queue.Register(TakeoverKind.Logbook, () => true);
            queue.MarkSeen(TakeoverKind.Logbook); // secuencia del 10 (06 §2.3) o tarjeta del inicio
            Assert.That(queue.QuotaLeft, Is.True);
            Assert.That(queue.WasSeen(TakeoverKind.Logbook), Is.True);
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.None), "ya vista en la sesión (Q-B9 [P])");
            queue.Register(TakeoverKind.Event, () => true);
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.Event), "el cupo sigue entero para otro");
            queue.MarkSeen(TakeoverKind.None);
            queue.MarkShown(TakeoverKind.None);
            Assert.That(queue.ShownThisSession, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.Register(TakeoverKind.None, () => true));
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.MarkSeen((TakeoverKind)9));
        }

        [Test]
        public void ANewSession_StartsAfterThirtyMinutesInTheBackground()
        {
            var queue = new TakeoverQueue();
            queue.Register(TakeoverKind.Logbook, () => true);
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.Logbook));

            queue.OnBackground(T0);
            Assert.That(queue.OnForeground(T0.AddMinutes(29)), Is.False, "menos de 30 min: la misma sesión");
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.None));

            queue.OnBackground(T0.AddMinutes(30));
            queue.OnBackground(T0.AddMinutes(50)); // una segunda llamada no mueve la hora
            Assert.That(queue.OnForeground(T0.AddMinutes(60)), Is.True, "≥ 30 min (Q-M4 (a))");
            Assert.That(queue.Session, Is.EqualTo(2));
            Assert.That(queue.TakeAtLobby(), Is.EqualTo(TakeoverKind.Logbook));

            queue.OnBackground(T0.AddHours(3));
            Assert.That(queue.OnForeground(T0), Is.False, "reloj hacia atrás: la misma sesión");
            Assert.That(queue.OnForeground(T0.AddHours(9)), Is.False, "sin segundo plano antes, nada");
            Assert.That(TakeoverQueue.SessionGapMinutes, Is.EqualTo(30));
        }

        [Test]
        public void TheRevealUnlockOfTheLogbook_IsTheMurkyLevelOfTheClient()
        {
            // lvl_murky_from_level (08 §9, default 100) aún no tiene clave en el cliente: las dos copias, iguales (Q-B5 de B4).
            StringAssert.Contains("| `lvl_murky_from_level` / `lvl_murky_max_cuias` / `lvl_murky_max_hidden_units` | int | 100 / 3 / 4 |", File.ReadAllText(Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs", "gdd", "08-arquitectura-tecnica-publicacion.md")));
            Assert.That(AF.UI.GameBootstrap.LogbookRevealUnlockLevel, Is.EqualTo(AF.UI.DevBench.MurkyFromLevel).And.EqualTo(100));
        }

        [Test]
        public void Ids_AreTheOnesOfTheProbe()
        {
            Assert.That(TakeoverQueue.IdOf(TakeoverKind.None), Is.EqualTo("none"));
            Assert.That(TakeoverQueue.IdOf(TakeoverKind.Logbook), Is.EqualTo("logbook"));
            Assert.That(TakeoverQueue.IdOf(TakeoverKind.Event), Is.EqualTo("event"));
            Assert.That(TakeoverQueue.IdOf(TakeoverKind.Offer), Is.EqualTo("offer"));
            Assert.That(TakeoverQueue.IdOf(TakeoverKind.League), Is.EqualTo("league"));
        }
    }
}
