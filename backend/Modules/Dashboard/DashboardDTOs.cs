namespace TecNM.Api.Modules.Dashboard;

/// <summary>
/// Payment method breakdown item for the dashboard donut chart.
/// </summary>
public sealed record MetodoPagoDesgloseDTO(
    string Key,
    string Nombre,
    decimal Monto,
    int Porcentaje,
    string Color);

/// <summary>
/// Weekly revenue bar chart item (Monday to Saturday).
/// </summary>
public sealed record DiaBarraDTO(
    string Dia,
    string DiaCorto,
    decimal Monto,
    string EtiquetaK,
    int PorcentajeAltura,
    bool EsDestacado);

/// <summary>
/// Top revenue concept progress bar item.
/// </summary>
public sealed record ConceptoProgresoDTO(
    string Nombre,
    decimal Monto,
    int Porcentaje);

/// <summary>
/// Recent receipt row for the dashboard table with real student enrollment and name.
/// </summary>
public sealed record ReciboDashboardItemDTO(
    Guid Id,
    string Folio,
    string Matricula,
    string Alumno,
    string MetodoPago,
    decimal Monto,
    string Estado,
    DateTimeOffset FechaOperacion,
    string CajeroUsuario);

/// <summary>
/// Complete role-scoped dashboard indicators payload calculated in C#.
/// </summary>
public sealed record DashboardIndicadoresDTO(
    string UsuarioActual,
    string RolActual,
    string ScopeLabel,
    string Periodo,
    string FechaReferencia,
    decimal RecaudacionTotal,
    decimal MetaObjetivo,
    int TotalRecibosEmitidos,
    string UltimoFolio,
    decimal PromedioPorRecibo,
    int TotalAlumnosAtendidos,
    int PorcentajeAlCorriente,
    int PorcentajeConAdeudo,
    decimal AdeudosRecuperadosMonto,
    int AlumnosAdeudoRecuperado,
    List<MetodoPagoDesgloseDTO> MetodosPago,
    List<DiaBarraDTO> TendenciaSemanal,
    decimal PromedioSemanal,
    List<ConceptoProgresoDTO> ConceptosTop,
    List<ReciboDashboardItemDTO> UltimosRecibos);

