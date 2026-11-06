import { http } from '../../../shared/api/http'
import type {
  AdeudoAlumno,
  AlumnoCaja,
  BecaAlumno,
  CarreraCatalogo,
  ConceptoCatalogo,
  CrearReciboPayload,
  PeriodoAcademicoItem,
  ReciboCaja,
  ReporteDetalladoItem,
  ResumenCajaTurno,
} from '../types/caja'

// Backend Recibo Response shape (flat alumno properties)
interface BackendReciboResponse {
  id: string
  numeroRecibo: number
  folioAdministrativo: string
  fechaOperacion: string
  alumnoMatricula: string
  alumnoNombre: string
  alumnoCarrera: string
  alumnoSemestre: number
  alumnoTurno: string
  metodoPago: string
  referenciaBancaria?: string
  cuentaBancaria?: string
  fechaBancaria?: string
  subtotal: number
  descuentoTotal: number
  total: number
  estado: string
  motivoCancelacion?: string
  fechaCancelacion?: string
  cajeroUsuario: string
  cajeroNombre: string
  detalles: Array<{
    id: string
    conceptoId?: string
    descripcion: string
    cantidad: number
    cuotaUnitario: number
    descuentoBeca: number
    importeTotal: number
    periodoAno?: string
  }>
}

function adaptReciboResponse(item: BackendReciboResponse): ReciboCaja {
  return {
    id: item.id,
    numeroRecibo: item.numeroRecibo,
    folioAdministrativo: item.folioAdministrativo,
    fechaOperacion: item.fechaOperacion?.split('T')[0] ?? new Date().toISOString().split('T')[0],
    fechaEfectivaPago: item.fechaBancaria,
    alumno: {
      matricula: item.alumnoMatricula,
      nombreCompleto: item.alumnoNombre,
      carrera: item.alumnoCarrera,
      semestre: item.alumnoSemestre,
      turno: (item.alumnoTurno as any) || 'Matutino',
      estatus: 'Inscrito',
      tieneBeca: item.descuentoTotal > 0,
      porcentajeBeca: item.subtotal > 0 ? (item.descuentoTotal / item.subtotal) * 100 : 0,
    },
    detalles: item.detalles.map(d => ({
      id: d.id,
      conceptoId: d.conceptoId,
      descripcion: d.descripcion,
      cantidad: d.cantidad,
      cuotaUnitario: d.cuotaUnitario,
      descuentoBeca: d.descuentoBeca,
      importeTotal: d.importeTotal,
      periodoAno: d.periodoAno,
    })),
    metodoPago: (item.metodoPago as any) || 'EFECTIVO',
    referenciaBancaria: item.referenciaBancaria,
    fechaBancaria: item.fechaBancaria,
    cuentaBancaria: item.cuentaBancaria,
    subtotal: item.subtotal,
    descuentoTotal: item.descuentoTotal,
    total: item.total,
    estado: (item.estado as any) || 'REGISTRADO',
    motivoCancelacion: item.motivoCancelacion,
    fechaCancelacion: item.fechaCancelacion,
    cajeroUsuario: item.cajeroUsuario,
    cajeroNombre: item.cajeroNombre,
    cajaNumero: '01',
  }
}

export async function getConceptos(): Promise<ConceptoCatalogo[]> {
  const { data } = await http.get<ConceptoCatalogo[]>('/caja/conceptos')
  return data
}

export async function getCarreras(): Promise<CarreraCatalogo[]> {
  const { data } = await http.get<CarreraCatalogo[]>('/caja/carreras')
  return data
}

export async function getPeriodos(): Promise<PeriodoAcademicoItem[]> {
  const { data } = await http.get<PeriodoAcademicoItem[]>('/caja/periodos')
  return data
}

export async function buscarAlumnos(query?: string): Promise<AlumnoCaja[]> {
  const { data } = await http.get<AlumnoCaja[]>('/caja/alumnos/buscar', {
    params: { q: query },
  })
  return data
}

export async function getAlumno(matricula: string): Promise<AlumnoCaja> {
  const { data } = await http.get<AlumnoCaja>(`/caja/alumnos/${encodeURIComponent(matricula)}`)
  return data
}

export async function getAdeudosAlumno(matricula: string): Promise<AdeudoAlumno[]> {
  const { data } = await http.get<AdeudoAlumno[]>(`/caja/alumnos/${encodeURIComponent(matricula)}/adeudos`)
  return data
}

export async function getBecasAlumno(matricula: string): Promise<BecaAlumno[]> {
  const { data } = await http.get<BecaAlumno[]>(`/caja/alumnos/${encodeURIComponent(matricula)}/becas`)
  return data
}

export async function getRecibosRecientes(limite = 15): Promise<ReciboCaja[]> {
  const { data } = await http.get<BackendReciboResponse[]>('/caja/recibos/recientes', {
    params: { limite },
  })
  return data.map(adaptReciboResponse)
}

export async function getReciboPorNumero(numero: number): Promise<ReciboCaja> {
  const { data } = await http.get<BackendReciboResponse>(`/caja/recibos/${numero}`)
  return adaptReciboResponse(data)
}

export async function crearRecibo(payload: CrearReciboPayload): Promise<ReciboCaja> {
  const { data } = await http.post<BackendReciboResponse>('/caja/recibos', payload)
  return adaptReciboResponse(data)
}

export async function crearReciboPrevio(payload: CrearReciboPayload): Promise<ReciboCaja> {
  const { data } = await http.post<BackendReciboResponse>('/caja/recibos/previo', payload)
  return adaptReciboResponse(data)
}

export async function cancelarRecibo(id: string, motivo: string): Promise<ReciboCaja> {
  const { data } = await http.post<BackendReciboResponse>(`/caja/recibos/${id}/cancelar`, { motivo })
  return adaptReciboResponse(data)
}

export async function getCorteCaja(fecha?: string): Promise<ResumenCajaTurno> {
  const { data } = await http.get<ResumenCajaTurno>('/caja/corte', {
    params: { fecha },
  })
  return data
}

export async function getReporteDetallado(): Promise<ReporteDetalladoItem[]> {
  const { data } = await http.get<ReporteDetalladoItem[]>('/caja/reportes/detallado')
  return data
}
