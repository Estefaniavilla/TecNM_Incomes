using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Alumnos;

/// <summary>
/// Domain service encapsulating student roster management, indexed multi-field search,
/// scholarship calculation, and enrollment history tracking.
/// </summary>
public sealed class AlumnosService(
    TecNMDbContext dbContext,
    ILogger<AlumnosService> logger)
{
    /// <summary>
    /// Searches students by enrollment ID, full name, or registration card number (Ficha)
    /// across the entire 14,000+ student roster in PostgreSQL.
    /// Also inspects historical matricula changes for legacy identification.
    /// </summary>
    public async Task<List<AlumnoDTO>> BuscarAlumnosAsync(
        string? query,
        int? limite = null,
        CancellationToken cancellationToken = default)
    {
        var q = query?.Trim().ToLowerInvariant() ?? string.Empty;
        var maxResults = limite is > 0 ? Math.Min(limite.Value, 15000) : (string.IsNullOrEmpty(q) ? 50 : 50);

        try
        {
            // 1. Search active student roster across all 14,000+ students
            var queryable = dbContext.Alumnos.AsNoTracking();

            if (!string.IsNullOrEmpty(q))
            {
                queryable = queryable.Where(a =>
                    a.Matricula.ToLower().Contains(q)
                    || a.NombreCompleto.ToLower().Contains(q)
                    || (a.NumeroFicha != null && a.NumeroFicha.ToLower().Contains(q)));
            }

            var alumnos = await queryable
                .OrderBy(a => a.NombreCompleto)
                .Take(maxResults)
                .Select(a => ToDTO(a, null))
                .ToListAsync(cancellationToken);

            // 2. If a specific query was provided, check if it matches a historical enrollment ID
            if (!string.IsNullOrEmpty(q) && alumnos.Count < maxResults)
            {
                var historicos = await dbContext.AlumnoMatriculasHistorial
                    .AsNoTracking()
                    .Include(h => h.Alumno)
                    .Where(h => h.MatriculaPrevia.ToLower().Contains(q))
                    .Take(10)
                    .ToListAsync(cancellationToken);

                foreach (var hist in historicos)
                {
                    if (hist.Alumno is not null && !alumnos.Any(a => a.Matricula == hist.Alumno.Matricula))
                    {
                        alumnos.Add(ToDTO(hist.Alumno, hist.MatriculaPrevia));
                    }
                }
            }

            return alumnos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing student search with query '{Query}'", query);
            throw;
        }
    }

    /// <summary>
    /// Retrieves active engineering careers from PostgreSQL, strictly excluding legacy Licenciaturas
    /// and deduplicating entries so frontend dropdowns never show repeated careers.
    /// </summary>
    public async Task<List<CarreraCatalogoDTO>> GetCarrerasAsync(CancellationToken cancellationToken = default)
    {
        var raw = await dbContext.Carreras
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Abreviatura)
            .ToListAsync(cancellationToken);

        return raw
            .Where(c => !c.Abreviatura.StartsWith("Lic.", StringComparison.OrdinalIgnoreCase)
                && !c.NombreCarrera.Contains("LICENCIATURA", StringComparison.OrdinalIgnoreCase))
            .Select(c => new
            {
                c.IdCarrera,
                CleanName = c.Abreviatura.Trim(),
                c.Titulo,
                c.Activo
            })
            .DistinctBy(c => c.CleanName, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CarreraCatalogoDTO(
                IdCarrera: c.IdCarrera,
                Carrera: c.CleanName,
                NombreCarrera: c.CleanName,
                Titulo: c.Titulo,
                Activo: c.Activo))
            .ToList();
    }

    /// <summary>
    /// Retrieves paginated student records supporting institutional batch-of-5 UI pagination.
    /// Filters by academic major, semester, and enrollment status.
    /// </summary>
    public async Task<PagedAlumnosResult<AlumnoDTO>> GetPagedAsync(
        AlumnoFilterParams filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize > 0 ? filters.PageSize : 5;

        var query = dbContext.Alumnos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filters.Q))
        {
            var q = filters.Q.Trim().ToLower();
            query = query.Where(a =>
                a.Matricula.ToLower().Contains(q)
                || a.NombreCompleto.ToLower().Contains(q)
                || (a.NumeroFicha != null && a.NumeroFicha.ToLower().Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(filters.Carrera))
        {
            var c = filters.Carrera.Trim().ToLower();
            query = query.Where(a => a.Carrera.ToLower().Contains(c));
        }

        if (filters.Semestre.HasValue && filters.Semestre.Value > 0)
        {
            query = query.Where(a => a.Semestre == filters.Semestre.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Estatus))
        {
            var e = filters.Estatus.Trim().ToLower();
            query = query.Where(a => a.Estatus.ToLower() == e);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(a => a.Matricula)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ToDTO(a, null))
            .ToListAsync(cancellationToken);

        return new PagedAlumnosResult<AlumnoDTO>(
            Items: items,
            TotalCount: totalCount,
            CurrentPage: page,
            PageSize: pageSize,
            TotalPages: totalPages);
    }

    /// <summary>
    /// Finds a student by exact enrollment ID (Matricula), including fallback to previous matriculas.
    /// </summary>
    public async Task<AlumnoDTO?> GetAlumnoAsync(string matricula, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(matricula)) return null;

        var m = matricula.Trim();

        // Direct match
        var alumno = await dbContext.Alumnos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Matricula.ToLower() == m.ToLower(), cancellationToken);

        if (alumno is not null)
        {
            return ToDTO(alumno, null);
        }

        // Search in historical matriculas
        var historial = await dbContext.AlumnoMatriculasHistorial
            .AsNoTracking()
            .Include(h => h.Alumno)
            .FirstOrDefaultAsync(h => h.MatriculaPrevia.ToLower() == m.ToLower(), cancellationToken);

        if (historial?.Alumno is not null)
        {
            return ToDTO(historial.Alumno, historial.MatriculaPrevia);
        }

        return null;
    }

    /// <summary>
    /// Retrieves matricula migration history for a student.
    /// </summary>
    public async Task<List<MatriculaHistorialDTO>> GetHistorialMatriculasAsync(string matricula, CancellationToken cancellationToken = default)
    {
        return await dbContext.AlumnoMatriculasHistorial
            .AsNoTracking()
            .Include(h => h.Periodo)
            .Where(h => h.MatriculaAlumno.ToLower() == matricula.ToLower())
            .OrderByDescending(h => h.FechaRegistro)
            .Select(h => new MatriculaHistorialDTO(
                h.IdHistorial,
                h.MatriculaAlumno,
                h.MatriculaPrevia,
                h.Periodo != null ? h.Periodo.Codigo : null,
                h.FechaRegistro,
                h.MotivoCambio))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reassigns an enrollment number while preserving audit history.
    /// </summary>
    public async Task<AlumnoDTO> CambiarMatriculaAsync(
        string matriculaActual,
        CambiarMatriculaRequest req,
        string usuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(req.NuevaMatricula))
        {
            throw new ArgumentException("The new enrollment ID (matricula) cannot be empty.");
        }

        var mActual = matriculaActual.Trim();
        var mNueva = req.NuevaMatricula.Trim();

        if (string.Equals(mActual, mNueva, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The new enrollment ID must differ from the current one.");
        }

        var alumno = await dbContext.Alumnos
            .FirstOrDefaultAsync(a => a.Matricula.ToLower() == mActual.ToLower(), cancellationToken);

        if (alumno is null)
        {
            throw new KeyNotFoundException($"Student with enrollment ID '{matriculaActual}' was not found.");
        }

        var existeNueva = await dbContext.Alumnos
            .AnyAsync(a => a.Matricula.ToLower() == mNueva.ToLower(), cancellationToken);

        if (existeNueva)
        {
            throw new InvalidOperationException($"The enrollment ID '{mNueva}' is already assigned to another student.");
        }

        var historial = new AlumnoMatriculaHistorial
        {
            MatriculaAlumno = mNueva,
            MatriculaPrevia = alumno.Matricula,
            IdPeriodo = req.IdPeriodo,
            MotivoCambio = req.MotivoCambio,
            FechaRegistro = DateTimeOffset.UtcNow
        };

        alumno.Matricula = mNueva;
        dbContext.AlumnoMatriculasHistorial.Add(historial);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Student matricula changed from '{Old}' to '{New}' by user '{User}'", mActual, mNueva, usuario);

        return ToDTO(alumno, mActual);
    }

    /// <summary>
    /// Retrieves outstanding debt entries for a student.
    /// </summary>
    public async Task<List<AdeudoDTO>> GetAdeudosAlumnoAsync(string matricula, CancellationToken cancellationToken = default)
    {
        return await dbContext.Adeudos
            .AsNoTracking()
            .Include(a => a.Periodo)
            .Where(a => a.Matricula == matricula.Trim() && (a.Estado == "PENDIENTE" || a.Estado == "PARCIAL"))
            .OrderBy(a => a.IdPeriodo)
            .Select(a => new AdeudoDTO(
                a.IdAdeudo,
                a.Matricula,
                a.ConceptoCobro,
                a.IdPeriodo,
                a.Periodo != null ? a.Periodo.Codigo : "VIGENTE",
                a.MontoOriginal,
                a.SaldoPendiente,
                a.FechaLimite,
                a.Estado))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// General audit debt query with multi-criteria filtering.
    /// </summary>
    public async Task<List<AdeudoDetalleDTO>> ConsultarAdeudosGeneralAsync(
        string? matricula,
        string? concepto,
        int? periodoId,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Adeudos
            .AsNoTracking()
            .Include(a => a.Alumno)
            .Include(a => a.Periodo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(matricula))
        {
            query = query.Where(a => a.Matricula.ToLower().Contains(matricula.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(concepto))
        {
            query = query.Where(a => a.ConceptoCobro.ToLower().Contains(concepto.ToLower()));
        }

        if (periodoId.HasValue && periodoId.Value > 0)
        {
            query = query.Where(a => a.IdPeriodo == periodoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(a => a.Estado.ToLower() == estado.ToLower());
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new AdeudoDetalleDTO(
                a.IdAdeudo,
                a.Matricula,
                a.Alumno != null ? a.Alumno.NombreCompleto : "Sin Nombre",
                a.Alumno != null ? a.Alumno.Carrera : "Sin Carrera",
                a.ConceptoCobro,
                a.Periodo != null ? a.Periodo.Codigo : "N/D",
                a.Periodo != null ? a.Periodo.Anio : (short)DateTime.UtcNow.Year,
                a.Periodo != null ? a.Periodo.Ciclo : "ENE-JUN",
                a.MontoOriginal,
                a.SaldoPendiente,
                a.FechaLimite,
                a.Estado))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves scholarships and fee exemptions assigned to a student.
    /// </summary>
    public async Task<List<BecaDTO>> GetBecasAlumnoAsync(string matricula, CancellationToken cancellationToken = default)
    {
        return await dbContext.BecasDescuentos
            .AsNoTracking()
            .Include(b => b.Periodo)
            .Where(b => b.Matricula == matricula.Trim() && b.Activa)
            .Select(b => new BecaDTO(
                b.IdBeca,
                b.Matricula,
                b.IdPeriodo,
                b.Periodo != null ? b.Periodo.Codigo : "VIGENTE",
                b.TipoBeca,
                b.Porcentaje,
                b.MontoFijo,
                b.Motivo,
                b.Activa))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Registers a new student or updates an existing one when synchronized from Academic Services.
    /// </summary>
    public async Task<AlumnoDTO> InscribirOActualizarAlumnoAsync(
        InscribirAlumnoRequest req,
        string usuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(req.Matricula))
        {
            throw new ArgumentException("Matricula is required.");
        }

        if (string.IsNullOrWhiteSpace(req.NombreCompleto))
        {
            throw new ArgumentException("Full student name is required.");
        }

        var m = req.Matricula.Trim();
        var existing = await dbContext.Alumnos.FirstOrDefaultAsync(a => a.Matricula.ToLower() == m.ToLower(), cancellationToken);

        if (existing is not null)
        {
            existing.NombreCompleto = req.NombreCompleto.Trim().ToUpper();
            existing.Carrera = req.Carrera.Trim();
            existing.Semestre = req.Semestre;
            existing.Turno = req.Turno.Trim();
            existing.Estatus = req.Estatus.Trim();
            existing.NumeroFicha = req.NumeroFicha?.Trim();
            existing.TieneBeca = req.TieneBeca;
            existing.PorcentajeBeca = req.PorcentajeBeca;
            if (req.CuentaA.HasValue) existing.CuentaA = req.CuentaA.Value;
            if (!string.IsNullOrWhiteSpace(req.Sexo)) existing.Sexo = req.Sexo.Trim();

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Student '{Matricula}' updated by '{User}'", m, usuario);
            return ToDTO(existing, null);
        }

        var nuevo = new Alumno
        {
            Matricula = m,
            NombreCompleto = req.NombreCompleto.Trim().ToUpper(),
            Carrera = req.Carrera.Trim(),
            Semestre = req.Semestre,
            Turno = req.Turno.Trim(),
            Estatus = req.Estatus.Trim(),
            NumeroFicha = req.NumeroFicha?.Trim(),
            TieneBeca = req.TieneBeca,
            PorcentajeBeca = req.PorcentajeBeca,
            CuentaA = req.CuentaA,
            Sexo = req.Sexo?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Alumnos.Add(nuevo);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("New student '{Matricula}' registered by '{User}'", m, usuario);

        return ToDTO(nuevo, null);
    }

    private static AlumnoDTO ToDTO(Alumno a, string? matriculaPrevia)
    {
        return new AlumnoDTO(
            Matricula: a.Matricula,
            NombreCompleto: a.NombreCompleto,
            Carrera: a.Carrera,
            Semestre: a.Semestre,
            Turno: a.Turno,
            Estatus: a.Estatus,
            NumeroFicha: a.NumeroFicha,
            TieneBeca: a.TieneBeca,
            PorcentajeBeca: a.PorcentajeBeca,
            FechaRegistro: a.CreatedAt,
            MatriculaPrevia: matriculaPrevia,
            CuentaA: a.CuentaA,
            Sexo: a.Sexo);
    }
}
