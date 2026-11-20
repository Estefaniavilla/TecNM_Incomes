using System.ComponentModel.DataAnnotations;

namespace TecNM.Api.Modules.ConceptosCobro;

/// <summary>
/// Data Transfer Object representing a payment concept response.
/// </summary>
public sealed record ConceptoCobroResponseDTO(
    Guid Id,
    string Clave,
    string Descripcion,
    decimal CuotaBase,
    int? Orden,
    string? CuentaReferencia,
    bool AplicaBeca,
    string Categoria,
    bool IsActive
);

/// <summary>
/// Request payload for creating a new payment concept.
/// </summary>
public sealed record CreateConceptoCobroRequest(
    [Required(ErrorMessage = "The concept code (Clave) is required.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Clave must be between 2 and 30 characters.")]
    string Clave,

    [Required(ErrorMessage = "The description is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Description must be between 3 and 200 characters.")]
    string Descripcion,

    [Range(0.00, 99999999.9999, ErrorMessage = "Base fee must be greater than or equal to 0.")]
    decimal CuotaBase,

    int? Orden,

    [StringLength(30, ErrorMessage = "Reference account cannot exceed 30 characters.")]
    string? CuentaReferencia,

    bool AplicaBeca,

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 50 characters.")]
    string Categoria
);

/// <summary>
/// Request payload for updating an existing payment concept.
/// </summary>
public sealed record UpdateConceptoCobroRequest(
    [Required(ErrorMessage = "The description is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Description must be between 3 and 200 characters.")]
    string Descripcion,

    [Range(0.00, 99999999.9999, ErrorMessage = "Base fee must be greater than or equal to 0.")]
    decimal CuotaBase,

    int? Orden,

    [StringLength(30, ErrorMessage = "Reference account cannot exceed 30 characters.")]
    string? CuentaReferencia,

    bool AplicaBeca,

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 50 characters.")]
    string Categoria,

    bool IsActive
);

/// <summary>
/// Query parameters for filtering and paginating payment concepts.
/// </summary>
public sealed class ConceptoCobroFilterParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public string? Search { get; set; }
    public string? Categoria { get; set; }
    public bool? IsActive { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : Math.Max(1, value);
    }
}

/// <summary>
/// Generic container for paginated query results.
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
