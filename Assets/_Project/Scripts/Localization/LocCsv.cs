#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Localization
{
    /// <summary>Error de formato en una tabla de textos, con el archivo y la línea (1 = la primera).</summary>
    public sealed class LocCsvException : FormatException
    {
        /// <summary>Error en <paramref name="file"/>, línea <paramref name="line"/>.</summary>
        public LocCsvException(string file, int line, string message)
            : base(file + ", línea " + line.ToString(CultureInfo.InvariantCulture) + ": " + message)
        {
            File = file;
            Line = line;
        }

        /// <summary>Archivo («strings.csv», «facts.csv»…).</summary>
        public string File { get; }

        /// <summary>Línea del problema (1 = la primera).</summary>
        public int Line { get; }
    }

    /// <summary>Un registro del CSV: sus campos y la línea donde empieza (un campo entre comillas puede ocupar varias).</summary>
    public sealed class LocCsvRow
    {
        internal LocCsvRow(int line, string[] fields)
        {
            Line = line;
            Fields = fields;
        }

        /// <summary>Línea donde empieza (1 = la primera).</summary>
        public int Line { get; }

        /// <summary>Campos, ya sin comillas; los saltos de línea de dentro, como «\n».</summary>
        public IReadOnlyList<string> Fields { get; }
    }

    /// <summary>
    /// Lector de las tablas de textos (contrato 32; 08 §4.7): RFC 4180 (campos entre comillas con comas, saltos de línea y
    /// comillas dobladas) en UTF-8 sin BOM. Un BOM es un error con su línea (las tablas las leen Unity, los agentes,
    /// <c>git diff</c> y los traductores, y el BOM se cuela como carácter en la primera clave). Acepta LF y CRLF (git puede
    /// convertir el final de línea en Windows) y deja los saltos de dentro de un campo como «\n». Salta las líneas vacías. No
    /// sabe nada de las columnas: eso es <see cref="LocTable"/>; B1 lo usa también para <c>glossary.csv</c>.
    /// </summary>
    public static class LocCsv
    {
        /// <summary>El BOM de UTF-8 como carácter (U+FEFF).</summary>
        public const char ByteOrderMark = '﻿';

        private const string BomMessage = "el archivo lleva BOM (U+FEFF): las tablas van en UTF-8 sin BOM (contrato 32)";

        /// <summary>
        /// Decodifica los bytes de un archivo en UTF-8 estricto: con BOM (EF BB BF) o con bytes que no son UTF-8, error con la
        /// línea. Úsalo al leer del disco: <c>File.ReadAllText</c> se come el BOM sin avisar.
        /// </summary>
        public static string Decode(byte[] bytes, string file)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                throw new LocCsvException(file, 1, BomMessage);
            }

            var strict = new UTF8Encoding(false, true);
            try
            {
                return strict.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                // Localiza la línea: el byte 0x0A nunca va dentro de una secuencia de UTF-8.
                int line = 1;
                int lineStart = 0;
                for (int i = 0; i <= bytes.Length; i++)
                {
                    if (i == bytes.Length || bytes[i] == 0x0A)
                    {
                        try
                        {
                            strict.GetString(bytes, lineStart, i - lineStart);
                        }
                        catch (DecoderFallbackException)
                        {
                            throw new LocCsvException(file, line, "bytes que no son UTF-8 válido");
                        }

                        line++;
                        lineStart = i + 1;
                    }
                }

                throw new LocCsvException(file, 1, "bytes que no son UTF-8 válido");
            }
        }

        /// <summary>Lee todos los registros de <paramref name="text"/> (el primero es la cabecera, si la hay).</summary>
        public static IReadOnlyList<LocCsvRow> Parse(string text, string file)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var rows = new List<LocCsvRow>();
            var fields = new List<string>();
            var field = new StringBuilder();
            int n = text.Length;
            int i = 0;
            int line = 1;
            int recordLine = 1;
            bool onlyEmptyUnquoted = true;
            while (i < n || fields.Count > 0)
            {
                bool quoted = i < n && text[i] == '"';
                if (quoted)
                {
                    int openLine = line;
                    i++;
                    while (true)
                    {
                        if (i >= n)
                        {
                            throw new LocCsvException(file, openLine, "comillas sin cerrar");
                        }

                        char c = text[i];
                        if (c == '"')
                        {
                            if (i + 1 < n && text[i + 1] == '"')
                            {
                                field.Append('"');
                                i += 2;
                                continue;
                            }

                            i++;
                            break;
                        }

                        if (c == '\r')
                        {
                            if (i + 1 < n && text[i + 1] == '\n')
                            {
                                field.Append('\n');
                                line++;
                                i += 2;
                                continue;
                            }

                            throw new LocCsvException(file, line, "retorno de carro (CR) suelto");
                        }

                        if (c == ByteOrderMark)
                        {
                            throw new LocCsvException(file, line, BomMessage);
                        }

                        if (c == '\n')
                        {
                            line++;
                        }

                        field.Append(c);
                        i++;
                    }

                    if (i < n && text[i] != ',' && text[i] != '\n' && text[i] != '\r')
                    {
                        throw new LocCsvException(file, line, "texto después de las comillas de cierre");
                    }
                }
                else
                {
                    while (i < n)
                    {
                        char c = text[i];
                        if (c == ',' || c == '\n' || c == '\r')
                        {
                            break;
                        }

                        if (c == '"')
                        {
                            throw new LocCsvException(file, line, "comillas dentro de un campo sin comillas (RFC 4180: el campo entero va entre comillas y las de dentro, dobladas)");
                        }

                        if (c == ByteOrderMark)
                        {
                            throw new LocCsvException(file, line, BomMessage);
                        }

                        field.Append(c);
                        i++;
                    }
                }

                onlyEmptyUnquoted &= !quoted && field.Length == 0;
                fields.Add(field.ToString());
                field.Clear();
                if (i < n && text[i] == ',')
                {
                    i++;
                    continue;
                }

                // Fin de registro: salto de línea o fin del texto.
                if (i < n)
                {
                    if (text[i] == '\r')
                    {
                        if (i + 1 >= n || text[i + 1] != '\n')
                        {
                            throw new LocCsvException(file, line, "retorno de carro (CR) suelto");
                        }

                        i++;
                    }

                    i++;
                }

                if (!(fields.Count == 1 && onlyEmptyUnquoted))
                {
                    rows.Add(new LocCsvRow(recordLine, fields.ToArray()));
                }

                fields.Clear();
                onlyEmptyUnquoted = true;
                line++;
                recordLine = line;
                if (i >= n)
                {
                    break;
                }
            }

            return rows;
        }
    }
}
