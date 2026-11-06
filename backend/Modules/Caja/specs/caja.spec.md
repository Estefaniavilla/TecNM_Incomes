# Spec backend: Módulo de Control de Caja

## 1. Alcance y Capacidades
- **Gestión y Auditoría de Empleados/Cajeros:** Registro y consulta de empleados y cajeros responsables de operaciones, asociando cada recibo, corte y cancelación a su identidad de sesión.
- **Catálogo de Cuentas y Conceptos Contables:** Administración de conceptos de cobro con clave contable (`5200-...`), descripción y costos oficiales, distinguiéndolos de las cuentas de usuario y bancarias.
- **Historial y Trazabilidad de Matrículas:** Manejo de cambios de matrícula en alumnos (`alumno_matriculas_historial`), manteniendo la relación histórica para búsquedas tanto por matrícula previa como actual.
- **Consulta y Listado Maestro de Alumnos:** Localización de estudiantes por matrícula (actual o histórica), nombre completo o ficha de admisión, con estado académico y becas.
- **Flexibilidad Administrativa de Fechas:** Emisión de recibos con fecha generada automáticamente y soporte de fecha modificada con justificación administrativa.
- **Desglose Inteligente de Referencia Bancaria (5+5):** Resolución de depósitos donde los primeros 5 dígitos identifican la cuenta/estudiante y los últimos 5 el concepto de cobro; detección y advertencia de referencias repetidas.
- **Administración Integral de Adeudos:** Consulta de adeudos por estudiante, concepto (ej. «Pago de Inscripción») y periodos académicos múltiples; liquidación automática al cobrar recibos.
- **Emisión, Impresión y Cancelación Justificada de Recibos:** Manejo de recibos previos y definitivos, almacenamiento contable en `movimientos_pago`, y bloqueo con motivo auditable.
- **Pólizas de Ingresos y Reportes Especializados:** Endpoints específicos para alimentar los modales de Póliza de Ingresos, Reporte de Ingresos Diario y Cursos de Verano.

## 2. Endpoints

### 2.1. Alumnos y Matrículas
- `GET /api/caja/alumnos` -> `200 OK [AlumnoDTO]` (con filtros opcionales de búsqueda y carrera)
- `GET /api/caja/alumnos/buscar?q={query}` -> `200 OK [AlumnoDTO]` (busca en matrícula actual e histórica)
- `GET /api/caja/alumnos/{matricula}` -> `200 OK AlumnoDTO` | `404 Not Found`
- `GET /api/caja/alumnos/{matricula}/historial-matriculas` -> `200 OK [MatriculaHistorialDTO]`
- `POST /api/caja/alumnos/{matricula}/cambiar-matricula` -> `200 OK AlumnoDTO` | `400 Bad Request`

### 2.2. Adeudos y Becas
- `GET /api/caja/alumnos/{matricula}/adeudos` -> `200 OK [AdeudoDTO]`
- `GET /api/caja/alumnos/{matricula}/becas` -> `200 OK [BecaDTO]`
- `GET /api/caja/adeudos?concepto={concepto}&matricula={matricula}&periodoId={id}&estado={estado}` -> `200 OK [AdeudoDetalleDTO]`

### 2.3. Cuentas y Empleados (Catálogos)
- `GET /api/caja/empleados` -> `200 OK [EmpleadoDTO]`
- `POST /api/caja/empleados` -> `201 Created EmpleadoDTO` | `400 Bad Request`
- `GET /api/caja/cuentas` -> `200 OK [CuentaConceptoDTO]`
- `POST /api/caja/cuentas` -> `201 Created CuentaConceptoDTO` | `400 Bad Request`
- `PUT /api/caja/cuentas/{id}` -> `200 OK CuentaConceptoDTO` | `404 Not Found`

### 2.4. Catálogos Académicos Oficiales
- `GET /api/caja/carreras` -> `200 OK [CarreraDTO]`
- `GET /api/caja/periodos` -> `200 OK [PeriodoDTO]`
- `GET /api/caja/conceptos` -> `200 OK [ConceptoDTO]`

### 2.5. Análisis de Referencia Bancaria (5+5)
- `GET /api/caja/referencias/desglosar?referencia={referencia}` -> `200 OK DesgloseReferenciaDTO`

### 2.6. Recibos, Cobros y Cancelación
- `POST /api/caja/recibos` -> `201 Created ReciboResponse` | `400 Bad Request`
- `POST /api/caja/recibos/previo` -> `200 OK ReciboResponse` | `400 Bad Request`
- `POST /api/caja/recibos/{id}/cancelar` -> `200 OK ReciboResponse` | `400 Bad Request` | `404 Not Found`
- `GET /api/caja/recibos/{numero}` -> `200 OK ReciboResponse` | `404 Not Found`
- `GET /api/caja/recibos/recientes` -> `200 OK [ReciboResponse]`

### 2.7. Corte, Pólizas y Reportes (para Modales)
- `GET /api/caja/corte` -> `200 OK CorteCajaResponse`
- `GET /api/caja/reportes/detallado` -> `200 OK [ReporteReciboDetalladoDTO]`
- `GET /api/caja/polizas/resumen?fecha={YYYY-MM-DD}` -> `200 OK PolizaIngresoResumenDTO`
- `GET /api/caja/reportes/ingresos?fecha={YYYY-MM-DD}` -> `200 OK ReporteIngresoResumenDTO`
- `GET /api/caja/reportes/verano?inicio={YYYY-MM-DD}&fin={YYYY-MM-DD}&concepto={nombre}` -> `200 OK ReporteVeranoResumenDTO`

## 3. Reglas de Negocio
1. **Identificación de Responsable:** Todo recibo, póliza o movimiento de pago se sella con el `cajero_usuario` y `cajero_nombre` del token JWT autenticado.
2. **Estructura 5+5:** Primeros 5 dígitos identifican al alumno (vía `cuenta_a` o prefijo de matrícula); últimos 5 identifican el concepto.
3. **Preservación Histórica:** La cancelación de un recibo no borra registros, audita usuario y motivo, y revierte los saldos de adeudos previamente liquidados.
4. **Fechas Administrativas:** Si se provee una fecha manual con justificación en la emisión, se respeta la fecha efectiva; de lo contrario, se usa la marca de tiempo UTC del sistema.
5. **Aislamiento de Cuentas:** Se mantiene estricta separación entre cuentas de acceso (`users`), cuentas contables/conceptos (`conceptos_cobro`) y números de cuenta bancarios universitarios.
