using System.ComponentModel.DataAnnotations;

namespace TecNM.Api.Modules.Recibos;

/// <summary>
/// Line item payload representing a payment concept in a receipt creation request.
/// </summary>
public sealed record DetalleReciboItemRequest(
    Guid? ConceptoId,

    [Required(ErrorMessage = "Concept description is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Description must be between 3 and 200 characters.")]
    string Descripcion,

    [Range(1, 999, ErrorMessage = "Quantity must be at least 1.")]
    int Cantidad,

    [Range(0.00, 99999999.9999, ErrorMessage = "Unit fee must be greater than or equal to 0.")]
    decimal CuotaUnitario,

    [Range(0.00, 99999999.9999, ErrorMessage = "Scholarship discount must be greater than or equal to 0.")]
    decimal DescuentoBeca,

    [Range(0.00, 99999999.9999, ErrorMessage = "Line total must be greater than or equal to 0.")]
    decimal ImporteTotal,

    string? PeriodoAno
);

/// <summary>
/// Request payload for creating a new official receipt.
/// </summary>
public sealed record CreateReciboRequest(
    [Required(ErrorMessage = "Student matricula is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Matricula must be between 3 and 20 characters.")]
    string Matricula,

    [Required(ErrorMessage = "Student full name is required.")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 160 characters.")]
    string NombreCompleto,

    [Required(ErrorMessage = "Degree program (Carrera) is required.")]
    string Carrera,

    [Range(1, 14, ErrorMessage = "Semester must be between 1 and 14.")]
    int Semestre,

    [Required(ErrorMessage = "Shift (Turno) is required.")]
    string Turno,

    [Required(ErrorMessage = "Payment method is required.")]
    string MetodoPago,

    string? ReferenciaBancaria,
    string? CuentaBancaria,
    DateTimeOffset? FechaBancaria,
    DateTimeOffset? FechaOperacion,
    string? JustificacionFecha,

    [Required(ErrorMessage = "Receipt must contain at least one line item.")]
    [MinLength(1, ErrorMessage = "Receipt must contain at least one line item.")]
    List<DetalleReciboItemRequest> Detalles
);

/// <summary>
/// Request payload for cancelling an existing receipt.
/// </summary>
public sealed record CancelarReciboRequest(
    [Required(ErrorMessage = "Cancellation justification / reason is mandatory.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Reason must contain between 5 and 300 characters.")]
    string Motivo
);

/// <summary>
/// Response DTO representing an individual concept item in a receipt.
/// </summary>
public sealed record DetalleReciboResponseDTO(
    Guid Id,
    Guid? ConceptoId,
    string Descripcion,
    int Cantidad,
    decimal CuotaUnitario,
    decimal DescuentoBeca,
    decimal ImporteTotal,
    string? PeriodoAno
);

/// <summary>
/// Response DTO representing full receipt details.
/// </summary>
public sealed record ReciboResponseDTO(
    Guid Id,
    long NumeroRecibo,
    string FolioAdministrativo,
    DateTimeOffset FechaOperacion,
    DateTimeOffset? FechaEfectivaPago,
    string Matricula,
    string NombreCompleto,
    string Carrera,
    int Semestre,
    string Turno,
    string MetodoPago,
    string? ReferenciaBancaria,
    string? CuentaBancaria,
    decimal Subtotal,
    decimal DescuentoTotal,
    decimal Total,
    short Status,
    string Estado,
    string TipoRecibo,
    string? MotivoCancelacion,
    DateTimeOffset? FechaCancelacion,
    string CajeroUsuario,
    string CajeroNombre,
    IReadOnlyList<DetalleReciboResponseDTO> Detalles
);

/// <summary>
/// Query filter parameters for searching and paginating receipts.
/// </summary>
public sealed class ReciboFilterParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public string? Search { get; set; }
    public string? Estado { get; set; }
    public string? MetodoPago { get; set; }
    public DateTimeOffset? FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : Math.Max(1, value);
    }
}

/// <summary>
/// Generic paginated result container for receipts queries.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
)
{
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Domain DTO representing daily/shift financial closing (corte de caja) and audit balance.
/// </summary>
public sealed record CorteTurnoResponseDTO(
    DateOnly Fecha,
    string? CajeroUsuario,
    int TotalEmitidos,
    int TotalActivos,
    int TotalCancelados,
    decimal TotalCobradoEfectivo,
    decimal TotalCobradoBanco,
    decimal TotalIngresosNetos,
    decimal TotalCanceladoImporte,
    DateTimeOffset GeneradoEn
);

/// <summary>
/// Domain DTO representing an audit trail entry for receipt cancellations.
/// </summary>
public sealed record AuditoriaCancelacionDTO(
    Guid ReciboId,
    long NumeroRecibo,
    string FolioAdministrativo,
    string AlumnoMatricula,
    string AlumnoNombre,
    decimal Total,
    string MetodoPago,
    DateTimeOffset FechaOperacion,
    DateTimeOffset? FechaCancelacion,
    string? MotivoCancelacion,
    string CajeroUsuario
);

/// <summary>
/// Lightweight DTO for quick receipt status lookups and verification.
/// </summary>
public sealed record ReciboEstadoResumenDTO(
    long NumeroRecibo,
    string Estado,
    string TipoRecibo,
    DateTimeOffset FechaOperacion,
    DateTimeOffset? FechaCancelacion,
    decimal Total
);
