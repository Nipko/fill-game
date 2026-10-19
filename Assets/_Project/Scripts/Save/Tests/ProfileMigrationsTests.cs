#nullable enable
using System;
using System.Collections.Generic;
using AF.Save.Json;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Migraciones <c>profile_schema n → n+1</c> encadenadas (GDD 08 §4.4: el esquema solo avanza). Se prueban con un
    /// juego sintético 1 → 2 → 3 sobre el documento JSON; el paso real 1 → 2 de la build (DC15), en <see cref="FaunaSightingTests"/>.
    /// </summary>
    public sealed class ProfileMigrationsTests
    {
        [Test]
        public void Steps_RunInOrder_AndUpdateTheSchemaAfterEach()
        {
            var log = new List<string>();
            var migrations = new ProfileMigrations(
                3,
                ProfileMigrations.Step(2, root =>
                {
                    log.Add("2->3 vio schema " + root.Get(ProfileJson.SchemaKey)!.AsInt());
                    root.Set("b", JsonNode.FromInt(root.Get("a")!.AsInt() * 10));
                }),
                ProfileMigrations.Step(1, root =>
                {
                    log.Add("1->2 vio schema " + root.Get(ProfileJson.SchemaKey)!.AsInt());
                    root.Set("a", JsonNode.FromInt(4));
                }));
            JsonNode doc = JsonText.Parse("{\"profile_schema\": 1}");

            migrations.Migrate(doc, 1);

            Assert.That(log, Is.EqualTo(new[] { "1->2 vio schema 1", "2->3 vio schema 2" }));
            Assert.That(doc.Get(ProfileJson.SchemaKey)!.AsInt(), Is.EqualTo(3));
            Assert.That(doc.Get("b")!.AsInt(), Is.EqualTo(40));
        }

        [Test]
        public void StartingMidChain_RunsOnlyTheRemainingSteps()
        {
            int calls = 0;
            var migrations = new ProfileMigrations(3, ProfileMigrations.Step(1, _ => calls += 100), ProfileMigrations.Step(2, _ => calls++));
            JsonNode doc = JsonText.Parse("{\"profile_schema\": 2}");

            migrations.Migrate(doc, 2);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(doc.Get(ProfileJson.SchemaKey)!.AsInt(), Is.EqualTo(3));
        }

        [Test]
        public void CurrentSchema_IsANoOp()
        {
            JsonNode doc = JsonText.Parse("{\"profile_schema\": " + Profile.CurrentSchema + "}");

            ProfileMigrations.Current.Migrate(doc, Profile.CurrentSchema);

            Assert.That(doc.Get(ProfileJson.SchemaKey)!.AsInt(), Is.EqualTo(Profile.CurrentSchema));
        }

        [Test]
        public void MissingStep_OrNewerSchema_Throw()
        {
            var migrations = new ProfileMigrations(3, ProfileMigrations.Step(2, _ => { }));

            Assert.Throws<InvalidOperationException>(() => migrations.Migrate(JsonText.Parse("{\"profile_schema\": 1}"), 1));
            Assert.Throws<InvalidOperationException>(() => migrations.Migrate(JsonText.Parse("{\"profile_schema\": 4}"), 4));
        }

        [Test]
        public void InvalidOrRepeatedSteps_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ProfileMigrations(2, ProfileMigrations.Step(2, _ => { })));
            Assert.Throws<ArgumentException>(() => new ProfileMigrations(2, ProfileMigrations.Step(-1, _ => { })));
            Assert.Throws<ArgumentException>(() => new ProfileMigrations(3, ProfileMigrations.Step(1, _ => { }), ProfileMigrations.Step(1, _ => { })));
        }
    }
}
