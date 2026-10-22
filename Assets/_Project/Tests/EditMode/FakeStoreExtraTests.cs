#nullable enable
using AF.Backend;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El extra de desarrollo <c>af_fake_store</c> (<see cref="LaunchOptions.IntentExtraFakeStore"/>; prueba en el teléfono del
    /// 2026-10-23: sin la app en Play Console, la tienda real no está disponible y el Mercado se oculta): en desarrollo, con el
    /// extra, la tienda es la simulada con el backend falso de verificación; en release no hace nada, así que nunca se acredita sin
    /// verificación (el backend del build sigue siendo el suyo: el real o ninguno).
    /// </summary>
    public sealed class FakeStoreExtraTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [Test]
        public void Release_IgnoresTheExtra_AndKeepsTheBuildBackend()
        {
            LaunchOptions.FakeStoreOverride = true;
            bool fakeStore = LaunchOptions.ResolveFakeStore(false);
            Assert.That(fakeStore, Is.False, "en release, af_fake_store no cuenta");
            Assert.That(GameBootstrap.StoreBackendMode(BackendMode.Http, fakeStore), Is.EqualTo(BackendMode.Http), "el backend real verifica");
            Assert.That(GameBootstrap.StoreBackendMode(BackendMode.Unavailable, fakeStore), Is.EqualTo(BackendMode.Unavailable),
                "sin backend, la tienda no vende");
        }

        [Test]
        public void Development_WithTheExtra_UsesTheFakeVerification()
        {
            LaunchOptions.FakeStoreOverride = true;
            bool fakeStore = LaunchOptions.ResolveFakeStore(true);
            Assert.That(fakeStore, Is.True);
            Assert.That(GameBootstrap.StoreBackendMode(BackendMode.Http, fakeStore), Is.EqualTo(BackendMode.Fake));
        }

        [Test]
        public void Development_WithoutTheExtra_KeepsTheBuildBackend()
        {
            LaunchOptions.FakeStoreOverride = false;
            Assert.That(LaunchOptions.ResolveFakeStore(true), Is.False);
            LaunchOptions.FakeStoreOverride = null;
            Assert.That(LaunchOptions.ResolveFakeStore(true), Is.False, "fuera de Android no hay intent: sin el extra");
            Assert.That(GameBootstrap.StoreBackendMode(BackendMode.Http, false), Is.EqualTo(BackendMode.Http));
        }

        [Test]
        public void Reset_ClearsTheOverride()
        {
            LaunchOptions.FakeStoreOverride = true;
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.FakeStoreOverride, Is.Null);
        }
    }
}
