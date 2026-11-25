using System.Text;

namespace TecNM.Api.Modules.Reportes.Exporters;

/// <summary>
/// RFC 4180 compliant CSV document exporter with UTF-8 BOM encoding for seamless Microsoft Excel compatibility.
/// </summary>
public static class CsvReportExporter
{
    public static byte[] BuildCsv(ReportDocumentData data)
    {
        var sb = new StringBuilder();

        // 1. Header Metadata Section
        sb.AppendLine($"\"{Escape(data.Title)}\"");
        if (!string.IsNullOrWhiteSpace(data.Subtitle))
        {
            sb.AppendLine($"\"{Escape(data.Subtitle)}\"");
        }

        foreach (var meta in data.Metadata)
        {
            sb.AppendLine($"\"# {Escape(meta)}\"");
        }
        sb.AppendLine();

        // 2. Table Column Headers
        var headerLine = string.Join(",", data.Headers.Select(h => $"\"{Escape(h)}\""));
        sb.AppendLine(headerLine);

        // 3. Table Rows
        foreach (var row in data.Rows)
        {
            var rowLine = string.Join(",", row.Select(col => $"\"{Escape(col)}\""));
            sb.AppendLine(rowLine);
        }

        // 4. Totals Summary
        if (data.Totals.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("\"--- RESUMEN DE TOTALES ---\"");
            foreach (var t in data.Totals)
            {
                sb.AppendLine($"\"{Escape(t.Label)}\",\"{Escape(t.Value)}\"");
            }
        }

        // Return with UTF-8 BOM
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);
        return result;
    }

    private static string Escape(string? val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }
}
