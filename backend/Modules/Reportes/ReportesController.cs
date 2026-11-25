using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Reportes;

/// <summary>
/// REST API Controller handling institutional document exports (PDF, CSV, Word)
/// for revenue policies, debtors roster, income reports, and official cash receipts.
/// </summary>
[ApiController]
[Route("api/reportes")]
[Route("api/caja/reportes")]
[AllowAnonymous]
public sealed class ReportesController(
    ReportesService reportesService,
    ILogger<ReportesController> logger) : ControllerBase
{
    /// <summary>
    /// Exports Revenue Policy (Póliza de Ingresos) in PDF, CSV, or Word format.
    /// GET /api/reportes/poliza/exportar?fechaInicio=2026-10-01&fechaFin=2026-10-31&modo=cuentas&formato=pdf
    /// </summary>
    [HttpGet("poliza/exportar")]
    [HttpPost("poliza/exportar")]
    public async Task<IActionResult> ExportarPoliza(
        [FromQuery] string? fechaInicio,
        [FromQuery] string? fechaFin,
        [FromQuery] string? formaPago,
        [FromQuery] string? cuentaContable,
        [FromQuery] string? modo = "cuentas",
        [FromQuery] string formato = "pdf",
        CancellationToken cancellationToken = default)
    {
        var filter = new ReportePolizaFilterDTO(
            FechaInicio: fechaInicio,
            FechaFin: fechaFin,
            FormaPago: formaPago,
            CuentaContable: cuentaContable,
            Modo: modo,
            Formato: formato);

        var result = await reportesService.ExportarPolizaAsync(filter, cancellationToken);
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports Student Debtors Report (Reporte de Deudores) in PDF, CSV, or Word format.
    /// GET /api/reportes/deudores/exportar?periodo=2026-1&carreraId=1&formato=csv
    /// </summary>
    [HttpGet("deudores/exportar")]
    [HttpPost("deudores/exportar")]
    public async Task<IActionResult> ExportarDeudores(
        [FromQuery] string? periodo,
        [FromQuery] int? carreraId,
        [FromQuery] string? fechaCorte,
        [FromQuery] bool soloInscritos = false,
        [FromQuery] string formato = "pdf",
        CancellationToken cancellationToken = default)
    {
        var filter = new ReporteDeudoresFilterDTO(
            Periodo: periodo,
            CarreraId: carreraId,
            FechaCorte: fechaCorte,
            SoloInscritos: soloInscritos,
            Formato: formato);

        var result = await reportesService.ExportarDeudoresAsync(filter, cancellationToken);
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports Income Statement (Reporte de Ingresos) in PDF, CSV, or Word format.
    /// GET /api/reportes/ingresos/exportar?fechaInicio=2026-10-01&fechaFin=2026-10-31&formato=word
    /// </summary>
    [HttpGet("ingresos/exportar")]
    [HttpPost("ingresos/exportar")]
    public async Task<IActionResult> ExportarIngresos(
        [FromQuery] string? fechaInicio,
        [FromQuery] string? fechaFin,
        [FromQuery] string? categoria,
        [FromQuery] string? cajero,
        [FromQuery] string formato = "pdf",
        CancellationToken cancellationToken = default)
    {
        var filter = new ReporteIngresosFilterDTO(
            FechaInicio: fechaInicio,
            FechaFin: fechaFin,
            Categoria: categoria,
            Cajero: cajero,
            Formato: formato);

        var result = await reportesService.ExportarIngresosAsync(filter, cancellationToken);
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports Individual Official Cash Receipt (Recibo Oficial) in PDF, CSV, or Word format.
    /// GET /api/reportes/recibos/{folio}/exportar?formato=pdf
    /// </summary>
    [HttpGet("recibos/{folio:long}/exportar")]
    public async Task<IActionResult> ExportarRecibo(
        [FromRoute] long folio,
        [FromQuery] string formato = "pdf",
        CancellationToken cancellationToken = default)
    {
        var result = await reportesService.ExportarReciboAsync(folio, formato, cancellationToken);
        return File(result.FileBytes, result.ContentType, result.FileName);
    }
}
