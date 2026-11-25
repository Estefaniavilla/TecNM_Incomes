using System.Text;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Recibos;

/// <summary>
/// Domain service encapsulating receipt creation, consecutive numbering, accounting movements, and cancellation audits.
/// </summary>
public sealed class RecibosService(
    TecNMDbContext dbContext,
    ILogger<RecibosService> logger)
{
    /// <summary>
    /// Processes and issues an official receipt (either Definitive or Preview).
    /// </summary>
    public async Task<ReciboResponseDTO> CreateReciboAsync(
        CreateReciboRequest request,
        string cashierUser,
        string cashierName,
        bool isPreview,
        CancellationToken cancellationToken = default)
    {
        if (request.Detalles == null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("The receipt must contain at least one line item.");
        }

        var operationDate = request.FechaOperacion ?? DateTimeOffset.UtcNow;

        var subtotal = request.Detalles.Sum(d => d.Cantidad * d.CuotaUnitario);
        var discountTotal = request.Detalles.Sum(d => d.DescuentoBeca);
        var total = request.Detalles.Sum(d => d.ImporteTotal);

        var receiptNumber = await GenerateNextReceiptNumberAsync(cancellationToken);
        var administrativeFolio = $"FOL-{operationDate:yyyy-MM}-{receiptNumber}";

        var recibo = new Recibo
        {
            Id = Guid.NewGuid(),
            NumeroRecibo = receiptNumber,
            FolioAdministrativo = administrativeFolio,
            FechaOperacion = operationDate,
            FechaEfectivaPago = operationDate,
            AlumnoMatricula = request.Matricula.Trim(),
            AlumnoNombre = request.NombreCompleto.Trim().ToUpperInvariant(),
            AlumnoCarrera = request.Carrera.Trim(),
            AlumnoSemestre = request.Semestre,
            AlumnoTurno = request.Turno.Trim(),
            MetodoPago = request.MetodoPago.Trim().ToUpperInvariant(),
            ReferenciaBancaria = request.ReferenciaBancaria?.Trim(),
            CuentaBancaria = request.CuentaBancaria?.Trim(),
            FechaBancaria = request.FechaBancaria,
            Subtotal = subtotal,
            DescuentoTotal = discountTotal,
            Total = total,
            Status = 1,
            Estado = isPreview ? "PREVIO" : "REGISTRADO",
            TipoRecibo = isPreview ? "PREVIO" : "DEFINITIVO",
            CajeroUsuario = cashierUser,
            CajeroNombre = cashierName,
            MotivoCancelacion = !string.IsNullOrWhiteSpace(request.JustificacionFecha)
                ? $"[Justified Date: {request.JustificacionFecha}]"
                : null
        };

        foreach (var item in request.Detalles)
        {
            recibo.Detalles.Add(new DetalleRecibo
            {
                Id = Guid.NewGuid(),
                ConceptoId = item.ConceptoId,
                Descripcion = item.Descripcion.Trim(),
                Cantidad = item.Cantidad,
                CuotaUnitario = item.CuotaUnitario,
                DescuentoBeca = item.DescuentoBeca,
                ImporteTotal = item.ImporteTotal,
                PeriodoAno = item.PeriodoAno ?? $"{operationDate.Year}-1"
            });
        }

        // Add accounting payment movement
        recibo.Movimientos.Add(new MovimientoPago
        {
            FormaPago = recibo.MetodoPago,
            ReferenciaBancaria = recibo.ReferenciaBancaria ?? "0",
            Monto = recibo.Total,
            FechaMovimiento = operationDate,
            CajeroUsuario = cashierUser
        });

        dbContext.Recibos.Add(recibo);

        // Amortize matching pending student debts if definitive
        if (!isPreview)
        {
            foreach (var detail in request.Detalles)
            {
                var cleanDesc = detail.Descripcion.Trim().ToLower();
                var pendingDebt = await dbContext.Adeudos
                    .FirstOrDefaultAsync(a =>
                        a.Matricula == recibo.AlumnoMatricula &&
                        a.ConceptoCobro.ToLower().Contains(cleanDesc) &&
                        a.Estado != "LIQUIDADO",
                        cancellationToken);

                if (pendingDebt != null)
                {
                    pendingDebt.SaldoPendiente = Math.Max(0, pendingDebt.SaldoPendiente - detail.ImporteTotal);
                    pendingDebt.Estado = pendingDebt.SaldoPendiente <= 0 ? "LIQUIDADO" : "PARCIAL";
                    logger.LogInformation("Amortized debt {Id} for student {Matricula}: new balance {Balance}",
                        pendingDebt.IdAdeudo, pendingDebt.Matricula, pendingDebt.SaldoPendiente);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully issued receipt #{NumeroRecibo} (Folio: {Folio}) for {Matricula} total {Total:C}",
            recibo.NumeroRecibo, recibo.FolioAdministrativo, recibo.AlumnoMatricula, recibo.Total);

        return MapToDTO(recibo);
    }

    /// <summary>
    /// Cancels an issued receipt, recording audit user, timestamp, and mandatory justification.
    /// </summary>
    public async Task<ReciboResponseDTO> CancelReciboAsync(
        Guid id,
        string reason,
        string cashierUser,
        CancellationToken cancellationToken = default)
    {
        var cleanReason = reason.Trim();
        if (cleanReason.Length < 5)
        {
            throw new ArgumentException("Cancellation reason must contain at least 5 characters.");
        }

        var recibo = await dbContext.Recibos
            .Include(r => r.Detalles)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recibo is null)
        {
            throw new KeyNotFoundException($"Receipt with ID '{id}' was not found.");
        }

        if (recibo.Estado == "CANCELADO")
        {
            throw new InvalidOperationException($"Receipt #{recibo.NumeroRecibo} is already cancelled.");
        }

        recibo.Estado = "CANCELADO";
        recibo.Status = 0;
        recibo.MotivoCancelacion = $"{cleanReason} (Cancelled by: {cashierUser})";
        recibo.FechaCancelacion = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Receipt #{NumeroRecibo} (ID: {Id}) cancelled by {User}. Reason: {Reason}",
            recibo.NumeroRecibo, id, cashierUser, cleanReason);

        return MapToDTO(recibo);
    }

    /// <summary>
    /// Cancels an issued receipt by its consecutive receipt number.
    /// </summary>
    public async Task<ReciboResponseDTO> CancelReciboByNumeroAsync(
        long numero,
        string reason,
        string cashierUser,
        CancellationToken cancellationToken = default)
    {
        var cleanReason = reason.Trim();
        if (cleanReason.Length < 5)
        {
            throw new ArgumentException("Cancellation reason must contain at least 5 characters.");
        }

        var recibo = await dbContext.Recibos
            .Include(r => r.Detalles)
            .SingleOrDefaultAsync(r => r.NumeroRecibo == numero, cancellationToken);

        if (recibo is null)
        {
            throw new KeyNotFoundException($"Receipt #{numero} was not found.");
        }

        if (recibo.Estado == "CANCELADO")
        {
            throw new InvalidOperationException($"Receipt #{recibo.NumeroRecibo} is already cancelled.");
        }

        recibo.Estado = "CANCELADO";
        recibo.Status = 0;
        recibo.MotivoCancelacion = $"{cleanReason} (Cancelled by: {cashierUser})";
        recibo.FechaCancelacion = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Receipt #{NumeroRecibo} cancelled by {User}. Reason: {Reason}",
            recibo.NumeroRecibo, cashierUser, cleanReason);

        return MapToDTO(recibo);
    }

    /// <summary>
    /// Retrieves a single receipt by consecutive receipt number.
    /// </summary>
    public async Task<ReciboResponseDTO?> GetByNumeroAsync(
        long numero,
        CancellationToken cancellationToken = default)
    {
        var recibo = await dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .SingleOrDefaultAsync(r => r.NumeroRecibo == numero, cancellationToken);

        return recibo is not null ? MapToDTO(recibo) : null;
    }

    /// <summary>
    /// Retrieves a single receipt by unique UUID identifier.
    /// </summary>
    public async Task<ReciboResponseDTO?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var recibo = await dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        return recibo is not null ? MapToDTO(recibo) : null;
    }

    /// <summary>
    /// Retrieves the most recent N receipts, ordered by receipt number descending.
    /// </summary>
    public async Task<IReadOnlyList<ReciboResponseDTO>> GetRecentAsync(
        int limit = 15,
        CancellationToken cancellationToken = default)
    {
        var maxLimit = Math.Clamp(limit, 1, 50);

        return await dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .OrderByDescending(r => r.NumeroRecibo)
            .Take(maxLimit)
            .Select(r => MapToDTO(r))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated and filtered list of receipts for query and audit screens.
    /// </summary>
    public async Task<PagedResult<ReciboResponseDTO>> GetPagedAsync(
        ReciboFilterParams filters,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var pattern = filters.Search.Trim().ToLower();
            query = query.Where(r =>
                r.AlumnoMatricula.ToLower().Contains(pattern) ||
                r.AlumnoNombre.ToLower().Contains(pattern) ||
                r.FolioAdministrativo.ToLower().Contains(pattern) ||
                r.NumeroRecibo.ToString().Contains(pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.Estado))
        {
            var estado = filters.Estado.Trim();
            query = query.Where(r => r.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(filters.MetodoPago))
        {
            var metodo = filters.MetodoPago.Trim().ToUpperInvariant();
            query = query.Where(r => r.MetodoPago == metodo);
        }

        if (filters.FechaInicio.HasValue)
        {
            query = query.Where(r => r.FechaOperacion >= filters.FechaInicio.Value);
        }

        if (filters.FechaFin.HasValue)
        {
            query = query.Where(r => r.FechaOperacion <= filters.FechaFin.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderByDescending(r => r.NumeroRecibo);

        var skip = (filters.Page - 1) * filters.PageSize;
        var items = await query
            .Skip(skip)
            .Take(filters.PageSize)
            .Select(r => MapToDTO(r))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize);

        return new PagedResult<ReciboResponseDTO>(
            items,
            totalCount,
            filters.Page,
            filters.PageSize,
            totalPages);
    }

    /// <summary>
    /// Computes shift financial closing (corte de caja) aggregating active income and deducting cancelled receipts.
    /// Used for shift balancing and fiscal audits.
    /// </summary>
    public async Task<CorteTurnoResponseDTO> GetCorteTurnoAsync(
        DateOnly? fecha,
        string? cajeroUsuario,
        CancellationToken cancellationToken = default)
    {
        var targetDate = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var query = dbContext.Recibos
            .AsNoTracking()
            .Where(r => r.FechaOperacion >= startOfDay && r.FechaOperacion <= endOfDay);

        if (!string.IsNullOrWhiteSpace(cajeroUsuario))
        {
            query = query.Where(r => r.CajeroUsuario == cajeroUsuario);
        }

        var recibos = await query.ToListAsync(cancellationToken);

        var totalEmitidos = recibos.Count;
        var cancelados = recibos.Where(r => r.Estado == "CANCELADO").ToList();
        var activos = recibos.Where(r => r.Estado != "CANCELADO").ToList();

        var efectivo = activos.Where(r => r.MetodoPago == "EFECTIVO").Sum(r => r.Total);
        var banco = activos.Where(r => r.MetodoPago != "EFECTIVO").Sum(r => r.Total);
        var netos = efectivo + banco;
        var totalCancelado = cancelados.Sum(r => r.Total);

        return new CorteTurnoResponseDTO(
            Fecha: targetDate,
            CajeroUsuario: cajeroUsuario,
            TotalEmitidos: totalEmitidos,
            TotalActivos: activos.Count,
            TotalCancelados: cancelados.Count,
            TotalCobradoEfectivo: efectivo,
            TotalCobradoBanco: banco,
            TotalIngresosNetos: netos,
            TotalCanceladoImporte: totalCancelado,
            GeneradoEn: DateTimeOffset.UtcNow
        );
    }

    /// <summary>
    /// Retrieves audit trail of cancelled receipts with reasons, dates, and author cashier.
    /// </summary>
    public async Task<IReadOnlyList<AuditoriaCancelacionDTO>> GetAuditoriaCancelacionesAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Recibos
            .AsNoTracking()
            .Where(r => r.Estado == "CANCELADO");

        if (desde.HasValue)
        {
            query = query.Where(r => r.FechaCancelacion >= desde.Value || r.FechaOperacion >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(r => r.FechaCancelacion <= hasta.Value || r.FechaOperacion <= hasta.Value);
        }

        return await query
            .OrderByDescending(r => r.FechaCancelacion ?? r.FechaOperacion)
            .Select(r => new AuditoriaCancelacionDTO(
                r.Id,
                r.NumeroRecibo,
                r.FolioAdministrativo,
                r.AlumnoMatricula,
                r.AlumnoNombre,
                r.Total,
                r.MetodoPago,
                r.FechaOperacion,
                r.FechaCancelacion,
                r.MotivoCancelacion,
                r.CajeroUsuario
            ))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves lightweight status and summary for a receipt by its consecutive number.
    /// </summary>
    public async Task<ReciboEstadoResumenDTO?> GetEstadoResumenAsync(
        long numeroRecibo,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Recibos
            .AsNoTracking()
            .Where(r => r.NumeroRecibo == numeroRecibo)
            .Select(r => new ReciboEstadoResumenDTO(
                r.NumeroRecibo,
                r.Estado,
                r.TipoRecibo,
                r.FechaOperacion,
                r.FechaCancelacion,
                r.Total
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Generates a downloadable institutional text/voucher receipt file with complete fiscal, student, and breakdown data.
    /// </summary>
    public async Task<(byte[] Content, string ContentType, string FileName)?> ExportarReciboAsync(
        long numero,
        CancellationToken cancellationToken = default)
    {
        var recibo = await dbContext.Recibos
            .AsNoTracking()
            .Include(r => r.Detalles)
            .FirstOrDefaultAsync(r => r.NumeroRecibo == numero, cancellationToken);

        if (recibo is null)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("                 TECNOLÓGICO NACIONAL DE MÉXICO - CAMPUS MONCLOVA               ");
        sb.AppendLine("                    DEPARTAMENTO DE INGRESOS PROPIOS Y RECURSOS                 ");
        sb.AppendLine("                         COMPROBANTE OFICIAL DE PAGO EN CAJA                    ");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"RECIBO OFICIAL NO:  #{recibo.NumeroRecibo:D6}");
        sb.AppendLine($"FOLIO FISCAL ADM:   {recibo.FolioAdministrativo}");
        sb.AppendLine($"FECHA DE OPERACIÓN: {recibo.FechaOperacion:dd/MM/yyyy HH:mm:ss} UTC");
        sb.AppendLine($"ESTATUS DEL RECIBO: {recibo.Estado}");
        sb.AppendLine($"MÉTODO DE PAGO:     {recibo.MetodoPago}");
        if (!string.IsNullOrWhiteSpace(recibo.ReferenciaBancaria))
        {
            sb.AppendLine($"REF. BANCARIA:      {recibo.ReferenciaBancaria}");
            sb.AppendLine($"CUENTA RECEPTORA:   {recibo.CuentaBancaria ?? "5400-001-002-0261"}");
        }
        sb.AppendLine($"CAJERO RESPONSABLE: {recibo.CajeroNombre} ({recibo.CajeroUsuario})");
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine("DATOS DEL ESTUDIANTE:");
        sb.AppendLine($"MATRÍCULA:          {recibo.AlumnoMatricula}");
        sb.AppendLine($"NOMBRE:             {recibo.AlumnoNombre}");
        sb.AppendLine($"CARRERA:            {recibo.AlumnoCarrera} (Semestre {recibo.AlumnoSemestre})");
        sb.AppendLine($"TURNO:              {recibo.AlumnoTurno}");
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine(string.Format("{0,-45} {1,5} {2,12} {3,12}", "CONCEPTO", "CANT", "CUOTA", "IMPORTE"));
        sb.AppendLine("--------------------------------------------------------------------------------");
        foreach (var d in recibo.Detalles)
        {
            var desc = d.Descripcion.Length > 45 ? d.Descripcion.Substring(0, 42) + "..." : d.Descripcion;
            sb.AppendLine(string.Format("{0,-45} {1,5} {2,12:C2} {3,12:C2}",
                desc,
                d.Cantidad,
                d.CuotaUnitario,
                d.ImporteTotal));
        }
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine($"SUBTOTAL:                                                   {recibo.Subtotal,12:C2}");
        if (recibo.DescuentoTotal > 0)
        {
            sb.AppendLine($"DESCUENTO BECA:                                            -{recibo.DescuentoTotal,12:C2}");
        }
        sb.AppendLine($"GRAN TOTAL M.N.:                                            {recibo.Total,12:C2}");
        sb.AppendLine("================================================================================");
        if (recibo.Estado == "CANCELADO")
        {
            sb.AppendLine("*** RECIBO CANCELADO ***");
            sb.AppendLine($"FECHA CANCELACIÓN:  {recibo.FechaCancelacion:dd/MM/yyyy HH:mm:ss} UTC");
            sb.AppendLine($"MOTIVO AUDITORÍA:   {recibo.MotivoCancelacion}");
            sb.AppendLine("================================================================================");
        }
        sb.AppendLine("COMPROBANTE EMITIDO CONFORME A LA NORMATIVA FISCAL Y CONTABLE DEL TECNM.");
        sb.AppendLine($"SELLO DIGITAL DE EMISIÓN: TECNM-INGRESOS-{recibo.Id:N}");
        sb.AppendLine("================================================================================");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"Recibo_TecNM_{recibo.NumeroRecibo}.txt";
        return (bytes, "text/plain; charset=utf-8", fileName);
    }

    /// <summary>
    /// Generates next consecutive receipt number ensuring contiguity and consistency.
    /// </summary>
    private async Task<long> GenerateNextReceiptNumberAsync(CancellationToken cancellationToken)
    {
        var maxInRecibos = await dbContext.Recibos
            .MaxAsync(r => (long?)r.NumeroRecibo, cancellationToken) ?? 0;

        var maxInFolios = await dbContext.Folios
            .MaxAsync(f => (long?)f.NumeroFolio, cancellationToken) ?? 0;

        var next = Math.Max(maxInRecibos, maxInFolios) + 1;
        if (next < 1000)
        {
            next = 1001; // Official base folio start
        }

        dbContext.Folios.Add(new Folio
        {
            NumeroFolio = next,
            Serie = "A",
            FechaCreacion = DateTimeOffset.UtcNow,
            Observaciones = $"Recibo emitido automáticamente {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"
        });

        return next;
    }

    private static ReciboResponseDTO MapToDTO(Recibo entity) =>
        new(
            entity.Id,
            entity.NumeroRecibo,
            entity.FolioAdministrativo,
            entity.FechaOperacion,
            entity.FechaEfectivaPago,
            entity.AlumnoMatricula,
            entity.AlumnoNombre,
            entity.AlumnoCarrera,
            entity.AlumnoSemestre,
            entity.AlumnoTurno,
            entity.MetodoPago,
            entity.ReferenciaBancaria,
            entity.CuentaBancaria,
            entity.Subtotal,
            entity.DescuentoTotal,
            entity.Total,
            entity.Status,
            entity.Estado,
            entity.TipoRecibo,
            entity.MotivoCancelacion,
            entity.FechaCancelacion,
            entity.CajeroUsuario,
            entity.CajeroNombre,
            entity.Detalles.Select(d => new DetalleReciboResponseDTO(
                d.Id,
                d.ConceptoId,
                d.Descripcion,
                d.Cantidad,
                d.CuotaUnitario,
                d.DescuentoBeca,
                d.ImporteTotal,
                d.PeriodoAno
            )).ToList()
        );
}
