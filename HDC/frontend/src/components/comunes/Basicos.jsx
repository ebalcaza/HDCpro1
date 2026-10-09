import { useEffect, useState } from 'react'

export function EncabezadoPagina({ titulo, descripcion, icono, children }) {
  return (
    <div className="encabezado-pagina aparecer">
      <div>
        <h1>{icono && <i className={`bi ${icono} me-2`} style={{ color: 'var(--verde)' }} aria-hidden="true" />}{titulo}</h1>
        {descripcion && <p>{descripcion}</p>}
      </div>
      {children && <div className="d-flex flex-wrap gap-2">{children}</div>}
    </div>
  )
}

export function Vacio({ icono = 'bi-inbox', titulo, children }) {
  return (
    <div className="vacio">
      <i className={`bi ${icono}`} aria-hidden="true" />
      {titulo && <div className="fw-bold texto-2 mb-1">{titulo}</div>}
      {children}
    </div>
  )
}

export function BotonCarga({ cargando, children, className = 'btn btn-primary', type = 'submit', ...props }) {
  return (
    <button type={type} className={className} disabled={cargando || props.disabled} {...props}>
      {cargando && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />}
      {children}
    </button>
  )
}

export function Dato({ etiqueta, children, className = '' }) {
  return (
    <div className={className}>
      <div className="dato-etiqueta">{etiqueta}</div>
      <div className="dato-valor">{children ?? '—'}</div>
    </div>
  )
}

/** Hook para cargar datos con estado de carga/error y función de recarga */
export function useCarga(fn, deps = []) {
  const [datos, setDatos] = useState(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let vigente = true
    setCargando(true)
    setError(null)
    fn()
      .then((d) => { if (vigente) setDatos(d) })
      .catch((e) => { if (vigente) setError(e) })
      .finally(() => { if (vigente) setCargando(false) })
    return () => { vigente = false }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, version])

  return { datos, setDatos, cargando, error, recargar: () => setVersion((v) => v + 1) }
}

export function ErrorCarga({ error, onReintentar }) {
  return (
    <div className="tarjeta tarjeta-cuerpo text-center py-5">
      <i className="bi bi-cloud-slash fs-1 text-muted d-block mb-2" />
      <div className="fw-bold mb-1">No se pudieron cargar los datos</div>
      <div className="text-muted small mb-3">{error?.message}</div>
      {onReintentar && <button type="button" className="btn btn-outline-primary btn-sm" onClick={onReintentar}><i className="bi bi-arrow-clockwise me-1" />Reintentar</button>}
    </div>
  )
}
