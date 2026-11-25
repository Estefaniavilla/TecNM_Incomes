using System.Globalization;
using System.Text;

namespace TecNM.Api.Modules.Reportes.Exporters;

/// <summary>
/// Pure C# PDF 1.4 document builder for institutional financial and operational reports.
/// Multi-page support, standards compliant, zero third-party binary dependencies.
/// </summary>
public static class PdfReportExporter
{
    private const int RowsPerPage = 38;

    public static byte[] BuildPdf(ReportDocumentData data)
    {
        // 1. Divide rows into pages
        var totalRows = data.Rows.Count;
        var pagesCount = Math.Max(1, (int)Math.Ceiling((double)totalRows / RowsPerPage));
        var pageStreams = new List<string>();

        for (int pageIndex = 0; pageIndex < pagesCount; pageIndex++)
        {
            var isFirstPage = pageIndex == 0;
            var isLastPage = pageIndex == pagesCount - 1;
            var rowsChunk = data.Rows.Skip(pageIndex * RowsPerPage).Take(RowsPerPage).ToList();

            var content = new StringBuilder();
            content.AppendLine("BT"); // Begin text

            // --- Header on every page ---
            content.AppendLine("/F2 12 Tf"); // Bold 12pt
            content.AppendLine("50 755 Td");
            content.AppendLine($"(INSTITUTO TECNOLOGICO SUPERIOR DE MONCLOVA \\(TecNM\\)) Tj");

            content.AppendLine("/F1 8 Tf");
            content.AppendLine("0 -13 Td");
            content.AppendLine($"(Departamento de Recursos Financieros y Control de Ingresos) Tj");

            content.AppendLine("/F2 11 Tf");
            content.AppendLine("0 -15 Td");
            content.AppendLine($"({EscapePdf(data.Title.ToUpperInvariant())}) Tj");

            if (!string.IsNullOrWhiteSpace(data.Subtitle))
            {
                content.AppendLine("/F1 8 Tf");
                content.AppendLine("0 -11 Td");
                content.AppendLine($"({EscapePdf(data.Subtitle)}) Tj");
            }

            // Page stamp top right
            content.AppendLine("/F1 7.5 Tf");
            content.AppendLine($"350 0 Td");
            content.AppendLine($"(Pagina {pageIndex + 1} de {pagesCount}  |  {DateTime.Now:yyyy-MM-dd HH:mm}) Tj");
            content.AppendLine($"-350 0 Td"); // Reset x

            // Metadata on first page
            if (isFirstPage && data.Metadata.Count > 0)
            {
                content.AppendLine("/F1 8 Tf");
                foreach (var meta in data.Metadata.Take(3))
                {
                    content.AppendLine("0 -10 Td");
                    content.AppendLine($"({EscapePdf(meta)}) Tj");
                }
            }

            // Divider line
            content.AppendLine("/F1 7 Tf");
            content.AppendLine("0 -12 Td");
            content.AppendLine($"({new string('-', 120)}) Tj");

            // Table Headers
            content.AppendLine("/F2 7.5 Tf");
            content.AppendLine("0 -12 Td");
            var headerLine = FormatTableRow(data.Headers, data.Headers);
            content.AppendLine($"({EscapePdf(headerLine)}) Tj");

            content.AppendLine("/F1 7 Tf");
            content.AppendLine("0 -8 Td");
            content.AppendLine($"({new string('-', 120)}) Tj");

            // Data rows
            content.AppendLine("/F1 7 Tf");
            foreach (var row in rowsChunk)
            {
                content.AppendLine("0 -10 Td");
                var rowText = FormatTableRow(row, data.Headers);
                content.AppendLine($"({EscapePdf(rowText)}) Tj");
            }

            // Totals and signatures on last page
            if (isLastPage)
            {
                content.AppendLine("0 -12 Td");
                content.AppendLine($"({new string('-', 120)}) Tj");

                if (data.Totals.Count > 0)
                {
                    content.AppendLine("/F2 8 Tf");
                    content.AppendLine("0 -12 Td");
                    content.AppendLine($"(RESUMEN DE TOTALES:) Tj");

                    content.AppendLine("/F1 7.5 Tf");
                    foreach (var t in data.Totals)
                    {
                        content.AppendLine("0 -10 Td");
                        content.AppendLine($"({EscapePdf(t.Label,-40)}: {EscapePdf(t.Value)}) Tj");
                    }
                }

                // Signatures
                var elaboro = data.ElaboradoPor ?? "Cajero en Turno";
                var reviso = data.RevisadoPor ?? "Jefe de Recursos Financieros";
                content.AppendLine("0 -25 Td");
                content.AppendLine("/F1 7.5 Tf");
                content.AppendLine($"({EscapePdf(new string('_', 30)),-50}     {EscapePdf(new string('_', 30))}) Tj");
                content.AppendLine("0 -10 Td");
                content.AppendLine($"({EscapePdf(elaboro),-50}     {EscapePdf(reviso)}) Tj");
                content.AppendLine("0 -8 Td");
                content.AppendLine($"(ELABORO{"",-43}     REVISO Y AUTORIZO) Tj");
            }

            content.AppendLine("ET"); // End text
            pageStreams.Add(content.ToString());
        }

        return AssemblePdfDocument(pageStreams);
    }

    private static string FormatTableRow(List<string> row, List<string> headers)
    {
        // Allocate space based on number of columns
        var colCount = Math.Max(1, headers.Count);
        var maxColWidth = Math.Max(10, 110 / colCount);

        var parts = new List<string>();
        for (int i = 0; i < row.Count; i++)
        {
            var val = Sanitize(row[i]);
            var header = i < headers.Count ? headers[i] : "";
            var isMoney = header.Contains("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                          header.Contains("IMPORTE", StringComparison.OrdinalIgnoreCase) ||
                          header.Contains("MONTO", StringComparison.OrdinalIgnoreCase) ||
                          header.Contains("SALDO", StringComparison.OrdinalIgnoreCase) ||
                          val.StartsWith('$');

            if (val.Length > maxColWidth) val = val[..maxColWidth];
            parts.Add(isMoney ? val.PadLeft(maxColWidth) : val.PadRight(maxColWidth));
        }

        return string.Join(" ", parts);
    }

    private static byte[] AssemblePdfDocument(List<string> pageStreams)
    {
        var objects = new List<string>();
        var objByteOffsets = new List<int>();
        var pdf = new MemoryStream();

        void WriteString(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            pdf.Write(bytes, 0, bytes.Length);
        }

        WriteString("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        int numPages = pageStreams.Count;
        // Base objects offset count
        int firstPageObjNum = 5;

        // Obj 1: Catalog
        objByteOffsets.Add((int)pdf.Position);
        WriteString($"1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // Obj 2: Pages (filled with Kids)
        var kids = new StringBuilder();
        for (int p = 0; p < numPages; p++)
        {
            int pageObj = firstPageObjNum + (p * 2);
            kids.Append($"{pageObj} 0 R ");
        }

        objByteOffsets.Add((int)pdf.Position);
        WriteString($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {numPages} >>\nendobj\n");

        // Obj 3: Font F1 (Helvetica)
        objByteOffsets.Add((int)pdf.Position);
        WriteString($"3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");

        // Obj 4: Font F2 (Helvetica-Bold)
        objByteOffsets.Add((int)pdf.Position);
        WriteString($"4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        // Page and Stream objects
        for (int p = 0; p < numPages; p++)
        {
            int pageObjNum = firstPageObjNum + (p * 2);
            int streamObjNum = pageObjNum + 1;
            var streamContent = pageStreams[p];
            var streamBytes = Encoding.Latin1.GetBytes(streamContent);

            // Page obj
            objByteOffsets.Add((int)pdf.Position);
            WriteString($"{pageObjNum} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents {streamObjNum} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>\nendobj\n");

            // Content Stream obj
            objByteOffsets.Add((int)pdf.Position);
            WriteString($"{streamObjNum} 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
            pdf.Write(streamBytes, 0, streamBytes.Length);
            WriteString("\nendstream\nendobj\n");
        }

        // Cross-Reference Table
        int xrefStart = (int)pdf.Position;
        int totalObjects = 4 + (numPages * 2);
        WriteString($"xref\n0 {totalObjects + 1}\n");
        WriteString("0000000000 65535 f \n");
        foreach (var offset in objByteOffsets)
        {
            WriteString($"{offset:D10} 00000 n \n");
        }

        // Trailer
        WriteString($"trailer\n<< /Size {totalObjects + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF\n");

        return pdf.ToArray();
    }

    private static string EscapePdf(string text)
    {
        return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string EscapePdf(string text, int pad)
    {
        var sanitized = Sanitize(text);
        if (sanitized.Length > Math.Abs(pad))
        {
            sanitized = sanitized[..Math.Abs(pad)];
        }
        return pad < 0 ? sanitized.PadRight(-pad) : sanitized.PadLeft(pad);
    }

    private static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                if (c >= 32 && c <= 126) sb.Append(c);
                else sb.Append(' ');
            }
        }
        return sb.ToString();
    }
}
