import { jsPDF } from 'jspdf'
import autoTable from 'jspdf-autotable'
import { EQUIVALENCIA_PPM, fechaHora, numero, periodo, porcentaje, ppm } from './formato'

// La fuente estándar de jsPDF no tiene el subíndice de CO₂: en el PDF se escribe CO2
const toneladasTexto = (kg) => `${numero(kg / 1000, 3)} t CO2`

// Formato con cantidad FIJA de decimales para que las columnas sean consistentes
// (evita que "1.365" se lea como 1,365 al lado de "1.046,1")
const fijo = (v, dec) => Number(v ?? 0).toLocaleString('es-AR', { minimumFractionDigits: dec, maximumFractionDigits: dec })
const kg1 = (v) => fijo(v, 1)
const t3 = (kg) => fijo(kg / 1000, 3)

const VERDE = [46, 139, 87]
const AZUL = [47, 109, 181]
const TEXTO = [31, 42, 37]
const GRIS = [107, 118, 113]

function encabezado(doc, titulo, subtitulo) {
  const ancho = doc.internal.pageSize.getWidth()
  doc.setFillColor(...VERDE)
  doc.rect(0, 0, ancho, 26, 'F')
  doc.setFillColor(...AZUL)
  doc.rect(ancho * 0.62, 0, ancho * 0.38, 26, 'F')
  doc.setTextColor(255, 255, 255)
  doc.setFont('helvetica', 'bold')
  doc.setFontSize(15)
  doc.text('Huella de Carbono', 14, 11)
  doc.setFont('helvetica', 'normal')
  doc.setFontSize(10)
  doc.text(titulo, 14, 18)
  if (subtitulo) doc.text(subtitulo, ancho - 14, 18, { align: 'right' })
  doc.setTextColor(...TEXTO)
  return 36
}

function pie(doc) {
  const n = doc.getNumberOfPages()
  const generado = `Generado el ${fechaHora(new Date())}`
  for (let i = 1; i <= n; i++) {
    doc.setPage(i)
    // FIX: el tamaño se lee DESPUÉS de setPage, porque hay páginas verticales y horizontales.
    // Antes se tomaba el de la última página (horizontal) y el pie quedaba a mitad de hoja
    // y el número de página fuera del borde en las verticales.
    const ancho = doc.internal.pageSize.getWidth()
    const alto = doc.internal.pageSize.getHeight()
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(8)
    doc.setTextColor(...GRIS)
    doc.text(generado, 14, alto - 8)
    doc.text(`Página ${i} de ${n}`, ancho - 14, alto - 8, { align: 'right' })
  }
}

/** Escribe un título de sección. `espacio` = lo mínimo que tiene que entrar debajo (evita títulos huérfanos). */
function seccion(doc, y, texto, espacio = 40) {
  if (y + espacio > doc.internal.pageSize.getHeight() - 15) { doc.addPage(); y = 20 }
  doc.setFont('helvetica', 'bold')
  doc.setFontSize(12)
  doc.setTextColor(...VERDE)
  doc.text(texto, 14, y)
  doc.setTextColor(...TEXTO)
  doc.setFont('helvetica', 'normal')
  return y + 4
}

function kpis(doc, y, items) {
  const ancho = doc.internal.pageSize.getWidth() - 28
  const w = ancho / items.length
  items.forEach(([etiqueta, valor], i) => {
    const x = 14 + i * w
    doc.setDrawColor(220, 228, 222)
    doc.setFillColor(246, 249, 246)
    doc.roundedRect(x, y, w - 4, 20, 2, 2, 'FD')
    doc.setFontSize(8)
    doc.setTextColor(...GRIS)
    doc.text(etiqueta.toUpperCase(), x + 4, y + 7)
    doc.setFontSize(12)
    doc.setFont('helvetica', 'bold')
    doc.setTextColor(...TEXTO)
    doc.text(String(valor), x + 4, y + 15)
    doc.setFont('helvetica', 'normal')
  })
  return y + 28
}

const estiloTabla = {
  styles: { fontSize: 8.5, cellPadding: 2.2, textColor: TEXTO, lineColor: [225, 231, 227], lineWidth: 0.1 },
  headStyles: { fillColor: [236, 242, 238], textColor: TEXTO, fontStyle: 'bold' },
  alternateRowStyles: { fillColor: [250, 252, 250] },
  margin: { left: 14, right: 14, bottom: 18 }, // bottom: deja lugar para el pie
}

function tabla(doc, y, head, body, opciones = {}) {
  autoTable(doc, { startY: y, head: [head], body, ...estiloTabla, ...opciones })
  return doc.lastAutoTable.finalY + 8
}

/** Convierte un color de Chart.js ('#rrggbb', 'rgb()', 'rgba()') a [r, g, b] para jsPDF */
function aRgb(color) {
  if (typeof color !== 'string') return [150, 150, 150]
  if (color.startsWith('#')) {
    let h = color.slice(1)
    if (h.length === 3) h = h.split('').map((c) => c + c).join('')
    return [0, 2, 4].map((i) => parseInt(h.slice(i, i + 2), 16))
  }
  const m = color.match(/\d+(\.\d+)?/g)
  return m ? m.slice(0, 3).map(Number) : [150, 150, 150]
}

/** Captura el canvas con los textos de los ejes oscurecidos (en pantalla son grises claros y en papel se lavan) */
function capturar(chart) {
  const escalas = Object.values(chart.options?.scales || {})
  const previos = escalas.map((s) => [s.ticks?.color, s.grid?.color])
  try {
    escalas.forEach((s) => {
      if (s.ticks) s.ticks.color = '#3a4540'
      if (s.grid) s.grid.color = '#e3e8e5'
    })
    chart.update('none')
    return chart.toBase64Image('image/png', 1)
  } finally {
    escalas.forEach((s, i) => {
      if (s.ticks) s.ticks.color = previos[i][0]
      if (s.grid) s.grid.color = previos[i][1]
    })
    chart.update('none')
  }
}

/** Leyenda dibujada a mano (la del canvas no se exporta) */
function leyenda(doc, x, y, chart) {
  const ds = chart.data?.datasets?.[0]
  const etiquetas = chart.data?.labels || []
  if (!ds) return
  const valores = ds.data.map(Number)
  const total = valores.reduce((a, b) => a + b, 0) || 1
  const colores = Array.isArray(ds.backgroundColor) ? ds.backgroundColor : etiquetas.map(() => ds.backgroundColor)
  doc.setFontSize(9)
  etiquetas.forEach((et, i) => {
    const yy = y + i * 8
    doc.setFillColor(...aRgb(colores[i]))
    doc.roundedRect(x, yy - 3.2, 4, 4, 0.8, 0.8, 'F')
    doc.setTextColor(...TEXTO)
    doc.setFont('helvetica', 'bold')
    doc.text(String(et), x + 7, yy)
    doc.setFont('helvetica', 'normal')
    doc.setTextColor(...GRIS)
    doc.text(`${kg1(valores[i])} kg · ${porcentaje((valores[i] / total) * 100)}`, x + 7, yy + 4)
  })
  doc.setTextColor(...TEXTO)
}

function imagenGrafico(doc, y, chart, alto = 70, { conLeyenda = false } = {}) {
  if (!chart?.canvas) return y
  try {
    const anchoUtil = doc.internal.pageSize.getWidth() - 28
    const proporcion = chart.canvas.width / chart.canvas.height
    if (y + alto > doc.internal.pageSize.getHeight() - 18) { doc.addPage(); y = 20 }
    const img = capturar(chart)
    if (conLeyenda) {
      // gráfico a la izquierda, leyenda a la derecha
      const w = Math.min(anchoUtil * 0.5, alto * proporcion)
      const x = 14 + (anchoUtil * 0.5 - w) / 2
      doc.addImage(img, 'PNG', x, y, w, alto)
      const n = chart.data?.labels?.length || 0
      leyenda(doc, 14 + anchoUtil * 0.55, y + alto / 2 - (n * 8) / 2 + 3, chart)
    } else {
      const w = Math.min(anchoUtil, alto * proporcion)
      doc.addImage(img, 'PNG', 14 + (anchoUtil - w) / 2, y, w, alto)
    }
    return y + alto + 6
  } catch {
    return y
  }
}

/** RFR07: descarga el detalle de un registro de emisiones */
export function pdfRegistro(r) {
  const doc = new jsPDF()
  let y = encabezado(doc, 'Registro de emisiones', periodo(r.periodo, true))
  y = kpis(doc, y, [
    ['Total', toneladasTexto(r.totalKgCo2)],
    ['kg CO2', kg1(r.totalKgCo2)],
    ['Consumos', r.detalles.length],
  ])
  doc.setFontSize(10)
  const datos = [
    ['Domicilio', `${r.domicilio}, ${r.ciudad}, ${r.provincia}`],
    ['Titular', r.titular],
    ['Cargado por', r.usuarioCarga + (r.esColaboracion ? ' (colaborador)' : '')],
    ['Última modificación', fechaHora(r.fechaUltimaModificacion)],
  ]
  if (r.observaciones) datos.push(['Observaciones', r.observaciones])
  y = tabla(doc, y, ['Dato', 'Valor'], datos, { theme: 'plain', columnStyles: { 0: { fontStyle: 'bold', cellWidth: 45 } } })
  y = seccion(doc, y, 'Detalle de consumos')
  tabla(doc, y,
    ['#', 'Fuente', 'Tipo / Alcance', 'Consumo', 'Factor', 'kg CO2'],
    r.detalles.map((d, i) => [i + 1, d.fuente, `${d.tipoEmision} · ${d.alcance}`, `${numero(d.consumo, 4)} ${d.unidadMedida}`, numero(d.valorFactorEmision, 6), kg1(d.kgCo2)]),
    { columnStyles: { 3: { halign: 'right' }, 4: { halign: 'right' }, 5: { halign: 'right' } } })
  pie(doc)
  doc.save(`registro-${r.periodo.slice(0, 7)}-${r.idRegistro}.pdf`)
}

/** Reporte con resumen, gráficos y tablas por categoría */
export function pdfReporte(rep, { nombre, graficos = {} } = {}) {
  const doc = new jsPDF()
  let y = encabezado(doc, nombre || (rep.esGlobal ? 'Reporte global de emisiones' : 'Reporte de emisiones'), fechaHora(rep.fechaGeneracion))

  doc.setFontSize(9)
  doc.setTextColor(...GRIS)
  rep.filtrosAplicados.forEach((f) => { doc.text(`• ${f}`, 14, y); y += 5 })
  doc.setTextColor(...TEXTO)
  y += 3

  const r = rep.resumen
  y = kpis(doc, y, [
    ['Total', toneladasTexto(r.totalKgCo2)],
    ['Promedio mensual', `${kg1(r.promedioMensualKgCo2)} kg`],
    ['Registros', r.cantidadRegistros],
    [rep.esGlobal ? 'Usuarios' : 'Domicilios', rep.esGlobal ? r.cantidadUsuarios : r.cantidadDomicilios],
  ])
  doc.setFontSize(8)
  doc.setTextColor(...GRIS)
  // El "₂" se pasa a "2" porque las fuentes estándar de jsPDF no tienen ese carácter
  doc.text(`Equivalente atmosférico: ${ppm(r.totalKgCo2)} (${EQUIVALENCIA_PPM.replace('₂', '2')})`, 14, y - 3)
  doc.setTextColor(...TEXTO)
  y += 4

  if (graficos.evolucion) {
    y = seccion(doc, y, 'Evolución mensual', 65 + 10)
    y = imagenGrafico(doc, y + 2, graficos.evolucion, 65)
  }
  if (graficos.alcances) {
    y = seccion(doc, y, 'Distribución por alcance', 55 + 10)
    y = imagenGrafico(doc, y + 2, graficos.alcances, 55, { conLeyenda: true })
  }

  const serie = (titulo, items, conGrupo) => {
    if (!items?.length) return
    y = seccion(doc, y, titulo)
    y = tabla(doc, y, conGrupo ? ['#', 'Nombre', conGrupo, 'kg CO2', '%'] : ['#', 'Nombre', 'kg CO2', '%'],
      items.map((s, i) => conGrupo
        ? [i + 1, s.nombre, s.grupo || '', kg1(s.kgCo2), porcentaje(s.porcentaje)]
        : [i + 1, s.nombre, kg1(s.kgCo2), porcentaje(s.porcentaje)]),
      { columnStyles: { [conGrupo ? 3 : 2]: { halign: 'right' }, [conGrupo ? 4 : 3]: { halign: 'right' } } })
  }

  if (rep.porPeriodo.length) {
    y = seccion(doc, y, 'Emisiones por período')
    y = tabla(doc, y, ['Período', 'kg CO2', 't CO2'], rep.porPeriodo.map((p) => [periodo(p.periodo, true), kg1(p.kgCo2), t3(p.kgCo2)]),
      { columnStyles: { 1: { halign: 'right' }, 2: { halign: 'right' } } })
  }
  serie('Por alcance', rep.porAlcance)
  serie('Por tipo de emisión', rep.porTipoEmision, 'Alcance')
  serie('Por fuente de emisión', rep.porFuente, 'Tipo')
  serie('Por tipo de usuario', rep.porRol)
  serie('Por provincia', rep.porProvincia)
  serie('Por distrito', rep.porDistrito, 'Provincia')
  serie('Por usuario', rep.porUsuario, 'Tipo')
  serie('Por domicilio', rep.porDomicilio, 'Titular')

  if (rep.filas.length) {
    doc.addPage('a4', 'landscape')
    // Cada fila es un consumo (no un registro): el KPI "Registros" cuenta otra cosa
    y = seccion(doc, 18, `Detalle de consumos (${rep.filas.length})`)
    tabla(doc, y,
      ['#', 'Período', 'Titular', 'Domicilio', 'Zona', 'Alcance', 'Fuente', 'Consumo', 'kg CO2'],
      rep.filas.map((f, i) => [i + 1, periodo(f.periodo), f.titular, f.domicilio, `${f.ciudad}, ${f.provincia}`, f.alcance, f.fuente,
        `${numero(f.consumo, 4)} ${f.unidadMedida}`, kg1(f.kgCo2)]),
      { styles: { ...estiloTabla.styles, fontSize: 7.5 }, columnStyles: { 7: { halign: 'right' }, 8: { halign: 'right' } } })
  }

  pie(doc)
  const fechaArchivo = new Date().toISOString().slice(0, 10)
  doc.save(`${(nombre || 'reporte-emisiones').toLowerCase().replace(/[^a-z0-9]+/gi, '-')}-${fechaArchivo}.pdf`)
}