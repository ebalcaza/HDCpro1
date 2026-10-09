/** 1 ppm de CO2 atmosférico ≈ 17.600 millones de toneladas de CO2 */
export const TONELADAS_POR_PPM = 17_600_000_000

const nf = (min, max) => new Intl.NumberFormat('es-AR', { minimumFractionDigits: min, maximumFractionDigits: max })
const n0 = nf(0, 0)
const n2 = nf(0, 2)
const n3 = nf(0, 3)

export const numero = (v, decimales = 2) => (v == null || isNaN(v) ? '—' : nf(0, decimales).format(v))

export const kg = (v) => (v == null ? '—' : `${v >= 1000 ? n0.format(v) : n2.format(v)} kg`)

/** Los totales se muestran en toneladas de CO2 */
export const toneladas = (kgCo2) => (kgCo2 == null ? '—' : n3.format(kgCo2 / 1000))

export const toneladasTexto = (kgCo2) => `${toneladas(kgCo2)} t CO₂`

/** Equivalente en ppm atmosféricas (valor muy pequeño: notación científica) */
export const ppm = (kgCo2) => {
  if (!kgCo2) return '0 ppm'
  const v = kgCo2 / 1000 / TONELADAS_POR_PPM
  return `${v.toExponential(2).replace('.', ',')} ppm`
}

export const porcentaje = (v) => (v == null ? '—' : `${n2.format(v)} %`)

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic']
const MESES_LARGOS = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre']

/** "2026-03-01" o "2026-03" -> "mar 2026" */
export const periodo = (p, largo = false) => {
  if (!p) return '—'
  const [a, m] = p.split('-')
  const meses = largo ? MESES_LARGOS : MESES
  return `${meses[Number(m) - 1]} ${a}`
}

/** "2026-03-01" -> "2026-03" (valor de input type=month) */
export const aMes = (p) => (p ? p.slice(0, 7) : '')

/** "2026-03" -> "2026-03-01" (DateOnly para la API) */
export const desdeMes = (m) => (m ? `${m}-01` : null)

export const mesActual = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

export const fecha = (f) => (f ? new Date(f).toLocaleDateString('es-AR') : '—')
export const fechaHora = (f) => (f ? new Date(f).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' }) : '—')

export const ESTADOS_COLABORACION = {
  PENDIENTE: { texto: 'Pendiente', clase: 'badge-marron', icono: 'bi-hourglass-split' },
  ACEPTADA: { texto: 'Aceptada', clase: 'badge-verde', icono: 'bi-check2-circle' },
  RECHAZADA: { texto: 'Rechazada', clase: 'badge-gris', icono: 'bi-x-circle' },
  REVOCADA: { texto: 'Revocada', clase: 'badge-rojo', icono: 'bi-slash-circle' },
}
