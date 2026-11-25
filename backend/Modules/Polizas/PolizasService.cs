using Microsoft.EntityFrameworkCore;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Polizas;

/// <summary>
/// Domain service encapsulating daily and period-based revenue policy calculation,
/// accounting journal aggregations, and detailed audit reports.
/// </summary>
public sealed class PolizasService(
    TecNMDbContext dbContext,
    ILogger<PolizasService> logger)
{
    /// <summary>
    /// Calculates daily revenue policy grouped by accounting account code and concept.
    /// Excludes cancelled receipts to ensure accurate financial closing.
    /// </summary>
    public async Task<PolizaIngresoResumenDTO> GetPolizaDiariaResumenAsync(
        string? fechaStr,
        CancellationToken cancellationToken = default)
    {
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(fechaStr) && DateOnly.TryParse(fechaStr, out var parsed))
        {
            fecha = parsed;
        }

        var inicio = new DateTimeOffset(fecha.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fin = new DateTimeOffset(fecha.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        try
        {
            var detalles = await dbContext.DetallesRecibo
                .AsNoTracking()
                .Include(d => d.Recibo)
                .Include(d => d.Concepto)
                .Where(d => d.Recibo != null
                    && d.Recibo.FechaOperacion >= inicio
                    && d.Recibo.FechaOperacion <= fin
                    && d.Recibo.Estado != "CANCELADO")
                .ToListAsync(cancellationToken);

            var agrupado = detalles
                .GroupBy(d => new
                {
                    Cuenta = d.Concepto != null && !string.IsNullOrWhiteSpace(d.Concepto.Clave)
                        ? d.Concepto.Clave
                        : "5200-001-002",
                    Concepto = d.Descripcion
                })
                .Select(g => new PolizaIngresoFilaDTO(
                    Cuenta: g.Key.Cuenta,
                    Concepto: g.Key.Concepto,
                    Cantidad: g.Sum(x => x.Cantidad),
                    Total: g.Sum(x => x.ImporteTotal)))
                .OrderBy(f => f.Cuenta)
                .ThenBy(f => f.Concepto)
                .ToList();

            var totalGeneral = agrupado.Sum(f => f.Total);
            var totalRecibos = detalles.Select(d => d.ReciboId).Distinct().Count();

            return new PolizaIngresoResumenDTO(
                Fecha: fecha.ToString("yyyy-MM-dd"),
                TotalGeneral: totalGeneral,
                Filas: agrupado,
                TotalRecibos: totalRecibos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating daily revenue policy for date '{Date}'", fecha);
            throw;
        }
    }

    /// <summary>
    /// Calculates period revenue policy for a date range (start date to end date).
    /// Provides both account-level summary and transaction breakdown.
    /// Powers ModalPolizaIngresoPeriodo.vue.
    /// </summary>
    public async Task<PolizaPeriodoResumenDTO> GetPolizaPeriodoResumenAsync(
        string? fechaInicioStr,
        string? fechaFinStr,
        string? cuentaFiltro,
        CancellationToken cancellationToken = default)
    {
        var fechaInicio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        if (!string.IsNullOrWhiteSpace(fechaInicioStr) && DateOnly.TryParse(fechaInicioStr, out var pIni))
        {
            fechaInicio = pIni;
        }

        var fechaFin = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(fechaFinStr) && DateOnly.TryParse(fechaFinStr, out var pFin))
        {
            fechaFin = pFin;
        }

        var inicio = new DateTimeOffset(fechaInicio.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fin = new DateTimeOffset(fechaFin.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        try
        {
            var query = dbContext.DetallesRecibo
                .AsNoTracking()
                .Include(d => d.Recibo)
                .Include(d => d.Concepto)
                .Where(d => d.Recibo != null
                    && d.Recibo.FechaOperacion >= inicio
                    && d.Recibo.FechaOperacion <= fin
                    && d.Recibo.Estado != "CANCELADO");

            if (!string.IsNullOrWhiteSpace(cuentaFiltro))
            {
                var c = cuentaFiltro.Trim();
                query = query.Where(d => d.Concepto != null && d.Concepto.Clave == c);
            }

            var detalles = await query.ToListAsync(cancellationToken);

            // Grouping by account
            var agrupado = detalles
                .GroupBy(d => new
                {
                    Cuenta = d.Concepto != null && !string.IsNullOrWhiteSpace(d.Concepto.Clave)
                        ? d.Concepto.Clave
                        : "5200-001-002",
                    Concepto = d.Descripcion
                })
                .Select(g => new PolizaIngresoFilaDTO(
                    Cuenta: g.Key.Cuenta,
                    Concepto: g.Key.Concepto,
                    Cantidad: g.Sum(x => x.Cantidad),
                    Total: g.Sum(x => x.ImporteTotal)))
                .OrderBy(f => f.Cuenta)
                .ThenBy(f => f.Concepto)
                .ToList();

            // Transaction detail list
            var transacciones = detalles
                .OrderBy(d => d.Recibo!.FechaOperacion)
                .Select(d => new PolizaPeriodoFilaDTO(
                    Fecha: DateOnly.FromDateTime(d.Recibo!.FechaOperacion.LocalDateTime),
                    Folio: d.Recibo!.FolioAdministrativo,
                    Cuenta: d.Concepto != null ? d.Concepto.Clave : "5200-001-002",
                    Concepto: d.Descripcion,
                    Importe: d.ImporteTotal,
                    MetodoPago: d.Recibo!.MetodoPago,
                    Cajero: d.Recibo!.CajeroUsuario))
                .ToList();

            var totalGeneral = agrupado.Sum(f => f.Total);
            var totalRecibos = detalles.Select(d => d.ReciboId).Distinct().Count();

            return new PolizaPeriodoResumenDTO(
                FechaInicio: fechaInicio.ToString("yyyy-MM-dd"),
                FechaFin: fechaFin.ToString("yyyy-MM-dd"),
                TotalGeneral: totalGeneral,
                TotalRecibos: totalRecibos,
                AgrupadoPorCuenta: agrupado,
                DetalleTransacciones: transacciones);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating period revenue policy between '{Start}' and '{End}'", fechaInicio, fechaFin);
            throw;
        }
    }

    /// <summary>
    /// Retrieves detailed line items across receipts for the official printable ledger.
    /// Supports optional filtering by date range, payment method, account/concept, and free-text query.
    /// Matches the endpoint consumed by ModalPolizaIngresos.vue (/caja/reportes/detallado).
    /// </summary>
    public async Task<List<ReporteReciboDetalladoDTO>> GetMovimientosPolizaDetalladoAsync(
        string? inicioStr = null,
        string? finStr = null,
        string? metodoPago = null,
        string? q = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.Recibos
                .AsNoTracking()
                .Include(r => r.Detalles)
                .ThenInclude(d => d.Concepto)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(inicioStr) && DateOnly.TryParse(inicioStr, out var pIni))
            {
                var iniOffset = new DateTimeOffset(pIni.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                query = query.Where(r => r.FechaOperacion >= iniOffset);
            }

            if (!string.IsNullOrWhiteSpace(finStr) && DateOnly.TryParse(finStr, out var pFin))
            {
                var finOffset = new DateTimeOffset(pFin.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
                query = query.Where(r => r.FechaOperacion <= finOffset);
            }

            if (!string.IsNullOrWhiteSpace(metodoPago) && !metodoPago.Equals("TODAS", StringComparison.OrdinalIgnoreCase))
            {
                var mp = metodoPago.Trim().ToUpperInvariant();
                query = query.Where(r => r.MetodoPago.ToUpper().Contains(mp));
            }

            var recibos = await query
                .OrderByDescending(r => r.FechaOperacion)
                .ToListAsync(cancellationToken);

            var searchLower = q?.Trim().ToLowerInvariant();
            var resultado = new List<ReporteReciboDetalladoDTO>();

            foreach (var r in recibos)
            {
                foreach (var d in r.Detalles)
                {
                    if (!string.IsNullOrWhiteSpace(searchLower))
                    {
                        var clave = d.Concepto?.Clave?.ToLowerInvariant() ?? string.Empty;
                        var desc = d.Descripcion.ToLowerInvariant();
                        var nombre = r.AlumnoNombre.ToLowerInvariant();
                        var mat = r.AlumnoMatricula.ToLowerInvariant();
                        var folio = r.FolioAdministrativo.ToLowerInvariant();

                        if (!clave.Contains(searchLower)
                            && !desc.Contains(searchLower)
                            && !nombre.Contains(searchLower)
                            && !mat.Contains(searchLower)
                            && !folio.Contains(searchLower))
                        {
                            continue;
                        }
                    }

                    resultado.Add(new ReporteReciboDetalladoDTO(
                        ReciboId: r.Id,
                        NumeroRecibo: r.NumeroRecibo,
                        FolioAdministrativo: r.FolioAdministrativo,
                        FechaOperacion: r.FechaOperacion,
                        AlumnoMatricula: r.AlumnoMatricula,
                        AlumnoNombre: r.AlumnoNombre,
                        AlumnoCarrera: r.AlumnoCarrera,
                        AlumnoSemestre: r.AlumnoSemestre,
                        AlumnoTurno: r.AlumnoTurno,
                        ConceptoDetalle: d.Descripcion,
                        Cantidad: d.Cantidad,
                        CuotaUnitario: d.CuotaUnitario,
                        DescuentoBeca: d.DescuentoBeca,
                        ImporteRenglon: d.ImporteTotal,
                        TotalRecibo: r.Total,
                        MetodoPago: r.MetodoPago,
                        ReferenciaBancaria: r.ReferenciaBancaria,
                        EstadoRecibo: r.Estado,
                        CajeroUsuario: r.CajeroUsuario));
                }
            }

            return resultado;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving detailed policy ledger movements.");
            throw;
        }
    }

    /// <summary>
    /// Performs official daily closing of cash register operations and policy generation.
    /// </summary>
    public async Task<CierrePolizaResponseDTO> CerrarPolizaDiariaAsync(
        CierrePolizaRequestDTO request,
        string cashierUser,
        CancellationToken cancellationToken = default)
    {
        var resumen = await GetPolizaDiariaResumenAsync(request.Fecha.ToString("yyyy-MM-dd"), cancellationToken);
        var folioPoliza = $"POL-{request.Fecha:yyyyMMdd}-01";

        logger.LogInformation("Daily policy closed for '{Date}' with Folio '{Folio}', Total: {Total}, User: '{User}'",
            request.Fecha, folioPoliza, resumen.TotalGeneral, cashierUser);

        return new CierrePolizaResponseDTO(
            Id: Guid.NewGuid(),
            FolioPoliza: folioPoliza,
            Fecha: request.Fecha,
            Total: resumen.TotalGeneral,
            TotalRecibos: resumen.TotalRecibos,
            GeneradoPor: cashierUser,
            FechaCierre: DateTimeOffset.UtcNow);
    }
}
