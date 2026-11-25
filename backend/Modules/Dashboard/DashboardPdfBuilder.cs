using System.Globalization;
using System.Text;

namespace TecNM.Api.Modules.Dashboard;

/// <summary>
/// Pure C# PDF 1.4 document builder for exporting the official Cash Register Dashboard Cut.
/// Generates a standards-compliant, downloadable PDF file without external dependencies.
/// </summary>
public static class DashboardPdfBuilder
{
    /// <summary>
    /// Builds an executive PDF report byte array from the role-scoped dashboard indicators.
    /// </summary>
    public static byte[] BuildCortePdf(DashboardIndicadoresDTO data)
    {
        var lines = new List<(string Text, bool Bold, int Size)>
        {
            ("INSTITUTO TECNOLOGICO SUPERIOR DE MONCLOVA (TecNM)", true, 13),
            ("CORTE EJECUTIVO DE CAJA E INDICADORES FINANCIEROS", true, 11),
            ($"Usuario: {Sanitize(data.UsuarioActual)}  |  Rol: {Sanitize(data.RolActual)}  |  Alcance: {Sanitize(data.ScopeLabel)}", false, 9),
            ($"Periodo: {Sanitize(data.Periodo.ToUpperInvariant())}  |  Fecha Referencia: {data.FechaReferencia}  |  Generado: {DateTime.Now:yyyy-MM-dd HH:mm}", false, 9),
            (new string('-', 95), false, 8),
            ("1. RESUMEN DE INDICADORES PRINCIPALES", true, 10),
            ($"   Recaudacion Total:      ${data.RecaudacionTotal:N2} MXN   (Meta: ${data.MetaObjetivo:N2})", false, 9),
            ($"   Recibos Emitidos:       {data.TotalRecibosEmitidos}   |   Ultimo Folio: #{data.UltimoFolio}   |   Promedio: ${data.PromedioPorRecibo:N2}", false, 9),
            ($"   Alumnos Atendidos:      {data.TotalAlumnosAtendidos}   |   Al Corriente: {data.PorcentajeAlCorriente}%   |   Con Adeudo: {data.PorcentajeConAdeudo}%", false, 9),
            ($"   Adeudos Recuperados:    ${data.AdeudosRecuperadosMonto:N2} MXN   ({data.AlumnosAdeudoRecuperado} alumnos regularizados)", false, 9),
            ("", false, 6),
            ("2. DESGLOSE POR METODO DE PAGO", true, 10)
        };

        foreach (var m in data.MetodosPago)
        {
            lines.Add(($"   - {Sanitize(m.Nombre),-30} ${m.Monto,12:N2} MXN   ({m.Porcentaje}%)", false, 9));
        }

        lines.Add(("", false, 6));
        lines.Add(("3. PRINCIPALES CONCEPTOS DE INGRESO", true, 10));
        foreach (var c in data.ConceptosTop)
        {
            var nombre = Sanitize(c.Nombre);
            if (nombre.Length > 42) nombre = nombre[..42] + "...";
            lines.Add(($"   - {nombre,-46} ${c.Monto,12:N2} MXN   ({c.Porcentaje}%)", false, 9));
        }

        lines.Add(("", false, 6));
        lines.Add(("4. ULTIMOS RECIBOS EMITIDOS EN EL ALCANCE", true, 10));
        lines.Add(($"   {"FOLIO",-10} {"MATRICULA",-12} {"ALUMNO",-32} {"METODO",-18} {"MONTO",12}  {"ESTADO",-10}", true, 8));

        foreach (var r in data.UltimosRecibos.Take(15))
        {
            var alumno = Sanitize(r.Alumno);
            if (alumno.Length > 30) alumno = alumno[..30];
            var metodo = Sanitize(r.MetodoPago);
            if (metodo.Length > 17) metodo = metodo[..17];
            lines.Add(($"   {Sanitize(r.Folio),-10} {Sanitize(r.Matricula),-12} {alumno,-32} {metodo,-18} ${r.Monto,11:N2}  {Sanitize(r.Estado),-10}", false, 8));
        }

        lines.Add(("", false, 10));
        lines.Add((new string('-', 95), false, 8));
        lines.Add(("Documento generado automaticamente por el modulo Dashboard C# (.NET 10) - TecNM Monclova", false, 8));

        return RenderSimplePdf(lines);
    }

    private static byte[] RenderSimplePdf(List<(string Text, bool Bold, int Size)> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        var y = 760;

        foreach (var (text, bold, size) in lines)
        {
            if (y < 45) break;
            var font = bold ? "/F2" : "/F1";
            content.AppendLine($"{font} {size} Tf");
            content.AppendLine($"1 0 0 1 36 {y} Tm");
            content.AppendLine($"({EscapePdfText(text)}) Tj");
            y -= size + 5;
        }

        content.AppendLine("ET");
        var streamBytes = Encoding.ASCII.GetBytes(content.ToString());

        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold >>\nendobj\n",
            $"6 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n{content}endstream\nendobj\n"
        };

        var pdf = new MemoryStream();
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        pdf.Write(header);

        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(pdf.Position);
            var bytes = Encoding.ASCII.GetBytes(obj);
            pdf.Write(bytes);
        }

        var xrefPosition = pdf.Position;
        var xref = new StringBuilder();
        xref.AppendLine($"xref\n0 {objects.Count + 1}");
        xref.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++)
        {
            xref.AppendLine($"{offsets[i]:D10} 00000 n ");
        }

        xref.AppendLine("trailer");
        xref.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        xref.AppendLine("startxref");
        xref.AppendLine(xrefPosition.ToString(CultureInfo.InvariantCulture));
        xref.AppendLine("%%EOF");

        var xrefBytes = Encoding.ASCII.GetBytes(xref.ToString());
        pdf.Write(xrefBytes);
        return pdf.ToArray();
    }

    private static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c <= 126)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string EscapePdfText(string text)
        => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}

