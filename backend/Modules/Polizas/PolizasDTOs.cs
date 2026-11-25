namespace TecNM.Api.Modules.Polizas;

/// <summary>
/// Row item within a revenue policy (Póliza de Ingresos) grouped by accounting account code and concept.
/// </summary>
public sealed record PolizaIngresoFilaDTO(
    string Cuenta,
    string Concepto,
    int Cantidad,
    decimal Total);

/// <summary>
/// Daily revenue policy summary for institutional accounting and bank reconciliation.
/// </summary>
public sealed record PolizaIngresoResumenDTO(
    string Fecha,
    decimal TotalGeneral,
    List<PolizaIngresoFilaDTO> Filas,
    int TotalRecibos = 0);

/// <summary>
/// Individual transaction item within a date-range revenue policy.
/// </summary>
public sealed record PolizaPeriodoFilaDTO(
    DateOnly Fecha,
    string Folio,
    string Cuenta,
    string Concepto,
    decimal Importe,
    string MetodoPago,
    string Cajero);

/// <summary>
/// Multi-day period revenue policy summary with account groupings and chronological breakdown.
/// </summary>
public sealed record PolizaPeriodoResumenDTO(
    string FechaInicio,
    string FechaFin,
    decimal TotalGeneral,
    int TotalRecibos,
    List<PolizaIngresoFilaDTO> AgrupadoPorCuenta,
    List<PolizaPeriodoFilaDTO> DetalleTransacciones);

/// <summary>
/// Detailed receipt item projection for the official printable revenue report (ModalPolizaIngresos).
/// Matches the exact contract expected by frontend ReporteDetalladoItem.
/// </summary>
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
    string CajeroUsuario);

/// <summary>
/// Request contract for closing a daily revenue policy and generating official archive log.
/// </summary>
public sealed record CierrePolizaRequestDTO(
    DateOnly Fecha,
    string? Observaciones = null);

/// <summary>
/// Response confirmation of a closed daily accounting policy.
/// </summary>
public sealed record CierrePolizaResponseDTO(
    Guid Id,
    string FolioPoliza,
    DateOnly Fecha,
    decimal Total,
    int TotalRecibos,
    string GeneradoPor,
    DateTimeOffset FechaCierre);
