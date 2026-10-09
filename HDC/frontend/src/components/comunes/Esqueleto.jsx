/** Placeholders animados que dan la sensación de "recibiendo datos" mientras carga. */
export function Esqueleto({ alto = 14, ancho = '100%', className = '', redondo }) {
  return <div className={`esqueleto ${className}`} style={{ height: alto, width: ancho, borderRadius: redondo ? '50%' : undefined }} aria-hidden="true" />
}

export function EsqueletoTabla({ filas = 5, columnas = 5 }) {
  return (
    <div className="d-flex flex-column gap-3" aria-busy="true" aria-label="Cargando">
      {Array.from({ length: filas }).map((_, i) => (
        <div key={i} className="d-flex gap-3">
          {Array.from({ length: columnas }).map((__, j) => (
            <Esqueleto key={j} alto={14} ancho={j === 0 ? '6%' : `${Math.round(94 / (columnas - 1))}%`} />
          ))}
        </div>
      ))}
    </div>
  )
}

export function EsqueletoTarjeta({ alto = 120 }) {
  return (
    <div className="tarjeta tarjeta-cuerpo" aria-busy="true">
      <Esqueleto alto={12} ancho="40%" className="mb-3" />
      <Esqueleto alto={alto - 50} className="mb-2" />
      <Esqueleto alto={10} ancho="60%" />
    </div>
  )
}

export function EsqueletoDona() {
  return (
    <div className="tarjeta tarjeta-cuerpo" aria-busy="true">
      <Esqueleto alto={12} ancho="45%" className="mb-4" />
      <div className="d-flex gap-4 align-items-center flex-wrap">
        <Esqueleto alto={170} ancho={170} redondo />
        <div className="flex-grow-1 d-flex flex-column gap-3">
          <Esqueleto alto={12} /><Esqueleto alto={12} ancho="80%" /><Esqueleto alto={12} ancho="65%" />
        </div>
      </div>
    </div>
  )
}

export function PuntosCarga({ texto = 'Recibiendo datos' }) {
  return (
    <span className="d-inline-flex align-items-center gap-2 text-muted small">
      <span className="puntos-carga"><span /><span /><span /></span>{texto}
    </span>
  )
}
