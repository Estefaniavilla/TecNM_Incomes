using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Caja;

[ApiController]
[Route("api/caja")]
public sealed class CajaController(CajaService cajaService) : ControllerBase
{
    // =========================================================================
    // 1. Alumnos y Control Escolar (Incluye Historial y Cambios de Matrícula)
    // =========================================================================

    [HttpGet("alumnos")]
    public async Task<ActionResult<List<AlumnoDTO>>> ListarAlumnos(
        [FromQuery] string? carrera,
        [FromQuery] int? semestre,
        [FromQuery] string? estatus,
        CancellationToken cancellationToken)
    {
        var alumnos = await cajaService.ListarAlumnosAsync(carrera, semestre, estatus, cancellationToken);
        return Ok(alumnos);
    }

    [HttpGet("alumnos/buscar")]
    public async Task<ActionResult<List<AlumnoDTO>>> BuscarAlumnos(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var resultados = await cajaService.BuscarAlumnosAsync(q, cancellationToken);
        return Ok(resultados);
    }

    [HttpGet("alumnos/{matricula}")]
    public async Task<ActionResult<AlumnoDTO>> GetAlumno(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var alumno = await cajaService.GetAlumnoAsync(matricula, cancellationToken);
        if (alumno is null)
        {
            return NotFound(new { message = $"No se encontró al alumno con matrícula '{matricula}'." });
        }

        return Ok(alumno);
    }

    [HttpGet("alumnos/{matricula}/historial-matriculas")]
    public async Task<ActionResult<List<MatriculaHistorialDTO>>> GetHistorialMatriculas(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var historial = await cajaService.GetHistorialMatriculasAsync(matricula, cancellationToken);
        return Ok(historial);
    }

    [HttpPost("alumnos/{matricula}/cambiar-matricula")]
    public async Task<ActionResult<AlumnoDTO>> CambiarMatricula(
        [FromRoute] string matricula,
        [FromBody] CambiarMatriculaRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var (usuario, _) = GetCajeroSesion();
            var resultado = await cajaService.CambiarMatriculaAsync(matricula, req, usuario, cancellationToken);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // =========================================================================
    // 2. Administración de Adeudos y Becas
    // =========================================================================

    [HttpGet("alumnos/{matricula}/adeudos")]
    public async Task<ActionResult<List<AdeudoDTO>>> GetAdeudosAlumno(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var adeudos = await cajaService.GetAdeudosAlumnoAsync(matricula, cancellationToken);
        return Ok(adeudos);
    }

    [HttpGet("adeudos")]
    public async Task<ActionResult<List<AdeudoDetalleDTO>>> ConsultarAdeudosGeneral(
        [FromQuery] string? matricula,
        [FromQuery] string? concepto,
        [FromQuery] int? periodoId,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var adeudos = await cajaService.ConsultarAdeudosGeneralAsync(matricula, concepto, periodoId, estado, cancellationToken);
        return Ok(adeudos);
    }

    [HttpGet("alumnos/{matricula}/becas")]
    public async Task<ActionResult<List<BecaDTO>>> GetBecasAlumno(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var becas = await cajaService.GetBecasAlumnoAsync(matricula, cancellationToken);
        return Ok(becas);
    }

    // =========================================================================
    // 3. Empleados/Cajeros y Cuentas de Cobro (Catálogos y Distinción)
    // =========================================================================

    [HttpGet("empleados")]
    public async Task<ActionResult<List<EmpleadoDTO>>> ListarEmpleados(CancellationToken cancellationToken)
    {
        var empleados = await cajaService.ListarEmpleadosAsync(cancellationToken);
        return Ok(empleados);
    }

    [HttpPost("empleados")]
    public async Task<ActionResult<EmpleadoDTO>> CrearEmpleado(
        [FromBody] CrearEmpleadoRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var empleado = await cajaService.CrearEmpleadoAsync(req, cancellationToken);
            return Created($"/api/caja/empleados/{empleado.Id}", empleado);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("cuentas")]
    public async Task<ActionResult<List<CuentaConceptoDTO>>> ListarCuentas(CancellationToken cancellationToken)
    {
        var cuentas = await cajaService.ListarCuentasConceptosAsync(cancellationToken);
        return Ok(cuentas);
    }

    [HttpPost("cuentas")]
    public async Task<ActionResult<CuentaConceptoDTO>> CrearCuenta(
        [FromBody] CrearCuentaConceptoRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var cuenta = await cajaService.CrearCuentaConceptoAsync(req, cancellationToken);
            return Created($"/api/caja/cuentas/{cuenta.Id}", cuenta);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("cuentas/{id:guid}")]
    public async Task<ActionResult<CuentaConceptoDTO>> ActualizarCuenta(
        [FromRoute] Guid id,
        [FromBody] ActualizarCuentaConceptoRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var cuenta = await cajaService.ActualizarCuentaConceptoAsync(id, req, cancellationToken);
            return Ok(cuenta);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // =========================================================================
    // 4. Catálogos Académicos (Carreras y Periodos) y Conceptos
    // =========================================================================

    [HttpGet("carreras")]
    public async Task<ActionResult<List<CarreraDTO>>> GetCarreras(CancellationToken cancellationToken)
    {
        var carreras = await cajaService.GetCarrerasAsync(cancellationToken);
        return Ok(carreras);
    }

    [HttpGet("periodos")]
    public async Task<ActionResult<List<PeriodoDTO>>> GetPeriodos(CancellationToken cancellationToken)
    {
        var periodos = await cajaService.GetPeriodosAsync(cancellationToken);
        return Ok(periodos);
    }

    [HttpGet("conceptos")]
    public async Task<ActionResult<List<ConceptoDTO>>> GetConceptos(CancellationToken cancellationToken)
    {
        var conceptos = await cajaService.GetConceptosAsync(cancellationToken);
        return Ok(conceptos);
    }

    // =========================================================================
    // 5. Análisis y Desglose de Referencias Bancarias (5+5)
    // =========================================================================

    [HttpGet("referencias/desglosar")]
    public async Task<ActionResult<DesgloseReferenciaDTO>> DesglosarReferencia(
        [FromQuery] string referencia,
        CancellationToken cancellationToken)
    {
        var desglose = await cajaService.DesglosarReferenciaAsync(referencia, cancellationToken);
        return Ok(desglose);
    }

    // =========================================================================
    // 6. Emisión y Cancelación de Recibos
    // =========================================================================

    [HttpPost("recibos")]
    public async Task<ActionResult<ReciboResponse>> CrearRecibo(
        [FromBody] CrearReciboRequest? request,
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
            var recibo = await cajaService.CrearReciboAsync(request, usuario, nombre, false, cancellationToken);
            return Created($"/api/caja/recibos/{recibo.NumeroRecibo}", recibo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("recibos/previo")]
    public async Task<ActionResult<ReciboResponse>> CrearReciboPrevio(
        [FromBody] CrearReciboRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Detalles == null || request.Detalles.Count == 0)
        {
            return BadRequest(new { message = "El recibo previo debe contener al menos un concepto." });
        }

        try
        {
            var (usuario, nombre) = GetCajeroSesion();
            var recibo = await cajaService.CrearReciboAsync(request, usuario, nombre, true, cancellationToken);
            return Ok(recibo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("recibos/{id:guid}/cancelar")]
    public async Task<ActionResult<ReciboResponse>> CancelarRecibo(
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
            var resultado = await cajaService.CancelarReciboAsync(id, request.Motivo, usuario, cancellationToken);
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

    [HttpGet("recibos/{numero:long}")]
    public async Task<ActionResult<ReciboResponse>> GetRecibo(
        [FromRoute] long numero,
        CancellationToken cancellationToken)
    {
        var recibo = await cajaService.GetReciboByNumeroAsync(numero, cancellationToken);
        if (recibo is null)
        {
            return NotFound(new { message = $"No se encontró el recibo número #{numero}." });
        }

        return Ok(recibo);
    }

    [HttpGet("recibos/recientes")]
    public async Task<ActionResult<List<ReciboResponse>>> GetRecibosRecientes(
        [FromQuery] int limite = 15,
        CancellationToken cancellationToken = default)
    {
        var maxLimit = Math.Clamp(limite, 1, 50);
        var recibos = await cajaService.GetRecibosRecientesAsync(maxLimit, cancellationToken);
        return Ok(recibos);
    }

    // =========================================================================
    // 7. Corte Diario, Pólizas y Reportes (Para Modales del Frontend)
    // =========================================================================

    [HttpGet("corte")]
    public async Task<ActionResult<CorteCajaResponse>> GetCorteDiario(
        [FromQuery] DateOnly? fecha,
        CancellationToken cancellationToken)
    {
        var corte = await cajaService.GetCorteDiarioAsync(fecha, null, cancellationToken);
        return Ok(corte);
    }

    [HttpGet("reportes/detallado")]
    public async Task<ActionResult<List<ReporteReciboDetalladoDTO>>> GetReporteDetallado(
        CancellationToken cancellationToken)
    {
        var reporte = await cajaService.GetReporteDetalladoAsync(cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("polizas/resumen")]
    public async Task<ActionResult<PolizaIngresoResumenDTO>> GetPolizaResumen(
        [FromQuery] string? fecha,
        CancellationToken cancellationToken)
    {
        var resumen = await cajaService.GetPolizaResumenAsync(fecha, cancellationToken);
        return Ok(resumen);
    }

    [HttpGet("reportes/ingresos")]
    public async Task<ActionResult<ReporteIngresoResumenDTO>> GetReporteIngresos(
        [FromQuery] string? fecha,
        CancellationToken cancellationToken)
    {
        var reporte = await cajaService.GetReporteIngresosAsync(fecha, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("reportes/verano")]
    public async Task<ActionResult<ReporteVeranoResumenDTO>> GetReporteVerano(
        [FromQuery] string? inicio,
        [FromQuery] string? fin,
        [FromQuery] string? concepto,
        CancellationToken cancellationToken)
    {
        var reporte = await cajaService.GetReporteVeranoAsync(inicio, fin, concepto, cancellationToken);
        return Ok(reporte);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

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
