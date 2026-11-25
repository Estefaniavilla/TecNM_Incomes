using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Alumnos;

/// <summary>
/// RESTful Controller for TecNM Student Management, Indexed Search Autocomplete,
/// Batch-of-5 UI Pagination, and School Services Roster Integration.
/// Implements dual routing for clean architecture and 100% frontend backwards compatibility.
/// </summary>
[ApiController]
[Route("api/alumnos")]
[Route("api/caja/alumnos")]
[Produces("application/json")]
public sealed class AlumnosController(AlumnosService alumnosService) : ControllerBase
{
    /// <summary>
    /// Lists students with batch-of-5 pagination, career, semester, and status filters.
    /// Matches the UI pagination requirements from PR #11.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedAlumnosResult<AlumnoDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAlumnosResult<AlumnoDTO>>> GetPaged(
        [FromQuery] AlumnoFilterParams filters,
        CancellationToken cancellationToken)
    {
        var result = await alumnosService.GetPagedAsync(filters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Real-time live search endpoint for StudentSearchDropdown autocomplete.
    /// Searches across matricula, full name, and ficha number over the 14,000+ student roster.
    /// </summary>
    [HttpGet("buscar")]
    [ProducesResponseType(typeof(List<AlumnoDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AlumnoDTO>>> BuscarAlumnos(
        [FromQuery] string? q,
        [FromQuery] int? limite,
        CancellationToken cancellationToken)
    {
        var resultados = await alumnosService.BuscarAlumnosAsync(q, limite, cancellationToken);
        return Ok(resultados);
    }

    /// <summary>
    /// Retrieves active engineering careers without duplicates and excluding legacy Licenciaturas.
    /// Dual routed to /api/caja/carreras for seamless frontend dropdown integration.
    /// </summary>
    [HttpGet("carreras")]
    [HttpGet("/api/caja/carreras")]
    [ProducesResponseType(typeof(List<CarreraCatalogoDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CarreraCatalogoDTO>>> GetCarreras(CancellationToken cancellationToken)
    {
        var carreras = await alumnosService.GetCarrerasAsync(cancellationToken);
        return Ok(carreras);
    }

    /// <summary>
    /// Retrieves a single student record by enrollment number (Matricula), including previous matriculas.
    /// </summary>
    [HttpGet("{matricula}")]
    [ProducesResponseType(typeof(AlumnoDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlumnoDTO>> GetAlumno(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var alumno = await alumnosService.GetAlumnoAsync(matricula, cancellationToken);
        if (alumno is null)
        {
            return NotFound(new { message = $"No se encontró al alumno con matrícula '{matricula}'." });
        }

        return Ok(alumno);
    }

    /// <summary>
    /// Retrieves historical matricula reassignments for the specified student.
    /// </summary>
    [HttpGet("{matricula}/historial-matriculas")]
    [ProducesResponseType(typeof(List<MatriculaHistorialDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MatriculaHistorialDTO>>> GetHistorialMatriculas(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var historial = await alumnosService.GetHistorialMatriculasAsync(matricula, cancellationToken);
        return Ok(historial);
    }

    /// <summary>
    /// Updates or reassigns an enrollment number while logging audit history.
    /// </summary>
    [HttpPost("{matricula}/cambiar-matricula")]
    [ProducesResponseType(typeof(AlumnoDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlumnoDTO>> CambiarMatricula(
        [FromRoute] string matricula,
        [FromBody] CambiarMatriculaRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = User.FindFirst("name")?.Value ?? User.Identity?.Name ?? "cajero1";
            var resultado = await alumnosService.CambiarMatriculaAsync(matricula, req, user, cancellationToken);
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

    /// <summary>
    /// Retrieves pending debts for a specific student.
    /// </summary>
    [HttpGet("{matricula}/adeudos")]
    [ProducesResponseType(typeof(List<AdeudoDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdeudoDTO>>> GetAdeudosAlumno(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var adeudos = await alumnosService.GetAdeudosAlumnoAsync(matricula, cancellationToken);
        return Ok(adeudos);
    }

    /// <summary>
    /// Retrieves active scholarships and percentage discounts for a student.
    /// </summary>
    [HttpGet("{matricula}/becas")]
    [ProducesResponseType(typeof(List<BecaDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BecaDTO>>> GetBecasAlumno(
        [FromRoute] string matricula,
        CancellationToken cancellationToken)
    {
        var becas = await alumnosService.GetBecasAlumnoAsync(matricula, cancellationToken);
        return Ok(becas);
    }

    /// <summary>
    /// Registers or synchronizes a student record from Academic Services.
    /// </summary>
    [HttpPost("inscribir")]
    [ProducesResponseType(typeof(AlumnoDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlumnoDTO>> InscribirAlumno(
        [FromBody] InscribirAlumnoRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = User.FindFirst("name")?.Value ?? User.Identity?.Name ?? "admin";
            var result = await alumnosService.InscribirOActualizarAlumnoAsync(req, user, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
