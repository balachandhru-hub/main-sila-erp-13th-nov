using System.Globalization;
using System.Text;
using Buyer.Domain.Dtos;
using ClosedXML.Excel;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Reading and writing of the POS Excel/CSV files (sales upload, outlet and item mapping imports, templates).
    /// The content is checked against its extension (xlsx = zip signature, csv = text without NUL bytes).
    /// </summary>
    public static class SilaPosFiles
    {
        public const long MAX_FILE_BYTES = 10 * 1024 * 1024;

        public const string XLSX_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        /// <summary>Rows of the first sheet (xlsx) or of the file (csv) as text; the first row is the header.</summary>
        public static List<List<string>> ReadTable(byte[] content, string? fileName, ILoggerManager logger, bool allowCsv)
        {
            string[] allowedKinds = allowCsv
                ? new[] { SilaFileRules.KIND_XLSX, SilaFileRules.KIND_CSV }
                : new[] { SilaFileRules.KIND_XLSX };
            string kind = SilaFileRules.Validate(logger, "POS file", fileName, content, allowedKinds, MAX_FILE_BYTES);
            return kind == SilaFileRules.KIND_CSV ? ReadCsv(content) : ReadXlsx(content, fileName, logger);
        }

        /// <summary>Column positions by normalised header name (letters and digits, upper case).</summary>
        public static Dictionary<string, int> HeaderMap(List<string> header)
        {
            Dictionary<string, int> columns = new Dictionary<string, int>();
            for (int index = 0; index < header.Count; index++)
            {
                string name = Normalize(header[index]);
                if (name.Length > 0 && !columns.ContainsKey(name))
                {
                    columns[name] = index;
                }
            }

            return columns;
        }

        /// <summary>The cell of the first of the column names present in the header (aliases), or null.</summary>
        public static string? Cell(List<string> cells, Dictionary<string, int> columns, params string[] names)
        {
            foreach (string name in names)
            {
                if (columns.TryGetValue(name, out int index))
                {
                    return index < cells.Count ? cells[index] : null;
                }
            }

            return null;
        }

        /// <summary>A workbook with one sheet: bold header row and optional example rows.</summary>
        public static SilaPosFileDto BuildTemplate(string fileName, string sheetName, string[] header, List<string[]> examples)
        {
            using XLWorkbook workbook = new XLWorkbook();
            WriteSheet(workbook, sheetName, header, examples);
            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new SilaPosFileDto { Content = stream.ToArray(), ContentType = XLSX_CONTENT_TYPE, FileName = fileName };
        }

        /// <summary>
        /// Sales template: SalesData (mandatory 9 columns + optional enhanced columns) and FieldGuide descriptions.
        /// Legacy nine-column files remain valid; optional columns are backward compatible.
        /// </summary>
        public static SilaPosFileDto BuildSalesTemplate()
        {
            string[] header =
            {
                "BusinessDate", "TransactionId", "LineId", "PosCode", "Qty", "UOM", "OutletCode", "Currency", "Amount",
                "SourceSystem", "TransactionType", "OriginalTransactionId", "OriginalLineId"
            };
            List<string[]> examples = new List<string[]>
            {
                new[] { "2026-11-18", "100045", "1", "34567", "2", "EA", "FL10", "AED", "380.00", "FILE", "SALE", "", "" },
                new[] { "2026-11-18", "100046", "1", "34567", "1", "EA", "FL10", "AED", "190.00", "FILE", "RETURN", "100045", "1" },
            };
            using XLWorkbook workbook = new XLWorkbook();
            WriteSheet(workbook, "SalesData", header, examples);
            IXLWorksheet guide = workbook.Worksheets.Add("FieldGuide");
            string[,] fields =
            {
                { "Field", "Required", "Description", "Format / notes" },
                { "BusinessDate", "Yes", "Business date of the sale (not the upload timestamp).", "yyyy-MM-dd preferred; also dd/MM/yyyy, yyyyMMdd" },
                { "TransactionId", "Yes", "POS transaction identifier. Keep numeric-looking values as text.", "String, max 100" },
                { "LineId", "No (defaults to 1)", "Line number within the transaction.", "Integer >= 1" },
                { "PosCode", "Yes", "POS item code mapped to a SILA recipe / menu item.", "String, max 100" },
                { "Qty", "Yes", "Quantity sold. Negative qty is not treated as a return unless TransactionType says so.", "Non-zero decimal; alias Quantity" },
                { "UOM", "No", "Sold unit of measure; must match recipe selling UOM when set.", "e.g. EA" },
                { "OutletCode", "Yes", "POS outlet code mapped to a SILA inventory location.", "String; alias OutletId" },
                { "Currency", "No", "ISO currency of Amount.", "3 letters e.g. AED" },
                { "Amount", "No", "POS sales amount for the line (source decides tax/discount semantics).", "Decimal" },
                { "SourceSystem", "No", "Optional POS source system label; upload still uses the selected POS source.", "String" },
                { "TransactionType", "No", "SALE (default for legacy files), RETURN or VOID.", "SALE | RETURN | VOID" },
                { "OriginalTransactionId", "No", "For RETURN/VOID: original sale transaction id.", "Required when TransactionType is RETURN/VOID" },
                { "OriginalLineId", "No", "For RETURN/VOID: original sale line id.", "Integer >= 1" },
            };
            for (int row = 0; row < fields.GetLength(0); row++)
            {
                for (int column = 0; column < fields.GetLength(1); column++)
                {
                    guide.Cell(row + 1, column + 1).Value = fields[row, column];
                    if (row == 0)
                    {
                        guide.Cell(row + 1, column + 1).Style.Font.Bold = true;
                    }
                }
            }

            guide.Columns().AdjustToContents();
            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new SilaPosFileDto { Content = stream.ToArray(), ContentType = XLSX_CONTENT_TYPE, FileName = "pos-sales-template.xlsx" };
        }

        private static void WriteSheet(XLWorkbook workbook, string sheetName, string[] header, List<string[]> examples)
        {
            IXLWorksheet sheet = workbook.Worksheets.Add(sheetName);
            for (int column = 0; column < header.Length; column++)
            {
                sheet.Cell(1, column + 1).Value = header[column];
                sheet.Cell(1, column + 1).Style.Font.Bold = true;
                // Text cells keep codes such as 0012 as typed.
                sheet.Column(column + 1).Style.NumberFormat.Format = "@";
            }

            for (int row = 0; row < examples.Count; row++)
            {
                for (int column = 0; column < examples[row].Length && column < header.Length; column++)
                {
                    sheet.Cell(row + 2, column + 1).Value = examples[row][column];
                }
            }

            sheet.Columns().AdjustToContents();
        }

        public static string Normalize(string value)
        {
            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private static List<List<string>> ReadXlsx(byte[] content, string? fileName, ILoggerManager logger)
        {
            try
            {
                using MemoryStream stream = new MemoryStream(content);
                using XLWorkbook workbook = new XLWorkbook(stream);
                // Prefer SalesData (authoritative POS sales sheet); otherwise first sheet.
                IXLWorksheet sheet = workbook.Worksheets.FirstOrDefault(x =>
                        string.Equals(x.Name, "SalesData", StringComparison.OrdinalIgnoreCase))
                    ?? workbook.Worksheet(1);
                IXLRange? used = sheet.RangeUsed();
                List<List<string>> table = new List<List<string>>();
                if (used == null)
                {
                    return table;
                }

                int columnCount = used.LastColumn().ColumnNumber();
                foreach (IXLRangeRow row in used.Rows())
                {
                    List<string> cells = new List<string>();
                    for (int column = 1; column <= columnCount; column++)
                    {
                        cells.Add(CellText(row.Cell(column).Value));
                    }

                    table.Add(cells);
                }

                return table;
            }
            catch (Exception exception) when (exception is not BaseCustomException)
            {
                logger.LogError($"POS workbook cannot be read. FileName: {fileName}, Error: {SilaLogText.Short(exception.Message)}");
                throw new BadRequestCustomException("The Excel file cannot be read.", "Save the file as a .xlsx workbook with the data on the first sheet.");
            }
        }

        private static string CellText(XLCellValue value)
        {
            if (value.IsDateTime)
            {
                return value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (value.IsNumber)
            {
                return value.GetNumber().ToString(CultureInfo.InvariantCulture);
            }

            return value.IsBlank ? string.Empty : value.ToString(CultureInfo.InvariantCulture).Trim();
        }

        /// <summary>RFC 4180 CSV: comma or semicolon separated, quoted fields may hold separators, quotes and line breaks.</summary>
        private static List<List<string>> ReadCsv(byte[] content)
        {
            string text = Encoding.UTF8.GetString(content).TrimStart('﻿');
            int firstLineEnd = text.IndexOfAny(new[] { '\r', '\n' });
            string header = firstLineEnd < 0 ? text : text.Substring(0, firstLineEnd);
            char separator = header.Count(x => x == ';') > header.Count(x => x == ',') ? ';' : ',';

            List<List<string>> table = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool quoted = false;
            for (int index = 0; index < text.Length; index++)
            {
                char current = text[index];
                if (quoted)
                {
                    if (current == '"' && index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (current == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(current);
                    }
                }
                else if (current == '"')
                {
                    quoted = true;
                }
                else if (current == separator)
                {
                    row.Add(field.ToString().Trim());
                    field.Clear();
                }
                else if (current == '\r' || current == '\n')
                {
                    if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString().Trim());
                    field.Clear();
                    table.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(current);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString().Trim());
                table.Add(row);
            }

            return table;
        }
    }
}
