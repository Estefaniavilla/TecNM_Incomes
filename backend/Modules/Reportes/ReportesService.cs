using Microsoft.EntityFrameworkCore;
using TecNM.Api.Data;
using TecNM.Api.Modules.Reportes.Exporters;

namespace TecNM.Api.Modules.Reportes;

/// <summary>
/// Domain service orchestrating the generation and multi-format export (PDF, CSV, Word)
/// of financial policies, debtor rosters, income statements, and official cash receipts.
/// </summary>
public sealed class ReportesService(
    TecNMDbContext dbContext,
    ILogger<ReportesService> logger)
{
    /// <summary>
    /// Exports Revenue Policy (Póliza de Ingresos) in either Accounting Account summary or Movement Detail mode.
    /// </summary>
    public async Task<ExportResultDTO> ExportarPolizaAsync(
        ReportePolizaFilterDTO filter,
        CancellationToken cancellationToken = default)
    {
        var fechaInicio = ParseDate(filter.FechaInicio) ?? DateTimeOffset.UtcNow.AddDays(-30);
        var fechaFin = ParseDate(filter.FechaFin, isEndOfDay: true) ?? DateTimeOffset.UtcNow;

        var query = dbContext.DetallesRecibo
            .AsNoTracking()
            .Include(d => d.Recibo)
            .Include(d => d.Concepto)
            .Where(d => d.Recibo != null &&
                        d.Recibo.FechaOperacion >= fechaInicio &&
                        d.Recibo.FechaOperacion <= fechaFin &&
                        d.Recibo.Estado != "CANCELADO");

        if (!string.IsNullOrWhiteSpace(filter.FormaPago))
        {
            var fp = filter.FormaPago.Trim().ToUpperInvariant();
            query = query.Where(d => d.Recibo!.MetodoPago.ToUpper().Contains(fp));
        }

        if (!string.IsNullOrWhiteSpace(filter.CuentaContable))
        {
            var cta = filter.CuentaContable.Trim();
            query = query.Where(d => d.Concepto != null && d.Concepto.Clave.Contains(cta));
        }

        var detalles = await query.ToListAsync(cancellationToken);

        var totalCaja = detalles
            .Where(d => d.Recibo != null && d.Recibo.MetodoPago.ToUpper().Contains("EFECTIVO"))
            .Sum(d => d.ImporteTotal);

        var totalBanco = detalles
            .Where(d => d.Recibo != null && !d.Recibo.MetodoPago.ToUpper().Contains("EFECTIVO"))
            .Sum(d => d.ImporteTotal);

        var totalGeneral = detalles.Sum(d => d.ImporteTotal);

        ReportDocumentData docData;
        var periodoStr = $"Del {fechaInicio:yyyy-MM-dd} al {fechaFin:yyyy-MM-dd}";

        if (filter.Modo?.ToLowerInvariant() == "detalle")
        {
            // MODO DETALLE DE MOVIMIENTOS
            var headers = new List<string> { "FOLIO", "FECHA", "MATRICULA", "ALUMNO", "CONCEPTO", "METODO", "IMPORTE" };
            var rows = detalles
                .OrderBy(d => d.Recibo?.NumeroRecibo)
                .Select(d => new List<string>
                {
                    d.Recibo?.NumeroRecibo.ToString() ?? "-",
                    d.Recibo?.FechaOperacion.ToString("yyyy-MM-dd") ?? "-",
                    d.Recibo?.AlumnoMatricula ?? "-",
                    Truncate(d.Recibo?.AlumnoNombre ?? "-", 25),
                    Truncate(d.Descripcion, 28),
                    d.Recibo?.MetodoPago ?? "-",
                    $"${d.ImporteTotal:N2}"
                })
                .ToList();

            docData = new ReportDocumentData(
                Title: "Poliza de Ingresos Propios - Detalle de Movimientos",
                Subtitle: periodoStr,
                Metadata: new List<string>
                {
                    $"Filtro Forma de Pago: {filter.FormaPago ?? "TODAS"}",
                    $"Filtro Cuenta Contable: {filter.CuentaContable ?? "TODAS"}",
                    $"Total Movimientos: {rows.Count}"
                },
                Headers: headers,
                Rows: rows,
                Totals: new List<(string Label, string Value)>
                {
                    ("TOTAL COBRO EN CAJA (EFECTIVO)", $"${totalCaja:N2} MXN"),
                    ("TOTAL DEPOSITO BANCARIO", $"${totalBanco:N2} MXN"),
                    ("TOTAL POLIZA DE INGRESOS", $"${totalGeneral:N2} MXN")
                }
            );
        }
        else
        {
            // MODO RESUMEN POR CUENTAS CONTABLES
            var grouped = detalles
                .GroupBy(d => new
                {
                    Cuenta = d.Concepto != null && !string.IsNullOrWhiteSpace(d.Concepto.Clave) ? d.Concepto.Clave : "5200-001-002",
                    d.Descripcion
                })
                .Select(g => new
                {
                    g.Key.Cuenta,
                    g.Key.Descripcion,
                    Movimientos = g.Count(),
                    Total = g.Sum(x => x.ImporteTotal)
                })
                .OrderBy(g => g.Cuenta)
                .ThenBy(g => g.Descripcion)
                .ToList();

            var headers = new List<string> { "CUENTA CONTABLE", "CONCEPTO DE INGRESO", "MOVIMIENTOS", "IMPORTE TOTAL" };
            var rows = grouped.Select(g => new List<string>
            {
                g.Cuenta,
                Truncate(g.Descripcion, 45),
                g.Movimientos.ToString(),
                $"${g.Total:N2}"
            }).ToList();

            docData = new ReportDocumentData(
                Title: "Poliza Unica de Ingresos Propios - Resumen Contable",
                Subtitle: periodoStr,
                Metadata: new List<string>
                {
                    $"Criterio de Periodo: {periodoStr}",
                    $"Cuentas Registradas: {rows.Count}",
                    $"Forma de Pago: {filter.FormaPago ?? "TODAS"}"
                },
                Headers: headers,
                Rows: rows,
                Totals: new List<(string Label, string Value)>
                {
                    ("TOTAL COBRO EN CAJA (EFECTIVO)", $"${totalCaja:N2} MXN"),
                    ("TOTAL DEPOSITO BANCARIO", $"${totalBanco:N2} MXN"),
                    ("TOTAL POLIZA DE INGRESOS", $"${totalGeneral:N2} MXN")
                }
            );
        }

        return GenerateFileResult(docData, filter.Formato, "Poliza_Ingresos");
    }

    /// <summary>
    /// Exports Debtors Roster (Reporte de Deudores) filtered by academic term and status.
    /// </summary>
    public async Task<ExportResultDTO> ExportarDeudoresAsync(
        ReporteDeudoresFilterDTO filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Adeudos
            .AsNoTracking()
            .Include(a => a.Alumno)
            .Include(a => a.Periodo)
            .Where(a => a.SaldoPendiente > 0 && a.Estado != "PAGADO" && a.Estado != "CANCELADO");

        if (!string.IsNullOrWhiteSpace(filter.Periodo))
        {
            query = query.Where(a => a.Periodo != null && a.Periodo.Codigo.Contains(filter.Periodo));
        }

        if (filter.CarreraId.HasValue && filter.CarreraId.Value > 0)
        {
            query = query.Where(a => a.Alumno != null && a.Alumno.IdCarrera == filter.CarreraId.Value);
        }

        if (filter.SoloInscritos)
        {
            query = query.Where(a => a.Alumno != null && a.Alumno.Estatus == "Inscrito");
        }

        if (!string.IsNullOrWhiteSpace(filter.FechaCorte) && DateOnly.TryParse(filter.FechaCorte, out var fechaCorte))
        {
            query = query.Where(a => a.FechaLimite == null || a.FechaLimite <= fechaCorte);
        }

        var adeudos = await query
            .OrderBy(a => a.Matricula)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var headers = new List<string> { "MATRICULA", "ALUMNO", "CARRERA", "SEM", "CONCEPTO ADEUDO", "SALDO PENDIENTE", "ESTADO" };
        var rows = adeudos.Select(a => new List<string>
        {
            a.Matricula,
            Truncate(a.Alumno?.NombreCompleto ?? "ALUMNO SIN ASIGNAR", 26),
            Truncate(a.Alumno?.Carrera ?? "N/D", 16),
            a.Alumno?.Semestre.ToString() ?? "1",
            Truncate(a.ConceptoCobro, 24),
            $"${a.SaldoPendiente:N2}",
            a.Estado
        }).ToList();

        var totalDeuda = adeudos.Sum(a => a.SaldoPendiente);

        var docData = new ReportDocumentData(
            Title: "Reporte Institucional de Deudores y Cartera Vencida",
            Subtitle: $"Periodo: {filter.Periodo ?? "Vigente"}  |  Fecha de Corte: {filter.FechaCorte ?? DateTime.Now.ToString("yyyy-MM-dd")}",
            Metadata: new List<string>
            {
                $"Total Alumnos con Adeudo: {adeudos.Select(a => a.Matricula).Distinct().Count()}",
                $"Total Partidas Vencidas: {adeudos.Count}",
                $"Filtro Solo Inscritos: {(filter.SoloInscritos ? "SI" : "NO")}"
            },
            Headers: headers,
            Rows: rows,
            Totals: new List<(string Label, string Value)>
            {
                ("TOTAL CARTERA PENDIENTE DE COBRO", $"${totalDeuda:N2} MXN")
            }
        );

        return GenerateFileResult(docData, filter.Formato, "Reporte_Deudores");
    }

    /// <summary>
    /// Exports Income Activity Report (Reporte de Ingresos).
    /// </summary>
    public async Task<ExportResultDTO> ExportarIngresosAsync(
        ReporteIngresosFilterDTO filter,
        CancellationToken cancellationToken = default)
    {
        var fechaInicio = ParseDate(filter.FechaInicio) ?? DateTimeOffset.UtcNow.AddDays(-7);
        var fechaFin = ParseDate(filter.FechaFin, isEndOfDay: true) ?? DateTimeOffset.UtcNow;

        var query = dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .Where(r => r.FechaOperacion >= fechaInicio &&
                        r.FechaOperacion <= fechaFin &&
                        r.Estado != "CANCELADO");

        if (!string.IsNullOrWhiteSpace(filter.Cajero))
        {
            query = query.Where(r => r.CajeroUsuario.Contains(filter.Cajero));
        }

        var recibos = await query
            .OrderBy(r => r.NumeroRecibo)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var headers = new List<string> { "FOLIO", "FECHA", "MATRICULA", "ALUMNO", "METODO", "CAJERO", "TOTAL" };
        var rows = recibos.Select(r => new List<string>
        {
            r.NumeroRecibo.ToString(),
            r.FechaOperacion.ToString("yyyy-MM-dd HH:mm"),
            r.AlumnoMatricula,
            Truncate(r.AlumnoNombre, 28),
            r.MetodoPago,
            r.CajeroUsuario,
            $"${r.Total:N2}"
        }).ToList();

        var totalRecaudado = recibos.Sum(r => r.Total);

        var docData = new ReportDocumentData(
            Title: "Reporte Concentrado de Ingresos en Caja",
            Subtitle: $"Del {fechaInicio:yyyy-MM-dd} al {fechaFin:yyyy-MM-dd}",
            Metadata: new List<string>
            {
                $"Total de Recibos Emitidos: {recibos.Count}",
                $"Cajero: {filter.Cajero ?? "TODOS"}"
            },
            Headers: headers,
            Rows: rows,
            Totals: new List<(string Label, string Value)>
            {
                ("TOTAL GENERAL DE INGRESOS", $"${totalRecaudado:N2} MXN")
            }
        );

        return GenerateFileResult(docData, filter.Formato, "Reporte_Ingresos");
    }

    /// <summary>
    /// Exports Official Cash Receipt (Recibo Oficial) in PDF, CSV, or Word format.
    /// </summary>
    public async Task<ExportResultDTO> ExportarReciboAsync(
        long numeroRecibo,
        string formato = ReportExportFormat.Pdf,
        CancellationToken cancellationToken = default)
    {
        var recibo = await dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .FirstOrDefaultAsync(r => r.NumeroRecibo == numeroRecibo, cancellationToken);

        if (recibo == null)
        {
            throw new KeyNotFoundException($"El recibo #{numeroRecibo} no fue encontrado.");
        }

        var headers = new List<string> { "CANT.", "DESCRIPCION DEL CONCEPTO", "CUOTA UNITARIA", "DESCUENTO", "IMPORTE" };
        var rows = recibo.Detalles.Select(d => new List<string>
        {
            d.Cantidad.ToString(),
            Truncate(d.Descripcion, 45),
            $"${d.CuotaUnitario:N2}",
            $"${d.DescuentoBeca:N2}",
            $"${d.ImporteTotal:N2}"
        }).ToList();

        var docData = new ReportDocumentData(
            Title: $"RECIBO OFICIAL DE COBRO #{recibo.NumeroRecibo}",
            Subtitle: $"Fecha: {recibo.FechaOperacion:yyyy-MM-dd HH:mm:ss}  |  Estado: {recibo.Estado}",
            Metadata: new List<string>
            {
                $"Matrícula: {recibo.AlumnoMatricula}  |  Estudiante: {recibo.AlumnoNombre}",
                $"Carrera: {recibo.AlumnoCarrera}  |  Semestre: {recibo.AlumnoSemestre}  |  Turno: {recibo.AlumnoTurno}",
                $"Método de Pago: {recibo.MetodoPago}  |  Cajero: {recibo.CajeroNombre} ({recibo.CajeroUsuario})"
            },
            Headers: headers,
            Rows: rows,
            Totals: new List<(string Label, string Value)>
            {
                ("SUBTOTAL", $"${recibo.Subtotal:N2} MXN"),
                ("DESCUENTO / BECA", $"${recibo.DescuentoTotal:N2} MXN"),
                ("TOTAL COBRADO", $"${recibo.Total:N2} MXN")
            },
            ElaboradoPor: recibo.CajeroNombre,
            RevisadoPor: "Jefe de Recursos Financieros"
        );

        return GenerateFileResult(docData, formato, $"Recibo_{recibo.NumeroRecibo}");
    }

    private static ExportResultDTO GenerateFileResult(ReportDocumentData data, string format, string baseFileName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var normalizedFormat = format.Trim().ToLowerInvariant();

        return normalizedFormat switch
        {
            ReportExportFormat.Csv => new ExportResultDTO(
                FileBytes: CsvReportExporter.BuildCsv(data),
                ContentType: "text/csv; charset=utf-8",
                FileName: $"{baseFileName}_{timestamp}.csv"),

            ReportExportFormat.Word => new ExportResultDTO(
                FileBytes: WordReportExporter.BuildWordDocument(data),
                ContentType: "application/msword",
                FileName: $"{baseFileName}_{timestamp}.doc"),

            _ => new ExportResultDTO(
                FileBytes: PdfReportExporter.BuildPdf(data),
                ContentType: "application/pdf",
                FileName: $"{baseFileName}_{timestamp}.pdf")
        };
    }

    private static DateTimeOffset? ParseDate(string? dateStr, bool isEndOfDay = false)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        if (DateTime.TryParse(dateStr, out var dt))
        {
            if (isEndOfDay)
            {
                dt = dt.Date.AddDays(1).AddTicks(-1);
            }
            return new DateTimeOffset(dt, TimeSpan.Zero);
        }
        return null;
    }

    private static string Truncate(string val, int maxLen)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Length <= maxLen ? val : val[..maxLen] + "...";
    }
}
