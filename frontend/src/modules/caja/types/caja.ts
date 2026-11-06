export type EstadoRecibo = 'REGISTRADO' | 'PREVIO' | 'CANCELADO' | 'BLOQUEADO'

export type MetodoPago = 'EFECTIVO' | 'DEPOSITO_BANCARIO' | 'TRANSFERENCIA' | 'TARJETA_DEBITO' | 'TARJETA_CREDITO'

export interface AlumnoCaja {
  matricula: string
  nombreCompleto: string
  carrera: string
  carreraId?: number
  idCarrera?: number
  semestre: number
  turno: 'Matutino' | 'Vespertino' | 'Nocturno' | 'Sabatino'
  estatus: 'Inscrito' | 'No Inscrito' | 'Baja Temporal' | 'Baja Definitiva' | 'Egresado'
  tieneBeca: boolean
  porcentajeBeca: number
  numeroFicha?: string
  cuentaA?: number
  motivoBeca?: string
}

export interface CarreraCatalogo {
  idCarrera: number
  carrera: string
  nombreCarrera: string
  titulo?: string
  activo: boolean
}

export interface PeriodoAcademicoItem {
  idPeriodo: number
  codigo: string
  descripcion?: string
  anio: number
  ciclo: string
  activo: boolean
}

export interface AdeudoAlumno {
  idAdeudo: number
  matricula: string
  conceptoCobro: string
  idPeriodo: number
  periodoCodigo: string
  montoOriginal: number
  saldoPendiente: number
  fechaLimite?: string
  estado: string
}

export interface BecaAlumno {
  idBeca: number
  matricula: string
  idPeriodo: number
  periodoCodigo: string
  tipoBeca: string
  porcentaje?: number
  montoFijo?: number
  motivo?: string
  activa: boolean
}

export interface ConceptoCatalogo {
  id: string
  clave: string
  descripcion: string
  cuotaBase: number
  aplicaBeca: boolean
  categoria: 'Inscripción' | 'Constancias' | 'Exámenes' | 'Cursos' | 'Otros' | string
  cuentaReferencia?: string
  orden?: number
}

export interface DetalleConceptoRecibo {
  id: string
  conceptoId?: string
  descripcion: string
  cantidad: number
  cuotaUnitario: number
  descuentoBeca: number
  importeTotal: number
  periodoAno?: string
}

export interface ReciboCaja {
  id: string
  numeroRecibo: number
  folioAdministrativo: string
  fechaOperacion: string
  fechaEfectivaPago?: string
  alumno: AlumnoCaja
  detalles: DetalleConceptoRecibo[]
  metodoPago: MetodoPago
  referenciaBancaria?: string
  fechaBancaria?: string
  cuentaBancaria?: string
  subtotal: number
  descuentoTotal: number
  total: number
  estado: EstadoRecibo
  motivoCancelacion?: string
  fechaCancelacion?: string
  cajeroUsuario?: string
  cajeroNombre: string
  cajaNumero?: string
}

export interface CrearReciboPayload {
  matricula: string
  nombreCompleto: string
  carrera: string
  semestre: number
  turno: string
  metodoPago: string
  referenciaBancaria?: string
  cuentaBancaria?: string
  fechaBancaria?: string
  fechaOperacion?: string
  justificacionFecha?: string
  detalles: Array<{
    conceptoId?: string
    descripcion: string
    cantidad: number
    cuotaUnitario: number
    descuentoBeca: number
    importeTotal: number
    periodoAno?: string
  }>
}

export interface ResumenCajaTurno {
  fecha: string
  totalEfectivo: number
  totalBanco: number
  totalRecaudado: number
  totalRecibosEmitidos: number
  totalRecibosCancelados: number
}

export interface ReporteDetalladoItem {
  reciboId: string
  numeroRecibo: number
  folioAdministrativo: string
  fechaOperacion: string
  alumnoMatricula: string
  alumnoNombre: string
  alumnoCarrera: string
  alumnoSemestre: number
  alumnoTurno: string
  conceptoDetalle: string
  cantidad: number
  cuotaUnitario: number
  descuentoBeca: number
  importeRenglon: number
  totalRecibo: number
  metodoPago: string
  referenciaBancaria?: string
  estadoRecibo: string
  cajeroUsuario: string
  cajeroNombre: string
}
