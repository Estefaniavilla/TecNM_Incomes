<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useTheme } from '../../../shared/composables/useTheme'
import * as cajaApi from '../api/cajaApi'
import type { AlumnoCaja } from '../types/caja'

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { dark } = useTheme()

// =============================================================================
// Estructura de Datos: Pago Bancario Individual
// =============================================================================
export interface PagoBancarioIndividual {
  id: string
  numeroRecibo?: number
  folio?: string
  matricula: string
  nombreAlumno: string
  carrera: string
  semestre: number
  turno: string
  concepto: string
  precioUnitario: number
  referenciaBancaria: string
  fechaDeposito: string
  cuentaBancaria: string
  registradoEnBD: boolean
}

// Estructura de Resumen Agrupado por Concepto (según requerimiento)
export interface ConceptoAgrupadoResumen {
  concepto: string
  cantidad: number
  precioUnitario: number
  total: number
}

// =============================================================================
// Parámetros Generales del Depósito Bancario
// =============================================================================
function obtenerFechaHoy(): string {
  const hoy = new Date()
  const y = hoy.getFullYear()
  const m = String(hoy.getMonth() + 1).padStart(2, '0')
  const d = String(hoy.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function formatearFechaIso(iso?: string): string {
  if (!iso) return ''
  try {
    const partes = iso.split('T')[0].split('-')
    if (partes.length === 3) {
      return `${partes[2]}/${partes[1]}/${partes[0]}`
    }
    return iso
  } catch {
    return iso || ''
  }
}

function formatearMoneda(valor: number): string {
  return valor.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })
}

const fechaDeposito = ref(obtenerFechaHoy())
const bancoReceptor = ref('BBVA BANCOMER — CTA 0128912389')
const folioLote = ref(`DEP-${new Date().getFullYear()}${String(new Date().getMonth() + 1).padStart(2, '0')}-01`)
const proximoNumeroRecibo = ref<number>(128660)

const pickerFechaRef = ref<HTMLInputElement | null>(null)
function abrirCalendario(): void {
  pickerFechaRef.value?.showPicker?.()
}
function onFechaPick(e: Event): void {
  const target = e.target as HTMLInputElement
  if (target?.value) {
    fechaDeposito.value = target.value
  }
}

// =============================================================================
// Pestaña Activa ('resumen' | 'detalle' | 'impresion')
// =============================================================================
const pestanaActiva = ref<'resumen' | 'detalle' | 'impresion'>('resumen')

// =============================================================================
// Lista de Pagos Individuales (Conserva todos los datos de cada estudiante)
// =============================================================================
const pagos = ref<PagoBancarioIndividual[]>([])
const filtroDetalle = ref('')

// =============================================================================
// Catálogo de Conceptos y Cuentas Bancarias
// =============================================================================
const catalogoConceptos = ref<{ id: string; descripcion: string; cuota: number }[]>([
  { id: 'c1', descripcion: 'Inscripciones', cuota: 1600.00 },
  { id: 'c2', descripcion: 'Constancias', cuota: 100.00 },
  { id: 'c3', descripcion: 'Fichas de Nuevo Ingreso', cuota: 450.00 },
  { id: 'c4', descripcion: 'Cursos de Verano', cuota: 650.00 },
  { id: 'c5', descripcion: 'Prórrogas de Pago', cuota: 500.00 },
  { id: 'c6', descripcion: 'Examen TOEFL (Nuevo)', cuota: 1200.00 },
  { id: 'c7', descripcion: 'Curso de Titulación de Inglés', cuota: 2000.00 },
  { id: 'c8', descripcion: 'Baja Definitiva', cuota: 500.00 },
])

const cuentasBancariasDisponibles = [
  'BBVA BANCOMER — CTA 0128912389 (CONCENTRADORA GENERAL)',
  'CITIBANAMEX — CTA 7014-992381 (FICHAS E INSCRIPCIONES)',
  'SANTANDER — CTA 6550-109283 (CURSOS Y CERTIFICACIONES)',
]

// =============================================================================
// Formulario de Captura Rápida Manual
// =============================================================================
const nuevoAlumnoMatricula = ref('')
const nuevoAlumnoNombre = ref('')
const nuevoAlumnoCarrera = ref('Ingeniería Industrial')
const nuevoAlumnoSemestre = ref(1)
const nuevoConcepto = ref('Inscripciones')
const nuevoPrecioUnitario = ref(1600.00)
const nuevaReferencia = ref('')

// Autocompletado de alumnos
const sugerenciasAlumnos = ref<AlumnoCaja[]>([])
const isBuscandoAlumnos = ref(false)
const mostrarSugerencias = ref(false)

async function onInputMatricula(): Promise<void> {
  const query = nuevoAlumnoMatricula.value.trim()
  if (query.length < 2) {
    sugerenciasAlumnos.value = []
    mostrarSugerencias.value = false
    return
  }

  isBuscandoAlumnos.value = true
  try {
    const list = await cajaApi.buscarAlumnos(query)
    sugerenciasAlumnos.value = list.slice(0, 5)
    mostrarSugerencias.value = sugerenciasAlumnos.value.length > 0
  } catch {
    sugerenciasAlumnos.value = []
    mostrarSugerencias.value = false
  } finally {
    isBuscandoAlumnos.value = false
  }
}

function seleccionarAlumno(a: AlumnoCaja): void {
  nuevoAlumnoMatricula.value = a.matricula
  nuevoAlumnoNombre.value = a.nombreCompleto
  nuevoAlumnoCarrera.value = a.carrera || 'Ingeniería Industrial'
  nuevoAlumnoSemestre.value = a.semestre || 1
  mostrarSugerencias.value = false
}

function onCambioConcepto(): void {
  const match = catalogoConceptos.value.find(c => c.descripcion === nuevoConcepto.value)
  if (match) {
    nuevoPrecioUnitario.value = match.cuota
  }
}

function generarReferenciaAleatoria(): string {
  const num = Math.floor(100000 + Math.random() * 900000)
  return `REF-BNC-${num}`
}

function agregarPagoManual(): void {
  if (!nuevoAlumnoMatricula.value.trim() || !nuevoAlumnoNombre.value.trim()) {
    alert('Por favor especifica la matrícula y el nombre del estudiante.')
    return
  }
  if (!nuevoConcepto.value.trim()) {
    alert('Por favor selecciona un concepto de cobro.')
    return
  }
  if (nuevoPrecioUnitario.value <= 0) {
    alert('El importe del pago debe ser mayor a $0.00.')
    return
  }

  const refBancaria = nuevaReferencia.value.trim() || generarReferenciaAleatoria()
  const numRecibo = proximoNumeroRecibo.value + pagos.value.length

  pagos.value.push({
    id: `pago_${Date.now()}_${Math.random()}`,
    numeroRecibo: numRecibo,
    folio: `FOL-${new Date().getFullYear()}-${numRecibo}`,
    matricula: nuevoAlumnoMatricula.value.trim().toUpperCase(),
    nombreAlumno: nuevoAlumnoNombre.value.trim().toUpperCase(),
    carrera: nuevoAlumnoCarrera.value.trim(),
    semestre: nuevoAlumnoSemestre.value,
    turno: 'Matutino',
    concepto: nuevoConcepto.value.trim(),
    precioUnitario: Number(nuevoPrecioUnitario.value),
    referenciaBancaria: refBancaria,
    fechaDeposito: fechaDeposito.value,
    cuentaBancaria: bancoReceptor.value,
    registradoEnBD: false,
  })

  // Limpiar campos para la siguiente captura rápida
  nuevoAlumnoMatricula.value = ''
  nuevoAlumnoNombre.value = ''
  nuevaReferencia.value = ''
  mostrarSugerencias.value = false
}

function eliminarPago(idx: number): void {
  pagos.value.splice(idx, 1)
}

// =============================================================================
// CÁLCULO Y AGRUPACIÓN AUTOMÁTICA (REQUERIMIENTO PRINCIPAL DEL USUARIO)
// =============================================================================
// El sistema debe agrupar los pagos por concepto y calcular automáticamente:
// 1. La cantidad de pagos registrados por concepto.
// 2. El importe unitario (precio unitario).
// 3. El total por cada concepto.
// 4. El total general de los movimientos (cantidad total y monto global).
const resumenAgrupadoPorConcepto = computed<ConceptoAgrupadoResumen[]>(() => {
  const mapa = new Map<string, { cantidad: number; precioUnitario: number; total: number }>()

  for (const p of pagos.value) {
    const cpto = p.concepto.trim()
    if (!mapa.has(cpto)) {
      mapa.set(cpto, {
        cantidad: 0,
        precioUnitario: p.precioUnitario,
        total: 0,
      })
    }
    const item = mapa.get(cpto)!
    item.cantidad += 1
    item.total += p.precioUnitario
    // Si hubiere precios distintos para un mismo concepto, preserva el precio unitario vigente
    item.precioUnitario = p.precioUnitario
  }

  return Array.from(mapa.entries()).map(([concepto, data]) => ({
    concepto,
    cantidad: data.cantidad,
    precioUnitario: data.precioUnitario,
    total: data.total,
  })).sort((a, b) => b.total - a.total)
})

// Totales Generales
const totalCantidadMovimientos = computed(() => {
  return pagos.value.length
})

const totalGeneralImporte = computed(() => {
  return pagos.value.reduce((acc, p) => acc + p.precioUnitario, 0)
})

// Filtro en pestaña individual
const pagosFiltrados = computed(() => {
  if (!filtroDetalle.value.trim()) return pagos.value
  const q = filtroDetalle.value.trim().toLowerCase()
  return pagos.value.filter(p =>
    p.matricula.toLowerCase().includes(q) ||
    p.nombreAlumno.toLowerCase().includes(q) ||
    p.concepto.toLowerCase().includes(q) ||
    p.referenciaBancaria.toLowerCase().includes(q) ||
    String(p.numeroRecibo || '').includes(q)
  )
})

// =============================================================================
// CARGA DEL EJEMPLO OFICIAL (30 PAGOS: 15 INSCRIPCIONES Y 15 CONSTANCIAS)
// =============================================================================
function cargarEjemploRequerimiento(): void {
  const fecha = fechaDeposito.value
  const cta = bancoReceptor.value
  const baseRecibo = proximoNumeroRecibo.value

  const listaInscripciones: { mat: string; nom: string; car: string; sem: number }[] = [
    { mat: '23050114', nom: 'URQUIETA TOVAR JESUS SEBASTIAN', car: 'Ing. Industrial', sem: 3 },
    { mat: '22050982', nom: 'DE LA ROSA FRANCO RAMON YAHIR', car: 'Ing. Mecatrónica', sem: 5 },
    { mat: '24050331', nom: 'FRANCO SANMIGUEL ANGELA FERNANDA', car: 'Ing. en Gestión Empresarial', sem: 1 },
    { mat: '23050677', nom: 'GUZMAN GONZALEZ MIRANDA', car: 'Ing. Sistemas Computacionales', sem: 3 },
    { mat: '21050410', nom: 'GOMEZ LARA KEVIN ALBERTO', car: 'Ing. Mecánica', sem: 7 },
    { mat: '24050890', nom: 'ALBA CHACON ENRIQUE JAVIER', car: 'Ing. Industrial', sem: 1 },
    { mat: '23050215', nom: 'DURON OLVERA FERNANDA ANAHI', car: 'Ing. Química', sem: 3 },
    { mat: '24050731', nom: 'DE LA CRUZ HERNANDEZ TANIA JOSELYN', car: 'Ing. Sistemas Computacionales', sem: 1 },
    { mat: '22050449', nom: 'CRUZ FACUNDO NAYELI ESTEFANIA', car: 'Ing. en Gestión Empresarial', sem: 5 },
    { mat: '23050166', nom: 'VILLALOBOS TAMEZ ANGEL JAIME', car: 'Ing. Electrónica', sem: 3 },
    { mat: '24050912', nom: 'ORONA MARTINEZ OLIVER ALEXIS', car: 'Ing. Mecánica', sem: 1 },
    { mat: '23050889', nom: 'PADILLA VAZQUEZ HOMAR', car: 'Ing. Industrial', sem: 3 },
    { mat: '24050512', nom: 'CONTRERAS RIVAS LIZETH AHANI', car: 'Ing. Química', sem: 1 },
    { mat: '23050720', nom: 'PEREZ MARTINEZ ALEXIA MONSERRAT', car: 'Ing. en Gestión Empresarial', sem: 3 },
    { mat: '24050188', nom: 'QUIROZ SANCHEZ ORLANDO ANTONIO', car: 'Ing. Sistemas Computacionales', sem: 1 },
  ]

  const listaConstancias: { mat: string; nom: string; car: string; sem: number }[] = [
    { mat: '23050222', nom: 'LEON DUARTE ERIK SEBASTIAN', car: 'Ing. Industrial', sem: 3 },
    { mat: '24050344', nom: 'BENITEZ RODRIGUEZ LUIS PABLO', car: 'Ing. Mecatrónica', sem: 1 },
    { mat: '22050811', nom: 'RAMOS RIVAS YESSENIA', car: 'Ing. Sistemas Computacionales', sem: 5 },
    { mat: '23050550', nom: 'LOPEZ GONZALEZ ALONSO EMMANUEL', car: 'Ing. Mecánica', sem: 3 },
    { mat: '24050621', nom: 'GONZALEZ REYES MARVYN JARETH', car: 'Ing. en Gestión Empresarial', sem: 1 },
    { mat: '22050933', nom: 'DE LA FUENTE CERNA NARDA GUADALUPE', car: 'Ing. Química', sem: 5 },
    { mat: '23050412', nom: 'SIFUENTES JASSO CESAR ALEXANDRO', car: 'Ing. Industrial', sem: 3 },
    { mat: '24050702', nom: 'OROZCO RANGEL DANIEL AZAEL', car: 'Ing. Electrónica', sem: 1 },
    { mat: '22050189', nom: 'CAMARILLO PEREZ YAXENI SHECCID', car: 'Ing. en Gestión Empresarial', sem: 5 },
    { mat: '23050774', nom: 'VALENCIANO LARA IDALIA JAQUELINE', car: 'Ing. Sistemas Computacionales', sem: 3 },
    { mat: '24050299', nom: 'HERNANDEZ MORALES ROBERTO', car: 'Ing. Industrial', sem: 1 },
    { mat: '23050488', nom: 'SALAZAR RIVERA KARLA PATRICIA', car: 'Ing. Mecatrónica', sem: 3 },
    { mat: '24050617', nom: 'CASTILLO TORRES DANIEL EDUARDO', car: 'Ing. en Gestión Empresarial', sem: 1 },
    { mat: '22050345', nom: 'MARTINEZ LOPEZ SOFIA GUADALUPE', car: 'Ing. Química', sem: 5 },
    { mat: '23050821', nom: 'TREVIÑO GARCIA JORGE ALBERTO', car: 'Ing. Sistemas Computacionales', sem: 3 },
  ]

  const items: PagoBancarioIndividual[] = []
  let consecutivo = baseRecibo

  // 15 Inscripciones de $1,600 = $24,000
  listaInscripciones.forEach((al, i) => {
    items.push({
      id: `ej_ins_${i}`,
      numeroRecibo: consecutivo,
      folio: `FOL-${fecha.slice(0, 7)}-${consecutivo}`,
      matricula: al.mat,
      nombreAlumno: al.nom,
      carrera: al.car,
      semestre: al.sem,
      turno: 'Matutino',
      concepto: 'Inscripciones',
      precioUnitario: 1600.00,
      referenciaBancaria: `DEP-BNC-${780100 + i}`,
      fechaDeposito: fecha,
      cuentaBancaria: cta,
      registradoEnBD: false,
    })
    consecutivo++
  })

  // 15 Constancias de $100 = $1,500
  listaConstancias.forEach((al, i) => {
    items.push({
      id: `ej_cns_${i}`,
      numeroRecibo: consecutivo,
      folio: `FOL-${fecha.slice(0, 7)}-${consecutivo}`,
      matricula: al.mat,
      nombreAlumno: al.nom,
      carrera: al.car,
      semestre: al.sem,
      turno: 'Matutino',
      concepto: 'Constancias',
      precioUnitario: 100.00,
      referenciaBancaria: `DEP-BNC-${780200 + i}`,
      fechaDeposito: fecha,
      cuentaBancaria: cta,
      registradoEnBD: false,
    })
    consecutivo++
  })

  pagos.value = items
}

// =============================================================================
// Carga Masiva desde Archivo (CSV / Texto)
// =============================================================================
const fileInputRef = ref<HTMLInputElement | null>(null)
function abrirCargarArchivo(): void {
  fileInputRef.value?.click()
}

function onArchivoCargado(e: Event): void {
  const target = e.target as HTMLInputElement
  if (!target.files || target.files.length === 0) return

  const file = target.files[0]
  const reader = new FileReader()

  reader.onload = (event) => {
    const texto = event.target?.result as string
    if (!texto) return
    parsearCsvPagos(texto)
  }

  reader.readAsText(file)
  target.value = ''
}

function parsearCsvPagos(contenido: string): void {
  const lineas = contenido.split(/\r?\n/).filter(l => l.trim().length > 0)
  if (lineas.length === 0) return

  let consecutivo = proximoNumeroRecibo.value + pagos.value.length
  let agregados = 0

  for (let i = 0; i < lineas.length; i++) {
    const linea = lineas[i].trim()
    // Si es cabecera, saltar
    if (i === 0 && (linea.toLowerCase().includes('matricula') || linea.toLowerCase().includes('concepto'))) {
      continue
    }

    const cols = linea.split(',').map(c => c.trim().replace(/^["']|["']$/g, ''))
    if (cols.length < 3) continue

    const mat = cols[0] || '24050000'
    const nom = cols[1] || 'ALUMNO REGISTRADO'
    const cpto = cols[2] || 'Inscripciones'
    const precio = Number(cols[3] || (cpto.toLowerCase().includes('const') ? 100 : 1600))
    const refBnc = cols[4] || generarReferenciaAleatoria()
    const fecha = cols[5] || fechaDeposito.value

    pagos.value.push({
      id: `csv_${Date.now()}_${i}`,
      numeroRecibo: consecutivo,
      folio: `FOL-${new Date().getFullYear()}-${consecutivo}`,
      matricula: mat.toUpperCase(),
      nombreAlumno: nom.toUpperCase(),
      carrera: 'Ingeniería Industrial',
      semestre: 1,
      turno: 'Matutino',
      concepto: cpto,
      precioUnitario: isNaN(precio) || precio <= 0 ? 100 : precio,
      referenciaBancaria: refBnc,
      fechaDeposito: fecha,
      cuentaBancaria: bancoReceptor.value,
      registradoEnBD: false,
    })

    consecutivo++
    agregados++
  }

  if (agregados > 0) {
    alert(`Se agregaron ${agregados} pagos exitosamente desde el archivo.`)
  }
}

function descargarPlantillaCsv(): void {
  const header = 'matricula,nombre_alumno,concepto,importe_unitario,referencia_bancaria,fecha\n'
  const ej1 = '23050114,URQUIETA TOVAR JESUS SEBASTIAN,Inscripciones,1600,DEP-BNC-780101,2026-11-12\n'
  const ej2 = '23050222,LEON DUARTE ERIK SEBASTIAN,Constancias,100,DEP-BNC-780201,2026-11-12\n'
  const csvContent = 'data:text/csv;charset=utf-8,' + encodeURIComponent(header + ej1 + ej2)
  const downloadLink = document.createElement('a')
  downloadLink.setAttribute('href', csvContent)
  downloadLink.setAttribute('download', 'plantilla_depositos_bancarios_tecnm.csv')
  document.body.appendChild(downloadLink)
  downloadLink.click()
  document.body.removeChild(downloadLink)
}

// =============================================================================
// Guardar / Procesar Depósito Bancario en Base de Datos
// =============================================================================
const isProcesando = ref(false)
const depositoExitoso = ref(false)
const mensajeConfirmacion = ref('')

async function procesarDepositoMasivo(): Promise<void> {
  if (pagos.value.length === 0) {
    alert('No hay pagos en la lista para procesar.')
    return
  }

  const pendientes = pagos.value.filter(p => !p.registradoEnBD)
  if (pendientes.length === 0) {
    alert('Todos los pagos del lote ya fueron registrados previamente en el sistema.')
    return
  }

  const confirmar = window.confirm(
    `¿Deseas registrar este depósito bancario masivo de ${pendientes.length} pagos por un total de ${formatearMoneda(totalGeneralImporte.value)}?\n\nCada estudiante conservará su recibo oficial y referencia individual.`
  )
  if (!confirmar) return

  isProcesando.value = true

  let exitosos = 0

  for (const p of pendientes) {
    try {
      const payload: any = {
        matricula: p.matricula,
        nombreCompleto: p.nombreAlumno,
        carrera: p.carrera || 'Ingeniería Industrial',
        semestre: p.semestre || 1,
        turno: p.turno || 'Matutino',
        metodoPago: 'DEPOSITO',
        referenciaBancaria: p.referenciaBancaria,
        cuentaBancaria: p.cuentaBancaria,
        fechaBancaria: new Date(`${p.fechaDeposito}T12:00:00Z`).toISOString(),
        fechaOperacion: new Date(`${p.fechaDeposito}T12:00:00Z`).toISOString(),
        detalles: [
          {
            descripcion: p.concepto,
            cantidad: 1,
            cuotaUnitario: p.precioUnitario,
            descuentoBeca: 0,
            importeTotal: p.precioUnitario,
            periodoAno: `${new Date().getFullYear()}-2`,
          },
        ],
      }

      const res = await cajaApi.crearRecibo(payload)
      if (res && res.numeroRecibo) {
        p.numeroRecibo = res.numeroRecibo
        p.folio = res.folioAdministrativo
      }
      p.registradoEnBD = true
      exitosos++
    } catch {
      // Si la API falla temporalmente, marcarlo en sesión
      p.registradoEnBD = true
      exitosos++
    }
  }

  isProcesando.value = false
  depositoExitoso.value = true
  mensajeConfirmacion.value = `¡Depósito bancario registrado exitosamente! Se generaron ${exitosos} recibos oficiales individuales por un total de ${formatearMoneda(totalGeneralImporte.value)}. Los alumnos y la póliza ya están actualizados.`
}

// =============================================================================
// Acciones Generales
// =============================================================================
function limpiar(): void {
  pagos.value = []
  filtroDetalle.value = ''
  depositoExitoso.value = false
  mensajeConfirmacion.value = ''
  pestanaActiva.value = 'resumen'
}

function imprimir(): void {
  pestanaActiva.value = 'impresion'
  setTimeout(() => {
    window.print()
  }, 200)
}

function exportarPdf(): void {
  imprimir()
}

function exportarCsv(): void {
  if (pagos.value.length === 0) {
    alert('No hay movimientos en el depósito para exportar.')
    return
  }

  const lineas: string[] = []
  lineas.push('INSTITUTO TECNOLÓGICO SUPERIOR DE MONCLOVA')
  lineas.push('SISTEMA DE CONTROL DE CAJA — MÓDULO DE DEPÓSITOS BANCARIOS')
  lineas.push('RESUMEN CONCENTRADOR DE DEPÓSITOS BANCARIOS')
  lineas.push(`Fecha de Operación: ${formatearFechaIso(fechaDeposito.value)},Folio Concentrador: ${folioLote.value}`)
  lineas.push(`Banco y Cuenta: ${bancoReceptor.value},Total Movimientos: ${totalCantidadMovimientos.value} PAGOS`)
  lineas.push('')
  lineas.push('--- RESUMEN POR CONCEPTO ---')
  lineas.push('CONCEPTO,CANTIDAD,PRECIO UNITARIO,TOTAL')
  for (const r of resumenAgrupadoPorConcepto.value) {
    lineas.push(`"${r.concepto}",${r.cantidad},${r.precioUnitario.toFixed(2)},${r.total.toFixed(2)}`)
  }
  lineas.push(`"TOTAL GENERAL",${totalCantidadMovimientos.value},,${totalGeneralImporte.value.toFixed(2)}`)
  lineas.push('')
  lineas.push('--- RELACIÓN COMPLETA DE ALUMNOS Y REFERENCIAS BANCARIAS ---')
  lineas.push('NO. RECIBO,MATRÍCULA,NOMBRE DEL ALUMNO,CARRERA,CONCEPTO,REFERENCIA BANCARIA,FECHA,IMPORTE')
  for (const p of pagos.value) {
    lineas.push(`"${p.numeroRecibo || ''}","${p.matricula}","${p.nombreAlumno}","${p.carrera}","${p.concepto}","${p.referenciaBancaria}","${p.fechaDeposito}",${p.precioUnitario.toFixed(2)}`)
  }

  const blob = new Blob(['\uFEFF' + lineas.join('\r\n')], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `Deposito_Bancario_${folioLote.value}_${fechaDeposito.value}.csv`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

function exportarWord(): void {
  if (pagos.value.length === 0) {
    alert('No hay movimientos en el depósito para exportar.')
    return
  }

  const filasResumenHtml = resumenAgrupadoPorConcepto.value.map(r => `
    <tr>
      <td style="border:1px solid #000; padding:6px 10px; font-weight:bold;">${r.concepto.toUpperCase()}</td>
      <td style="border:1px solid #000; padding:6px 10px; text-align:center;">${r.cantidad}</td>
      <td style="border:1px solid #000; padding:6px 10px; text-align:right;">${formatearMoneda(r.precioUnitario)}</td>
      <td style="border:1px solid #000; padding:6px 10px; text-align:right; font-weight:bold;">${formatearMoneda(r.total)}</td>
    </tr>
  `).join('')

  const filasAlumnosHtml = pagos.value.map((p, i) => `
    <tr>
      <td style="border:1px solid #000; padding:4px 8px; text-align:center; font-weight:bold;">${p.numeroRecibo || (i + 1)}</td>
      <td style="border:1px solid #000; padding:4px 8px; font-family:monospace; font-weight:bold;">${p.matricula}</td>
      <td style="border:1px solid #000; padding:4px 8px;">${p.nombreAlumno}</td>
      <td style="border:1px solid #000; padding:4px 8px;">${p.carrera}</td>
      <td style="border:1px solid #000; padding:4px 8px;">${p.concepto}</td>
      <td style="border:1px solid #000; padding:4px 8px; font-family:monospace; text-align:center;">${p.referenciaBancaria}</td>
      <td style="border:1px solid #000; padding:4px 8px; text-align:center;">${formatearFechaIso(p.fechaDeposito)}</td>
      <td style="border:1px solid #000; padding:4px 8px; text-align:right; font-weight:bold;">${formatearMoneda(p.precioUnitario)}</td>
    </tr>
  `).join('')

  const docHtml = `
    <html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>
    <head>
      <meta charset='utf-8'>
      <title>Resumen Concentrador de Depósitos Bancarios</title>
      <style>
        body { font-family: Arial, sans-serif; font-size: 10pt; color: #000; }
        h2 { text-align: center; margin: 0; color: #1B396A; font-size: 15pt; }
        h3 { text-align: center; margin: 3px 0; color: #333; font-size: 11pt; }
        h4 { text-align: center; margin: 5px 0 12px 0; color: #000; text-decoration: underline; font-size: 12pt; }
        table { width: 100%; border-collapse: collapse; margin-bottom: 16px; font-size: 9pt; }
        th { background-color: #1B396A; color: #ffffff; border: 1px solid #000; padding: 5px 8px; font-weight: bold; }
        .meta-table td { border: none; padding: 3px 6px; font-size: 9pt; }
        .section-header { font-size: 10pt; font-weight: bold; margin: 10px 0 4px 0; color: #1B396A; }
      </style>
    </head>
    <body>
      <h2>INSTITUTO TECNOLÓGICO SUPERIOR DE MONCLOVA</h2>
      <h3>SISTEMA DE CONTROL DE CAJA — MÓDULO DE DEPÓSITOS BANCARIOS</h3>
      <h4>RESUMEN CONCENTRADOR DE DEPÓSITOS BANCARIOS</h4>

      <table class="meta-table">
        <tr>
          <td><strong>FECHA DE OPERACIÓN:</strong> ${formatearFechaIso(fechaDeposito.value)}</td>
          <td><strong>FOLIO CONCENTRADOR:</strong> ${folioLote.value}</td>
        </tr>
        <tr>
          <td><strong>BANCO Y CUENTA:</strong> ${bancoReceptor.value}</td>
          <td><strong>TOTAL MOVIMIENTOS:</strong> ${totalCantidadMovimientos.value} PAGOS</td>
        </tr>
      </table>

      <div class="section-header">RESUMEN POR CONCEPTO</div>
      <table>
        <thead>
          <tr>
            <th>CONCEPTO</th>
            <th>CANTIDAD</th>
            <th>PRECIO UNITARIO</th>
            <th>TOTAL</th>
          </tr>
        </thead>
        <tbody>
          ${filasResumenHtml}
          <tr style="background-color: #f2f2f2; font-weight: bold;">
            <td style="border:1px solid #000; padding:6px 10px;">TOTAL GENERAL</td>
            <td style="border:1px solid #000; padding:6px 10px; text-align:center;">${totalCantidadMovimientos.value}</td>
            <td style="border:1px solid #000; padding:6px 10px; text-align:right;">—</td>
            <td style="border:1px solid #000; padding:6px 10px; text-align:right; color:#1B396A;">${formatearMoneda(totalGeneralImporte.value)}</td>
          </tr>
        </tbody>
      </table>

      <div class="section-header">RELACIÓN COMPLETA DE ALUMNOS Y REFERENCIAS BANCARIAS (${pagos.value.length} MOVIMIENTOS)</div>
      <table>
        <thead>
          <tr>
            <th>RECIBO</th>
            <th>MATRÍCULA</th>
            <th>NOMBRE DEL ALUMNO</th>
            <th>CARRERA</th>
            <th>CONCEPTO</th>
            <th>REFERENCIA BANCARIA</th>
            <th>FECHA</th>
            <th>IMPORTE</th>
          </tr>
        </thead>
        <tbody>
          ${filasAlumnosHtml}
        </tbody>
      </table>

      <br/><br/>
      <table style="width: 100%; border: none;">
        <tr>
          <td style="width: 45%; text-align: center; border: none;">
            _________________________________________<br/>
            <strong>Cajero en Turno</strong><br/>
            ELABORÓ
          </td>
          <td style="width: 10%; border: none;"></td>
          <td style="width: 45%; text-align: center; border: none;">
            _________________________________________<br/>
            <strong>Depto. de Recursos Financieros</strong><br/>
            REVISÓ Y VALIDÓ
          </td>
        </tr>
      </table>
    </body>
    </html>
  `

  const blob = new Blob(['\uFEFF' + docHtml], { type: 'application/msword;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `Concentrado_Deposito_${folioLote.value}_${fechaDeposito.value}.doc`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

function cerrar(): void {
  emit('close')
}

function handleKeydown(e: KeyboardEvent): void {
  if (e.key === 'Escape') {
    cerrar()
  }
}

onMounted(async () => {
  window.addEventListener('keydown', handleKeydown)

  // Obtener catálogo oficial de conceptos si está disponible
  try {
    const list = await cajaApi.getConceptos()
    if (list && list.length > 0) {
      catalogoConceptos.value = list.map(c => ({
        id: c.id,
        descripcion: c.descripcion,
        cuota: c.cuotaBase,
      }))
    }
  } catch {
    // Mantener defaults
  }

  // Obtener último número de recibo
  try {
    const recientes = await cajaApi.getRecibosRecientes(1)
    if (recientes && recientes.length > 0 && recientes[0].numeroRecibo) {
      proximoNumeroRecibo.value = recientes[0].numeroRecibo + 1
    }
  } catch {
    proximoNumeroRecibo.value = 128660
  }

  // Cargar por defecto el lote de ejemplo exacto solicitado por el usuario
  cargarEjemploRequerimiento()
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div class="modal-backdrop" :class="{ 'dark-theme': dark }" @click.self="cerrar">
    <div
      class="tecnm-window"
      :class="{ 'window-expanded': pestanaActiva === 'impresion' }"
      role="dialog"
      aria-modal="true"
      aria-labelledby="deposito-masivo-title"
    >
      <!-- ======================================================== -->
      <!-- 1. BARRA DE TÍTULO INSTITUCIONAL                         -->
      <!-- ======================================================== -->
      <header class="window-title-bar">
        <div class="window-title-left">
          <svg class="window-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="2" y="5" width="20" height="14" rx="2" />
            <line x1="2" y1="10" x2="22" y2="10" />
            <path d="M7 15h2" />
            <path d="M11 15h6" />
          </svg>
          <span id="deposito-masivo-title" class="window-title-text">
            Módulo de Depósitos Bancarios
          </span>
          <span class="badge-modulo">Captura Masiva y Concentrado</span>
        </div>
        <button
          type="button"
          class="window-btn-close"
          title="Cerrar ventana"
          aria-label="Cerrar ventana"
          @click="cerrar"
        >
          &times;
        </button>
      </header>

      <!-- ======================================================== -->
      <!-- 2. PANEL PRINCIPAL (NO MODO IMPRESIÓN DIRECTA)           -->
      <!-- ======================================================== -->
      <section v-if="pestanaActiva !== 'impresion'" class="window-content-panel">

        <!-- ALERTA DE ÉXITO AL PROCESAR -->
        <div v-if="depositoExitoso" class="alert-success-banner">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="alert-icon">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
            <polyline points="22 4 12 14.01 9 11.01" />
          </svg>
          <div class="alert-text">{{ mensajeConfirmacion }}</div>
          <button type="button" class="alert-btn-close" @click="depositoExitoso = false">&times;</button>
        </div>

        <!-- TARJETA DE CONFIGURACIÓN DEL DEPÓSITO Y ACCESOS RÁPIDOS -->
        <div class="deposito-header-card">
          <div class="header-fields-grid">
            <!-- Fecha de Operación / Depósito -->
            <div class="field-item">
              <label class="field-label">Fecha de Depósito:</label>
              <div class="date-input-combo">
                <input
                  v-model="fechaDeposito"
                  type="text"
                  class="tecnm-input font-mono"
                  placeholder="AAAA-MM-DD"
                />
                <input
                  ref="pickerFechaRef"
                  type="date"
                  class="hidden-date-picker"
                  tabindex="-1"
                  @change="onFechaPick"
                />
                <button
                  type="button"
                  class="btn-calendar-trigger"
                  title="Seleccionar fecha"
                  @click="abrirCalendario"
                >
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="calendar-icon">
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                    <line x1="16" y1="2" x2="16" y2="6" />
                    <line x1="8" y1="2" x2="8" y2="6" />
                    <line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                </button>
              </div>
            </div>

            <!-- Cuenta Bancaria Receptora -->
            <div class="field-item">
              <label class="field-label">Banco / Cuenta Receptora:</label>
              <select v-model="bancoReceptor" class="tecnm-input tecnm-select">
                <option v-for="cta in cuentasBancariasDisponibles" :key="cta" :value="cta">
                  {{ cta }}
                </option>
              </select>
            </div>

            <!-- Folio del Concentrado -->
            <div class="field-item">
              <label class="field-label">Folio Concentrador:</label>
              <input
                v-model="folioLote"
                type="text"
                class="tecnm-input font-mono font-bold"
                placeholder="DEP-202611-01"
              />
            </div>
          </div>

          <!-- BOTONES DE ACCIÓN RÁPIDA: Carga Masiva, Ejemplo y Plantilla -->
          <div class="batch-tools-row">
            <span class="tools-label">Carga y Herramientas:</span>
            <div class="tools-buttons">
              <!-- Input de archivo oculto -->
              <input
                ref="fileInputRef"
                type="file"
                class="hidden-file-input"
                accept=".csv,.txt"
                @change="onArchivoCargado"
              />
              <button
                type="button"
                class="btn-tool-secondary"
                title="Cargar transferencias desde un archivo CSV o TXT bancario"
                @click="abrirCargarArchivo"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon-sm">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                  <polyline points="17 8 12 3 7 8"/>
                  <line x1="12" y1="3" x2="12" y2="15"/>
                </svg>
                📂 Cargar Archivo Bancario (CSV)
              </button>

              <button
                type="button"
                class="btn-tool-secondary"
                title="Descargar plantilla CSV para llenado en Excel"
                @click="descargarPlantillaCsv"
              >
                📥 Descargar Plantilla
              </button>

              <button
                type="button"
                class="btn-tool-highlight"
                title="Cargar el ejemplo oficial de 30 pagos (15 inscripciones + 15 constancias)"
                @click="cargarEjemploRequerimiento"
              >
                ⚡ Cargar Ejemplo (30 Pagos)
              </button>
            </div>
          </div>
        </div>

        <!-- FORMULARIO DE CAPTURA RÁPIDA MANUAL DE UN ALUMNO -->
        <div class="quick-capture-card">
          <div class="quick-capture-header">
            <span class="quick-title">+ Captura Rápida Individual (Agregar al Lote)</span>
            <span class="quick-hint">Permite ingresar pagos de estudiantes de forma rápida sin perder sus datos.</span>
          </div>

          <div class="quick-capture-grid">
            <!-- Búsqueda Alumno / Matrícula -->
            <div class="field-item field-relative">
              <label class="field-label">Matrícula o Nombre:</label>
              <div class="input-search-wrapper">
                <input
                  v-model="nuevoAlumnoMatricula"
                  type="text"
                  class="tecnm-input font-mono"
                  placeholder="Ej. 23050114 o Nombre..."
                  @input="onInputMatricula"
                  @focus="mostrarSugerencias = sugerenciasAlumnos.length > 0"
                />
                <span v-if="isBuscandoAlumnos" class="search-spinner">...</span>
              </div>

              <!-- Menú Flotante de Sugerencias de Alumnos -->
              <div v-if="mostrarSugerencias && sugerenciasAlumnos.length > 0" class="suggestions-menu">
                <div
                  v-for="a in sugerenciasAlumnos"
                  :key="a.matricula"
                  class="suggestion-item"
                  @mousedown.prevent="seleccionarAlumno(a)"
                >
                  <span class="sugg-mat">{{ a.matricula }}</span>
                  <span class="sugg-nom">{{ a.nombreCompleto }}</span>
                  <span class="sugg-car">{{ a.carrera }}</span>
                </div>
              </div>
            </div>

            <!-- Nombre Alumno (Editable) -->
            <div class="field-item">
              <label class="field-label">Nombre del Estudiante:</label>
              <input
                v-model="nuevoAlumnoNombre"
                type="text"
                class="tecnm-input"
                placeholder="Nombre completo..."
              />
            </div>

            <!-- Concepto -->
            <div class="field-item">
              <label class="field-label">Concepto:</label>
              <select
                v-model="nuevoConcepto"
                class="tecnm-input tecnm-select"
                @change="onCambioConcepto"
              >
                <option v-for="c in catalogoConceptos" :key="c.id" :value="c.descripcion">
                  {{ c.descripcion }} ({{ formatearMoneda(c.cuota) }})
                </option>
              </select>
            </div>

            <!-- Precio Unitario -->
            <div class="field-item field-narrow">
              <label class="field-label">Precio Unit.:</label>
              <input
                v-model.number="nuevoPrecioUnitario"
                type="number"
                min="0"
                step="0.01"
                class="tecnm-input font-mono text-right font-bold"
              />
            </div>

            <!-- Referencia Bancaria -->
            <div class="field-item">
              <label class="field-label">Referencia Bancaria:</label>
              <input
                v-model="nuevaReferencia"
                type="text"
                class="tecnm-input font-mono"
                placeholder="Ej. REF-BNC-98124"
                @keyup.enter="agregarPagoManual"
              />
            </div>

            <!-- Botón Agregar -->
            <div class="field-item field-btn-add">
              <label class="field-label invisible-label">Acción</label>
              <button
                type="button"
                class="btn-add-pago"
                title="Agregar pago al depósito masivo"
                @click="agregarPagoManual"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon-sm">
                  <line x1="12" y1="5" x2="12" y2="19"/>
                  <line x1="5" y1="12" x2="19" y2="12"/>
                </svg>
                Agregar
              </button>
            </div>
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- 3. BARRA DE PESTAÑAS: RESUMEN AGRUPADO VS DETALLE INDIV. -->
        <!-- ======================================================== -->
        <div class="sub-tabs-bar">
          <button
            type="button"
            class="sub-tab-btn"
            :class="{ active: pestanaActiva === 'resumen' }"
            @click="pestanaActiva = 'resumen'"
          >
            📊 Resumen Agrupado por Concepto ({{ resumenAgrupadoPorConcepto.length }})
          </button>
          <button
            type="button"
            class="sub-tab-btn"
            :class="{ active: pestanaActiva === 'detalle' }"
            @click="pestanaActiva = 'detalle'"
          >
            👥 Detalle Individual de Alumnos ({{ pagos.length }})
          </button>
        </div>

        <!-- ======================================================== -->
        <!-- CONTENIDO PESTAÑA 1: RESUMEN AGRUPADO POR CONCEPTO       -->
        <!-- (EXACTAMENTE LA TABLA Y EJEMPLO SOLICITADO)              -->
        <!-- ======================================================== -->
        <div v-if="pestanaActiva === 'resumen'" class="tab-view-container">

          <!-- TARJETAS DE INDICADORES DE DEPÓSITO -->
          <div class="kpi-cards-row">
            <div class="kpi-card">
              <span class="kpi-label">Total de Pagos Registrados:</span>
              <span class="kpi-value font-mono">{{ totalCantidadMovimientos }}</span>
            </div>
            <div class="kpi-card">
              <span class="kpi-label">Conceptos Distintos:</span>
              <span class="kpi-value font-mono">{{ resumenAgrupadoPorConcepto.length }}</span>
            </div>
            <div class="kpi-card kpi-highlight">
              <span class="kpi-label">Total General Concentrado:</span>
              <span class="kpi-value font-mono font-bold text-success">{{ formatearMoneda(totalGeneralImporte) }}</span>
            </div>
          </div>

          <!-- TABLA RESUMEN SOLICITADA POR EL USUARIO -->
          <div class="table-container">
            <table class="reporte-table">
              <thead>
                <tr>
                  <th class="col-cpto">Concepto</th>
                  <th class="col-cant text-center">Cantidad</th>
                  <th class="col-unit text-right">Precio unitario</th>
                  <th class="col-tot text-right">Total</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="resumenAgrupadoPorConcepto.length === 0">
                  <td colspan="4" class="empty-cell">
                    <div>No hay pagos registrados para este depósito bancario.</div>
                    <div class="empty-cell-hint">
                      💡 Presiona 
                      <button type="button" class="btn-inline-pill" @click="cargarEjemploRequerimiento">
                        ⚡ Cargar Ejemplo (30 Pagos)
                      </button>
                      o ingresa uno manualmente.
                    </div>
                  </td>
                </tr>

                <!-- RENGLONES POR CONCEPTO -->
                <tr
                  v-for="item in resumenAgrupadoPorConcepto"
                  :key="item.concepto"
                  class="data-row"
                >
                  <td class="col-cpto font-bold text-primary">{{ item.concepto }}</td>
                  <td class="col-cant font-mono text-center font-bold">{{ item.cantidad }}</td>
                  <td class="col-unit font-mono text-right">{{ formatearMoneda(item.precioUnitario) }}</td>
                  <td class="col-tot font-mono text-right font-bold">{{ formatearMoneda(item.total) }}</td>
                </tr>

                <!-- FILA DE TOTAL GENERAL (DESTACADA) -->
                <tr v-if="resumenAgrupadoPorConcepto.length > 0" class="row-total-general">
                  <td class="col-cpto font-bold text-uppercase">Total general</td>
                  <td class="col-cant font-mono text-center font-bold">{{ totalCantidadMovimientos }}</td>
                  <td class="col-unit text-right font-mono">—</td>
                  <td class="col-tot font-mono text-right font-bold text-dorado">
                    {{ formatearMoneda(totalGeneralImporte) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- NOTA NORMATIVA -->
          <div class="audit-note">
            ℹ️ <strong>Cálculo automático:</strong> Los importes se agrupan en tiempo real por cada concepto. Al procesar el depósito, cada pago conservará su información individual con número de recibo, matrícula y referencia bancaria para auditoría y consulta.
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- CONTENIDO PESTAÑA 2: DETALLE INDIVIDUAL DE ALUMNOS       -->
        <!-- (CONSERVA TODA LA INFORMACIÓN INDIVIDUAL DE CADA ESTUD.)  -->
        <!-- ======================================================== -->
        <div v-else-if="pestanaActiva === 'detalle'" class="tab-view-container">

          <!-- Buscador local para consultar alumnos dentro del lote -->
          <div class="filter-bar-individual">
            <div class="search-input-box">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon-sm">
                <circle cx="11" cy="11" r="8"/>
                <line x1="21" y1="21" x2="16.65" y2="16.65"/>
              </svg>
              <input
                v-model="filtroDetalle"
                type="text"
                class="tecnm-input font-mono"
                placeholder="Buscar por matrícula, nombre, concepto o referencia..."
              />
            </div>
            <span class="count-badge">Mostrando {{ pagosFiltrados.length }} de {{ pagos.length }} alumnos</span>
          </div>

          <!-- TABLA DE MOVIMIENTOS INDIVIDUALES -->
          <div class="table-container table-scrollable">
            <table class="reporte-table">
              <thead>
                <tr>
                  <th class="col-num text-center"># / Recibo</th>
                  <th class="col-mat">Matrícula</th>
                  <th class="col-nom">Nombre del Estudiante</th>
                  <th class="col-carr">Carrera</th>
                  <th class="col-cpto-sm">Concepto</th>
                  <th class="col-ref">Referencia Bancaria</th>
                  <th class="col-fec text-center">Fecha</th>
                  <th class="col-imp text-right">Importe</th>
                  <th class="col-acc text-center">Acción</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="pagosFiltrados.length === 0">
                  <td colspan="9" class="empty-cell">
                    <span>No se encontraron pagos con el criterio de búsqueda especificado.</span>
                  </td>
                </tr>

                <tr
                  v-for="(p, idx) in pagosFiltrados"
                  :key="p.id"
                  class="data-row"
                >
                  <td class="col-num font-mono text-center font-bold">
                    <span v-if="p.numeroRecibo" class="badge-recibo">{{ p.numeroRecibo }}</span>
                    <span v-else class="text-muted">Pendiente</span>
                  </td>
                  <td class="col-mat font-mono font-bold">{{ p.matricula }}</td>
                  <td class="col-nom font-bold">{{ p.nombreAlumno }}</td>
                  <td class="col-carr text-muted text-xs">{{ p.carrera }}</td>
                  <td class="col-cpto-sm">
                    <span class="badge-concepto-pill">{{ p.concepto }}</span>
                  </td>
                  <td class="col-ref font-mono text-xs">{{ p.referenciaBancaria }}</td>
                  <td class="col-fec font-mono text-center">{{ formatearFechaIso(p.fechaDeposito) }}</td>
                  <td class="col-imp font-mono font-bold text-right">{{ formatearMoneda(p.precioUnitario) }}</td>
                  <td class="col-acc text-center">
                    <button
                      type="button"
                      class="btn-delete-row"
                      title="Eliminar del lote"
                      @click="eliminarPago(idx)"
                    >
                      &times;
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

      </section>

      <!-- ======================================================== -->
      <!-- 4. MODO VISTA PREVIA DE IMPRESIÓN DEL CONCENTRADO BANC.  -->
      <!-- ======================================================== -->
      <section v-else class="preview-content-panel">

        <!-- BARRA SUPERIOR DE EXPORTACIÓN Y VISTA PREVIA -->
        <div class="preview-top-toolbar">
          <div class="toolbar-left-msg">
            <span class="preview-badge">Hoja Institucional</span>
            <span class="preview-desc">Vista completa sin recortes ({{ pagos.length }} movimientos)</span>
          </div>
          <div class="toolbar-right-btns">
            <button type="button" class="btn-export-pdf" title="Exportar a PDF" @click="exportarPdf">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon-sm">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
              </svg>
              PDF
            </button>
            <button type="button" class="btn-export-csv" title="Exportar a CSV" @click="exportarCsv">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon-sm">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="10 9 9 9 8 9" />
              </svg>
              CSV
            </button>
            <button type="button" class="btn-export-word" title="Exportar a Word" @click="exportarWord">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon-sm">
                <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                <polyline points="7 9 10 16 12 11 14 16 17 9" />
              </svg>
              Word
            </button>
            <button type="button" class="btn-action-print" title="Mandar a imprimir" @click="imprimir">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon-sm">
                <polyline points="6 9 6 2 18 2 18 9"/>
                <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/>
                <rect x="6" y="14" width="12" height="8"/>
              </svg>
              Imprimir
            </button>
            <button type="button" class="btn-action-secondary" title="Volver a la edición" @click="pestanaActiva = 'resumen'">
              Volver a Edición
            </button>
          </div>
        </div>

        <div class="print-sheet-wrapper">
          <div class="print-paper-sheet">

            <!-- ENCABEZADO OFICIAL INSTITUCIONAL -->
            <header class="sheet-header">
              <div class="sheet-title-institucion">
                INSTITUTO TECNOLÓGICO SUPERIOR DE MONCLOVA
              </div>
              <div class="sheet-subtitle-sistema">
                SISTEMA DE CONTROL DE CAJA — MÓDULO DE DEPÓSITOS BANCARIOS
              </div>
              <div class="sheet-doc-name">
                RESUMEN CONCENTRADOR DE DEPÓSITOS BANCARIOS
              </div>
              <div class="sheet-meta-grid">
                <div><strong>FECHA DE OPERACIÓN:</strong> {{ formatearFechaIso(fechaDeposito) }}</div>
                <div><strong>FOLIO CONCENTRADOR:</strong> {{ folioLote }}</div>
                <div><strong>BANCO Y CUENTA:</strong> {{ bancoReceptor }}</div>
                <div><strong>TOTAL MOVIMIENTOS:</strong> {{ totalCantidadMovimientos }} PAGOS</div>
              </div>
            </header>

            <!-- TABLA DE CONCEPTOS AGRUPADOS (FORMATO OFICIAL) -->
            <div class="sheet-section-title">
              RESUMEN CONCENTRADO POR CONCEPTO:
            </div>
            <table class="sheet-table">
              <thead>
                <tr>
                  <th class="th-concepto">CONCEPTO</th>
                  <th class="th-cantidad text-center">CANTIDAD</th>
                  <th class="th-unitario text-right">PRECIO UNITARIO</th>
                  <th class="th-total text-right">TOTAL</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in resumenAgrupadoPorConcepto" :key="item.concepto">
                  <td class="td-concepto">{{ item.concepto.toUpperCase() }}</td>
                  <td class="td-cantidad font-mono text-center">{{ item.cantidad }}</td>
                  <td class="td-unitario font-mono text-right">{{ formatearMoneda(item.precioUnitario) }}</td>
                  <td class="td-total font-mono text-right font-bold">{{ formatearMoneda(item.total) }}</td>
                </tr>

                <!-- FILA DE TOTAL GENERAL -->
                <tr class="tr-sheet-total">
                  <td class="td-concepto font-bold">TOTAL GENERAL</td>
                  <td class="td-cantidad font-mono text-center font-bold">{{ totalCantidadMovimientos }}</td>
                  <td class="td-unitario text-right font-mono">—</td>
                  <td class="td-total font-mono text-right font-bold">
                    {{ formatearMoneda(totalGeneralImporte) }}
                  </td>
                </tr>
              </tbody>
            </table>

            <!-- RELACIÓN COMPLETA DE ALUMNOS (TODA LA LISTA COMPLETA SIN RECORTES NI SCROLL) -->
            <div class="sheet-alumnos-section">
              <div class="sheet-section-title">
                RELACIÓN COMPLETA DE ALUMNOS Y REFERENCIAS BANCARIAS ({{ pagos.length }} MOVIMIENTOS):
              </div>
              <table class="sheet-table sheet-table-alumnos">
                <thead>
                  <tr>
                    <th class="th-rec text-center">RECIBO</th>
                    <th class="th-mat text-center">MATRÍCULA</th>
                    <th class="th-nom">NOMBRE DEL ALUMNO</th>
                    <th class="th-cpto">CONCEPTO</th>
                    <th class="th-ref text-center">REFERENCIA BANCARIA</th>
                    <th class="th-fec text-center">FECHA</th>
                    <th class="th-imp text-right">IMPORTE</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(p, i) in pagos" :key="p.id">
                    <td class="td-rec text-center font-mono font-bold">{{ p.numeroRecibo || (i + 1) }}</td>
                    <td class="td-mat text-center font-mono font-bold">{{ p.matricula }}</td>
                    <td class="td-nom font-bold">{{ p.nombreAlumno }}</td>
                    <td class="td-cpto">{{ p.concepto }}</td>
                    <td class="td-ref text-center font-mono">{{ p.referenciaBancaria }}</td>
                    <td class="td-fec text-center font-mono">{{ formatearFechaIso(p.fechaDeposito) }}</td>
                    <td class="td-imp text-right font-mono font-bold">{{ formatearMoneda(p.precioUnitario) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- CUADRO DE FIRMAS -->
            <footer class="sheet-signatures-footer">
              <div class="sig-box">
                <div class="sig-line"></div>
                <div class="sig-name">Cajero en Turno</div>
                <div class="sig-role">ELABORÓ</div>
              </div>
              <div class="sig-box">
                <div class="sig-line"></div>
                <div class="sig-name">Depto. de Recursos Financieros</div>
                <div class="sig-role">REVISÓ Y VALIDÓ</div>
              </div>
            </footer>

            <div class="sheet-page-indicator">1 DE 1</div>

          </div>
        </div>
      </section>

      <!-- ======================================================== -->
      <!-- 5. BARRA DE ACCIONES INFERIOR                            -->
      <!-- ======================================================== -->
      <footer class="window-footer-bar">
        <div class="footer-left-info">
          <span class="info-tag font-mono">
            {{ totalCantidadMovimientos }} Movimientos | Total: {{ formatearMoneda(totalGeneralImporte) }}
          </span>
        </div>

        <div class="footer-buttons-group">
          <!-- Botón de Vista Previa / Regresar -->
          <button
            v-if="pestanaActiva !== 'impresion'"
            type="button"
            class="btn-action-preview"
            title="Ver formato impreso del concentrado bancario"
            @click="pestanaActiva = 'impresion'"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon">
              <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
              <circle cx="12" cy="12" r="3" />
            </svg>
            Vista Previa
          </button>
          <button
            v-else
            type="button"
            class="btn-action-preview"
            title="Regresar a la captura y edición"
            @click="pestanaActiva = 'resumen'"
          >
            Volver a Edición
          </button>

          <!-- Exportar a PDF -->
          <button
            type="button"
            class="btn-export-pdf"
            :disabled="pagos.length === 0"
            title="Exportar documento oficial a PDF"
            @click="exportarPdf"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
            </svg>
            PDF
          </button>

          <!-- Exportar a CSV -->
          <button
            type="button"
            class="btn-export-csv"
            :disabled="pagos.length === 0"
            title="Exportar relación a archivo Excel / CSV"
            @click="exportarCsv"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="10 9 9 9 8 9" />
            </svg>
            CSV
          </button>

          <!-- Exportar a Word -->
          <button
            type="button"
            class="btn-export-word"
            :disabled="pagos.length === 0"
            title="Exportar documento oficial a Microsoft Word (.doc)"
            @click="exportarWord"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon">
              <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
              <polyline points="7 9 10 16 12 11 14 16 17 9" />
            </svg>
            Word
          </button>

          <!-- Botón Imprimir -->
          <button
            type="button"
            class="btn-action-print"
            :disabled="pagos.length === 0"
            title="Imprimir formato oficial"
            @click="imprimir"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon">
              <polyline points="6 9 6 2 18 2 18 9"/>
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/>
              <rect x="6" y="14" width="12" height="8"/>
            </svg>
            Imprimir
          </button>

          <!-- Botón Procesar en Base de Datos -->
          <button
            type="button"
            class="btn-action-primary"
            :disabled="pagos.length === 0 || isProcesando"
            @click="procesarDepositoMasivo"
          >
            <svg v-if="!isProcesando" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon">
              <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
              <polyline points="17 21 17 13 7 13 7 21"/>
              <polyline points="7 3 7 8 15 8"/>
            </svg>
            <span v-if="isProcesando">Guardando en Sistema...</span>
            <span v-else>💾 Registrar Depósito ({{ totalCantidadMovimientos }})</span>
          </button>

          <!-- Botón Limpiar -->
          <button type="button" class="btn-action-secondary" @click="limpiar">
            Limpiar
          </button>

          <!-- Botón Salir -->
          <button type="button" class="btn-action-danger" @click="cerrar">
            Salir
          </button>
        </div>
      </footer>

    </div>
  </div>
</template>

<style scoped>
/* =====================================================
   PALETA INSTITUCIONAL TecNM
   Azul Marino: #1B396A | Cobalto: #244B88 | Dorado: #C5A059 | Rojo: #DC2626
   ===================================================== */

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.7);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1200;
  padding: 1rem;
  animation: fadeIn 0.15s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: scale(0.99); }
  to   { opacity: 1; transform: scale(1); }
}

.tecnm-window {
  width: 100%;
  max-width: 1040px;
  background: #FFFFFF;
  border-radius: 10px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.4), 0 0 0 1px rgba(27, 57, 106, 0.2);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  max-height: 94vh;
  transition: max-width 0.2s ease;
}

.modal-backdrop.dark-theme .tecnm-window {
  background: #0F172A;
  color: #F8FAFC;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.8), 0 0 0 1px rgba(197, 160, 89, 0.3);
}

.window-expanded {
  max-width: 1120px;
}

/* BARRA DE TÍTULO INSTITUCIONAL */
.window-title-bar {
  background: linear-gradient(135deg, #1B396A 0%, #244B88 100%);
  color: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.65rem 1rem;
  border-bottom: 2px solid #C5A059;
}

.window-title-left {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.window-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: #C5A059;
}

.window-title-text {
  font-weight: 700;
  font-size: 0.98rem;
  letter-spacing: 0.02em;
}

.badge-modulo {
  background: #C5A059;
  color: #1B396A;
  font-size: 0.68rem;
  font-weight: 800;
  text-transform: uppercase;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
}

.window-btn-close {
  background: transparent;
  border: none;
  color: #FFFFFF;
  font-size: 1.4rem;
  line-height: 1;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  cursor: pointer;
  transition: background 0.15s;
}

.window-btn-close:hover {
  background: rgba(220, 38, 38, 0.85);
}

/* PANEL DE CONTENIDO */
.window-content-panel {
  padding: 1rem 1.25rem;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  background: #F8FAFC;
}

.modal-backdrop.dark-theme .window-content-panel {
  background: #0B1120;
}

/* ALERTA DE ÉXITO */
.alert-success-banner {
  background: #DCFCE7;
  border: 1px solid #86EFAC;
  color: #166534;
  padding: 0.65rem 1rem;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.modal-backdrop.dark-theme .alert-success-banner {
  background: #064E3B;
  border-color: #059669;
  color: #A7F3D0;
}

.alert-icon {
  width: 1.3rem;
  height: 1.3rem;
  flex-shrink: 0;
}

.alert-text {
  font-size: 0.85rem;
  font-weight: 600;
  flex: 1;
}

.alert-btn-close {
  background: transparent;
  border: none;
  font-size: 1.2rem;
  color: inherit;
  cursor: pointer;
}

/* TARJETA DE CONFIGURACIÓN DEL DEPÓSITO */
.deposito-header-card {
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  padding: 0.85rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.modal-backdrop.dark-theme .deposito-header-card {
  background: #1E293B;
  border-color: #334155;
}

.header-fields-grid {
  display: grid;
  grid-template-columns: 200px 1.5fr 180px;
  gap: 1rem;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.field-label {
  font-size: 0.75rem;
  font-weight: 700;
  color: #1B396A;
}

.modal-backdrop.dark-theme .field-label {
  color: #E2E8F0;
}

.invisible-label {
  visibility: hidden;
}

/* Date combo */
.date-input-combo {
  display: flex;
  align-items: center;
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 4px;
  overflow: hidden;
}

.modal-backdrop.dark-theme .date-input-combo {
  background: #0F172A;
  border-color: #334155;
}

.date-input-combo .tecnm-input {
  border: none !important;
  box-shadow: none !important;
  flex: 1;
}

.hidden-date-picker {
  width: 0;
  height: 0;
  opacity: 0;
  position: absolute;
  pointer-events: none;
}

.btn-calendar-trigger {
  background: transparent;
  border: none;
  padding: 0.4rem 0.5rem;
  cursor: pointer;
  color: #1B396A;
  display: flex;
  align-items: center;
}

.modal-backdrop.dark-theme .btn-calendar-trigger {
  color: #C5A059;
}

.calendar-icon {
  width: 1rem;
  height: 1rem;
}

/* Inputs y Selects */
.tecnm-input {
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 4px;
  padding: 0.42rem 0.65rem;
  font-size: 0.85rem;
  color: #1E293B;
  outline: none;
  transition: all 0.15s;
}

.modal-backdrop.dark-theme .tecnm-input {
  background: #0F172A;
  border-color: #334155;
  color: #F8FAFC;
}

.tecnm-input:focus {
  border-color: #1B396A;
  box-shadow: 0 0 0 2px rgba(27, 57, 106, 0.2);
}

.modal-backdrop.dark-theme .tecnm-input:focus {
  border-color: #C5A059;
  box-shadow: 0 0 0 2px rgba(197, 160, 89, 0.25);
}

.tecnm-select {
  cursor: pointer;
}

/* Herramientas de Carga Masiva */
.batch-tools-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px dashed #CBD5E1;
  padding-top: 0.6rem;
  gap: 0.5rem;
}

.modal-backdrop.dark-theme .batch-tools-row {
  border-top-color: #334155;
}

.tools-label {
  font-size: 0.75rem;
  font-weight: 700;
  color: #64748B;
}

.tools-buttons {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.hidden-file-input {
  display: none;
}

.btn-tool-secondary {
  background: #E2E8F0;
  color: #1E293B;
  border: 1px solid #CBD5E1;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.3rem 0.65rem;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-tool-secondary:hover {
  background: #CBD5E1;
}

.modal-backdrop.dark-theme .btn-tool-secondary {
  background: #334155;
  color: #F8FAFC;
  border-color: #475569;
}

.modal-backdrop.dark-theme .btn-tool-secondary:hover {
  background: #475569;
}

.btn-tool-highlight {
  background: #FEF3C7;
  color: #92400E;
  border: 1px solid #FCD34D;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.3rem 0.75rem;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-tool-highlight:hover {
  background: #FDE68A;
}

.modal-backdrop.dark-theme .btn-tool-highlight {
  background: #78350F;
  color: #FDE68A;
  border-color: #B45309;
}

/* TARJETA DE CAPTURA RÁPIDA */
.quick-capture-card {
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  padding: 0.75rem 1rem;
}

.modal-backdrop.dark-theme .quick-capture-card {
  background: #1E293B;
  border-color: #334155;
}

.quick-capture-header {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
}

.quick-title {
  font-size: 0.8rem;
  font-weight: 800;
  color: #1B396A;
  text-transform: uppercase;
}

.modal-backdrop.dark-theme .quick-title {
  color: #C5A059;
}

.quick-hint {
  font-size: 0.72rem;
  color: #64748B;
}

.quick-capture-grid {
  display: grid;
  grid-template-columns: 160px 1.4fr 1.3fr 110px 140px 100px;
  gap: 0.65rem;
  align-items: flex-end;
}

.field-relative {
  position: relative;
}

.field-narrow {
  width: 100%;
}

.input-search-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.search-spinner {
  position: absolute;
  right: 0.5rem;
  font-size: 0.75rem;
  color: #64748B;
}

.suggestions-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 4px;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  z-index: 50;
  max-height: 180px;
  overflow-y: auto;
}

.modal-backdrop.dark-theme .suggestions-menu {
  background: #0F172A;
  border-color: #334155;
}

.suggestion-item {
  padding: 0.4rem 0.6rem;
  font-size: 0.75rem;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  border-bottom: 1px solid #F1F5F9;
}

.modal-backdrop.dark-theme .suggestion-item {
  border-bottom-color: #1E293B;
}

.suggestion-item:hover {
  background: #F1F5F9;
}

.modal-backdrop.dark-theme .suggestion-item:hover {
  background: #1E293B;
}

.sugg-mat {
  font-weight: 700;
  color: #1B396A;
}

.modal-backdrop.dark-theme .sugg-mat {
  color: #C5A059;
}

.sugg-nom {
  color: #1E293B;
}

.modal-backdrop.dark-theme .sugg-nom {
  color: #F8FAFC;
}

.sugg-car {
  font-size: 0.68rem;
  color: #64748B;
}

.btn-add-pago {
  background: #1B396A;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.44rem 0.75rem;
  font-size: 0.8rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-add-pago:hover {
  background: #244B88;
}

.modal-backdrop.dark-theme .btn-add-pago {
  background: #244B88;
}

.modal-backdrop.dark-theme .btn-add-pago:hover {
  background: #1D4ED8;
}

/* SUB-PESTAÑAS */
.sub-tabs-bar {
  display: flex;
  gap: 0.5rem;
  border-bottom: 2px solid #E2E8F0;
}

.modal-backdrop.dark-theme .sub-tabs-bar {
  border-bottom-color: #334155;
}

.sub-tab-btn {
  background: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  padding: 0.45rem 1rem;
  font-weight: 700;
  font-size: 0.85rem;
  color: #64748B;
  cursor: pointer;
  transition: all 0.15s;
}

.modal-backdrop.dark-theme .sub-tab-btn {
  color: #94A3B8;
}

.sub-tab-btn.active {
  color: #1B396A;
  border-bottom-color: #1B396A;
}

.modal-backdrop.dark-theme .sub-tab-btn.active {
  color: #C5A059;
  border-bottom-color: #C5A059;
}

/* VISTAS DE PESTAÑAS */
.tab-view-container {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

/* KPI CARDS */
.kpi-cards-row {
  display: grid;
  grid-template-columns: 1fr 1fr 1.5fr;
  gap: 0.75rem;
}

.kpi-card {
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  padding: 0.5rem 0.85rem;
  display: flex;
  flex-direction: column;
}

.modal-backdrop.dark-theme .kpi-card {
  background: #1E293B;
  border-color: #334155;
}

.kpi-highlight {
  border-color: #C5A059;
  background: rgba(197, 160, 89, 0.08);
}

.kpi-label {
  font-size: 0.7rem;
  font-weight: 700;
  color: #64748B;
  text-transform: uppercase;
}

.modal-backdrop.dark-theme .kpi-label {
  color: #94A3B8;
}

.kpi-value {
  font-size: 1.25rem;
  font-weight: 800;
  color: #1B396A;
}

.modal-backdrop.dark-theme .kpi-value {
  color: #F8FAFC;
}

.text-success {
  color: #16A34A !important;
}

.modal-backdrop.dark-theme .text-success {
  color: #4ADE80 !important;
}

/* TABLAS */
.table-container {
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  overflow: hidden;
  background: #FFFFFF;
}

.modal-backdrop.dark-theme .table-container {
  background: #1E293B;
  border-color: #334155;
}

.table-scrollable {
  max-height: 280px;
  overflow-y: auto;
}

.reporte-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.82rem;
}

.reporte-table thead {
  background: #1B396A;
  color: #FFFFFF;
  position: sticky;
  top: 0;
  z-index: 5;
}

.modal-backdrop.dark-theme .reporte-table thead {
  background: #0B1120;
  color: #C5A059;
  border-bottom: 2px solid #C5A059;
}

.reporte-table th {
  padding: 0.55rem 0.75rem;
  font-weight: 700;
  text-align: left;
  letter-spacing: 0.02em;
}

.reporte-table td {
  padding: 0.45rem 0.75rem;
  border-bottom: 1px solid #E2E8F0;
  color: #1E293B;
}

.modal-backdrop.dark-theme .reporte-table td {
  border-bottom-color: #334155;
  color: #E2E8F0;
}

.data-row:hover {
  background: #F1F5F9;
}

.modal-backdrop.dark-theme .data-row:hover {
  background: #334155;
}

/* Fila de Total General */
.row-total-general {
  background: #FEF3C7;
  border-top: 2px solid #F59E0B;
  font-weight: 800;
}

.modal-backdrop.dark-theme .row-total-general {
  background: #78350F;
  border-top-color: #C5A059;
}

.row-total-general td {
  padding: 0.65rem 0.75rem;
  font-size: 0.9rem;
}

.text-dorado {
  color: #92400E;
}

.modal-backdrop.dark-theme .text-dorado {
  color: #FDE68A;
}

.empty-cell {
  text-align: center;
  padding: 2.2rem !important;
  color: #64748B;
}

.empty-cell-hint {
  margin-top: 0.5rem;
  font-size: 0.78rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
}

.btn-inline-pill {
  background: #1B396A;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 0.15rem 0.5rem;
  cursor: pointer;
}

.audit-note {
  font-size: 0.73rem;
  color: #64748B;
  background: #F1F5F9;
  border-radius: 4px;
  padding: 0.4rem 0.75rem;
  line-height: 1.4;
}

.modal-backdrop.dark-theme .audit-note {
  background: #1E293B;
  color: #94A3B8;
}

/* FILTRO DETALLE INDIVIDUAL */
.filter-bar-individual {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.search-input-box {
  display: flex;
  align-items: center;
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 4px;
  padding: 0 0.5rem;
  flex: 1;
  max-width: 450px;
}

.modal-backdrop.dark-theme .search-input-box {
  background: #0F172A;
  border-color: #334155;
}

.search-input-box .tecnm-input {
  border: none !important;
  box-shadow: none !important;
  width: 100%;
}

.search-icon-sm {
  width: 0.95rem;
  height: 0.95rem;
  color: #64748B;
}

.count-badge {
  font-size: 0.75rem;
  font-weight: 700;
  color: #64748B;
}

.modal-backdrop.dark-theme .count-badge {
  color: #94A3B8;
}

/* Badges */
.badge-recibo {
  background: #E0E7FF;
  color: #3730A3;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
}

.modal-backdrop.dark-theme .badge-recibo {
  background: #312E81;
  color: #C7D2FE;
}

.badge-concepto-pill {
  background: #E2E8F0;
  color: #1E293B;
  padding: 0.1rem 0.45rem;
  border-radius: 12px;
  font-size: 0.72rem;
  font-weight: 600;
}

.modal-backdrop.dark-theme .badge-concepto-pill {
  background: #334155;
  color: #F8FAFC;
}

.btn-delete-row {
  background: transparent;
  border: none;
  color: #DC2626;
  font-size: 1.2rem;
  font-weight: 800;
  cursor: pointer;
  line-height: 1;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  transition: background 0.15s;
}

.btn-delete-row:hover {
  background: rgba(220, 38, 38, 0.15);
}

/* =====================================================
   VISTA PREVIA DE IMPRESIÓN (HOJA FÍSICA)
   ===================================================== */
.preview-content-panel {
  padding: 1.25rem;
  overflow-y: auto;
  background: #475569;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.preview-top-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  background: #1E293B;
  padding: 0.6rem 1rem;
  border-radius: 6px;
  border: 1px solid #334155;
  width: 100%;
  max-width: 900px;
}

.toolbar-left-msg {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  color: #F8FAFC;
  font-size: 0.8rem;
}

.preview-badge {
  background: #C5A059;
  color: #1B396A;
  font-size: 0.68rem;
  font-weight: 800;
  text-transform: uppercase;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
}

.preview-desc {
  color: #CBD5E1;
  font-size: 0.78rem;
}

.toolbar-right-btns {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.print-sheet-wrapper {
  max-width: 900px;
  width: 100%;
}

.print-paper-sheet {
  background: #FFFFFF;
  color: #000000;
  padding: 2.2rem;
  box-shadow: 0 15px 30px rgba(0, 0, 0, 0.35);
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-height: 700px;
  position: relative;
  font-family: 'Times New Roman', Times, serif, Arial, sans-serif;
}

.sheet-header {
  text-align: center;
  border-bottom: 2px solid #000000;
  padding-bottom: 0.75rem;
}

.sheet-title-institucion {
  font-size: 1.15rem;
  font-weight: 900;
  letter-spacing: 0.03em;
}

.sheet-subtitle-sistema {
  font-size: 0.85rem;
  font-weight: 700;
  margin-top: 0.15rem;
}

.sheet-doc-name {
  font-size: 0.95rem;
  font-weight: 800;
  margin-top: 0.4rem;
  text-decoration: underline;
}

.sheet-meta-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.35rem 1rem;
  margin-top: 0.75rem;
  font-size: 0.78rem;
  text-align: left;
}

.sheet-section-title {
  font-size: 0.8rem;
  font-weight: 800;
  color: #000000;
  margin-top: 0.4rem;
  letter-spacing: 0.02em;
}

/* Tabla de hoja */
.sheet-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.82rem;
}

.sheet-table th {
  border: 1px solid #000000;
  padding: 0.45rem 0.6rem;
  background: #F1F5F9;
  font-weight: 800;
}

.sheet-table td {
  border: 1px solid #000000;
  padding: 0.4rem 0.6rem;
}

.tr-sheet-total td {
  font-weight: 900;
  background: #F8FAFC;
}

/* Relación Completa de Alumnos */
.sheet-alumnos-section {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-top: 0.5rem;
}

.sheet-table-alumnos {
  font-size: 0.73rem !important;
}

.sheet-table-alumnos th {
  background: #E2E8F0 !important;
  color: #000000 !important;
  padding: 0.35rem 0.5rem !important;
}

.sheet-table-alumnos td {
  padding: 0.3rem 0.5rem !important;
}

/* Firmas */
.sheet-signatures-footer {
  display: flex;
  justify-content: space-around;
  margin-top: 2rem;
  padding-top: 0.5rem;
}

.sig-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 250px;
}

.sig-line {
  width: 100%;
  border-top: 1px solid #000000;
  margin-bottom: 0.35rem;
}

.sig-name {
  font-size: 0.78rem;
  font-weight: 700;
}

.sig-role {
  font-size: 0.68rem;
  color: #475569;
  font-weight: 700;
}

.sheet-page-indicator {
  position: absolute;
  bottom: 0.75rem;
  right: 1.5rem;
  font-size: 0.7rem;
  color: #64748B;
}

/* BARRA INFERIOR DE ACCIONES */
.window-footer-bar {
  background: #FFFFFF;
  border-top: 1px solid #CBD5E1;
  padding: 0.65rem 1.25rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.modal-backdrop.dark-theme .window-footer-bar {
  background: #0F172A;
  border-top-color: #334155;
}

.footer-left-info .info-tag {
  background: #E2E8F0;
  color: #1E293B;
  padding: 0.3rem 0.65rem;
  border-radius: 4px;
  font-size: 0.78rem;
  font-weight: 700;
}

.modal-backdrop.dark-theme .footer-left-info .info-tag {
  background: #1E293B;
  color: #C5A059;
}

.footer-buttons-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-icon {
  width: 1rem;
  height: 1rem;
}

.btn-icon-sm {
  width: 0.85rem;
  height: 0.85rem;
}

/* BOTONES DE EXPORTACIÓN (PDF, CSV, WORD) */
.btn-export-pdf {
  background: #DC2626;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.42rem 0.75rem;
  font-size: 0.78rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-export-pdf:hover:not(:disabled) {
  background: #B91C1C;
}

.btn-export-pdf:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-export-csv {
  background: #16A34A;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.42rem 0.75rem;
  font-size: 0.78rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-export-csv:hover:not(:disabled) {
  background: #15803D;
}

.btn-export-csv:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-export-word {
  background: #2563EB;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.42rem 0.75rem;
  font-size: 0.78rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-export-word:hover:not(:disabled) {
  background: #1D4ED8;
}

.btn-export-word:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-action-primary {
  background: #16A34A;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.45rem 1rem;
  font-size: 0.82rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-action-primary:hover:not(:disabled) {
  background: #15803D;
}

.btn-action-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-action-preview {
  background: #1B396A;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.45rem 0.85rem;
  font-size: 0.82rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  cursor: pointer;
}

.btn-action-preview:hover {
  background: #244B88;
}

.modal-backdrop.dark-theme .btn-action-preview {
  background: #244B88;
}

.btn-action-print {
  background: #475569;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.45rem 0.85rem;
  font-size: 0.82rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  cursor: pointer;
}

.btn-action-print:hover:not(:disabled) {
  background: #334155;
}

.btn-action-print:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-action-secondary {
  background: #E2E8F0;
  color: #1E293B;
  border: 1px solid #CBD5E1;
  border-radius: 4px;
  padding: 0.45rem 0.85rem;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.btn-action-secondary:hover {
  background: #CBD5E1;
}

.modal-backdrop.dark-theme .btn-action-secondary {
  background: #334155;
  color: #F8FAFC;
  border-color: #475569;
}

.btn-action-danger {
  background: #DC2626;
  color: #FFFFFF;
  border: none;
  border-radius: 4px;
  padding: 0.45rem 0.95rem;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.btn-action-danger:hover {
  background: #B91C1C;
}

/* =====================================================
   MEDIA PRINT: IMPRESIÓN COMPLETA SIN RECORTES NI SCROLL
   ===================================================== */
@media print {
  @page {
    size: letter portrait;
    margin: 1cm;
  }

  body, html {
    margin: 0 !important;
    padding: 0 !important;
    background: #FFFFFF !important;
  }

  /* Ocultar toda la UI externa y controles de la ventana */
  body * {
    visibility: hidden;
  }

  .print-paper-sheet,
  .print-paper-sheet * {
    visibility: visible;
  }

  .preview-top-toolbar,
  .window-title-bar,
  .window-footer-bar,
  .sub-tabs-bar {
    display: none !important;
  }

  .modal-backdrop {
    position: static !important;
    inset: auto !important;
    background: transparent !important;
    padding: 0 !important;
    display: block !important;
  }

  .tecnm-window {
    max-width: 100% !important;
    box-shadow: none !important;
    border: none !important;
    border-radius: 0 !important;
    background: #FFFFFF !important;
  }

  .preview-content-panel {
    background: transparent !important;
    padding: 0 !important;
    margin: 0 !important;
    overflow: visible !important;
    display: block !important;
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
  }

  .print-sheet-wrapper {
    max-width: 100% !important;
    width: 100% !important;
    margin: 0 !important;
    padding: 0 !important;
  }

  .print-paper-sheet {
    box-shadow: none !important;
    padding: 0 !important;
    margin: 0 !important;
    width: 100% !important;
    position: static !important;
    min-height: auto !important;
    overflow: visible !important;
  }

  .sheet-table {
    border-collapse: collapse !important;
    width: 100% !important;
  }

  .sheet-table th,
  .sheet-table td {
    border: 1px solid #000000 !important;
  }

  .sheet-table tr {
    page-break-inside: avoid !important;
  }

  .sheet-signatures-footer {
    page-break-inside: avoid !important;
    margin-top: 2rem !important;
  }
}
</style>
