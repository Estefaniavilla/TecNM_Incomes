using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Recibos;

/// <summary>
/// RESTful Controller for Receipt Issuance, Payments, and Cancellation Auditing.
/// Follows Thin Controller architecture, delegating domain business logic to RecibosService.
/// </summary>
[ApiController]
[Route("api/recibos")]
[Route("api/caja/recibos")]
[Produces("application/json")]
public sealed class RecibosController(RecibosService recibosService) : ControllerBase
{
    /// <summary>
    /// Retrieves paginated list of receipts with optional text search and status/method filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReciboResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReciboResponseDTO>>> GetPaged(
        [FromQuery] ReciboFilterParams filters,
        CancellationToken cancellationToken)
    {
        var result = await recibosService.GetPagedAsync(filters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the most recent receipts for cashier quick view.
    /// </summary>
    [HttpGet("recientes")]
    [ProducesResponseType(typeof(IReadOnlyList<ReciboResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReciboResponseDTO>>> GetRecent(
        [FromQuery] int limite = 15,
        CancellationToken cancellationToken = default)
    {
        var result = await recibosService.GetRecentAsync(limite, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a receipt by its consecutive receipt number.
    /// </summary>
    [HttpGet("{numero:long}")]
    [ProducesResponseType(typeof(ReciboResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReciboResponseDTO>> GetByNumero(
        [FromRoute] long numero,
        CancellationToken cancellationToken)
    {
        var recibo = await recibosService.GetByNumeroAsync(numero, cancellationToken);
        if (recibo is null)
        {
            return NotFound(new { message = $"No se encontró el recibo número #{numero}." });
        }

        return Ok(recibo);
    }

    /// <summary>
    /// Retrieves full details of a receipt by its unique UUID identifier.
    /// </summary>
    [HttpGet("id/{id:guid}")]
    [ProducesResponseType(typeof(ReciboResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReciboResponseDTO>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var recibo = await recibosService.GetByIdAsync(id, cancellationToken);
        if (recibo is null)
        {
            return NotFound(new { message = $"No se encontró el recibo con ID '{id}'." });
        }

        return Ok(recibo);
    }

    /// <summary>
    /// Issues an official definitive receipt, generating fiscal folios and amortizing matching student debt.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReciboResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReciboResponseDTO>> Create(
        [FromBody] CreateReciboRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Detalles == null || request.Detalles.Count == 0)
        {
            return BadRequest(new { message = "El recibo debe contener al menos un concepto de cobro." });
        }

        if (string.IsNullOrWhiteSpace(request.Matricula) || string.IsNullOrWhiteSpace(request.NombreCompleto))
        {
            return BadRequest(new { message = "La matrícula y el nombre del estudiante son obligatorios." });
        }

        try
        {
            var (usuario, nombre) = GetCajeroSesion();
            var recibo = await recibosService.CreateReciboAsync(request, usuario, nombre, false, cancellationToken);
            return CreatedAtAction(nameof(GetByNumero), new { numero = recibo.NumeroRecibo }, recibo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generates a preview receipt without allocating definitive consecutive folios.
    /// </summary>
    [HttpPost("previo")]
    [ProducesResponseType(typeof(ReciboResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReciboResponseDTO>> CreatePreview(
        [FromBody] CreateReciboRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Detalles == null || request.Detalles.Count == 0)
        {
            return BadRequest(new { message = "El recibo previo debe contener al menos un concepto." });
        }

        try
        {
            var (usuario, nombre) = GetCajeroSesion();
            var recibo = await recibosService.CreateReciboAsync(request, usuario, nombre, true, cancellationToken);
            return Ok(recibo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels an issued receipt with mandatory audit justification.
    /// </summary>
    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType(typeof(ReciboResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReciboResponseDTO>> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelarReciboRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Motivo))
        {
            return BadRequest(new { message = "Debe proporcionar una justificación para cancelar el recibo." });
        }

        try
        {
            var (usuario, _) = GetCajeroSesion();
            var resultado = await recibosService.CancelReciboAsync(id, request.Motivo, usuario, cancellationToken);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels an issued receipt by its integer receipt number.
    /// </summary>
    [HttpPost("{numero:long}/cancelar")]
    [HttpPost("numero/{numero:long}/cancelar")]
    [ProducesResponseType(typeof(ReciboResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReciboResponseDTO>> CancelByNumero(
        [FromRoute] long numero,
        [FromBody] CancelarReciboRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Motivo))
        {
            return BadRequest(new { message = "Debe proporcionar una justificación para cancelar el recibo." });
        }

        try
        {
            var (usuario, _) = GetCajeroSesion();
            var resultado = await recibosService.CancelReciboByNumeroAsync(numero, request.Motivo, usuario, cancellationToken);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Computes and returns the financial closing (corte de caja) for the current shift.
    /// Deducts cancelled receipts and itemizes income by payment channel (cash vs bank).
    /// </summary>
    [HttpGet("corte-turno")]
    [ProducesResponseType(typeof(CorteTurnoResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<CorteTurnoResponseDTO>> GetCorteTurno(
        [FromQuery] DateOnly? fecha,
        CancellationToken cancellationToken)
    {
        var (usuario, _) = GetCajeroSesion();
        var resumen = await recibosService.GetCorteTurnoAsync(fecha, usuario, cancellationToken);
        return Ok(resumen);
    }

    /// <summary>
    /// Exports the full receipt voucher file with official fiscal layout for downloading.
    /// </summary>
    [HttpGet("{numero:long}/exportar")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportarRecibo(
        [FromRoute] long numero,
        CancellationToken cancellationToken)
    {
        var file = await recibosService.ExportarReciboAsync(numero, cancellationToken);
        if (file is null)
        {
            return NotFound(new { message = $"No se encontró el recibo número #{numero}." });
        }

        return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    /// <summary>
    /// Retrieves fiscal audit trail of all cancelled receipts with reasons and supervisor/cashier metadata.
    /// </summary>
    [HttpGet("auditoria/cancelaciones")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditoriaCancelacionDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditoriaCancelacionDTO>>> GetAuditoriaCancelaciones(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var auditoria = await recibosService.GetAuditoriaCancelacionesAsync(desde, hasta, cancellationToken);
        return Ok(auditoria);
    }

    /// <summary>
    /// Quick status check endpoint for receipts (useful for UI state synchronizations).
    /// </summary>
    [HttpGet("estatus/{numero:long}")]
    [ProducesResponseType(typeof(ReciboEstadoResumenDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReciboEstadoResumenDTO>> GetEstado(
        [FromRoute] long numero,
        CancellationToken cancellationToken)
    {
        var resumen = await recibosService.GetEstadoResumenAsync(numero, cancellationToken);
        if (resumen is null)
        {
            return NotFound(new { message = $"No se encontró el recibo #{numero}." });
        }

        return Ok(resumen);
    }

    private (string usuario, string nombre) GetCajeroSesion()
    {
        var usuario = User.FindFirst("name")?.Value
            ?? User.Identity?.Name
            ?? "cajero1";

        var nombre = User.FindFirst("fullName")?.Value
            ?? "Cajero Turno Matutino";

        return (usuario, nombre);
    }
}
