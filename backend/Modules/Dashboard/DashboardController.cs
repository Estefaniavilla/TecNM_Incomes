using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Dashboard;

/// <summary>
/// RESTful Controller for Role-Scoped Cash Register Dashboard Indicators and PDF Export.
/// Automatically resolves user role and shift from JWT claims (or optional test overrides for Admin).
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Route("api/caja/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Retrieves real-time KPIs, charts, and recent receipts scoped to the logged-in role/shift.
    /// </summary>
    [HttpGet("indicadores")]
    [ProducesResponseType(typeof(DashboardIndicadoresDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardIndicadoresDTO>> GetIndicadores(
        [FromQuery] string periodo = "hoy",
        [FromQuery] string? role = null,
        [FromQuery] string? username = null,
        CancellationToken cancellationToken = default)
    {
        var (resolvedUser, resolvedName, resolvedRole) = ResolveUserContext(role, username);
        var result = await dashboardService.GetIndicadoresAsync(
            periodo,
            resolvedUser,
            resolvedName,
            resolvedRole,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Generates and streams an official PDF report for the current role-scoped dashboard cut.
    /// </summary>
    [HttpGet("exportar-pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ExportarPdf(
        [FromQuery] string periodo = "hoy",
        [FromQuery] string? role = null,
        [FromQuery] string? username = null,
        CancellationToken cancellationToken = default)
    {
        var (resolvedUser, resolvedName, resolvedRole) = ResolveUserContext(role, username);
        var pdfBytes = await dashboardService.ExportarCortePdfAsync(
            periodo,
            resolvedUser,
            resolvedName,
            resolvedRole,
            cancellationToken);

        var fileName = $"Corte_Caja_{resolvedUser}_{periodo}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private (string Username, string DisplayName, string Role) ResolveUserContext(
        string? queryRole,
        string? queryUsername)
    {
        var claimUser = User.FindFirst("username")?.Value ?? User.Identity?.Name;
        var claimName = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? User.FindFirst("name")?.Value;
        var claimRole = User.FindFirst("role")?.Value;

        var resolvedRole = !string.IsNullOrWhiteSpace(claimRole)
            ? claimRole
            : (!string.IsNullOrWhiteSpace(queryRole) ? queryRole : "Admin");

        var resolvedUser = !string.IsNullOrWhiteSpace(claimUser)
            ? claimUser
            : (!string.IsNullOrWhiteSpace(queryUsername) ? queryUsername : "admin");

        var resolvedName = !string.IsNullOrWhiteSpace(claimName)
            ? claimName
            : resolvedUser switch
            {
                "cajero1" => "Cajero Turno Matutino",
                "cajero2" => "Cajero Turno Nocturno",
                _ => "Administrador General"
            };

        return (resolvedUser, resolvedName, resolvedRole);
    }
}

