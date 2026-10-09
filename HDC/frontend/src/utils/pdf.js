import { jsPDF } from 'jspdf'
import autoTable from 'jspdf-autotable'
import { fechaHora, numero, periodo, porcentaje, ppm } from './formato'

// La fuente estándar de jsPDF no tiene el subíndice de CO₂: en el PDF se escribe CO2
const toneladasTexto = (kg) => `${numero(kg / 1000, 3)} t CO2`

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
  const ancho = doc.internal.pageSize.getWidth()
  const alto = doc.internal.pageSize.getHeight()
  for (let i = 1; i <= n; i++) {
    doc.setPage(i)
    doc.setFontSize(8)
    doc.setTextColor(...GRIS)
    doc.text(`Generado el ${fechaHora(new Date())}`, 14, alto - 8)
    doc.text(`Página ${i} de ${n}`, ancho - 14, alto - 8, { align: 'right' })
  }
}

function seccion(doc, y, texto) {
  if (y > doc.internal.pageSize.getHeight() - 40) { doc.addPage(); y = 20 }
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
  margin: { left: 14, right: 14 },
}

function tabla(doc, y, head, body, opciones = {}) {
  autoTable(doc, { startY: y, head: [head], body, ...estiloTabla, ...opciones })
  return doc.lastAutoTable.finalY + 8
}

function imagenGrafico(doc, y, chart, alto = 70) {
  if (!chart?.canvas) return y
  try {
    const ancho = doc.internal.pageSize.getWidth() - 28
    const proporcion = chart.canvas.width / chart.canvas.height
    const w = Math.min(ancho, alto * proporcion)
    if (y + alto > doc.internal.pageSize.getHeight() - 20) { doc.addPage(); y = 20 }
    doc.addImage(chart.toBase64Image('image/png', 1), 'PNG', 14 + (ancho - w) / 2, y, w, alto)
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
    ['kg CO2', numero(r.totalKgCo2)],
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
    r.detalles.map((d, i) => [i + 1, d.fuente, `${d.tipoEmision} · ${d.alcance}`, `${numero(d.consumo, 4)} ${d.unidadMedida}`, numero(d.valorFactorEmision, 6), numero(d.kgCo2)]),
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
    ['Promedio mensual', `${numero(r.promedioMensualKgCo2)} kg`],
    ['Registros', r.cantidadRegistros],
    [rep.esGlobal ? 'Usuarios' : 'Domicilios', rep.esGlobal ? r.cantidadUsuarios : r.cantidadDomicilios],
  ])
  doc.setFontSize(8)
  doc.setTextColor(...GRIS)
  doc.text(`Equivalente atmosférico: ${ppm(r.totalKgCo2)} (1 ppm = 17.600 millones de t CO2)`, 14, y - 3)
  doc.setTextColor(...TEXTO)
  y += 4

  if (graficos.evolucion) { y = seccion(doc, y, 'Evolución mensual'); y = imagenGrafico(doc, y + 2, graficos.evolucion, 65) }
  if (graficos.alcances) { y = seccion(doc, y, 'Distribución por alcance'); y = imagenGrafico(doc, y + 2, graficos.alcances, 55) }

  const serie = (titulo, items, conGrupo) => {
    if (!items?.length) return
    y = seccion(doc, y, titulo)
    y = tabla(doc, y, conGrupo ? ['#', 'Nombre', conGrupo, 'kg CO2', '%'] : ['#', 'Nombre', 'kg CO2', '%'],
      items.map((s, i) => conGrupo
        ? [i + 1, s.nombre, s.grupo || '', numero(s.kgCo2), porcentaje(s.porcentaje)]
        : [i + 1, s.nombre, numero(s.kgCo2), porcentaje(s.porcentaje)]),
      { columnStyles: { [conGrupo ? 3 : 2]: { halign: 'right' }, [conGrupo ? 4 : 3]: { halign: 'right' } } })
  }

  if (rep.porPeriodo.length) {
    y = seccion(doc, y, 'Emisiones por período')
    y = tabla(doc, y, ['Período', 'kg CO2', 't CO2'], rep.porPeriodo.map((p) => [periodo(p.periodo, true), numero(p.kgCo2), numero(p.kgCo2 / 1000, 3)]),
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
    y = seccion(doc, 18, `Detalle de registros (${rep.filas.length})`)
    tabla(doc, y,
      ['#', 'Período', 'Titular', 'Domicilio', 'Zona', 'Alcance', 'Fuente', 'Consumo', 'kg CO2'],
      rep.filas.map((f, i) => [i + 1, periodo(f.periodo), f.titular, f.domicilio, `${f.ciudad}, ${f.provincia}`, f.alcance, f.fuente,
        `${numero(f.consumo, 4)} ${f.unidadMedida}`, numero(f.kgCo2)]),
      { styles: { ...estiloTabla.styles, fontSize: 7.5 }, columnStyles: { 7: { halign: 'right' }, 8: { halign: 'right' } } })
  }

  pie(doc)
  const fechaArchivo = new Date().toISOString().slice(0, 10)
  doc.save(`${(nombre || 'reporte-emisiones').toLowerCase().replace(/[^a-z0-9]+/gi, '-')}-${fechaArchivo}.pdf`)
}
