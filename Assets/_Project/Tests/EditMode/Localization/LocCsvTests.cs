#nullable enable
using System.Collections.Generic;
using System.Text;
using AF.Localization;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Lector de las tablas de textos (L2a; contrato 32; 08 §4.7): RFC 4180 (comillas, comas y saltos dentro de un campo),
    /// UTF-8 sin BOM (con BOM, error con su línea) y columnas por nombre, con las dos formas de estado de la pregunta 49
    /// (<c>status</c> solo, o <c>status</c> + <c>status_&lt;idioma&gt;</c>) y la columna <c>args</c> [P].
    /// </summary>
    public sealed class LocCsvTests
    {
        [Test]
        public void Parse_QuotedFields_KeepCommasQuotesAndLineBreaks()
        {
            IReadOnlyList<LocCsvRow> rows = LocCsv.Parse(
                "a,\"b, c\",\"Dijo \"\"hola\"\"\"\n\"línea 1\nlínea 2\",x,\"\"\nsiguiente,y,z\n", "t.csv");
            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[0].Fields, Is.EqualTo(new[] { "a", "b, c", "Dijo \"hola\"" }));
            Assert.That(rows[1].Fields, Is.EqualTo(new[] { "línea 1\nlínea 2", "x", "" }));
            Assert.That(rows[0].Line, Is.EqualTo(1));
            Assert.That(rows[1].Line, Is.EqualTo(2), "el registro empieza en la línea 2");
            Assert.That(rows[2].Line, Is.EqualTo(4), "el salto de dentro del campo cuenta como línea");
        }

        [Test]
        public void Parse_AcceptsCrLf_AndNormalizesBreaksInsideQuotes()
        {
            IReadOnlyList<LocCsvRow> rows = LocCsv.Parse("k,v\r\nuno,\"a\r\nb\"\r\n", "t.csv");
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[0].Fields, Is.EqualTo(new[] { "k", "v" }));
            Assert.That(rows[1].Fields, Is.EqualTo(new[] { "uno", "a\nb" }), "sin CR: el salto de dentro queda como \\n");
        }

        [Test]
        public void Parse_EmptyFields_TrailingComma_BlankLines_AndNoFinalNewline()
        {
            IReadOnlyList<LocCsvRow> rows = LocCsv.Parse("a,,c,\n\n\nd,e,f,g", "t.csv");
            Assert.That(rows.Count, Is.EqualTo(2), "las líneas vacías no son registros");
            Assert.That(rows[0].Fields, Is.EqualTo(new[] { "a", "", "c", "" }));
            Assert.That(rows[1].Fields, Is.EqualTo(new[] { "d", "e", "f", "g" }));
            Assert.That(rows[1].Line, Is.EqualTo(4));
            Assert.That(LocCsv.Parse(string.Empty, "t.csv").Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_Bom_IsAnErrorWithItsLine()
        {
            var atStart = Assert.Throws<LocCsvException>(() => LocCsv.Parse("﻿key,es-419\nnext,Siguiente\n", "strings.csv"));
            Assert.That(atStart!.Line, Is.EqualTo(1));
            Assert.That(atStart.File, Is.EqualTo("strings.csv"));
            Assert.That(atStart.Message, Does.Contain("BOM").And.Contain("strings.csv, línea 1"));

            var inside = Assert.Throws<LocCsvException>(() => LocCsv.Parse("key,es-419\nnext,Siguiente\n﻿play,Jugar\n", "strings.csv"));
            Assert.That(inside!.Line, Is.EqualTo(3), "un BOM pegado a mitad del archivo (dos archivos unidos) da su línea");
        }

        [Test]
        public void Decode_RejectsTheUtf8Bom_AndInvalidBytes_WithTheirLine()
        {
            byte[] text = new UTF8Encoding(false).GetBytes("key,es-419\nfruta,Açaí\n");
            Assert.That(LocCsv.Decode(text, "t.csv"), Is.EqualTo("key,es-419\nfruta,Açaí\n"));

            byte[] withBom = new UTF8Encoding(true).GetPreamble();
            var bomBytes = new byte[withBom.Length + text.Length];
            withBom.CopyTo(bomBytes, 0);
            text.CopyTo(bomBytes, withBom.Length);
            var bom = Assert.Throws<LocCsvException>(() => LocCsv.Decode(bomBytes, "facts.csv"));
            Assert.That(bom!.Line, Is.EqualTo(1));
            Assert.That(bom.Message, Does.Contain("BOM"));

            byte[] invalid = { (byte)'k', (byte)'\n', (byte)'a', 0xC3, (byte)'\n' };
            var bad = Assert.Throws<LocCsvException>(() => LocCsv.Decode(invalid, "facts.csv"));
            Assert.That(bad!.Line, Is.EqualTo(2));
        }

        [Test]
        public void Parse_MalformedQuotes_GiveTheLine()
        {
            Assert.That(Assert.Throws<LocCsvException>(() => LocCsv.Parse("a,b\nc,\"sin cerrar\n", "t.csv"))!.Line, Is.EqualTo(2));
            Assert.That(Assert.Throws<LocCsvException>(() => LocCsv.Parse("a,b\nc,d\"e\n", "t.csv"))!.Line, Is.EqualTo(2));
            Assert.That(Assert.Throws<LocCsvException>(() => LocCsv.Parse("a,\"b\"c\n", "t.csv"))!.Line, Is.EqualTo(1));
            Assert.That(Assert.Throws<LocCsvException>(() => LocCsv.Parse("a,b\rc,d\n", "t.csv"))!.Line, Is.EqualTo(1), "CR suelto");
        }

        [Test]
        public void Table_PerLocaleStatusHeader_ReadsEveryColumnByName()
        {
            LocTable table = LocTable.Parse(LocSamples.Strings, "strings.csv");
            Assert.That(table.PerLocaleStatus, Is.True);
            Assert.That(table.HasArgs, Is.True);
            Assert.That(table.Locales, Is.EqualTo(new[] { "es-419", "en", "pt-BR" }));
            Assert.That(table.Count, Is.EqualTo(5));
            Assert.That(table.UnsortedAt, Is.EqualTo(-1), "filas en orden alfabético");

            int row = table.IndexOf("stuck_rescue_missing");
            Assert.That(table.KeyAt(row), Is.EqualTo("stuck_rescue_missing"));
            Assert.That(table.TextAt(row, "es-419"), Is.EqualTo("{n:plural:Te falta {} semilla|Te faltan {} semillas}"));
            Assert.That(table.TextAt(row, "pt-BR"), Is.EqualTo(string.Empty));
            Assert.That(table.TextAt(row, "fr"), Is.EqualTo(string.Empty), "sin columna: vacío");
            Assert.That(table.ContextAt(row), Is.EqualTo("Modal Aguas estancadas; n = semillas que faltan"));
            Assert.That(table.MaxCharsAt(row), Is.EqualTo(32));
            Assert.That(table.StatusAt(row, "es-419"), Is.EqualTo(LocStatus.Draft));
            Assert.That(table.StatusAt(row, "en"), Is.EqualTo(LocStatus.Translated));
            Assert.That(table.StatusAt(row, "pt-BR"), Is.EqualTo(LocStatus.Missing), "sin texto: falta");
            Assert.That(table.LineAt(row), Is.EqualTo(5));
            Assert.That(table.IndexOf("no_existe"), Is.EqualTo(-1));
            Assert.That(table.IsComplete("es-419"), Is.True);
            Assert.That(table.IsComplete("en"), Is.True);
            Assert.That(table.IsComplete("pt-BR"), Is.False);
            Assert.That(table.IsComplete("fr"), Is.False);
        }

        [Test]
        public void Table_SingleStatusHeader_StatusCoversTheWholeRow()
        {
            const string csv =
                "key,es-419,en,pt-BR,context,max_chars,status,notes\n" +
                "a_draft,Uno,One,,c,,borrador,\n" +
                "b_reviewed,Dos,Two,,c,,revisado_nativo,\n" +
                "c_approved,Tres,Three,Três,c,10,aprobado_es,nota\n";
            LocTable table = LocTable.Parse(csv, "strings.csv");
            Assert.That(table.PerLocaleStatus, Is.False);
            Assert.That(table.HasArgs, Is.False);
            Assert.That(table.ArgsAt(0).Count, Is.EqualTo(0));

            int reviewed = table.IndexOf("b_reviewed");
            Assert.That(table.StatusAt(reviewed, "es-419"), Is.EqualTo(LocStatus.ApprovedEs), "más allá de aprobado_es, el texto fuente está aprobado");
            Assert.That(table.StatusAt(reviewed, "en"), Is.EqualTo(LocStatus.NativeReviewed));
            Assert.That(table.StatusAt(reviewed, "pt-BR"), Is.EqualTo(LocStatus.Missing));
            Assert.That(table.IsApprovedAt(reviewed, "en"), Is.True);

            int approved = table.IndexOf("c_approved");
            Assert.That(table.StatusAt(approved, "en"), Is.EqualTo(LocStatus.Draft), "traducción sin marcar como traducida");
            Assert.That(table.IsApprovedAt(approved, "es-419"), Is.True);
            Assert.That(table.IsApprovedAt(approved, "en"), Is.False);
            Assert.That(table.NotesAt(approved), Is.EqualTo("nota"));
            Assert.That(table.MaxCharsAt(0), Is.EqualTo(0), "max_chars vacío = sin límite");
        }

        [Test]
        public void Table_ColumnsAreReadByName_InAnyOrder()
        {
            LocTable table = LocTable.Parse("status,notes,es-419,key\naprobado_es,n,Siguiente,next\n", "strings.csv");
            Assert.That(table.Text("next", "es-419"), Is.EqualTo("Siguiente"));
            Assert.That(table.StatusAt(0, "es-419"), Is.EqualTo(LocStatus.ApprovedEs));
            Assert.That(table.ContextAt(0), Is.EqualTo(string.Empty), "sin columna de contexto");
        }

        [Test]
        public void Table_ArgsColumn_ParsesNamedNumbers()
        {
            LocTable table = LocTable.Parse(LocSamples.Facts, "facts.csv");
            LocArgs args = table.ArgsAt(table.IndexOf("fact.acai.01"));
            Assert.That(args.Count, Is.EqualTo(1));
            Assert.That(args[0].Name, Is.EqualTo("n"));
            Assert.That(args[0].Kind, Is.EqualTo(LocArgKind.Number));
            Assert.That(args[0].Integer, Is.EqualTo(5200L));
            LocArgs real = table.ArgsAt(table.IndexOf("fact.acai.04"));
            Assert.That(real[0].Kind, Is.EqualTo(LocArgKind.Decimal));
            Assert.That(real[0].Decimals, Is.EqualTo(1));
            Assert.That(real[0].Real, Is.EqualTo(12.5d));

            Assert.That(LocArgs.TryParse("n=5200; m=-3.25", out LocArgs two, out string? error), Is.True, error);
            Assert.That(two.Count, Is.EqualTo(2));
            Assert.That(two[1].Decimals, Is.EqualTo(2));
            Assert.That(LocArgs.TryParse("n=5.200,5", out _, out _), Is.False);
            Assert.That(LocArgs.TryParse("0=1", out _, out _), Is.False, "nada de posicionales");
            Assert.That(LocArgs.TryParse("a=1;b=2;c=3;d=4", out _, out _), Is.False, "como mucho 3");
            Assert.That(LocArgs.TryParse("n=1;n=2", out _, out _), Is.False);
            Assert.That(LocArgs.TryParse("sep=1", out _, out _), Is.False);
        }

        [Test]
        public void Table_HeaderErrors_GiveLineOne()
        {
            AssertError("key,es-419,contxt\nnext,Siguiente,c\n", 1, "contxt");
            AssertError("es-419,en\nSiguiente,Next\n", 1, "key");
            AssertError("key,en\nnext,Next\n", 1, "es-419");
            AssertError("key,es-419,status_en\nnext,Siguiente,traducido\n", 1, "falta «status»");
            AssertError("key,es-419,status,status_en\nnext,Siguiente,aprobado_es,traducido\n", 1, "status_en");
            AssertError("key,es-419,en,pt-BR,status,status_en\nnext,Siguiente,Next,Próximo,aprobado_es,traducido\n", 1, "status_pt-BR");
            AssertError("key,es-419,key\nnext,Siguiente,next\n", 1, "repetida");
            AssertError(string.Empty, 1, "cabecera");
        }

        [Test]
        public void Table_RowErrors_GiveTheirLine()
        {
            AssertError("key,es-419,status\nnext,Siguiente\n", 2, "campos");
            AssertError("key,es-419\nnext,Siguiente\nplay,Jugar\nnext,Otra vez\n", 4, "línea 2");
            AssertError("key,es-419\nNext,Siguiente\n", 2, "Next");
            AssertError("key,es-419\nnext level,Siguiente\n", 2, "clave");
            AssertError("key,es-419,max_chars\nnext,Siguiente,doce\n", 2, "max_chars");
            AssertError("key,es-419,status\nnext,Siguiente,aprobado\n", 2, "aprobado");
            AssertError("key,es-419,args\nnext,Siguiente,n=mil\n", 2, "args");
        }

        [Test]
        public void Table_UnsortedAt_FindsTheFirstRowOutOfOrder()
        {
            Assert.That(LocTable.Parse("key,es-419\nb,B\na,A\nc,C\n", "t.csv").UnsortedAt, Is.EqualTo(1));
            Assert.That(LocTable.Parse("key,es-419\nfact.acai.01,A\nfact_progress,B\n", "t.csv").UnsortedAt, Is.EqualTo(-1), "orden ordinal: «.» va antes que «_»");
        }

        [Test]
        public void Catalog_KeyInBothTables_IsAnError()
        {
            var error = Assert.Throws<LocCsvException>(() => LocCatalog.Parse("key,es-419\nnext,Siguiente\n", "key,es-419\nfact.a,A\nnext,Otra\n"));
            Assert.That(error!.File, Is.EqualTo("facts.csv"));
            Assert.That(error.Line, Is.EqualTo(3));
        }

        [Test]
        public void StatusNames_RoundTrip()
        {
            foreach (LocStatus status in new[] { LocStatus.Draft, LocStatus.PendingAdvisor, LocStatus.ApprovedEs, LocStatus.Translated, LocStatus.NativeReviewed })
            {
                Assert.That(LocStatusNames.TryParse(LocStatusNames.ToCsv(status), out LocStatus parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(status));
            }

            Assert.That(LocStatusNames.TryParse("Aprobado_ES", out _), Is.False, "exacto, en minúsculas");
            Assert.That(LocStatus.ApprovedEs > LocStatus.PendingAdvisor, Is.True, "el orden es el del flujo");
        }

        private static void AssertError(string csv, int line, string fragment)
        {
            var error = Assert.Throws<LocCsvException>(() => LocTable.Parse(csv, "t.csv"));
            Assert.That(error!.Line, Is.EqualTo(line), error.Message);
            Assert.That(error.Message, Does.Contain(fragment));
        }
    }
}
