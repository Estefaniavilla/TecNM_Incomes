namespace TecNM.Api.Modules.Reportes;

/// <summary>
/// Supported document export formats for financial and operational reports.
/// </summary>
public static class ReportExportFormat
{
    public const string Pdf = "pdf";
    public const string Csv = "csv";
    public const string Word = "word";
}

/// <summary>
/// Binary result of an exported report document.
/// </summary>
public sealed record ExportResultDTO(
    byte[] FileBytes,
    string ContentType,
    string FileName);

/// <summary>
/// Query filters for Revenue Policy (Póliza de Ingresos) export.
/// </summary>
public sealed record ReportePolizaFilterDTO(
    string? FechaInicio,
    string? FechaFin,
    string? FormaPago,
    string? CuentaContable,
    string? Modo = "cuentas", // "cuentas" (resumen contable) o "detalle" (movimientos)
    string Formato = ReportExportFormat.Pdf);

/// <summary>
/// Query filters for Student Debtors Report (Reporte de Deudores).
/// </summary>
public sealed record ReporteDeudoresFilterDTO(
    string? Periodo,
    int? CarreraId,
    string? FechaCorte,
    bool SoloInscritos = false,
    string Formato = ReportExportFormat.Pdf);

/// <summary>
/// Query filters for Income Report (Reporte de Ingresos).
/// </summary>
public sealed record ReporteIngresosFilterDTO(
    string? FechaInicio,
    string? FechaFin,
    string? Categoria,
    string? Cajero,
    string Formato = ReportExportFormat.Pdf);

/// <summary>
/// Standard tabular row model for generic report building.
/// </summary>
public sealed record ReportRowDTO(
    List<string> Values);

/// <summary>
/// Standard report data model passed to PDF, CSV, and Word exporters.
/// </summary>
public sealed record ReportDocumentData(
    string Title,
    string Subtitle,
    List<string> Metadata,
    List<string> Headers,
    List<List<string>> Rows,
    List<(string Label, string Value)> Totals,
    string? ElaboradoPor = null,
    string? RevisadoPor = null);
