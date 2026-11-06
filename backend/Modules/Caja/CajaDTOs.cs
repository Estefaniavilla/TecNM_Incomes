namespace TecNM.Api.Modules.Caja;

public sealed record CarreraDTO(
    int IdCarrera,
    string Carrera,
    string NombreCarrera,
    string? Titulo,
    bool Activo);

public sealed record PeriodoDTO(
    int IdPeriodo,
    string Codigo,
    string? Descripcion,
    short Anio,
    string Ciclo,
    DateOnly? FechaInicio,
    DateOnly? FechaFin,
    bool Activo);

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

public sealed record AlumnoDTO(
    string Matricula,
    string NombreCompleto,
    string Carrera,
    int Semestre,
    string Turno,
    string Estatus,
    bool TieneBeca,
    decimal PorcentajeBeca,
    int? IdCarrera = null,
    string? NumeroFicha = null,
    int? CuentaA = null);

public sealed record ConceptoDTO(
    Guid Id,
    string Clave,
    string Descripcion,
    decimal CuotaBase,
    bool AplicaBeca,
    string Categoria,
    string? CuentaReferencia = null,
    int? Orden = null);

public sealed record DetalleItemRequest(
    Guid? ConceptoId,
    string Descripcion,
    int Cantidad,
    decimal CuotaUnitario,
    decimal DescuentoBeca,
    decimal ImporteTotal,
    string? PeriodoAno);

public sealed record CrearReciboRequest(
    string Matricula,
    string NombreCompleto,
    string Carrera,
    int Semestre,
    string Turno,
    string MetodoPago,
    string? ReferenciaBancaria,
    string? CuentaBancaria,
    DateTimeOffset? FechaBancaria,
    List<DetalleItemRequest> Detalles);

public sealed record DetalleItemResponse(
    Guid Id,
    Guid? ConceptoId,
    string Descripcion,
    int Cantidad,
    decimal CuotaUnitario,
    decimal DescuentoBeca,
    decimal ImporteTotal,
    string? PeriodoAno);

public sealed record ReciboResponse(
    Guid Id,
    long NumeroRecibo,
    string FolioAdministrativo,
    DateTimeOffset FechaOperacion,
    string AlumnoMatricula,
    string AlumnoNombre,
    string AlumnoCarrera,
    int AlumnoSemestre,
    string AlumnoTurno,
    string MetodoPago,
    string? ReferenciaBancaria,
    string? CuentaBancaria,
    DateTimeOffset? FechaBancaria,
    decimal Subtotal,
    decimal DescuentoTotal,
    decimal Total,
    string Estado,
    string? MotivoCancelacion,
    DateTimeOffset? FechaCancelacion,
    string CajeroUsuario,
    string CajeroNombre,
    List<DetalleItemResponse> Detalles);

public sealed record CancelarReciboRequest(
    string Motivo);

public sealed record CorteCajaResponse(
    DateOnly Fecha,
    decimal TotalEfectivo,
    decimal TotalBanco,
    decimal TotalRecaudado,
    int TotalRecibosEmitidos,
    int TotalRecibosCancelados);

public sealed record ReporteReciboDetalladoDTO(
    Guid ReciboId,
    long NumeroRecibo,
    string FolioAdministrativo,
    DateTimeOffset FechaOperacion,
    string AlumnoMatricula,
    string AlumnoNombre,
    string AlumnoCarrera,
    int AlumnoSemestre,
    string AlumnoTurno,
    string ConceptoDetalle,
    int Cantidad,
    decimal CuotaUnitario,
    decimal DescuentoBeca,
    decimal ImporteRenglon,
    decimal TotalRecibo,
    string MetodoPago,
    string? ReferenciaBancaria,
    string EstadoRecibo,
    string CajeroUsuario,
    string CajeroNombre);
