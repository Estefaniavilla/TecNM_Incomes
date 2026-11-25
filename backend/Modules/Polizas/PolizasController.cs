using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Polizas;

/// <summary>
/// RESTful Controller for Daily Revenue Policies, Accounting Journals, and Financial Ledgers.
/// Implements dual routing for clean architecture and 100% frontend backwards compatibility.
/// </summary>
[ApiController]
[Route("api/polizas")]
[Route("api/caja/polizas")]
[Produces("application/json")]
public sealed class PolizasController(PolizasService polizasService) : ControllerBase
{
    /// <summary>
    /// Retrieves daily revenue policy summary grouped by accounting accounts and concepts.
    /// Powers ModalPolizaIngresos.vue and CajaPrincipalView financial reports.
    /// </summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(PolizaIngresoResumenDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolizaIngresoResumenDTO>> GetPolizaResumen(
        [FromQuery] string? fecha,
        CancellationToken cancellationToken)
    {
        var resumen = await polizasService.GetPolizaDiariaResumenAsync(fecha, cancellationToken);
        return Ok(resumen);
    }

    /// <summary>
    /// Retrieves revenue policy for a multi-day period with account groupings and chronological breakdown.
    /// Powers ModalPolizaIngresoPeriodo.vue.
    /// </summary>
    [HttpGet("periodo")]
    [ProducesResponseType(typeof(PolizaPeriodoResumenDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolizaPeriodoResumenDTO>> GetPolizaPeriodo(
        [FromQuery] string? inicio,
        [FromQuery] string? fin,
        [FromQuery] string? cuenta,
        CancellationToken cancellationToken)
    {
        var resultado = await polizasService.GetPolizaPeriodoResumenAsync(inicio, fin, cuenta, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Retrieves detailed line items across all receipts for the official printable ledger.
    /// Supports optional query parameters for date range, payment method, and account/concept search.
    /// Dual routed to /api/caja/reportes/detallado for seamless frontend integration.
    /// </summary>
    [HttpGet("detallado")]
    [HttpGet("/api/caja/reportes/detallado")]
    [ProducesResponseType(typeof(List<ReporteReciboDetalladoDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReporteReciboDetalladoDTO>>> GetReporteDetallado(
        [FromQuery] string? inicio,
        [FromQuery] string? fin,
        [FromQuery] string? metodoPago,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var reporte = await polizasService.GetMovimientosPolizaDetalladoAsync(inicio, fin, metodoPago, q, cancellationToken);
        return Ok(reporte);
    }

    /// <summary>
    /// Officially closes daily operations and records the revenue policy folio.
    /// </summary>
    [HttpPost("cerrar")]
    [ProducesResponseType(typeof(CierrePolizaResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<CierrePolizaResponseDTO>> CerrarPoliza(
        [FromBody] CierrePolizaRequestDTO request,
        CancellationToken cancellationToken)
    {
        var user = User.FindFirst("name")?.Value ?? User.Identity?.Name ?? "cajero1";
        var resultado = await polizasService.CerrarPolizaDiariaAsync(request, user, cancellationToken);
        return Ok(resultado);
    }
}
