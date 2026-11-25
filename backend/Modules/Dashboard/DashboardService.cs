using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Dashboard;

/// <summary>
/// Domain service encapsulating role-scoped dashboard KPIs, payment breakdowns,
/// weekly revenue trends, top concepts, recent receipts, and PDF export generation.
/// </summary>
public sealed class DashboardService(
    TecNMDbContext dbContext,
    ILogger<DashboardService> logger)
{
    /// <summary>
    /// Computes complete dashboard indicators scoped by the authenticated user's role and shift.
    /// Admin/Auditor see global institutional figures; Cashiers see only their assigned shift/user transactions.
    /// </summary>
    public async Task<DashboardIndicadoresDTO> GetIndicadoresAsync(
        string periodo,
        string username,
        string userDisplayName,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalizedPeriodo = (periodo ?? "hoy").Trim().ToLowerInvariant();
        if (normalizedPeriodo is not ("hoy" or "semana" or "mes"))
        {
            normalizedPeriodo = "hoy";
        }

        var normalizedRole = string.IsNullOrWhiteSpace(role) ? "Admin" : role.Trim();
        var normalizedUsername = string.IsNullOrWhiteSpace(username) ? "admin" : username.Trim();
        var displayName = string.IsNullOrWhiteSpace(userDisplayName) ? "Administrador General" : userDisplayName.Trim();

        try
        {
            // 1. Base query scoped by role and shift
            var (recibosQuery, scopeLabel) = BuildRoleScopedQuery(normalizedUsername, displayName, normalizedRole);

            var allScopedRecibos = await recibosQuery
                .Include(r => r.Detalles)
                .OrderByDescending(r => r.FechaOperacion)
                .ToListAsync(cancellationToken);

            // 2. Reference date (today if receipts exist today, otherwise most recent receipt date)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var hasToday = allScopedRecibos.Any(r => DateOnly.FromDateTime(r.FechaOperacion.UtcDateTime) == today);
            var refDate = hasToday || allScopedRecibos.Count == 0
                ? today
                : DateOnly.FromDateTime(allScopedRecibos[0].FechaOperacion.UtcDateTime);

            // 3. Filter receipts by active period ('hoy' | 'semana' | 'mes')
            var recibosPeriodo = FilterByPeriod(allScopedRecibos, refDate, normalizedPeriodo);
            var recibosVigentes = recibosPeriodo.Where(r => !string.Equals(r.Estado, "CANCELADO", StringComparison.OrdinalIgnoreCase)).ToList();

            // 4. Top KPI Calculations
            var recaudacionTotal = recibosVigentes.Sum(r => r.Total);
            var metaObjetivo = normalizedPeriodo switch
            {
                "hoy" => string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase) ? 165000m : 65000m,
                "semana" => string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase) ? 850000m : 320000m,
                _ => string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase) ? 3200000m : 1250000m
            };

            var totalRecibosEmitidos = recibosVigentes.Count;
            var ultimoFolio = recibosPeriodo.Count > 0
                ? recibosPeriodo.Max(r => r.NumeroRecibo).ToString()
                : "0";
            var promedioPorRecibo = totalRecibosEmitidos > 0
                ? Math.Round(recaudacionTotal / totalRecibosEmitidos, 2)
                : 0m;

            // 5. Student attendance & pending debt compliance
            var matriculasAtendidas = recibosVigentes
                .Select(r => r.AlumnoMatricula)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var totalAlumnosAtendidos = matriculasAtendidas.Count;

            var matriculasConAdeudo = await dbContext.Adeudos
                .AsNoTracking()
                .Where(a => a.Estado == "PENDIENTE" || a.Estado == "PARCIAL")
                .Select(a => a.Matricula)
                .Distinct()
                .ToListAsync(cancellationToken);

            var adeudoSet = new HashSet<string>(matriculasConAdeudo, StringComparer.OrdinalIgnoreCase);
            var conAdeudoCount = matriculasAtendidas.Count(m => adeudoSet.Contains(m));
            var pctAlCorriente = totalAlumnosAtendidos > 0
                ? (int)Math.Round(((totalAlumnosAtendidos - conAdeudoCount) / (double)totalAlumnosAtendidos) * 100)
                : 100;
            var pctConAdeudo = 100 - pctAlCorriente;

            // 6. Recovered Debts
            var detallesVigentes = recibosVigentes.SelectMany(r => r.Detalles.Select(d => new { r.AlumnoMatricula, Detalle = d })).ToList();
            var regularizacionDetalles = detallesVigentes.Where(x =>
            {
                var desc = x.Detalle.Descripcion.ToLowerInvariant();
                return desc.Contains("adeudo") || desc.Contains("previo") || desc.Contains("regularizacion") || desc.Contains("especial");
            }).ToList();

            var adeudosRecuperadosMonto = regularizacionDetalles.Sum(x => x.Detalle.ImporteTotal);
            var alumnosAdeudoRecuperado = regularizacionDetalles
                .Select(x => x.AlumnoMatricula)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            // 7. Payment Methods Breakdown
            var metodosPago = BuildPaymentMethodsBreakdown(recibosVigentes);

            // 8. Weekly Revenue Trend (Monday - Saturday)
            var (tendenciaSemanal, promedioSemanal) = BuildWeeklyTrend(allScopedRecibos, refDate);

            // 9. Top 4 Revenue Concepts
            var conceptosTop = BuildTopConcepts(detallesVigentes.Select(x => x.Detalle).ToList());

            // 10. Recent Receipts with Real Student Matriculas & Names (verified against Alumnos table if needed)
            var sourceForTable = recibosPeriodo.Count > 0 ? recibosPeriodo : allScopedRecibos;
            var recentSlice = sourceForTable.Take(5).ToList();
            var matriculasLookup = recentSlice.Select(r => r.AlumnoMatricula).Distinct().ToList();
            var alumnosMap = await dbContext.Alumnos
                .AsNoTracking()
                .Where(a => matriculasLookup.Contains(a.Matricula))
                .ToDictionaryAsync(a => a.Matricula, a => a.NombreCompleto, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var ultimosRecibos = recentSlice.Select(r =>
            {
                var nombreReal = !string.IsNullOrWhiteSpace(r.AlumnoNombre) && !r.AlumnoNombre.Equals("Estudiante TecNM", StringComparison.OrdinalIgnoreCase)
                    ? r.AlumnoNombre
                    : (alumnosMap.TryGetValue(r.AlumnoMatricula, out var dbName) ? dbName : r.AlumnoNombre);

                return new ReciboDashboardItemDTO(
                    Id: r.Id,
                    Folio: r.NumeroRecibo > 0 ? r.NumeroRecibo.ToString() : r.FolioAdministrativo,
                    Matricula: r.AlumnoMatricula,
                    Alumno: nombreReal,
                    MetodoPago: FormatMetodoPago(r.MetodoPago, r.ReferenciaBancaria),
                    Monto: r.Total,
                    Estado: string.Equals(r.Estado, "CANCELADO", StringComparison.OrdinalIgnoreCase) ? "Cancelado" : "Pagado",
                    FechaOperacion: r.FechaOperacion,
                    CajeroUsuario: r.CajeroUsuario);
            }).ToList();

            return new DashboardIndicadoresDTO(
                UsuarioActual: displayName,
                RolActual: normalizedRole,
                ScopeLabel: scopeLabel,
                Periodo: normalizedPeriodo,
                FechaReferencia: refDate.ToString("yyyy-MM-dd"),
                RecaudacionTotal: recaudacionTotal,
                MetaObjetivo: metaObjetivo,
                TotalRecibosEmitidos: totalRecibosEmitidos,
                UltimoFolio: ultimoFolio,
                PromedioPorRecibo: promedioPorRecibo,
                TotalAlumnosAtendidos: totalAlumnosAtendidos,
                PorcentajeAlCorriente: pctAlCorriente,
                PorcentajeConAdeudo: pctConAdeudo,
                AdeudosRecuperadosMonto: adeudosRecuperadosMonto,
                AlumnosAdeudoRecuperado: alumnosAdeudoRecuperado,
                MetodosPago: metodosPago,
                TendenciaSemanal: tendenciaSemanal,
                PromedioSemanal: promedioSemanal,
                ConceptosTop: conceptosTop,
                UltimosRecibos: ultimosRecibos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error computing dashboard indicators for user '{User}' with role '{Role}'", normalizedUsername, normalizedRole);
            throw;
        }
    }

    /// <summary>
    /// Generates an official PDF byte stream for the current role-scoped dashboard cut.
    /// </summary>
    public async Task<byte[]> ExportarCortePdfAsync(
        string periodo,
        string username,
        string userDisplayName,
        string role,
        CancellationToken cancellationToken = default)
    {
        var indicators = await GetIndicadoresAsync(periodo, username, userDisplayName, role, cancellationToken);
        return DashboardPdfBuilder.BuildCortePdf(indicators);
    }

    private (IQueryable<Recibo> Query, string ScopeLabel) BuildRoleScopedQuery(
        string username,
        string displayName,
        string role)
    {
        var baseQuery = dbContext.Recibos.AsNoTracking();

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Auditor", StringComparison.OrdinalIgnoreCase))
        {
            return (baseQuery, "Vista Global (Administrador)");
        }

        if (string.Equals(role, "Cajero", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(username, "cajero1", StringComparison.OrdinalIgnoreCase)
                || displayName.Contains("Matutino", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    baseQuery.Where(r => r.CajeroUsuario == "cajero1"
                        || r.CajeroUsuario == "Cajero Turno Matutino"
                        || r.CajeroNombre.Contains("Matutino")),
                    "Turno Matutino (cajero1)");
            }

            if (string.Equals(username, "cajero2", StringComparison.OrdinalIgnoreCase)
                || displayName.Contains("Vespertino", StringComparison.OrdinalIgnoreCase)
                || displayName.Contains("Nocturno", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    baseQuery.Where(r => r.CajeroUsuario == "cajero2"
                        || r.CajeroUsuario == "Cajero Turno Nocturno"
                        || r.CajeroUsuario == "Cajero Turno Vespertino"
                        || r.CajeroNombre.Contains("Nocturno")
                        || r.CajeroNombre.Contains("Vespertino")),
                    "Turno Vespertino / Nocturno (cajero2)");
            }
        }

        return (
            baseQuery.Where(r => r.CajeroUsuario == username || r.CajeroNombre == displayName),
            $"Usuario ({username})");
    }

    private static List<Recibo> FilterByPeriod(List<Recibo> recibos, DateOnly refDate, string periodo)
    {
        if (periodo == "hoy")
        {
            return recibos.Where(r => DateOnly.FromDateTime(r.FechaOperacion.UtcDateTime) == refDate).ToList();
        }

        if (periodo == "semana")
        {
            var dayOfWeek = (int)refDate.DayOfWeek; // 0 = Sun, 1 = Mon
            var distToMon = (dayOfWeek + 6) % 7;
            var monday = refDate.AddDays(-distToMon);
            var sunday = monday.AddDays(6);

            return recibos.Where(r =>
            {
                var d = DateOnly.FromDateTime(r.FechaOperacion.UtcDateTime);
                return d >= monday && d <= sunday;
            }).ToList();
        }

        if (periodo == "mes")
        {
            return recibos.Where(r =>
            {
                var d = DateOnly.FromDateTime(r.FechaOperacion.UtcDateTime);
                return d.Year == refDate.Year && d.Month == refDate.Month;
            }).ToList();
        }

        return recibos;
    }

    private static List<MetodoPagoDesgloseDTO> BuildPaymentMethodsBreakdown(List<Recibo> recibosVigentes)
    {
        decimal tarjeta = 0m, transferencia = 0m, efectivo = 0m, referenciado = 0m;

        foreach (var r in recibosVigentes)
        {
            var m = (r.MetodoPago ?? string.Empty).ToUpperInvariant();
            if (m.Contains("TARJETA") || m.Contains("CREDITO") || m.Contains("DEBITO"))
            {
                tarjeta += r.Total;
            }
            else if (m.Contains("SPEI") || m.Contains("TRANSFERENCIA"))
            {
                transferencia += r.Total;
            }
            else if (m.Contains("DEPOSITO") || (!string.IsNullOrWhiteSpace(r.ReferenciaBancaria) && r.ReferenciaBancaria != "0"))
            {
                referenciado += r.Total;
            }
            else
            {
                efectivo += r.Total;
            }
        }

        var total = tarjeta + transferencia + efectivo + referenciado;
        int Pct(decimal v) => total > 0 ? (int)Math.Round((v / total) * 100) : 0;

        return
        [
            new("tarjeta", "Tarjeta Débito/Crédito", tarjeta, Pct(tarjeta), "#1B396A"),
            new("transferencia", "Transferencia SPEI", transferencia, Pct(transferencia), "#3B82F6"),
            new("efectivo", "Efectivo", efectivo, Pct(efectivo), "#C5A059"),
            new("referenciado", "Depósito Referenciado", referenciado, Pct(referenciado), "#10B981")
        ];
    }

    private static (List<DiaBarraDTO> Barras, decimal Promedio) BuildWeeklyTrend(List<Recibo> allRecibos, DateOnly refDate)
    {
        var dayOfWeek = (int)refDate.DayOfWeek;
        var distToMon = (dayOfWeek + 6) % 7;
        var monday = refDate.AddDays(-distToMon);
        var sunday = monday.AddDays(6);

        var nombresDias = new Dictionary<int, string>
        {
            [1] = "Lun", [2] = "Mar", [3] = "Mié", [4] = "Jue", [5] = "Vie", [6] = "Sáb"
        };

        var acumulado = new Dictionary<int, decimal>
        {
            [1] = 0m, [2] = 0m, [3] = 0m, [4] = 0m, [5] = 0m, [6] = 0m
        };

        foreach (var r in allRecibos)
        {
            if (string.Equals(r.Estado, "CANCELADO", StringComparison.OrdinalIgnoreCase)) continue;
            var d = DateOnly.FromDateTime(r.FechaOperacion.UtcDateTime);
            if (d < monday || d > sunday) continue;
            var dow = (int)d.DayOfWeek;
            if (dow >= 1 && dow <= 6)
            {
                acumulado[dow] += r.Total;
            }
        }

        var maxMonto = Math.Max(acumulado.Values.Max(), 1000m);
        var diaMax = acumulado.OrderByDescending(kv => kv.Value).First().Key;

        var barras = acumulado.Select(kv =>
        {
            var monto = kv.Value;
            var pct = monto > 0 ? (int)Math.Min(100, Math.Max(15, Math.Round((monto / maxMonto) * 100))) : 8;
            var etiqueta = monto >= 1000
                ? $"${(monto / 1000m):0.#}k"
                : $"${monto:0}";

            return new DiaBarraDTO(
                Dia: nombresDias[kv.Key],
                DiaCorto: nombresDias[kv.Key],
                Monto: monto,
                EtiquetaK: etiqueta,
                PorcentajeAltura: pct,
                EsDestacado: monto > 0 && kv.Key == diaMax);
        }).ToList();

        var promedio = Math.Round(acumulado.Values.Average(), 2);
        return (barras, promedio);
    }

    private static List<ConceptoProgresoDTO> BuildTopConcepts(List<DetalleRecibo> detalles)
    {
        if (detalles.Count == 0) return [];

        var agrupado = detalles
            .GroupBy(d => string.IsNullOrWhiteSpace(d.Descripcion) ? "Otros Derechos" : d.Descripcion.Trim())
            .Select(g => new { Nombre = g.Key, Monto = g.Sum(x => x.ImporteTotal) })
            .OrderByDescending(x => x.Monto)
            .ToList();

        var total = agrupado.Sum(x => x.Monto);

        return agrupado
            .Take(4)
            .Select(x => new ConceptoProgresoDTO(
                Nombre: x.Nombre,
                Monto: x.Monto,
                Porcentaje: total > 0 ? (int)Math.Round((x.Monto / total) * 100) : 0))
            .ToList();
    }

    private static string FormatMetodoPago(string? metodoPago, string? referencia)
    {
        var m = (metodoPago ?? string.Empty).ToUpperInvariant();
        if (m.Contains("TARJETA") || m.Contains("CREDITO") || m.Contains("DEBITO")) return "Tarjeta Débito";
        if (m.Contains("SPEI") || m.Contains("TRANSFERENCIA")) return "Transferencia SPEI";
        if (m.Contains("DEPOSITO") || (!string.IsNullOrWhiteSpace(referencia) && referencia != "0")) return "Depósito Bancario";
        return "Efectivo";
    }
}

