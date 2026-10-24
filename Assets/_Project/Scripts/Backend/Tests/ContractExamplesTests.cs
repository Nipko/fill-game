#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Backend.Contract;
using AF.Backend.Json;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>
    /// F3-1: el cliente (de)serializa todos los ejemplos válidos de <c>infra/contracts/v1/examples</c> y rechaza los inválidos,
    /// guiado por <c>index.json</c> (contrato único, Q-AWS-12). Los ejemplos se leen del repositorio, sin copiarlos.
    /// </summary>
    public sealed class ContractExamplesTests
    {
        private delegate bool Parser<T>(string? json, out T? value, out string? error)
            where T : class;

        /// <summary>Lee y escribe un esquema del contrato.</summary>
        private sealed class Codec
        {
            public Codec(Func<string, (bool ok, string? error, Func<string>? write)> read)
            {
                Read = read;
            }

            public Func<string, (bool ok, string? error, Func<string>? write)> Read { get; }

            public static Codec Of<T>(Parser<T> parse, Func<T, string> write)
                where T : class
            {
                return new Codec(json =>
                {
                    bool ok = parse(json, out T? value, out string? error);
                    return (ok, error, ok ? () => write(value!) : (Func<string>?)null);
                });
            }
        }

        private static readonly Dictionary<string, Codec> Codecs = new Dictionary<string, Codec>(StringComparer.Ordinal)
        {
            ["verify-purchase.request.schema.json"] = Codec.Of<VerifyPurchaseRequest>(VerifyPurchaseRequest.TryParse, v => v.ToJson()),
            ["verify-purchase.response.schema.json"] = Codec.Of<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, v => v.ToJson()),
            ["restore-ledger.request.schema.json"] = Codec.Of<RestoreLedgerRequest>(RestoreLedgerRequest.TryParse, v => v.ToJson()),
            ["restore-ledger.response.schema.json"] = Codec.Of<RestoreLedgerResponse>(RestoreLedgerResponse.TryParseStrict, v => v.ToJson()),
            ["rewarded-ssv.request.schema.json"] = Codec.Of<SsvCallback>(SsvCallback.TryParse, v => v.ToJson()),
            ["rewarded-ssv.response.schema.json"] = Codec.Of<SsvResponse>(SsvResponse.TryParseStrict, v => v.ToJson()),
            ["account-code.response.schema.json"] = Codec.Of<AccountCodeResponse>(AccountCodeResponse.TryParseStrict, v => v.ToJson()),
            ["account-delete.request.schema.json"] = Codec.Of<DeleteAccountRequest>(DeleteAccountRequest.TryParse, v => v.ToJson()),
            ["account-delete.response.schema.json"] = Codec.Of<DeleteAccountResponse>(DeleteAccountResponse.TryParseStrict, v => v.ToJson()),
            ["account-delete-request.request.schema.json"] = Codec.Of<DeleteRequestRequest>(DeleteRequestRequest.TryParse, v => v.ToJson()),
            ["account-delete-request.response.schema.json"] = Codec.Of<DeleteRequestResponse>(DeleteRequestResponse.TryParseStrict, v => v.ToJson()),
            ["account-delete-confirm.request.schema.json"] = Codec.Of<DeleteConfirmRequest>(DeleteConfirmRequest.TryParse, v => v.ToJson()),
            ["voided-purchases-job.input.schema.json"] = Codec.Of<VoidedPurchasesJobInput>(VoidedPurchasesJobInput.TryParseStrict, v => v.ToJson()),
            ["voided-purchases-job.output.schema.json"] = Codec.Of<VoidedPurchasesJobOutput>(VoidedPurchasesJobOutput.TryParseStrict, v => v.ToJson()),
            ["league-wins.request.schema.json"] = Codec.Of<LeagueWinsRequestDto>(LeagueWinsRequestDto.TryParse, v => v.ToJson()),
            ["league-wins.response.schema.json"] = Codec.Of<LeagueWinsResponseDto>(LeagueWinsResponseDto.TryParseStrict, v => v.ToJson()),
            ["league-board.response.schema.json"] = Codec.Of<LeagueBoardResponseDto>(LeagueBoardResponseDto.TryParseStrict, v => v.ToJson()),
            ["error.schema.json"] = Codec.Of<ApiError>(ApiError.TryParseStrict, v => v.ToJson()),
        };

        /// <summary>Un ejemplo con su esquema y si debe pasar.</summary>
        private readonly struct ExampleCase
        {
            public ExampleCase(string example, string schema, bool valid)
            {
                Example = example;
                Schema = schema;
                Valid = valid;
            }

            public string Example { get; }

            public string Schema { get; }

            public bool Valid { get; }
        }

        [Test]
        public void EverySchemaOfTheContractHasACodec()
        {
            var missing = new List<string>();
            foreach (string path in Directory.GetFiles(BackendTestFiles.ContractDir, "*.schema.json"))
            {
                string name = Path.GetFileName(path);
                if (name != "common.schema.json" && !Codecs.ContainsKey(name))
                {
                    missing.Add(name);
                }
            }

            Assert.That(missing, Is.Empty, "esquemas del contrato sin lector en AF.Backend");
        }

        [Test]
        public void EveryExampleFileIsCoveredByTheIndex()
        {
            var covered = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExampleCase c in Cases())
            {
                covered.Add(c.Example);
            }

            var uncovered = new List<string>();
            foreach (string path in Directory.GetFiles(BackendTestFiles.ExamplesDir, "*.json"))
            {
                if (!covered.Contains(Path.GetFileName(path)))
                {
                    uncovered.Add(Path.GetFileName(path));
                }
            }

            Assert.That(uncovered, Is.Empty, "ejemplos que ningún test lee");
            Assert.That(covered.Count, Is.GreaterThanOrEqualTo(25), "el índice debería listar todos los ejemplos");
        }

        [Test]
        public void ValidExamplesRoundTrip()
        {
            var failures = new List<string>();
            int checkedCount = 0;
            foreach (ExampleCase c in Cases())
            {
                if (!c.Valid)
                {
                    continue;
                }

                checkedCount++;
                string original = BackendTestFiles.ReadExample(c.Example);
                (bool ok, string? error, Func<string>? write) = Codecs[c.Schema].Read(original);
                if (!ok)
                {
                    failures.Add(c.Example + ": no se lee (" + error + ")");
                    continue;
                }

                string written = write!();
                if (!JsonNode.StructurallyEqual(BackendJson.Parse(original), BackendJson.Parse(written)))
                {
                    failures.Add(c.Example + ": la escritura no coincide: " + written);
                    continue;
                }

                (bool again, string? againError, _) = Codecs[c.Schema].Read(written);
                if (!again)
                {
                    failures.Add(c.Example + ": lo escrito no se vuelve a leer (" + againError + ")");
                }
            }

            Assert.That(failures, Is.Empty);
            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(19));
        }

        [Test]
        public void InvalidExamplesAreRejected()
        {
            var accepted = new List<string>();
            int checkedCount = 0;
            foreach (ExampleCase c in Cases())
            {
                if (c.Valid)
                {
                    continue;
                }

                checkedCount++;
                (bool ok, string? error, _) = Codecs[c.Schema].Read(BackendTestFiles.ReadExample(c.Example));
                if (ok)
                {
                    accepted.Add(c.Example);
                }
                else
                {
                    Assert.That(error, Is.Not.Null.And.Not.Empty, c.Example);
                }
            }

            Assert.That(accepted, Is.Empty, "ejemplos inválidos que el cliente acepta");
            Assert.That(checkedCount, Is.EqualTo(7), "con league-wins.request.invalid-too-many (D-046)");
        }

        [Test]
        public void AppOperationsMatchTheIndex()
        {
            JsonNode index = BackendJson.Parse(BackendTestFiles.ReadContract("index.json"));
            Assert.That(index.Get("version")!.Text, Is.EqualTo("v1"));
            foreach (BackendOperation op in BackendOperation.AppOperations)
            {
                JsonNode? entry = null;
                foreach (JsonNode candidate in index.Get("operations")!.Items)
                {
                    if (candidate.Get("operation")!.Text == op.Name)
                    {
                        entry = candidate;
                    }
                }

                Assert.That(entry, Is.Not.Null, op.Name);
                Assert.That(entry!.Get("method")!.Text, Is.EqualTo(op.Method), op.Name);
                Assert.That(entry.Get("path")!.Text, Is.EqualTo(op.Path), op.Name);
                Assert.That(entry.Get("auth")!.Text, Is.EqualTo("firebase"), op.Name);
                Assert.That(entry.Get("integrity")!.BoolValue, Is.EqualTo(op.RequiresIntegrity), op.Name);
            }
        }

        [Test]
        public void AccountCodeHasNoRequestBody()
        {
            JsonNode index = BackendJson.Parse(BackendTestFiles.ReadContract("index.json"));
            foreach (JsonNode candidate in index.Get("operations")!.Items)
            {
                if (candidate.Get("operation")!.Text == "getAccountCode")
                {
                    Assert.That(candidate.Get("request")!.Kind, Is.EqualTo(JsonKind.Null));
                    return;
                }
            }

            Assert.Fail("getAccountCode no está en index.json");
        }

        [Test]
        public void ErrorCodesMatchTheSchema()
        {
            JsonNode schema = BackendJson.Parse(BackendTestFiles.ReadContract("error.schema.json"));
            JsonNode codes = schema.Get("properties")!.Get("error")!.Get("properties")!.Get("code")!.Get("enum")!;
            var wire = new List<string>();
            foreach (JsonNode code in codes.Items)
            {
                wire.Add(code.Text!);
                Assert.That(ApiError.CodeFromWire(code.Text), Is.Not.EqualTo(ApiErrorCode.Unknown), code.Text);
                Assert.That(ApiError.CodeToWire(ApiError.CodeFromWire(code.Text)), Is.EqualTo(code.Text));
            }

            Assert.That(wire.Count, Is.EqualTo(Enum.GetValues(typeof(ApiErrorCode)).Length - 1), "un código por valor de ApiErrorCode");
        }

        /// <summary>Todos los ejemplos de <c>index.json</c> (peticiones, respuestas, inválidos y jobs) y los de error.</summary>
        private static List<ExampleCase> Cases()
        {
            JsonNode index = BackendJson.Parse(BackendTestFiles.ReadContract("index.json"));
            var cases = new List<ExampleCase>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            void Add(JsonNode? list, JsonNode? schema, bool valid)
            {
                if (list == null || schema == null || schema.Kind != JsonKind.String)
                {
                    return;
                }

                foreach (JsonNode item in list.Items)
                {
                    if (seen.Add(item.Text!))
                    {
                        cases.Add(new ExampleCase(item.Text!, schema.Text!, valid));
                    }
                }
            }

            foreach (JsonNode op in index.Get("operations")!.Items)
            {
                JsonNode examples = op.Get("examples")!;
                Add(examples.Get("request"), op.Get("request"), true);
                Add(examples.Get("response"), op.Get("response"), true);
                Add(examples.Get("invalidRequest"), op.Get("request"), false);
            }

            foreach (JsonNode job in index.Get("jobs")!.Items)
            {
                JsonNode examples = job.Get("examples")!;
                Add(examples.Get("input"), job.Get("input"), true);
                Add(examples.Get("output"), job.Get("output"), true);
            }

            foreach (string path in Directory.GetFiles(BackendTestFiles.ExamplesDir, "error.*.json"))
            {
                string name = Path.GetFileName(path);
                if (seen.Add(name))
                {
                    cases.Add(new ExampleCase(name, "error.schema.json", true));
                }
            }

            return cases;
        }
    }
}
