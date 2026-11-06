# Spec Frontend: Módulo de Interfaz de Caja (CajaPrincipalView)

## 1. Alcance y Objetivos
- Interfaz reactiva para ventanilla de caja universitaria del TecNM Campus Monclova.
- Integración completa con API REST (`/api/caja/...`) y base de datos PostgreSQL (`cajadb`).
- Soporte visual de estados de carga (Skeleton y Spinners), notificaciones Toast semánticas y Empty States.

## 2. Integración de Servicios (`api/cajaApi.ts`)
- **Sesión de Cajero:** Carga en tiempo de inicialización de datos del usuario (`authApi.me()`) para mostrar nombre y turno en barra institucional superior.
- **Catálogo de Oferta Académica:** Consulta dinámica de carreras oficiales (`GET /api/caja/carreras`) y conceptos vigentes (`GET /api/caja/conceptos`).
- **Padrón de Estudiantes y Autocompletado:** Búsqueda en vivo (`GET /api/caja/alumnos/buscar?q=...`) con debounce y selección asistida.
- **Auditoría de Adeudos y Becas:** Al cargar el estudiante, consulta adeudos activos (`GET /api/caja/alumnos/{matricula}/adeudos`) y despliega banner institucional preventivo (`UiAlert` warning) antes del cobro.
- **Emisión de Recibos y Desglose Contable:**
  - Registro de cobro (`POST /api/caja/recibos`): genera el número oficial y folio administrativo secuencial, asociando movimiento financiero en `movimientos_pago`.
  - Recibo previo (`POST /api/caja/recibos/previo`): emisión en borrador para revisión del solicitante.
- **Cancelación Administrativa:** Modal con requerimiento de motivo formal (mínimo 5 caracteres) que invoca `POST /api/caja/recibos/{id}/cancelar` y actualiza la vista histórica.
- **Historial del Turno:** Panel lateral con recibos recientes (`GET /api/caja/recibos/recientes`) y cálculo de arqueo en tiempo real.

## 3. Criterios de Aceptación UI/UX
- Conexión fluida con o sin base de datos activa (resiliencia con catálogos institucionales).
- Impresión de recibo con formato oficial de ventanilla TecNM (modal de impresión con CSS `@media print`).
- Soporte completo para modo oscuro y claro (`useTheme`).
