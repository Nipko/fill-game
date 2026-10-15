#nullable enable
using System.Text.RegularExpressions;
using AF.EditorTools;
using AF.Localization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Compuerta de release de los textos (08 §4.7; D-018, Q10-A; propuesta de idiomas §3.10; tarea B1): con
    /// <c>com.amazonfill.dev</c> solo avisa; con otro <c>applicationId</c>, para el build si un idioma ofrecido (una columna de
    /// <c>strings.csv</c>) tiene la UI incompleta o hay texto cultural sin <c>aprobado_es</c> (toda fila de <c>facts.csv</c> y
    /// las de <c>strings.csv</c> en <c>pendiente_asesor</c>).
    /// </summary>
    public sealed class LocReleaseGateTests
    {
        private const string ReleaseId = "com.estudio.tintalu";
        private const string Header = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args\n";

        // Todo completo y aprobado: un build de release pasa.
        private const string CleanStrings = Header
            + "back,Volver,Back,Voltar,Botón Volver.,12,aprobado_es,revisado_nativo,revisado_nativo,,\n"
            + "play,Jugar,Play,Jogar,Botón Jugar.,12,aprobado_es,revisado_nativo,revisado_nativo,,\n";

        private const string CleanFacts = Header
            + "fauna_x_dato,Dato aprobado,,,Dato.,50,aprobado_es,,,,\n";

        [Test]
        public void CleanTables_ReleaseId_Passes()
        {
            LocReleaseReport report = LocReleaseGate.Evaluate(LocCatalog.Parse(CleanStrings, CleanFacts), ReleaseId);

            Assert.That(report.Problems, Is.Empty, string.Join("\n", report.Problems));
            Assert.That(report.Blocks, Is.False);
            LogAssert.Expect(LogType.Log, new Regex(@"\[AF\.LocReleaseGate\] OK"));
            Assert.That(LocReleaseGate.Log(report, "[test] "), Is.True);
        }

        [Test]
        public void IncompleteUi_ReleaseId_Blocks_AndListsTheLocaleAndKeys()
        {
            string strings = Header
                + "back,Volver,Back,,Botón Volver.,12,aprobado_es,revisado_nativo,,,\n"
                + "play,Jugar,,,Botón Jugar.,12,aprobado_es,,,,\n";

            LocReleaseReport report = LocReleaseGate.Evaluate(LocCatalog.Parse(strings, CleanFacts), ReleaseId);

            Assert.That(report.Blocks, Is.True);
            Assert.That(report.Problems, Has.Count.EqualTo(2), string.Join("\n", report.Problems));
            Assert.That(report.Problems[0], Is.EqualTo("UI incompleta en en: claves de strings.csv sin texto: 1 (play)."));
            Assert.That(report.Problems[1], Is.EqualTo("UI incompleta en pt-BR: claves de strings.csv sin texto: 2 (back, play)."));

            LogAssert.Expect(LogType.Error, new Regex(@"ERROR UI incompleta en en"));
            LogAssert.Expect(LogType.Error, new Regex(@"ERROR UI incompleta en pt-BR"));
            LogAssert.Expect(LogType.Error, new Regex(@"build de release \(com\.estudio\.tintalu\) parado"));
            Assert.That(LocReleaseGate.Log(report, "[test] "), Is.False);
        }

        [Test]
        public void CulturalTextWithoutApprovedEs_ReleaseId_Blocks()
        {
            string strings = CleanStrings
                + "world_name,Tapajós,Tapajós,Tapajós,Nombre de mundo.,20,pendiente_asesor,revisado_nativo,revisado_nativo,,\n";
            string facts = Header
                + "fauna_a_dato,Dato pendiente,,,Dato.,50,pendiente_asesor,,,,\n"
                + "fauna_b_dato,Dato en borrador,,,Dato.,50,borrador,,,,\n"
                + "fauna_c_dato,Dato aprobado,,,Dato.,50,aprobado_es,,,,\n";

            LocReleaseReport report = LocReleaseGate.Evaluate(LocCatalog.Parse(strings, facts), ReleaseId);

            Assert.That(report.Blocks, Is.True);
            Assert.That(report.Problems, Is.EqualTo(new[]
            {
                "texto cultural sin aprobado_es: strings.csv:4 world_name (pendiente_asesor).",
                "texto cultural sin aprobado_es: facts.csv:2 fauna_a_dato (pendiente_asesor).",
                "texto cultural sin aprobado_es: facts.csv:3 fauna_b_dato (borrador).",
            }));
        }

        [Test]
        public void ApprovedFacts_UntranslatedOrHalfTranslated_DoNotBlock()
        {
            // Q10-A: un dato sin traducir (o traducido a medias) no se muestra en ese idioma; no para el build. Solo cuenta el
            // aprobado_es del texto fuente. Varios datos aprobados: uno sin traducir, uno con en traducido y pt-BR vacío, uno con
            // en en borrador y pt-BR revisado, y uno traducido del todo.
            string facts = Header
                + "fauna_a_dato,Dato sin traducir,,,Dato.,50,aprobado_es,,,,\n"
                + "fauna_b_dato,Dato a medias,Half fact,,Dato.,50,aprobado_es,traducido,,,\n"
                + "fauna_c_dato,Dato con borrador,Draft fact,Dado revisado,Dato.,50,aprobado_es,borrador,revisado_nativo,,\n"
                + "fauna_d_dato,Dato completo,Full fact,Dado completo,Dato.,50,aprobado_es,revisado_nativo,revisado_nativo,,\n";

            LocCatalog catalog = LocCatalog.Parse(CleanStrings, facts);
            LocReleaseReport report = LocReleaseGate.Evaluate(catalog, ReleaseId);

            // Las tablas tienen de verdad los estados mezclados que se prueban.
            LocTable table = catalog.Facts;
            Assert.That(table.Count, Is.EqualTo(4));
            Assert.That(table.StatusAt(0, "en"), Is.EqualTo(LocStatus.Missing));
            Assert.That(table.StatusAt(0, "pt-BR"), Is.EqualTo(LocStatus.Missing));
            Assert.That(table.StatusAt(1, "en"), Is.EqualTo(LocStatus.Translated));
            Assert.That(table.StatusAt(1, "pt-BR"), Is.EqualTo(LocStatus.Missing));
            Assert.That(table.StatusAt(2, "en"), Is.EqualTo(LocStatus.Draft));
            Assert.That(table.StatusAt(2, "pt-BR"), Is.EqualTo(LocStatus.NativeReviewed));
            Assert.That(table.StatusAt(3, "en"), Is.EqualTo(LocStatus.NativeReviewed));

            Assert.That(report.Problems, Is.Empty, string.Join("\n", report.Problems));
            Assert.That(report.Blocks, Is.False);
        }

        [Test]
        public void DevelopmentId_OnlyWarns()
        {
            string strings = Header + "play,Jugar,,,Botón Jugar.,12,pendiente_asesor,,,,\n";

            LocReleaseReport report = LocReleaseGate.Evaluate(LocCatalog.Parse(strings, CleanFacts), ProjectSetup.ApplicationId);

            Assert.That(report.IsDevelopmentId, Is.True);
            Assert.That(report.Problems, Has.Count.EqualTo(3), string.Join("\n", report.Problems));
            Assert.That(report.Blocks, Is.False);
            LogAssert.Expect(LogType.Warning, new Regex(@"aviso \(com\.amazonfill\.dev, build de prueba: no bloquea\)\. Problemas de textos que pararían un build de release: 3\."));
            Assert.That(LocReleaseGate.Log(report, "[test] "), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ProjectTables_GateIsCoherent()
        {
            // Con las tablas reales solo se comprueba la coherencia (hoy en y pt-BR están incompletos, pero eso cambiará con las
            // traducciones): en desarrollo nunca bloquea y en release bloquea si y solo si hay problemas. Las ramas de error las
            // cubren las tablas sintéticas de los otros tests.
            LocCatalog catalog = LocReleaseGate.ReadProjectCatalog();

            LocReleaseReport dev = LocReleaseGate.Evaluate(catalog, ProjectSetup.ApplicationId);
            LocReleaseReport release = LocReleaseGate.Evaluate(catalog, ReleaseId);

            Assert.That(dev.IsDevelopmentId, Is.True);
            Assert.That(dev.Blocks, Is.False);
            Assert.That(release.IsDevelopmentId, Is.False);
            Assert.That(release.Blocks, Is.EqualTo(release.Problems.Count > 0));
            Assert.That(release.Problems, Is.EqualTo(dev.Problems), "los problemas no dependen del applicationId");

            Assert.That(LocReleaseGate.Check(ProjectSetup.ApplicationId, "[test] "), Is.True);
            LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.That(LocReleaseGate.Check(ReleaseId, "[test] "), Is.EqualTo(!release.Blocks));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void UnreadableTables_ReleaseId_BlocksWithAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^\[test\] \[AF\.LocReleaseGate\] no se pueden leer las tablas de textos: facts\.csv, línea 5: comillas sin cerrar$"));

            bool ok = LocReleaseGate.Check(ReleaseId, "[test] ", () => throw new LocCsvException("facts.csv", 5, "comillas sin cerrar"));

            Assert.That(ok, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnreadableTables_DevelopmentId_OnlyWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[test\] \[AF\.LocReleaseGate\] no se pueden leer las tablas de textos: facts\.csv, línea 5: comillas sin cerrar$"));

            bool ok = LocReleaseGate.Check(ProjectSetup.ApplicationId, "[test] ", () => throw new LocCsvException("facts.csv", 5, "comillas sin cerrar"));

            Assert.That(ok, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
