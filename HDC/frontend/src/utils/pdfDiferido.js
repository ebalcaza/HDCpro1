/*
 * Carga diferida (lazy) del generador de PDF.
 *
 * `pdf.js` usa jsPDF + jspdf-autotable (~430 kB). Si se importa normalmente, el navegador lo descarga
 * apenas se entra a Mis emisiones, Detalle de registro o Reportes, aunque el usuario nunca exporte nada.
 * Con import() dinámico, Vite lo separa en un archivo aparte que se descarga recién la primera vez
 * que se pide un PDF; las veces siguientes ya está en memoria y no se vuelve a bajar.
 */

/** Genera y descarga el PDF de un registro (mismos parámetros que pdfRegistro de pdf.js). */
export async function descargarPdfRegistro(registro) {
  const { pdfRegistro } = await import('./pdf')
  pdfRegistro(registro)
}

/** Genera y descarga el PDF de un reporte (mismos parámetros que pdfReporte de pdf.js). */
export async function descargarPdfReporte(reporte, opciones) {
  const { pdfReporte } = await import('./pdf')
  pdfReporte(reporte, opciones)
}
