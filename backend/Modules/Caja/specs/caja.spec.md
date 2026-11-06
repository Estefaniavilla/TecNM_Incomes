# Spec backend: Módulo de Control de Caja

## 1. Alcance y Capacidades
- Búsqueda y consulta de estudiantes (matrícula, nombre, carrera, semestre, beca, ficha, cuenta).
- Catálogos académicos oficiales del TecNM (Carreras y Periodos Académicos).
- Consulta de adeudos y becas activas por estudiante.
- Catálogo de conceptos de cobro vigentes, categorías e importes base.
- Emisión de recibos y cobros (efectivo y depósito bancario 5+5) con generación de `movimientos_pago`.
- Cancelación administrativa justificada de recibos (preservación histórica).
- Consulta de corte diario y recibos por turno de cajero.
- Reporte detallado de recibos para auditoría y exportación a Excel.

## 2. Endpoints
- `GET /api/caja/carreras` -> `200 OK [CarreraDTO]`
- `GET /api/caja/periodos` -> `200 OK [PeriodoDTO]`
- `GET /api/caja/alumnos/buscar?q={query}` -> `200 OK [AlumnoDTO]`
- `GET /api/caja/alumnos/{matricula}` -> `200 OK AlumnoDTO` | `404 Not Found`
- `GET /api/caja/alumnos/{matricula}/adeudos` -> `200 OK [AdeudoDTO]`
- `GET /api/caja/alumnos/{matricula}/becas` -> `200 OK [BecaDTO]`
- `GET /api/caja/conceptos` -> `200 OK [ConceptoDTO]`
- `POST /api/caja/recibos` -> `201 Created ReciboResponse` | `400 Bad Request`
- `POST /api/caja/recibos/previo` -> `200 OK ReciboResponse` | `400 Bad Request`
- `POST /api/caja/recibos/{id}/cancelar` -> `200 OK ReciboResponse` | `400 Bad Request` | `404 Not Found`
- `GET /api/caja/recibos/{numero}` -> `200 OK ReciboResponse` | `404 Not Found`
- `GET /api/caja/recibos/recientes` -> `200 OK [ReciboResponse]`
- `GET /api/caja/corte` -> `200 OK CorteCajaResponse`
- `GET /api/caja/reportes/detallado` -> `200 OK [ReporteReciboDetalladoDTO]`

## 3. Reglas de Negocio
- Folio administrativo con formato: `FOL-{Año}-{Mes}-{NumeroRecibo}`.
- Validación de cancelación: motivo formal obligatorio de al menos 5 caracteres.
- Preservación histórica: las cancelaciones cambian el estado a `CANCELADO` y registran timestamp y usuario; nunca eliminan físicamente el registro.
- Respaldo de pago: todo recibo genera su registro contable en `movimientos_pago`.
- Resiliencia de datos: soporte de carga en base de datos PostgreSQL `cajadb` y catálogo institucional semilla integrado.
