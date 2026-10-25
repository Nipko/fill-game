#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Prueba del 2026-10-25, jornada 2: tras la edad y el consentimiento, el nivel 1 (06 §2.1, regla 3, y §2.2) también cuando el perfil
    /// ya existía porque la primera apertura lo guardó y la app se cerró en la pantalla de edad (<see cref="GameBootstrap.StartsAsFirstRun"/>).
    /// </summary>
    public sealed class FirstRunAfterConsentTests
    {
        private static Profile Fresh()
        {
            return Wallet.NewProfile(new ManualClock(new DateTime(2026, 10, 25, 15, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void ANewProfile_IsTheFirstRun()
        {
            Assert.That(GameBootstrap.StartsAsFirstRun(true, false, false, Fresh()), Is.True);
            Assert.That(GameBootstrap.StartsAsFirstRun(true, false, true, Fresh()), Is.True);
        }

        [Test]
        public void ASavedProfileNeverStarted_AskedForTheConsent_IsTheFirstRun()
        {
            Assert.That(GameBootstrap.StartsAsFirstRun(false, false, true, Fresh()), Is.True, "cerrada en la edad y vuelta a abrir");
        }

        [Test]
        public void ASavedProfile_WithoutTheConsentScreen_GoesToTheLobby()
        {
            Assert.That(GameBootstrap.StartsAsFirstRun(false, false, false, Fresh()), Is.False, "ya pasó la pantalla previa al nivel 1");
        }

        [Test]
        public void AProfileWithProgressOrALevel_AskedForTheConsent_GoesToTheLobby()
        {
            Profile progressed = Fresh();
            progressed.Progress.NextLevel = 4;
            progressed.Progress.MaxLevel = 3;
            Assert.That(GameBootstrap.StartsAsFirstRun(false, false, true, progressed), Is.False);

            Profile withLevel = Fresh();
            withLevel.LevelState = new LevelStateData();
            Assert.That(GameBootstrap.StartsAsFirstRun(false, false, true, withLevel), Is.False);
        }

        [Test]
        public void AfterARebuild_Never()
        {
            Assert.That(GameBootstrap.StartsAsFirstRun(true, true, true, Fresh()), Is.False);
        }
    }
}
