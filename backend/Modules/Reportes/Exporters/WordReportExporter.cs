using System.Text;

namespace TecNM.Api.Modules.Reportes.Exporters;

/// <summary>
/// Microsoft Word document exporter generating institutional-grade .doc reports
/// using Office-compatible HTML/XML styling, embedded typography, tables, and signature blocks.
/// </summary>
public static class WordReportExporter
{
    public static byte[] BuildWordDocument(ReportDocumentData data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(data.Title)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            @page {
                size: letter portrait;
                margin: 2cm 1.5cm 2cm 1.5cm;
                mso-page-orientation: portrait;
            }
            body {
                font-family: 'Segoe UI', Calibri, Arial, sans-serif;
                font-size: 10pt;
                color: #1e293b;
                line-height: 1.35;
            }
            .header-table {
                width: 100%;
                border-bottom: 3px solid #1B396A;
                margin-bottom: 18px;
                padding-bottom: 8px;
            }
            .inst-title {
                font-size: 13pt;
                font-weight: bold;
                color: #1B396A;
                text-transform: uppercase;
                margin: 0;
            }
            .inst-subtitle {
                font-size: 9pt;
                color: #64748b;
                margin: 2px 0 0 0;
            }
            .doc-title {
                font-size: 12pt;
                font-weight: bold;
                color: #0f172a;
                margin: 10px 0 4px 0;
                text-transform: uppercase;
            }
            .doc-subtitle {
                font-size: 9.5pt;
                color: #475569;
                margin-bottom: 12px;
            }
            .meta-box {
                background-color: #f8fafc;
                border: 1px solid #e2e8f0;
                padding: 8px 12px;
                font-size: 8.5pt;
                margin-bottom: 16px;
                border-radius: 4px;
            }
            .data-table {
                width: 100%;
                border-collapse: collapse;
                margin-bottom: 20px;
                font-size: 8.5pt;
            }
            .data-table th {
                background-color: #1B396A;
                color: #ffffff;
                font-weight: 600;
                text-align: left;
                padding: 6px 8px;
                border: 1px solid #1B396A;
            }
            .data-table td {
                padding: 5px 8px;
                border: 1px solid #cbd5e1;
            }
            .data-table tr:nth-child(even) td {
                background-color: #f8fafc;
            }
            .text-right {
                text-align: right;
            }
            .text-center {
                text-align: center;
            }
            .font-mono {
                font-family: 'Consolas', 'Courier New', monospace;
            }
            .totals-table {
                width: 320px;
                margin-left: auto;
                border-collapse: collapse;
                margin-bottom: 30px;
                font-size: 9pt;
            }
            .totals-table td {
                padding: 4px 8px;
                border: 1px solid #cbd5e1;
            }
            .totals-grand {
                font-weight: bold;
                background-color: #eff6ff;
                color: #1B396A;
            }
            .signatures-table {
                width: 100%;
                margin-top: 40px;
                border: none;
            }
            .signatures-table td {
                width: 50%;
                text-align: center;
                border: none;
                vertical-align: top;
                padding: 0 30px;
            }
            .signature-line {
                border-top: 1px solid #475569;
                margin-bottom: 6px;
                padding-top: 4px;
                font-size: 9pt;
                font-weight: bold;
                color: #1e293b;
            }
            .signature-role {
                font-size: 8pt;
                color: #64748b;
            }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Institutional Header
        sb.AppendLine("<table class='header-table'>");
        sb.AppendLine("  <tr>");
        sb.AppendLine("    <td style='border:none;'>");
        sb.AppendLine("      <div class='inst-title'>Instituto Tecnológico Superior de Monclova (TecNM)</div>");
        sb.AppendLine("      <div class='inst-subtitle'>Departamento de Recursos Financieros y Control de Ingresos</div>");
        sb.AppendLine($"      <div class='doc-title'>{System.Net.WebUtility.HtmlEncode(data.Title)}</div>");
        if (!string.IsNullOrWhiteSpace(data.Subtitle))
        {
            sb.AppendLine($"      <div class='doc-subtitle'>{System.Net.WebUtility.HtmlEncode(data.Subtitle)}</div>");
        }
        sb.AppendLine("    </td>");
        sb.AppendLine("  </tr>");
        sb.AppendLine("</table>");

        // Metadata box
        if (data.Metadata.Count > 0)
        {
            sb.AppendLine("<div class='meta-box'>");
            foreach (var meta in data.Metadata)
            {
                sb.AppendLine($"<div>{System.Net.WebUtility.HtmlEncode(meta)}</div>");
            }
            sb.AppendLine("</div>");
        }

        // Table Content
        sb.AppendLine("<table class='data-table'>");
        sb.AppendLine("  <thead><tr>");
        foreach (var h in data.Headers)
        {
            var alignClass = h.Contains("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                             h.Contains("IMPORTE", StringComparison.OrdinalIgnoreCase) ||
                             h.Contains("MONTO", StringComparison.OrdinalIgnoreCase) ||
                             h.Contains("SALDO", StringComparison.OrdinalIgnoreCase)
                ? "text-right"
                : "";
            sb.AppendLine($"    <th class='{alignClass}'>{System.Net.WebUtility.HtmlEncode(h)}</th>");
        }
        sb.AppendLine("  </tr></thead>");
        sb.AppendLine("  <tbody>");

        foreach (var row in data.Rows)
        {
            sb.AppendLine("    <tr>");
            for (int i = 0; i < row.Count; i++)
            {
                var val = row[i];
                var header = i < data.Headers.Count ? data.Headers[i] : "";
                var isMoney = header.Contains("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                              header.Contains("IMPORTE", StringComparison.OrdinalIgnoreCase) ||
                              header.Contains("MONTO", StringComparison.OrdinalIgnoreCase) ||
                              header.Contains("SALDO", StringComparison.OrdinalIgnoreCase) ||
                              val.StartsWith('$');
                var alignClass = isMoney ? "text-right font-mono" : "";
                sb.AppendLine($"      <td class='{alignClass}'>{System.Net.WebUtility.HtmlEncode(val)}</td>");
            }
            sb.AppendLine("    </tr>");
        }
        sb.AppendLine("  </tbody>");
        sb.AppendLine("</table>");

        // Totals
        if (data.Totals.Count > 0)
        {
            sb.AppendLine("<table class='totals-table'>");
            for (int i = 0; i < data.Totals.Count; i++)
            {
                var t = data.Totals[i];
                var isLast = i == data.Totals.Count - 1;
                var cls = isLast ? "class='totals-grand'" : "";
                sb.AppendLine($"  <tr {cls}>");
                sb.AppendLine($"    <td><strong>{System.Net.WebUtility.HtmlEncode(t.Label)}</strong></td>");
                sb.AppendLine($"    <td class='text-right font-mono'>{System.Net.WebUtility.HtmlEncode(t.Value)}</td>");
                sb.AppendLine("  </tr>");
            }
            sb.AppendLine("</table>");
        }

        // Signatures
        var elaboro = data.ElaboradoPor ?? "Cajero en Turno";
        var reviso = data.RevisadoPor ?? "Jefe de Departamento de Recursos Financieros";
        sb.AppendLine("<table class='signatures-table'>");
        sb.AppendLine("  <tr>");
        sb.AppendLine("    <td>");
        sb.AppendLine($"      <div class='signature-line'>{System.Net.WebUtility.HtmlEncode(elaboro)}</div>");
        sb.AppendLine("      <div class='signature-role'>ELABORÓ</div>");
        sb.AppendLine("    </td>");
        sb.AppendLine("    <td>");
        sb.AppendLine($"      <div class='signature-line'>{System.Net.WebUtility.HtmlEncode(reviso)}</div>");
        sb.AppendLine("      <div class='signature-role'>REVISÓ Y AUTORIZÓ</div>");
        sb.AppendLine("    </td>");
        sb.AppendLine("  </tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
