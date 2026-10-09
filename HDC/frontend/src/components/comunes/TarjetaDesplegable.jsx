import { useId, useState } from 'react'
import Destello from './Destello.jsx'

/**
 * Tarjeta del dashboard. Plegada muestra lo mínimo (resumen); desplegada agrega el detalle.
 * Si no hay detalle, no muestra el control de desplegar.
 */
export default function TarjetaDesplegable({ titulo, subtitulo, icono, resumen, detalle, inicialAbierta = false, valorCambio, acciones, className = '' }) {
  const [abierta, setAbierta] = useState(inicialAbierta)
  const id = useId()
  const desplegable = !!detalle

  const alternar = () => desplegable && setAbierta((a) => !a)

  return (
    <Destello valor={valorCambio} className={`tarjeta tarjeta-hover tarjeta-desplegable ${abierta ? 'abierta' : ''} ${className}`}>
      <div
        className="cabecera"
        role={desplegable ? 'button' : undefined}
        tabIndex={desplegable ? 0 : undefined}
        aria-expanded={desplegable ? abierta : undefined}
        aria-controls={desplegable ? id : undefined}
        onClick={alternar}
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); alternar() } }}
      >
        {icono && <i className={`bi ${icono} fs-5`} style={{ color: 'var(--verde)' }} aria-hidden="true" />}
        <div className="ancho-min-0">
          <h2 className="tarjeta-titulo text-truncate">{titulo}</h2>
          {subtitulo && <div className="tarjeta-sub">{subtitulo}</div>}
        </div>
        {acciones && <div className="ms-auto" onClick={(e) => e.stopPropagation()}>{acciones}</div>}
        {desplegable && (
          <span className={`indicador-desplegar ${acciones ? '' : 'ms-auto'}`}>
            <span className="d-none d-sm-inline">{abierta ? 'Ver menos' : 'Ver más'}</span>
            <i className="bi bi-chevron-down chevron" style={{ marginLeft: 0 }} />
          </span>
        )}
      </div>
      {resumen && <div className="px-4 pb-3">{resumen}</div>}
      {desplegable && (
        <div className="contenido-extra" id={id}>
          <div>
            <div className="px-4 pb-4 pt-1">{abierta && detalle}</div>
          </div>
        </div>
      )}
    </Destello>
  )
}
