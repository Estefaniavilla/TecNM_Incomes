namespace TecNM.Api.Modules.Alumnos;

/// <summary>
/// Data Transfer Object representing an official student record in TecNM.
/// </summary>
public sealed record AlumnoDTO(
    string Matricula,
    string NombreCompleto,
    string Carrera,
    int Semestre,
    string Turno,
    string Estatus,
    string? NumeroFicha = null,
    bool TieneBeca = false,
    decimal PorcentajeBeca = 0m,
    DateTimeOffset? FechaRegistro = null,
    string? MatriculaPrevia = null,
    int? CuentaA = null,
    string? Sexo = null);

/// <summary>
/// Query filter parameters for searching and paginating students.
/// Supports batch-of-5 pagination natively matching the UI standard.
/// </summary>
public sealed record AlumnoFilterParams(
    string? Q = null,
    string? Carrera = null,
    int? Semestre = null,
    string? Estatus = null,
    int Page = 1,
    int PageSize = 5);

/// <summary>
/// Paginated envelope for student listings.
/// </summary>
public sealed record PagedAlumnosResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int CurrentPage,
    int PageSize,
    int TotalPages)
{
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// History record of student enrollment ID (matricula) changes.
/// </summary>
public sealed record MatriculaHistorialDTO(
    long IdHistorial,
    string MatriculaAlumno,
    string MatriculaPrevia,
    string? PeriodoCodigo,
    DateTimeOffset FechaRegistro,
    string? MotivoCambio);

/// <summary>
/// Request contract for migrating or reassigning a student enrollment ID.
/// </summary>
public sealed record CambiarMatriculaRequest(
    string NuevaMatricula,
    string MotivoCambio,
    int? IdPeriodo = null);

/// <summary>
/// Outstanding debt item associated with a student.
/// </summary>
public sealed record AdeudoDTO(
    long IdAdeudo,
    string Matricula,
    string ConceptoCobro,
    int IdPeriodo,
    string PeriodoCodigo,
    decimal MontoOriginal,
    decimal SaldoPendiente,
    DateOnly? FechaLimite,
    string Estado);

/// <summary>
/// Detailed student debt contract for institutional audits and collection reports.
/// </summary>
public sealed record AdeudoDetalleDTO(
    long IdAdeudo,
    string Matricula,
    string AlumnoNombre,
    string Carrera,
    string ConceptoCobro,
    string PeriodoCodigo,
    short PeriodoAnio,
    string PeriodoCiclo,
    decimal MontoOriginal,
    decimal SaldoPendiente,
    DateOnly? FechaLimite,
    string Estado);

/// <summary>
/// Scholarship grant or fee reduction linked to a student.
/// </summary>
public sealed record BecaDTO(
    long IdBeca,
    string Matricula,
    int IdPeriodo,
    string PeriodoCodigo,
    string TipoBeca,
    decimal? Porcentaje,
    decimal? MontoFijo,
    string? Motivo,
    bool Activa);

/// <summary>
/// Request contract for student registration or data synchronization from School Services.
/// </summary>
public sealed record InscribirAlumnoRequest(
    string Matricula,
    string NombreCompleto,
    string Carrera,
    int Semestre,
    string Turno,
    string? NumeroFicha = null,
    string Estatus = "Inscrito",
    bool TieneBeca = false,
    decimal PorcentajeBeca = 0m,
    int? CuentaA = null,
    string? Sexo = null);

/// <summary>
/// Academic program (career) catalog entry returned without duplicates and excluding legacy bachelor's degrees.
/// </summary>
public sealed record CarreraCatalogoDTO(
    int IdCarrera,
    string Carrera,
    string NombreCarrera,
    string? Titulo,
    bool Activo);
